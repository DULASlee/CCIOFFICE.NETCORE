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
        // _context 可能为 null，如果 IHttpContextAccessor.HttpContext 返回 null (例如在单元测试或后台任务中)
        // 在使用前应进行null检查，或确保总是在有效的HTTP请求上下文中调用此类方法。
        private readonly Microsoft.AspNetCore.Http.HttpContext? _context;
        private readonly ISys_UserRepository _repository; // _repository 在构造函数中赋值，应不为null

        [ActivatorUtilitiesConstructor]
        public Sys_UserService(IHttpContextAccessor httpContextAccessor, ISys_UserRepository repository)
            : base(repository) // repository 已由ServiceBase处理nullability
        {
            _context = httpContextAccessor.HttpContext; // HttpContext 可能为null
            _repository = repository; // repository 已由ServiceBase处理nullability
        }

        // webResponse 在方法中通常会重新 new 或者通过 base 调用返回，所以这里初始化一个实例意义不大，
        // 且如果方法内逻辑确保赋值，则此处可不初始化。
        // 为安全起见，如果方法可能不完全覆盖所有路径来初始化它，则应在使用前检查或确保初始化。
        // 考虑到它在多个方法开始时被使用，这里初始化一个默认实例。
        WebResponseContent webResponse = new WebResponseContent();
        /// <summary>
        /// WebApi登陆
        /// </summary>
        /// <param name="loginInfo">登录信息，不能为空。</param>
        /// <param name="verificationCode">是否校验验证码。</param>
        /// <returns>包含登录结果的WebResponseContent。</returns>
        public async Task<WebResponseContent> Login(LoginInfo loginInfo, bool verificationCode = true)
        {
            // loginInfo 参数不应为null，调用者应保证。如果可能为null，则需添加null检查。
            // string msg = string.Empty; // msg 未被使用，可移除

            // _context 可能为 null，需要在使用前进行检查。
            if (_context == null)
            {
                Logger.Error(LoggerType.Login, "Login方法无法获取HttpContext。", loginInfo.Serialize(), null, new InvalidOperationException("HttpContext is null."));
                return webResponse.Error("登录服务内部错误，无法获取HTTP上下文。");
            }

            IMemoryCache memoryCache = _context.RequestServices.GetService<IMemoryCache>()!; // GetService<T> 可能返回null，使用!表示我们期望它不为null
            if (memoryCache == null)
            {
                Logger.Error(LoggerType.Login, "Login方法无法获取IMemoryCache服务。", loginInfo.Serialize(), null, new InvalidOperationException("IMemoryCache is null."));
                return webResponse.Error("登录服务内部错误，无法获取缓存服务。");
            }

            // loginInfo.UUID 和 loginInfo.VerificationCode 可能为null，取决于LoginInfo定义。假设它们不为null。
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
                // repository.FindAsIQueryable(...).FirstOrDefaultAsync() 可能返回 null
                Sys_User? user = await repository.FindAsIQueryable(x => x.UserName == loginInfo.UserName)
                    .FirstOrDefaultAsync();

                // user.UserPwd 可能为null，已使用 ?? "" 处理。 AppSetting.Secret.User 假定不为null。
                if (user == null || loginInfo.Password.Trim().EncryptDES(AppSetting.Secret.User!) != (user.UserPwd ?? ""))
                {
                    webResponse.Error(ResponseType.LoginError);
                    // Log failed login attempt before returning
                    Logger.Warning(LoggerType.Login, $"登录失败: 用户名或密码错误. UserName={loginInfo.UserName}", loginInfo.Serialize(), webResponse.Message);
                    memoryCache.Remove(loginInfo.UUID);
                    return webResponse;
                }

                // user 在此已确认不为null
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

                // Chinese Comment: 登录成功后，主动预热用户权限缓存。
                // (After successful login, proactively pre-warm the user's permission cache.)
                if (AppSetting.UseRedis && user != null) // 仅当配置了Redis时预热，且user不为null
                {
                    // UserContext.Current 在此阶段可能尚未完全初始化（取决于中间件和过滤器顺序），
                    // 但GetPermissions方法内部会处理UserContext的获取。
                    // 此处直接调用 UserContext.Current.GetPermissions 可能更直接，
                    // 但为了确保 UserContext 实例是最新的，并且与当前请求相关联，
                    // 通过 UserContext.Current 访问是标准做法。
                    // 如果UserContext.Current依赖于已填充的HttpContext.User.Claims，确保它们已设置。
                    // JwtHelper.IssueJwt 填充的是token，实际的ClaimsPrincipal可能在后续中间件中设置。
                    // 为确保安全，我们依赖 GetPermissions 内部逻辑来正确获取或构建 UserContext。
                    // 此处的 UserContext.Current.GetPermissions() 将会触发 UserContext 的实例化和 UserInfo 的加载（如果尚未加载）。
                    var userContextInstance = UserContext.Current; // 获取当前请求的UserContext实例
                    if (userContextInstance != null)
                    {
                        userContextInstance.GetPermissions(user.Role_Id);
                        Logger.Log(LogLevel.Debug, LogEvent.LoginCacheWarmup, $"用户 {user.UserName} (RoleID: {user.Role_Id}) 的权限已预热到缓存。 (Permissions for user {user.UserName} (RoleID: {user.Role_Id}) have been pre-warmed to cache.)");
                    }
                    else
                    {
                        Logger.Log(LogLevel.Warning, LogEvent.LoginCacheWarmup, $"用户 {user.UserName} (RoleID: {user.Role_Id}) 的权限预热失败：无法获取UserContext实例。 (Failed to pre-warm permissions for user {user.UserName} (RoleID: {user.Role_Id}): UserContext instance is null.)");
                    }
                }

                webResponse.OK(ResponseType.LoginSuccess);
                // Record successful user login for audit and security monitoring.
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
            UserInfo? userInfo = null; // userInfo 可能保持null
            try
            {
                // _context 可能为null
                if (_context == null)
                {
                    Logger.Error(LoggerType.ReplaceToeken, "ReplaceToken方法无法获取HttpContext。", null, null, new InvalidOperationException("HttpContext is null."));
                    return webResponse.Error("Token替换服务内部错误，无法获取HTTP上下文。");
                }
                // AppSetting.TokenHeaderName 假定不为null
                string? requestToken = _context.Request.Headers[AppSetting.TokenHeaderName!]; // 使用 ! 假设TokenHeaderName一定存在
                requestToken = requestToken?.Replace("Bearer ", ""); // requestToken 可能为null

                if (string.IsNullOrEmpty(requestToken)) // // 增加对 requestToken 为null或空的检查
                {
                    webResponse.Error("请求中未找到Token!");
                    Logger.Warning(LoggerType.ReplaceToeken, "Token替换失败: 请求中未提供Token。", null, webResponse.Message);
                    return webResponse;
                }

                var currentUserContext = UserContext.Current; // UserContext.Current 可能为null
                if (currentUserContext == null)
                {
                    webResponse.Error("无法获取当前用户信息!");
                    Logger.Warning(LoggerType.ReplaceToeken, "Token替换失败: 无法获取当前用户上下文。", null, webResponse.Message);
                    return webResponse;
                }
                // currentUserContext.Token 可能为null
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

                int userId = currentUserContext.UserId; // UserId 是int，不为null
                // repository.FindFirstAsync 可能返回null
                userInfo = await repository.FindFirstAsync(x => x.User_Id == userId,
                     s => new UserInfo() // 假定s.UserName等属性在此上下文中不为null
                     {
                         User_Id = userId,
                         UserName = s.UserName, // s.UserName 来自数据库，Sys_User.UserName是非null的
                         UserTrueName = s.UserTrueName, // s.UserTrueName 来自数据库，Sys_User.UserTrueName是非null的
                         Role_Id = s.Role_Id, // s.Role_Id 是int，不为null
                         RoleName = s.RoleName // s.RoleName 来自数据库，Sys_User.RoleName是可null的
                     });

                if (userInfo == null)
                {
                    webResponse.Error("未查到用户信息!");
                    Logger.Warning(LoggerType.ReplaceToeken, $"Token替换失败: 未查到用户信息. UserId={userId}", null, webResponse.Message);
                    return webResponse;
                }
                // userInfo 在此已确认不为null
                string newToken = JwtHelper.IssueJwt(userInfo);
                base.CacheContext.Remove(userId.GetUserIdKey()); // CacheContext 假定不为null
                // Token 属性在 Sys_User 中是 string?
                repository.Update(new Sys_User() { User_Id = userId, Token = newToken }, x => new { x.Token }, true);
                await repository.SaveChangesAsync();

                webResponse.OK(null, newToken);
                // Audit successful token replacement for user.
                Logger.Info(LoggerType.ReplaceToeken, $"Token替换成功: UserId={userId}, UserTrueName={userInfo.UserTrueName}", null, webResponse.Message);
            }
            catch (Exception ex)
            {
                var logUserId = userInfo?.User_Id ?? UserContext.Current?.UserId; // UserContext.Current 可能为null
                var logUserTrueName = userInfo?.UserTrueName ?? UserContext.Current?.UserTrueName; // UserTrueName 可能为null
                Logger.Error(LoggerType.ReplaceToeken, $"Token替换异常: UserId={logUserId}, UserTrueName={logUserTrueName}", null, null, ex);
                webResponse.Error("Token替换服务异常。");
            }
            return webResponse;
        }

        /// <summary>
        /// 修改密码
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public async Task<WebResponseContent> ModifyPwd(string? oldPwd, string? newPwd) // 参数可空
        {
            oldPwd = oldPwd?.Trim(); // oldPwd 可能为null
            newPwd = newPwd?.Trim(); // newPwd 可能为null

            var currentUser = UserContext.Current; // UserContext.Current 可能为null
            if (currentUser == null)
            {
                Logger.Warning(LoggerType.ApiModifyPwd, "修改密码失败: 无法获取当前用户上下文。", null, "UserContext is null");
                return webResponse.Error("无法获取用户信息，请重新登录。");
            }
            int userId = currentUser.UserId;

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

                // repository.FindFirstAsync 可能返回null (理论上不应发生，因为用户已登录)
                // s.UserPwd 在 Sys_User 中已标记为 string (非null)，假定数据库层面也是非null
                string? userCurrentPwd = await base.repository.FindFirstAsync(x => x.User_Id == userId, s => s.UserPwd);
                if (userCurrentPwd == null) // // 防御性检查，理论上不应发生
                {
                    webResponse.Error("无法获取当前用户密码信息。");
                    Logger.Error(LoggerType.ApiModifyPwd, $"修改密码失败: 未能获取用户当前密码. UserId={userId}", new { userId }, webResponse.Message);
                    return webResponse;
                }
                // AppSetting.Secret.User 假定不为null
                string _oldPwd = oldPwd.EncryptDES(AppSetting.Secret.User!);
                if (_oldPwd != userCurrentPwd)
                {
                    webResponse.Error("旧密码不正确");
                    Logger.Warning(LoggerType.ApiModifyPwd, $"修改密码失败: 旧密码不正确. UserId={userId}", new { userId }, webResponse.Message);
                    return webResponse;
                }

                string _newPwd = newPwd.EncryptDES(AppSetting.Secret.User!);
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
                // Record successful password modification by user for security auditing.
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
            var currentUser = UserContext.Current; // UserContext.Current 可能为null
            if (currentUser == null)
            {
                Logger.Warning(LoggerType.Select, "获取当前用户信息失败: 无法获取当前用户上下文。", null, "UserContext is null");
                return webResponse.Error("无法获取用户信息，请重新登录。");
            }
            var userId = currentUser.UserId;

            try
            {
                // repository.FindAsIQueryable(...).Select(...).FirstOrDefaultAsync() 可能返回null
                var data = await base.repository
                    .FindAsIQueryable(x => x.User_Id == userId)
                    .Select(s => new // 属性已在Sys_User中标记为可空或非空
                    {
                        s.UserName,       // string
                        s.UserTrueName,   // string
                        s.Address,        // string?
                        s.PhoneNo,        // string?
                        s.Email,          // string?
                        s.Remark,         // string?
                        s.Gender,         // int?
                        s.RoleName,       // string?
                        s.HeadImageUrl,   // string?
                        s.CreateDate      // DateTime?
                    })
                    .FirstOrDefaultAsync();

                if (data == null)
                {
                    Logger.Warning(LoggerType.Select, $"获取当前用户信息失败: 用户未找到. UserId={userId}", new { userId }, webResponse.Message);
                    return webResponse.Error("当前用户信息未找到。");
                }
                // Record successful retrieval of user's own information.
                Logger.Info(LoggerType.Select, $"获取当前用户信息成功: UserId={userId}", new { userId }, null); // data不直接序列化，因包含PDI
                return webResponse.OK(null, data);
            }
            catch (Exception ex)
            {
                // userId 在此作用域内有效
                Logger.Error(LoggerType.Select, $"获取当前用户信息异常: UserId={userId}", new { UserId = userId }, null, ex);
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
            // pageData.Value 可能为null
            if (pageData.Value != null)
            {
                roleId = pageData.Value.ToString()?.GetInt() ?? -1; // // 安全转换，如果Value.ToString()为null则用-1
            }

            IQueryable<Sys_UserDepartment>? deptQuery = null; // deptQuery 可以为null
            QueryRelativeList = (List<SearchParameters> parameters) => // parameters 不会为null
            {
                foreach (var item in parameters) // item 不会为null
                {
                    // item.Value 可能为null
                    if (!string.IsNullOrEmpty(item.Value) && item.Name == "DeptIds")
                    {
                        // item.Value 在此已确认不为null
                        var deptIds = item.Value.Split(",").Select(s => s.GetGuid()).Where(x => x != null);
                        item.Value = null; // 清空原值，避免重复处理
                        deptQuery = repository.DbContext.Set<Sys_UserDepartment>().Where(x => x.Enable == 1 && deptIds.Contains(x.DepartmentId));
                    }
                }
            };

            QueryRelativeExpression = (IQueryable<Sys_User> queryable) => // queryable 不会为null
             {
                 if (deptQuery != null) // deptQuery 可能为null
                 {
                     queryable = queryable.Where(c => deptQuery.Any(x => x.UserId == c.User_Id));
                 }

                 var currentUser = UserContext.Current; // UserContext.Current 可能为null
                 if (currentUser == null) // // 如果无法获取当前用户，则不进行角色过滤，或抛出异常
                 {
                      Logger.Warning(LoggerType.Select, "GetPageData无法获取当前用户上下文，角色过滤可能不准确。", pageData.Serialize(), null);
                      return queryable; // 或者根据业务需求返回错误或空数据
                 }

                 if (roleId <= 0)
                 {
                     if (currentUser.IsSuperAdmin) return queryable;
                     roleId = currentUser.RoleId;
                 }

                 List<int> roleIds = Sys_RoleService.Instance.GetAllChildrenRoleId(roleId); // GetAllChildrenRoleId 确保返回非null List
                 roleIds.Add(roleId);

                 if (roleId != currentUser.RoleId && !roleIds.Contains(roleId)) // // 判断查询的角色是否越权
                 {
                     roleId = -999; // 无效角色ID，使其查不到数据
                 }
                 return queryable.Where(x => roleIds.Contains(x.Role_Id));
             };
            var gridData = base.GetPageData(pageData); // GetPageData 返回非null, gridData.rows也非null

            gridData.rows.ForEach(x => // x 不会为null
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
            // saveModel.MainData 假定不为null，因已在上层Add(SaveModel)中处理
            base.AddOnExecute = (SaveModel userModel) => // userModel 不会为null
            {
                // userModel.MainData 可能为null，但通常AddOnExecute在主逻辑中被调用，此时MainData应有值
                int roleId = userModel.MainData?["Role_Id"].GetInt() ?? 0; // 安全访问
                var currentUser = UserContext.Current; // UserContext.Current 可能为null
                if (currentUser == null)
                {
                     Logger.Warning(LoggerType.Add, "AddOnExecute无法获取当前用户上下文。", userModel.Serialize(), "UserContext is null");
                     return webResponse.Error("无法获取用户信息，操作被中止。");
                }
                if (roleId > 0 && !currentUser.IsSuperAdmin)
                {
                    string? roleName = GetChildrenName(roleId); // GetChildrenName 可能返回null
                    if ((roleId == 1) || string.IsNullOrEmpty(roleName)) // roleName 可能为null
                        return webResponse.Error("不能选择此角色");
                }
                return webResponse.OK();
            };

            string pwd = 6.GenerateRandomNumber(); // 确保返回非null
            base.AddOnExecuting = (Sys_User user, object? obj) => // user不为null, obj可以为null
            {
                try
                {
                    // user.UserName 是 string (非null), Trim()安全。
                    user.UserName = user.UserName.Trim();
                    if (repository.Exists(x => x.UserName == user.UserName))
                        return new WebResponseContent().Error("用户名已经被注册");
                    // AppSetting.Secret.User 假定不为null
                    user.UserPwd = pwd.EncryptDES(AppSetting.Secret.User!); // UserPwd 是 string (非null)
                    return new WebResponseContent().OK();
                }
                catch (Exception ex)
                {
                    Logger.Error(LoggerType.Add, $"新建用户校验用户名存在性异常: UserName={user.UserName}", user.Serialize(), null, ex);
                    return new WebResponseContent().Error("校验用户名时发生数据库错误。");
                }
            };

            base.AddOnExecuted = (Sys_User user, object? list) => // user不为null, list可以为null
            {
                try
                {
                    // user.DeptIds 是 string? (可null)
                    var deptIds = user.DeptIds?.Split(",").Select(s => s.GetGuid()).Where(x => x != null).Select(s => (Guid)s).ToArray();
                    SaveDepartment(deptIds, user.User_Id); // deptIds可以为null, SaveDepartment会处理
                    return new WebResponseContent().OK($"用户新建成功.帐号{user.UserName}密码{pwd}");
                }
                catch (Exception ex)
                {
                    Logger.Error(LoggerType.Add, $"新建用户后保存部门信息异常: UserId={user.User_Id}, UserName={user.UserName}", user.Serialize(), null, ex);
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
            // ids 不会为null
            base.DelOnExecuting = (object[] ids) =>
            {
                try
                {
                    var currentUser = UserContext.Current; // UserContext.Current 可能为null
                    if (currentUser == null)
                    {
                        Logger.Warning(LoggerType.Delete, "DelOnExecuting无法获取当前用户上下文。", new { UserIds = ids.Serialize() }, "UserContext is null");
                        return webResponse.Error("无法获取用户信息，操作被中止。");
                    }
                    if (!currentUser.IsSuperAdmin)
                    {
                        // ids中的元素可能为null，但Select会处理。Convert.ToInt32如果遇到null或非数字会抛异常。
                        // // 确保ids中的元素是有效的int。
                        int[] userIds = ids.Where(id => id != null && int.TryParse(id.ToString(), out _))
                                           .Select(id => Convert.ToInt32(id))
                                           .ToArray();

                        var delUserInfos = repository.Find(x => userIds.Contains(x.User_Id), s => new { s.User_Id, s.Role_Id, s.UserTrueName });

                        List<int> roleIds = Sys_RoleService.Instance.GetAllChildrenRoleId(currentUser.RoleId); // GetAllChildrenRoleId确保返回非null List

                        // s.UserTrueName 来自数据库，Sys_User.UserTrueName 是非null的
                        string[] userNames = delUserInfos.Where(x => !roleIds.Contains(x.Role_Id))
                                                         .Select(s => s.UserTrueName)
                                                         .ToArray();
                        if (userNames.Any())
                        {
                            return new WebResponseContent().Error($"没有权限删除用户：{string.Join(',', userNames)}");
                        }
                    }
                    return new WebResponseContent().OK();
                }
                catch (Exception ex)
                {
                    Logger.Error(LoggerType.Delete, "删除用户前置校验异常", new { UserIds = ids.Serialize() }, null, ex);
                    return new WebResponseContent().Error("删除用户校验时发生数据库错误。");
                }
            };
            // userIds 不会为null
            base.DelOnExecuted = (object[] userIds) =>
            {
                try
                {
                    // GetInt() 和 GetUserIdKey() 扩展方法应能处理潜在的null元素
                    var objKeys = userIds.Select(x => x.GetInt().GetUserIdKey());
                    base.CacheContext.RemoveAll(objKeys); // CacheContext 假定不为null
                    return new WebResponseContent().OK("用户缓存已清除。");
                }
                catch (Exception ex)
                {
                    Logger.Error(LoggerType.Delete, "删除用户后清除缓存异常", new { UserIds = userIds.Serialize() }, null, ex);
                    return new WebResponseContent().OK("用户已删除，但清除缓存时遇到问题。");
                }
            };
            return base.Del(keys, delList);
        }

        // 返回类型可以是null
        private string? GetChildrenName(int roleId)
        {
            var currentUserInfo = UserContext.Current?.UserInfo; // UserContext.Current 或 UserInfo 可能为null
            if (currentUserInfo == null)
            {
                Logger.Warning(LoggerType.Unknown, $"GetChildrenName无法获取当前用户信息。RoleId={roleId}", null, "UserContext or UserInfo is null");
                return null; // // 如果没有当前用户信息，则无法判断角色关系
            }
            // GetAllChildren 返回 List<Sys_Role_extend>，FirstOrDefault 可能返回null
            // s.RoleName 在 Sys_Role_extend 中是 string?
            string? roleName = Sys_RoleService.Instance
                .GetAllChildren(currentUserInfo.Role_Id)
                .FirstOrDefault(x => x.Id == roleId)
                ?.RoleName;
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
            // userInfo 在此作用域内保证不为null (来自 Update 方法的 UserContext.Current.UserInfo)
            // saveModel.MainData 假定不为null
            saveModel.MainData["RoleName"] = "无"; // RoleName 在Sys_User中是string?，"无"是有效字符串

            base.UpdateOnExecute = (SaveModel saveInfo) => // saveInfo 不会为null
            {
                var currentUser = UserContext.Current; // UserContext.Current 可能为null
                if (currentUser == null)
                {
                     Logger.Warning(LoggerType.Update, "UpdateOnExecute无法获取当前用户上下文。", saveInfo.Serialize(), "UserContext is null");
                     return webResponse.Error("无法获取用户信息，操作被中止。");
                }
                if (!currentUser.IsSuperAdmin)
                {
                    // saveModel.MainData 可能为null，但在此上下文中，它来自外部Update的saveModel，已被检查
                    int roleId = saveModel.MainData!["Role_Id"].GetInt(); // 使用 ! 确认 MainData 不为null
                    string? roleName = GetChildrenName(roleId); // GetChildrenName 可能返回null
                    saveInfo.MainData.TryAdd("RoleName", roleName); // roleName 可以为null
                    if (UserContext.IsRoleIdSuperAdmin(userInfo.Role_Id)) // userInfo 来自外部 Update 方法，保证不为null
                    {
                        return webResponse.OK();
                    }
                    if (string.IsNullOrEmpty(roleName)) return webResponse.Error("不能选择此角色");
                }
                return webResponse.OK();
            };
            // user不为null, obj1, obj2, list 都可以为null
            base.UpdateOnExecuting = (Sys_User user, object? obj1, object? obj2, List<object>? list) =>
            {
                try
                {
                    // userInfo 来自外部 Update 方法，保证不为null
                    if (user.User_Id == userInfo.User_Id && user.Role_Id != userInfo.Role_Id)
                        return new WebResponseContent().Error("不能修改自己的角色");

                    // repository.Find(...).FirstOrDefault() 可能返回null
                    var _userDBInfo = repository.Find(x => x.User_Id == user.User_Id,
                        s => new { s.UserName, s.UserPwd }) // UserName 和 UserPwd 在Sys_User中是非null的
                        .FirstOrDefault();

                    if (_userDBInfo == null)
                        return new WebResponseContent().Error("未找到要更新的用户信息。");

                    user.UserName = _userDBInfo.UserName; // Preserve original UserName
                    user.UserPwd = _userDBInfo.UserPwd;   // Preserve original Password
                    return new WebResponseContent().OK();
                }
                catch (Exception ex)
                {
                    Logger.Error(LoggerType.Update, $"更新用户前置校验异常: UserId={user.User_Id}", user.Serialize(), null, ex);
                    return new WebResponseContent().Error("更新用户校验时发生数据库错误。");
                }
            };
            // user不为null, obj1, obj2, List可以为null
            base.UpdateOnExecuted = (Sys_User user, object? obj1, object? obj2, List<object>? List) =>
            {
                try
                {
                    base.CacheContext.Remove(user.User_Id.GetUserIdKey()); // CacheContext 假定不为null
                    // user.DeptIds 是 string? (可null)
                    var deptIds = user.DeptIds?.Split(",").Select(s => s.GetGuid()).Where(x => x != null).Select(s => (Guid)s).ToArray();
                    SaveDepartment(deptIds, user.User_Id); // deptIds可以为null, SaveDepartment会处理
                    return new WebResponseContent(true).OK("用户信息已更新，缓存已清除，部门已保存。");
                }
                catch (Exception ex)
                {
                    Logger.Error(LoggerType.Update, $"更新用户后操作异常 (缓存或部门): UserId={user.User_Id}", user.Serialize(), null, ex);
                    return new WebResponseContent(true).OK("用户信息已更新，但后续操作(清除缓存或保存部门)时遇到问题。");
                }
            };
            return base.Update(saveModel);
        }


        /// <summary>
        /// 保存用户关联的部门信息。
        /// </summary>
        /// <param name="deptIds">新的部门ID数组，可以为null或空数组。</param>
        /// <param name="userId">用户ID。</param>
        public void SaveDepartment(Guid[]? deptIds, int userId) // deptIds 可以为null
        {
            if (userId <= 0)
            {
                Logger.Warning(LoggerType.Update, $"保存用户部门失败: 无效的UserId. UserId={userId}", new { userId, deptIds = deptIds?.Serialize() }, null);
                return;
            }
            // 如果传入的deptIds为null，则视为空数组，表示清除所有部门关联或按逻辑处理。
            deptIds ??= Array.Empty<Guid>();

            try
            {
                var existingUserDepts = repository.DbContext.Set<Sys_UserDepartment>()
                                          .Where(x => x.UserId == userId)
                                          .Select(s => new { s.DepartmentId, s.Enable, s.Id })
                                          .ToList(); // ToList确保后续操作在内存中进行

                if (deptIds.Length == 0 && !existingUserDepts.Exists(x => x.Enable == 1))
                {
                    return;
                }

                var currentUser = UserContext.Current?.UserInfo; // UserContext.Current 或 UserInfo 可能为null
                if (currentUser == null)
                {
                    Logger.Error(LoggerType.Update, $"保存用户部门失败: 无法获取当前操作用户信息. UserId={userId}", new { userId, deptIds = deptIds.Serialize() }, "UserContext or UserInfo is null");
                    // // 根据业务决定是否抛出异常或仅记录错误并返回
                    // throw new InvalidOperationException("无法获取当前操作用户信息，无法保存部门。");
                    return; // // 或者直接返回，不进行操作
                }
                // 新增的部门关联
                var deptsToAdd = deptIds.Where(id => !existingUserDepts.Any(eud => eud.DepartmentId == id))
                                       .Select(id => new Sys_UserDepartment()
                                       {
                                           DepartmentId = id, UserId = userId, Enable = 1,
                                           CreateDate = DateTime.Now, Creator = currentUser.UserTrueName, // UserTrueName是string (非null)
                                           CreateID = currentUser.User_Id
                                       }).ToList();

                List<Sys_UserDepartment> deptsToUpdate = new List<Sys_UserDepartment>();
                // 停用不再选择的部门 (之前是启用的)
                deptsToUpdate.AddRange(existingUserDepts
                    .Where(eud => !deptIds.Contains(eud.DepartmentId) && eud.Enable == 1)
                    .Select(eud => new Sys_UserDepartment()
                    {
                        Id = eud.Id, UserId = userId, DepartmentId = eud.DepartmentId, Enable = 0,
                        ModifyDate = DateTime.Now, Modifier = currentUser.UserTrueName, ModifyID = currentUser.User_Id
                    }));

                // 重新启用的部门 (之前是停用的)
                deptsToUpdate.AddRange(existingUserDepts
                    .Where(eud => deptIds.Contains(eud.DepartmentId) && eud.Enable != 1)
                    .Select(eud => new Sys_UserDepartment()
                    {
                        Id = eud.Id, UserId = userId, DepartmentId = eud.DepartmentId, Enable = 1,
                        ModifyDate = DateTime.Now, Modifier = currentUser.UserTrueName, ModifyID = currentUser.User_Id
                    }));

                if (deptsToAdd.Any())
                    repository.AddRange(deptsToAdd); // AddRange 内部处理保存

                if (deptsToUpdate.Any())
                    repository.UpdateRange(deptsToUpdate, x => new { x.Enable, x.ModifyDate, x.Modifier, x.ModifyID }); // UpdateRange 内部处理保存

                if (deptsToAdd.Any() || deptsToUpdate.Any())
                {
                    repository.SaveChanges(); // // 统一保存所有更改
                    // Audit changes to user's department assignments.
                    Logger.Info(LoggerType.Update, $"保存用户部门成功: UserId={userId}", new { userId, deptIds = deptIds.Serialize(), added = deptsToAdd.Count, updated = deptsToUpdate.Count }, null);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LoggerType.Update, $"保存用户部门异常: UserId={userId}", new { userId, deptIds = deptIds?.Serialize() }, null, ex);
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

