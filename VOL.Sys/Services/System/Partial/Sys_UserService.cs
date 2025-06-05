using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VOL.Core.Configuration;
using VOL.Core.Enums;
using VOL.Core.Extensions;
using VOL.Core.ManageUser;
using VOL.Core.Services;
using VOL.Core.Utilities;
using VOL.Entity.DomainModels;
using VOL.Sys.IRepositories;

namespace VOL.Sys.Services
{
    public partial class Sys_UserService
    {
        private Microsoft.AspNetCore.Http.HttpContext _context;
        private ISys_UserRepository _repository;
        [ActivatorUtilitiesConstructor]
        public Sys_UserService(IHttpContextAccessor httpContextAccessor, ISys_UserRepository repository)
            : base(repository)
        {
            _context = httpContextAccessor.HttpContext;
            _repository = repository;
        }
        WebResponseContent webResponse = new WebResponseContent();
        /// <summary>
        /// WebApi登陆
        /// </summary>
        /// <param name="loginInfo"></param>
        /// <param name="verificationCode"></param>
        /// <returns></returns>
        public async Task<WebResponseContent> Login(LoginInfo loginInfo, bool verificationCode = true)
        {
            string msg = string.Empty;
            //   2020.06.12增加验证码
            IMemoryCache memoryCache = _context.GetService<IMemoryCache>();
            string cacheCode = (memoryCache.Get(loginInfo.UUID) ?? "").ToString();
            if (string.IsNullOrEmpty(cacheCode))
            {
                return webResponse.Error("验证码已失效");
            }
            if (cacheCode.ToLower() != loginInfo.VerificationCode.ToLower())
            {
                memoryCache.Remove(loginInfo.UUID);
                return webResponse.Error("验证码不正确");
            }
            try
            {
                Sys_User user = await repository.FindAsIQueryable(x => x.UserName == loginInfo.UserName)
                    .FirstOrDefaultAsync();

                if (user == null || loginInfo.Password.Trim().EncryptDES(AppSetting.Secret.User) != (user.UserPwd ?? ""))
                {
                    webResponse.Error(ResponseType.LoginError);
                    // Log failed login attempt before returning
                    Logger.Warning(LoggerType.Login, $"登录失败: 用户名或密码错误. UserName={loginInfo.UserName}", loginInfo.Serialize(), webResponse.Message);
                    memoryCache.Remove(loginInfo.UUID); // Ensure cache is removed on this path too
                    return webResponse;
                }

                string token = JwtHelper.IssueJwt(new UserInfo()
                {
                    User_Id = user.User_Id,
                    UserName = user.UserName,
                    Role_Id = user.Role_Id
                });
                user.Token = token;
                webResponse.Data = new { token, userName = user.UserTrueName, img = user.HeadImageUrl };
                // Assuming SaveChanges is called by the framework or a higher level method if this is part of a larger UoW
                repository.Update(user, x => new { x.Token }, true);
                await repository.SaveChangesAsync(); // Explicitly save changes for the token update

                UserContext.Current.LogOut(user.User_Id);
                loginInfo.Password = string.Empty; // Clear password from input object
                webResponse.OK(ResponseType.LoginSuccess);
                Logger.Info(LoggerType.Login, $"登录成功: UserName={loginInfo.UserName}, UserId={user.User_Id}", loginInfo.Serialize(), webResponse.Message);
            }
            catch (Exception ex)
            {
                // Log the full exception
                Logger.Error(LoggerType.Login, $"登录异常: UserName={loginInfo.UserName}", loginInfo.Serialize(), null, ex);
                if (_context.GetService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>().IsDevelopment())
                {
                    // In dev, rethrow to see the detailed error
                    throw;
                }
                // For production, return a generic error
                webResponse.Error(ResponseType.ServerError);
            }
            finally
            {
                memoryCache.Remove(loginInfo.UUID);
                // Logging in finally might be redundant if specific outcomes are logged within try/catch.
                // However, if we want a guaranteed log entry for every attempt, this is one way.
                // For now, specific outcome logging is preferred.
            }
        }

