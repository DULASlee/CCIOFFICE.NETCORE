/*
 *接口编写处...
*如果接口需要做Action的权限验证，请在Action上使用属性
*如: [ApiActionPermission("Sys_QuartzOptions",Enums.ActionPermissionOptions.Search)]
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
using Microsoft.Extensions.Logging;

namespace VOL.Sys.Controllers
{
    public partial class Sys_QuartzOptionsController
    {
        private readonly ISys_QuartzOptionsService _service;//访问业务代码
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<Sys_QuartzOptionsController> _logger;

        [ActivatorUtilitiesConstructor]
        public Sys_QuartzOptionsController(
            ISys_QuartzOptionsService service,
            IHttpContextAccessor httpContextAccessor,
            ILogger<Sys_QuartzOptionsController> logger
        )
        : base(service)
        {
            _service = service;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        /// <summary>
        /// api加上属性 [ApiTask]
        /// </summary>
        /// <returns></returns>
        [ApiTask]
        [HttpGet, HttpPost, Route("test2")]
        public IActionResult Test2()
        {
            _logger.LogInformation("Test2 method called.");
            try
            {
                return Content(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Test2 method.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { status = false, message = "An error occurred in Test2." });
            }
        }

        /// <summary>
        /// api加上属性 [ApiTask]
        /// </summary>
        /// <returns></returns>
        [ApiTask]
        [HttpGet, HttpPost, Route("taskTest")]
        public IActionResult TaskTest()
        {
            _logger.LogInformation("TaskTest method called.");
            try
            {
                return Content(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TaskTest method.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { status = false, message = "An error occurred in TaskTest." });
            }
        }

        /// <summary>
        /// 手动执行一次
        /// </summary>
        /// <param name="taskOptions"></param>
        /// <returns></returns>
        [Route("run"), HttpPost]
        [ActionPermission(ActionPermissionOptions.Update)]
        public async Task<object> Run([FromBody] Sys_QuartzOptions taskOptions)
        {
            _logger.LogInformation("Run called for TaskId: {TaskId}, TaskName: {TaskName}", taskOptions?.Id, taskOptions?.TaskName);
            if (taskOptions == null)
            {
                _logger.LogWarning("Run called with null taskOptions.");
                return new BadRequestObjectResult(new { status = false, message = "Task options cannot be null." });
            }
            try
            {
                return await Service.Run(taskOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Run for TaskId: {TaskId}, TaskName: {TaskName}", taskOptions.Id, taskOptions.TaskName);
                return StatusCode(StatusCodes.Status500InternalServerError, new { status = false, message = "An error occurred while running the task." });
            }
        }
        /// <summary>
        /// 开启任务
        /// </summary>
        /// <param name="schedulerFactory"></param>
        /// <param name="taskOptions"></param>
        /// <returns></returns>
        [Route("start"), HttpPost]
        [ActionPermission(ActionPermissionOptions.Update)]
        public async Task<object> Start([FromBody] Sys_QuartzOptions taskOptions)
        {
            _logger.LogInformation("Start called for TaskId: {TaskId}, TaskName: {TaskName}", taskOptions?.Id, taskOptions?.TaskName);
            if (taskOptions == null)
            {
                _logger.LogWarning("Start called with null taskOptions.");
                return new BadRequestObjectResult(new { status = false, message = "Task options cannot be null." });
            }
            try
            {
                return await Service.Start(taskOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Start for TaskId: {TaskId}, TaskName: {TaskName}", taskOptions.Id, taskOptions.TaskName);
                return StatusCode(StatusCodes.Status500InternalServerError, new { status = false, message = "An error occurred while starting the task." });
            }
        }

        /// <summary>
        /// 暂停任务
        /// </summary>
        /// <param name="schedulerFactory"></param>
        /// <param name="taskOptions"></param>
        /// <returns></returns>
        [Route("pause"), HttpPost]
        [ActionPermission(ActionPermissionOptions.Update)]
        public async Task<object> Pause([FromBody] Sys_QuartzOptions taskOptions)
        {
            _logger.LogInformation("Pause called for TaskId: {TaskId}, TaskName: {TaskName}", taskOptions?.Id, taskOptions?.TaskName);
            if (taskOptions == null)
            {
                _logger.LogWarning("Pause called with null taskOptions.");
                return new BadRequestObjectResult(new { status = false, message = "Task options cannot be null." });
            }
            try
            {
                return await Service.Pause(taskOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Pause for TaskId: {TaskId}, TaskName: {TaskName}", taskOptions.Id, taskOptions.TaskName);
                return StatusCode(StatusCodes.Status500InternalServerError, new { status = false, message = "An error occurred while pausing the task." });
            }
        }
    }
}
