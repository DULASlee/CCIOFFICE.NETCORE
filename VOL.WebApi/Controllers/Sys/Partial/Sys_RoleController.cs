using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using VOL.Core.Controllers.Basic;
using VOL.Core.Enums;
using VOL.Core.Extensions;
using VOL.Core.Filters;
using VOL.Core.ManageUser;
using VOL.Core.UserManager;
using VOL.Core.Utilities;
using VOL.Entity.AttributeManager;
using VOL.Entity.DomainModels;
using VOL.Sys.IRepositories;
using VOL.Sys.IServices;
using VOL.Sys.Repositories;
using VOL.Sys.Services;

namespace VOL.Sys.Controllers
{
    [Route("api/role")]
    public partial class Sys_RoleController
    {
        private readonly ISys_RoleService _service;
        private readonly ISys_RoleRepository _repository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        // 用于防止并发修改的锁
        private static readonly ConcurrentDictionary<int, SemaphoreSlim> _roleLocks = new();

        [ActivatorUtilitiesConstructor]
        public Sys_RoleController(
            ISys_RoleService service,
            ISys_RoleRepository repository,
            IHttpContextAccessor httpContextAccessor)
            : base(service)
        {
            _service = service;
            _repository = repository;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// 获取当前用户的树形权限
        /// </summary>
        [HttpPost, Route("getCurrentTreePermission")]
        [ApiActionPermission(ActionPermissionOptions.Search)]
        public async Task<IActionResult> GetCurrentTreePermission()
        {
            var userId = UserContext.Current.UserId;
            var clientIp = GetClientIpAddress();
            var requestId = Guid.NewGuid().ToString("N");

            try
            {
                Log.Information("获取当前用户树形权限 - UserId: {UserId}, IP: {ClientIp}, RequestId: {RequestId}",
                    userId, clientIp, requestId);

                var result = await Service.GetCurrentTreePermission();

                if (!result.Status)
                {
                    Log.Warning("获取树形权限失败 - UserId: {UserId}, Error: {Error}, RequestId: {RequestId}",
                        userId, result.Message, requestId);
                }

                return Json(result);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "获取树形权限异常 - UserId: {UserId}, IP: {ClientIp}, RequestId: {RequestId}",
                    userId, clientIp, requestId);
                return Json(new WebResponseContent().Error("获取权限失败，请稍后重试"));
            }
        }

        /// <summary>
        /// 获取指定角色的树形权限
        /// </summary>
        [HttpPost, Route("getUserTreePermission")]
        [ApiActionPermission(ActionPermissionOptions.Search)]
        public async Task<IActionResult> GetUserTreePermission(int roleId)
        {
            var userId = UserContext.Current.UserId;
            var clientIp = GetClientIpAddress();
            var requestId = Guid.NewGuid().ToString("N");

            try
            {
                // 输入验证
                if (roleId <= 0)
                {
                    Log.Warning("获取角色权限参数无效 - RoleId: {RoleId}, UserId: {UserId}, IP: {ClientIp}, RequestId: {RequestId}",
                        roleId, userId, clientIp, requestId);
                    return Json(new WebResponseContent().Error("角色ID无效"));
                }

                // 权限验证：检查是否有权限查看该角色
                if (!UserContext.Current.IsSuperAdmin)
                {
                    var hasPermission = await CheckRolePermissionAsync(roleId);
                    if (!hasPermission)
                    {
                        Log.Warning("无权限查看角色权限 - RoleId: {RoleId}, UserId: {UserId}, IP: {ClientIp}, RequestId: {RequestId}",
                            roleId, userId, clientIp, requestId);
                        return Json(new WebResponseContent().Error("无权限查看该角色"));
                    }
                }

                Log.Information("获取角色树形权限 - RoleId: {RoleId}, UserId: {UserId}, IP: {ClientIp}, RequestId: {RequestId}",
                    roleId, userId, clientIp, requestId);

                var result = await Service.GetUserTreePermission(roleId);

                return Json(result);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "获取角色权限异常 - RoleId: {RoleId}, UserId: {UserId}, IP: {ClientIp}, RequestId: {RequestId}",
                    roleId, userId, clientIp, requestId);
                return Json(new WebResponseContent().Error("获取角色权限失败，请稍后重试"));
            }
        }