        /// <summary>
        ///当token将要过期时，提前置换一个新的token
        /// </summary>
        /// <returns></returns>
        public async Task<WebResponseContent> ReplaceToken()
        {
            UserInfo userInfo = null; // Keep userInfo declaration for logging context
            try
            {
                string requestToken = _context.Request.Headers[AppSetting.TokenHeaderName];
                requestToken = requestToken?.Replace("Bearer ", "");

                // It's better to get CurrentUser before async calls if its context can change
                var currentUserContext = UserContext.Current;
                if (currentUserContext.Token != requestToken)
                {
                    webResponse.Error("Token已失效!");
                    Logger.Warning(LoggerType.ReplaceToeken, $"Token替换失败: Request token does not match current user token. UserId={currentUserContext.UserId}", null, webResponse.Message);
                    return webResponse;
                }

                if (JwtHelper.IsExp(requestToken))
                {
                    webResponse.Error("Token已过期!");
                    Logger.Warning(LoggerType.ReplaceToeken, $"Token替换失败: Token已过期. UserId={currentUserContext.UserId}", null, webResponse.Message);
                    return webResponse;
                }

                int userId = currentUserContext.UserId;
                userInfo = await repository.FindFirstAsync(x => x.User_Id == userId,
                     s => new UserInfo()
                     {
                         User_Id = userId,
                         UserName = s.UserName,
                         UserTrueName = s.UserTrueName,
                         Role_Id = s.Role_Id,
                         RoleName = s.RoleName
                     });

                if (userInfo == null)
                {
                    webResponse.Error("未查到用户信息!");
                    Logger.Warning(LoggerType.ReplaceToeken, $"Token替换失败: 未查到用户信息. UserId={userId}", null, webResponse.Message);
                    return webResponse;
                }

                string newToken = JwtHelper.IssueJwt(userInfo);
                base.CacheContext.Remove(userId.GetUserIdKey());
                repository.Update(new Sys_User() { User_Id = userId, Token = newToken }, x => new { x.Token }, true);
                await repository.SaveChangesAsync(); // Explicitly save changes

                webResponse.OK(null, newToken);
                Logger.Info(LoggerType.ReplaceToeken, $"Token替换成功: UserId={userId}, UserTrueName={userInfo.UserTrueName}", null, webResponse.Message);
            }
            catch (Exception ex)
            {
                // Use UserContext.Current if userInfo is null, otherwise use userInfo for more specific context
                var logUserId = userInfo?.User_Id ?? UserContext.Current?.UserId;
                var logUserTrueName = userInfo?.UserTrueName ?? UserContext.Current?.UserTrueName;
                Logger.Error(LoggerType.ReplaceToeken, $"Token替换异常: UserId={logUserId}, UserTrueName={logUserTrueName}", null, null, ex);
                webResponse.Error("Token替换服务异常。");
            }
            // Finally block is removed as specific logging is done in try/catch.
            return webResponse;
        }

