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
using System.Text.RegularExpressions;
using Serilog;
using System.IO;

namespace VOL.Sys.Controllers
{
    [Route("api/User")]
    public partial class Sys_UserController
    {
        private ISys_UserRepository _userRepository;
        private ICacheService _cache;

        // 用于防止并发修改的锁
        private readonly ConcurrentDictionary<int, object> _lockCurrent = new ConcurrentDictionary<int, object>();

        // 密码复杂度正则表达式（至少8位，包含大小写字母和数字）
        private static readonly Regex PasswordRegex = new(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        [ActivatorUtilitiesConstructor]
        public Sys_UserController(
            ISys_UserService userService,
            ISys_UserRepository userRepository,
            ICacheService cache)
            : base(userService)
        {
            _userRepository = userRepository;
            _cache = cache;
        }

        /// <summary>
        /// 用户登录接口
        /// </summary>
        [HttpPost, HttpGet, Route("login"), AllowAnonymous]
        [ObjectModelValidatorFilter(ValidatorModel.Login)]
        public async Task<IActionResult> Login([FromBody] LoginInfo loginInfo)
        {
            var clientIp = GetClientIpAddress();
            var requestId = Guid.NewGuid().ToString("N");

            try
            {
                // 输入验证
                if (loginInfo == null || string.IsNullOrWhiteSpace(loginInfo.UserName) || string.IsNullOrWhiteSpace(loginInfo.Password))
                {
                    Log.Warning("登录验证失败 - IP: {ClientIp}, 用户名: {UserName}, RequestId: {RequestId}",
                        clientIp, loginInfo?.UserName, requestId);
                    return Json(new WebResponseContent().Error("用户名和密码不能为空"));
                }

                // XSS防护
                loginInfo.UserName = SanitizeInput(loginInfo.UserName);

                // 检查失败次数
                var failedAttemptKey = $"login:failed:{loginInfo.UserName}";
                var failedAttemptsStr = _cache.Get(failedAttemptKey);
                var failedAttempts = 0;
                if (!string.IsNullOrEmpty(failedAttemptsStr))
                {
                    int.TryParse(failedAttemptsStr, out failedAttempts);
                }

                if (failedAttempts >= 5)
                {
                    Log.Warning("登录尝试次数过多被锁定 - IP: {ClientIp}, 用户名: {UserName}, RequestId: {RequestId}",
                        clientIp, loginInfo.UserName, requestId);
                    return Json(new WebResponseContent().Error("账户已被锁定，请30分钟后再试"));
                }

                var result = await Service.Login(loginInfo);

                if (result.Status)
                {
                    _cache.Remove(failedAttemptKey);
                    Log.Information("用户登录成功 - IP: {ClientIp}, 用户名: {UserName}, RequestId: {RequestId}",
                        clientIp, loginInfo.UserName, requestId);
                }
                else
                {
                    // 增加失败次数
                    _cache.Add(failedAttemptKey, (failedAttempts + 1).ToString());
                    Log.Warning("用户登录失败 - IP: {ClientIp}, 用户名: {UserName}, 尝试次数: {Attempts}, RequestId: {RequestId}",
                        clientIp, loginInfo.UserName, failedAttempts + 1, requestId);
                }

                return Json(result);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "登录异常 - IP: {ClientIp}, 用户名: {UserName}, RequestId: {RequestId}",
                    clientIp, loginInfo?.UserName, requestId);
                return Json(new WebResponseContent().Error("登录失败，请稍后重试"));
            }
        }

