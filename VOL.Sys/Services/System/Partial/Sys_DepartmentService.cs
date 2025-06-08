/*
 * author:doubao! codesoft
 * date:2025-6-10
 * detail:优化部门循环调用检测代码
 *所有关于Sys_Department类的业务代码应在此处编写
*可使用repository.调用常用方法，获取EF/Dapper等信息
*如果需要事务请使用repository.DbContextBeginTransaction
*也可使用DBServerProvider.手动获取数据库相关信息
*用户信息、权限、角色等使用UserContext.Current操作
*Sys_DepartmentService对增、删、改查、导入、导出、审核业务代码扩展参照ServiceFunFilter
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using VOL.Core.BaseProvider;
using VOL.Core.Extensions;
using VOL.Core.Extensions.AutofacManager;
using VOL.Core.ManageUser;
using VOL.Core.UserManager;
using VOL.Core.Utilities;
using VOL.Entity.DomainModels;
using VOL.Sys.IRepositories;

namespace VOL.Sys.Services
{
    public partial class Sys_DepartmentService
    {
        #region 私有字段
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ISys_DepartmentRepository _departmentRepository; // 重命名避免与基类冲突

        // 常量定义
        private const string ERROR_PARENT_CANNOT_BE_SELF = "上级组织不能选择自己";
        private const string ERROR_INVALID_PARENT_SELECTION = "不能选择此上级组织";
        private const string ERROR_INVALID_OPERATION_CONTEXT = "操作上下文无效";
        private const string ERROR_DATABASE_OPERATION_FAILED = "数据库操作失败";
        #endregion

        #region 构造函数
        /// <summary>
        /// 初始化部门服务业务实例
        /// </summary>
        /// <param name="dbRepository">部门数据仓储</param>
        /// <param name="httpContextAccessor">HTTP上下文访问器</param>
        /// <exception cref="ArgumentNullException">当参数为空时抛出</exception>
        [ActivatorUtilitiesConstructor]
        public Sys_DepartmentService(
            ISys_DepartmentRepository dbRepository,
            IHttpContextAccessor httpContextAccessor)
        : base(dbRepository)
        {
            try
            {
                _httpContextAccessor = httpContextAccessor ??
                    throw new ArgumentNullException(nameof(httpContextAccessor), "HTTP上下文访问器不能为空");
                _departmentRepository = dbRepository ??
                    throw new ArgumentNullException(nameof(dbRepository), "部门数据仓储不能为空");

                // 多租户会用到这init代码，其他情况可以不用
                // base.Init(dbRepository);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "部门服务初始化时发生严重异常：{ErrorMessage}，异常类型：{ExceptionType}，StackTrace: {StackTrace}",
                    ex.Message, ex.GetType().Name, ex.StackTrace);
                throw new InvalidOperationException($"创建部门服务实例失败: {ex.Message}", ex);
            }
        }
        #endregion

        #region 查询相关方法
        /// <summary>
        /// 获取分页数据，应用数据权限过滤
        /// </summary>
        /// <param name="options">分页参数</param>
        /// <returns>分页数据结果</returns>
        /// <exception cref="ArgumentNullException">当options为空时抛出</exception>
        /// <exception cref="InvalidOperationException">当数据库操作失败时抛出</exception>
        public override PageGridData<Sys_Department> GetPageData(PageDataOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options), "分页参数不能为空");
            }

            try
            {
                ApplyDataFilter();
                return base.GetPageData(options);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "获取部门分页数据时发生数据库异常：{ErrorMessage}，UserId: {UserId}，异常类型：{ExceptionType}，StackTrace: {StackTrace}",
                    ex.Message, UserContext.Current.UserId,ex.GetType().Name, ex.StackTrace);
                throw new InvalidOperationException(ERROR_DATABASE_OPERATION_FAILED, ex);
            }
        }

        /// <summary>
        /// 应用数据权限过滤
        /// 限制只能看自己部门及下级组织的数据
        /// </summary>
        private void ApplyDataFilter()
        {
            try
            {
                if (UserContext.Current == null)
                {
                    throw new InvalidOperationException(ERROR_INVALID_OPERATION_CONTEXT);
                }

                QueryRelativeExpression = (IQueryable<Sys_Department> queryable) =>
                {
                    try
                    {
                        if (UserContext.Current.IsSuperAdmin)
                        {
                            return queryable;
                        }

                        var deptIds = UserContext.Current.GetAllChildrenDeptIds();
                        if (deptIds == null || !deptIds.Any())
                        {
                            return queryable.Where(x => false); // 返回空结果
                        }

                        return queryable.Where(x => deptIds.Contains(x.DepartmentId));
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "数据权限过滤表达式执行异常：{ErrorMessage}，UserId: {UserId}，异常类型：{ExceptionType}，StackTrace: {StackTrace}",
                            ex.Message, UserContext.Current.UserId , ex.GetType().Name, ex.StackTrace);
                        throw;
                    }
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "设置数据权限过滤时发生严重异常：{ErrorMessage}，UserId: {UserId}，异常类型：{ExceptionType}，StackTrace: {StackTrace}",
                    ex.Message, UserContext.Current?.UserId , ex.GetType().Name, ex.StackTrace);
                throw;
            }
        }
        #endregion

        #region 导出方法
        /// <summary>
        /// 导出数据，应用数据权限过滤
        /// </summary>
        /// <param name="pageData">分页数据参数</param>
        /// <returns>导出结果</returns>
        /// <exception cref="ArgumentNullException">当pageData为空时抛出</exception>
        /// <exception cref="InvalidOperationException">当导出失败时抛出</exception>
        public override WebResponseContent Export(PageDataOptions pageData)
        {
            if (pageData == null)
            {
                throw new ArgumentNullException(nameof(pageData), "分页参数不能为空");
            }

            try
            {
                ApplyDataFilter();
                return base.Export(pageData);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "部门数据导出时发生异常：{ErrorMessage}，UserId: {UserId}，异常类型：{ExceptionType}，StackTrace: {StackTrace}",
                    ex.Message, UserContext.Current?.UserId ,
                     ex.GetType().Name, ex.StackTrace);
                throw new InvalidOperationException("导出数据失败", ex);
            }
        }
        #endregion

        #region 增删改方法
        /// <summary>
        /// 添加部门
        /// </summary>
        /// <param name="saveDataModel">保存数据模型</param>
        /// <returns>操作结果</returns>
        /// <exception cref="ArgumentNullException">当saveDataModel为空时抛出</exception>
        /// <exception cref="InvalidOperationException">当添加失败时抛出</exception>
        public override WebResponseContent Add(SaveModel saveDataModel)
        {
            if (saveDataModel == null)
            {
                throw new ArgumentNullException(nameof(saveDataModel), "保存数据模型不能为空");
            }

            try
            {
                AddOnExecuting = (Sys_Department dept, object list) =>
                {
                    try
                    {
                        if (dept == null)
                        {
                            return new WebResponseContent().Error("部门信息不能为空");
                        }

                        // 这里可以添加更多的业务验证逻辑
                        return new WebResponseContent().OK();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "添加部门验证时发生异常：{ErrorMessage}，UserId: {UserId}，部门信息: {DeptInfo}，异常类型：{ExceptionType}，StackTrace: {StackTrace}",
                            ex.Message, UserContext.Current?.UserId ,
                            new { dept?.DepartmentName, dept?.DepartmentId, dept?.ParentId }, ex.GetType().Name, ex.StackTrace);
                        return new WebResponseContent().Error($"验证失败：{ex.Message}");
                    }
                };

                return base.Add(saveDataModel).Reload();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "添加部门时发生数据库异常：{ErrorMessage}，UserId: {UserId}，保存模型: {SaveModel}，异常类型：{ExceptionType}，StackTrace: {StackTrace}",
                    ex.Message, UserContext.Current?.UserId ,
                     ex.GetType().Name, ex.StackTrace);
                throw new InvalidOperationException("添加部门失败", ex);
            }
        }

        /// <summary>
        /// 更新部门信息
        /// </summary>
        /// <param name="saveModel">保存数据模型</param>
        /// <returns>操作结果</returns>
        /// <exception cref="ArgumentNullException">当saveModel为空时抛出</exception>
        /// <exception cref="InvalidOperationException">当更新失败时抛出</exception>
        public override WebResponseContent Update(SaveModel saveModel)
        {
            if (saveModel == null)
            {
                throw new ArgumentNullException(nameof(saveModel), "保存数据模型不能为空");
            }

            try
            {
                UpdateOnExecuting = (Sys_Department dept, object addList, object updateList, List<object> delKeys) =>
                {
                    return ValidateDepartmentHierarchy(dept);
                };

                return base.Update(saveModel).Reload();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "更新部门时发生数据库异常：{ErrorMessage}，UserId: {UserId}，保存模型: {SaveModel}，异常类型：{ExceptionType}，StackTrace: {StackTrace}",
                    ex.Message, UserContext.Current?.UserId ,
                    ex.GetType().Name, ex.StackTrace);
                throw new InvalidOperationException("更新部门失败", ex);
            }
        }

        /// <summary>
        /// 验证部门层级关系
        /// </summary>
        /// <param name="dept">部门信息</param>
        /// <returns>验证结果</returns>
        private WebResponseContent ValidateDepartmentHierarchy(Sys_Department dept)
        {
            try
            {
                if (dept == null)
                {
                    return new WebResponseContent().Error("部门信息不能为空");
                }

                // 检查是否选择自己作为上级组织
                if (dept.ParentId.HasValue && dept.ParentId.Value == dept.DepartmentId)
                {
                    return new WebResponseContent().Error(ERROR_PARENT_CANNOT_BE_SELF);
                }

                // 检查是否会形成循环引用
                if (dept.ParentId.HasValue && HasCircularReference(dept.DepartmentId, dept.ParentId))
                {
                    return new WebResponseContent().Error(ERROR_INVALID_PARENT_SELECTION);
                }

                return new WebResponseContent().OK();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "验证部门层级关系时发生异常：{ErrorMessage}，部门信息: {DeptInfo}，UserId: {UserId}，异常类型：{ExceptionType}，StackTrace: {StackTrace}",
                    ex.Message, new { dept?.DepartmentName, dept?.DepartmentId, dept?.ParentId },
                    UserContext.Current?.UserId , ex.GetType().Name, ex.StackTrace);
                return new WebResponseContent().Error($"验证失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 检测循环引用（GUID版本）
        /// </summary>
        private bool HasCircularReference(Guid currentDeptId, Guid? parentId)
        {
            // 若父部门为空，直接无循环
            if (parentId == null || parentId == Guid.Empty)
                return false;

            //var visited = new HashSet<Guid> { currentDeptId };
            //var currentId = parentId;

            //while (currentId != Guid.Empty)
            //{
            //    if (visited.Contains(currentId))
            //        return true;

            //    visited.Add(currentId);

            //    // 获取当前部门的父部门ID
            //    currentId = _departmentRepository
            //        .FindAsIQueryable(x => x.DepartmentId == currentId)
            //        .Select(x => x.ParentId ?? Guid.Empty)
            //        .FirstOrDefault();
            //}

            //return false;
            // 使用CTE一次性获取所有祖先部门的父ID（包含当前父部门）
            var ancestorParentIds = _departmentRepository
                .FromSqlInterpolated($@"
            WITH DepartmentHierarchy AS (
                -- 初始查询：当前部门的父部门
                SELECT ParentId
                FROM Departments
                WHERE DepartmentId = {parentId}
                
                UNION ALL
                
                -- 递归查询：向上查找父部门
                SELECT d.ParentId
                FROM Departments d
                INNER JOIN DepartmentHierarchy dh 
                    ON d.DepartmentId = dh.ParentId
                WHERE dh.ParentId != {Guid.Empty}  -- 终止条件：父ID为空时停止
            )
            SELECT ParentId FROM DepartmentHierarchy
            OPTION (MAXRECURSION 100)  -- 限制最大递归层级（根据业务调整）
        ")
                .Select(x => x.ParentId)
                .ToList();

            // 检查祖先链中是否包含当前部门ID（形成循环）
            return ancestorParentIds.Contains(currentDeptId);
        }

       

        /// <summary>
        /// 删除部门
        /// </summary>
        /// <param name="keys">要删除的部门ID数组</param>
        /// <param name="delList">是否删除关联数据</param>
        /// <returns>操作结果</returns>
        /// <exception cref="ArgumentNullException">当keys为空时抛出</exception>
        /// <exception cref="InvalidOperationException">当删除失败时抛出</exception>
        public override WebResponseContent Del(object[] keys, bool delList = true)
        {
            if (keys == null || keys.Length == 0)
            {
                throw new ArgumentNullException(nameof(keys), "删除键数组不能为空");
            }

            try
            {
                return base.Del(keys, delList).Reload();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "删除部门时发生数据库异常：{ErrorMessage}，UserId: {UserId}，删除键: {Keys}，删除数量: {Count}，删除关联: {DelList}，异常类型：{ExceptionType}，StackTrace: {StackTrace}",
                    ex.Message, UserContext.Current.UserId, string.Join(",", keys), keys.Length, delList, ex.GetType().Name, ex.StackTrace);
                throw new InvalidOperationException("删除部门失败", ex);
            }
        }
        #endregion
    }
}