using Microsoft.EntityFrameworkCore;
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
        private WebResponseContent _responseContent = new WebResponseContent();
        public override PageGridData<Sys_Role> GetPageData(PageDataOptions pageData)
        {
            //角色Id=1默认为超级管理员角色，界面上不显示此角色
            QueryRelativeExpression = (IQueryable<Sys_Role> queryable) =>
            {
                if (UserContext.Current.IsSuperAdmin)
                {
                    return queryable;
                }
                List<int> roleIds = GetAllChildrenRoleIdAndSelf();
                return queryable.Where(x => roleIds.Contains(x.Role_Id));
            };
            return base.GetPageData(pageData);
        }
        /// <summary>
        /// 编辑权限时，获取当前用户的所有菜单权限
        /// </summary>
        /// <returns></returns>
        public async Task<WebResponseContent> GetCurrentUserTreePermission()
        {
            return await GetUserTreePermission(UserContext.Current.RoleId);
        }

        /// <summary>
        /// 编辑权限时，获取指定角色的所有菜单权限
        /// </summary>
        /// <param name="roleId"></param>
        /// <returns></returns>
        public async Task<WebResponseContent> GetUserTreePermission(int roleId)
        {
            if (!UserContext.IsRoleIdSuperAdmin(roleId) && UserContext.Current.RoleId != roleId)
            {
                if (!(await GetAllChildrenAsync(UserContext.Current.RoleId)).Exists(x => x.Id == roleId))
                {
                    return _responseContent.Error("没有权限获取此角色的权限信息");
                }
            }
            //获取用户权限
            List<Permissions> permissions = UserContext.Current.GetPermissions(roleId);
            List<Sys_Menu> menus;
            try
            {
                //权限用户权限查询所有的菜单信息
                menus = await Task.Run(() => Sys_MenuService.Instance.GetUserMenuList(roleId));
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LoggerType.Select, $"获取角色菜单列表失败 (GetUserTreePermission): RoleId={roleId}", new { roleId }, null, ex);
                return _responseContent.Error("获取角色菜单信息时出错。");
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

        private List<Sys_Actions> GetActions(int menuId, List<Sys_Actions> menuActions, List<Permissions> permissions, int roleId)
        {
            if (UserContext.IsRoleIdSuperAdmin(roleId))
            {
                return menuActions;
            }

            return menuActions.Where(p => permissions
                 .Exists(w => menuId == w.Menu_Id
                 && w.UserAuthArr.Contains(p.Value)))
                  .ToList();
        }

        private List<RoleNodes> rolesChildren = new List<RoleNodes>();

        /// <summary>
        /// 编辑权限时获取当前用户下的所有角色与当前用户的菜单权限
        /// </summary>
        /// <returns></returns>
        public async Task<WebResponseContent> GetCurrentTreePermission()
        {
            _responseContent = await GetCurrentUserTreePermission();
            int roleId = UserContext.Current.RoleId;
            return _responseContent.OK(null, new
            {
                tree = _responseContent.Data,
                roles = await GetAllChildrenAsync(roleId)
            });
        }

        private List<RoleNodes> roles = null;

        /// <summary>
        /// 获取当前角色下的所有角色包括自己的角色Id
        /// </summary>
        /// <returns></returns>
        public List<int> GetAllChildrenRoleIdAndSelf()
        {
            int roleId = UserContext.Current.RoleId;
            List<int> roleIds = GetAllChildren(roleId).Select(x => x.Id).ToList();
            roleIds.Add(roleId);
            return roleIds;
        }


        /// <summary>
        /// 获取当前角色下的所有角色
        /// </summary>
        /// <param name="roleId"></param>
        /// <returns></returns>
        public List<RoleNodes> GetAllChildren(int roleId)
        {
            try
            {
                roles = GetAllRoleQueryable(roleId).ToList();
                return GetAllChildrenNodes(roleId);
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LoggerType.Select, $"获取子角色列表失败: RoleId={roleId}", new { roleId }, null, ex);
                return new List<RoleNodes>(); // Return empty list on error
            }
        }

        public async Task<List<RoleNodes>> GetAllChildrenAsync(int roleId)
        {
            try
            {
                roles = await GetAllRoleQueryable(roleId).ToListAsync();
                // GetAllChildrenNodes is assumed to be CPU-bound or use the 'roles' populated above.
                // If GetAllChildrenNodes itself does I/O, it would need its own try-catch.
                return GetAllChildrenNodes(roleId);
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LoggerType.Select, $"获取子角色列表失败 (Async): RoleId={roleId}", new { roleId }, null, ex);
                return new List<RoleNodes>(); // Return empty list on error
            }
        }
        private IQueryable<RoleNodes> GetAllRoleQueryable(int roleId)
        {
            return repository
                   .FindAsIQueryable(
                   x => x.Enable == 1 && x.Role_Id > 1)
                   .Select(
                   s => new RoleNodes()
                   {
                       Id = s.Role_Id,
                       ParentId = s.ParentId,
                       RoleName = s.RoleName
                   });
        }

        public async Task<List<int>> GetAllChildrenRoleIdAsync(int roleId)
        {
            return (await GetAllChildrenAsync(roleId)).Select(x => x.Id).ToList();
        }


        public List<int> GetAllChildrenRoleId(int roleId)
        {
            return GetAllChildren(roleId).Select(x => x.Id).ToList();
        }

        private List<RoleNodes> GetAllChildrenNodes(int roleId)
        {
            return RoleContext.GetAllChildren(roleId);
        }
        /// <summary>
        /// 递归获取所有子节点权限
        /// </summary>
        /// <param name="roleId"></param>


        /// <summary>
        /// 保存角色权限
        /// </summary>
        /// <param name="userPermissions"></param>
        /// <param name="roleId"></param>
        /// <returns></returns>
        public async Task<WebResponseContent> SavePermission(List<UserPermissions> userPermissions, int roleId)
        {

            // Removed string message = "";
            try
            {
                UserInfo user = UserContext.Current.UserInfo;
                // This GetAllChildrenAsync is already wrapped, but if it returns empty due to error,
                // .Exists might behave unexpectedly. Assuming it returns empty list not null on error.
                if (!(await GetAllChildrenAsync(user.Role_Id)).Exists(x => x.Id == roleId))
                    return _responseContent.Error("没有权限修改此角色的权限信息");

                //当前用户的权限
                List<Permissions> permissions = UserContext.Current.Permissions;

                List<int> originalMeunIds = new List<int>();
                //被分配角色的权限
                List<Sys_RoleAuth> roleAuths = await repository.FindAsync<Sys_RoleAuth>(x => x.Role_Id == roleId); // DB Call
                List<Sys_RoleAuth> updateAuths = new List<Sys_RoleAuth>();
                foreach (UserPermissions x in userPermissions)
                {
                    Permissions per = permissions.Where(p => p.Menu_Id == x.Id).FirstOrDefault();
                    //不能分配超过当前用户的权限
                    if (per == null) continue;
                    //per.UserAuthArr.Contains(a.Value)校验权限范围
                    string[] arr = x.Actions == null || x.Actions.Count == 0
                      ? new string[0]
                      : x.Actions.Where(a => per.UserAuthArr.Contains(a.Value))
                      .Select(s => s.Value).ToArray();

                    //如果当前权限没有分配过，设置Auth_Id默认为0，表示新增的权限
                    var auth = roleAuths.Where(r => r.Menu_Id == x.Id).Select(s => new { s.Auth_Id, s.AuthValue, s.Menu_Id }).FirstOrDefault();
                    string newAuthValue = string.Join(",", arr);
                    //权限没有发生变化则不处理
                    if (auth == null || auth.AuthValue != newAuthValue)
                    {
                        updateAuths.Add(new Sys_RoleAuth()
                        {
                            Role_Id = roleId,
                            Menu_Id = x.Id,
                            AuthValue = string.Join(",", arr),
                            Auth_Id = auth == null ? 0 : auth.Auth_Id,
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
                repository.UpdateRange(updateAuths.Where(x => x.Auth_Id > 0), x => new // DB Call (marks for update)
                {
                    x.Menu_Id,
                    x.AuthValue,
                    x.Modifier,
                    x.ModifyDate
                });
                //新增的权限
                repository.AddRange(updateAuths.Where(x => x.Auth_Id <= 0)); // DB Call (marks for add)

                //获取权限取消的权限
                int[] authIds = roleAuths.Where(x => userPermissions.Select(u => u.Id)
                 .ToList().Contains(x.Menu_Id) || originalMeunIds.Contains(x.Menu_Id))
                .Select(s => s.Auth_Id)
                .ToArray();
                List<Sys_RoleAuth> delAuths = roleAuths.Where(x => x.AuthValue != "" && !authIds.Contains(x.Auth_Id)).ToList();
                delAuths.ForEach(x =>
                {
                    x.AuthValue = "";
                });
                //将取消的权限设置为""
                repository.UpdateRange(delAuths, x => new // DB Call (marks for update)
                {
                    x.Menu_Id,
                    x.AuthValue,
                    x.Modifier,
                    x.ModifyDate
                });

                int addCount = updateAuths.Where(x => x.Auth_Id <= 0).Count();
                int updateCount = updateAuths.Where(x => x.Auth_Id > 0).Count();
                await repository.SaveChangesAsync(); // DB Call (commits changes)

                string _version = DateTime.Now.ToString("yyyyMMddHHMMssfff");
                //标识缓存已更新
                base.CacheContext.Add(roleId.GetRoleIdKey(), _version); // Cache Call

                _responseContent.OK($"保存成功：新增加配菜单权限{addCount}条,更新菜单{updateCount}条,删除权限{delAuths.Count()}条");
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LoggerType.Exception, $"保存角色权限失败: RoleId={roleId}", new { roleId, permissions = userPermissions?.Serialize() }, null, ex);
                _responseContent.Error("保存角色权限时发生内部错误。"); // Return the object itself
                // No 'return' here, responseContent is modified and returned in finally or after block
            }
            // The finally block was problematic as 'message' would not be set if no exception.
            // Logging the outcome (success or specific failure) is better.
            if (_responseContent.Status)
            {
                VOL.Core.Services.Logger.Info(VOL.Core.Enums.LoggerType.Save, $"权限配置成功: RoleId={roleId}. {_responseContent.Message}", userPermissions?.Serialize(), _responseContent.Serialize());
            }
            else
            {
                // Error already logged by catch block if it was an exception.
                // This logs business logic errors or if _responseContent was directly set to Error.
                 if (!_responseContent.Message.Contains("内部错误")) // Avoid duplicate logging for exceptions
                     VOL.Core.Services.Logger.Warning(VOL.Core.Enums.LoggerType.Save, $"权限配置失败: RoleId={roleId}. Message: {_responseContent.Message}", userPermissions?.Serialize(), _responseContent.Serialize());
            }
            return _responseContent;
        }


        public override WebResponseContent Add(SaveModel saveDataModel)
        {
            AddOnExecuting = (Sys_Role role, object obj) =>
            {
                if (!UserContext.Current.IsSuperAdmin && role.ParentId > 0 && !RoleContext.GetAllChildrenIds(UserContext.Current.RoleId).Contains(role.ParentId))
                {
                    return new WebResponseContent().Error("不能添加此角色"); // Use new instance
                }
                var validationResult = ValidateRoleName(role, x => x.RoleName == role.RoleName);
                if (!validationResult.Status)
                {
                    return validationResult; // Return the error response from ValidateRoleName
                }
                return new WebResponseContent().OK(); // Explicitly return OK if validation passed
            };
            return RemoveCache(base.Add(saveDataModel));
        }

        public override WebResponseContent Del(object[] keys, bool delList = true)
        {
            if (!UserContext.Current.IsSuperAdmin)
            {
                var roleIds = RoleContext.GetAllChildrenIds(UserContext.Current.RoleId);
                var _keys = keys.Select(s => s.GetInt());
                if (_keys.Any(x => !roleIds.Contains(x)))
                {
                    return _responseContent.Error("没有权限删除此角色");
                }
            }
        
            return RemoveCache(base.Del(keys, delList));
        }

        private WebResponseContent ValidateRoleName(Sys_Role role, Expression<Func<Sys_Role, bool>> predicate)
        {
            try
            {
                if (repository.Exists(predicate))
                {
                    return new WebResponseContent().Error($"角色名【{role.RoleName}】已存在,请设置其他角色名");
                }
                return new WebResponseContent().OK();
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LoggerType.Select, $"校验角色名失败: RoleName={role?.RoleName}", role?.Serialize(), null, ex);
                return new WebResponseContent().Error("校验角色名时发生数据库错误。");
            }
        }

        public override WebResponseContent Update(SaveModel saveModel)
        {
            UpdateOnExecuting = (Sys_Role role, object obj1, object obj2, List<object> obj3) =>
            {
                //2020.05.07新增禁止选择上级角色为自己
                if (role.Role_Id == role.ParentId)
                {
                    return new WebResponseContent().Error($"上级角色不能选择自己");
                }
                if (role.Role_Id == UserContext.Current.RoleId)
                {
                    return new WebResponseContent().Error($"不能修改自己的角色");
                }
                try
                {
                    if (repository.Exists(x => x.Role_Id == role.ParentId && x.ParentId == role.Role_Id))
                    {
                        return new WebResponseContent().Error($"不能选择此上级角色，选择的上级角色与当前角色形成依赖关系");
                    }
                }
                catch (Exception ex)
                {
                    VOL.Core.Services.Logger.Error(VOL.Core.Enums.LoggerType.Update, $"校验角色依赖关系失败: RoleId={role.Role_Id}", role.Serialize(), null, ex);
                    return new WebResponseContent().Error("校验角色依赖关系时发生数据库错误。");
                }

                if (!UserContext.Current.IsSuperAdmin)
                {
                    // This section seems to be authorization logic, keep it as is for now.
                    // Ensure RoleContext.GetAllChildrenIds doesn't throw unhandled exceptions. Assume it's safe or handled internally.
                    var roleIds = RoleContext.GetAllChildrenIds(UserContext.Current.RoleId);
                    if (role.ParentId > 0)
                    {
                        if (!roleIds.Contains(role.ParentId))
                        {
                            return new WebResponseContent().Error($"不能选择此角色");
                        }
                    }
                    if (!roleIds.Contains(role.Role_Id))
                    {
                        return new WebResponseContent().Error($"不能选择此角色");
                    }
                    // If the non-SuperAdmin checks pass, it still needs to validate role name
                }

                var validationResult = ValidateRoleName(role, x => x.RoleName == role.RoleName && x.Role_Id != role.Role_Id);
                if (!validationResult.Status)
                {
                    return validationResult; // Propagate error from ValidateRoleName
                }
                return new WebResponseContent().OK(); // Explicitly OK if all checks passed
            };
            return RemoveCache(base.Update(saveModel));
        }
        private WebResponseContent RemoveCache(WebResponseContent webResponse)
        {
            if (webResponse.Status)
            {
                try
                {
                    RoleContext.Refresh();
                }
                catch (Exception ex)
                {
                    VOL.Core.Services.Logger.Error(VOL.Core.Enums.LoggerType.Exception, "刷新角色缓存失败 (RoleContext.Refresh)", null, webResponse.Serialize(), ex);
                    // The main operation (Add/Update/Del) was successful, but cache refresh failed.
                    // This is usually not critical enough to change webResponse.Status to false,
                    // but it depends on application requirements. For now, just log.
                }
            }
            return webResponse;
        }
    }


    public class UserPermissions
    {
        public int Id { get; set; }
        public int Pid { get; set; }
        public string Text { get; set; }
        public bool IsApp { get; set; }
        public List<Sys_Actions> Actions { get; set; }
    }
}