        /// <summary>
        /// 保存角色权限
        /// </summary>
        [HttpPost, Route("savePermission")]
        [ApiActionPermission(ActionPermissionOptions.Update)]
        public async Task<IActionResult> SavePermission([FromBody] List<UserPermissions> userPermissions, int roleId)
        {
            var userId = UserContext.Current.UserId;
            var userName = UserContext.Current.UserName;
            var clientIp = GetClientIpAddress();
            var requestId = Guid.NewGuid().ToString("N");

            try
            {
                // 输入验证
                if (roleId <= 0)
                {
                    Log.Warning("保存权限参数无效 - RoleId: {RoleId}, UserId: {UserId}, IP: {ClientIp}, RequestId: {RequestId}",
                        roleId, userId, clientIp, requestId);
                    return Json(new WebResponseContent().Error("角色ID无效"));
                }

                if (userPermissions == null)
                {
                    userPermissions = new List<UserPermissions>();
                }

                // 权限验证
                if (!UserContext.Current.IsSuperAdmin)
                {
                    var hasPermission = await CheckRolePermissionAsync(roleId);
                    if (!hasPermission)
                    {
                        Log.Warning("无权限修改角色权限 - RoleId: {RoleId}, UserId: {UserId}, IP: {ClientIp}, RequestId: {RequestId}",
                            roleId, userId, clientIp, requestId);
                        return Json(new WebResponseContent().Error("无权限修改该角色"));
                    }
                }

                // 使用角色级别的锁防止并发修改
                var semaphore = _roleLocks.GetOrAdd(roleId, _ => new SemaphoreSlim(1, 1));
                await semaphore.WaitAsync();

                try
                {
                    Log.Information("开始保存角色权限 - RoleId: {RoleId}, PermissionCount: {Count}, UserId: {UserId}, UserName: {UserName}, IP: {ClientIp}, RequestId: {RequestId}",
                        roleId, userPermissions.Count, userId, userName, clientIp, requestId);

                    var result = await Service.SavePermission(userPermissions, roleId);

                    if (result.Status)
                    {
                        Log.Information("角色权限保存成功 - RoleId: {RoleId}, UserId: {UserId}, UserName: {UserName}, IP: {ClientIp}, RequestId: {RequestId}",
                            roleId, userId, userName, clientIp, requestId);
                    }
                    else
                    {
                        Log.Warning("角色权限保存失败 - RoleId: {RoleId}, Error: {Error}, UserId: {UserId}, IP: {ClientIp}, RequestId: {RequestId}",
                            roleId, result.Message, userId, clientIp, requestId);
                    }

                    return Json(result);
                }
                finally
                {
                    semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "保存角色权限异常 - RoleId: {RoleId}, UserId: {UserId}, IP: {ClientIp}, RequestId: {RequestId}",
                    roleId, userId, clientIp, requestId);
                return Json(new WebResponseContent().Error("保存权限失败，请稍后重试"));
            }
        }

