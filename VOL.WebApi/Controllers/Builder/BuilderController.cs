using VOL.Builder.IServices;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using VOL.Core.Filters;
using VOL.Entity.DomainModels;

namespace VOL.WebApi.Controllers.Builder
{
    [JWTAuthorize]
    [Route("/api/Builder")]
    public class BuilderController : Controller
    {
        private ISys_TableInfoService Service;
        public BuilderController(ISys_TableInfoService service)
        {
            Service = service;
        }
        [HttpPost]
        [Route("GetTableTree")]
        //[ApiActionPermission(ActionRolePermission.SuperAdmin)]
        public async Task<ActionResult> GetTableTree()
        {
            try
            {
                (string, string) builderInfo = await Service.GetTableTree();
                return Json(new { list = builderInfo.Item1, nameSpace = builderInfo.Item2, status = true });
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LoggerType.Exception, "BuilderController GetTableTree action error", null, null, ex);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "获取表结构树时发生内部服务器错误。", status = false });
            }
        }

        [Route("CreateVuePage")]
        [ApiActionPermission(ActionRolePermission.SuperAdmin)]
        [HttpPost]
        public ActionResult CreateVuePage([FromBody] Sys_TableInfo sysTableInfo, string vuePath)
        {
            try
            {
                return Content(Service.CreateVuePage(sysTableInfo, vuePath));
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LoggerType.Exception, $"BuilderController CreateVuePage action error: TableName={sysTableInfo?.TableName}, VuePath={vuePath}", sysTableInfo?.Serialize(), null, ex);
                // Content result might not be the best for errors, consider standard JSON error response
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "创建Vue页面时发生内部服务器错误。", status = false });
            }
        }
        [Route("CreateModel")]
        [ApiActionPermission(ActionRolePermission.SuperAdmin)]
        [HttpPost]
        public ActionResult CreateEntityModel([FromBody] Sys_TableInfo tableInfo)
        {
            try
            {
                return Content(Service.CreateEntityModel(tableInfo));
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LoggerType.Exception, $"BuilderController CreateEntityModel action error: TableName={tableInfo?.TableName}", tableInfo?.Serialize(), null, ex);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "创建实体模型时发生内部服务器错误。", status = false });
            }
        }
        [Route("Save")]
        [ApiActionPermission(ActionRolePermission.SuperAdmin)]
        [HttpPost]
        public ActionResult SaveEidt([FromBody] Sys_TableInfo tableInfo)
        {
            try
            {
                return Json(Service.SaveEidt(tableInfo));
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LoggerType.Exception, $"BuilderController SaveEidt action error: TableName={tableInfo?.TableName}", tableInfo?.Serialize(), null, ex);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "保存编辑时发生内部服务器错误。", status = false });
            }
        }
        [Route("CreateServices")]
        [ApiActionPermission(ActionRolePermission.SuperAdmin)]
        [HttpPost]
        public ActionResult CreateServices(string tableName, string nameSpace, string foldername, bool? partial, bool? api)
        {
            try
            {
                // Note: The parameters 'partial' and 'api' received by the controller are not passed to the service.
                // The service is called with hardcoded false, true: Service.CreateServices(tableName, nameSpace, foldername, false, true)
                // This might be intentional or a bug in original code. Exception handling is added around the existing call.
                return Content(Service.CreateServices(tableName, nameSpace, foldername, false, true));
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LoggerType.Exception, $"BuilderController CreateServices action error: TableName={tableName}", new { TableName = tableName, Namespace = nameSpace, Foldername = foldername, Partial = partial, Api = api }, null, ex);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "创建服务时发生内部服务器错误。", status = false });
            }
        }
        [Route("LoadTableInfo")]
        [HttpPost]
        public ActionResult LoadTable(int parentId, string tableName, string columnCNName, string nameSpace, string foldername, int table_Id, bool isTreeLoad)
        {
            try
            {
                return Json(Service.LoadTable(parentId, tableName, columnCNName, nameSpace, foldername, table_Id, isTreeLoad));
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LoggerType.Exception, $"BuilderController LoadTable action error: TableName={tableName}", new { ParentId = parentId, TableName = tableName, ColumnCNName = columnCNName, Namespace = nameSpace, Foldername = foldername, Table_Id = table_Id, IsTreeLoad = isTreeLoad }, null, ex);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "加载表信息时发生内部服务器错误。", status = false });
            }
        }
        [Route("delTree")]
        [ApiActionPermission(ActionRolePermission.SuperAdmin)]
        [HttpPost]
        public async Task<ActionResult> DelTree(int table_Id)
        {
            try
            {
                return Json(await Service.DelTree(table_Id));
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LoggerType.Exception, $"BuilderController DelTree action error: Table_Id={table_Id}", new { Table_Id = table_Id }, null, ex);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "删除树节点时发生内部服务器错误。", status = false });
            }
        }
        [Route("syncTable")]
        [ApiActionPermission(ActionRolePermission.SuperAdmin)]
        [HttpPost]
        public async Task<ActionResult> SyncTable(string tableName)
        {
            try
            {
                return Json(await Service.SyncTable(tableName));
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LoggerType.Exception, $"BuilderController SyncTable action error: TableName={tableName}", new { TableName = tableName }, null, ex);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "同步表结构时发生内部服务器错误。", status = false });
            }
        }
    }
}