        /// <summary>
        /// 修改密码
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public async Task<WebResponseContent> ModifyPwd(string oldPwd, string newPwd)
        {
            oldPwd = oldPwd?.Trim();
            newPwd = newPwd?.Trim();
            // string message = ""; // Not needed anymore
            int userId = UserContext.Current.UserId; // Get userId early for logging in case of error
            try
            {
                if (string.IsNullOrEmpty(oldPwd))
                {
                    webResponse.Error("旧密码不能为空");
                    Logger.Warning(LoggerType.ApiModifyPwd, $"修改密码失败: 旧密码为空. UserId={userId}", new { userId }, webResponse.Message);
                    return webResponse;
                }
                if (string.IsNullOrEmpty(newPwd))
                {
                    webResponse.Error("新密码不能为空");
                    Logger.Warning(LoggerType.ApiModifyPwd, $"修改密码失败: 新密码为空. UserId={userId}", new { userId }, webResponse.Message);
                    return webResponse;
                }
                if (newPwd.Length < 6)
                {
                    webResponse.Error("密码不能少于6位");
                    Logger.Warning(LoggerType.ApiModifyPwd, $"修改密码失败: 新密码长度小于6位. UserId={userId}", new { userId }, webResponse.Message);
                    return webResponse;
                }

                string userCurrentPwd = await base.repository.FindFirstAsync(x => x.User_Id == userId, s => s.UserPwd);

                string _oldPwd = oldPwd.EncryptDES(AppSetting.Secret.User);
                if (_oldPwd != userCurrentPwd)
                {
                    webResponse.Error("旧密码不正确");
                    Logger.Warning(LoggerType.ApiModifyPwd, $"修改密码失败: 旧密码不正确. UserId={userId}", new { userId }, webResponse.Message);
                    return webResponse;
                }

                string _newPwd = newPwd.EncryptDES(AppSetting.Secret.User);
                if (userCurrentPwd == _newPwd)
                {
                    webResponse.Error("新密码不能与旧密码相同");
                    Logger.Warning(LoggerType.ApiModifyPwd, $"修改密码失败: 新密码与旧密码相同. UserId={userId}", new { userId }, webResponse.Message);
                    return webResponse;
                }

                repository.Update(new Sys_User
                {
                    User_Id = userId,
                    UserPwd = _newPwd,
                    LastModifyPwdDate = DateTime.Now
                }, x => new { x.UserPwd, x.LastModifyPwdDate }, true);
                await repository.SaveChangesAsync(); // Explicitly save changes

                webResponse.OK("密码修改成功");
                Logger.Info(LoggerType.ApiModifyPwd, $"密码修改成功: UserId={userId}", new { userId }, webResponse.Message);
            }
            catch (Exception ex)
            {
                Logger.Error(LoggerType.ApiModifyPwd, $"修改密码异常: UserId={userId}", new { userId }, null, ex);
                webResponse.Error("密码修改服务异常，请稍后再试。");
            }
            // Finally block is removed as specific logging is done in try/catch for success/failure/exception.
            return webResponse;
        }
        /// <summary>
        /// 个人中心获取当前用户信息
        /// </summary>
        /// <returns></returns>
        public async Task<WebResponseContent> GetCurrentUserInfo()
        {
            try
            {
                var userId = UserContext.Current.UserId; // For logging
                var data = await base.repository
                    .FindAsIQueryable(x => x.User_Id == userId)
                    .Select(s => new
                    {
                        s.UserName,
                        s.UserTrueName,
                        s.Address,
                        s.PhoneNo,
                        s.Email,
                        s.Remark,
                        s.Gender,
                        s.RoleName,
                        s.HeadImageUrl,
                        s.CreateDate
                    })
                    .FirstOrDefaultAsync();

                if (data == null)
                {
                    Logger.Warning(LoggerType.Select, $"获取当前用户信息失败: 用户未找到. UserId={userId}", new { userId }, null);
                    return webResponse.Error("当前用户信息未找到。");
                }
                Logger.Info(LoggerType.Select, $"获取当前用户信息成功: UserId={userId}", new { userId }, null);
                return webResponse.OK(null, data);
            }
            catch (Exception ex)
            {
                var userIdForLog = UserContext.Current?.UserId; // Might be null if context is lost
                Logger.Error(LoggerType.Select, $"获取当前用户信息异常: UserId={userIdForLog}", new { UserId = userIdForLog }, null, ex);
                return webResponse.Error("获取用户信息时发生服务异常。");
            }
        }

