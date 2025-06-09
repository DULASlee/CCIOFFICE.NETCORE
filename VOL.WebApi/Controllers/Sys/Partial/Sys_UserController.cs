
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<Sys_UserController> _logger;
        [ActivatorUtilitiesConstructor]
        public Sys_UserController(
               ISys_UserService userService,
               ISys_UserRepository userRepository,
               ICacheService cahce,
               ILogger<Sys_UserController> logger
              )
          : base(userService)
        {
            _userRepository = userRepository;
            _cache = cahce;
            _logger = logger;
        }

        [HttpPost, HttpGet, Route("login"), AllowAnonymous]
        [ObjectModelValidatorFilter(ValidatorModel.Login)]
        public async Task<IActionResult> Login([FromBody] LoginInfo loginInfo)
        {
            _logger.LogInformation("Login attempt for User: {UserName}", loginInfo?.UserName);
            if (loginInfo == null)
            {
                _logger.LogWarning("Login called with null loginInfo.");
                return new BadRequestObjectResult(new { status = false, message = "Login data cannot be null." });
            }
            try
            {
                return Json(await Service.Login(loginInfo));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Login for User: {UserName}", loginInfo.UserName);
                return StatusCode(StatusCodes.Status500InternalServerError, new { status = false, message = "An error occurred during login." });
            }
        }

        private readonly ConcurrentDictionary<int, object> _lockCurrent = new ConcurrentDictionary<int, object>();
        [HttpPost, Route("replaceToken")]
        public IActionResult ReplaceToken()
        {
            _logger.LogInformation("ReplaceToken called for UserId: {UserId}", UserContext.Current.UserId);
            WebResponseContent responseContent = new WebResponseContent();
            string key = $"rp:Token:{UserContext.Current.UserId}";
            UserInfo userInfo = null;
            try
            {
                //如果5秒内替换过token,直接使用最新的token(防止一个页面多个并发请求同时替换token导致token错位)
                if (_cache.Exists(key))
                {
                    _logger.LogInformation("ReplaceToken cache hit for UserId: {UserId}", UserContext.Current.UserId);
                    return Json(responseContent.OK(null, _cache.Get(key)));
                }
                var _obj = _lockCurrent.GetOrAdd(UserContext.Current.UserId, new object() { });
                lock (_obj)
                {
                    if (_cache.Exists(key))
                    {
                        _logger.LogInformation("ReplaceToken cache hit (inside lock) for UserId: {UserId}", UserContext.Current.UserId);
                        return Json(responseContent.OK(null, _cache.Get(key)));
                    }
                    string requestToken = HttpContext.Request.Headers[AppSetting.TokenHeaderName];
                    requestToken = requestToken?.Replace("Bearer ", "");

                    if (JwtHelper.IsExp(requestToken))
                    {
                        _logger.LogWarning("Token expired for UserId: {UserId} during ReplaceToken", UserContext.Current.UserId);
                        return Json(responseContent.Error("Token已过期!"));
                    }

                    int userId = UserContext.Current.UserId;

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
                        _logger.LogWarning("User info not found for token replacement for UserId: {UserId}", userId);
                        return Json(responseContent.Error("未查到用户信息!"));
                    }

                    string token = JwtHelper.IssueJwt(userInfo);
                    //移除当前缓存
                    _cache.Remove(userId.GetUserIdKey());
                    //只更新的token字段
                    _userRepository.Update(new Sys_User() { User_Id = userId, Token = token }, x => x.Token, true);
                    //添加一个5秒缓存
                    _cache.Add(key, token, 5);
                    responseContent.OK(null, token);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during token replacement for UserId: {UserId}", UserContext.Current.UserId);
                responseContent.Error("token替换异常");
            }
            finally
            {
                _lockCurrent.TryRemove(UserContext.Current.UserId, out object val);
                string outcomeMessage = $"用户{userInfo?.User_Id}_{userInfo?.UserTrueName} token replacement {(responseContent.Status ? "succeeded" : "failed")}.";
                if (responseContent.Status)
                {
                    _logger.LogInformation(outcomeMessage + " New token present in response data."); // Token itself is not logged for security. responseContent.Data holds it.
                }
                else
                {
                    _logger.LogWarning(outcomeMessage + (string.IsNullOrEmpty(responseContent.Message) ? "" : " Reason: " + responseContent.Message));
                }
            }
            return Json(responseContent);
        }


        [HttpPost, Route("modifyPwd")]
        [ApiActionPermission]
        public async Task<IActionResult> ModifyPwd([FromBody] Dictionary<string, string> info)
        {
            _logger.LogInformation("ModifyPwd called for UserId: {UserId}", UserContext.Current.UserId);
            if (info == null || !info.ContainsKey("oldPwd") || !info.ContainsKey("newPwd"))
            {
                _logger.LogWarning("ModifyPwd called with null info or missing keys for UserId: {UserId}", UserContext.Current.UserId);
                return new BadRequestObjectResult(new { status = false, message = "Password information is incomplete." });
            }
            try
            {
                return Json(await Service.ModifyPwd(info["oldPwd"], info["newPwd"]));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ModifyPwd for UserId: {UserId}", UserContext.Current.UserId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { status = false, message = "An error occurred while modifying password." });
            }
        }


        [HttpPost, Route("getCurrentUserInfo")]
        public async Task<IActionResult> GetCurrentUserInfo()
        {
            _logger.LogInformation("GetCurrentUserInfo called for UserId: {UserId}", UserContext.Current.UserId);
            try
            {
                return Json(await Service.GetCurrentUserInfo());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCurrentUserInfo for UserId: {UserId}", UserContext.Current.UserId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { status = false, message = "An error occurred while fetching current user info." });
            }
        }

        //只能超级管理员才能修改密码
        //2020.08.01增加修改密码功能
        //[HttpPost, Route("modifyUserPwd"), ApiActionPermission(ActionRolePermission.SuperAdmin)]
        [HttpPost, Route("modifyUserPwd"), ApiActionPermission(ActionPermissionOptions.Add | ActionPermissionOptions.Update)]
        public IActionResult ModifyUserPwd([FromBody] LoginInfo loginInfo)
        {
            _logger.LogInformation("ModifyUserPwd attempt for User: {UserName}", loginInfo?.UserName);
            WebResponseContent webResponse = new WebResponseContent();

            if (loginInfo == null)
            {
                _logger.LogWarning("ModifyUserPwd called with null loginInfo.");
                return Json(webResponse.Error("参数不完整"));
            }

            string userName = loginInfo.UserName;
            string password = loginInfo.Password;

            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(userName))
            {
                _logger.LogWarning("ModifyUserPwd called with incomplete parameters for User: {UserName}", userName);
                return Json(webResponse.Error("参数不完整"));
            }
            if (password.Length < 6)
            {
                _logger.LogWarning("ModifyUserPwd password length too short for User: {UserName}", userName);
                return Json(webResponse.Error("密码长度不能少于6位"));
            }

            try
            {
                Sys_User user = _userRepository.FindFirst(x => x.UserName == userName);
                if (user == null)
                {
                    _logger.LogWarning("User not found for ModifyUserPwd: {UserName}", userName);
                    return Json(webResponse.Error("用户不存在"));
                }
                user.UserPwd = password.EncryptDES(AppSetting.Secret.User);
                _userRepository.Update(user, x => new { x.UserPwd }, true);
                UserContext.Current.LogOut(user.User_Id);
                _logger.LogInformation("Password successfully modified for User: {UserName}, UserId: {UserId}", userName, user.User_Id);
                return Json(webResponse.OK("密码修改成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ModifyUserPwd for User: {UserName}", userName);
                return Json(webResponse.Error("修改密码时发生异常"));
            }
        }

        /// <summary>
        /// 2020.06.15增加登陆验证码
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("getVierificationCode"), AllowAnonymous]
        public IActionResult GetVierificationCode()
        {
            _logger.LogInformation("GetVierificationCode called.");
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetVierificationCode.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { status = false, message = "An error occurred while generating verification code." });
            }
        }

        [ApiActionPermission()]
        public override IActionResult Upload(IEnumerable<IFormFile> fileInput)
        {
            _logger.LogInformation("Upload (Sys_UserController) called with {FileCount} files.", fileInput?.Count() ?? 0);
            if (fileInput == null || !fileInput.Any())
            {
                _logger.LogWarning("Upload (Sys_UserController) called with no files.");
                return new BadRequestObjectResult(new { status = false, message = "No files provided for upload." });
            }
            try
            {
                return base.Upload(fileInput);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Upload (Sys_UserController). File count: {FileCount}", fileInput.Count());
                return StatusCode(StatusCodes.Status500InternalServerError, new { status = false, message = "An error occurred during file upload." });
            }
        }
        [HttpPost, Route("updateUserInfo")]
        public IActionResult UpdateUserInfo([FromBody] Sys_User user)
        {
            _logger.LogInformation("UpdateUserInfo called for UserId: {UserId}", UserContext.Current.UserId);
            if (user == null)
            {
                _logger.LogWarning("UpdateUserInfo called with null user data for UserId: {UserId}", UserContext.Current.UserId);
                return new BadRequestObjectResult(new { status = false, message = "User data cannot be null." });
            }
            try
            {
                user.User_Id = UserContext.Current.UserId;
                _userRepository.Update(user, x => new { x.UserTrueName, x.Gender, x.Remark, x.HeadImageUrl }, true);
                _logger.LogInformation("User info updated successfully for UserId: {UserId}", user.User_Id);
                return Content("修改成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateUserInfo for UserId: {UserId}", UserContext.Current.UserId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { status = false, message = "修改用户信息时发生异常" });
            }
        }
    }
}
