using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
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
using Microsoft.Extensions.Logging;

namespace VOL.Sys.Controllers
{
    [Route("api/role")]
    public partial class Sys_RoleController
    {
        private readonly ISys_RoleService _service;//访问业务代码
        private readonly ISys_RoleRepository _repository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<Sys_RoleController> _logger;

        [ActivatorUtilitiesConstructor]
        public Sys_RoleController(
            ISys_RoleService service,
            ISys_RoleRepository repository,
            IHttpContextAccessor httpContextAccessor,
            ILogger<Sys_RoleController> logger
        )
        : base(service)
        {
            _service = service;
            _repository = repository;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }


        [HttpPost, Route("getCurrentTreePermission")]
        [ApiActionPermission(ActionPermissionOptions.Search)]
        public async Task<IActionResult> GetCurrentTreePermission()
        {
            _logger.LogInformation("GetCurrentTreePermission called.");
            try
            {
                return Json(await Service.GetCurrentTreePermission());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCurrentTreePermission.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { status = false, message = "An error occurred while fetching current tree permission." });
            }
        }

        [HttpPost, Route("getUserTreePermission")]
        [ApiActionPermission(ActionPermissionOptions.Search)]
        public async Task<IActionResult> GetUserTreePermission(int roleId)
        {
            _logger.LogInformation("GetUserTreePermission called for roleId: {RoleId}", roleId);
            if (roleId <= 0)
            {
                _logger.LogWarning("GetUserTreePermission called with invalid roleId: {RoleId}", roleId);
                return new BadRequestObjectResult(new { status = false, message = "Invalid Role ID." });
            }
            try
            {
                return Json(await Service.GetUserTreePermission(roleId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUserTreePermission for roleId: {RoleId}", roleId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { status = false, message = "An error occurred while fetching user tree permission." });
            }
        }

        [HttpPost, Route("savePermission")]
        [ApiActionPermission(ActionPermissionOptions.Update)]
        public async Task<IActionResult> SavePermission([FromBody] List<UserPermissions> userPermissions, int roleId)
        {
            _logger.LogInformation("SavePermission called for roleId: {RoleId}, with {PermissionCount} permissions.", roleId, userPermissions?.Count ?? 0);
            if (roleId <= 0)
            {
                _logger.LogWarning("SavePermission called with invalid roleId: {RoleId}", roleId);
                return new BadRequestObjectResult(new { status = false, message = "Invalid Role ID." });
            }
            if (userPermissions == null)
            {
                _logger.LogWarning("SavePermission called with null userPermissions for roleId: {RoleId}", roleId);
                return new BadRequestObjectResult(new { status = false, message = "User permissions data cannot be null." });
            }
            try
            {
                return Json(await Service.SavePermission(userPermissions, roleId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SavePermission for roleId: {RoleId}", roleId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { status = false, message = "An error occurred while saving permissions." });
            }
        }

        /// <summary>
        /// 获取当前角色下的所有角色 
        /// </summary>
        /// <returns></returns>

        [HttpPost, Route("getUserChildRoles")]
        [ApiActionPermission(ActionPermissionOptions.Search)]
        public IActionResult GetUserChildRoles()
        {
            _logger.LogInformation("GetUserChildRoles called for current user's RoleId: {RoleId}", UserContext.Current.RoleId);
            try
            {
                int roleId = UserContext.Current.RoleId;
                var data = RoleContext.GetAllChildren(roleId);

                if (UserContext.Current.IsSuperAdmin)
                {
                    return Json(WebResponseContent.Instance.OK(null, data));
                }
                //不是超级管理，将自己的角色查出来，在树形菜单上作为根节点
                var self = _repository.FindAsIQueryable(x => x.Role_Id == roleId)
                     .Select(s => new VOL.Core.UserManager.RoleNodes()
                     {
                         Id = s.Role_Id,
                         ParentId = 0,//将自己的角色作为root节点
                         RoleName = s.RoleName
                     }).ToList();
                data.AddRange(self);
                return Json(WebResponseContent.Instance.OK(null, data));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUserChildRoles for RoleId: {RoleId}", UserContext.Current.RoleId);
                return Json(WebResponseContent.Instance.Error("An error occurred while fetching user child roles."));
            }
        }



        /// <summary>
        /// treetable 获取子节点数据(2021.05.02)
        /// </summary>
        /// <param name="loadData"></param>
        /// <returns></returns>
        [ApiActionPermission(ActionPermissionOptions.Search)]
        [HttpPost, Route("GetPageData")]
        public override ActionResult GetPageData([FromBody] PageDataOptions loadData)
        {
            _logger.LogInformation("GetPageData (Sys_RoleController) called with Value: {Value}, Page: {Page}, Rows: {Rows}", loadData?.Value, loadData?.Page, loadData?.Rows);
            if (loadData == null)
            {
                _logger.LogWarning("GetPageData (Sys_RoleController) called with null loadData.");
                return new BadRequestObjectResult(new { status = false, message = "Load data cannot be null." });
            }
            try
            {
                //获取根节点数据(对应Sys_Role1.js中searchBefore方法)
                if (loadData.Value.GetInt() == 1)
                {
                    _logger.LogDebug("GetPageData routing to GetTreeTableRootData.");
                    return GetTreeTableRootData(loadData).Result;
                }
                _logger.LogDebug("GetPageData routing to base.GetPageData.");
                return base.GetPageData(loadData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPageData (Sys_RoleController). LoadData Value: {Value}", loadData.Value);
                return StatusCode(StatusCodes.Status500InternalServerError, new { status = false, message = "An error occurred while processing page data." });
            }
        }

        /// <summary>
        /// treetable 获取子节点数据(2021.05.02)
        /// </summary>
        /// <returns></returns>
        [HttpPost, Route("getTreeTableRootData")]
        [ApiActionPermission(ActionPermissionOptions.Search)]
        public async Task<ActionResult> GetTreeTableRootData([FromBody] PageDataOptions options)
        {
            _logger.LogInformation("GetTreeTableRootData (Sys_RoleController) called with Page: {Page}, Rows: {Rows}, Value: {Value}", options?.Page, options?.Rows, options?.Value);
            if (options == null)
            {
                _logger.LogWarning("GetTreeTableRootData (Sys_RoleController) called with null options.");
                return new BadRequestObjectResult(new { status = false, message = "Options cannot be null." });
            }
            try
            {
                //页面加载根节点数据条件x => x.ParentId == 0,自己根据需要设置
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
                var queryChild = _repository.FindAsIQueryable(x => true);
                var rows = await query.TakeOrderByPage(options.Page, options.Rows)
                    .OrderBy(x => x.Role_Id).Select(s => new
                    {
                        s.Role_Id,
                        //   ParentId=0,
                        s.ParentId,
                        s.RoleName,
                        s.DeptName,
                        s.Dept_Id,
                        s.Enable,
                        s.CreateDate,
                        s.Creator,
                        s.Modifier,
                        s.ModifyDate,
                        s.OrderNo,
                        hasChildren = queryChild.Any(x => x.ParentId == s.Role_Id)
                    }).ToListAsync();
                return JsonNormal(new { total = await query.CountAsync(), rows });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetTreeTableRootData (Sys_RoleController). Options: Page {Page}, Rows {Rows}, Value {Value}", options.Page, options.Rows, options.Value);
                return StatusCode(StatusCodes.Status500InternalServerError, new { status = false, message = "An error occurred while fetching root role data." });
            }
        }

        /// <summary>
        ///treetable 获取子节点数据(2021.05.02)
        /// </summary>
        /// <returns></returns>
        [HttpPost, Route("getTreeTableChildrenData")]
        [ApiActionPermission(ActionPermissionOptions.Search)]
        public async Task<ActionResult> GetTreeTableChildrenData(int roleId)
        {
            _logger.LogInformation("GetTreeTableChildrenData (Sys_RoleController) called for roleId: {RoleId}", roleId);
            if (roleId <= 0)
            {
                _logger.LogWarning("GetTreeTableChildrenData (Sys_RoleController) called with invalid roleId: {RoleId}", roleId);
                return new BadRequestObjectResult(new { status = false, message = "Invalid Role ID." });
            }
            try
            {
                if (!UserContext.Current.IsSuperAdmin && roleId != UserContext.Current.RoleId && !RoleContext.GetAllChildren(UserContext.Current.RoleId).Any(x => x.Id == roleId))
                {
                    _logger.LogInformation("GetTreeTableChildrenData access condition not met for roleId {RoleId}, returning empty rows.", roleId);
                    return JsonNormal(new { rows = new object[] { } });
                }
                //点击节点时，加载子节点数据
                var roleRepository = Sys_RoleRepository.Instance.FindAsIQueryable(x => true); // Consider replacing Sys_RoleRepository.Instance with _repository
                var rows = await roleRepository.Where(x => x.ParentId == roleId)
                    .Select(s => new
                    {
                        s.Role_Id,
                        s.ParentId,
                        s.RoleName,
                        s.DeptName,
                        s.Dept_Id,
                        s.Enable,
                        s.CreateDate,
                        s.Creator,
                        s.Modifier,
                        s.ModifyDate,
                        s.OrderNo,
                        hasChildren = roleRepository.Any(x => x.ParentId == s.Role_Id)
                    }).ToListAsync();
                return JsonNormal(new { rows });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetTreeTableChildrenData (Sys_RoleController) for roleId {RoleId}.", roleId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { status = false, message = "An error occurred while fetching children role data." });
            }
        }

    }
}