        /// <summary>
        /// 设置固定排序方式及显示用户过滤
        /// </summary>
        /// <param name="pageData"></param>
        /// <returns></returns>
        public override PageGridData<Sys_User> GetPageData(PageDataOptions pageData)
        {
            int roleId = -1;
            //树形菜单传查询角色下所有用户
            if (pageData.Value != null)
            {
                roleId = pageData.Value.ToString().GetInt();
            }

            IQueryable<Sys_UserDepartment> deptQuery = null;
            QueryRelativeList = (List<SearchParameters> parameters) =>
            {
                foreach (var item in parameters)
                {
                    if (!string.IsNullOrEmpty(item.Value) && item.Name == "DeptIds")
                    {

                        var deptIds = item.Value.Split(",").Select(s => s.GetGuid()).Where(x => x != null);
                        item.Value = null;
                        deptQuery = repository.DbContext.Set<Sys_UserDepartment>().Where(x => x.Enable == 1 && deptIds.Contains(x.DepartmentId));
                    }
                }
            };

            QueryRelativeExpression = (IQueryable<Sys_User> queryable) =>
             {

                 if (deptQuery != null)
                 {
                     queryable = queryable.Where(c => deptQuery.Any(x => x.UserId == c.User_Id));
                 }

                 if (roleId <= 0)
                 {
                     if (UserContext.Current.IsSuperAdmin) return queryable;
                     roleId = UserContext.Current.RoleId;
                 }

                 //查看用户时，只能看下自己角色下的所有用户
                 List<int> roleIds = Sys_RoleService
                     .Instance
                     .GetAllChildrenRoleId(roleId);
                 roleIds.Add(roleId);
                 //判断查询的角色是否越权
                 if (roleId != UserContext.Current.RoleId && !roleIds.Contains(roleId))
                 {
                     roleId = -999;
                 }
                 return queryable.Where(x => roleIds.Contains(x.Role_Id));
             };
            var gridData = base.GetPageData(pageData);

            gridData.rows.ForEach(x =>
            {
                x.Token = null;
            });
            return gridData;
        }

        /// <summary>
        /// 新建用户，根据实际情况自行处理
        /// </summary>
        /// <param name="saveModel"></param>
        /// <returns></returns>
        public override WebResponseContent Add(SaveModel saveModel)
        {
            saveModel.MainData["RoleName"] = "无";
            base.AddOnExecute = (SaveModel userModel) =>
            {
                int roleId = userModel?.MainData?["Role_Id"].GetInt() ?? 0;
                if (roleId > 0 && !UserContext.Current.IsSuperAdmin)
                {
                    string roleName = GetChildrenName(roleId);
                    if ((roleId == 1) || string.IsNullOrEmpty(roleName))
                        return webResponse.Error("不能选择此角色");
                }
                return webResponse.OK();
            };


            ///生成6位数随机密码
            string pwd = 6.GenerateRandomNumber();
            //在AddOnExecuting之前已经对提交的数据做过验证是否为空
            base.AddOnExecuting = (Sys_User user, object obj) =>
            {
                try
                {
                    user.UserName = user.UserName.Trim();
                    if (repository.Exists(x => x.UserName == user.UserName))
                        return new WebResponseContent().Error("用户名已经被注册"); // Use new instance
                    user.UserPwd = pwd.EncryptDES(AppSetting.Secret.User);
                    //设置默认头像
                    // user.HeadImageUrl = string.IsNullOrEmpty(user.HeadImageUrl) ? AppSetting.DownLoad.DefaultHeadImage : user.HeadImageUrl; // Example for default image
                    return new WebResponseContent().OK(); // Use new instance
                }
                catch (Exception ex)
                {
                    Logger.Error(LoggerType.Add, $"新建用户校验用户名存在性异常: UserName={user?.UserName}", user?.Serialize(), null, ex);
                    return new WebResponseContent().Error("校验用户名时发生数据库错误。");
                }
            };

            base.AddOnExecuted = (Sys_User user, object list) =>
            {
                try
                {
                    var deptIds = user.DeptIds?.Split(",").Select(s => s.GetGuid()).Where(x => x != null).Select(s => (Guid)s).ToArray();
                    SaveDepartment(deptIds, user.User_Id); // SaveDepartment has its own try-catch
                    // If SaveDepartment could throw and we need to react, the catch here would be useful.
                    // For now, it mainly ensures any unexpected error in this delegate is caught.
                    return new WebResponseContent().OK($"用户新建成功.帐号{user.UserName}密码{pwd}");
                }
                catch (Exception ex)
                {
                    Logger.Error(LoggerType.Add, $"新建用户后保存部门信息异常: UserId={user?.User_Id}, UserName={user?.UserName}", user?.Serialize(), null, ex);
                    // Return OK since main user was added, but log the department save issue.
                    // Or, if this is critical, return an error:
                    // return new WebResponseContent().Error($"用户新建成功，但保存部门信息失败。帐号{user.UserName}密码{pwd}");
                    return new WebResponseContent().OK($"用户新建成功，但保存部门信息时遇到问题。帐号{user.UserName}密码{pwd}");
                }
            };
            return base.Add(saveModel);
        }

