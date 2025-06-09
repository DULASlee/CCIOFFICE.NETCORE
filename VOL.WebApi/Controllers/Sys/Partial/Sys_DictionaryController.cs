using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using VOL.Core.Controllers.Basic;
using VOL.Core.Extensions;
using VOL.Core.Filters;
using VOL.Sys.IServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace VOL.Sys.Controllers
{
    public partial class Sys_DictionaryController
    {
        private readonly ILogger<Sys_DictionaryController> _logger;

        [ActivatorUtilitiesConstructor]
        public Sys_DictionaryController(
            ISys_DictionaryService service,
            ILogger<Sys_DictionaryController> logger
        )
        : base(service)
        {
            _logger = logger;
        }

        [HttpPost, Route("GetVueDictionary"), AllowAnonymous]
        [ApiActionPermission()]
        public IActionResult GetVueDictionary([FromBody] string[] dicNos)
        {
            _logger.LogInformation("GetVueDictionary called with dicNos: {DicNosCount}", dicNos?.Length ?? 0);
            if (dicNos == null || dicNos.Length == 0)
            {
                _logger.LogWarning("GetVueDictionary called with null or empty dicNos.");
                return new BadRequestObjectResult(new { status = false, message = "Dictionary numbers cannot be empty." });
            }
            try
            {
                return Content(Service.GetVueDictionary(dicNos).Serialize());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetVueDictionary. dicNos count: {DicNosCount}", dicNos.Length);
                return StatusCode(StatusCodes.Status500InternalServerError, new { status = false, message = "An error occurred while fetching Vue dictionary." });
            }
        }
        /// <summary>
        /// table加载数据后刷新当前table数据的字典项(适用字典数据量比较大的情况)
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [HttpPost, Route("getTableDictionary")]
        public IActionResult GetTableDictionary([FromBody] Dictionary<string, object[]> keyData)
        {
            _logger.LogInformation("GetTableDictionary called with keyData count: {KeyDataCount}", keyData?.Count ?? 0);
            if (keyData == null || keyData.Count == 0)
            {
                _logger.LogWarning("GetTableDictionary called with null or empty keyData.");
                return new BadRequestObjectResult(new { status = false, message = "Key data cannot be empty." });
            }
            try
            {
                return Json(Service.GetTableDictionary(keyData));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetTableDictionary. keyData count: {KeyDataCount}", keyData.Count);
                return StatusCode(StatusCodes.Status500InternalServerError, new { status = false, message = "An error occurred while fetching table dictionary." });
            }
        }
        /// <summary>
        /// 远程搜索
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [HttpPost, Route("getSearchDictionary"), AllowAnonymous]
        public IActionResult GetSearchDictionary(string dicNo, string value)
        {
            _logger.LogInformation("GetSearchDictionary called with dicNo: {DicNo}, value: {Value}", dicNo, value);
            if (string.IsNullOrEmpty(dicNo))
            {
                _logger.LogWarning("GetSearchDictionary called with null or empty dicNo.");
                return new BadRequestObjectResult(new { status = false, message = "Dictionary number cannot be empty." });
            }
            try
            {
                return Json(Service.GetSearchDictionary(dicNo, value));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetSearchDictionary. dicNo: {DicNo}, value: {Value}", dicNo, value);
                return StatusCode(StatusCodes.Status500InternalServerError, new { status = false, message = "An error occurred while searching dictionary." });
            }
        }

        /// <summary>
        /// 表单设置为远程查询，重置或第一次添加表单时，获取字典的key、value
        /// </summary>
        /// <param name="dicNo"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        [HttpPost, Route("getRemoteDefaultKeyValue"), AllowAnonymous]
        public async Task<IActionResult> GetRemoteDefaultKeyValue(string dicNo, string key)
        {
            _logger.LogInformation("GetRemoteDefaultKeyValue called with dicNo: {DicNo}, key: {Key}", dicNo, key);
            if (string.IsNullOrEmpty(dicNo) || string.IsNullOrEmpty(key))
            {
                _logger.LogWarning("GetRemoteDefaultKeyValue called with null or empty dicNo or key. DicNo: {DicNo}, Key: {Key}", dicNo, key);
                return new BadRequestObjectResult(new { status = false, message = "Dictionary number and key cannot be empty." });
            }
            try
            {
                return Json(await Service.GetRemoteDefaultKeyValue(dicNo, key));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetRemoteDefaultKeyValue. dicNo: {DicNo}, key: {Key}", dicNo, key);
                return StatusCode(StatusCodes.Status500InternalServerError, new { status = false, message = "An error occurred while fetching remote default key value." });
            }
        }
        /// <summary>
        /// 代码生成器获取所有字典项(超级管理权限)
        /// </summary>
        /// <returns></returns>
        [HttpPost, Route("GetBuilderDictionary")]
        // [ApiActionPermission(ActionRolePermission.SuperAdmin)]
        public async Task<IActionResult> GetBuilderDictionary()
        {
            _logger.LogInformation("GetBuilderDictionary called.");
            try
            {
                return Json(await Service.GetBuilderDictionary());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetBuilderDictionary.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { status = false, message = "An error occurred while fetching builder dictionary." });
            }
        }

    }
}
