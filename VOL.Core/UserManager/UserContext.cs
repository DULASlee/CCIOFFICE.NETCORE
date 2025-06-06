using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using VOL.Core.CacheManager;
using VOL.Core.DBManager;
using VOL.Core.Enums;
using VOL.Core.Extensions;
using VOL.Core.Extensions.AutofacManager;
using VOL.Core.UserManager;
using VOL.Entity;
using VOL.Entity.DomainModels;

namespace VOL.Core.ManageUser
{
    public class UserContext
    {
        /// <summary>
        /// 为了尽量减少redis或Memory读取,保证执行效率,将UserContext注入到DI，
        /// 每个UserContext的属性至多读取一次redis或Memory缓存从而提高查询效率
        /// </summary>
        public static UserContext Current
        {
            get
            {
                return Context.RequestServices.GetService(typeof(UserContext)) as UserContext;
            }
        }

        private static Microsoft.AspNetCore.Http.HttpContext Context
        {
            get
            {
                return Utilities.HttpContext.Current;
            }
        }
        private static ICacheService CacheService
        {
            get { return GetService<ICacheService>(); }
        }

        private static T GetService<T>() where T : class
        {
            return AutofacContainerModule.GetService<T>();
        }

        public UserInfo UserInfo
        {
            get
            {
                if (_userInfo != null)
                {
                    return _userInfo;
                }
                return GetUserInfo(UserId);
            }
        }

        private UserInfo _userInfo { get; set; }

        /// <summary>
        /// 角色ID为1的默认为超级管理员
        /// </summary>
        public bool IsSuperAdmin
        {
            get { return IsRoleIdSuperAdmin(this.RoleId); }
        }
        /// <summary>
        /// 角色ID为1的默认为超级管理员
        /// </summary>
        public static bool IsRoleIdSuperAdmin(int roleId)
        {
            return roleId == 1;
        }

        public UserInfo GetUserInfo(int userId)
        {
            if (_userInfo != null) return _userInfo;
            if (userId <= 0)
            {
                _userInfo = new UserInfo();
                return _userInfo;
            }
            string key = userId.GetUserIdKey();
            _userInfo = CacheService.Get<UserInfo>(key);
            if (_userInfo != null && _userInfo.User_Id > 0) return _userInfo;

            _userInfo = DBServerProvider.DbContext.Set<Sys_User>()
                .Where(x => x.User_Id == userId).Select(s => new
                {
                    User_Id = userId,
                    Role_Id = s.Role_Id.GetInt(),
                    RoleName = s.RoleName,
                    //2022.08.15增加部门id
                    DeptId = s.Dept_Id??0,
                    Token = s.Token,
                    UserName = s.UserName,
                    UserTrueName = s.UserTrueName,
                    Enable = s.Enable,
                    DeptIds= s.DeptIds
                }).ToList().Select(s => new UserInfo()
                {
                    User_Id = userId,
                    Role_Id = s.Role_Id,
                    Token = s.Token,
                    UserName = s.UserName,
                    UserTrueName = s.UserTrueName,
                    Enable = 1,
                    DeptIds = string.IsNullOrEmpty(s.DeptIds) ? new List<Guid>() : s.DeptIds.Split(",").Select(x => (Guid)x.GetGuid()).ToList(),
                }).FirstOrDefault();

            if (_userInfo != null && _userInfo.User_Id > 0)
            {
                CacheService.AddObject(key, _userInfo);
            }
            return _userInfo ?? new UserInfo();
        }

        // Static caches objKeyValue, rolePermissionsVersion, rolePermissions are removed.
        // Permissions are now cached directly in ICacheService (Redis or Memory).

        /// <summary>
        /// 获取用户所有的菜单权限。
        /// 权限数据会从 ICacheService 中获取，如果缓存未命中，则从数据库加载并存入缓存。
        /// (Gets all menu permissions for the user.
        /// Permission data is retrieved from ICacheService. If cache miss, it's loaded from DB and stored in cache.)
        /// </summary>
        public List<Permissions> Permissions
        {
            get
            {
                return GetPermissions(RoleId);
            }
        }

