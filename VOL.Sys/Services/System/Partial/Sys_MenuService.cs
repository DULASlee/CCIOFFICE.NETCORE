using Microsoft.EntityFrameworkCore;
using Serilog;
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
        public Sys_MenuService() {
            Log.ForContext<Sys_MenuService>();
        }
        
        /// <summary>
        /// 菜单静态化处理，每次获取菜单时先比较菜单是否发生变化，如果发生变化从数据库重新获取，否则直接获取_menus菜单
        /// </summary>
        private static List<Sys_Menu> _menus { get; set; }

        /// <summary>
        /// 从数据库获取菜单时锁定的对象
        /// </summary>
        private static readonly object _menuObj = new object();

        /// <summary>
        /// 当前服务器的菜单版本
        /// </summary>
        private static string _menuVersion = "";

        /// <summary>
        /// 菜单缓存键
        /// </summary>
        private const string _menuCacheKey = "inernalMenu";

        /// <summary>
        /// 编辑修改菜单时,获取所有菜单
        /// </summary>
        /// <returns></returns>
        public async Task<object> GetMenu()
        {
            try
            {
                var menuList = await repository.FindAsync(x => 1 == 1, a =>
                 new
                 {
                     id = a.Menu_Id,
                     parentId = a.ParentId,
                     name = a.MenuName,
                     a.MenuType,
                     a.OrderNo
                 });

                if (menuList == null)
                {
                    return new List<object>();
                }

                return menuList
                    .OrderByDescending(a => a.OrderNo)
                    .ThenByDescending(q => q.parentId)
                    .ToList();
            }
            catch (Exception ex)
            {
                Log.Error( $"获取菜单列表失败: {ex.Message}", ex);
                return new List<object>();
            }
        }

        /// <summary>
        /// 获取所有菜单（带缓存）
        /// </summary>
        private List<Sys_Menu> GetAllMenu()
        {
            try
            {
                //每次比较缓存是否更新过，如果更新则重新获取数据
                string _cacheVersion = CacheContext.Get(_menuCacheKey);

                // 使用双重检查锁定模式
                if (!string.IsNullOrEmpty(_menuVersion) && _menuVersion == _cacheVersion && _menus != null)
                {
                    return new List<Sys_Menu>(_menus); // 返回副本，避免外部修改
                }

                lock (_menuObj)
                {
                    // 再次检查，避免重复加载
                    _cacheVersion = CacheContext.Get(_menuCacheKey);
                    if (!string.IsNullOrEmpty(_menuVersion) && _menus != null && _menuVersion == _cacheVersion)
                    {
                        return new List<Sys_Menu>(_menus);
                    }

                    //2020.12.27增加菜单界面上不显示，但可以分配权限
                    var menuList = repository.FindAsIQueryable(x => x.Enable == 1 || x.Enable == 2)
                        .OrderByDescending(a => a.OrderNo)
                        .ThenByDescending(q => q.ParentId)
                        .ToList();

                    if (menuList == null)
                    {
                        menuList = new List<Sys_Menu>();
                    }

                    menuList.ForEach(x =>
                    {
                        // 2022.03.26增移动端加菜单类型
                        x.MenuType = x.MenuType ?? 0;

                        // 安全地反序列化权限数据
                        if (!string.IsNullOrEmpty(x.Auth) && x.Auth.Length > 10)
                        {
                            try
                            {
                                x.Actions = x.Auth.DeserializeObject<List<Sys_Actions>>();
                            }
                            catch (Exception ex)
                            {
                                Logger.Error(Core.Enums.LoggerType.System,
                                    $"菜单权限反序列化失败: MenuId={x.Menu_Id}, Auth={x.Auth}, Error={ex.Message}");
                                x.Actions = new List<Sys_Actions>();
                            }
                        }

                        if (x.Actions == null)
                        {
                            x.Actions = new List<Sys_Actions>();
                        }
                    });

                    // 更新缓存版本
                    string cacheVersion = CacheContext.Get(_menuCacheKey);
                    if (string.IsNullOrEmpty(cacheVersion))
                    {
                        cacheVersion = DateTime.Now.ToString("yyyyMMddHHMMssfff");
                        CacheContext.Add(_menuCacheKey, cacheVersion);
                    }

                    _menuVersion = cacheVersion;
                    _menus = menuList;
                }

                return new List<Sys_Menu>(_menus);
            }
            catch (Exception ex)
            {
                Log.Error( $"获取所有菜单失败: {ex.Message}", ex);
                return new List<Sys_Menu>();
            }
        }

        /// <summary>
        /// 获取当前用户有权限查看的菜单
        /// </summary>
        /// <returns></returns>
        public List<Sys_Menu> GetCurrentMenuList()
        {
            try
            {
                int roleId = UserContext.Current.RoleId;
                return GetUserMenuList(roleId);
            }
            catch (Exception ex)
            {
                Log.Error($"获取当前用户菜单失败: {ex.Message}", ex);
                return new List<Sys_Menu>();
            }
        }

        /// <summary>
        /// 根据角色ID获取用户菜单列表
        /// </summary>
        public List<Sys_Menu> GetUserMenuList(int roleId)
        {
            if (roleId <= 0)
            {
                return new List<Sys_Menu>();
            }

            try
            {
                // 超级管理员获取所有菜单
                if (UserContext.IsRoleIdSuperAdmin(roleId))
                {
                    return GetAllMenu();
                }

                // 获取角色的菜单权限
                var permissions = UserContext.Current.GetPermissions(roleId);
                if (permissions == null || permissions.Count == 0)
                {
                    return new List<Sys_Menu>();
                }

                List<int> menuIds = permissions.Select(x => x.Menu_Id).Distinct().ToList();
                var allMenus = GetAllMenu();

                return allMenus.Where(x => menuIds.Contains(x.Menu_Id)).ToList();
            }
            catch (Exception ex)
            {
                Log.Error( $"获取用户菜单失败: RoleId={roleId}, {ex.Message}", ex);
                return new List<Sys_Menu>();
            }
        }

        /// <summary>
        /// 获取当前用户所有菜单与权限
        /// </summary>
        /// <returns></returns>
        public object GetCurrentMenuActionList()
        {
            try
            {
                return GetMenuActionList(UserContext.Current.RoleId);
            }
            catch (Exception ex)
            {
                Log.Error( $"获取当前用户菜单权限失败: {ex.Message}", ex);
                return new List<object>();
            }
        }

        /// <summary>
        /// 根据角色ID获取菜单与权限
        /// </summary>
        /// <param name="roleId"></param>
        /// <returns></returns>
        public object GetMenuActionList(int roleId)
        {
            if (roleId <= 0)
            {
                return new List<object>();
            }

            try
            {
                var menuType = UserContext.MenuType;
                var allMenus = GetAllMenu().Where(c => c.MenuType == menuType).ToList();

                // 超级管理员返回所有菜单
                if (UserContext.IsRoleIdSuperAdmin(roleId))
                {
                    return allMenus
                    .Select(x => new
                    {
                        id = x.Menu_Id,
                        name = x.MenuName,
                        url = x.Url,
                        parentId = x.ParentId,
                        icon = x.Icon,
                        x.Enable,
                        x.TableName, // 2022.03.26增移动端加菜单类型
                        permission = x.Actions?.Select(s => s.Value).ToArray() ?? new string[0]
                    }).ToList();
                }

                // 普通用户根据权限返回菜单
                var permissions = UserContext.Current.GetPermissions(roleId);
                if (permissions == null || permissions.Count == 0)
                {
                    return new List<object>();
                }

                var menu = from a in permissions
                           join b in allMenus on a.Menu_Id equals b.Menu_Id
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
                               permission = a.UserAuthArr ?? new string[0]
                           };

                return menu.ToList();
            }
            catch (Exception ex)
            {
                Log.Error($"获取菜单权限列表失败: RoleId={roleId}, {ex.Message}", ex);
                return new List<object>();
            }
        }

        /// <summary>
        /// 新建或编辑菜单
        /// </summary>
        /// <param name="menu"></param>
        /// <returns></returns>
        public async Task<WebResponseContent> Save(Sys_Menu menu)
        {
            WebResponseContent webResponse = new WebResponseContent();

            // 参数验证
            if (menu == null)
            {
                return webResponse.Error("没有获取到提交的参数");
            }

            if (menu.Menu_Id > 0 && menu.Menu_Id == menu.ParentId)
            {
                return webResponse.Error("父级ID不能是当前菜单的ID");
            }

            bool authChanged = false;

            try
            {
                // 实体验证
                webResponse = menu.ValidationEntity(x => new { x.MenuName, x.TableName });
                if (!webResponse.Status)
                {
                    return webResponse;
                }

                // 检查表名是否被占用
                if (!string.IsNullOrEmpty(menu.TableName) && menu.TableName != "/" && menu.TableName != ".")
                {
                    // 2022.03.26增移动端加菜单类型判断
                    Sys_Menu existingMenu = await repository.FindAsyncFirst(x => x.TableName == menu.TableName);
                    if (existingMenu != null)
                    {
                        existingMenu.MenuType = existingMenu.MenuType ?? 0;
                        menu.MenuType = menu.MenuType ?? 0;

                        if (existingMenu.MenuType == menu.MenuType)
                        {
                            if ((menu.Menu_Id > 0 && existingMenu.Menu_Id != menu.Menu_Id) || menu.Menu_Id <= 0)
                            {
                                return webResponse.Error($"视图/表名【{menu.TableName}】已被其他菜单使用");
                            }
                        }
                    }
                }

                if (menu.Menu_Id <= 0)
                {
                    // 新增菜单
                    repository.Add(menu.SetCreateDefaultVal());
                }
                else
                {
                    // 编辑菜单
                    // 2020.05.07新增禁止选择上级角色为自己
                    if (menu.Menu_Id == menu.ParentId)
                    {
                        return webResponse.Error("父级id不能为自己");
                    }

                    // 检查循环依赖
                    if (repository.Exists(x => x.ParentId == menu.Menu_Id && menu.ParentId == x.Menu_Id))
                    {
                        return webResponse.Error("不能选择此父级id，选择的父级id与当前菜单形成循环依赖关系");
                    }

                    // 检查是否存在更深层的循环依赖
                    if (menu.ParentId > 0 && await CheckCircularDependency(menu.Menu_Id, menu.ParentId))
                    {
                        return webResponse.Error("不能选择此父级id，会形成循环依赖关系");
                    }

                    // 检查权限是否变更
                    string oldAuth = repository.FindAsIQueryable(c => c.Menu_Id == menu.Menu_Id)
                        .Select(s => s.Auth)
                        .FirstOrDefault();
                    authChanged = oldAuth != menu.Auth;

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

                // 清除缓存
                RefreshMenuCache();

                // 如果权限变更，刷新用户权限缓存
                if (authChanged)
                {
                    UserContext.Current.RefreshWithMenuActionChange(menu.Menu_Id);
                }

                webResponse.OK("保存成功", menu);
            }
            catch (Exception ex)
            {
                Log.Error( $"保存菜单失败: {ex.Message}", ex);
                webResponse.Error("保存菜单失败，请稍后重试");
            }
            finally
            {
                Logger.Info($"保存菜单: 表名={menu?.TableName}, 菜单名={menu?.MenuName}, " +
                    $"权限={menu?.Auth}, 结果={(webResponse.Status ? "成功" : "失败")}, {webResponse.Message}");
            }

            return webResponse;
        }

        /// <summary>
        /// 检查循环依赖
        /// </summary>
        private async Task<bool> CheckCircularDependency(int menuId, int parentId)
        {
            if (parentId <= 0) return false;

            // 获取所有子菜单ID
            var childrenIds = new HashSet<int>();
            await GetAllChildrenIds(menuId, childrenIds);

            return childrenIds.Contains(parentId);
        }

        /// <summary>
        /// 递归获取所有子菜单ID
        /// </summary>
        private async Task GetAllChildrenIds(int parentId, HashSet<int> childrenIds)
        {
            var children = await repository.FindAsync(x => x.ParentId == parentId, s => s.Menu_Id);
            foreach (var childId in children)
            {
                if (childrenIds.Add(childId))
                {
                    await GetAllChildrenIds(childId, childrenIds);
                }
            }
        }

        /// <summary>
        /// 删除菜单
        /// </summary>
        public async Task<WebResponseContent> DelMenu(int menuId)
        {
            WebResponseContent webResponse = new WebResponseContent();

            if (menuId <= 0)
            {
                return webResponse.Error("菜单ID无效");
            }

            try
            {
                // 检查是否存在子菜单
                if (await repository.ExistsAsync(x => x.ParentId == menuId))
                {
                    return webResponse.Error("当前菜单存在子菜单，请先删除子菜单!");
                }

                // 检查是否有角色使用此菜单权限
                bool hasRoleAuth = await repository.DbContext.Set<Sys_RoleAuth>()
                    .AnyAsync(x => x.Menu_Id == menuId && !string.IsNullOrEmpty(x.AuthValue));

                if (hasRoleAuth)
                {
                    return webResponse.Error("当前菜单已分配给角色，请先移除角色权限!");
                }

                repository.Delete(new Sys_Menu() { Menu_Id = menuId }, true);

                await repository.SaveChangesAsync();

                // 清除缓存
                RefreshMenuCache();

                webResponse.OK("删除成功");
            }
            catch (Exception ex)
            {
                Log.Error( $"删除菜单失败: MenuId={menuId}, {ex.Message}", ex);
                webResponse.Error("删除菜单失败，请稍后重试");
            }

            return webResponse;
        }

        /// <summary>
        /// 编辑菜单时，获取菜单信息
        /// </summary>
        /// <param name="menuId"></param>
        /// <returns></returns>
        public async Task<object> GetTreeItem(int menuId)
        {
            if (menuId <= 0)
            {
                return null;
            }

            try
            {
                var menuList = await repository.FindAsync(x => x.Menu_Id == menuId);
                if (menuList == null || menuList.Count == 0)
                {
                    return null;
                }

                var sysMenu = menuList
                    .Select(p => new
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
                    })
                    .FirstOrDefault();

                return sysMenu;
            }
            catch (Exception ex)
            {
                Log.Error( $"获取菜单详情失败: MenuId={menuId}, {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 刷新菜单缓存
        /// </summary>
        private void RefreshMenuCache()
        {
            try
            {
                lock (_menuObj)
                {
                    _menus = null;
                    _menuVersion = "";
                    CacheContext.Add(_menuCacheKey, DateTime.Now.ToString("yyyyMMddHHMMssfff"));
                }
            }
            catch (Exception ex)
            {
                Log.Error( $"刷新菜单缓存失败: {ex.Message}", ex);
            }
        }
    }
}