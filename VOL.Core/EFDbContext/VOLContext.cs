using EntityFrameworkCore.UseRowNumberForPaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using VOL.Core.Configuration;
using VOL.Core.Const;
using VOL.Core.DBManager;
using VOL.Core.Enums;
using VOL.Core.Extensions;
using VOL.Core.Extensions.AutofacManager;
using VOL.Entity.SystemModels;
using VOL.Entity.IBaseInterface; // // For ITenantEntity
using System.Linq.Expressions; // // For Expression
using VOL.Core.ManageUser; // // For UserContext
using Microsoft.EntityFrameworkCore.Diagnostics; // // Added for RelationalEventId and CommandExecutedEventData

namespace VOL.Core.EFDbContext
{
    public class VOLContext : DbContext, IDependency
    {
        /// <summary>
        /// 数据库连接名称 
        /// </summary>
        public string? DataBaseName = null; // // 标记为可空
        private readonly UserContext? _userContext; // // 注入UserContext以便访问TenantId

        public VOLContext()
                : base()
        {
             // _userContext 在此构造函数中将为 null。
             // 如果OnModelCreating依赖它，则需要确保它通过其他方式注入或可空处理。
             // 通常，无参数构造函数用于EF设计时工具，可能不需要UserContext。
        }
        public VOLContext(string connction)
            : base()
        {
            DataBaseName = connction;
            // _userContext 在此构造函数中也将为 null。
        }

