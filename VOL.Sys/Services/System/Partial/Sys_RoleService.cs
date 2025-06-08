using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using VOL.Core.Extensions;
using VOL.Core.ManageUser;
using VOL.Core.Services;
using VOL.Core.UserManager;
using VOL.Core.Utilities;
using VOL.Entity;
using VOL.Entity.DomainModels;

namespace VOL.Sys.Services
{
    public partial class Sys_RoleService
    {  
        public Sys_RoleService() {
            Log.ForContext<Sys_UserService>();
        }

        private WebResponseContent _responseContent = new WebResponseContent();

        /// <summary>
        /// 获取角色分页数据
        /// </summary>
        /// <param name="pageData"></param>
        /// <returns></returns>
        public override PageGridData<Sys_Role> GetPageData(PageDataOptions pageData)
        {
            try
            {
                //角色Id=1默认为超级管理员角色，界面上不显示此角色
                QueryRelativeExpression = (IQueryable<Sys_Role> queryable) =>
                {
                    if (queryable == null) return queryable;

                    if (UserContext.Current.IsSuperAdmin)
                    {
                        return queryable;
                    }

                    List<int> roleIds = GetAllChildrenRoleIdAndSelf();
                    return queryable.Where(x => roleIds.Contains(x.Role_Id));
                };

                return base.GetPageData(pageData);
            }
            catch (Exception ex)
            {
                Log.Error( $"获取角色分页数据失败: {ex.Message}", ex);
                return new PageGridData<Sys_Role>
                {
                    rows = new List<Sys_Role>(),
                    total = 0,
                    msg = "获取数据失败"
                };
            }
        }

        /// <summary>
        /// 编辑权限时，获取当前用户的所有菜单权限
        /// </summary>
        /// <returns></returns>
        public async Task<WebResponseContent> GetCurrentUserTreePermission()
        {
            try
            {
                return await GetUserTreePermission(UserContext.Current.RoleId);
            }
            catch (Exception ex)
            {
                Log.Error($"获取当前用户权限树失败: {ex.Message}", ex);
                return _responseContent.Error("获取权限信息失败");
            }
        }

        /// <summary>
        /// 编辑权限时，获取指定角色的所有菜单权限
        /// </summary>
        /// <param name="roleId"></param>
        /// <returns></returns>
        public async Task<WebResponseContent> GetUserTreePermission(int roleId)
        {
            if (roleId <= 0)
            {
                return _responseContent.Error("角色ID无效");
            }

            try
            {
                // 权限验证
                if (!UserContext.IsRoleIdSuperAdmin(roleId) && UserContext.Current.RoleId != roleId)
                {
                    var children = await GetAllChildrenAsync(UserContext.Current.RoleId);
                    if (!children.Exists(x => x.Id == roleId))
                    {
                        return _responseContent.Error("没有权限获取此角色的权限信息");
                    }
                }

                //获取用户权限
                List<Permissions> permissions = UserContext.Current.GetPermissions(roleId);

                //权限用户权限查询所有的菜单信息
                List<Sys_Menu> menus = await Task.Run(() => Sys_MenuService.Instance.GetUserMenuList(roleId));

                if (menus == null)
                {
                    menus = new List<Sys_Menu>();
                }

                //获取当前用户权限如:(Add,Search)对应的显示文本信息如:Add：添加，Search:查询
                var data = menus.Select(x => new
                {
                    Id = x.Menu_Id,
                    Pid = x.ParentId,
                    Text = x.MenuName,
                    IsApp = x.MenuType == 1,
                    Actions = GetActions(x.Menu_Id, x.Actions, permissions, roleId)
                });

                return _responseContent.OK(null, data);
            }
            catch (Exception ex)
            {
                Log.Error($"获取角色权限树失败: RoleId={roleId}, {ex.Message}", ex);
                return _responseContent.Error("获取权限信息失败");
            }
        }

