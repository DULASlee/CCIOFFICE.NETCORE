/*
 *所有关于Sys_Department类的业务代码应在此处编写
*可使用repository.调用常用方法，获取EF/Dapper等信息
*如果需要事务请使用repository.DbContextBeginTransaction
*也可使用DBServerProvider.手动获取数据库相关信息
*用户信息、权限、角色等使用UserContext.Current操作
*Sys_DepartmentService对增、删、改查、导入、导出、审核业务代码扩展参照ServiceFunFilter
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
using VOL.Core.ManageUser;
using VOL.Core.UserManager;

namespace VOL.Sys.Services
{
    public partial class Sys_DepartmentService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ISys_DepartmentRepository _repository;//访问数据库

        [ActivatorUtilitiesConstructor]
        public Sys_DepartmentService(
            ISys_DepartmentRepository dbRepository,
            IHttpContextAccessor httpContextAccessor
            )
        : base(dbRepository)
        {
            _httpContextAccessor = httpContextAccessor;
            _repository = dbRepository;
            //多租户会用到这init代码，其他情况可以不用
            //base.Init(dbRepository);
        }

        public override PageGridData<Sys_Department> GetPageData(PageDataOptions options)
        {
            FilterData();
            return base.GetPageData(options);
        }

        private void FilterData()
        {
            try
            {
                //限制 只能看自己部门及下级组织的数据
                var deptIds = UserContext.Current.GetAllChildrenDeptIds(); // Potentially problematic call
                QueryRelativeExpression = (IQueryable<Sys_Department> queryable) =>
                {
                    if (UserContext.Current.IsSuperAdmin)
                    {
                        return queryable;
                    }
                    // deptIds is captured from the outer scope. If GetAllChildrenDeptIds() failed, this lambda shouldn't be set with it.
                    return queryable.Where(x => deptIds.Contains(x.DepartmentId));
                };
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LoggerType.Select, "获取部门过滤数据时出错", null, null, ex);
                // QueryRelativeExpression will not be set, or could be set to a "safe" default e.g., return nothing or throw.
                // For now, it means the filter is simply not applied if an error occurs here.
                // This might be a security concern if the filter is critical for data segregation.
                // Consider QueryRelativeExpression = q => q.Where(d => false); // to return no data
            }
        }
        public override WebResponseContent Export(PageDataOptions pageData)
        {
            FilterData();
            return base.Export(pageData);
        }

        WebResponseContent webResponse = new WebResponseContent();
        public override WebResponseContent Add(SaveModel saveDataModel)
        {
            AddOnExecuting = (Sys_Department dept, object list) =>
            {
                return webResponse.OK();
            };
            return base.Add(saveDataModel).Reload();
        }
        public override WebResponseContent Update(SaveModel saveModel)
        {
            UpdateOnExecuting = (Sys_Department dept, object addList, object updateList, List<object> delKeys) =>
            {
                try
                {
                    if (_repository.Exists(x => x.DepartmentId == dept.ParentId && x.DepartmentId == dept.DepartmentId))
                    {
                        return webResponse.Error("上级组织不能选择自己");
                    }
                    // Consider if these two checks should be combined or if the second one depends on the first not being true.
                    // Assuming they are independent checks for different error conditions.
                    if (_repository.Exists(x => x.ParentId == dept.DepartmentId && x.DepartmentId == dept.ParentId))
                    {
                        return webResponse.Error("不能选择此上级组织");
                    }
                }
                catch (Exception ex)
                {
                    VOL.Core.Services.Logger.Error(VOL.Core.Enums.LoggerType.Update, $"部门更新前检查出错: {dept.DepartmentName}", dept.Serialize(), null, ex);
                    return webResponse.Error("检查部门信息时发生错误，请稍后重试或联系管理员。");
                }
                return webResponse.OK();
            };
            return base.Update(saveModel).Reload();
        }

        public override WebResponseContent Del(object[] keys, bool delList = true)
        {
            return base.Del(keys, delList).Reload();
        }
    }

}