        public VOLContext(DbContextOptions<VOLContext> options, UserContext? userContext) // // 注入UserContext
            : base(options)
        {
            _userContext = userContext;
        }
        public override void Dispose()
        {
            base.Dispose();
        }
        public override int SaveChanges()
        {
            try
            {
                return base.SaveChanges();
            }
            catch (Exception ex)//DbUpdateException 
            {
                throw (ex.InnerException as Exception ?? ex);
            }
        }
        public override DbSet<TEntity> Set<TEntity>()
        {
            return base.Set<TEntity>();
        }
        //public DbSet<TEntity> Set<TEntity>(bool trackAll = false) where TEntity : class
        //{
        //    return base.Set<TEntity>();
        //}
        /// <summary>
        /// 设置跟踪状态
        /// </summary>
        public bool QueryTracking
        {
            set
            {
                this.ChangeTracker.QueryTrackingBehavior =
                       value ? QueryTrackingBehavior.TrackAll
                       : QueryTrackingBehavior.NoTracking;
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (optionsBuilder.IsConfigured)
            {
                return;
            }
            string connectionString = DBServerProvider.GetConnectionString(null);
            if (Const.DBType.Name == Enums.DbCurrentType.MySql.ToString())
            {
                optionsBuilder.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 11)));
            }
            else if (Const.DBType.Name == Enums.DbCurrentType.PgSql.ToString())
            {
                optionsBuilder.UseNpgsql(connectionString);
            }
            else if (Const.DBType.Name == Enums.DbCurrentType.DM.ToString())
            {
                //optionsBuilder.UseDm(connectionString);
            }
            else if (Const.DBType.Name == Enums.DbCurrentType.Oracle.ToString())
            {
                optionsBuilder.UseOracle(connectionString,x=>x.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion19));
               // optionsBuilder.UseOracle(connectionString, b => b.UseOracleSQLCompatibility("11"));
            }
            else
            {
                if (AppSetting.GetSettingString("UseSqlserver2008") =="1")
                {
                   optionsBuilder.UseSqlServer(connectionString, x => x.UseRowNumberForPaging());
                }
                optionsBuilder.UseSqlServer(connectionString, o => o.UseCompatibilityLevel(120));
            }
            //默认禁用实体跟踪
            optionsBuilder = optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

            // Chinese Comment: 配置EF Core日志记录器以输出SQL命令和执行时间。
            // (Configure EF Core logger to output SQL commands and execution times.)
            optionsBuilder.LogTo(
                (eventId, logLevel) =>
                    // Chinese Comment: 仅记录命令执行相关的事件。
                    // (Only log command execution related events.)
                    logLevel >= LogLevel.Information && eventId.Id == RelationalEventId.CommandExecuted.Id,
                eventData => {
                    // Chinese Comment: 当命令执行完毕时，记录SQL、参数和执行时间。
                    // (When a command is executed, log SQL, parameters, and execution time.)
                    if (eventData is CommandExecutedEventData commandExecutedEventData)
                    {
                        var command = commandExecutedEventData.Command;
                        var duration = commandExecutedEventData.Duration.TotalMilliseconds;
                        string message = $"-- EF Core Command Executed -- {System.Environment.NewLine}" +
                                         $"-- Execution Time: {duration} ms {System.Environment.NewLine}" +
                                         $"{command.CommandText}";

                        // Chinese Comment: 参数值通常包含在 CommandText 中，或者可以通过 command.Parameters 访问。
                        // (Parameter values are often included in CommandText, or can be accessed via command.Parameters.)
                        // For brevity, not iterating parameters here, but they are available in command.Parameters.

                        if (duration > 500) // 定义慢查询阈值为500毫秒 (Define slow query threshold as 500ms)
                        {
                            // Chinese Comment: 执行时间超过500毫秒的慢查询，使用警告级别记录。
                            // (Slow query with execution time > 500ms, log with Warning level.)
                            // Note: This Console.WriteLine is for direct visibility. Ideally, integrate with VOL.Core.Services.Logger.
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"[SLOW QUERY WARNING] {message}");
                            Console.ResetColor();
                            // Example of using the main application logger (if accessible and appropriate here):
                            // VOL.Core.Services.Logger.Log(VOL.Core.Enums.LogLevel.Warning, VOL.Core.Enums.LogEvent.SlowQuery, $"EF Core Slow Query ({duration}ms)", command.CommandText, null);
                        }
                        else
                        {
                            // Chinese Comment: 正常查询，使用信息级别记录。
                            // (Normal query, log with Information level.)
                            Console.WriteLine(message);
                            // Example of using the main application logger:
                            // VOL.Core.Services.Logger.Log(VOL.Core.Enums.LogLevel.Debug, VOL.Core.Enums.LogEvent.DbQuery, $"EF Core Query ({duration}ms)", command.CommandText, null);
                        }
                    }
                });
           // optionsBuilder.AddInterceptors(new SqlCommandInterceptor()); // 已通过LogTo实现更详细的日志记录 (Detailed logging implemented via LogTo)
            base.OnConfiguring(optionsBuilder);
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            Type type = null;
            try
            {
                //获取所有类库
                var compilationLibrary = DependencyContext
                    .Default
                    .RuntimeLibraries
                    .Where(x => !x.Serviceable && x.Type != "package" && x.Type == "project");
                foreach (var _compilation in compilationLibrary)
                {
                    //加载指定类
                    AssemblyLoadContext.Default
                    .LoadFromAssemblyName(new AssemblyName(_compilation.Name))
                    .GetTypes()
                    .Where(x =>
                        x.GetTypeInfo().BaseType != null
                        && x.BaseType == (typeof(BaseEntity)))
                        .ToList().ForEach(t =>
                        {
                            modelBuilder.Entity(t);
                            //  modelBuilder.Model.AddEntityType(t);
                        });
                }

                //Oracle数据库指定表名与列名全部大写
                if (DBType.Name == DbCurrentType.Oracle.ToString())
                {
                    foreach (var entity in modelBuilder.Model.GetEntityTypes())
                    {
                        string tableName = entity.GetTableName().ToUpper();
                       // if (tableName.StartsWith("SYS_") || tableName.StartsWith("DEMO_"))
                        {
                            entity.SetTableName(entity.GetTableName().ToUpper());
                            foreach (var property in entity.GetProperties())
                            {
                                property.SetColumnName(property.Name.ToUpper());
                                if (property.ClrType == typeof(Guid))
                                {
                                    property.SetValueConverter(new ValueConverter<Guid, string>(v => v.ToString(), v => new Guid(v)));
                                }
                                else if (property.ClrType == typeof(Guid?))
                                {
                                    property.SetValueConverter(new ValueConverter<Guid?, string>(v => v.ToString(), v => new Guid(v)));
                                }
                            }
                        }
                    }
                }

                //modelBuilder.AddEntityConfigurationsFromAssembly(GetType().Assembly);
                base.OnModelCreating(modelBuilder);
            }
            catch (Exception ex)
            {
                string mapPath = ($"Log/").MapPath();
                Utilities.FileHelper.WriteFile(mapPath,
                    $"syslog_{DateTime.Now.ToString("yyyyMMddHHmmss")}.txt",
                    type?.Name + "--------" + ex.Message + ex.StackTrace + ex.Source);
            }

            // Chinese Comment: 为 Sys_User 表的 Role_Id 和 UserName 添加复合索引，以优化按角色筛选并按用户名排序的查询。
            // (Add a composite index to the Sys_User table for Role_Id and UserName to optimize queries filtered by role and sorted by username.)
            modelBuilder.Entity<VOL.Entity.DomainModels.Sys_User>(entity =>
            {
                entity.HasIndex(e => new { e.Role_Id, e.UserName }, "IX_Sys_User_RoleId_UserName");
            });

            // Chinese Comment: 为实现多租户数据隔离，对所有实现了 ITenantEntity 接口的实体应用全局查询过滤器。
            // (To achieve multi-tenant data isolation, a global query filter is applied to all entities implementing the ITenantEntity interface.)
            if (_userContext?.UserInfo != null) // // 仅当UserContext和UserInfo可用时应用过滤器
            {
                foreach (var entityType in modelBuilder.Model.GetEntityTypes())
                {
                    if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
                    {
                        // Chinese Comment: 构建动态 Lambda 表达式以应用过滤器。
                        // (Construct a dynamic Lambda expression to apply the filter.)
                        var parameter = Expression.Parameter(entityType.ClrType, "e");
                        var propertyAccess = Expression.MakeMemberAccess(parameter, entityType.ClrType.GetProperty(nameof(ITenantEntity.TenantId))!);

                        // Chinese Comment: 获取当前用户的 TenantId。 UserInfo.TenantId 为 null 时不过滤 (例如超级管理员)。
                        // (Get the current user's TenantId. Do not filter if UserInfo.TenantId is null (e.g., for a super administrator).)
                        var currentTenantId = _userContext.UserInfo.TenantId;

                        // Expression for: e.TenantId == currentTenantId
                        // Ensure correct handling if e.TenantId itself is nullable and currentTenantId is null (for non-tenant entities if filter still applies)
                        // Or if currentTenantId is null (super admin), then the filter should allow all.

                        Expression filterExpression;
                        if (currentTenantId.HasValue)
                        {
                            // e.TenantId == currentTenantId.Value
                            filterExpression = Expression.Equal(propertyAccess, Expression.Constant(currentTenantId.Value, typeof(Guid)));
                        }
                        else
                        {
                            // If currentTenantId is null (e.g. super admin), effectively no tenant filter is applied for them.
                            // This means they see all data. If specific non-tenant data should be seen, adjust this.
                            // For this example, a null TenantId on the user means "see all tenants" for ITenantEntity.
                            // A simpler way to achieve "see all if user TenantId is null" is:
                            // builder.HasQueryFilter(e => !_userContext.UserInfo.TenantId.HasValue || e.TenantId == _userContext.UserInfo.TenantId);
                            // The dynamic equivalent:
                            filterExpression = Expression.Constant(true); // No filtering if user's TenantId is null
                        }

                        // For a more direct translation of the simplified logic:
                        // Expression for: !_userContext.UserInfo.TenantId.HasValue
                        var userTenantIdIsNull = Expression.Equal(Expression.Constant(currentTenantId, typeof(Guid?)), Expression.Constant(null, typeof(Guid?)));
                        // Expression for: e.TenantId == _userContext.UserInfo.TenantId
                        var tenantIdsMatch = Expression.Equal(propertyAccess, Expression.Constant(currentTenantId, typeof(Guid?)));
                        // Combined: !_userContext.UserInfo.TenantId.HasValue || e.TenantId == _userContext.UserInfo.TenantId
                        var combinedFilter = Expression.OrElse(userTenantIdIsNull, tenantIdsMatch);


                        var lambda = Expression.Lambda(combinedFilter, parameter);
                        modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
                    }
                }
            }
        }
    }
}