        /// <summary>
        /// 获取菜单操作权限
        /// </summary>
        private List<Sys_Actions> GetActions(int menuId, List<Sys_Actions> menuActions, List<Permissions> permissions, int roleId)
        {
            if (menuActions == null)
            {
                return new List<Sys_Actions>();
            }

            if (UserContext.IsRoleIdSuperAdmin(roleId))
            {
                return menuActions;
            }

            if (permissions == null)
            {
                return new List<Sys_Actions>();
            }

            return menuActions.Where(p => permissions
                 .Exists(w => menuId == w.Menu_Id
                 && w.UserAuthArr != null
                 && w.UserAuthArr.Contains(p.Value)))
                  .ToList();
        }

        /// <summary>
        /// 编辑权限时获取当前用户下的所有角色与当前用户的菜单权限
        /// </summary>
        /// <returns></returns>
        public async Task<WebResponseContent> GetCurrentTreePermission()
        {
            try
            {
                _responseContent = await GetCurrentUserTreePermission();
                int roleId = UserContext.Current.RoleId;

                var children = await GetAllChildrenAsync(roleId);

                return _responseContent.OK(null, new
                {
                    tree = _responseContent.Data,
                    roles = children
                });
            }
            catch (Exception ex)
            {
                Log.Error( $"获取当前权限树失败: {ex.Message}", ex);
                return _responseContent.Error("获取权限信息失败");
            }
        }

        /// <summary>
        /// 获取当前角色下的所有角色包括自己的角色Id
        /// </summary>
        /// <returns></returns>
        public List<int> GetAllChildrenRoleIdAndSelf()
        {
            try
            {
                int roleId = UserContext.Current.RoleId;
                List<int> roleIds = GetAllChildren(roleId).Select(x => x.Id).ToList();
                roleIds.Add(roleId);
                return roleIds;
            }
            catch (Exception ex)
            {
                Log.Error( $"获取所有子角色ID失败: {ex.Message}", ex);
                return new List<int> { UserContext.Current.RoleId };
            }
        }

        /// <summary>
        /// 获取当前角色下的所有角色
        /// </summary>
        /// <param name="roleId"></param>
        /// <returns></returns>
        public List<RoleNodes> GetAllChildren(int roleId)
        {
            if (roleId <= 0)
            {
                return new List<RoleNodes>();
            }

            try
            {
                var allRoles = GetAllRoleQueryable(roleId).ToList();
                return GetAllChildrenNodes(roleId);
            }
            catch (Exception ex)
            {
                Log.Error( $"获取所有子角色失败: RoleId={roleId}, {ex.Message}", ex);
                return new List<RoleNodes>();
            }
        }

        /// <summary>
        /// 异步获取所有子角色
        /// </summary>
        public async Task<List<RoleNodes>> GetAllChildrenAsync(int roleId)
        {
            if (roleId <= 0)
            {
                return new List<RoleNodes>();
            }

            try
            {
                var allRoles = await GetAllRoleQueryable(roleId).ToListAsync();
                return GetAllChildrenNodes(roleId);
            }
            catch (Exception ex)
            {
                Log.Error( $"异步获取所有子角色失败: RoleId={roleId}, {ex.Message}", ex);
                return new List<RoleNodes>();
            }
        }

        /// <summary>
        /// 获取所有角色查询对象
        /// </summary>
        private IQueryable<RoleNodes> GetAllRoleQueryable(int roleId)
        {
            return repository
                   .FindAsIQueryable(x => x.Enable == 1 && x.Role_Id > 1)
                   .Select(s => new RoleNodes()
                   {
                       Id = s.Role_Id,
                       ParentId = s.ParentId,
                       RoleName = s.RoleName
                   });
        }

        /// <summary>
        /// 异步获取所有子角色ID
        /// </summary>
        public async Task<List<int>> GetAllChildrenRoleIdAsync(int roleId)
        {
            if (roleId <= 0)
            {
                return new List<int>();
            }

            try
            {
                var children = await GetAllChildrenAsync(roleId);
                return children.Select(x => x.Id).ToList();
            }
            catch (Exception ex)
            {
                Log.Error( $"异步获取所有子角色ID失败: RoleId={roleId}, {ex.Message}", ex);
                return new List<int>();
            }
        }