        /// <summary>
        /// 获取当前角色下的所有子角色
        /// </summary>
        [HttpPost, Route("getUserChildRoles")]
        [ApiActionPermission(ActionPermissionOptions.Search)]
        public async Task<IActionResult> GetUserChildRoles()
        {
            var userId = UserContext.Current.UserId;
            var roleId = UserContext.Current.RoleId;
            var clientIp = GetClientIpAddress();
            var requestId = Guid.NewGuid().ToString("N");

            try
            {
                Log.Information("获取用户子角色 - RoleId: {RoleId}, UserId: {UserId}, IP: {ClientIp}, RequestId: {RequestId}",
                    roleId, userId, clientIp, requestId);

                var data = RoleContext.GetAllChildren(roleId);

                if (!UserContext.Current.IsSuperAdmin)
                {
                    // 不是超级管理员，将自己的角色查出来，在树形菜单上作为根节点
                    var self = await _repository.FindAsIQueryable(x => x.Role_Id == roleId)
                        .Select(s => new VOL.Core.UserManager.RoleNodes()
                        {
                            Id = s.Role_Id,
                            ParentId = 0, // 将自己的角色作为root节点
                            RoleName = s.RoleName
                        }).ToListAsync();

                    data.AddRange(self);
                }

                // 对所有角色名称进行XSS防护
                foreach (var node in data)
                {
                    node.RoleName = SanitizeInput(node.RoleName);
                }

                return Json(WebResponseContent.Instance.OK(null, data));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "获取子角色异常 - RoleId: {RoleId}, UserId: {UserId}, IP: {ClientIp}, RequestId: {RequestId}",
                    roleId, userId, clientIp, requestId);
                return Json(WebResponseContent.Instance.Error("获取子角色失败，请稍后重试"));
            }
        }

        /// <summary>
        /// TreeTable获取分页数据
        /// </summary>
        [ApiActionPermission(ActionPermissionOptions.Search)]
        [HttpPost, Route("GetPageData")]
        public override ActionResult GetPageData([FromBody] PageDataOptions loadData)
        {
            var clientIp = GetClientIpAddress();

            try
            {
                // 输入验证
                if (loadData == null)
                {
                    loadData = new PageDataOptions();
                }

                // 防止SQL注入
                if (!string.IsNullOrEmpty(loadData.Sort))
                {
                    loadData.Sort = SanitizeInput(loadData.Sort);
                }

                Log.Information("获取角色分页数据 - Page: {Page}, Rows: {Rows}, IP: {ClientIp}",
                    loadData.Page, loadData.Rows, clientIp);

                // 获取根节点数据
                if (loadData.Value.GetInt() == 1)
                {
                    return GetTreeTableRootData(loadData).Result;
                }

                return base.GetPageData(loadData);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "获取分页数据异常 - IP: {ClientIp}", clientIp);
                return JsonNormal(new { total = 0, rows = new object[] { } });
            }
        }

        /// <summary>
        /// 获取TreeTable根节点数据
        /// </summary>
        [HttpPost, Route("getTreeTableRootData")]
        [ApiActionPermission(ActionPermissionOptions.Search)]
        public async Task<ActionResult> GetTreeTableRootData([FromBody] PageDataOptions options)
        {
            var userId = UserContext.Current.UserId;
            var clientIp = GetClientIpAddress();

            try
            {
                if (options == null)
                {
                    options = new PageDataOptions { Page = 1, Rows = 30 };
                }

                // 验证分页参数
                options.Page = Math.Max(1, options.Page);
                options.Rows = Math.Min(Math.Max(1, options.Rows), 100); // 限制最大行数

                Log.Information("获取树形表格根数据 - Page: {Page}, Rows: {Rows}, UserId: {UserId}, IP: {ClientIp}",
                    options.Page, options.Rows, userId, clientIp);

                var query = _repository.FindAsIQueryable(x => true);

                if (UserContext.Current.IsSuperAdmin)
                {
                    query = query.Where(x => x.ParentId == 0);
                }
                else
                {
                    int roleId = UserContext.Current.RoleId;
                    query = query.Where(x => x.Role_Id == roleId);
                }

                var total = await query.CountAsync();

                // 第一步：查询角色数据
                var roles = await query
                    .OrderBy(x => x.OrderNo).ThenBy(x => x.Role_Id)
                    .Skip((options.Page - 1) * options.Rows)
                    .Take(options.Rows)
                    .AsNoTracking()
                    .ToListAsync();

                // 第二步：获取所有角色ID
                var roleIds = roles.Select(r => r.Role_Id).ToList();

                // 第三步：查询哪些角色有子节点
                var rolesWithChildren = new List<int>();
                if (roleIds.Any())
                {
                    rolesWithChildren = await _repository.FindAsIQueryable(x => roleIds.Contains(x.ParentId))
                        .Select(x => x.ParentId)
                        .Distinct()
                        .ToListAsync();
                }

                // 第四步：组装结果并进行XSS防护
                var rows = roles.Select(s => new
                {
                    s.Role_Id,
                    s.ParentId,
                    RoleName = SanitizeInput(s.RoleName),
                    DeptName = SanitizeInput(s.DeptName),
                    s.Dept_Id,
                    s.Enable,
                    s.CreateDate,
                    Creator = SanitizeInput(s.Creator),
                    Modifier = SanitizeInput(s.Modifier),
                    s.ModifyDate,
                    s.OrderNo,
                    hasChildren = rolesWithChildren.Contains(s.Role_Id)
                }).ToList();

                return JsonNormal(new { total, rows });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "获取根节点数据异常 - UserId: {UserId}, IP: {ClientIp}", userId, clientIp);
                return JsonNormal(new { total = 0, rows = new object[] { } });
            }
        }

        /// <summary>
        /// 获取TreeTable子节点数据
        /// </summary>
        [HttpPost, Route("getTreeTableChildrenData")]
        [ApiActionPermission(ActionPermissionOptions.Search)]
        public async Task<ActionResult> GetTreeTableChildrenData(int roleId)
        {
            var userId = UserContext.Current.UserId;
            var clientIp = GetClientIpAddress();
            var requestId = Guid.NewGuid().ToString("N");

            try
            {
                // 输入验证
                if (roleId <= 0)
                {
                    Log.Warning("获取子节点参数无效 - RoleId: {RoleId}, UserId: {UserId}, IP: {ClientIp}, RequestId: {RequestId}",
                        roleId, userId, clientIp, requestId);
                    return JsonNormal(new { rows = new object[] { } });
                }

                // 权限验证
                if (!UserContext.Current.IsSuperAdmin &&
                    roleId != UserContext.Current.RoleId &&
                    !RoleContext.GetAllChildren(UserContext.Current.RoleId).Any(x => x.Id == roleId))
                {
                    Log.Warning("无权限查看子节点 - RoleId: {RoleId}, UserId: {UserId}, IP: {ClientIp}, RequestId: {RequestId}",
                        roleId, userId, clientIp, requestId);
                    return JsonNormal(new { rows = new object[] { } });
                }

                Log.Information("获取角色子节点 - RoleId: {RoleId}, UserId: {UserId}, IP: {ClientIp}, RequestId: {RequestId}",
                    roleId, userId, clientIp, requestId);

                // 第一步：查询子角色数据
                var roles = await _repository.FindAsIQueryable(x => x.ParentId == roleId)
                    .OrderBy(x => x.OrderNo).ThenBy(x => x.Role_Id)
                    .AsNoTracking()
                    .ToListAsync();

                // 第二步：获取所有角色ID
                var roleIds = roles.Select(r => r.Role_Id).ToList();

                // 第三步：查询哪些角色有子节点
                var rolesWithChildren = new List<int>();
                if (roleIds.Any())
                {
                    rolesWithChildren = await _repository.FindAsIQueryable(x =>  roleIds.Contains(x.ParentId))
                        .Select(x => x.ParentId)
                        .Distinct()
                        .ToListAsync();
                }

                // 第四步：组装结果并进行XSS防护
                var rows = roles.Select(s => new
                {
                    s.Role_Id,
                    s.ParentId,
                    RoleName = SanitizeInput(s.RoleName),
                    DeptName = SanitizeInput(s.DeptName),
                    s.Dept_Id,
                    s.Enable,
                    s.CreateDate,
                    Creator = SanitizeInput(s.Creator),
                    Modifier = SanitizeInput(s.Modifier),
                    s.ModifyDate,
                    s.OrderNo,
                    hasChildren = rolesWithChildren.Contains(s.Role_Id)
                }).ToList();

                return JsonNormal(new { rows });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "获取子节点数据异常 - RoleId: {RoleId}, UserId: {UserId}, IP: {ClientIp}, RequestId: {RequestId}",
                    roleId, userId, clientIp, requestId);
                return JsonNormal(new { rows = new object[] { } });
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
                var headers = new[] { "X-Forwarded-For", "X-Real-IP", "CF-Connecting-IP" };
                foreach (var header in headers)
                {
                    if (HttpContext.Request.Headers.TryGetValue(header, out var value))
                    {
                        var ip = value.ToString().Split(',').FirstOrDefault()?.Trim();
                        if (!string.IsNullOrEmpty(ip))
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
                         .Replace("'", "&#x27;");

            return input.Trim();
        }

        /// <summary>
        /// 检查用户是否有权限操作指定角色
        /// </summary>
        private async Task<bool> CheckRolePermissionAsync(int roleId)
        {
            try
            {
                // 检查是否是自己的角色
                if (roleId == UserContext.Current.RoleId)
                    return true;

                // 检查是否是子角色
                var childRoles = RoleContext.GetAllChildren(UserContext.Current.RoleId);
                if (childRoles.Any(x => x.Id == roleId))
                    return true;

                // 检查是否是父角色（向上查找）
                var role = await _repository.FindFirstAsync(x => x.Role_Id == roleId);
                if (role != null && role.ParentId == UserContext.Current.RoleId)
                    return true;

                return false;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}