        /// <summary>
        /// 删除用户拦截过滤
        /// 用户被删除后同时清空对应缓存
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="delList"></param>
        /// <returns></returns>
        public override WebResponseContent Del(object[] keys, bool delList = false)
        {
            base.DelOnExecuting = (object[] ids) =>
            {
                try
                {
                    if (!UserContext.Current.IsSuperAdmin)
                    {
                        int[] userIds = ids.Select(x => Convert.ToInt32(x)).ToArray();
                        //校验只能删除当前角色下能看到的用户
                        //var xxx = repository.Find(x => userIds.Contains(x.User_Id)); // This line seems unused, can be removed.
                        var delUserInfos = repository.Find(x => userIds.Contains(x.User_Id), s => new { s.User_Id, s.Role_Id, s.UserTrueName });

                        // Assuming Sys_RoleService.Instance.GetAllChildrenRoleId is handled for exceptions
                        List<int> roleIds = Sys_RoleService.Instance.GetAllChildrenRoleId(UserContext.Current.RoleId);

                        string[] userNames = delUserInfos.Where(x => !roleIds.Contains(x.Role_Id))
                         .Select(s => s.UserTrueName)
                         .ToArray();
                        if (userNames.Any()) // Use .Any() for checking existence
                        {
                            return new WebResponseContent().Error($"没有权限删除用户：{string.Join(',', userNames)}");
                        }
                    }
                    return new WebResponseContent().OK();
                }
                catch (Exception ex)
                {
                    Logger.Error(LoggerType.Delete, "删除用户前置校验异常", new { UserIds = ids?.Serialize() }, null, ex);
                    return new WebResponseContent().Error("删除用户校验时发生数据库错误。");
                }
            };
            base.DelOnExecuted = (object[] userIds) =>
            {
                try
                {
                    var objKeys = userIds.Select(x => x.GetInt().GetUserIdKey());
                    base.CacheContext.RemoveAll(objKeys); // Cache operation
                    return new WebResponseContent().OK("用户缓存已清除。"); // Explicit OK
                }
                catch (Exception ex)
                {
                    Logger.Error(LoggerType.Delete, "删除用户后清除缓存异常", new { UserIds = userIds?.Serialize() }, null, ex);
                    // Main deletion was successful, so still return OK, but log the cache error.
                    return new WebResponseContent().OK("用户已删除，但清除缓存时遇到问题。");
                }
            };
            return base.Del(keys, delList);
        }

        private string GetChildrenName(int roleId)
        {
            //只能修改当前角色能看到的用户
            string roleName = Sys_RoleService
                .Instance
                .GetAllChildren(UserContext.Current.UserInfo.Role_Id).Where(x => x.Id == roleId)
                .Select(s => s.RoleName).FirstOrDefault();
            return roleName;
        }

