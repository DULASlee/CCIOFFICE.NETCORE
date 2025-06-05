
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using VOL.Core.CacheManager;
using VOL.Core.Configuration;
using VOL.Core.Controllers.Basic;
using VOL.Core.DBManager;
using VOL.Core.EFDbContext;
using VOL.Core.Enums;
using VOL.Core.Extensions;
using VOL.Core.Filters;
using VOL.Core.Infrastructure;
using VOL.Core.ManageUser;
using VOL.Core.ObjectActionValidator;
using VOL.Core.Services;
using VOL.Core.Utilities;
using VOL.Entity.AttributeManager;
using VOL.Entity.DomainModels;
using VOL.Sys.IRepositories;
using VOL.Sys.IServices;
using VOL.Sys.Repositories;

namespace VOL.Sys.Controllers
{
    [Route("api/User")]
    public partial class Sys_UserController
    {
        private ISys_UserRepository _userRepository;
        private ICacheService _cache;
        [ActivatorUtilitiesConstructor]
        public Sys_UserController(
               ISys_UserService userService,
               ISys_UserRepository userRepository,
               ICacheService cahce
              )
          : base(userService)
        {
            _userRepository = userRepository;
            _cache = cahce;
        }

        [HttpPost, HttpGet, Route("login"), AllowAnonymous]
        [ObjectModelValidatorFilter(ValidatorModel.Login)]
        public async Task<IActionResult> Login([FromBody] LoginInfo loginInfo)
        {
            try
            {
                var response = await Service.Login(loginInfo);
                // Assuming Service.Login returns a WebResponseContent
                // If response.Status is false, the service should have logged the specific reason.
                // The controller's responsibility here is to return the response or handle unexpected service errors.
                return Json(response);
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(LogLevel.Error, LogEvent.Login, $"Login action 执行异常: UserName={loginInfo?.UserName}", loginInfo?.Serialize(), ex);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "登录时发生内部服务器错误。", status = false });
            }
        }

        private readonly ConcurrentDictionary<int, object> _lockCurrent = new ConcurrentDictionary<int, object>();
        [HttpPost, Route("replaceToken")]
        public IActionResult ReplaceToken()
        {
            WebResponseContent responseContent = new WebResponseContent();
            string key = $"rp:Token:{UserContext.Current.UserId}"; // Safe to get UserId here for key
            UserInfo userInfo = null;
            WebResponseContent responseContent = new WebResponseContent(); // Initialize here
            try
            {
                //如果5秒内替换过token,直接使用最新的token(防止一个页面多个并发请求同时替换token导致token错位)
                if (_cache.Exists(key))
                {
                    return Json(responseContent.OK(null, _cache.Get(key)));
                }
                // Use current user context carefully before async/await or complex logic
                var currentUser = UserContext.Current;
                var _obj = _lockCurrent.GetOrAdd(currentUser.UserId, new object() { });

                lock (_obj)
                {
                    if (_cache.Exists(key))
                    {
                        return Json(responseContent.OK(null, _cache.Get(key)));
                    }
                    string requestToken = HttpContext.Request.Headers[AppSetting.TokenHeaderName];
                    requestToken = requestToken?.Replace("Bearer ", "");

                    if (currentUser.Token != requestToken) // Check against current user's token from context
                    {
                         Logger.Warning(LogEvent.ReplaceToeken, $"Token失效(请求与上下文不一致): UserId={currentUser.UserId}", null, responseContent.Message);
                         return Json(responseContent.Error("Token已失效!"));
                    }

                    if (JwtHelper.IsExp(requestToken))
                    {
                        Logger.Warning(LogEvent.ReplaceToeken, $"Token过期: UserId={currentUser.UserId}", null, responseContent.Message);
                        return Json(responseContent.Error("Token已过期!"));
                    }

                    int userId = currentUser.UserId;
                    // DB Call
                    userInfo = _userRepository.FindAsIQueryable(x => x.User_Id == userId).Select(
                             s => new UserInfo()
                             {
                                 User_Id = userId,
                                 UserName = s.UserName,
                                 UserTrueName = s.UserTrueName,
                                 Role_Id = s.Role_Id,
                                 RoleName = s.RoleName
                             }).FirstOrDefault();

                    if (userInfo == null)
                    {
                        Logger.Warning(LogEvent.ReplaceToeken, $"Token替换失败(用户未找到): UserId={userId}", null, responseContent.Message);
                        return Json(responseContent.Error("未查到用户信息!"));
                    }

                    string token = JwtHelper.IssueJwt(userInfo);
                    _cache.Remove(userId.GetUserIdKey()); // Cache call
                    // DB Call - Assuming Update and SaveChanges are handled by repository or UoW
                    _userRepository.Update(new Sys_User() { User_Id = userId, Token = token }, x => x.Token, true);
                    _userRepository.SaveChanges(); // Explicitly save if Update doesn't auto-save

                    _cache.Add(key, token, 5); // Cache call
                    responseContent.OK(null, token);
                    Logger.Info(LogLevel.Information, LogEvent.ReplaceToeken, $"Token替换成功: UserId={userId}, UserTrueName={userInfo.UserTrueName}", null, responseContent.Message);
                }
            }
            catch (Exception ex)
            {
                var userIdForLog = UserContext.Current?.UserId ?? userInfo?.User_Id; // Attempt to get UserId for logging
                Logger.Error(LogLevel.Error, LogEvent.ReplaceToeken, $"Token替换异常: UserId={userIdForLog}", null, ex);
                responseContent.Error("Token替换服务发生内部错误。");
            }
            finally
            {
                // Ensure lock object is removed if it was added.
                if (UserContext.Current != null && UserContext.Current.UserId != 0) // Check if UserContext is valid
                     _lockCurrent.TryRemove(UserContext.Current.UserId, out object val);
            }
            return Json(responseContent);
        }


        [HttpPost, Route("modifyPwd")]
        [ApiActionPermission]
        public async Task<IActionResult> ModifyPwd([FromBody] Dictionary<string, string> info)
        {
            try
            {
                var oldPwd = info?.ContainsKey("oldPwd") == true ? info["oldPwd"] : null;
                var newPwd = info?.ContainsKey("newPwd") == true ? info["newPwd"] : null;
                // It's good to log what is being received by the controller for sensitive operations if parameters allow.
                // Here, passwords themselves are not logged, only their presence or absence could be inferred if needed.
                var response = await Service.ModifyPwd(oldPwd, newPwd);
                return Json(response);
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(LogLevel.Error, LogEvent.ApiModifyPwd, $"ModifyPwd action 执行异常: UserId={UserContext.Current?.UserId}", null, ex);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "修改密码时发生内部服务器错误。", status = false });
            }
        }


        [HttpPost, Route("getCurrentUserInfo")]
        public async Task<IActionResult> GetCurrentUserInfo()
        {
            try
            {
                var response = await Service.GetCurrentUserInfo();
                return Json(response);
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(LogLevel.Error, LogEvent.GetUserInfo, $"GetCurrentUserInfo action 执行异常: UserId={UserContext.Current?.UserId}", null, ex);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "获取当前用户信息时发生内部服务器错误。", status = false });
            }
        }

        //只能超级管理员才能修改密码
        //2020.08.01增加修改密码功能
        //[HttpPost, Route("modifyUserPwd"), ApiActionPermission(ActionRolePermission.SuperAdmin)]
        [HttpPost, Route("modifyUserPwd"), ApiActionPermission(ActionPermissionOptions.Add | ActionPermissionOptions.Update)]
        public IActionResult ModifyUserPwd([FromBody] LoginInfo loginInfo)
        {
            WebResponseContent webResponse = new WebResponseContent();
            string userName = loginInfo?.UserName;
            string password = loginInfo?.Password; // In a real scenario, ensure this isn't logged directly if it's a new password.

            try
            {
                if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(userName))
                {
                    Logger.Warning(LogEvent.ApiModifyPwd, $"修改用户密码失败: 参数不完整. UserName={(string.IsNullOrEmpty(userName) ? "[null_or_empty]" : userName)}", loginInfo?.Serialize());
                    return Json(webResponse.Error("参数不完整"));
                }
                if (password.Length < 6)
                {
                    Logger.Warning(LogEvent.ApiModifyPwd, $"修改用户密码失败: 密码长度小于6位. UserName={userName}", loginInfo?.Serialize());
                    return Json(webResponse.Error("密码长度不能少于6位"));
                }

                Sys_User user = _userRepository.FindFirst(x => x.UserName == userName); // DB Call
                if (user == null)
                {
                    Logger.Warning(LogEvent.ApiModifyPwd, $"修改用户密码失败: 用户不存在. UserName={userName}", loginInfo?.Serialize());
                    return Json(webResponse.Error("用户不存在"));
                }

                user.UserPwd = password.EncryptDES(AppSetting.Secret.User);
                _userRepository.Update(user, x => new { x.UserPwd }, true); // DB Call
                _userRepository.SaveChanges(); // Explicitly save changes

                //如果用户在线，强制下线
                UserContext.Current.LogOut(user.User_Id);
                Logger.Info(LogLevel.Information, LogEvent.ApiModifyPwd, $"用户密码修改成功: UserName={userName}, TargetUserId={user.User_Id}", loginInfo?.Serialize());
                return Json(webResponse.OK("密码修改成功"));
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(LogLevel.Error, LogEvent.ApiModifyPwd, $"修改用户密码异常: UserName={userName}", loginInfo?.Serialize(), ex);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "修改用户密码时发生内部服务器错误。", status = false });
            }
        }

        /// <summary>
        /// 2020.06.15增加登陆验证码
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("getVierificationCode"), AllowAnonymous]
        public IActionResult GetVierificationCode()
        {
            string code = VierificationCode.RandomText();
            var data = new
            {
                img = VierificationCode.CreateBase64Imgage(code),
                uuid = Guid.NewGuid()
            };
            HttpContext.GetService<IMemoryCache>().Set(data.uuid.ToString(), code, new TimeSpan(0, 5, 0));
            return Json(data);
        }

        [ApiActionPermission()]
        public override IActionResult Upload(IEnumerable<IFormFile> fileInput)
        {
            return base.Upload(fileInput);
        }
        [HttpPost, Route("updateUserInfo")]
        public IActionResult UpdateUserInfo([FromBody] Sys_User user)
        {
            try
            {
                user.User_Id = UserContext.Current.UserId;

                // Log the attempt, be cautious about logging the entire 'user' object if it contains sensitive data not being updated.
                // Here, we are updating specific fields, so logging those or just UserId is safer.
                var fieldsToUpdate = new { user.UserTrueName, user.Gender, user.Remark, user.HeadImageUrl };
                Logger.Info(LogLevel.Information, LogEvent.EditUserInfo, $"尝试更新用户信息: UserId={user.User_Id}", new { UserId = user.User_Id, Data = fieldsToUpdate });

                _userRepository.Update(user, x => new { x.UserTrueName, x.Gender, x.Remark, x.HeadImageUrl }, true); // DB Call
                _userRepository.SaveChanges(); // Explicitly save changes

                Logger.Info(LogLevel.Information, LogEvent.EditUserInfo, $"用户信息更新成功: UserId={user.User_Id}", new { UserId = user.User_Id });
                return Json(new { status = true, message = "修改成功" }); // Return JSON for consistency
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(LogLevel.Error, LogEvent.EditUserInfo, $"更新用户信息异常: UserId={UserContext.Current?.UserId}", user?.Serialize(Newtonsoft.Json.Formatting.None,ส่วนบุคคลFields:new string[] { "UserPwd", "Token"}), ex);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "更新用户信息时发生内部服务器错误。", status = false });
            }
        }
    }
}
