/*
 *所有关于Sys_WorkFlow类的业务代码应在此处编写
*可使用repository.调用常用方法，获取EF/Dapper等信息
*如果需要事务请使用repository.DbContextBeginTransaction
*也可使用DBServerProvider.手动获取数据库相关信息
*用户信息、权限、角色等使用UserContext.Current操作
*Sys_WorkFlowService对增、删、改查、导入、导出、审核业务代码扩展参照ServiceFunFilter
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
using System.Collections.Generic;
using VOL.Core.WorkFlow;
using System;
using VOL.Sys.Repositories;

namespace VOL.Sys.Services
{
    public partial class Sys_WorkFlowService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ISys_WorkFlowRepository _repository;//访问数据库
        private readonly ISys_WorkFlowStepRepository _stepRepository;
        [ActivatorUtilitiesConstructor]
        public Sys_WorkFlowService(
            ISys_WorkFlowRepository dbRepository,
            IHttpContextAccessor httpContextAccessor,
            ISys_WorkFlowStepRepository stepRepository
            )
        : base(dbRepository)
        {
            _httpContextAccessor = httpContextAccessor;
            _repository = dbRepository;
            _stepRepository = stepRepository;
            //多租户会用到这init代码，其他情况可以不用
            //base.Init(dbRepository);
        }

        WebResponseContent webResponse = new WebResponseContent();
        public override WebResponseContent Add(SaveModel saveDataModel)
        {
            saveDataModel.MainData["Enable"] = 1;


            AddOnExecuting = (Sys_WorkFlow workFlow, object list) =>
            {
                try
                {
                    workFlow.WorkFlow_Id = Guid.NewGuid();
                    var steps = list as List<Sys_WorkFlowStep>;
                    // Ensure webResponse is initialized for this delegate context
                    WebResponseContent localResponse = WorkFlowContainer.Instance.AddTable(workFlow, steps);
                    if (!localResponse.Status)
                    {
                        return localResponse;
                    }
                    return new WebResponseContent().OK(); // Use new instance for OK
                }
                catch (Exception ex)
                {
                    VOL.Core.Services.Logger.Error(VOL.Core.Enums.LoggerType.Add, $"工作流AddOnExecuting处理失败: WorkFlowName={workFlow?.WorkFlowName}", workFlow?.Serialize(), null, ex);
                    return new WebResponseContent().Error("处理工作流时发生内部错误。");
                }
            };

            AddOnExecuted = (Sys_WorkFlow workFlow, object list) =>
            {
                return webResponse.OK();
            };
            return base.Add(saveDataModel);
        }

        public override WebResponseContent Update(SaveModel saveModel)
        {
            Sys_WorkFlow flow = null;
            UpdateOnExecuting = (Sys_WorkFlow workFlow, object addList, object updateList, List<object> delKeys) =>
            {
                flow = workFlow; // Used later in the main Update method body
                try
                {
                    if ((workFlow.AuditingEdit ?? 0) == 0)
                    {
                        // DB Call
                        if (Sys_WorkFlowTableRepository.Instance.Exists(x => x.WorkFlow_Id == workFlow.WorkFlow_Id && (x.AuditStatus == (int)AuditStatus.审核中)))
                        {
                            return new WebResponseContent().Error("当前流程有审核中的数据，不能修改,可以修改,流程中的【审核中数据是否可以编辑】属性");
                        }
                    }

                    List<Sys_WorkFlowStep> add = addList as List<Sys_WorkFlowStep>;
                    var stepsClone = add.Serialize().DeserializeObject<List<Sys_WorkFlowStep>>();
                    add.Clear();

                    // DB Call
                    var steps = _stepRepository.FindAsIQueryable(x => x.WorkFlow_Id == workFlow.WorkFlow_Id)
                                             .Select(s => new { s.WorkStepFlow_Id, s.StepId })
                                             .ToList();

                    var delIds = steps.Where(x => !stepsClone.Any(c => c.StepId == x.StepId))
                                     .Select(s => s.WorkStepFlow_Id).ToList();
                    delKeys.AddRange(delIds.Select(s => s as object));

                    var newSteps = stepsClone.Where(x => !steps.Any(c => c.StepId == x.StepId)).ToList();
                    add.AddRange(newSteps);

                    List<Sys_WorkFlowStep> update = updateList as List<Sys_WorkFlowStep>;
                    var updateSteps = stepsClone.Where(x => steps.Any(c => c.StepId == x.StepId)).ToList();
                    update.AddRange(updateSteps);

                    updateSteps.ForEach(x =>
                    {
                        x.WorkStepFlow_Id = steps.First(c => c.StepId == x.StepId).WorkStepFlow_Id;
                        foreach (var item in saveModel.DetailData)
                        {
                            if (item["StepId"].ToString() == x.StepId)
                            {
                                item["WorkFlow_Id"] = workFlow.WorkFlow_Id;
                                item["WorkStepFlow_Id"] = x.WorkStepFlow_Id;
                            }
                        }
                    });
                    return new WebResponseContent().OK();
                }
                catch (Exception ex)
                {
                    VOL.Core.Services.Logger.Error(VOL.Core.Enums.LoggerType.Update, $"工作流UpdateOnExecuting处理失败: WorkFlowName={workFlow?.WorkFlowName}", workFlow?.Serialize(), null, ex);
                    return new WebResponseContent().Error("更新工作流前置处理时发生内部错误。");
                }
            };

            // The rest of the Update method (base call and post-execution logic)
            try
            {
                webResponse = base.Update(saveDataModel); // This can throw, handled by ServiceBase or needs its own try-catch if specific handling is required here
                if (webResponse.Status && flow != null) // flow is set in UpdateOnExecuting
                {
                    // DB Call
                    flow = repository.FindAsIQueryable(x => x.WorkFlow_Id == flow.WorkFlow_Id)
                                   .Include(x => x.Sys_WorkFlowStep)
                                   .FirstOrDefault();
                    if (flow != null)
                    {
                        // External component call
                        WebResponseContent containerResponse = WorkFlowContainer.Instance.AddTable(flow, flow.Sys_WorkFlowStep);
                        if (!containerResponse.Status)
                        {
                            // Log this failure but don't necessarily overwrite webResponse from base.Update if it was successful
                            VOL.Core.Services.Logger.Warning(VOL.Core.Enums.LoggerType.Update, $"更新后，WorkFlowContainer.AddTable失败: WorkFlowName={flow.WorkFlowName}. Message: {containerResponse.Message}", flow.Serialize(), null);
                            // Optionally, append to existing message:
                            // webResponse.Message += " (WorkFlowContainer update failed: " + containerResponse.Message + ")";
                        }
                    }
                    else
                    {
                         VOL.Core.Services.Logger.Warning(VOL.Core.Enums.LoggerType.Update, $"更新后，未能重新加载工作流: WorkFlowId={saveDataModel.MainData.GetGuid("WorkFlow_Id")}", saveDataModel.MainData, null);
                    }
                }
            }
            catch (Exception ex) // Catches exceptions from base.Update or subsequent operations if not handled by ServiceBase
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LoggerType.Update, $"工作流更新操作整体失败: WorkFlowId={saveDataModel.MainData.GetGuid("WorkFlow_Id")}", saveDataModel.MainData, null, ex);
                 // Ensure webResponse is set to an error state if it's not already
                if (webResponse == null || webResponse.Status) // webResponse might be null if base.Update throws early
                {
                    webResponse = new WebResponseContent().Error("更新工作流时发生严重错误。");
                }
                else if (!webResponse.Message.EndsWith("发生严重错误。") && !webResponse.Message.EndsWith("内部错误。")) // Avoid appending if already a detailed error
                {
                     webResponse.Message += " 更新过程中发生严重错误。"; // Append to existing error message
                }
            }
            return webResponse;
        }

        public override WebResponseContent Del(object[] keys, bool delList = true)
        {
            
            try
            {
                webResponse = base.Del(keys, delList); // This can throw, handled by ServiceBase or needs its own try-catch
                if (webResponse.Status)
                {
                    try
                    {
                        WorkFlowContainer.DelRange(keys.Select(s => (Guid)s.GetGuid()).ToArray());
                    }
                    catch (Exception exContainer)
                    {
                        VOL.Core.Services.Logger.Error(VOL.Core.Enums.LoggerType.Delete, $"删除后，WorkFlowContainer.DelRange失败: Keys={string.Join(",", keys)}", new { Keys = keys?.Serialize() }, null, exContainer);
                        // Main deletion was successful. Optionally append to message.
                        // webResponse.Message += " (WorkFlowContainer cleanup failed: " + exContainer.Message + ")";
                    }
                }
            }
            catch (Exception ex) // Catches exceptions from base.Del if not handled by ServiceBase
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LoggerType.Delete, $"工作流删除操作整体失败: Keys={string.Join(",", keys)}", new { Keys = keys?.Serialize() }, null, ex);
                if (webResponse == null || webResponse.Status)
                {
                    webResponse = new WebResponseContent().Error("删除工作流时发生严重错误。");
                }
                 else if (!webResponse.Message.EndsWith("发生严重错误。") && !webResponse.Message.EndsWith("内部错误。"))
                {
                     webResponse.Message += " 删除过程中发生严重错误。";
                }
            }
            return webResponse;
        }
    }
}