        /// <summary>
        /// 修改用户拦截过滤
        /// 
        /// </summary>
        /// <param name="saveModel"></param>
        /// <returns></returns>
        public override WebResponseContent Update(SaveModel saveModel)
        {
            UserInfo userInfo = UserContext.Current.UserInfo;
            saveModel.MainData["RoleName"] = "无";
            //禁止修改用户名
            base.UpdateOnExecute = (SaveModel saveInfo) =>
            {
                if (!UserContext.Current.IsSuperAdmin)
                {
                    int roleId = saveModel.MainData["Role_Id"].GetInt();
                    string roleName = GetChildrenName(roleId);
                    saveInfo.MainData.TryAdd("RoleName", roleName);
                    if (UserContext.IsRoleIdSuperAdmin(userInfo.Role_Id))
                    {
                        return webResponse.OK();
                    }
                    if (string.IsNullOrEmpty(roleName)) return webResponse.Error("不能选择此角色");
                }

                return webResponse.OK();
            };
            base.UpdateOnExecuting = (Sys_User user, object obj1, object obj2, List<object> list) =>
            {
                try
                {
                    if (user.User_Id == userInfo.User_Id && user.Role_Id != userInfo.Role_Id)
                        return new WebResponseContent().Error("不能修改自己的角色");

                    var _user = repository.Find(x => x.User_Id == user.User_Id,
                        s => new { s.UserName, s.UserPwd })
                        .FirstOrDefault();

                    if (_user == null)
                        return new WebResponseContent().Error("未找到要更新的用户信息。");

                    user.UserName = _user.UserName; // Preserve original UserName
                    user.UserPwd = _user.UserPwd;   // Preserve original Password
                    return new WebResponseContent().OK();
                }
                catch (Exception ex)
                {
                    Logger.Error(LoggerType.Update, $"更新用户前置校验异常: UserId={user?.User_Id}", user?.Serialize(), null, ex);
                    return new WebResponseContent().Error("更新用户校验时发生数据库错误。");
                }
            };
            //用户信息被修改后，将用户的缓存信息清除
            base.UpdateOnExecuted = (Sys_User user, object obj1, object obj2, List<object> List) =>
            {
                try
                {
                    base.CacheContext.Remove(user.User_Id.GetUserIdKey()); // Cache operation
                    var deptIds = user.DeptIds?.Split(",").Select(s => s.GetGuid()).Where(x => x != null).Select(s => (Guid)s).ToArray();
                    SaveDepartment(deptIds, user.User_Id); // SaveDepartment has its own try-catch
                    return new WebResponseContent(true).OK("用户信息已更新，缓存已清除，部门已保存。");
                }
                catch (Exception ex)
                {
                    Logger.Error(LoggerType.Update, $"更新用户后操作异常 (缓存或部门): UserId={user?.User_Id}", user?.Serialize(), null, ex);
                    // Main update was successful. Log this error but return success for the main operation.
                    return new WebResponseContent(true).OK("用户信息已更新，但后续操作(清除缓存或保存部门)时遇到问题。");
                }
            };
            return base.Update(saveModel);
        }