        /// <summary>
        /// 当菜单或其关联的操作权限发生变更时，刷新相关缓存。
        /// 目前简化为清除超级管理员的权限缓存，并标记其他角色缓存需要更精细的失效策略。
        /// (When a menu or its associated action permissions change, refresh relevant caches.
        /// Currently simplified to clear super admin's permission cache and mark other role caches as needing a more granular invalidation strategy.)
        /// </summary>
        /// <param name="menuId">发生变更的菜单ID。(The ID of the menu that changed.)</param>
        public void RefreshWithMenuActionChange(int menuId)
        {
            // Chinese Comment: 清除超级管理员的权限缓存。
            // (Clear the permission cache for the super administrator.)
            CacheService.Remove("UserPermissions_SuperAdmin");

            // TODO: 需要更精细的缓存失效策略来仅清除受影响角色的权限缓存。
            // 这可能需要查询数据库找出哪些角色拥有对此menuId的权限，或者维护一个menuId到roleId列表的反向索引。
            // 当前简单实现可能导致不必要的缓存重建。
            // (TODO: A more granular cache invalidation strategy is needed to clear only affected roles' permission caches.
            // This might involve querying the database to find which roles have permissions for this menuId,
            // or maintaining a reverse index of menuId to roleId list.
            // The current simple implementation might lead to unnecessary cache rebuilds.)
            Logger.Warning(LogEvent.SystemCache, $"菜单 (MenuId: {menuId}) 权限已变更。超级管理员缓存已清除。请考虑实现更精细的角色权限缓存失效机制。 (Permissions for menu (MenuId: {menuId}) have changed. SuperAdmin cache cleared. Consider implementing a more granular role permission cache invalidation mechanism.)");
        }

        /// <summary>
        /// 获取单个表的权限
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public Permissions GetPermissions(string tableName)
        {
            return GetPermissions(RoleId).Where(x => x.TableName == tableName).FirstOrDefault();
        }
        /// <summary>
        /// 2022.03.26
        /// 菜单类型1:移动端，0:PC端
        /// </summary>
        public static int MenuType
        {
            get
            {
                return Context.Request.Headers.ContainsKey("uapp") ? 1 : 0;
            }
        }
        /// <summary>
        /// 自定条件查询权限
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public Permissions GetPermissions(Func<Permissions, bool> func)
        {
            // 2022.03.26增移动端加菜单类型判断
            return GetPermissions(RoleId).Where(func).Where(x => x.MenuType == MenuType).FirstOrDefault();
        }

