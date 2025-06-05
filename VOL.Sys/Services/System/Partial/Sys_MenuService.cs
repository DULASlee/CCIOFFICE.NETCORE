using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VOL.Core.Extensions;
using VOL.Core.ManageUser;
using VOL.Core.Services;
using VOL.Core.Utilities;
using VOL.Entity;
using VOL.Entity.DomainModels;

namespace VOL.Sys.Services
{
    public partial class Sys_MenuService
    {
        /// <summary>
        /// 菜单静态化处理，每次获取菜单时先比较菜单是否发生变化，如果发生变化从数据库重新获取，否则直接获取_menus菜单
        /// </summary>
        private static List<Sys_Menu> _menus { get; set; }

        /// <summary>
        /// 从数据库获取菜单时锁定的对象
        /// </summary>
        private static object _menuObj = new object();

        /// <summary>
        /// 当前服务器的菜单版本
        /// </summary>
        private static string _menuVersionn = "";

        private const string _menuCacheKey = "inernalMenu";

        /// <summary>
        /// 编辑修改菜单时,获取所有菜单
        /// </summary>
        /// <returns></returns>
        public async Task<object> GetMenu()
        {
            try
            {
                //  DBServerProvider.SqlDapper.q
                var menus = await repository.FindAsync(x => 1 == 1, a =>
                 new
                 {
                     id = a.Menu_Id,
                     parentId = a.ParentId,
                     name = a.MenuName,
                     a.MenuType,
                     a.OrderNo
                 });
                return menus.OrderByDescending(a => a.OrderNo)
                            .ThenByDescending(q => q.parentId).ToList();
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LoggerType.Select, "获取所有菜单失败 (GetMenu)", null, null, ex);
                return new List<object>(); // Return empty list or handle error as appropriate
            }
        }

        private List<Sys_Menu> GetAllMenu()
        {
            //每次比较缓存是否更新过，如果更新则重新获取数据
            string _currentCacheVersion = null;
            try
            {
                _currentCacheVersion = CacheContext.Get(_menuCacheKey);
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LoggerType.Exception, "获取菜单缓存版本失败 (GetAllMenu)", null, null, ex);
                // Proceed without cache version check, will attempt to load from DB.
            }

            if (_menuVersionn != "" && _menuVersionn == _currentCacheVersion && _menus != null)
            {
                return _menus;
            }