        /// <summary>
        /// 替换Token
        /// </summary>
        [HttpPost, Route("replaceToken")]
        public IActionResult ReplaceToken()
        {
            WebResponseContent responseContent = new WebResponseContent();
            string error = "";
            string key = $"rp:Token:{UserContext.Current.UserId}";
            UserInfo userInfo = null;
            var clientIp = GetClientIpAddress();
            var requestId = Guid.NewGuid().ToString("N");

            try
            {
                // 如果5秒内替换过token，直接使用最新的token
                if (_cache.Exists(key))
                {
                    Log.Information("使用缓存的Token - UserId: {UserId}, IP: {ClientIp}, RequestId: {RequestId}",
                        UserContext.Current.UserId, clientIp, requestId);
                    return Json(responseContent.OK(null, _cache.Get(key)));
                }

                var _obj = _lockCurrent.GetOrAdd(UserContext.Current.UserId, new object() { });
                lock (_obj)
                {
                    // 双重检查
                    if (_cache.Exists(key))
                    {
                        return Json(responseContent.OK(null, _cache.Get(key)));
                    }

                    string requestToken = HttpContext.Request.Headers[AppSetting.TokenHeaderName];
                    requestToken = requestToken?.Replace("Bearer ", "");

                    if (JwtHelper.IsExp(requestToken))
                    {
                        Log.Warning("Token已过期 - UserId: {UserId}, IP: {ClientIp}, RequestId: {RequestId}",
                            UserContext.Current.UserId, clientIp, requestId);
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
                        Log.Error("未找到用户信息 - UserId: {UserId}, IP: {ClientIp}, RequestId: {RequestId}",
                            userId, clientIp, requestId);
                        return Json(responseContent.Error("未查到用户信息!"));
                    }

                    string token = JwtHelper.IssueJwt(userInfo);

                    // 移除当前缓存
                    _cache.Remove(userId.GetUserIdKey());

                    // 更新token
                    _userRepository.Update(new Sys_User() { User_Id = userId, Token = token }, x => x.Token, true);

                    // 添加一个5秒缓存，防止并发
                    _cache.Add(key, token);

                    Log.Information("Token替换成功 - UserId: {UserId}, UserName: {UserName}, IP: {ClientIp}, RequestId: {RequestId}",
                        userId, userInfo.UserName, clientIp, requestId);

                    responseContent.OK(null, token);
                }
            }
            catch (Exception ex)
            {
                error = ex.Message + ex.StackTrace;
                Log.Error(ex, "Token替换异常 - UserId: {UserId}, IP: {ClientIp}, RequestId: {RequestId}",
                    UserContext.Current.UserId, clientIp, requestId);
                responseContent.Error("token替换异常");
            }
            finally
            {
                _lockCurrent.TryRemove(UserContext.Current.UserId, out object val);
            }
            return Json(responseContent);
        }

        /// <summary>
        /// 修改当前用户密码
        /// </summary>
        [HttpPost, Route("modifyPwd")]
        [ApiActionPermission]
        public async Task<IActionResult> ModifyPwd([FromBody] Dictionary<string, string> info)
        {
            var userId = UserContext.Current.UserId;
            var clientIp = GetClientIpAddress();
            var requestId = Guid.NewGuid().ToString("N");

            try
            {
                var oldPwd = info?["oldPwd"];
                var newPwd = info?["newPwd"];

                // 验证参数
                if (string.IsNullOrWhiteSpace(oldPwd) || string.IsNullOrWhiteSpace(newPwd))
                {
                    Log.Warning("修改密码参数不完整 - UserId: {UserId}, IP: {ClientIp}, RequestId: {RequestId}",
                        userId, clientIp, requestId);
                    return Json(new WebResponseContent().Error("新旧密码不能为空"));
                }

                // 验证新密码复杂度
                if (!ValidatePasswordComplexity(newPwd, out var passwordError))
                {
                    Log.Warning("新密码不符合复杂度要求 - UserId: {UserId}, IP: {ClientIp}, Error: {Error}, RequestId: {RequestId}",
                        userId, clientIp, passwordError, requestId);
                    return Json(new WebResponseContent().Error(passwordError));
                }

                var result = await Service.ModifyPwd(oldPwd, newPwd);

                if (result.Status)
                {
                    Log.Information("密码修改成功 - UserId: {UserId}, IP: {ClientIp}, RequestId: {RequestId}",
                        userId, clientIp, requestId);
                }
                else
                {
                    Log.Warning("密码修改失败 - UserId: {UserId}, IP: {ClientIp}, Error: {Error}, RequestId: {RequestId}",
                        userId, clientIp, result.Message, requestId);
                }

                return Json(result);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "修改密码异常 - UserId: {UserId}, IP: {ClientIp}, RequestId: {RequestId}",
                    userId, clientIp, requestId);
                return Json(new WebResponseContent().Error("修改密码失败，请稍后重试"));
            }
        }

