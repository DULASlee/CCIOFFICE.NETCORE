/*
 *接口编写处...
*如果接口需要做Action的权限验证，请在Action上使用属性
*如: [ApiActionPermission("Sys_WorkFlow",Enums.ActionPermissionOptions.Search)]
 */
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using VOL.Entity.DomainModels;
using VOL.Sys.IServices;
using VOL.Core.WorkFlow;
using VOL.Sys.IRepositories;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using VOL.Core.ManageUser;
using VOL.Core.Services;
using VOL.Core.Infrastructure;
using VOL.Core.DBManager;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using VOL.Core.Extensions;
using VOL.Core.UserManager;

namespace VOL.Sys.Controllers
{
    public partial class Sys_WorkFlowController
    {
        private readonly ISys_WorkFlowService _service;//访问业务代码
        private readonly ISys_UserRepository _userRepository;
        private readonly ISys_RoleRepository _roleRepository;
        private readonly ISys_DepartmentRepository _departmentRepository;
        private readonly ISys_WorkFlowRepository _workFlowRepository;
        private readonly ISys_WorkFlowTableRepository _workFlowTableRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<Sys_WorkFlowController> _logger;

        [ActivatorUtilitiesConstructor]
        public Sys_WorkFlowController(
            ISys_WorkFlowService service,
            ISys_UserRepository userRepository,
            ISys_RoleRepository roleRepository,
            ISys_WorkFlowRepository workFlowRepository,
            ISys_WorkFlowTableRepository workFlowTableRepository,
            IHttpContextAccessor httpContextAccessor,
            ISys_DepartmentRepository departmentRepository,
            ILogger<Sys_WorkFlowController> logger
        )
        : base(service)
        {
            _service = service;
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _departmentRepository = departmentRepository;
            _workFlowRepository = workFlowRepository;
            _workFlowTableRepository = workFlowTableRepository;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }
        /// <summary>
        /// 获取工作流程表数据源
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("getTableInfo")]
        public IActionResult GetTableInfo()
        {
            _logger.LogInformation("GetTableInfo called.");
            try
            {
                return Json(WorkFlowContainer.GetDic());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetTableInfo.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { status = false, message = "An error occurred while fetching table info." });
            }
        }
        /// <summary>
        /// 获取流程节点数据源(用户与角色)
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("getNodeDic")]
        public async Task<IActionResult> GetNodeDic()
        {
            _logger.LogInformation("GetNodeDic called.");
            try
            {
                var data = new
                {
                    users = await _userRepository.FindAsIQueryable(x => true).Select(s => new { key = s.User_Id, value = s.UserTrueName }).Take(5000).ToListAsync(),
                    roles = await _roleRepository.FindAsIQueryable(x => true).Select(s => new { key = s.Role_Id, value = s.RoleName }).ToListAsync(),
                    dept = await _departmentRepository.FindAsIQueryable(x => true).Select(s => new { key = s.DepartmentId, value = s.DepartmentName }).ToListAsync(),
                };
                return Json(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetNodeDic.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { status = false, message = "An error occurred while fetching node dictionary." });
            }
        }
        /// <summary>
        /// 获取单据的审批流程进度
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost, Route("getSteps")]
        public async Task<IActionResult> GetSteps([FromBody] List<string> ids, string tableName)
        {
            _logger.LogInformation("GetSteps called with {IdCount} IDs for tableName: {TableName}", ids?.Count ?? 0, tableName);

            if (ids == null || !ids.Any())
            {
                _logger.LogWarning("GetSteps called with null or empty IDs.");
                return new BadRequestObjectResult(new { status = false, message = "IDs list cannot be empty." });
            }
            if (string.IsNullOrEmpty(tableName))
            {
                _logger.LogWarning("GetSteps called with null or empty tableName.");
                return new BadRequestObjectResult(new { status = false, message = "Table name cannot be empty." });
            }

            try
            {
                var flows = await _workFlowTableRepository.FindAsIQueryable(x => x.WorkTable == tableName && ids.Contains(x.WorkTableKey))
                                   .Include(x => x.Sys_WorkFlowTableStep)
                                   .ToListAsync();
                //不在审核中的数据
                if (flows.Count == 0)
                {
                    _logger.LogInformation("No active flows found for IDs: {IdsString}, Table: {TableName}", string.Join(",", ids), tableName);
                    return Json(new { status = true });
                }
                if (flows.Count > 1 || flows.Count != ids.Count)
                {
                    _logger.LogWarning("GetSteps validation: Mismatch in flow count or ID count. Expected 1 for IDs: {IdsString}, Table: {TableName}", string.Join(",", ids), tableName);
                    return Json(new { status = false, message = "只能选择一条数据进行审核" });
                }

                var flow = flows[0];
                var user = UserContext.Current.UserInfo;

                var unauditSteps = flow.Sys_WorkFlowTableStep
                    .Where(x => (x.AuditId == null || x.AuditId == 0) && x.StepType == (int)AuditType.用户审批)
                    .Select(s => new { s.Sys_WorkFlowTableStep_Id, userIds = s.StepValue.Split(",").Select(s => s.GetInt()) }
                    ).ToList();

                var unauditUsers = unauditSteps.SelectMany(c => c.userIds).ToList();
                List<(int userId, string userName)> userInfo = new List<(int userId, string userName)>();
                if (unauditUsers.Count > 0)
                {
                    userInfo = (await _userRepository.FindAsIQueryable(x => unauditUsers.Contains(x.User_Id))
                                            .Select(u => new { u.User_Id, u.UserTrueName }).ToListAsync())
                                            .Select(c => (c.User_Id, c.UserTrueName)).ToList();
                }

                var log = await _workFlowTableRepository.DbContext.Set<Sys_WorkFlowTableAuditLog>()
                      .Where(x => x.WorkFlowTable_Id == flow.WorkFlowTable_Id)
                      .OrderBy(x => x.CreateDate)
                      .ToListAsync();

                string GetAuditUsers(Sys_WorkFlowTableStep step)
                {
                    if (step.StepType == (int)AuditType.角色审批)
                    {
                        int roleId = step.StepValue.GetInt();
                        return RoleContext.GetAllRoleId().Where(c => c.Id == roleId).Select(c => c.RoleName).FirstOrDefault();
                    }
                    if (step.StepType == (int)AuditType.部门审批)
                    {
                        var deptId = step.StepValue.GetGuid();
                        return DepartmentContext.GetAllDept().Where(c => c.id == deptId).Select(c => c.value).FirstOrDefault();
                    }
                    var userIds = unauditSteps.Where(c => c.Sys_WorkFlowTableStep_Id == step.Sys_WorkFlowTableStep_Id)
                          .Select(c => c.userIds).FirstOrDefault();
                    if (userIds == null)
                    {
                        return "";
                    }
                    return string.Join("/", userInfo.Where(c => userIds.Contains(c.userId)).Select(s => s.userName));
                }

                var steps = flow.Sys_WorkFlowTableStep
                        .Select(c => new
                        {
                            c.AuditId,
                            Auditor = c.Auditor ?? GetAuditUsers(c),
                            c.AuditDate,
                            c.AuditStatus,
                            c.Remark,
                            c.StepValue,
                            c.StepName,
                            c.OrderId,
                            c.Enable,
                            c.StepId,
                            c.StepAttrType,
                            c.CreateDate,
                            c.Creator,
                            isCurrentUser = (c.AuditStatus == null || c.AuditStatus == (int)AuditStatus.审核中 || c.AuditStatus == (int)AuditStatus.待审核)
                                            && c.StepId == flow.CurrentStepId && GetAuditStepValue(c),
                            isCurrent = c.StepId == flow.CurrentStepId && c.AuditStatus != (int)AuditStatus.审核通过
                        }).OrderBy(o => o.OrderId);

                object form = await WorkFlowManager.GetAuditFormDataAsync(ids[0], tableName);

                var data = new
                {
                    status = true,
                    step = flow.CurrentStepId,
                    flow.AuditStatus,
                    auditDic = DictionaryManager.GetDictionary("audit")?.Sys_DictionaryList?.Select(s => new { key = s.DicValue, value = s.DicName }),
                    list = steps.OrderBy(x => x.OrderId).ToList(),
                    log = log,
                    form
                };
                return Json(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetSteps for IDs: {IdsString}, TableName: {TableName}", string.Join(",", ids), tableName);
                return StatusCode(StatusCodes.Status500InternalServerError, new { status = false, message = "An error occurred while fetching workflow steps." });
            }
        }
        [HttpPost, Route("getFields")]
        public async Task<IActionResult> GetFields(string table)
        {
            _logger.LogInformation("GetFields called for table: {TableName}", table);
            if (string.IsNullOrEmpty(table))
            {
                _logger.LogWarning("GetFields called with null or empty table name.");
                return new BadRequestObjectResult(new { status = false, message = "Table name cannot be empty." });
            }
            try
            {
                var query = _workFlowTableRepository.DbContext.Set<Sys_TableColumn>().Where(c => c.TableName == table);
                var fields = WorkFlowContainer.GetFilterFields(table);
                if (fields != null && fields.Length > 0)
                {
                    query = query.Where(x => fields.Contains(x.ColumnName));
                }
                else
                {
                    query = query.Where(x => x.IsDisplay == 1);
                }
                var columns = await query.OrderByDescending(c => c.OrderNo)
                     .Select(s => new
                     {
                         field = s.ColumnName,
                         name = s.ColumnCnName,
                         dicNo = s.DropNo,
                         s.ColumnType
                     }).ToListAsync();

                var data = columns.Select(s => new
                {
                    s.field,
                    s.name,
                    s.dicNo,
                    columnType = s.ColumnType,
                    data = string.IsNullOrEmpty(s.dicNo)
                    ? null
                    : DictionaryManager.GetDictionary(s.dicNo)?.Sys_DictionaryList?.Select(c => new { key = c.DicValue, value = c.DicName })?.ToList()
                }).ToList();
                return JsonNormal(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetFields for table: {TableName}", table);
                return StatusCode(StatusCodes.Status500InternalServerError, new { status = false, message = "An error occurred while fetching fields." });
            }
        }

        private bool GetAuditStepValue(Sys_WorkFlowTableStep flow)
        {
            if (flow.StepType == (int)AuditType.角色审批)
            {
                return UserContext.Current.RoleId.ToString() == flow.StepValue;
            }
            //按部门审批
            if (flow.StepType == (int)AuditType.部门审批)
            {
                return UserContext.Current.UserInfo.DeptIds.Select(s => s.ToString()).Contains(flow.StepValue);
            }
            //按用户审批
            //return UserContext.Current.UserId.ToString() == flow.StepValue;
            return flow.StepValue.Split(",").Contains(UserContext.Current.UserId.ToString());

        }
        [Route("getOptions"), HttpGet]
        public async Task<IActionResult> GetOptions(Guid id)
        {
            _logger.LogInformation("GetOptions called for id: {WorkflowId}", id);
            if (id == Guid.Empty)
            {
                _logger.LogWarning("GetOptions called with empty Guid.");
                return new BadRequestObjectResult(new { status = false, message = "Workflow ID cannot be empty." });
            }
            try
            {
                var data = await _workFlowRepository.FindAsIQueryable(x => x.WorkFlow_Id == id)
                    .Include(c => c.Sys_WorkFlowStep)
                    .FirstOrDefaultAsync();
                if (data == null)
                {
                    _logger.LogInformation("No workflow options found for id: {WorkflowId}", id);
                    return NotFound(new { status = false, message = "Workflow options not found." });
                }
                return JsonNormal(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetOptions for id: {WorkflowId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { status = false, message = "An error occurred while fetching workflow options." });
            }
        }
    }
}
