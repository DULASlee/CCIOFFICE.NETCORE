/*
 *所有关于Sys_QuartzOptions类的业务代码应在此处编写
*可使用repository.调用常用方法，获取EF/Dapper等信息
*如果需要事务请使用repository.DbContextBeginTransaction
*也可使用DBServerProvider.手动获取数据库相关信息
*用户信息、权限、角色等使用UserContext.Current操作
*Sys_QuartzOptionsService对增、删、改查、导入、导出、审核业务代码扩展参照ServiceFunFilter
*/
using VOL.Core.BaseProvider;
using VOL.Core.Extensions.AutofacManager;
using VOL.Entity.DomainModels;
using System.Linq;
using VOL.Core.Utilities;
using System.Linq.Expressions;
using VOL.Core.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using VOL.Sys.IRepositories;
using VOL.Core.Quartz;
using Quartz;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace VOL.Sys.Services
{
    public partial class Sys_QuartzOptionsService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ISys_QuartzOptionsRepository _repository;//访问数据库
        private readonly ISchedulerFactory _schedulerFactory;
        [ActivatorUtilitiesConstructor]
        public Sys_QuartzOptionsService(
            ISys_QuartzOptionsRepository dbRepository,
            IHttpContextAccessor httpContextAccessor,
            ISchedulerFactory schedulerFactory
            )
        : base(dbRepository)
        {
            _httpContextAccessor = httpContextAccessor;
            _repository = dbRepository;
            _schedulerFactory = schedulerFactory;
            //多租户会用到这init代码，其他情况可以不用
            //base.Init(dbRepository);
        }

        public override PageGridData<Sys_QuartzOptions> GetPageData(PageDataOptions options)
        {
            var result = base.GetPageData(options);
            return result;
        }

        WebResponseContent webResponse = new WebResponseContent();
        public override WebResponseContent Add(SaveModel saveDataModel)
        {
            AddOnExecuting = (Sys_QuartzOptions options, object list) =>
            {
                options.Status = (int)TriggerState.Paused;
                return webResponse.OK();
            };
            Sys_QuartzOptions ops = null;
            AddOnExecuted = (Sys_QuartzOptions options, object list) =>
            {
                ops = options;
                return webResponse.OK();
            };
            var result = base.Add(saveDataModel);
            if (result.Status)
            {
                try
                {
                    ops.AddJob(_schedulerFactory).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    VOL.Core.Services.Logger.Error(VOL.Core.Enums.LoggerType.Insert, $"添加到调度器失败:{ops.Id}_{ops.TaskName}", ops.Serialize(), null, ex);
                    // Decide if the main operation should still be considered successful
                    // For now, we just log and the original result status is maintained.
                    // If this failure should make the entire Add operation fail, you might need to:
                    // result.Status = false;
                    // result.Message = "添加主业务成功，但添加到调度器失败";
                    // Or rethrow a custom exception if preferred.
                }
            }
            return result;
        }

        public override WebResponseContent Del(object[] keys, bool delList = true)
        {
            var ids = keys.Select(s => (Guid)(s.GetGuid())).ToArray();

            // It's good practice to fetch options before calling base.Del,
            // as base.Del might commit transaction and make options unavailable if fetched later.
            var optionsToRemove = repository.FindAsIQueryable(x => ids.Contains(x.Id)).ToList();

            // Call base.Del first. If it fails, we don't proceed to scheduler removal.
            var response = base.Del(keys, delList);
            if (!response.Status)
            {
                return response;
            }

            // If base.Del was successful, try to remove from scheduler
            optionsToRemove.ForEach(options =>
            {
                try
                {
                    _schedulerFactory.Remove(options).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    VOL.Core.Services.Logger.Error(VOL.Core.Enums.LoggerType.Delete, $"从调度器移除失败:{options.Id}_{options.TaskName}", options.Serialize(), null, ex);
                    // Potentially collect these errors to return a partial success message
                    // For now, just logging. The main delete operation was successful.
                }
            });

            return response;
        }

        public override WebResponseContent Update(SaveModel saveModel)
        {

            UpdateOnExecuted = (Sys_QuartzOptions options, object addList, object updateList, List<object> delKeys) =>
            {
                try
                {
                    _schedulerFactory.Update(options).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    VOL.Core.Services.Logger.Error(VOL.Core.Enums.LoggerType.Update, $"更新调度器任务失败:{options.Id}_{options.TaskName}", options.Serialize(), null, ex);
                    // The main entity update in DB was successful.
                    // This error indicates a sync issue with the scheduler.
                    // Consider if this should alter the overall response.
                    // For now, returning webResponse.OK() as per original logic, but logging the error.
                }
                return webResponse.OK();
            };
            return base.Update(saveModel);
        }

        /// <summary>
        /// 手动执行一次
        /// </summary>
        /// <param name="taskOptions"></param>
        /// <returns></returns>
        public async Task<object> Run(Sys_QuartzOptions taskOptions)
        {
            try
            {
                return await _schedulerFactory.Run(taskOptions);
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LoggerType.Exception, $"手动执行任务失败:{taskOptions.Id}_{taskOptions.TaskName}", taskOptions.Serialize(), null, ex);
                // Return a value indicating failure, or rethrow, depending on desired contract.
                // For now, rethrowing to let the caller know something went wrong.
                // If a specific return type is expected on failure, adjust accordingly.
                throw;
            }
        }
        /// <summary>
        /// 开启任务
        /// </summary>
        /// <param name="schedulerFactory"></param>
        /// <param name="taskOptions"></param>
        /// <returns></returns>
        public async Task<object> Start(Sys_QuartzOptions taskOptions)
        {
            object schedulerResult;
            try
            {
                schedulerResult = await _schedulerFactory.Start(taskOptions);
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LoggerType.Exception, $"启动任务失败:{taskOptions.Id}_{taskOptions.TaskName}", taskOptions.Serialize(), null, ex);
                // Rethrow or return specific error object
                throw;
            }

            if (taskOptions.Status != (int)TriggerState.Normal)
            {
                taskOptions.Status = (int)TriggerState.Normal;
                try
                {
                    _repository.Update(taskOptions, x => new { x.Status }, true);
                }
                catch (Exception dbEx)
                {
                    VOL.Core.Services.Logger.Error(VOL.Core.Enums.LoggerType.Update, $"更新任务状态到Normal失败(DB):{taskOptions.Id}_{taskOptions.TaskName}", taskOptions.Serialize(), null, dbEx);
                    // The task started in scheduler, but DB update of status failed.
                    // This is a state inconsistency. Decide on how to handle.
                    // Maybe rethrow, or return schedulerResult but with a warning.
                    // For now, logging the error and the original schedulerResult will be returned.
                }
            }
            return schedulerResult;
        }

        /// <summary>
        /// 暂停任务
        /// </summary>
        /// <param name="schedulerFactory"></param>
        /// <param name="taskOptions"></param>
        /// <returns></returns>
        public async Task<object> Pause(Sys_QuartzOptions taskOptions)
        {
            object schedulerResult;
            try
            {
                //  var result = await _schedulerFactory.Remove(taskOptions); // Original commented out code
                schedulerResult = await _schedulerFactory.Pause(taskOptions);
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LoggerType.Exception, $"暂停任务失败:{taskOptions.Id}_{taskOptions.TaskName}", taskOptions.Serialize(), null, ex);
                // Rethrow or return specific error object
                throw;
            }

            taskOptions.Status = (int)TriggerState.Paused;
            try
            {
                _repository.Update(taskOptions, x => new { x.Status }, true);
            }
            catch (Exception dbEx)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LoggerType.Update, $"更新任务状态到Paused失败(DB):{taskOptions.Id}_{taskOptions.TaskName}", taskOptions.Serialize(), null, dbEx);
                // The task was paused in scheduler, but DB update of status failed.
                // This is a state inconsistency.
                // For now, logging the error and the original schedulerResult will be returned.
            }
            return schedulerResult;
        }
    }
}