        private List<Permissions> ActionToArray(List<Permissions> permissions)
        {
            permissions.ForEach(x =>
            {
                try
                {
                    var menuAuthArr = x.MenuAuth.DeserializeObject<List<Sys_Actions>>();
                    x.UserAuthArr = string.IsNullOrEmpty(x.UserAuth)
                    ? new string[0]
                    : x.UserAuth.Split(",").Where(c => menuAuthArr.Any(m => m.Value == c)).ToArray();

                }
                catch { }
                finally
                {
                    if (x.UserAuthArr == null)
                    {
                        x.UserAuthArr = new string[0];
                    }
                }
            });
            return permissions;
        }
        private List<Permissions> MenuActionToArray(List<Permissions> permissions)
        {
            permissions.ForEach(x =>
            {
                try
                {
                    x.UserAuthArr = string.IsNullOrEmpty(x.UserAuth)
                    ? new string[0]
                    : x.UserAuth.DeserializeObject<List<Sys_Actions>>().Select(s => s.Value).ToArray();
                }
                catch { }
                finally
                {
                    if (x.UserAuthArr == null)
                    {
                        x.UserAuthArr = new string[0];
                    }
                }
            });
            return permissions;
        }
        public List<Permissions> GetPermissions(int roleId)
        {
            // Chinese Comment: 定义缓存键和缓存过期时间（例如1小时）。
            // (Define cache key and cache expiration time (e.g., 1 hour).)
            string cacheKey;
            int expireSeconds = 3600; // 1 hour

            if (IsRoleIdSuperAdmin(roleId))
            {
                cacheKey = "UserPermissions_SuperAdmin";
                // Chinese Comment: 尝试从缓存中获取超级管理员权限。
                // (Try to get super administrator permissions from cache.)
                List<Permissions>? cachedPermissions = CacheService.Get<List<Permissions>>(cacheKey);
                if (cachedPermissions != null)
                {
                    return cachedPermissions;
                }

                // Chinese Comment: 缓存未命中，从数据库查询超级管理员权限。
                // (Cache miss, query super administrator permissions from the database.)
                var permissions = DBServerProvider.DbContext.Set<Sys_Menu>()
                    .Where(x => x.Enable == 1 || x.Enable == 2) // 启用或隐藏的菜单
                    .Select(a => new Permissions
                    {
                        Menu_Id = a.Menu_Id,
                        ParentId = a.ParentId,
                        TableName = (a.TableName ?? "").ToLower(),
                        UserAuth = a.Auth, // 超级管理员拥有菜单定义的所有权限
                        MenuType = a.MenuType ?? 0
                    }).ToList();
                var processedPermissions = MenuActionToArray(permissions);

                // Chinese Comment: 将获取到的权限存入缓存。
                // (Store the retrieved permissions in the cache.)
                CacheService.AddObject(cacheKey, processedPermissions, expireSeconds: expireSeconds);
                return processedPermissions;
            }
            else // Regular roles
            {
                cacheKey = $"UserPermissions_{roleId}";
                // Chinese Comment: 尝试从缓存中获取普通角色权限。
                // (Try to get regular role permissions from cache.)
                List<Permissions>? cachedPermissions = CacheService.Get<List<Permissions>>(cacheKey);
                if (cachedPermissions != null)
                {
                    return cachedPermissions;
                }

                // Chinese Comment: 缓存未命中，从数据库查询普通角色权限。
                // (Cache miss, query regular role permissions from the database.)
                var dbContext = DBServerProvider.DbContext;
                List<Permissions> _permissions = (from a in dbContext.Set<Sys_Menu>()
                                                  join b in dbContext.Set<Sys_RoleAuth>()
                                                  on a.Menu_Id equals b.Menu_Id
                                                  where b.Role_Id == roleId
                                                  && b.AuthValue != "" //确保有实际权限值
                                                  orderby a.ParentId
                                                  select new Permissions
                                                  {
                                                      Menu_Id = a.Menu_Id,
                                                      ParentId = a.ParentId,
                                                      TableName = (a.TableName ?? "").ToLower(),
                                                      MenuAuth = a.Auth, // 菜单定义的所有可选权限
                                                      UserAuth = b.AuthValue ?? "", //角色实际拥有的权限
                                                      MenuType = a.MenuType ?? 0
                                                  }).ToList();
                var processedPermissions = ActionToArray(_permissions);

                // Chinese Comment: 将获取到的权限存入缓存。
                // (Store the retrieved permissions in the cache.)
                CacheService.AddObject(cacheKey, processedPermissions, expireSeconds: expireSeconds);
                return processedPermissions;
            }
        }

        /// <summary>
        /// 判断是否有权限
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="authName"></param>
        /// <param name="roleId"></param>
        /// <returns></returns>
        public bool ExistsPermissions(string tableName, string authName, int roleId = 0)
        {
            if (roleId <= 0) roleId = RoleId;
            tableName = tableName.ToLower();
            return GetPermissions(roleId).Any(x => x.TableName == tableName && x.UserAuthArr.Contains(authName));
        }

        /// <summary>
        /// 判断是否有权限
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="authName"></param>
        /// <param name="roleId"></param>
        /// <returns></returns>
        public bool ExistsPermissions(string tableName, ActionPermissionOptions actionPermission, int roleId = 0)
        {
            return ExistsPermissions(tableName, actionPermission.ToString(), roleId);
        }
        public int UserId
        {
            get
            {
                return (Context.User.FindFirstValue(JwtRegisteredClaimNames.Jti)
                    ?? Context.User.FindFirstValue(ClaimTypes.NameIdentifier)).GetInt();
            }
        }

        public string UserName
        {
            get { return UserInfo.UserName; }
        }

        public string UserTrueName
        {
            get { return UserInfo.UserTrueName; }
        }

        public string Token
        {
            get { return UserInfo.Token; }
        }

        public int RoleId
        {
            get { return UserInfo.Role_Id; }
        }
        public List<Guid> DeptIds
        {
            get { return UserInfo.DeptIds; }
        }
        /// <summary>
        /// 获取所有子部门
        /// </summary>
        /// <returns></returns>
        public List<Guid> GetAllChildrenDeptIds()
        {
            return DepartmentContext.GetAllChildrenIds(DeptIds);
        }

        public void LogOut(int userId)
        {
            CacheService.Remove(userId.GetUserIdKey());
        }
    }
}