        /// <summary>
        /// 获取所有子角色ID
        /// </summary>
        public List<int> GetAllChildrenRoleId(int roleId)
        {
            if (roleId <= 0)
            {
                return new List<int>();
            }

            try
            {
                return GetAllChildren(roleId).Select(x => x.Id).ToList();
            }
            catch (Exception ex)
            {
                Log.Error( $"获取所有子角色ID失败: RoleId={roleId}, {ex.Message}", ex);
                return new List<int>();
            }
        }

        /// <summary>
        /// 递归获取所有子节点权限
        /// </summary>
        private List<RoleNodes> GetAllChildrenNodes(int roleId)
        {
            try
            {
                return RoleContext.GetAllChildren(roleId);
            }
            catch (Exception ex)
            {
                Log.Error( $"获取所有子节点失败: RoleId={roleId}, {ex.Message}", ex);
                return new List<RoleNodes>();
            }
        }

        /// <summary>
        /// 保存角色权限
        /// </summary>
        /// <param name="userPermissions"></param>
        /// <param name="roleId"></param>
        /// <returns></returns>
        public async Task<WebResponseContent> SavePermission(List<UserPermissions> userPermissions, int roleId)
        {
            // 参数验证
            if (userPermissions == null)
            {
                return _responseContent.Error("权限数据不能为空");
            }

            if (roleId <= 0)
            {
                return _responseContent.Error("角色ID无效");
            }

            string message = "";

            // 使用事务确保数据一致性
            using (var transaction = await repository.DbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    UserInfo user = UserContext.Current.UserInfo;

                    // 权限验证
                    var childrenRoles = await GetAllChildrenAsync(user.Role_Id);
                    if (!childrenRoles.Exists(x => x.Id == roleId) && user.Role_Id != roleId && !UserContext.Current.IsSuperAdmin)
                    {
                        return _responseContent.Error("没有权限修改此角色的权限信息");
                    }

                    //当前用户的权限
                    List<Permissions> permissions = UserContext.Current.Permissions ?? new List<Permissions>();

                    List<int> originalMeunIds = new List<int>();

                    //被分配角色的权限
                    List<Sys_RoleAuth> roleAuths = await repository.FindAsync<Sys_RoleAuth>(x => x.Role_Id == roleId);
                    if (roleAuths == null)
                    {
                        roleAuths = new List<Sys_RoleAuth>();
                    }

                    List<Sys_RoleAuth> updateAuths = new List<Sys_RoleAuth>();

                    foreach (UserPermissions x in userPermissions)
                    {
                        if (x == null) continue;

                        Permissions per = permissions.Where(p => p.Menu_Id == x.Id).FirstOrDefault();

                        //不能分配超过当前用户的权限
                        if (per == null && !UserContext.Current.IsSuperAdmin) continue;

                        //per.UserAuthArr.Contains(a.Value)校验权限范围
                        string[] arr = x.Actions == null || x.Actions.Count == 0
                          ? new string[0]
                          : x.Actions.Where(a => a != null &&
                                (UserContext.Current.IsSuperAdmin ||
                                (per != null && per.UserAuthArr != null && per.UserAuthArr.Contains(a.Value))))
                          .Select(s => s.Value).ToArray();

                        //如果当前权限没有分配过，设置Auth_Id默认为0，表示新增的权限
                        var auth = roleAuths.Where(r => r.Menu_Id == x.Id)
                            .Select(s => new { s.Auth_Id, s.AuthValue, s.Menu_Id })
                            .FirstOrDefault();

                        string newAuthValue = string.Join(",", arr);

                        //权限没有发生变化则不处理
                        if (auth == null || auth.AuthValue != newAuthValue)
                        {
                            updateAuths.Add(new Sys_RoleAuth()
                            {
                                Role_Id = roleId,
                                Menu_Id = x.Id,
                                AuthValue = newAuthValue,
                                Auth_Id = auth?.Auth_Id ?? 0,
                                ModifyDate = DateTime.Now,
                                Modifier = user.UserTrueName,
                                CreateDate = DateTime.Now,
                                Creator = user.UserTrueName
                            });
                        }
                        else
                        {
                            originalMeunIds.Add(auth.Menu_Id);
                        }
                    }

                    //更新权限
                    var updateItems = updateAuths.Where(x => x.Auth_Id > 0).ToList();
                    if (updateItems.Count > 0)
                    {
                        repository.UpdateRange(updateItems, x => new
                        {
                            x.Menu_Id,
                            x.AuthValue,
                            x.Modifier,
                            x.ModifyDate
                        });
                    }

                    //新增的权限
                    var addItems = updateAuths.Where(x => x.Auth_Id <= 0).ToList();
                    if (addItems.Count > 0)
                    {
                        repository.AddRange(addItems);
                    }

                    //获取权限取消的权限
                    int[] authIds = roleAuths.Where(x => userPermissions.Select(u => u.Id)
                     .ToList().Contains(x.Menu_Id) || originalMeunIds.Contains(x.Menu_Id))
                    .Select(s => s.Auth_Id)
                    .ToArray();

                    List<Sys_RoleAuth> delAuths = roleAuths
                        .Where(x => !string.IsNullOrEmpty(x.AuthValue) && !authIds.Contains(x.Auth_Id))
                        .ToList();

                    if (delAuths.Count > 0)
                    {
                        delAuths.ForEach(x =>
                        {
                            x.AuthValue = "";
                            x.ModifyDate = DateTime.Now;
                            x.Modifier = user.UserTrueName;
                        });

                        //将取消的权限设置为""
                        repository.UpdateRange(delAuths, x => new
                        {
                            x.Menu_Id,
                            x.AuthValue,
                            x.Modifier,
                            x.ModifyDate
                        });
                    }

                    int addCount = addItems.Count;
                    int updateCount = updateItems.Count;
                    await repository.SaveChangesAsync();

                    // 提交事务
                    await transaction.CommitAsync();

                    string _version = DateTime.Now.ToString("yyyyMMddHHMMssfff");
                    //标识缓存已更新
                    base.CacheContext.Add(roleId.GetRoleIdKey(), _version);

                    message = $"保存成功：新增加配菜单权限{addCount}条,更新菜单{updateCount}条,删除权限{delAuths.Count}条";
                    _responseContent.OK(message);
                }
                catch (Exception ex)
                {
                    // 回滚事务
                    await transaction.RollbackAsync();

                    message = $"保存权限失败: {ex.Message}";
                    Log.Error( message, ex);
                    return _responseContent.Error("保存权限失败，请稍后重试");
                }
                finally
                {
                    Logger.Info($"权限分配: RoleId={roleId}, {message}");
                }
            }