            lock (_menuObj)
            {
                // Double check after acquiring lock
                if (_menuVersionn != "" && _menus != null && _menuVersionn == _currentCacheVersion) return _menus;
                try
                {
                    //2020.12.27增加菜单界面上不显示，但可以分配权限
                    _menus = repository.FindAsIQueryable(x => x.Enable == 1 || x.Enable == 2)
                        .OrderByDescending(a => a.OrderNo)
                        .ThenByDescending(q => q.ParentId).ToList();

                    _menus.ForEach(x =>
                    {
                        // 2022.03.26增移动端加菜单类型
                        x.MenuType ??= 0;
                        if (!string.IsNullOrEmpty(x.Auth) && x.Auth.Length > 10)
                        {
                            try
                            {
                                x.Actions = x.Auth.DeserializeObject<List<Sys_Actions>>();
                            }
                            catch (Exception dex)
                            {
                                VOL.Core.Services.Logger.Warning(VOL.Core.Enums.LoggerType.Exception, $"菜单权限JSON反序列化失败: MenuId={x.Menu_Id}, AuthString='{x.Auth}'", null, null, dex);
                                // Keep x.Actions as null or new List if deserialization fails
                            }
                        }
                        if (x.Actions == null) x.Actions = new List<Sys_Actions>();
                    });

                    string updatedCacheVersion = _currentCacheVersion; // Use the version fetched before lock, if available
                    if (string.IsNullOrEmpty(updatedCacheVersion)) // If initial Get failed or was empty
                    {
                        updatedCacheVersion = CacheContext.Get(_menuCacheKey); // Try fetching again inside lock
                    }

                    if (string.IsNullOrEmpty(updatedCacheVersion))
                    {
                        updatedCacheVersion = DateTime.Now.ToString("yyyyMMddHHMMssfff");
                        CacheContext.Add(_menuCacheKey, updatedCacheVersion);
                    }
                    _menuVersionn = updatedCacheVersion;
                }
                catch (Exception ex)
                {
                    VOL.Core.Services.Logger.Error(VOL.Core.Enums.LoggerType.Select, "从数据库或缓存加载菜单失败 (GetAllMenu)", null, null, ex);
                    // If _menus is null or empty due to error, return an empty list to prevent NullReferenceException downstream
                    return _menus ?? new List<Sys_Menu>();
                }
            }
            return _menus ?? new List<Sys_Menu>(); // Ensure non-null return
        }

        /// <summary>
        /// 获取当前用户有权限查看的菜单
        /// </summary>
        /// <returns></returns>
        public List<Sys_Menu> GetCurrentMenuList()
        {
            int roleId = UserContext.Current.RoleId;
            return GetUserMenuList(roleId);
        }


        public List<Sys_Menu> GetUserMenuList(int roleId)
        {
            if (UserContext.IsRoleIdSuperAdmin(roleId))
            {
                return GetAllMenu();
            }
            List<int> menuIds = UserContext.Current.GetPermissions(roleId).Select(x => x.Menu_Id).ToList();
            return GetAllMenu().Where(x => menuIds.Contains(x.Menu_Id)).ToList();
        }

        /// <summary>
        /// 获取当前用户所有菜单与权限
        /// </summary>
        /// <returns></returns>
        public object GetCurrentMenuActionList()
        {
            return GetMenuActionList(UserContext.Current.RoleId);
        }

        /// <summary>
        /// 根据角色ID获取菜单与权限
        /// </summary>
        /// <param name="roleId"></param>
        /// <returns></returns>
        public object GetMenuActionList(int roleId)
        {
            if (UserContext.IsRoleIdSuperAdmin(roleId))
            {
                return GetAllMenu()
                .Where(c => c.MenuType == UserContext.MenuType)
                .Select(x =>
                new
                {
                    id = x.Menu_Id,
                    name = x.MenuName,
                    url = x.Url,
                    parentId = x.ParentId,
                    icon = x.Icon,
                    x.Enable,
                    x.TableName, // 2022.03.26增移动端加菜单类型
                    permission = x.Actions.Select(s => s.Value).ToArray()
                }).ToList();
            }

            var menu = from a in UserContext.Current.Permissions
                       join b in GetAllMenu().Where(c => c.MenuType == UserContext.MenuType)
                       on a.Menu_Id equals b.Menu_Id
                       orderby b.OrderNo descending
                       select new
                       {
                           id = a.Menu_Id,
                           name = b.MenuName,
                           url = b.Url,
                           parentId = b.ParentId,
                           icon = b.Icon,
                           b.Enable,
                           b.TableName, // 2022.03.26增移动端加菜单类型
                           permission = a.UserAuthArr
                       };
            return menu.ToList();
        }

        /// <summary>
        /// 新建或编辑菜单
        /// </summary>
        /// <param name="menu"></param>
        /// <returns></returns>
        public async Task<WebResponseContent> Save(Sys_Menu menu)
        {
            WebResponseContent webResponse = new WebResponseContent();
            if (menu == null) return webResponse.Error("没有获取到提交的参数");
            if (menu.Menu_Id > 0 && menu.Menu_Id == menu.ParentId) return webResponse.Error("父级ID不能是当前菜单的ID");
            try
            {
                webResponse = menu.ValidationEntity(x => new { x.MenuName, x.TableName });
                if (!webResponse.Status) return webResponse;
                if (menu.TableName != "/" && menu.TableName != ".")
                {
                    // 2022.03.26增移动端加菜单类型判断
                    Sys_Menu sysMenu = await repository.FindAsyncFirst(x => x.TableName == menu.TableName);
                    if (sysMenu != null)
                    {
                        sysMenu.MenuType ??= 0;
                        if (sysMenu.MenuType == menu.MenuType)
                        {
                            if ((menu.Menu_Id > 0 && sysMenu.Menu_Id != menu.Menu_Id)
                            || menu.Menu_Id <= 0)
                            {
                                return webResponse.Error($"视图/表名【{menu.TableName}】已被其他菜单使用");
                            }
                        }
                    }
                }
                bool _changed = false;
                if (menu.Menu_Id <= 0)
                {
                    repository.Add(menu.SetCreateDefaultVal());
                }
                else
                {
                    //2020.05.07新增禁止选择上级角色为自己
                    if (menu.Menu_Id == menu.ParentId)
                    {
                        return webResponse.Error($"父级id不能为自己");
                    }
                    if (repository.Exists(x => x.ParentId == menu.Menu_Id && menu.ParentId == x.Menu_Id))
                    {
                        return webResponse.Error($"不能选择此父级id，选择的父级id与当前菜单形成依赖关系");
                    }

                    _changed = repository.FindAsIQueryable(c => c.Menu_Id == menu.Menu_Id).Select(s => s.Auth).FirstOrDefault() != menu.Auth;

                    repository.Update(menu.SetModifyDefaultVal(), p => new
                    {
                        p.ParentId,
                        p.MenuName,
                        p.Url,
                        p.Auth,
                        p.OrderNo,
                        p.Icon,
                        p.Enable,
                        p.MenuType,// 2022.03.26增移动端加菜单类型
                        p.TableName,
                        p.ModifyDate,
                        p.Modifier
                    });
                }
                await repository.SaveChangesAsync();

                CacheContext.Add(_menuCacheKey, DateTime.Now.ToString("yyyyMMddHHMMssfff"));
                if (_changed)
                {
                    UserContext.Current.RefreshWithMenuActionChange(menu.Menu_Id);
                }
                _menus = null;
                webResponse.OK("保存成功", menu);
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LoggerType.Exception, $"保存菜单失败: {menu?.MenuName}", menu?.Serialize(), null, ex);
                webResponse.Error("保存菜单时发生内部错误，请联系管理员。");
            }
            finally
            {
                // Log outcome: Success or Failure with context
                var logMessage = $"保存菜单操作: 表:{menu?.TableName}, 菜单：{menu?.MenuName}, 权限:{menu?.Auth}, 结果:{(webResponse.Status ? "成功" : "失败")}, 消息:{webResponse.Message}";
                if (webResponse.Status)
                {
                    VOL.Core.Services.Logger.Info(VOL.Core.Enums.LoggerType.Save,logMessage, menu?.Serialize(), webResponse.Serialize());
                }
                else
                {
                    // Already logged with full exception in catch block if an exception occurred.
                    // If webResponse.Status is false due to business logic (not exception), this provides context.
                    if (webResponse.Message?.Contains("内部错误")!=true) // Avoid duplicate logging if already logged by catch
                         VOL.Core.Services.Logger.Warning(VOL.Core.Enums.LoggerType.Save, logMessage, menu?.Serialize(), webResponse.Serialize());
                }
            }
            return webResponse;

        }

        public async Task<WebResponseContent> DelMenu(int menuId)
        {
            WebResponseContent webResponse = new WebResponseContent();
            try
            {
                if (await repository.ExistsAsync(x => x.ParentId == menuId))
                {
                    return webResponse.Error("当前菜单存在子菜单,请先删除子菜单!");
                }
                // The Delete method in repository might not be async, check its signature.
                // If it's not async, this SaveChangesAsync is good.
                // If Delete itself is async or calls SaveChangesAsync, this might be redundant or cause issues.
                // Assuming repository.Delete is synchronous for now.
                repository.Delete(new Sys_Menu() { Menu_Id = menuId }, true);
                await repository.SaveChangesAsync(); // Ensure changes are persisted if Delete itself doesn't.

                CacheContext.Add(_menuCacheKey, DateTime.Now.ToString("yyyyMMddHHMMssfff"));
                return webResponse.OK("删除成功");
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LoggerType.Delete, $"删除菜单失败: MenuId={menuId}", new { menuId }, null, ex);
                return webResponse.Error("删除菜单时发生内部错误，请联系管理员。");
            }
        }
        /// <summary>
        /// 编辑菜单时，获取菜单信息
        /// </summary>
        /// <param name="menuId"></param>
        /// <returns></returns>
        public async Task<object> GetTreeItem(int menuId)
        {
            try
            {
                var menuData = await base.repository.FindAsync(x => x.Menu_Id == menuId);
                var sysMenu = menuData.Select(
                    p => new
                    {
                        p.Menu_Id,
                        p.ParentId,
                        p.MenuName,
                        p.Url,
                        p.Auth,
                        p.OrderNo,
                        p.Icon,
                        p.Enable,
                        // 2022.03.26增移动端加菜单类型
                        MenuType = p.MenuType ?? 0,
                        p.CreateDate,
                        p.Creator,
                        p.TableName,
                        p.ModifyDate
                    }).FirstOrDefault();
                return sysMenu;
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LoggerType.Select, $"获取菜单项失败: MenuId={menuId}", new { menuId }, null, ex);
                return null; // Or throw, or return a specific error object
            }
        }
    }
}