        /// <summary>
        /// 保存部门
        /// </summary>
        /// <param name="deptIds"></param>
        /// <param name="userId"></param>
        public void SaveDepartment(Guid[] deptIds, int userId)
        {
            if (userId <= 0)
            {
                Logger.Warning(LoggerType.Update, $"保存用户部门失败: 无效的UserId. UserId={userId}", new { userId, deptIds = deptIds?.Serialize() }, null);
                return;
            }
            if (deptIds == null)
            {
                deptIds = new Guid[] { };
            }

            try
            {
                //如果需要判断当前角色是否越权，再调用一下获取当前部门下的所有子角色判断即可
                var existingUserDepts = repository.DbContext.Set<Sys_UserDepartment>().Where(x => x.UserId == userId)
                  .Select(s => new { s.DepartmentId, s.Enable, s.Id })
                  .ToList();

                //没有设置部门且原来也没有启用任何部门
                if (deptIds.Length == 0 && !existingUserDepts.Exists(x => x.Enable == 1))
                {
                    return; // No changes needed
                }

                UserInfo currentUser = UserContext.Current.UserInfo;
                //新设置的部门
                var deptsToAdd = deptIds.Where(x => !existingUserDepts.Exists(r => r.DepartmentId == x)).Select(s => new Sys_UserDepartment()
                {
                    DepartmentId = s,
                    UserId = userId,
                    Enable = 1,
                    CreateDate = DateTime.Now,
                    Creator = currentUser.UserTrueName,
                    CreateID = currentUser.User_Id
                }).ToList();

                List<Sys_UserDepartment> deptsToUpdate = new List<Sys_UserDepartment>();
                //停用不再选择的部门
                deptsToUpdate.AddRange(existingUserDepts.Where(x => !deptIds.Contains(x.DepartmentId) && x.Enable == 1).Select(s => new Sys_UserDepartment()
                {
                    Id = s.Id, // Important: Need Id for update
                    UserId = userId, DepartmentId = s.DepartmentId, // Keep other fields for potential full entity update needs
                    Enable = 0,
                    ModifyDate = DateTime.Now,
                    Modifier = currentUser.UserTrueName,
                    ModifyID = currentUser.User_Id
                }));

                //重新启用的部门
                deptsToUpdate.AddRange(existingUserDepts.Where(x => deptIds.Contains(x.DepartmentId) && x.Enable != 1).Select(s => new Sys_UserDepartment()
                {
                    Id = s.Id, // Important: Need Id for update
                    UserId = userId, DepartmentId = s.DepartmentId,
                    Enable = 1,
                    ModifyDate = DateTime.Now,
                    Modifier = currentUser.UserTrueName,
                    ModifyID = currentUser.User_Id
                }));

                if (deptsToAdd.Any())
                    repository.AddRange(deptsToAdd);

                if (deptsToUpdate.Any())
                    repository.UpdateRange(deptsToUpdate, x => new { x.Enable, x.ModifyDate, x.Modifier, x.ModifyID });

                // Only save if there are actual changes
                if (deptsToAdd.Any() || deptsToUpdate.Any())
                {
                    repository.SaveChanges();
                    Logger.Info(LoggerType.Update, $"保存用户部门成功: UserId={userId}", new { userId, deptIds = deptIds.Serialize(), added = deptsToAdd.Count, updated = deptsToUpdate.Count }, null);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LoggerType.Update, $"保存用户部门异常: UserId={userId}", new { userId, deptIds = deptIds?.Serialize() }, null, ex);
                // This method is void, so can't return error. Consider if it should throw or if AddOnExecuted/UpdateOnExecuted should handle this.
                // For now, just logging. If this is critical, an exception should be thrown to halt the parent operation.
                // throw; // Optionally rethrow if this failure should stop the parent Add/Update.
            }
        }

        /// <summary>
        /// 导出处理
        /// </summary>
        /// <param name="pageData"></param>
        /// <returns></returns>
        public override WebResponseContent Export(PageDataOptions pageData)
        {
            //限定只能导出当前角色能看到的所有用户
            QueryRelativeExpression = (IQueryable<Sys_User> queryable) =>
            {
                if (UserContext.Current.IsSuperAdmin) return queryable;
                List<int> roleIds = Sys_RoleService
                 .Instance
                 .GetAllChildrenRoleId(UserContext.Current.RoleId);
                return queryable.Where(x => roleIds.Contains(x.Role_Id) || x.User_Id == UserContext.Current.UserId);
            };

            base.ExportOnExecuting = (List<Sys_User> list, List<string> ignoreColumn) =>
            {
                if (!ignoreColumn.Contains("Role_Id"))
                {
                    ignoreColumn.Add("Role_Id");
                }
                if (!ignoreColumn.Contains("RoleName"))
                {
                    ignoreColumn.Remove("RoleName");
                }
                WebResponseContent responseData = new WebResponseContent(true);
                return responseData;
            };
            return base.Export(pageData);
        }
    }
}

