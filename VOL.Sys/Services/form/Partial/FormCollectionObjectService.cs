/*
 *所有关于FormCollectionObject类的业务代码应在此处编写
*可使用repository.调用常用方法，获取EF/Dapper等信息
*如果需要事务请使用repository.DbContextBeginTransaction
*也可使用DBServerProvider.手动获取数据库相关信息
*用户信息、权限、角色等使用UserContext.Current操作
*FormCollectionObjectService对增、删、改查、导入、导出、审核业务代码扩展参照ServiceFunFilter
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
using VOL.Core.Configuration;
using VOL.Core.Services;
using System;
using OfficeOpenXml;
using System.IO;
using OfficeOpenXml.Style;
using System.Drawing;

namespace VOL.Sys.Services
{
    public partial class FormCollectionObjectService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IFormCollectionObjectRepository _repository;//访问数据库
        private readonly IFormDesignOptionsRepository _designOptionsRepository;
        [ActivatorUtilitiesConstructor]
        public FormCollectionObjectService(
            IFormCollectionObjectRepository dbRepository,
            IHttpContextAccessor httpContextAccessor,
             IFormDesignOptionsRepository designOptionsRepository
            )
        : base(dbRepository)
        {
            _httpContextAccessor = httpContextAccessor;
            _repository = dbRepository;
            _designOptionsRepository = designOptionsRepository;
        }

        public override WebResponseContent Export(PageDataOptions pageData)
        {
            string path = null;
            string fileName = null;
            // webResponse is declared at class level, ensure it's reset or use local instances for delegates.
            // Using local instances for clarity within the delegate.
            ExportOnExecuting = (List<FormCollectionObject> list, List<string> columns) =>
            {
                WebResponseContent localResponse = new WebResponseContent();
                if (list == null || !list.Any())
                {
                    return localResponse.Error("没有可导出的数据。");
                }

                var formId = list[0].FormId;
                dynamic formDesignData; // Use dynamic as its structure is simple { Title, FormConfig }
                try
                {
                    formDesignData = _designOptionsRepository.FindAsIQueryable(x => x.FormId == formId)
                                       .Select(s => new { s.Title, s.FormConfig })
                                       .FirstOrDefault();
                    if (formDesignData == null)
                    {
                        Logger.Warning(VOL.Core.Enums.LoggerType.Export, $"导出表单数据失败: 未找到表单设计. FormId={formId}", new { FormId = formId }, null);
                        return localResponse.Error($"未找到ID为 {formId} 的表单设计。");
                    }
                }
                catch (Exception exDb)
                {
                    VOL.Core.Services.Logger.Error(VOL.Core.Enums.LoggerType.Export, $"导出表单数据时查询表单设计失败: FormId={formId}", new { FormId = formId }, null, exDb);
                    return localResponse.Error("查询表单设计时发生数据库错误。");
                }

                try
                {
                    List<FormOptions> formObj = formDesignData.FormConfig.DeserializeObject<List<FormOptions>>();
                    List<Dictionary<string, object>> listDic = new List<Dictionary<string, object>>();
                    foreach (var item in list)
                    {
                        Dictionary<string, object> dic = new Dictionary<string, object>();
                        var formData = item.FormData.DeserializeObject<Dictionary<string, string>>();
                        dic.Add("标题", formDesignData.Title);
                        dic.Add("提交人", item.Creator);
                        dic.Add("提交时间", item.CreateDate.ToString("yyyy-MM-dd HH:mm:sss"));
                        foreach (var objConfig in formObj) // Renamed to avoid conflict
                        {
                            dic.Add(objConfig.Title, formData.Where(x => x.Key == objConfig.Field).Select(s => s.Value).FirstOrDefault());
                        }
                        listDic.Add(dic);
                    }
                    string generatedFileName = formDesignData.Title + ".xlsx";
                    string generatedPath = EPPlusHelper.ExportGeneralExcel(listDic, generatedFileName);

                    // Signal to base.Export to use this path instead of its own logic
                    localResponse.Code = "-1";
                    // Assign to outer scope variables if they are indeed used by base.Export or other parts.
                    // However, this pattern is risky. It's better if base.Export can take this path directly.
                    path = generatedPath;
                    fileName = generatedFileName;
                    return localResponse.OK(null, generatedPath.EncryptDES(AppSetting.Secret.ExportFile));
                }
                catch (Exception ex)
                {
                    VOL.Core.Services.Logger.Error(VOL.Core.Enums.LoggerType.Export, $"导出表单数据时解析或生成Excel失败: FormId={formId}, Title={formDesignData.Title}", new { FormId = formId, Title = formDesignData.Title, FormConfig = formDesignData.FormConfig }, null, ex);
                    return localResponse.Error("生成导出的表单文件时出错。");
                }
            };
            // The base.Export(pageData) will use the 'path' and 'fileName' set in ExportOnExecuting if webResponse.Code == "-1"
            // This interaction pattern with base class via side effects (setting path/fileName) and a special Code value is a bit fragile.
            var exportResult = base.Export(pageData);
            if (exportResult.Code == "-1" && !string.IsNullOrEmpty(path)) // path would have been set by ExportOnExecuting
            {
                 // If ExportOnExecuting was meant to completely override and provide the file:
                 return new WebResponseContent().OK(null, path.EncryptDES(AppSetting.Secret.ExportFile));
            }
            return exportResult; // Return result from base.Export if not overridden
        } 
    }

    public class FormOptions
    {
        public string Field { get; set; }

        public string Title { get; set; }

        public string Type { get; set; }
    }
}