            return _responseContent;
        }

        /// <summary>
        /// 新增角色
        /// </summary>
        public override WebResponseContent Add(SaveModel saveDataModel)
        {
            if (saveDataModel == null)
            {
                return _responseContent.Error("参数不能为空");
            }

            AddOnExecuting = (Sys_Role role, object obj) =>
            {
                if (role == null)
                {
                    return _responseContent.Error("角色信息不能为空");
                }

                // 验证角色名
                if (string.IsNullOrWhiteSpace(role.RoleName))
                {
                    return _responseContent.Error("角色名不能为空");
                }

                // 权限验证
                if (!UserContext.Current.IsSuperAdmin && role.ParentId > 0)
                {
                    var childrenIds = RoleContext.GetAllChildrenIds(UserContext.Current.RoleId);
                    if (!childrenIds.Contains(role.ParentId))
                    {
                        return _responseContent.Error("不能添加此角色");
                    }
                }

                return ValidateRoleName(role, x => x.RoleName == role.RoleName);
            };

            return RemoveCache(base.Add(saveDataModel));
        }

        /// <summary>
        /// 删除角色
        /// </summary>
        public override WebResponseContent Del(object[] keys, bool delList = true)
        {
            if (keys == null || keys.Length == 0)
            {
                return _responseContent.Error("请选择要删除的角色");
            }

            try
            {
                // 权限验证
                if (!UserContext.Current.IsSuperAdmin)
                {
                    var roleIds = RoleContext.GetAllChildrenIds(UserContext.Current.RoleId);
                    var _keys = keys.Select(s => s.GetInt()).Where(x => x > 0).ToList();

                    if (_keys.Any(x => !roleIds.Contains(x)))
                    {
                        return _responseContent.Error("没有权限删除此角色");
                    }
                }

                // 检查是否有用户使用此角色
                var roleIdList = keys.Select(s => s.GetInt()).Where(x => x > 0).ToList();
                var hasUsers = repository.DbContext.Set<Sys_User>()
                    .Any(x =>roleIdList.Contains(x.Role_Id));

                if (hasUsers)
                {
                    return _responseContent.Error("存在用户使用此角色，不能删除");
                }

                return RemoveCache(base.Del(keys, delList));
            }
            catch (Exception ex)
            {
                Log.Error( $"删除角色失败: {ex.Message}", ex);
                return _responseContent.Error("删除角色失败");
            }
        }

