using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using VOL.Core.Controllers.Basic;
using VOL.Core.Extensions;
using VOL.Core.Filters;
using VOL.Sys.IServices;

namespace VOL.Sys.Controllers
{
    public partial class Sys_DictionaryController
    {
        [HttpPost, Route("GetVueDictionary"), AllowAnonymous]
        [ApiActionPermission()]
        public IActionResult GetVueDictionary([FromBody] string[] dicNos)
        {
            try
            {
                var result = Service.GetVueDictionary(dicNos);
                // Assuming Service.GetVueDictionary returns a structured object, not pre-serialized JSON
                // If it returns an object that should be serialized to JSON for the response:
                return Json(result);
                // If Service.GetVueDictionary itself returns a JSON string, then Content(result, "application/json") is fine.
                // The original code .Serialize() suggests it might be returning an object.
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LoggerType.Exception, "获取Vue字典时出错", new { DicNos = dicNos?.Serialize() }, null, ex);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "获取字典时发生内部服务器错误。" });
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
            try
            {
                var result = Service.GetTableDictionary(keyData);
                return Json(result);
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LoggerType.Exception, "获取表格字典数据时出错", new { KeyDataCount = keyData?.Count }, null, ex);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "获取表格字典数据时发生内部服务器错误。" });
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
            try
            {
                var result = Service.GetSearchDictionary(dicNo, value);
                return Json(result);
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LoggerType.Exception, $"远程搜索字典时出错: DicNo={dicNo}", new { DicNo = dicNo, Value = value }, null, ex);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "远程搜索字典时发生内部服务器错误。" });
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
            try
            {
                var result = await Service.GetRemoteDefaultKeyValue(dicNo, key);
                return Json(result);
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LoggerType.Exception, $"获取远程字典默认键值时出错: DicNo={dicNo}, Key={key}", new { DicNo = dicNo, Key = key }, null, ex);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "获取远程字典默认键值时发生内部服务器错误。" });
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
            try
            {
                var result = await Service.GetBuilderDictionary();
                return Json(result);
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LoggerType.Exception, "代码生成器获取所有字典项时出错", null, null, ex);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "获取代码生成器字典项时发生内部服务器错误。" });
            }
        }

    }
}