        /// <summary>
        /// 获取当前用户信息
        /// </summary>
        [HttpPost, Route("getCurrentUserInfo")]
        public async Task<IActionResult> GetCurrentUserInfo()
        {
            var userId = UserContext.Current.UserId;
            var clientIp = GetClientIpAddress();

            try
            {
                var result = await Service.GetCurrentUserInfo();

                // 敏感信息脱敏
                if (result.Status && result.Data != null)
                {
                    result.Data = SanitizeUserInfo(result.Data);
                }

                Log.Information("获取用户信息 - UserId: {UserId}, IP: {ClientIp}", userId, clientIp);

                return Json(result);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "获取用户信息异常 - UserId: {UserId}, IP: {ClientIp}", userId, clientIp);
                return Json(new WebResponseContent().Error("获取用户信息失败"));
            }
        }

        /// <summary>
        /// 管理员修改用户密码
        /// </summary>
        [HttpPost, Route("modifyUserPwd")]
        [ApiActionPermission(ActionPermissionOptions.Add | ActionPermissionOptions.Update)]
        public IActionResult ModifyUserPwd([FromBody] LoginInfo loginInfo)
        {
            string userName = loginInfo?.UserName;
            string password = loginInfo?.Password;
            WebResponseContent webResponse = new WebResponseContent();
            var adminId = UserContext.Current.UserId;
            var adminName = UserContext.Current.UserName;
            var clientIp = GetClientIpAddress();
            var requestId = Guid.NewGuid().ToString("N");

            try
            {
                if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(userName))
                {
                    return Json(webResponse.Error("参数不完整"));
                }

                // XSS防护
                userName = SanitizeInput(userName);

                // 验证密码复杂度
                if (!ValidatePasswordComplexity(password, out var passwordError))
                {
                    Log.Warning("管理员修改密码失败，密码不符合要求 - AdminId: {AdminId}, TargetUser: {UserName}, IP: {ClientIp}, RequestId: {RequestId}",
                        adminId, userName, clientIp, requestId);
                    return Json(webResponse.Error(passwordError));
                }

                Sys_User user = _userRepository.FindFirst(x => x.UserName == userName);
                if (user == null)
                {
                    Log.Warning("管理员修改密码失败，用户不存在 - AdminId: {AdminId}, TargetUser: {UserName}, IP: {ClientIp}, RequestId: {RequestId}",
                        adminId, userName, clientIp, requestId);
                    return Json(webResponse.Error("用户不存在"));
                }

                user.UserPwd = password.EncryptDES(AppSetting.Secret.User);
                _userRepository.Update(user, x => new { x.UserPwd }, true);

                // 如果用户在线，强制下线
                UserContext.Current.LogOut(user.User_Id);

                Log.Information("管理员修改用户密码成功 - AdminId: {AdminId}, AdminName: {AdminName}, TargetUserId: {UserId}, TargetUserName: {UserName}, IP: {ClientIp}, RequestId: {RequestId}",
                    adminId, adminName, user.User_Id, user.UserName, clientIp, requestId);

                return Json(webResponse.OK("密码修改成功"));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "管理员修改密码异常 - AdminId: {AdminId}, TargetUser: {UserName}, IP: {ClientIp}, RequestId: {RequestId}",
                    adminId, userName, clientIp, requestId);
                return Json(webResponse.Error("密码修改失败，请稍后重试"));
            }
        }

        /// <summary>
        /// 获取验证码
        /// </summary>
        [HttpGet, Route("getVierificationCode"), AllowAnonymous]
        public IActionResult GetVierificationCode()
        {
            var clientIp = GetClientIpAddress();
            var uuid = Guid.NewGuid();

            try
            {
                // IP限制检查，防止恶意请求
                var ipKey = $"verification:ip:{clientIp}";
                var ipCountStr = _cache.Get(ipKey);
                var ipCount = 0;
                if (!string.IsNullOrEmpty(ipCountStr))
                {
                    int.TryParse(ipCountStr, out ipCount);
                }

                if (ipCount > 10)
                {
                    Log.Warning("验证码请求过于频繁 - IP: {ClientIp}", clientIp);
                    return Json(new { error = "请求过于频繁，请稍后再试" });
                }

                string code = VierificationCode.RandomText();
                var data = new
                {
                    img = VierificationCode.CreateBase64Imgage(code),
                    uuid = uuid
                };

                HttpContext.GetService<IMemoryCache>().Set(uuid.ToString(), code, new TimeSpan(0, 5, 0));
                _cache.Add(ipKey, (ipCount + 1).ToString());

                Log.Information("生成验证码 - IP: {ClientIp}, UUID: {UUID}", clientIp, uuid);

                return Json(data);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "生成验证码异常 - IP: {ClientIp}", clientIp);
                return Json(new { error = "生成验证码失败" });
            }
        }

        /// <summary>
        /// 文件上传
        /// </summary>
        [ApiActionPermission()]
        public override IActionResult Upload(IEnumerable<IFormFile> fileInput)
        {
            var userId = UserContext.Current.UserId;
            var clientIp = GetClientIpAddress();

            try
            {
                // 文件验证
                if (fileInput == null || !fileInput.Any())
                {
                    return Json(new WebResponseContent().Error("请选择文件"));
                }

                // 文件类型和大小验证
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".xls", ".xlsx" };
                var maxFileSize = 10 * 1024 * 1024; // 10MB

                foreach (var file in fileInput)
                {
                    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(extension))
                    {
                        Log.Warning("文件类型不允许 - UserId: {UserId}, FileName: {FileName}, IP: {ClientIp}",
                            userId, file.FileName, clientIp);
                        return Json(new WebResponseContent().Error($"不允许上传{extension}类型的文件"));
                    }

                    if (file.Length > maxFileSize)
                    {
                        Log.Warning("文件过大 - UserId: {UserId}, FileName: {FileName}, Size: {Size}, IP: {ClientIp}",
                            userId, file.FileName, file.Length, clientIp);
                        return Json(new WebResponseContent().Error("文件大小不能超过10MB"));
                    }
                }

                Log.Information("文件上传 - UserId: {UserId}, FileCount: {Count}, IP: {ClientIp}",
                    userId, fileInput.Count(), clientIp);

                return base.Upload(fileInput);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "文件上传异常 - UserId: {UserId}, IP: {ClientIp}", userId, clientIp);
                return Json(new WebResponseContent().Error("文件上传失败"));
            }
        }

        /// <summary>
        /// 更新用户信息
        /// </summary>
        [HttpPost, Route("updateUserInfo")]
        public IActionResult UpdateUserInfo([FromBody] Sys_User user)
        {
            var clientIp = GetClientIpAddress();
            var currentUserId = UserContext.Current.UserId;
            var requestId = Guid.NewGuid().ToString("N");

            try
            {
                if (user == null)
                {
                    return Content("参数不能为空");
                }

                // 防止修改其他用户信息
                user.User_Id = currentUserId;

                // XSS防护 - 清理输入
                user.UserTrueName = SanitizeInput(user.UserTrueName);
                user.Remark = SanitizeInput(user.Remark);
                user.HeadImageUrl = SanitizeInput(user.HeadImageUrl);

                // 验证性别值
                if (user.Gender.HasValue && (user.Gender < 0 || user.Gender > 2))
                {
                    Log.Warning("性别参数错误 - UserId: {UserId}, Gender: {Gender}, IP: {ClientIp}, RequestId: {RequestId}",
                        currentUserId, user.Gender, clientIp, requestId);
                    return Content("性别参数错误");
                }

                // 使用锁防止并发更新同一用户信息
                var _obj = _lockCurrent.GetOrAdd(currentUserId, new object() { });
                lock (_obj)
                {
                    _userRepository.Update(user, x => new { x.UserTrueName, x.Gender, x.Remark, x.HeadImageUrl }, true);
                }

                Log.Information("用户信息更新成功 - UserId: {UserId}, IP: {ClientIp}, RequestId: {RequestId}",
                    currentUserId, clientIp, requestId);

                return Content("修改成功");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "更新用户信息异常 - UserId: {UserId}, IP: {ClientIp}, RequestId: {RequestId}",
                    currentUserId, clientIp, requestId);
                return Content("更新失败，请稍后重试");
            }
            finally
            {
                _lockCurrent.TryRemove(currentUserId, out object val);
            }
        }

        #region 辅助方法

        /// <summary>
        /// 获取客户端IP地址
        /// </summary>
        private string GetClientIpAddress()
        {
            try
            {
                // 检查各种代理头
                var headers = new[] { "X-Forwarded-For", "X-Real-IP", "CF-Connecting-IP", "X-Original-For" };
                foreach (var header in headers)
                {
                    if (HttpContext.Request.Headers.TryGetValue(header, out var value))
                    {
                        var ip = value.ToString().Split(',').FirstOrDefault()?.Trim();
                        if (!string.IsNullOrEmpty(ip) && IsValidIpAddress(ip))
                        {
                            return ip;
                        }
                    }
                }

                return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        /// <summary>
        /// 验证IP地址格式
        /// </summary>
        private bool IsValidIpAddress(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip)) return false;

            // 简单的IP格式验证
            var parts = ip.Split('.');
            if (parts.Length != 4) return false;

            foreach (var part in parts)
            {
                if (!int.TryParse(part, out var num) || num < 0 || num > 255)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 验证密码复杂度
        /// </summary>
        private bool ValidatePasswordComplexity(string password, out string error)
        {
            error = null;

            if (string.IsNullOrWhiteSpace(password))
            {
                error = "密码不能为空";
                return false;
            }

            if (password.Length < 8)
            {
                error = "密码长度不能少于8位";
                return false;
            }

            //if (!PasswordRegex.IsMatch(password))
            //{
            //    error = "密码必须包含大小写字母和数字";
            //    return false;
            //}

            // 检查是否包含常见弱密码
            var weakPasswords = new[] { "12345678", "password", "Password1", "admin123", "Admin123" };
            if (weakPasswords.Any(wp => password.IndexOf(wp, StringComparison.OrdinalIgnoreCase) >= 0))
            {
                error = "密码过于简单，请使用更复杂的密码";
                return false;
            }

            return true;
        }

        /// <summary>
        /// 清理输入，防止XSS攻击
        /// </summary>
        private string SanitizeInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;

            // 移除危险字符和标签
            input = Regex.Replace(input, @"<[^>]*>", string.Empty);
            input = input.Replace("&", "&amp;")
                         .Replace("<", "&lt;")
                         .Replace(">", "&gt;")
                         .Replace("\"", "&quot;")
                         .Replace("'", "&#x27;")
                         .Replace("/", "&#x2F;");

            // 限制长度，防止过长输入
            if (input.Length > 200)
            {
                input = input.Substring(0, 200);
            }

            return input.Trim();
        }

        /// <summary>
        /// 敏感信息脱敏
        /// </summary>
        private dynamic SanitizeUserInfo(dynamic userInfo)
        {
            if (userInfo == null) return userInfo;

            try
            {
                // 将动态对象转换为字典处理
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(userInfo);
                var dict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

                // 身份证脱敏
                if (dict.ContainsKey("IdCard") && dict["IdCard"] != null)
                {
                    var idCard = dict["IdCard"].ToString();
                    if (idCard.Length >= 15)
                    {
                        dict["IdCard"] = $"{idCard.Substring(0, 6)}****{idCard.Substring(idCard.Length - 4)}";
                    }
                }

                // 手机号脱敏
                if (dict.ContainsKey("Phone") && dict["Phone"] != null)
                {
                    var phone = dict["Phone"].ToString();
                    if (phone.Length >= 11)
                    {
                        dict["Phone"] = $"{phone.Substring(0, 3)}****{phone.Substring(7)}";
                    }
                }

                // 邮箱脱敏
                if (dict.ContainsKey("Email") && dict["Email"] != null)
                {
                    var email = dict["Email"].ToString();
                    var atIndex = email.IndexOf('@');
                    if (atIndex > 2)
                    {
                        dict["Email"] = $"{email.Substring(0, 2)}***{email.Substring(atIndex)}";
                    }
                }

                // 银行卡号脱敏
                if (dict.ContainsKey("BankCard") && dict["BankCard"] != null)
                {
                    var bankCard = dict["BankCard"].ToString();
                    if (bankCard.Length >= 16)
                    {
                        dict["BankCard"] = $"{bankCard.Substring(0, 4)}****{bankCard.Substring(bankCard.Length - 4)}";
                    }
                }

                return dict;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "用户信息脱敏失败");
                return userInfo;
            }
        }

        #endregion
    }
}