        /// <summary>
        /// 验证角色名是否存在
        /// </summary>
        private WebResponseContent ValidateRoleName(Sys_Role role, Expression<Func<Sys_Role, bool>> predicate)
        {
            try
            {
                if (repository.Exists(predicate))
                {
                    return _responseContent.Error($"角色名【{role.RoleName}】已存在,请设置其他角色名");
                }
                return _responseContent.OK();
            }
            catch (Exception ex)
            {
                Log.Error( $"验证角色名失败: {ex.Message}", ex);
                return _responseContent.Error("验证角色名失败");
            }
        }

        /// <summary>
        /// 更新角色
        /// </summary>
        public override WebResponseContent Update(SaveModel saveModel)
        {
            if (saveModel == null)
            {
                return _responseContent.Error("参数不能为空");
            }

            UpdateOnExecuting = (Sys_Role role, object obj1, object obj2, List<object> obj3) =>
            {
                if (role == null)
                {
                    return _responseContent.Error("角色信息不能为空");
                }

                // 验证角色名
                if (string.IsNullOrWhiteSpace(role.RoleName))
                {
                    return _responseContent.Error("角色名不能为空");
                }

                //2020.05.07新增禁止选择上级角色为自己
                if (role.Role_Id == role.ParentId)
                {
                    return _responseContent.Error("上级角色不能选择自己");
                }

                if (role.Role_Id == UserContext.Current.RoleId)
                {
                    return _responseContent.Error("不能修改自己的角色");
                }

                // 检查循环依赖
                if (role.ParentId > 0)
                {
                    if (repository.Exists(x => x.Role_Id == role.ParentId && x.ParentId == role.Role_Id))
                    {
                        return _responseContent.Error("不能选择此上级角色，选择的上级角色与当前角色形成循环依赖关系");
                    }

                    // 检查是否选择了自己的子角色作为父级
                    var childrenIds = RoleContext.GetAllChildrenIds(role.Role_Id);
                    if (childrenIds.Contains(role.ParentId))
                    {
                        return _responseContent.Error("不能选择子角色作为上级角色");
                    }
                }

                // 权限验证
                if (!UserContext.Current.IsSuperAdmin)
                {
                    var roleIds = RoleContext.GetAllChildrenIds(UserContext.Current.RoleId);

                    if (role.ParentId > 0)
                    {
                        if (!roleIds.Contains(role.ParentId))
                        {
                            return _responseContent.Error("没有权限选择此上级角色");
                        }
                    }

                    if (!roleIds.Contains(role.Role_Id))
                    {
                        return _responseContent.Error("没有权限修改此角色");
                    }

                    return _responseContent.OK("");
                }

                return ValidateRoleName(role, x => x.RoleName == role.RoleName && x.Role_Id != role.Role_Id);
            };

            return RemoveCache(base.Update(saveModel));
        }

        /// <summary>
        /// 清除缓存
        /// </summary>
        private WebResponseContent RemoveCache(WebResponseContent webResponse)
        {
            if (webResponse != null && webResponse.Status)
            {
                try
                {
                    RoleContext.Refresh();
                }
                catch (Exception ex)
                {
                    Log.Error( $"刷新角色缓存失败: {ex.Message}", ex);
                }
            }
            return webResponse;
        }
    }

    /// <summary>
    /// 用户权限模型
    /// </summary>
    public class UserPermissions
    {
        public int Id { get; set; }
        public int Pid { get; set; }
        public string Text { get; set; }
        public bool IsApp { get; set; }
        public List<Sys_Actions> Actions { get; set; }
    }
}