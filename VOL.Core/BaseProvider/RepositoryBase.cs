using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using VOL.Core.Dapper;
using VOL.Core.DBManager;
using VOL.Core.EFDbContext;
using VOL.Core.Enums;
using VOL.Core.Extensions;
using VOL.Core.Utilities;
using VOL.Entity;
using VOL.Entity.SystemModels;

namespace VOL.Core.BaseProvider
{
    public abstract class RepositoryBase<TEntity> where TEntity : BaseEntity, new() // 添加new()约束以便Activator.CreateInstance
    {
        // 注意: 如果使用默认构造函数，DefaultDbContext初始为null。
        // 后续通过EFContext属性访问时，会尝试通过DBServerProvider.GetDbContextConnection进行初始化。
        // 这种延迟初始化机制需要确保在使用DbContext之前它已经被正确设置。
        // 考虑将DefaultDbContext声明为 VOLContext? 并处理潜在的null，或者确保构造函数注入。
        public RepositoryBase()
        {
            // DefaultDbContext 在这里是 null，依赖后续的 EFContext getter 来初始化。
            // 如果直接访问 DefaultDbContext 或 DbContext 属性而未先触发 EFContext getter, 可能导致 NullReferenceException。
            // 为了更安全，可以考虑强制构造函数注入，或者在 EFContext/DbContext getter 中进行更严格的 null 检查和初始化。
            // 本次修改中，我们假设 DBServerProvider.GetDbContextConnection 会正确处理 DefaultDbContext 的初始化。
        }
        public RepositoryBase(VOLContext dbContext)
        {
            // 通过构造函数注入的DbContext被认为是可靠的非null。
            this.DefaultDbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext), "dbContext未实例化。");
        }

        private VOLContext? DefaultDbContext { get; set; } // 标记为可空，尽管构造函数注入版会确保非空

        /// <summary>
        /// 获取当前实体类型的EF Core上下文。
        /// 如果DefaultDbContext为null（例如通过默认构造函数创建RepositoryBase实例后首次访问），
        /// 会尝试通过DBServerProvider动态获取或创建DbContext实例并赋值给DefaultDbContext。
        /// </summary>
        private VOLContext EFContext
        {
            get
            {
                // 如果DefaultDbContext为null，GetDbContextConnection应该负责初始化它。
                // 如果初始化失败或未正确处理，DefaultDbContext仍可能为null，导致后续操作失败。
                DBServerProvider.GetDbContextConnection<TEntity>(DefaultDbContext);
                // 经过GetDbContextConnection后，假定DefaultDbContext已有效。
                // 如果DefaultDbContext在此处仍可能为null，则需要抛出异常或有更强的处理机制。
                return DefaultDbContext!; // 使用null包容运算符，因为我们信任GetDbContextConnection会处理好。 // 确保 DefaultDbContext 在这里已初始化
            }
        }

        /// <summary>
        /// 获取当前仓储操作的DbContext实例。
        /// 警告：如果通过默认构造函数创建RepositoryBase且DBServerProvider未能正确初始化DefaultDbContext，
        /// 此属性可能返回null，导致调用方出现NullReferenceException。
        /// </summary>
        public virtual VOLContext DbContext
        {
            // 确保DefaultDbContext在使用前被正确初始化 (通常通过EFContext的getter)
            // 如果直接访问此属性而DefaultDbContext为null，将返回null。
            // 调用者需要注意处理潜在的null情况，或者确保EFContext已被调用。
            get { return DefaultDbContext!; } // 假设它在被使用时已经被EFContext getter初始化了
        }
        private DbSet<TEntity> DBSet
        {
            get { return EFContext.Set<TEntity>(); } // EFContext getter会确保DbContext初始化
        }
        public ISqlDapper DapperContext
        {
            // 假设 GetSqlDapper 总是返回一个有效的 ISqlDapper 实例
            get { return DBServerProvider.GetSqlDapper<TEntity>(); }
        }
        /// <summary>
        /// 执行事务
        /// </summary>
        /// <param name="action">如果返回false则回滚事务(可自行定义规则)</param>
        /// <returns></returns>
        public virtual WebResponseContent DbContextBeginTransaction(Func<WebResponseContent> action)
        {
            WebResponseContent webResponse = new WebResponseContent();
            using (IDbContextTransaction transaction = DefaultDbContext.Database.BeginTransaction())
            {
                try
                {
                    webResponse = action();
                    if (webResponse.Status)
                    {
                        transaction.Commit();
                    }
                    else
                    {
                        transaction.Rollback();
                    }

                    return webResponse;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return new WebResponseContent().Error(ex.Message);
                }
            }
        }

        public virtual bool Exists<TExists>(Expression<Func<TExists, bool>> predicate) where TExists : class
        {
            return EFContext.Set<TExists>().Any(predicate);
        }

        public virtual Task<bool> ExistsAsync<TExists>(Expression<Func<TExists, bool>> predicate) where TExists : class
        {
            return EFContext.Set<TExists>().AnyAsync(predicate);
        }

        public virtual bool Exists(Expression<Func<TEntity, bool>> predicate)
        {
            return DBSet.Any(predicate);
        }

        public virtual Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return DBSet.AnyAsync(predicate);
        }
        public virtual List<TFind> Find<TFind>(Expression<Func<TFind, bool>> predicate) where TFind : class
        {
            return EFContext.Set<TFind>().Where(predicate).ToList();
        }

        public virtual Task<TFind?> FindAsyncFirst<TFind>(Expression<Func<TFind, bool>> predicate) where TFind : class
        {
            return FindAsIQueryable<TFind>(predicate).FirstOrDefaultAsync();
        }

        public virtual Task<TEntity?> FindAsyncFirst(Expression<Func<TEntity, bool>> predicate)
        {
            return FindAsIQueryable<TEntity>(predicate).FirstOrDefaultAsync();
        }

        public virtual Task<List<TFind>> FindAsync<TFind>(Expression<Func<TFind, bool>> predicate) where TFind : class
        {
            return FindAsIQueryable<TFind>(predicate).ToListAsync();
        }

        public virtual Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return FindAsIQueryable(predicate).ToListAsync();
        }

        public virtual Task<TEntity?> FindFirstAsync(Expression<Func<TEntity, bool>> predicate) // 返回类型可以是null
        {
            return FindAsIQueryable(predicate).FirstOrDefaultAsync();
        }

        public virtual Task<List<T>> FindAsync<T>(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, T>> selector)
        {
            return FindAsIQueryable(predicate).Select(selector).ToListAsync();
        }

        public virtual Task<T?> FindFirstAsync<T>(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, T>> selector) // 返回类型可以是null
        {
            return FindAsIQueryable(predicate).Select(selector).FirstOrDefaultAsync();
        }

        public virtual IQueryable<TFind> FindAsIQueryable<TFind>(Expression<Func<TFind, bool>> predicate) where TFind : class
        {
            return EFContext.Set<TFind>().Where(predicate);
        }

        public virtual List<TEntity> Find<Source>(IEnumerable<Source> sources,
            Func<Source, Expression<Func<TEntity, bool>>> predicate)
            where Source : class
        {
            return FindAsIQueryable(sources, predicate).ToList();
        }
        public virtual List<TResult> Find<Source, TResult>(IEnumerable<Source> sources,
              Func<Source, Expression<Func<TEntity, bool>>> predicate,
              Expression<Func<TEntity, TResult>> selector)
              where Source : class
        {
            return FindAsIQueryable(sources, predicate).Select(selector).ToList();
        }

        /// <summary>
        /// 多条件查询
        /// </summary>
        /// <typeparam name="Source"></typeparam>
        /// <param name="sources"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public virtual IQueryable<TEntity> FindAsIQueryable<Source>(IEnumerable<Source> sources,
            Func<Source, Expression<Func<TEntity, bool>>> predicate) // predicate可能为null，调用者应保证其有效性
            where Source : class
        {
            // EFContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;
            Expression<Func<TEntity, bool>> resultPredicate = x => 1 == 2; // 初始谓词为false
            foreach (Source source in sources)
            {
                Expression<Func<TEntity, bool>> expression = predicate(source);
                resultPredicate = resultPredicate.Or<TEntity>(expression); // Or扩展方法需要保证expression不为null
            }
            return EFContext.Set<TEntity>().Where(resultPredicate);
        }

        public virtual List<T> Find<T>(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, T>> selector)
        {
            return DBSet.Where(predicate).Select(selector).ToList();
        }
        /// <summary>
        /// 单表查询
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public virtual List<TEntity> Find(Expression<Func<TEntity, bool>> predicate)
        {
            return FindAsIQueryable(predicate).ToList();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="orderBy">排序字段,可以为null</param>
        /// <returns>返回单个实体，可能为null</returns>
        public virtual TEntity? FindFirst(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, Dictionary<object, QueryOrderBy>>>? orderBy = null)
        {
            return FindAsIQueryable(predicate, orderBy).FirstOrDefault();
        }


        public IQueryable<TEntity> FindAsIQueryable(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, Dictionary<object, QueryOrderBy>>>? orderBy = null)
        {
            var query = DbContext.Set<TEntity>().Where(predicate);
            if (orderBy != null)
            {
                // GetExpressionToDic() 需要保证orderBy表达式有效，或者内部处理null情况
                query = query.GetIQueryableOrderBy(orderBy.GetExpressionToDic());
            }
            return query;
        }

        public IIncludableQueryable<TEntity, TProperty> Include<TProperty>(Expression<Func<TEntity, TProperty>> incluedProperty)
        {
            return DbContext.Set<TEntity>().Include(incluedProperty);
        }

        /// <summary>
        /// 通过条件查询返回指定列的数据(将TEntity映射到匿名或实体T)
        ///var result = Sys_UserRepository.GetInstance.Find(x => x.UserName == loginInfo.userName, p => new { uname = p.UserName });
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="pageIndex"></param>
        /// <param name="pagesize"></param>
        /// <param name="rowcount"></param>
        /// <param name="predicate">查询条件,可以为null，如果为null则查询所有</param>
        /// <param name="orderBy">排序字段</param>
        /// <returns></returns>
        public virtual IQueryable<TFind> IQueryablePage<TFind>(int pageIndex, int pagesize, out int rowcount, Expression<Func<TFind, bool>>? predicate, Expression<Func<TEntity, Dictionary<object, QueryOrderBy>>> orderBy, bool returnRowCount = true) where TFind : class
        {
            pageIndex = pageIndex <= 0 ? 1 : pageIndex;
            pagesize = pagesize <= 0 ? 10 : pagesize;
            // 如果 predicate 为 null, 则设定为 true 以便查询所有记录。
            // 调用者应明确此行为，或者传入一个有效的 predicate。
            Expression<Func<TFind, bool>> queryPredicate = predicate ?? (x => true);

            var _db = DbContext.Set<TFind>();
            rowcount = returnRowCount ? _db.Count(queryPredicate) : 0;
            // GetExpressionToDic() 需要保证orderBy表达式有效
            return DbContext.Set<TFind>().Where(queryPredicate)
                .GetIQueryableOrderBy(orderBy.GetExpressionToDic())
                .Skip((pageIndex - 1) * pagesize)
                .Take(pagesize);
        }

        /// <summary>
        /// 分页排序
        /// </summary>
        /// <param name="queryable"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pagesize"></param>
        /// <param name="rowcount"></param>
        /// <param name="orderBy">排序字典，key为排序字段，value为排序方式</param>
        /// <returns></returns>
        public virtual IQueryable<TEntity> IQueryablePage(IQueryable<TEntity> queryable, int pageIndex, int pagesize, out int rowcount, Dictionary<string, QueryOrderBy> orderBy, bool returnRowCount = true)
        {
            pageIndex = pageIndex <= 0 ? 1 : pageIndex;
            pagesize = pagesize <= 0 ? 10 : pagesize;
            rowcount = returnRowCount ? queryable.Count() : 0;
            // GetIQueryableOrderBy 需要保证orderBy字典有效
            return queryable.GetIQueryableOrderBy<TEntity>(orderBy)
                .Skip((pageIndex - 1) * pagesize)
                .Take(pagesize);
        }

        public virtual List<TResult> QueryByPage<TResult>(int pageIndex, int pagesize, out int rowcount, Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, Dictionary<object, QueryOrderBy>>> orderBy, Expression<Func<TEntity, TResult>> selectorResult, bool returnRowCount = true)
        {
            return IQueryablePage<TEntity>(pageIndex, pagesize, out rowcount, predicate, orderBy, returnRowCount).Select(selectorResult).ToList();
        }

        public List<TEntity> QueryByPage(int pageIndex, int pagesize, out int rowcount, Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, Dictionary<object, QueryOrderBy>>> orderBy, bool returnRowCount = true)
        {
            return IQueryablePage<TEntity>(pageIndex, pagesize, out rowcount, predicate, orderBy).ToList();
        }

        public virtual List<TResult> QueryByPage<TResult>(int pageIndex, int pagesize, Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, Dictionary<object, QueryOrderBy>>> orderBy, Expression<Func<TEntity, TResult>>? selectorResult = null)
        {
            // selectorResult 为 null 时会抛出异常，需要调用者保证其不为 null。
            // 或者在此处进行判断和处理。
            if (selectorResult == null) throw new ArgumentNullException(nameof(selectorResult), "Selector result cannot be null.");
            return IQueryablePage<TEntity>(pageIndex, pagesize, out int rowcount, predicate, orderBy).Select(selectorResult).ToList();
        }


        /// <summary>
        /// 更新表数据
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="saveChanges">是否保存</param>
        /// <param name="properties">格式 Expression<Func<entityt, object>> expTree = x => new { x.字段1, x.字段2 }; 可以为null，表示更新所有映射字段</param>
        public virtual int Update(TEntity entity, Expression<Func<TEntity, object>>? properties, bool saveChanges = false)
        {
            return Update<TEntity>(entity, properties, saveChanges);
        }

        public virtual int Update<TSource>(TSource entity, Expression<Func<TSource, object>>? properties, bool saveChanges = false) where TSource : class
        {
            return UpdateRange(new List<TSource>
            {
                entity
            }, properties, saveChanges);
        }


        public virtual int Update<TSource>(TSource entity, string[]? properties, bool saveChanges = false) where TSource : class
        {
            return UpdateRange<TSource>(new List<TSource>() { entity }, properties, saveChanges);
        }
        public virtual int Update<TSource>(TSource entity, bool saveChanges = false) where TSource : class
        {
            return UpdateRange<TSource>(new List<TSource>() { entity }, new string[0], saveChanges);
        }
        public virtual int UpdateRange<TSource>(IEnumerable<TSource> entities, Expression<Func<TSource, object>>? properties, bool saveChanges = false) where TSource : class
        {
            return UpdateRange<TSource>(entities, properties?.GetExpressionProperty(), saveChanges);
        }
        public virtual int UpdateRange<TSource>(IEnumerable<TSource> entities, bool saveChanges = false) where TSource : class
        {
            return UpdateRange<TSource>(entities, new string[0], saveChanges);
        }

        /// <summary>
        /// 更新表数据
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="properties">要更新的属性列表，如果为null或空，则更新所有映射字段</param>
        public int UpdateRange<TSource>(IEnumerable<TSource> entities, string[]? properties, bool saveChanges = false) where TSource : class
        {
            string[]? effectiveProperties = properties; // // 使用局部变量来存储可能修改的属性列表
            if (effectiveProperties != null && effectiveProperties.Length > 0)
            {
                PropertyInfo[] entityPropertyInfos = typeof(TSource).GetProperties()
                     .Where(x => x.GetCustomAttribute<NotMappedAttribute>() == null && !(x.PropertyType.IsGenericType && x.PropertyType.GetGenericTypeDefinition() == typeof(List<>))).ToArray();
                string? keyName = entityPropertyInfos.GetKeyName(); // GetKeyName() 可能返回 null
                if (keyName != null && effectiveProperties.Contains(keyName))
                {
                    effectiveProperties = effectiveProperties.Where(x => x != keyName).ToArray();
                }
                // 确保只更新实际存在的属性
                effectiveProperties = effectiveProperties.Where(x => entityPropertyInfos.Select(s => s.Name).Contains(x)).ToArray();
            }

            foreach (TSource item in entities)
            {
                // 如果没有指定属性或列表为空，则标记整个实体为已修改
                if (effectiveProperties == null || effectiveProperties.Length == 0)
                {
                    DbContext.Entry<TSource>(item).State = EntityState.Modified;
                    continue;
                }
                // 否则，只标记指定属性为已修改
                var entry = DbContext.Entry(item);
                foreach (var propName in effectiveProperties)
                {
                    entry.Property(propName).IsModified = true;
                }
            }
            if (!saveChanges) return 0;

            //2020.04.24增加更新时并行重试处理
            try
            {
                // Attempt to save changes to the database
                return DbContext.SaveChanges();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                int affectedRows = 0;
                foreach (var entry in ex.Entries)
                {
                    var proposedValues = entry.CurrentValues;
                    var databaseValues = entry.GetDatabaseValues();

                    //databaseValues == null说明数据已被删除
                    if (databaseValues != null)
                    {
                        // 根据 effectiveProperties 决定比较哪些属性
                        var propertiesToCompare = (effectiveProperties == null || effectiveProperties.Length == 0)
                            ? proposedValues.Properties
                            : proposedValues.Properties.Where(x => effectiveProperties.Contains(x.Name));

                        foreach (var property in propertiesToCompare)
                        {
                            // 此处原代码仅获取值但未使用，可移除或添加实际冲突处理逻辑
                            // var proposedValue = proposedValues[property];
                            // var databaseValue = databaseValues[property];
                        }
                        affectedRows++;
                        entry.OriginalValues.SetValues(databaseValues); // 使用数据库当前值覆盖原始值以解决并发冲突
                    }
                }
                if (affectedRows == 0 && ex.Entries.Any(x => x.GetDatabaseValues() == null))
                {
                     // 如果所有冲突都是因为记录已被删除，则返回0行受影响
                    return 0;
                }
                // 重试保存，如果仍然失败，异常将冒泡
                return DbContext.SaveChanges();
            }
        }




        /// <summary>
        ///
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="updateDetail">是否修改明细</param>
        /// <param name="delNotExist">是否删除明细不存在的数据</param>
        /// <param name="updateMainFields">主表指定修改字段,可以为null</param>
        /// <param name="updateDetailFields">明细指定修改字段,可以为null</param>
        /// <param name="saveChange">是否保存</param>
        /// <returns></returns>
        public virtual WebResponseContent UpdateRange<Detail>(TEntity entity,
            bool updateDetail = false,
            bool delNotExist = false,
            Expression<Func<TEntity, object>>? updateMainFields = null,
            Expression<Func<Detail, object>>? updateDetailFields = null,
            bool saveChange = false) where Detail : class, new() // Detail需要new()约束
        {
            WebResponseContent webResponse = new WebResponseContent();
            Update(entity, updateMainFields); // 更新主表
            string message = "";
            if (updateDetail)
            {
                string detailTypeName = typeof(List<Detail>).FullName!; // FullName 不会是 null
                PropertyInfo[] properties = typeof(TEntity).GetProperties();
                PropertyInfo? detailPropertyInfo = properties.FirstOrDefault(x => x.PropertyType.FullName == detailTypeName); // 可能为null

                if (detailPropertyInfo != null)
                {
                    PropertyInfo? keyPropertyInfo = properties.GetKeyProperty(); // GetKeyProperty可能返回null
                    if (keyPropertyInfo == null) throw new InvalidOperationException("主表实体未找到主键属性。");

                    object? detailListObj = detailPropertyInfo.GetValue(entity);
                    // Type? detailEntityType = typeof(TEntity).GetCustomAttribute<EntityAttribute>()?.DetailTable?[0]; // DetailTable可能为null或空
                    // if (detailEntityType == null) throw new InvalidOperationException("实体的DetailTable特性未正确配置。");

                    // UpdateDetail的逻辑需要保证detailListObj不为null
                    message = UpdateDetail<Detail>(detailListObj as List<Detail>, keyPropertyInfo.Name, keyPropertyInfo.GetValue(entity)!, updateDetailFields, delNotExist);
                }
            }
            if (!saveChange) return webResponse.OK();

            DbContext.SaveChanges();
            return webResponse.OK("修改成功,明细" + message, entity);
        }
        private string UpdateDetail<TDetail>(List<TDetail>? list, // list可以为null
            string keyName, // 主表外键字段名
            object keyValue, // 主表外键值
            Expression<Func<TDetail, object>>? updateDetailFields = null,
            bool delNotExist = false) where TDetail : class, new() // TDetail需要new()约束
        {
            if (list == null || !list.Any()) // 如果传入的list为null或空，则不进行任何操作或按需删除现有明细
            {
                 if (delNotExist && keyValue != null) // 如果需要删除不存在的明细且主键值有效
                 {
                    Expression<Func<TDetail, bool>> whereExpression = keyName.CreateExpression<TDetail>(keyValue, LinqExpressionType.Equal);
                    var existingDetails = DbContext.Set<TDetail>().Where(whereExpression).ToList();
                    if (existingDetails.Any())
                    {
                        DbContext.Set<TDetail>().RemoveRange(existingDetails);
                        return $"删除[{existingDetails.Count}]条原有明细";
                    }
                 }
                 return "没有明细数据需要处理";
            }

            PropertyInfo? detailKeyPropertyInfo = typeof(TDetail).GetKeyProperty(); // 明细表主键属性
            if (detailKeyPropertyInfo == null) throw new InvalidOperationException("明细实体未找到主键属性。");
            string detailKeyName = detailKeyPropertyInfo.Name;

            DbSet<TDetail> detailsDbSet = DbContext.Set<TDetail>();
            Expression<Func<TDetail, object>> selectExpression = detailKeyName.GetExpression<TDetail, object>();
            Expression<Func<TDetail, bool>> whereExpression = keyName.CreateExpression<TDetail>(keyValue, LinqExpressionType.Equal); // 根据主表外键值查询现有明细

            List<object> existingDetailKeys = detailsDbSet.Where(whereExpression).Select(selectExpression).ToList();

            // 获取明细表主键的默认值，用于判断是新增还是修改
            object? keyDefaultValObj = detailKeyPropertyInfo.PropertyType.IsValueType ? Activator.CreateInstance(detailKeyPropertyInfo.PropertyType) : null;
            string keyDefaultVal = keyDefaultValObj?.ToString() ?? "";


            int addCount = 0;
            int editCount = 0;
            int delCount = 0;
            PropertyInfo? mainKeyPropertyInDetail = typeof(TDetail).GetProperty(keyName); // 明细表中对应主表外键的属性
            if (mainKeyPropertyInDetail == null) throw new InvalidOperationException($"明细实体中未找到名为'{keyName}'的主表外键属性。");

            List<object> currentDetailKeys = new List<object>(); // 存储当前提交的明细数据的主键

            foreach (var item in list)
            {
                object? val = detailKeyPropertyInfo.GetValue(item);
                currentDetailKeys.Add(val!); // 假定val不为null，因为它是主键

                // 主键是默认值的为新增的数据
                if (val == null || val.ToString() == keyDefaultVal || Convert.ToInt64(val) == 0) // Convert.ToInt64(val)==0 适用于int/long类型主键的默认新增值判断
                {
                    item.SetCreateDefaultVal(); // 设置创建相关的默认字段值
                    mainKeyPropertyInDetail.SetValue(item, keyValue); // 设置明细的外键值
                    detailsDbSet.Add(item);
                    addCount++;
                }
                else // 修改的数据
                {
                    item.SetModifyDefaultVal(); // 设置修改相关的默认字段值
                    Update<TDetail>(item, updateDetailFields); // 调用Update方法标记修改
                    editCount++;
                }
            }

            // 删除数据库中存在但当前提交数据中已不存在的明细记录
            if (delNotExist)
            {
                List<object> keysToDelete = existingDetailKeys.Where(x => !currentDetailKeys.Contains(x)).ToList();
                foreach (var keyToDelete in keysToDelete)
                {
                    TDetail detailInstance = new TDetail(); // 使用new()约束
                    detailKeyPropertyInfo.SetValue(detailInstance, keyToDelete);
                    DbContext.Entry<TDetail>(detailInstance).State = EntityState.Deleted;
                    delCount++;
                }
            }
            return $"修改[{editCount}]条,新增[{addCount}]条,删除[{delCount}]条";
        }

        public virtual void Delete(TEntity model, bool saveChanges)
        {
            DBSet.Remove(model);
            if (saveChanges)
            {
                DbContext.SaveChanges();
            }
        }
        /// <summary>
        /// 通过主键批量删除
        /// </summary>
        /// <param name="keys">主键key</param>
        /// <param name="delList">是否连明细一起删除</param>
        /// <returns></returns>
        public virtual int DeleteWithKeys(object[] keys, bool delList = false)
        {
            Type entityType = typeof(TEntity);
            PropertyInfo? keyProperty = entityType.GetKeyProperty();
            if (keyProperty == null) throw new InvalidOperationException("实体未找到主键属性。");
            string tKey = keyProperty.Name;

            // 对于字符串类型的主键，直接使用RemoveRange进行批量删除效率可能更高，且能避免SQL注入风险
            if (keyProperty.PropertyType == typeof(string))
            {
                List<TEntity> listToDelete = new List<TEntity>();
                foreach (var key in keys.Distinct())
                {
                    var entity = new TEntity(); // 利用 new() 约束
                    keyProperty.SetValue(entity, key);
                    listToDelete.Add(entity);
                }
                DbContext.RemoveRange(listToDelete);
                // 如果需要删除明细，需要额外逻辑处理
                if (delList) { /* TODO: 实现明细删除逻辑, 可能需要查询明细并RemoveRange */ }
                return DbContext.SaveChanges();
            }

            // 对于数值类型主键，拼接SQL仍然是选项之一，但需注意SQL注入（尽管这里是object[]，但仍需谨慎）
            // EF Core 6+ 也支持 ExecuteDeleteAsync，是更安全的选择
            FieldType fieldType = entityType.GetFieldType(); // GetFieldType() 扩展方法
            string joinKeys = (fieldType == FieldType.Int || fieldType == FieldType.BigInt)
                 ? string.Join(",", keys)
                 : $"'{string.Join("','", keys)}'"; // 注意SQL注入风险

            string sql = $"DELETE FROM {entityType.GetEntityTableName()} WHERE {tKey} IN ({joinKeys});";
            if (delList)
            {
                EntityAttribute? entityAttribute = entityType.GetCustomAttribute<EntityAttribute>();
                if (entityAttribute?.DetailTable != null && entityAttribute.DetailTable.Length > 0)
                {
                    Type? detailType = entityAttribute.DetailTable[0];
                    if (detailType != null)
                        sql = sql + $"DELETE FROM {detailType.GetEntityTableName()} WHERE {tKey} IN ({joinKeys});";
                }
            }
            return ExecuteSqlCommand(sql);
        }
        public virtual int Delete([NotNull] Expression<Func<TEntity, bool>> wheres, bool saveChange = false)
        {
            return Delete<TEntity>(wheres, saveChange);
        }
        public virtual int Delete<T>([NotNull] Expression<Func<T, bool>> wheres, bool saveChange = false) where T : class, new() // 添加new()约束
        {
            PropertyInfo? keyProperty = typeof(T).GetKeyProperty();
            if (keyProperty == null) throw new InvalidOperationException("实体未找到主键属性。");
            string keyName = keyProperty.Name;

            var expression = keyName.GetExpression<T, object>();
            // 先查询出符合条件的实体ID
            var ids = DbContext.Set<T>().Where(wheres).Select(expression).ToList();
            if (!ids.Any()) return 0;

            List<T> listToDelete = new List<T>();
            foreach (var id in ids)
            {
                T entity = new T(); // 利用new()约束
                keyProperty.SetValue(entity, id);
                listToDelete.Add(entity);
            }
            DbContext.RemoveRange(listToDelete); // 批量标记删除

            if (saveChange)
            {
                return DbContext.SaveChanges();
            }
            return 0; // 如果不立即保存，返回0，表示未执行数据库操作
        }

        public virtual Task AddAsync(TEntity entities)
        {
            return DBSet.AddRangeAsync(entities);
        }

        public virtual Task AddRangeAsync(IEnumerable<TEntity> entities)
        {
            return DBSet.AddRangeAsync(entities);
        }

        public virtual void Add(TEntity entities, bool saveChanges = false)
        {
            AddRange(new List<TEntity>() { entities }, saveChanges);
        }
        public virtual void AddRange(IEnumerable<TEntity> entities, bool saveChanges = false)
        {
            DBSet.AddRange(entities);
            if (saveChanges) DbContext.SaveChanges();
        }

        public virtual void AddRange<T>(IEnumerable<T> entities, bool saveChanges = false)
            where T : class
        {
            DbContext.Set<T>().AddRange(entities);
            if (saveChanges) DbContext.SaveChanges();
        }

        /// <summary>
        /// 注意List生成的table的列顺序必须要和数据库表的列顺序一致
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entities"></param>
        public virtual void BulkInsert(IEnumerable<TEntity> entities, bool setOutputIdentity = false)
        {
            //  EFContext.Model.FindEntityType("").Relational()
            //Pomelo.EntityFrameworkCore.MySql
            try
            {
                // EFContext.BulkInsert(entities.ToList()); // BulkInsert 不是标准EF Core功能, 可能是第三方库
                // 如果使用的是第三方库，需要确保其与EF Core 8 和 Nullable Reference Types 兼容
                // 作为标准实现，可以考虑使用 Dapper 或 EF Core 7+ 的 ExecuteUpdate/ExecuteDelete
                 DapperContext.BulkInsert(typeof(TEntity).GetEntityTableName(), entities.ToList(), null, null, setOutputIdentity);
            }
            catch (DbUpdateException ex)
            {
                // throw (ex.InnerException as Exception ?? ex); // 抛出内部异常或原始异常
                 throw ex.InnerException ?? ex; // 简化
            }
            //  BulkInsert(entities.ToDataTable(), typeof(T).GetEntityTableName(), null);
        }

        public virtual int SaveChanges()
        {
            return EFContext.SaveChanges();
        }

        public virtual Task<int> SaveChangesAsync()
        {
            return EFContext.SaveChangesAsync();
        }

        public virtual int ExecuteSqlCommand(string sql, params SqlParameter[] sqlParameters)
        {
            // 执行原生SQL命令。sql字符串本身不应由用户输入直接构造。参数应通过 sqlParameters 传递。
            // EF Core 会将 sqlParameters 处理为数据库参数，防止SQL注入。
            return DbContext.Database.ExecuteSqlRaw(sql, sqlParameters);
        }

        public virtual List<TEntity> FromSql(string sql, params SqlParameter[] sqlParameters)
        {
            // 执行原生SQL查询并映射到实体列表。sql字符串本身不应由用户输入直接构造。参数应通过 sqlParameters 传递。
            // EF Core 会将 sqlParameters 处理为数据库参数，防止SQL注入。
            return DBSet.FromSqlRaw(sql, sqlParameters).ToList();
        }

        /// <summary>
        /// 执行sql
        /// 使用方式 FormattableString sql=$"select * from xx where name ={xx} and pwd={xx1} "，
        /// FromSqlInterpolated内部处理sql注入的问题，直接在{xx}写对应的值即可
        /// 注意：sql必须 select * 返回所有TEntity字段，
        /// </summary>
        /// <param name="sql">可格式化SQL字符串</param>
        /// <returns>一个IQueryable<TEntity>，允许进一步组合查询</returns>
        public virtual IQueryable<TEntity> FromSqlInterpolated([NotNull] FormattableString sql)
        {
            //DBSet.FromSqlInterpolated(sql).Select(x => new { x,xxx}).ToList();
            return DBSet.FromSqlInterpolated(sql);
        }

        /// <summary>
        /// 取消上下文跟踪
        /// </summary>
        /// <param name="entity"></param>
        public virtual void Detached(TEntity entity)
        {
            DbContext.Entry(entity).State = EntityState.Detached;
        }
        public virtual void DetachedRange(IEnumerable<TEntity> entities)
        {
            foreach (var entity in entities)
            {
                DbContext.Entry(entity).State = EntityState.Detached;
            }
        }

        /// <summary>
        /// 查询字段不为null或者为空
        /// </summary>
        /// <param name="field">x=>new {x.字段}</param>
        /// <param name="value">查询的值，如果为null或空则不应用此条件</param>
        /// <param name="linqExpression">查询类型</param>
        /// <returns></returns>
        public virtual IQueryable<TEntity> WhereIF([NotNull] Expression<Func<TEntity, object>> field, string? value, LinqExpressionType linqExpression = LinqExpressionType.Equal)
        {
            // LINQ 查询由EF Core转换为参数化SQL，防止SQL注入。
            // WhereNotEmpty 扩展方法需要检查，确保它能正确处理 nullable 'value'
            return EFContext.Set<TEntity>().WhereNotEmpty(field, value, linqExpression);
        }

        public virtual IQueryable<TEntity> WhereIF(bool checkCondition, Expression<Func<TEntity, bool>> predicate)
        {
            // LINQ 查询由EF Core转换为参数化SQL，防止SQL注入。
            if (checkCondition)
            {
                return EFContext.Set<TEntity>().Where(predicate);
            }
            return EFContext.Set<TEntity>(); // 返回整个集合，如果条件不满足
        }

        public virtual IQueryable<T> WhereIF<T>(bool checkCondition, Expression<Func<T, bool>> predicate) where T : class
        {
            // LINQ 查询由EF Core转换为参数化SQL，防止SQL注入。
            if (checkCondition)
            {
                return EFContext.Set<T>().Where(predicate);
            }
            return EFContext.Set<T>(); // 返回整个集合，如果条件不满足
        }
    }
}
