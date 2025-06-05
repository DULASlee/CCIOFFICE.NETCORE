/*
 *接口编写处...
*如果接口需要做Action的权限验证，请在Action上使用属性
*如: [ApiActionPermission("FormDesignOptions",Enums.ActionPermissionOptions.Search)]
 */
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using VOL.Entity.DomainModels;
using VOL.Sys.IServices;
using VOL.Sys.IRepositories;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using VOL.Sys.Services;

namespace VOL.Sys.Controllers
{
    public partial class FormDesignOptionsController
    { 
        private readonly IFormDesignOptionsService _service;//访问业务代码
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IFormCollectionObjectRepository _formCollectionRepository;
        private readonly IFormDesignOptionsRepository _formDesignOptionsRepository;
        [ActivatorUtilitiesConstructor]
        public FormDesignOptionsController(
            IFormDesignOptionsService service,
            IHttpContextAccessor httpContextAccessor,
            IFormCollectionObjectRepository formCollectionRepository,
            IFormDesignOptionsRepository formDesignOptionsRepository
        )
        : base(service)
        {
            _service = service;
            _httpContextAccessor = httpContextAccessor;
            _formCollectionRepository = formCollectionRepository;
            _formDesignOptionsRepository = formDesignOptionsRepository;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Route("getFormOptions"), HttpGet]
        public async Task<IActionResult> GetFormOptions(Guid id)
        {
            try
            {
                var options = await _formDesignOptionsRepository.FindAsIQueryable(x => x.FormId == id)
                        .Select(s => new { s.Title, s.FormOptions })
                        .FirstOrDefaultAsync();
                if (options == null)
                {
                    VOL.Core.Services.Logger.Warning(VOL.Core.Enums.LoggerType.Select, $"表单选项未找到: FormId={id}", new { FormId = id });
                    return NotFound(new { message = $"表单选项未找到: FormId={id}" });
                }
                return Json(new { data = options });
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LoggerType.Exception, $"获取表单选项时出错: FormId={id}", new { FormId = id }, null, ex);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "获取表单选项时发生内部服务器错误。" });
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="saveModel"></param>
        /// <returns></returns>
        [Route("submit"), HttpPost]
        public IActionResult Submit([FromBody] SaveModel saveModel)
        {
            try
            {
                // Service call should ideally return a WebResponseContent or similar structured response
                var result = FormCollectionObjectService.Instance.Add(saveModel);
                // Assuming 'result' has a 'Status' property and 'Message' or 'Data' property
                // For example, if result is WebResponseContent:
                if (result.Status)
                {
                    return Json(result); // Or Ok(result) if appropriate
                }
                else
                {
                    VOL.Core.Services.Logger.Warning(VOL.Core.Enums.LoggerType.Add, $"表单提交失败: {result.Message}", saveModel.Serialize(), result.Serialize());
                    return BadRequest(new { message = result.Message, data = result.Data });
                }
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LoggerType.Exception, "提交表单时出错", saveModel.Serialize(), null, ex);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "提交表单时发生内部服务器错误。" });
            }
        }
        /// <summary>
        ///获取有数据的设计器
        /// </summary>
        /// <returns></returns>
        [Route("getList"), HttpGet]
        public IActionResult GetList()
        {
            try
            {
                var query = _formCollectionRepository.FindAsIQueryable(x => true); // Defines query
                // Actual DB call happens with .ToList()
                var data = _formDesignOptionsRepository.FindAsIQueryable(x => query.Any(c => c.FormId == x.FormId))
                      .Select(s => new { s.FormId, s.Title, s.FormOptions })
                      .ToList();
                return Json(data);
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LoggerType.Exception, "获取有数据的设计器列表时出错", null, null, ex);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "获取设计器列表时发生内部服务器错误。" });
            }
        }
    }
}
