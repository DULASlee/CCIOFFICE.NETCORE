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
using Microsoft.Extensions.Logging;

namespace VOL.Sys.Controllers
{
    public partial class FormDesignOptionsController
    { 
        private readonly IFormDesignOptionsService _service;//访问业务代码
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IFormCollectionObjectRepository _formCollectionRepository;
        private readonly IFormDesignOptionsRepository _formDesignOptionsRepository;
        private readonly ILogger<FormDesignOptionsController> _logger;
        [ActivatorUtilitiesConstructor]
        public FormDesignOptionsController(
            IFormDesignOptionsService service,
            IHttpContextAccessor httpContextAccessor,
            IFormCollectionObjectRepository formCollectionRepository,
            IFormDesignOptionsRepository formDesignOptionsRepository,
            ILogger<FormDesignOptionsController> logger
        )
        : base(service)
        {
            _service = service;
            _httpContextAccessor = httpContextAccessor;
            _formCollectionRepository = formCollectionRepository;
            _formDesignOptionsRepository = formDesignOptionsRepository;
            _logger = logger;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Route("getFormOptions"), HttpGet]
        public async Task<IActionResult> GetFormOptions(Guid id)
        {
            _logger.LogInformation("GetFormOptions called with ID {FormId}", id);

            if (id == Guid.Empty)
            {
                _logger.LogWarning("GetFormOptions called with an empty Guid.");
                return new BadRequestObjectResult(new { message = "Form ID cannot be empty." });
            }

            try
            {
                var options = await _formDesignOptionsRepository.FindAsIQueryable(x => x.FormId == id)
                        .Select(s => new { s.Title, s.FormOptions })
                        .FirstOrDefaultAsync();

                if (options == null)
                {
                    _logger.LogInformation("Form options not found for ID {FormId}", id);
                    return new NotFoundObjectResult(new { message = "Form options not found." });
                }
                return Json(new { data = options });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching form options for ID {FormId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while fetching form options." });
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
            _logger.LogInformation("Submit method called for FormDesignOptions.");

            if (saveModel == null)
            {
                _logger.LogWarning("Submit called with null saveModel.");
                return new BadRequestObjectResult(new { message = "Save data cannot be null." });
            }

            try
            {
                var result = FormCollectionObjectService.Instance.Add(saveModel);

                if (result.Status)
                {
                    _logger.LogInformation("Submit successful for FormDesignOptions.");
                }
                else
                {
                    _logger.LogWarning("Submit for FormDesignOptions failed with service message: {ServiceMessage}", result.Message);
                }
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during Submit for FormDesignOptions.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred while submitting form data." });
            }
        }
        /// <summary>
        ///获取有数据的设计器
        /// </summary>
        /// <returns></returns>
        [Route("getList"), HttpGet]
        public IActionResult GetList()
        {
            _logger.LogInformation("GetList called for FormDesignOptions.");
            try
            {
                var query = _formCollectionRepository.FindAsIQueryable(x => true);
                var data = _formDesignOptionsRepository.FindAsIQueryable(x => query.Any(c => c.FormId == x.FormId))
                      .Select(s => new { s.FormId, s.Title, s.FormOptions })
                      .ToList();
                return Json(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching form design list.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while fetching the form design list." });
            }
        }
    }
}
