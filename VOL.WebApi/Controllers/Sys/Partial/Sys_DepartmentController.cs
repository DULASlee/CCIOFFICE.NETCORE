/*
 *接口编写处...
*如果接口需要做Action的权限验证，请在Action上使用属性
*如: [ApiActionPermission("Sys_Department",Enums.ActionPermissionOptions.Search)]
 */
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using VOL.Entity.DomainModels;
using VOL.Sys.IServices;
using VOL.Core.Filters;
using VOL.Core.Enums;
using VOL.Core.Extensions;
using VOL.Sys.IRepositories;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using VOL.Core.ManageUser;
using VOL.Core.UserManager;
using Microsoft.Extensions.Logging;

namespace VOL.Sys.Controllers
{
    public partial class Sys_DepartmentController
    {
        private readonly ISys_DepartmentService _service;//访问业务代码
        private readonly ISys_DepartmentRepository _repository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<Sys_DepartmentController> _logger;

        [ActivatorUtilitiesConstructor]
        public Sys_DepartmentController(
            ISys_DepartmentService service,
             ISys_DepartmentRepository repository,
            IHttpContextAccessor httpContextAccessor,
            ILogger<Sys_DepartmentController> logger
        )
        : base(service)
        {
            _service = service;
            _repository = repository;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
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
            _logger.LogInformation("GetPageData (Sys_DepartmentController) called with Value: {Value}, Page: {Page}, Rows: {Rows}", loadData?.Value, loadData?.Page, loadData?.Rows);

            if (loadData == null)
            {
                _logger.LogWarning("GetPageData (Sys_DepartmentController) called with null loadData.");
                return new BadRequestObjectResult(new { status = false, message = "Load data cannot be null." });
            }

            try
            {
                return base.GetPageData(loadData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling base.GetPageData in Sys_DepartmentController.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { status = false, message = "An error occurred while processing page data." });
            }
        }

        /// <summary>
        /// treetable 获取子节点数据
        /// </summary>
        /// <returns></returns>
        [HttpPost, Route("getTreeTableRootData")]
        [ApiActionPermission(ActionPermissionOptions.Search)]
        public async Task<ActionResult> GetTreeTableRootData([FromBody] PageDataOptions options)
        {
            _logger.LogInformation("GetTreeTableRootData called with Page: {Page}, Rows: {Rows}, Value: {Value}", options?.Page, options?.Rows, options?.Value);

            if (options == null)
            {
                _logger.LogWarning("GetTreeTableRootData called with null options.");
                return new BadRequestObjectResult(new { status = false, message = "Options cannot be null." });
            }

            try
            {
                //页面加载根节点数据条件x => x.ParentId == 0,自己根据需要设置
                var query = _repository.FindAsIQueryable(x => true);
                if (UserContext.Current.IsSuperAdmin)
                {
                    query = query.Where(x => x.ParentId == null);
                }
                else
                {
                    var deptIds = UserContext.Current.DeptIds;
                    var list = DepartmentContext.GetAllDept().Where(c => deptIds.Contains(c.id)).ToList();
                    deptIds = list.Where(c => !list.Any(x => x.id == c.parentId)).Select(x => x.id).ToList();
                    query = query.Where(x => deptIds.Contains(x.DepartmentId));
                }
                var queryChild = _repository.FindAsIQueryable(x => true);
                var rows = await query.TakeOrderByPage(options.Page, options.Rows)
                    .OrderBy(x => x.DepartmentName).Select(s => new
                    {
                        s.DepartmentId,
                        s.ParentId,
                        s.DepartmentName,
                        s.DepartmentCode,
                        s.Enable,
                        s.Remark,
                        s.CreateDate,
                        s.Creator,
                        s.Modifier,
                        s.ModifyDate,
                        hasChildren = queryChild.Any(x => x.ParentId == s.DepartmentId)
                    }).ToListAsync();
                return JsonNormal(new { total = await query.CountAsync(), rows });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetTreeTableRootData. Options: Page {Page}, Rows {Rows}, Value {Value}", options.Page, options.Rows, options.Value);
                return StatusCode(StatusCodes.Status500InternalServerError, new { status = false, message = "An error occurred while fetching root department data." });
            }
        }

        /// <summary>
        ///treetable 获取子节点数据
        /// </summary>
        /// <returns></returns>
        [HttpPost, Route("getTreeTableChildrenData")]
        [ApiActionPermission(ActionPermissionOptions.Search)]
        public async Task<ActionResult> GetTreeTableChildrenData(Guid departmentId)
        {
            _logger.LogInformation("GetTreeTableChildrenData called for departmentId {DepartmentId}", departmentId);

            if (departmentId == Guid.Empty)
            {
                _logger.LogWarning("GetTreeTableChildrenData called with an empty departmentId.");
                return new BadRequestObjectResult(new { status = false, message = "Department ID cannot be empty." });
            }

            try
            {
                //点击节点时，加载子节点数据
                var query = _repository.FindAsIQueryable(x => true);
                var rows = await query.Where(x => x.ParentId == departmentId)
                    .Select(s => new
                    {
                        s.DepartmentId,
                        s.ParentId,
                        s.DepartmentName,
                        s.DepartmentCode,
                        s.Enable,
                        s.Remark,
                        s.CreateDate,
                        s.Creator,
                        s.Modifier,
                        s.ModifyDate,
                        hasChildren = query.Any(x => x.ParentId == s.DepartmentId)
                    }).ToListAsync();
                return JsonNormal(new { rows });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetTreeTableChildrenData for departmentId {DepartmentId}.", departmentId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { status = false, message = "An error occurred while fetching children department data." });
            }
        }
    }
}

