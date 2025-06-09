/*
 *接口编写处...
*如果接口需要做Action的权限验证，请在Action上使用属性
*如: [ApiActionPermission("Sys_DictionaryList",Enums.ActionPermissionOptions.Search)]
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
using Microsoft.Extensions.Logging;
using System.Linq;

namespace VOL.Sys.Controllers
{
    public partial class Sys_DictionaryListController
    {
        private readonly ISys_DictionaryListService _service;//访问业务代码
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<Sys_DictionaryListController> _logger;

        [ActivatorUtilitiesConstructor]
        public Sys_DictionaryListController(
            ISys_DictionaryListService service,
            IHttpContextAccessor httpContextAccessor,
            ILogger<Sys_DictionaryListController> logger
        )
        : base(service)
        {
            _service = service;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }
        /// <summary>
        /// 导出明细
        /// （重写权限）将子表的权限指向主表权限
        /// </summary>
        /// <param name="loadData"></param>
        /// <returns></returns>
        [ApiActionPermission("Sys_Dictionary", Core.Enums.ActionPermissionOptions.Export)]
        [ApiExplorerSettings(IgnoreApi = false)]
        [HttpPost, Route("Export")]
        public override ActionResult Export([FromBody] PageDataOptions loadData)
        {
            _logger.LogInformation("Export (Sys_DictionaryListController) called with Value: {Value}, Page: {Page}, Rows: {Rows}", loadData?.Value, loadData?.Page, loadData?.Rows);
            if (loadData == null)
            {
                _logger.LogWarning("Export (Sys_DictionaryListController) called with null loadData.");
                return new BadRequestObjectResult(new { status = false, message = "Load data cannot be null for export." });
            }
            try
            {
                return base.Export(loadData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Export (Sys_DictionaryListController). LoadData Value: {Value}", loadData.Value);
                return StatusCode(StatusCodes.Status500InternalServerError, new { status = false, message = "An error occurred during export." });
            }
        }
        /// <summary>
        /// 导入表数据Excel
        ///  （重写权限）将子表的权限指向主表权限
        /// </summary>
        /// <param name="fileInput"></param>
        /// <returns></returns>
        [HttpPost, Route("Import")]
        [ApiActionPermission("Sys_Dictionary", Core.Enums.ActionPermissionOptions.Import)]
        [ApiExplorerSettings(IgnoreApi = false)]
        public override ActionResult Import(List<IFormFile> fileInput)
        {
            _logger.LogInformation("Import (Sys_DictionaryListController) called with file count: {FileCount}", fileInput?.Count ?? 0);
            if (fileInput == null || !fileInput.Any())
            {
                _logger.LogWarning("Import (Sys_DictionaryListController) called with no files.");
                return new BadRequestObjectResult(new { status = false, message = "No files provided for import." });
            }
            try
            {
                return base.Import(fileInput);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Import (Sys_DictionaryListController). File count: {FileCount}", fileInput.Count);
                return StatusCode(StatusCodes.Status500InternalServerError, new { status = false, message = "An error occurred during import." });
            }
        }
        /// <summary>
        /// 下载导入Excel模板
        /// （重写权限）将子表的权限指向主表权限
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("DownLoadTemplate")]
        [ApiActionPermission("Sys_Dictionary", Core.Enums.ActionPermissionOptions.Import)]
        [ApiExplorerSettings(IgnoreApi = false)]
        public override ActionResult DownLoadTemplate()
        {
            _logger.LogInformation("DownLoadTemplate (Sys_DictionaryListController) called.");
            try
            {
                return base.DownLoadTemplate();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DownLoadTemplate (Sys_DictionaryListController).");
                return StatusCode(StatusCodes.Status500InternalServerError, new { status = false, message = "An error occurred while downloading the template." });
            }
        }
    }
}
