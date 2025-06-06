using VOL.Builder.Utility;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;
using VOL.Core.Const;
using VOL.Core.DBManager;
using VOL.Core.Enums;
using VOL.Core.Extensions;
using VOL.Core.ManageUser;
using VOL.Core.Utilities;
using VOL.Entity.DomainModels;
using VOL.Entity.DomainModels.Sys;
using VOL.Entity.SystemModels;

namespace VOL.Builder.Services
{
    /// <summary>
    /// Partial class for Sys_TableInfoService responsible for code generation and table metadata management.
    /// Contains methods for creating entities, services, Vue pages, and synchronizing table structures.
    /// </summary>
    public partial class Sys_TableInfoService
    {
        private int totalWidth = 0;
        private int totalCol = 0;
        private string webProject = null;
        private string apiNameSpace = null;
        private string startName = "";

        /// <summary>
        /// Gets the base name of the web project (e.g., "VOL" from "VOL.WebApi").
        /// Used for namespace generation in templates.
        /// </summary>
        private string StratName
        {
            get
            {
                if (startName == "")
                {
                    startName = WebProject.Substring(0, webProject.LastIndexOf('.'));
                }
                return startName;
            }
        }

        /// <summary>
        /// Gets the full name of the Web/Api project (e.g., "VOL.WebApi").
        /// Dynamically determined by looking for projects ending with ".WebApi", ".Api", or ".Web".
        /// Throws an exception if not found, as it's critical for page creation.
        /// </summary>
        private string WebProject
        {
            get
            {
                if (webProject != null)
                    return webProject;
                // Attempt to find the web project by common naming conventions
                webProject = ProjectPath.GetLastIndexOfDirectoryName(".WebApi") ?? ProjectPath.GetLastIndexOfDirectoryName("Api") ?? ProjectPath.GetLastIndexOfDirectoryName(".Web");
                if (webProject == null)
                {
                    VOL.Core.Services.Logger.Error(LogLevel.Critical, LogEvent.Exception, "关键配置错误: 未获取到以.WebApi结尾的项目名称,无法创建页面");
                    throw new Exception("未获取到以.WebApi结尾的项目名称,无法创建页面");
                }
                return webProject;
            }
        }

        /// <summary>
        /// Gets the namespace of the API project (e.g., "VOL.WebApi").
        /// Specifically looks for a project ending with ".WebApi".
        /// Throws an exception if not found, as it's critical for API controller creation.
        /// </summary>
        private string ApiNameSpace
        {
            get
            {
                if (apiNameSpace != null)
                    return apiNameSpace;
                apiNameSpace = ProjectPath.GetLastIndexOfDirectoryName(".WebApi");
                if (apiNameSpace == null)
                {
                    VOL.Core.Services.Logger.Error(LogLevel.Critical, LogEvent.Exception, "关键配置错误: 未获取到.WebApi项目,无法创建Api控制器");
                    throw new Exception("未获取到以.WebApi,无法创建Api控制器");
                }
                return apiNameSpace;
            }
        }

        /// <summary>
        /// Retrieves a tree structure of tables for display.
        /// </summary>
        /// <returns>A tuple containing the JSON serialized tree data and the main project file name.</returns>
        /// <exception cref="Exception">Throws an exception if an error occurs during data retrieval.</exception>
        public async Task<(string, string)> GetTableTree()
        {
            try
            {
                // Retrieve all table info, ordered by OrderNo
                var treeData = await repository.FindAsIQueryable(x => 1 == 1)
                    .Select(c => new
                    {
                        id = c.Table_Id,
                        pId = c.ParentId,
                        parentId = c.ParentId,
                        name = c.ColumnCNName, // Using ColumnCNName as the display name for the node
                        orderNo = c.OrderNo
                    }).OrderByDescending(c => c.orderNo).ToListAsync();

                // Determine if a node is a parent to correctly display tree structure in UI
                var treeList = treeData.Select(a => new
                {
                    a.id,
                    a.pId,
                    a.parentId,
                    a.name,
                    isParent = treeData.Select(x => x.pId).Contains(a.id)
                });

                // Get the base name of the web project (e.g., "VOL" from "VOL.WebApi")
                string startsWith = WebProject.Substring(0, WebProject.IndexOf('.'));
                return (treeList.Count() == 0 ? "[]" : treeList.Serialize() ?? "[]", ProjectPath.GetProjectFileName(startsWith));
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(LogLevel.Error, LogEvent.Exception, "GetTableTree方法异常", null, ex);
                throw new Exception("获取表结构树时发生错误。", ex);
            }
        }

        /// <summary>
        /// Gets the database schema name for MySQL.
        /// Extracts the schema name from the connection string.
        /// </summary>
        /// <returns>A string containing the schema filter for SQL queries, or an empty string if an error occurs.</returns>
        private string GetMysqlTableSchema()
        {
            try
            {
                string dbName = DBServerProvider.GetConnectionString().Split("Database=")[1].Split(";")[0]?.Trim();
                if (!string.IsNullOrEmpty(dbName))
                {
                    dbName = $" and table_schema = '{dbName}' ";
                }
                return dbName;
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LogLevel.Error, VOL.Core.Enums.LogEvent.Exception, "获取MySQL数据库名异常", null, ex);
                return ""; // Return empty string to avoid breaking the SQL query
            }
        }

        /// <summary>
        /// Gets the schema/owner name for DM (达梦) database.
        /// Extracts the schema name from the connection string.
        /// </summary>
        /// <returns>The schema name, or an empty string if an error occurs.</returns>
        private string GetDMOwner()
        {
            try
            {
                string dbName = DBServerProvider.GetConnectionString().Split("schema=")[1].Split(";")[0]?.Trim();
                return dbName;
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LogLevel.Error, VOL.Core.Enums.LogEvent.Exception, "获取达梦数据库名异常", null, ex);
                return ""; // Return empty string to avoid breaking the SQL query
            }
        }

        // Helper methods to get SQL queries for retrieving table column information for different database types.
        // These are used when generating entity models.
        private string GetMySqlModelInfo() { /* ... SQL string ... */ return $@"SELECT DISTINCT CONCAT(NUMERIC_PRECISION,',',NUMERIC_SCALE) as Prec_Scale, CASE WHEN data_type IN( 'BIT', 'BOOL','bit', 'bool') THEN 'bool' WHEN data_type in('smallint','SMALLINT') THEN 'short' WHEN data_type in('tinyint', 'TINYINT') THEN 'sbyte' WHEN data_type IN('MEDIUMINT','mediumint', 'int','INT','year', 'Year') THEN 'int' WHEN data_type in ( 'BIGINT','bigint') THEN 'bigint' WHEN data_type IN('FLOAT',  'DECIMAL','float', 'decimal') THEN 'decimal' WHEN data_type IN( 'DOUBLE', 'double') THEN 'double' WHEN data_type IN('CHAR', 'VARCHAR', 'TINY TEXT', 'TEXT', 'MEDIUMTEXT', 'LONGTEXT', 'TINYBLOB', 'BLOB', 'MEDIUMBLOB', 'LONGBLOB', 'Time','char', 'varchar', 'tiny text', 'text', 'mediumtext', 'longtext', 'tinyblob', 'blob', 'mediumblob', 'longblob', 'time') THEN 'nvarchar' WHEN data_type IN('Date', 'DateTime', 'TimeStamp','date', 'datetime', 'timestamp') THEN 'datetime' ELSE 'nvarchar' END AS ColumnType, Column_Name AS ColumnName FROM information_schema.COLUMNS WHERE table_name = ?tableName {GetMysqlTableSchema()};"; }
        private string GetDMModelInfo() { /* ... SQL string ... */ return $@"SELECT DISTINCT IF(DATA_PRECISION IS NOT NULL, CONCAT(DATA_PRECISION,',',DATA_SCALE),'') as Prec_Scale, CASE WHEN data_type IN( 'BIT', 'BOOL','bit', 'bool') THEN 'bool' WHEN data_type in('smallint','SMALLINT') THEN 'short' WHEN data_type in('tinyint', 'TINYINT') THEN 'sbyte' WHEN data_type IN('MEDIUMINT','mediumint', 'int','INT','year', 'Year') THEN 'int' WHEN data_type in ( 'BIGINT','bigint') THEN 'bigint' WHEN data_type IN('FLOAT',  'DECIMAL','float', 'decimal') THEN 'decimal' WHEN data_type IN( 'DOUBLE', 'double') THEN 'double' WHEN data_type IN('CHAR', 'VARCHAR', 'TINY TEXT', 'TEXT', 'MEDIUMTEXT', 'LONGTEXT', 'TINYBLOB', 'BLOB', 'MEDIUMBLOB', 'LONGBLOB', 'Time','char', 'varchar', 'tiny text', 'text', 'mediumtext', 'longtext', 'tinyblob', 'blob', 'mediumblob', 'longblob', 'time') THEN 'nvarchar' WHEN data_type IN('Date', 'DateTime', 'TimeStamp','date', 'datetime', 'timestamp') THEN 'datetime' ELSE 'nvarchar' END AS ColumnType, Column_Name AS ColumnName FROM user_tab_columns WHERE table_name = :tableName "; }
        private string GetSqlServerModelInfo() { /* ... SQL string ... */ return $@" SELECT CASE WHEN t.ColumnType IN ('DECIMAL','smallmoney','money') THEN CONVERT(VARCHAR(30),t.Prec)+','+CONVERT(VARCHAR(30),t.Scale) ELSE '' END AS Prec_Scale,t.ColumnType,t.ColumnName FROM ( SELECT col.prec AS 'Prec',col.scale AS 'Scale',t.name AS ColumnType,col.name AS ColumnName FROM dbo.syscolumns col LEFT JOIN dbo.systypes t ON col.xtype = t.xusertype INNER JOIN dbo.sysobjects obj ON col.id = obj.id AND obj.xtype IN ('U','V') AND obj.status >= 0 LEFT JOIN dbo.syscomments comm ON col.cdefault = comm.id LEFT JOIN sys.extended_properties ep ON col.id = ep.major_id AND col.colid = ep.minor_id AND ep.name = 'MS_Description' LEFT JOIN sys.extended_properties epTwo ON obj.id = epTwo.major_id AND epTwo.minor_id = 0 AND epTwo.name = 'MS_Description' WHERE obj.name =@tableName) AS t"; }
        private string GetOracleModelInfo(string tableName) { /* ... SQL string ... */ return $@"SELECT c.TABLE_NAME TableName , cc.COLUMN_NAME COLUMNNAME, cc.COMMENTS as ColumnCNName, CASE WHEN c.DATA_TYPE IN('smallint', 'INT') or (c.DATA_TYPE='NUMBER' and c.DATA_LENGTH=0) THEN 'int' WHEN c.DATA_TYPE IN('NUMBER') THEN 'decimal' WHEN c.DATA_TYPE IN('CHAR', 'VARCHAR', 'NVARCHAR','VARCHAR2', 'NVARCHAR2','text', 'image') THEN 'nvarchar' WHEN c.DATA_TYPE IN('DATE') THEN 'date' ELSE 'nvarchar' end as ColumnType, c.DATA_LENGTH as Maxlength, case WHEN c.NULLABLE='Y' THEN 1 ELSE 0 end as ISNULL FROM ALL_tab_columns c LEFT JOIN ALL_col_comments cc ON c.table_name = cc.table_name AND c.column_name = cc.column_name LEFT JOIN ALL_tab_comments t ON c.table_name = t.table_name WHERE c.table_name='{tableName.ToUpper()}'"; }
        private string GetPgSqlModelInfo() { /* ... SQL string ... */ StringBuilder stringBuilder = new StringBuilder(); stringBuilder.Append(" SELECT "); stringBuilder.Append(" col.COLUMN_NAME AS \"ColumnName\", "); stringBuilder.Append(" CASE "); stringBuilder.Append(" WHEN col.udt_name = 'uuid' THEN 'guid' "); stringBuilder.Append(" WHEN col.udt_name IN ( 'int2') THEN 'short' "); stringBuilder.Append(" WHEN col.udt_name IN ( 'int4' ) THEN 'int' "); stringBuilder.Append(" WHEN col.udt_name = 'int8' THEN 'long' "); stringBuilder.Append(" WHEN col.udt_name IN ( 'char', 'varchar', 'text', 'xml', 'bytea' ) THEN 'string' "); stringBuilder.Append(" WHEN col.udt_name IN ( 'bool' ) THEN 'bool' "); stringBuilder.Append(" WHEN col.udt_name IN ( 'date','timestamp' ) THEN 'DateTime' "); stringBuilder.Append(" WHEN col.udt_name IN ( 'decimal', 'money','numeric' ) THEN 'decimal' "); stringBuilder.Append(" WHEN col.udt_name IN ( 'float4', 'float8' ) THEN 'float' ELSE'string ' "); stringBuilder.Append(" END  as ColumnType "); stringBuilder.Append("from information_schema.COLUMNS col  "); stringBuilder.Append("WHERE \"lower\" ( TABLE_NAME ) = \"lower\" (@tableName )  "); return stringBuilder.ToString(); }

        /// <summary>
        /// Checks if a table (or an alias for a table) already has a corresponding entity model generated.
        /// This prevents conflicts when generating new entities.
        /// </summary>
        /// <param name="tableName">The potential alias or name to be used for the new entity.</param>
        /// <param name="tableTrueName">The actual database table name.</param>
        /// <returns>A WebResponseContent indicating success or an error message if a conflict exists.</returns>
        private WebResponseContent ExistsTable(string tableName, string tableTrueName)
        {
            WebResponseContent webResponse = new WebResponseContent(true);
            // Get all project assemblies (excluding serviceable ones like NuGet packages)
            var compilationLibrary = DependencyContext.Default.CompileLibraries.Where(x => !x.Serviceable && x.Type == "project");
            foreach (var _compilation in compilationLibrary)
            {
                try
                {
                    // Load assembly and find types inheriting from BaseEntity
                    foreach (var entity in AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName(_compilation.Name))
                        .GetTypes().Where(x => x.GetTypeInfo().BaseType != null && x.BaseType == typeof(BaseEntity)))
                    {
                        // Check if the entity name matches the true table name but the desired alias is different
                        if (entity.Name == tableTrueName && !string.IsNullOrEmpty(tableName) && tableName != tableTrueName)
                            return webResponse.Error($"实际表名【{tableTrueName}】已创建实体，不能创建别名【{tableName}】实体");

                        // Check if another entity is already mapped to the true table name
                        if (entity.Name != tableName)
                        {
                            var tableAttr = entity.GetCustomAttribute<TableAttribute>();
                            if (tableAttr != null && tableAttr.Name == tableTrueName)
                            {
                                return webResponse.Error($"实际表名【{tableTrueName}】已被【{entity.Name}】创建建实体,不能创建别名【{tableName}】实体,请将别名更换为【{entity.Name}】");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log error during assembly loading/reflection but continue checking other assemblies
                    VOL.Core.Services.Logger.Error(VOL.Core.Enums.LogLevel.Error, VOL.Core.Enums.LogEvent.Exception, $"反射加载程序集查找表是否存在时异常: Assembly={_compilation?.Name}", null, ex);
                }
            }
            return webResponse; // No conflicts found
        }

        /// <summary>
        /// Creates an entity model class file based on the provided table information and database schema.
        /// </summary>
        /// <param name="sysTableInfo">The table information including columns and configuration.</param>
        /// <returns>A message indicating success or failure.</returns>
        public string CreateEntityModel(Sys_TableInfo sysTableInfo)
        {
            if (sysTableInfo == null || sysTableInfo.TableColumns == null || sysTableInfo.TableColumns.Count == 0)
                return "提交的配置数据不完整";

            // Validate column string (e.g., foreign key checks)
            WebResponseContent webResponse = ValidColumnString(sysTableInfo);
            if (!webResponse.Status) return webResponse.Message;

            string currentTableName = sysTableInfo.TableName;
            // Check for existing entity conflicts
            webResponse = ExistsTable(currentTableName, sysTableInfo.TableTrueName);
            if (!webResponse.Status) return webResponse.Message;

            // Use the true table name if an alias is provided
            if (!string.IsNullOrEmpty(sysTableInfo.TableTrueName) && sysTableInfo.TableTrueName != currentTableName)
            {
                currentTableName = sysTableInfo.TableTrueName;
            }

            try
            {
                // Get the appropriate SQL query for the current database type
                string sql = "";
                switch (DBType.Name)
                {
                    case "MySql": sql = GetMySqlModelInfo(); break;
                    case "PgSql": sql = GetPgSqlModelInfo(); break;
                    case "Oracle": sql = GetOracleModelInfo(currentTableName); break;
                    case "DM": sql = GetDMModelInfo(); break;
                    default: sql = GetSqlServerModelInfo(); break;
                }

                // Retrieve column information from the database
                List<TableColumnInfo> tableColumnInfoList = repository.DapperContext.QueryList<TableColumnInfo>(sql, new { tableName = currentTableName });
                if (tableColumnInfoList == null || !tableColumnInfoList.Any())
                {
                    return $"未能获取表 '{currentTableName}' 的列信息，请检查表是否存在或数据库连接。";
                }

                List<Sys_TableColumn> list = sysTableInfo.TableColumns;
                // Call the internal method to generate and write the entity file
                string msg = CreateEntityModel(list, sysTableInfo, tableColumnInfoList, 1); // 1 for standard domain model
                if (msg != "") return msg; // Return any error messages from the internal method

                return "Model创建成功!";
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LogLevel.Error, VOL.Core.Enums.LogEvent.Exception, $"生成实体模型失败: TableName={sysTableInfo?.TableName}", sysTableInfo?.Serialize(), ex);
                return $"生成实体模型时发生错误: {ex.Message}";
            }
        }

        /// <summary>
        /// Saves the edited table configuration, including column details.
        /// </summary>
        /// <param name="sysTableInfo">The table information object with changes.</param>
        /// <returns>A WebResponseContent indicating success or failure.</returns>
        public WebResponseContent SaveEidt(Sys_TableInfo sysTableInfo)
        {
            try
            {
                // Validate column string (e.g., foreign key checks)
                WebResponseContent webResponse = ValidColumnString(sysTableInfo);
                if (!webResponse.Status) return webResponse;

                // Prevent setting a table as its own parent
                if (sysTableInfo.Table_Id == sysTableInfo.ParentId && sysTableInfo.Table_Id != 0)
                {
                    return WebResponseContent.Instance.Error($"父级id不能为自己");
                }
                // Validate express field for quick edit (cannot have a data source)
                if (sysTableInfo.TableColumns != null && sysTableInfo.TableColumns.Any(x => !string.IsNullOrEmpty(x.DropNo) && x.ColumnName == sysTableInfo.ExpressField))
                {
                    return WebResponseContent.Instance.Error($"不能将字段【{sysTableInfo.ExpressField}】设置为快捷编辑,因为已经设置了数据源");
                }

                // Ensure TableName is set for all columns and default IsReadDataset
                if (sysTableInfo.TableColumns != null)
                {
                    sysTableInfo.TableColumns.ForEach(x => { x.TableName = sysTableInfo.TableName; });
                }
                sysTableInfo.TableColumns?.ForEach(x => { if (x.IsReadDataset == null) x.IsReadDataset = 0; }); // Default to not read dataset

                // Update the table info and its columns in the database
                return repository.UpdateRange<Sys_TableColumn>(sysTableInfo, true, true, null, null, true);
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LogLevel.Error, VOL.Core.Enums.LogEvent.Update, $"保存代码生成配置失败: TableName={sysTableInfo?.TableName}", sysTableInfo?.Serialize(), ex);
                return WebResponseContent.Instance.Error("保存配置信息时发生内部错误。");
            }
        }

        /// <summary>
        /// Gets the SQL query string for retrieving table structure based on the current database type.
        /// </summary>
        /// <param name="tableName">The name of the table.</param>
        /// <returns>The SQL query string.</returns>
        private string GetCurrentSql(string tableName)
        {
            string sql;
            if (DBType.Name.ToLower() == DbCurrentType.MySql.ToString().ToLower()) sql = GetMySqlStructure(tableName);
            else if (DBType.Name.ToLower() == DbCurrentType.PgSql.ToString().ToLower()) sql = GetPgSqlStructure(tableName);
            else if (DBType.Name.ToLower() == DbCurrentType.DM.ToString().ToLower()) sql = GetDMStructure(tableName);
            else if (DBType.Name.ToLower() == DbCurrentType.Oracle.ToString().ToLower()) sql = GetOracleStructure(tableName);
            else sql = GetSqlServerStructure(tableName); // Default to SQL Server
            return sql;
        }

        /// <summary>
        /// Synchronizes the stored table column information with the actual database table structure.
        /// Adds new columns, removes deleted columns, and updates existing columns if their type, length, or nullability changes.
        /// </summary>
        /// <param name="tableName">The name of the table to synchronize.</param>
        /// <returns>A WebResponseContent indicating the result of the synchronization.</returns>
        public async Task<WebResponseContent> SyncTable(string tableName)
        {
            WebResponseContent webResponse = new WebResponseContent();
            try
            {
                if (string.IsNullOrEmpty(tableName)) return webResponse.OK("表名不能为空"); // Changed to OK for consistency with other messages

                string originalTableName = tableName;
                // Retrieve existing table configuration
                Sys_TableInfo tableInfo = await repository.FindAsIQueryable(x => x.TableName == originalTableName)
                                                     .Include(o => o.TableColumns)
                                                     .FirstOrDefaultAsync();

                if (tableInfo == null)
                    return webResponse.Error("未获取到【" + originalTableName + "】的配置信息，请使用新建功能");

                // Determine the actual database table name (could be an alias)
                string actualDbTableName = (!string.IsNullOrEmpty(tableInfo.TableTrueName) && tableInfo.TableTrueName != originalTableName)
                                           ? tableInfo.TableTrueName
                                           : originalTableName;

                // Get the SQL query for the current DB and retrieve columns from the database
                string sql = GetCurrentSql(actualDbTableName);
                List<Sys_TableColumn> columnsFromDb = await repository.DapperContext.QueryListAsync<Sys_TableColumn>(sql, new { tableName = actualDbTableName });

                if (columnsFromDb == null || !columnsFromDb.Any())
                    return webResponse.Error("未获取到【" + actualDbTableName + "】表结构信息，请确认表是否存在");

                List<Sys_TableColumn> existingConfigColumns = tableInfo.TableColumns ?? new List<Sys_TableColumn>();
                List<Sys_TableColumn> addColumns = new List<Sys_TableColumn>();
                List<Sys_TableColumn> updateColumns = new List<Sys_TableColumn>();

                // Compare DB columns with configured columns
                foreach (Sys_TableColumn dbCol in columnsFromDb)
                {
                    Sys_TableColumn existingCol = existingConfigColumns.FirstOrDefault(x => x.ColumnName == dbCol.ColumnName);
                    if (existingCol == null) // New column in DB
                    {
                        dbCol.TableName = tableInfo.TableName; // Set original table name for consistency
                        dbCol.Table_Id = tableInfo.Table_Id;
                        addColumns.Add(dbCol);
                    }
                    else // Existing column, check for changes
                    {
                        if (dbCol.ColumnType != existingCol.ColumnType ||
                            dbCol.Maxlength != existingCol.Maxlength ||
                            (dbCol.IsNull ?? 0) != (existingCol.IsNull ?? 0)) // Treat null IsNull as 0 for comparison
                        {
                            existingCol.ColumnType = dbCol.ColumnType;
                            existingCol.Maxlength = dbCol.Maxlength;
                            existingCol.IsNull = dbCol.IsNull;
                            updateColumns.Add(existingCol);
                        }
                    }
                }
                // Columns in config but not in DB (deleted)
                List<Sys_TableColumn> delColumns = existingConfigColumns.Where(a => !columnsFromDb.Any(c => c.ColumnName == a.ColumnName)).ToList();

                if (!addColumns.Any() && !delColumns.Any() && !updateColumns.Any())
                {
                    return webResponse.OK("【" + actualDbTableName + "】表结构未发生变化,无需同步");
                }

                // Perform database operations
                if (addColumns.Any()) repository.AddRange(addColumns);
                if (delColumns.Any()) repository.DbContext.Set<Sys_TableColumn>().RemoveRange(delColumns);
                if (updateColumns.Any()) repository.UpdateRange(updateColumns, x => new { x.ColumnType, x.Maxlength, x.IsNull }); // Update only relevant fields

                await repository.DbContext.SaveChangesAsync();

                return webResponse.OK($"新加字段【{addColumns.Count}】个,删除字段【{delColumns.Count}】,修改字段【{updateColumns.Count}】");
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LogLevel.Error, VOL.Core.Enums.LogEvent.Exception, $"同步表结构失败: TableName={tableName}", new { TableName = tableName }, ex);
                return webResponse.Error($"同步表结构时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates service, repository, and optionally controller classes based on templates.
        /// </summary>
        /// <param name="tableName">The name of the table (entity name).</param>
        /// <param name="nameSpace">The base namespace for the generated classes (e.g., "VOL.Project").</param>
        /// <param name="foldername">The subfolder name within Services, Repositories, etc. (e.g., "System").</param>
        /// <param name="webController">Whether to generate a Web (MVC) controller.</param>
        /// <param name="apiController">Whether to generate an API controller.</param>
        /// <returns>A message indicating success or failure.</returns>
        public string CreateServices(string tableName, string nameSpace, string foldername, bool webController, bool apiController)
        {
            try
            {
                // Ensure table configuration exists
                var tableColumn = repository.FindAsyncFirst<Sys_TableColumn>(x => x.TableName == tableName).Result;
                if (tableColumn == null) return $"没有查到{tableName}表信息";
                if (string.IsNullOrEmpty(nameSpace) || string.IsNullOrEmpty(foldername)) return $"命名空间、项目文件夹都不能为空";

                string domainContent = "";
                // Get the root directory of the framework
                string frameworkFolder = ProjectPath.GetProjectDirectoryInfo()?.FullName;
                if (string.IsNullOrEmpty(frameworkFolder)) return "无法确定项目框架根目录。";

                string[] splitArr = nameSpace.Split('.');
                string projectName = splitArr.Length > 1 ? splitArr[splitArr.Length - 1] : splitArr[0]; // e.g., "Project" from "VOL.Project"
                string baseOptions = $"\"{projectName}\",\"{foldername}\",\"{tableName}\""; // Used in controller templates

                // Create API Controller if requested
                if (apiController)
                {
                    string apiPath = ProjectPath.GetProjectDirectoryInfo().GetDirectories().FirstOrDefault(x => x.Name.ToLower().EndsWith(".webapi"))?.FullName;
                    if (string.IsNullOrEmpty(apiPath)) return "未找到webapi类库,请确认是存在weiapi类库命名以.webapi结尾";

                    string controllerDirPath = Path.Combine(apiPath, "Controllers", projectName);
                    string partialControllerPath = Path.Combine(controllerDirPath, "Partial", tableName + "Controller.cs");
                    string mainControllerPath = Path.Combine(controllerDirPath, tableName + "Controller.cs");

                    // Create partial controller if it doesn't exist
                    if (!FileHelper.FileExists(partialControllerPath))
                    {
                        string partialControllerContent = FileHelper.ReadFile(Path.Combine("Template","Controller","ControllerApiPartial.html"))
                           .Replace("{Namespace}", nameSpace).Replace("{TableName}", tableName).Replace("{StartName}", StratName);
                        FileHelper.WriteFile(Path.GetDirectoryName(partialControllerPath), Path.GetFileName(partialControllerPath), partialControllerContent);
                    }
                    // Create main API controller
                    domainContent = FileHelper.ReadFile(Path.Combine("Template","Controller","ControllerApi.html"))
                        .Replace("{Namespace}", nameSpace).Replace("{TableName}", tableName).Replace("{StartName}", StratName).Replace("{BaseOptions}", baseOptions);
                    FileHelper.WriteFile(Path.GetDirectoryName(mainControllerPath), Path.GetFileName(mainControllerPath), domainContent);
                }

                // Create Repository and IRepository
                string repoDir = Path.Combine(frameworkFolder, nameSpace, "Repositories", foldername);
                string iRepoDir = Path.Combine(frameworkFolder, nameSpace, "IRepositories", foldername);
                domainContent = FileHelper.ReadFile(Path.Combine("Template","Repositorys","BaseRepository.html")).Replace("{Namespace}", nameSpace).Replace("{TableName}", tableName).Replace("{StartName}", StratName);
                FileHelper.WriteFile(repoDir, tableName + "Repository.cs", domainContent);
                domainContent = FileHelper.ReadFile(Path.Combine("Template","IRepositorys","BaseIRepositorie.html")).Replace("{Namespace}", nameSpace).Replace("{TableName}", tableName).Replace("{StartName}", StratName);
                FileHelper.WriteFile(iRepoDir, "I" + tableName + "Repository.cs", domainContent);

                // Create IService (main and partial)
                string iServicePath = Path.Combine(frameworkFolder, nameSpace, "IServices", foldername);
                string iServiceFileName = "I" + tableName + "Service.cs";
                string partialIServicePath = Path.Combine(iServicePath, "Partial", iServiceFileName);
                if (!FileHelper.FileExists(partialIServicePath))
                {
                    domainContent = FileHelper.ReadFile(Path.Combine("Template","IServices","IServiceBasePartial.html")).Replace("{Namespace}", nameSpace).Replace("{TableName}", tableName).Replace("{StartName}", StratName);
                    FileHelper.WriteFile(Path.GetDirectoryName(partialIServicePath), Path.GetFileName(partialIServicePath), domainContent);
                }
                domainContent = FileHelper.ReadFile(Path.Combine("Template","IServices","IServiceBase.html")).Replace("{Namespace}", nameSpace).Replace("{TableName}", tableName).Replace("{StartName}", StratName);
                FileHelper.WriteFile(iServicePath, iServiceFileName, domainContent);

                // Create Service (main and partial)
                string servicePath = Path.Combine(frameworkFolder, nameSpace, "Services", foldername);
                string serviceFileName = tableName + "Service.cs";
                string partialServicePath = Path.Combine(servicePath, "Partial", serviceFileName);
                if (!FileHelper.FileExists(partialServicePath))
                {
                    domainContent = FileHelper.ReadFile(Path.Combine("Template","Services","ServiceBasePartial.html")).Replace("{Namespace}", nameSpace).Replace("{TableName}", tableName).Replace("{StartName}", StratName);
                    FileHelper.WriteFile(Path.GetDirectoryName(partialServicePath), Path.GetFileName(partialServicePath), domainContent);
                }
                domainContent = FileHelper.ReadFile(Path.Combine("Template","Services","ServiceBase.html"))
                    .Replace("{Namespace}", nameSpace).Replace("{TableName}", tableName).Replace("{StartName}", StratName);
                FileHelper.WriteFile(servicePath, serviceFileName, domainContent);

                // Create Web (MVC) Controller if requested
                if (webController)
                {
                    string webCtrlPathDir = Path.Combine(frameworkFolder, nameSpace, "Controllers", foldername);
                    string webCtrlFileName = tableName + "Controller.cs";
                    string partialWebCtrlPath = Path.Combine(webCtrlPathDir, "Partial", webCtrlFileName);
                    if (!FileHelper.FileExists(partialWebCtrlPath)) // Create partial if not exists
                    {
                        domainContent = FileHelper.ReadFile(Path.Combine("Template","Controller","ControllerPartial.html")).Replace("{Namespace}", nameSpace).Replace("{TableName}", tableName).Replace("{BaseOptions}", baseOptions).Replace("{StartName}", StratName);
                        FileHelper.WriteFile(Path.GetDirectoryName(partialWebCtrlPath), Path.GetFileName(partialWebCtrlPath), domainContent);
                    }
                    domainContent = FileHelper.ReadFile(Path.Combine("Template","Controller","Controller.html")).Replace("{Namespace}", nameSpace).Replace("{TableName}", tableName).Replace("{BaseOptions}", baseOptions).Replace("{StartName}", StratName);
                    FileHelper.WriteFile(webCtrlPathDir, webCtrlFileName, domainContent);
                }
                return "业务类创建成功!";
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LogLevel.Error, VOL.Core.Enums.LogEvent.Exception, $"生成业务类失败: TableName={tableName}, Namespace={nameSpace}, Foldername={foldername}",
                                 new { TableName = tableName, Namespace = nameSpace, Foldername = foldername }, ex);
                return $"生成业务类时发生错误: {ex.Message}";
            }
        }

        /// <summary>
        /// Generates data for search or edit forms (Vue/App).
        /// </summary>
        private List<object> GetSearchData(List<List<PanelHtml>> panelHtml, List<Sys_TableColumn> sysColumnList, bool vue = false, bool edit = false, bool app = false)
        {
            // Populate panelHtml based on edit or search mode
            if (edit) { GetPanelData(sysColumnList, panelHtml, x => x.EditRowNo, c => c.EditRowNo != null && c.EditRowNo > 0, false, q => q.EditRowNo, vue, app: app); }
            else { GetPanelData(sysColumnList, panelHtml, x => x.SearchRowNo, c => c.SearchRowNo != null, true, q => q.SearchRowNo, vue, app: app); }

            List<object> list = new List<object>();
            int index = 0;
            bool group = panelHtml.Exists(x => x.Count > 1); // Check if grouping is needed for App UI

            panelHtml.ForEach(x => {
                index++;
                List<Dictionary<string, object>> keyValuePairs = new List<Dictionary<string, object>>();
                x.ForEach(s => {
                    Dictionary<string, object> keyValues = new Dictionary<string, object>();
                    // Vue specific properties
                    if (vue) {
                        if (!string.IsNullOrEmpty(s.dataSource) && s.dataSource != "''") {
                            if (app) { keyValues.Add("key", s.dataSource); }
                            else { keyValues.Add("dataKey", s.dataSource); }
                            keyValues.Add("data", new string[] { }); // Placeholder for data source items
                        }
                        keyValues.Add("title", s.text);
                        if (s.require) { keyValues.Add("required", s.require); }
                        keyValues.Add("field", s.id);
                        if (s.disabled) { keyValues.Add("disabled", true); }
                        if (s.colSize > 0 && !app) { keyValues.Add("colSize", s.colSize); } // Column size for layout
                        if (!string.IsNullOrEmpty(s.displayType) && s.displayType != "''") {
                            keyValues.Add("type", s.columnType == "img" ? s.columnType : s.displayType);
                        }
                    }
                    // General properties
                    else {
                        keyValues.Add("columnType", s.columnType);
                        if (!string.IsNullOrEmpty(s.dataSource)) { keyValues.Add("dataSource", s.dataSource); }
                        keyValues.Add("text", s.text);
                        if (s.require) { keyValues.Add("require", s.require); }
                        keyValues.Add("id", s.id);
                    }
                    if (!app) { keyValuePairs.Add(keyValues); }
                    else { list.Add(keyValues); } // For App, add directly to the main list
                });
                if (!app) { list.Add(keyValuePairs); } // For Vue, add the row of controls
                else { if (index != panelHtml.Count() && group) { list.Add(new { type = "group" }); } } // Add group separator for App if needed
            });
            return list;
        }

        /// <summary>
        /// Creates Vue.js page files (.vue, options.js, extension.js) and updates router configuration.
        /// </summary>
        /// <param name="sysTableInfo">The table information containing column definitions and configuration.</param>
        /// <param name="vuePath">The absolute path to the Vue project's 'Views' or 'pages' directory.</param>
        /// <returns>A message indicating success or failure.</returns>
        public string CreateVuePage(Sys_TableInfo sysTableInfo, string vuePath)
        {
            // TODO: (安全审查) 对所有源自 Sys_TableColumn 或 Sys_TableInfo 并将嵌入到Vue模板 (<script>标签内或HTML属性中) 的字符串元数据值，
            // 在替换到模板前，应调用 ContainsPotentiallyDangerousScript() 进行检查。
            // 如果检测到危险脚本，应记录警告并阻止或清理该特定内容。
            // (TODO: (Security Review) For all string metadata values originating from Sys_TableColumn or Sys_TableInfo
            // that will be embedded into Vue templates (within <script> tags or HTML attributes),
            // a check using ContainsPotentiallyDangerousScript() should be performed before template replacement.
            // If dangerous script is detected, a warning should be logged and the specific content blocked or sanitized.)

            // 重要安全提示 (Important Security Note):
            // 当将任何数据（特别是用户可配置的元数据）嵌入到生成的Vue模板的HTML部分或JavaScript字符串时，
            // 必须确保进行了适当的上下文编码/转义，以防止XSS攻击。
            // 例如，用于HTML属性的值应进行HTML属性编码，用于JavaScript字符串的值应进行JavaScript字符串编码。
            // Newtonsoft.Json.Serialize() 在序列化为JSON时通常能正确处理JS字符串转义。
            // (When embedding any data (especially user-configurable metadata) into the HTML parts or JavaScript strings
            // of generated Vue templates, it's crucial to ensure proper contextual encoding/escaping to prevent XSS attacks.
            // For example, values for HTML attributes should be HTML attribute encoded, and values for JavaScript strings
            // should be JavaScript string encoded. Newtonsoft.Json.Serialize() generally handles JS string escaping correctly when serializing to JSON.)
            try
            {
                // Determine if generating for Vite, App, or standard Vue
                bool isVite = HttpContext.Current.Request.Query["vite"].GetInt() > 0;
                bool isApp = HttpContext.Current.Request.Query["app"].GetInt() > 0;

                // Validations
                if (string.IsNullOrEmpty(vuePath)) return isApp ? "请设置App路径" : "请设置Vue所在Views的绝对路径!";
                if (!FileHelper.DirectoryExists(vuePath)) return $"未找项目路径{vuePath}!";
                if (sysTableInfo == null || sysTableInfo.TableColumns == null || !sysTableInfo.TableColumns.Any()) return "提交的配置数据不完整";

                vuePath = vuePath.Trim().TrimEnd('/').TrimEnd('\\');
                List<Sys_TableColumn> sysColumnList = sysTableInfo.TableColumns;
                string[] eidtTye = { "select", "selectList", "drop", "dropList", "checkbox" }; // Types requiring a data source
                if (sysColumnList.Exists(x => eidtTye.Contains(x.EditType) && string.IsNullOrEmpty(x.DropNo))) return $"编辑类型为[{string.Join(',', eidtTye)}]时必须选择数据源";
                if (sysColumnList.Exists(x => eidtTye.Contains(x.SearchType) && string.IsNullOrEmpty(x.DropNo))) return $"查询类型为[{string.Join(',', eidtTye)}]时必须选择数据源";
                if (isApp && !sysColumnList.Exists(x => x.Enable > 0)) return $"请设置[app列]"; // App requires at least one enabled column

                bool editLine = false; // Placeholder for inline editing feature, currently seems unused for this assignment
                // Generate grid column definitions
                StringBuilder sb = GetGridColumns(sysColumnList, sysTableInfo.ExpressField, detail: editLine, true, app: isApp);
                if (sb.Length == 0) return "未获取到数据!";
                string columns = sb.ToString().Trim().TrimEnd(',');
                string key = sysColumnList.FirstOrDefault(c => c.IsKey == 1)?.ColumnName ?? ""; // Primary key

                // Define function to filter columns for editing based on platform (App/Vue)
                Func<Sys_TableColumn, bool> editFunc = c => c.EditRowNo != null && c.EditRowNo > 0;
                if (isApp) editFunc = x => new int[] { 1, 2, 5, 7 }.Any(c => c == x.Enable); // Specific enable flags for App edit fields

                // Serialize form fields for editing
                var formFileds = sysColumnList.Where(editFunc).OrderBy(o => o.EditRowNo).ThenByDescending(t => t.OrderNo)
                    .Select(x => new KeyValuePair<string, object>(x.ColumnName, (x.EditType == "checkbox" || x.EditType == "selectList" || x.EditType == "cascader") ? new string[0] : "" as object))
                    .ToList().ToDictionary(x => x.Key, x => x.Value).Serialize();

                List<List<PanelHtml>> panelHtml = new List<List<PanelHtml>>();
                // Generate search form options
                List<object> searchDataList = GetSearchData(panelHtml, sysColumnList, true, app: isApp);

                string pageContent, editOptions = "", vueOptions = "";
                // Load appropriate templates based on platform
                if (isApp) pageContent = FileHelper.ReadFile(Path.Combine("Template", "Page", "app", "options.html"));
                else if (HttpContext.Current.Request.Query.ContainsKey("v3")) // Vue 3
                { pageContent = FileHelper.ReadFile(Path.Combine("Template", "Page", "Vue3SearchPage.html")); editOptions = FileHelper.ReadFile(Path.Combine("Template", "Page", "EditOptions.html")); vueOptions = FileHelper.ReadFile(Path.Combine("Template", "Page", "VueOptions.html")); }
                else pageContent = FileHelper.ReadFile(Path.Combine("Template", "Page", "VueSearchPage.html")); // Vue 2

                if (string.IsNullOrEmpty(pageContent)) return "未找到Template模板文件";

                // Define function to filter columns for searching
                Func<Sys_TableColumn, bool> searchFunc = c => c.SearchRowNo != null && c.SearchRowNo > 0;
                if (isApp) { searchFunc = x => new int[] { 1, 3, 5, 6 }.Any(c => c == x.Enable); vueOptions = pageContent; } // Specific enable flags for App search

                // Serialize search form fields
                var searchFormFileds = sysColumnList.Where(searchFunc)
                    .Select(x => new KeyValuePair<string, object>(x.ColumnName, (x.SearchType == "checkbox" || x.SearchType == "selectList" || x.EditType == "cascader") ? new string[0] : x.SearchType == "range" ? new string[] { null, null } : "" as object))
                    .ToList().ToDictionary(x => x.Key, x => x.Value).Serialize();

                // Populate vueOptions template
                vueOptions = vueOptions.Replace("#searchFormFileds", searchFormFileds)
                    .Replace("#searchFormOptions", searchDataList.Serialize() ?? "".Replace("},{", "},\r\n                               {").Replace("],[", "],\r\n                              ["));
                panelHtml = new List<List<PanelHtml>>();
                // Generate edit form options
                string formOptions = GetSearchData(panelHtml, sysColumnList.Where(editFunc).ToList(), true, true, app: isApp).Serialize() ?? "";

                string[] arr = sysTableInfo.Namespace.Split('.');
                string spaceFolder = (arr.Length > 1 ? arr[arr.Length - 1] : arr[0]).ToLower(); // e.g., "system" from "VOL.System"

                if (editLine) vueOptions = vueOptions.Replace("'#key'", "'#key',\r\n                editTable:true "); // Inline edit flag
                vueOptions = vueOptions.Replace("#columns", columns)
                                .Replace("#SortName", string.IsNullOrEmpty(sysTableInfo.SortName) ? key : sysTableInfo.SortName)
                                .Replace("#key", key).Replace("#Foots", " ").Replace("#TableName", sysTableInfo.TableName)
                                .Replace("#cnName", sysTableInfo.ColumnCNName).Replace("#url", $"/{sysTableInfo.TableName}/")
                                .Replace("#folder", spaceFolder).Replace("#editFormFileds", formFileds)
                                .Replace("#editFormOptions", formOptions.Replace("},{", "},\r\n                               {").Replace("],[", "],\r\n                              ["));

                string currentVuePath = vuePath;

                // Handle master-detail relationships
                bool hasSubDetail = false;
                List<string> detailItems = new List<string>();
                if (!string.IsNullOrEmpty(sysTableInfo.DetailName))
                {
                    var tables = sysTableInfo.DetailName.Replace("，", ",").Split(',');
                    // Retrieve detail table configurations
                    var detailTables = repository.FindAsIQueryable(x => tables.Contains(x.TableName)).Include(x => x.TableColumns).ToList();
                    if (detailTables.Count != tables.Length) return $"请将明细表生成代码!"; // Ensure all detail tables are generated
                    var obj = detailTables.FirstOrDefault(c => c.TableColumns == null || !c.TableColumns.Any());
                    if (obj != null) return $"明细表{obj.TableName}没有列的信息,请确认是否有列数据或列数据是否被删除!";

                    var tableCNNameArr = sysTableInfo.DetailCnName?.Replace("，", ",")?.Split(',');
                    if (tableCNNameArr == null || tableCNNameArr.Length != tables.Length) return $"明细表中文名与明细表数量不一致，请以逗号隔开数量也一致!";

                    List<Sys_TableInfo> tables2 = tables.Select(name => detailTables.First(x => x.TableName == name)).ToList(); // Ensure correct order
                    detailTables = tables2;
                    hasSubDetail = detailTables.Exists(c => !string.IsNullOrEmpty(c.DetailName)) || detailTables.Count > 1; // Check for nested details or multiple details

                    int tableIndex = 0;
                    foreach (var detailTable in detailTables)
                    {
                        string tableCNName = tableCNNameArr[tableIndex++];
                        // Format for detail table configuration in options.js
                        string detailItemFormat = hasSubDetail ?
                            @"  { cnName: '#detailCnName', table: '#detailTable', columns: [#detailColumns], sortName: '#detailSortName', key: '#detailKey', buttons:[], delKeys:[], detail:null }" :
                            @"  { cnName: '#detailCnName', table: '#detailTable', columns: [#detailColumns], sortName: '#detailSortName', key: '#detailKey' }";

                        List<Sys_TableColumn> detailList = detailTable.TableColumns;
                        StringBuilder sbDetail = GetGridColumns(detailList, detailTable.ExpressField, true, true); // Generate columns for detail grid
                        string detailKey = detailList.First(c => c.IsKey == 1).ColumnName;
                        string detailCols = sbDetail.ToString().Trim().TrimEnd(',');

                        detailItemFormat = detailItemFormat.Replace("#detailColumns", detailCols).Replace("#detailCnName", tableCNName)
                                           .Replace("#detailTable", detailTable.TableName).Replace("#detailKey", detailKey)
                                           .Replace("#detailSortName", string.IsNullOrEmpty(detailTable.SortName) ? detailKey : detailTable.SortName);
                        detailItems.Add(detailItemFormat);

                        // If only one detail table, populate specific placeholders in editOptions
                        if (detailTables.Count == 1)
                        {
                             editOptions = editOptions.Replace("#detailColumns", detailCols).Replace("#detailCnName", detailTable.ColumnCNName)
                                .Replace("#detailTable", detailTable.TableName).Replace("#detailKey", detailKey)
                                .Replace("#detailSortName", string.IsNullOrEmpty(detailTable.SortName) ? detailKey : detailTable.SortName);
                        }
                    }
                    // Populate placeholders for detail tables in vueOptions
                    vueOptions = vueOptions.Replace("#tables1", $"{detailItems[0]}"); // First detail table
                    vueOptions = vueOptions.Replace("#tables2", hasSubDetail && detailItems.Count > 1 ? $"[{string.Join(",\r                  ", detailItems.Skip(1))}]" : "[]"); // Subsequent detail tables
                }
                else // No detail tables
                {
                    string emptyReplacement = isApp ? "[]" : "{columns:[]}";
                    vueOptions = vueOptions.Replace("#tables1", emptyReplacement).Replace("#tables2", "[]");
                    string[]phs = { "#detailColumns", "#detailKey", "#detailSortName", "#detailCnName", "#detailTable" };
                    foreach(var p in phs) { vueOptions = vueOptions.Replace(p, ""); editOptions = editOptions.Replace(p, ""); } // Clear detail placeholders
                    vueOptions = vueOptions.Replace("api/#TableName/getDetailPage", "");
                    editOptions = editOptions.Replace("api/#TableName/getDetailPage", "");
                }

                // Determine paths for generated files
                string srcPath = new DirectoryInfo(currentVuePath.MapPath()).Parent.FullName; // Assumes currentVuePath is 'Views' or 'pages'
                string spaceFolderForPath = spaceFolder.Replace("/", Path.DirectorySeparatorChar.ToString());
                string extensionPath = isApp ? Path.Combine(srcPath, "pages", spaceFolderForPath) : Path.Combine(srcPath, "extension", spaceFolderForPath);
                string exFileName = sysTableInfo.TableName + ".js" + (isVite ? "x" : ""); // .jsx for Vite
                string currentTableNameForPath = sysTableInfo.TableName;

                if (!isApp) // Vue specific path adjustments
                {
                    string specificFolderPath = sysTableInfo.FolderName?.ToLower(); // Optional subfolder
                    if (!string.IsNullOrEmpty(specificFolderPath))
                    {
                         extensionPath = Path.Combine(srcPath, "extension", spaceFolderForPath, specificFolderPath);
                         spaceFolderForPath = Path.Combine(spaceFolderForPath, specificFolderPath);
                    }
                    pageContent = pageContent.Replace("#folder", spaceFolderForPath.Replace(Path.DirectorySeparatorChar, '/'));
                }

                // Create extension file if it doesn't exist (for Vue)
                if (!isApp && !FileHelper.FileExists(Path.Combine(extensionPath, exFileName)))
                {
                    string exContent = FileHelper.ReadFile(Path.Combine("Template", "Page", "VueExtension.html"));
                    exContent = exContent.Replace("#TableName", sysTableInfo.TableName);
                    FileHelper.WriteFile(extensionPath, exFileName, exContent);
                }
                pageContent = pageContent.Replace("#TableName", currentTableNameForPath);

                // App specific file generation
                if (isApp)
                {
                    pageContent = vueOptions; // App options.js content is derived from vueOptions
                    pageContent = pageContent.Replace("#TableName", currentTableNameForPath);
                    pageContent = pageContent.Replace("#titleField", sysTableInfo.ExpressField).Replace("{#table}", currentTableNameForPath);
                    string appTablePath = Path.Combine(currentVuePath, spaceFolderForPath, currentTableNameForPath);
                    FileHelper.WriteFile(appTablePath, currentTableNameForPath + "Options.js", pageContent);

                    string appEditPath = $"pages/{spaceFolderForPath.Replace(Path.DirectorySeparatorChar, '/')}/{currentTableNameForPath}/{currentTableNameForPath}Edit";
                    // Create .vue page and edit page if they don't exist
                    if (!FileHelper.FileExists(Path.Combine(appTablePath, currentTableNameForPath + ".vue")))
                    {
                        pageContent = FileHelper.ReadFile(Path.Combine("Template", "Page", "app", "page.html")).Replace("#TableName", currentTableNameForPath).Replace("#path", appEditPath);
                        FileHelper.WriteFile(appTablePath, currentTableNameForPath + ".vue", pageContent);
                    }
                    if (!FileHelper.FileExists(Path.Combine(appTablePath, currentTableNameForPath + "Edit.vue")))
                    {
                        pageContent = FileHelper.ReadFile(Path.Combine("Template", "Page", "app", "edit.html")).Replace("#TableName", currentTableNameForPath);
                        FileHelper.WriteFile(appTablePath, currentTableNameForPath + "Edit.vue", pageContent);
                    }
                    // Update pages.json for App navigation
                    string pagesJsonPath = Path.Combine(srcPath, "pages.json");
                    string pagesJsonContent = FileHelper.ReadFile(pagesJsonPath);
                    string appPathForPagesJson = $"pages/{spaceFolderForPath.Replace(Path.DirectorySeparatorChar, '/')}/{currentTableNameForPath}/{currentTableNameForPath}";
                    StringBuilder newEntries = new StringBuilder();
                    Action<string, string> addPageJsonEntry = (pPath, title) => {
                        if (!pagesJsonContent.Contains($"\"{pPath}\"")) { // Add if not exists
                            newEntries.AppendLine("		,{");
                            newEntries.AppendLine($"			\"path\": \"{pPath}\",");
                            newEntries.AppendLine( "			\"style\": {");
                            newEntries.AppendLine($"				\"navigationBarTitleText\": \"{title}\"");
                            newEntries.AppendLine( "			}");
                            newEntries.AppendLine("		}");
                        }
                    };
                    addPageJsonEntry(appPathForPagesJson, sysTableInfo.ColumnCNName);
                    addPageJsonEntry(appEditPath, sysTableInfo.ColumnCNName); // Add entry for edit page as well
                    if (newEntries.Length > 0) // If new entries were added, update pages.json
                    {
                        int closingBracketIndex = pagesJsonContent.LastIndexOf("]");
                        if (closingBracketIndex != -1)
                        {
                            string insertPoint = pagesJsonContent.Substring(0, closingBracketIndex).TrimEnd();
                            if (insertPoint.EndsWith("}")) newEntries.Insert(0, ",\r\n"); // Add comma if needed
                            pagesJsonContent = pagesJsonContent.Insert(closingBracketIndex, newEntries.ToString());
                            FileHelper.WriteFile(srcPath, "pages.json", pagesJsonContent);
                        }
                    }
                }
                else // Vue specific file generation
                {
                    if (isVite && !pageContent.Contains(currentTableNameForPath + ".jsx")) pageContent = pageContent.Replace(currentTableNameForPath + ".js", currentTableNameForPath + ".jsx");
                    pageContent = pageContent.Replace(".jsxx", ".jsx"); // Ensure correct extension for Vite
                    string viewFilePath = Path.Combine(currentVuePath, spaceFolderForPath, currentTableNameForPath + ".vue");
                    // Create .vue file if it doesn't exist or is an old version (heuristic check for setup())
                    if (!FileHelper.FileExists(viewFilePath) || (FileHelper.ReadFile(viewFilePath)?.Contains("setup()") == true))
                    {
                         pageContent = pageContent.Replace("#folder", spaceFolderForPath.Replace(Path.DirectorySeparatorChar, '/'));
                         FileHelper.WriteFile(Path.Combine(currentVuePath, spaceFolderForPath), currentTableNameForPath + ".vue", pageContent);
                    }
                    if (hasSubDetail) vueOptions = vueOptions.Replace("#tables2", $"[{string.Join(",\r                  ", detailItems)}]"); // Populate all detail tables for hasSubDetail case
                    vueOptions = vueOptions.Replace("[[]]", "[]"); // Cleanup for empty detail arrays
                    FileHelper.WriteFile(Path.Combine(currentVuePath, spaceFolderForPath, currentTableNameForPath), "options.js", vueOptions);

                    // Update router configuration
                    string routerPath = Path.Combine(srcPath, "router", "viewGird.js");
                    string routerContent = FileHelper.ReadFile(routerPath);
                    if (!routerContent.Contains($"path: '/{currentTableNameForPath}'")) // Add route if not exists
                    {
                        string routerTemplate = FileHelper.ReadFile(Path.Combine("Template", "Page", "router.html"))
                         .Replace("#TableName", currentTableNameForPath).Replace("#folder", spaceFolderForPath.Replace(Path.DirectorySeparatorChar, '/'));
                        routerContent = routerContent.Replace("]", routerTemplate + "\n]"); // Insert before closing bracket
                        FileHelper.WriteFile(Path.Combine(srcPath, "router"), "viewGird.js", routerContent);
                    }
                }
                return "页面创建成功!";
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LogLevel.Error, VOL.Core.Enums.LogEvent.Exception, $"生成Vue页面失败: TableName={sysTableInfo?.TableName}, VuePath={vuePath}",
                                 new { TableName = sysTableInfo?.TableName, VuePath = vuePath }, ex);
                return $"生成Vue页面时发生错误: {ex.Message}";
            }
        }

        /// <summary>
        /// Initializes table metadata in the system if it doesn't already exist.
        /// Retrieves column information from the database and creates Sys_TableInfo and Sys_TableColumn records.
        /// </summary>
        /// <param name="parentId">The parent ID in the table tree.</param>
        /// <param name="tableName">The name of the database table.</param>
        /// <param name="columnCNName">The Chinese name/description for the table.</param>
        /// <param name="nameSpace">The namespace for generated code.</param>
        /// <param name="foldername">The folder name for generated code.</param>
        /// <param name="tableId">The existing table ID if any (used in tree load scenario).</param>
        /// <param name="isTreeLoad">A flag indicating if this is part of a tree loading operation (skips creation if true and tableId is provided).</param>
        /// <returns>The Table_Id of the initialized or existing table, or -1 if tableName is empty.</returns>
        /// <exception cref="Exception">Throws an exception if table structure cannot be found or if there's an error during DB operations.</exception>
        private int InitTable(int parentId, string tableName, string columnCNName, string nameSpace, string foldername, int tableId, bool isTreeLoad)
        {
            try
            {
                // If it's a tree load operation and tableId is provided, just return the tableId (already initialized)
                if (isTreeLoad) return tableId;
                if (string.IsNullOrEmpty(tableName)) return -1; // Table name is mandatory

                // Check if table configuration already exists
                var existingTable = repository.FindAsIQueryable(x => x.TableName == tableName)
                                         .Select(s => new { s.Table_Id }).FirstOrDefault();
                if (existingTable != null && existingTable.Table_Id > 0) return existingTable.Table_Id; // Return existing ID

                // Create new Sys_TableInfo record
                Sys_TableInfo tableInfo = new Sys_TableInfo()
                {
                    ParentId = parentId, ColumnCNName = columnCNName, CnName = columnCNName, TableName = tableName,
                    Namespace = nameSpace, FolderName = foldername, Enable = 1 // Default to enabled
                };

                // Retrieve column structure from the database
                List<Sys_TableColumn> columns = repository.DapperContext.QueryList<Sys_TableColumn>(GetCurrentSql(tableName), new { tableName });

                if (columns == null || !columns.Any())
                {
                    VOL.Core.Services.Logger.Warning(VOL.Core.Enums.LogLevel.Warning, VOL.Core.Enums.LogEvent.Exception, $"InitTable: 表结构未找到或为空: TableName={tableName}", new { TableName = tableName });
                    throw new Exception($"表结构未找到或为空: {tableName}");
                }

                // Initialize OrderNo and EditRowNo for new columns
                int orderNo = (columns.Count + 10) * 50; // Start with a high OrderNo for descending sort
                for (int i = 0; i < columns.Count; i++) { columns[i].OrderNo = orderNo; orderNo -= 50; columns[i].EditRowNo = 0; } // Default EditRowNo to 0
                SetMaxLength(columns); // Adjust MaxLength for certain column types (internal logic)

                // Add table info and columns to the database
                var result = base.Add<Sys_TableColumn>(tableInfo, columns, false); // 'false' likely means don't save immediately
                if (!result.Status)
                {
                    VOL.Core.Services.Logger.Error(VOL.Core.Enums.LogLevel.Error, VOL.Core.Enums.LogEvent.Add, $"InitTable: base.Add<Sys_TableColumn>失败: TableName={tableName}", new { TableName = tableName, ErrorMessage = result.Message });
                    throw new Exception($"加载表结构写入异常：{result.Message}");
                }
                return tableInfo.Table_Id; // Return the new Table_Id
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LogLevel.Error, VOL.Core.Enums.LogEvent.Exception, $"InitTable 方法异常: TableName={tableName}", new { TableName = tableName }, ex);
                // Wrap the original exception to provide more context
                throw new Exception($"初始化表信息时发生内部错误: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Loads table configuration details, initializing them if necessary.
        /// </summary>
        /// <param name="parentId">The parent ID in the table tree.</param>
        /// <param name="tableName">The name of the database table.</param>
        /// <param name="columnCNName">The Chinese name/description for the table.</param>
        /// <param name="nameSpace">The namespace for generated code.</param>
        /// <param name="foldername">The folder name for generated code.</param>
        /// <param name="tableId">The existing table ID if any.</param>
        /// <param name="isTreeLoad">A flag indicating if this is part of a tree loading operation.</param>
        /// <returns>A WebResponseContent object containing the table information or an error message.</returns>
        public object LoadTable(int parentId, string tableName, string columnCNName, string nameSpace, string foldername, int tableId, bool isTreeLoad)
        {
            try
            {
                // Authorization check: Only super admins can perform this unless it's a tree load
                if (!UserContext.Current.IsSuperAdmin && !isTreeLoad)
                {
                    return new WebResponseContent().Error("只有超级管理员才能进行此操作");
                }

                // Initialize table if not already done (or get existing tableId)
                tableId = InitTable(parentId, tableName?.Trim(), columnCNName, nameSpace, foldername, tableId, isTreeLoad);
                if (tableId == -1 && !isTreeLoad) // -1 indicates an issue like empty table name from InitTable
                {
                     return new WebResponseContent().Error($"表名 {(tableName?.Trim())} 为空或无效。");
                }

                // Retrieve the complete table information including columns
                Sys_TableInfo tableInfo = repository.FindAsIQueryable(x => x.Table_Id == tableId)
                    .Include(c => c.TableColumns).FirstOrDefault();

                if (tableInfo == null)
                {
                    VOL.Core.Services.Logger.Warning(VOL.Core.Enums.LogLevel.Warning, VOL.Core.Enums.LogEvent.Select, $"LoadTable: 未找到TableInfo: TableId={tableId}", new {TableId = tableId});
                    return new WebResponseContent().Error($"未找到ID为 {tableId} 的表配置信息。");
                }

                // Order columns by OrderNo for consistent display
                if (tableInfo.TableColumns != null)
                {
                    tableInfo.TableColumns = tableInfo.TableColumns.OrderByDescending(x => x.OrderNo).ToList();
                }
                return new WebResponseContent().OK(null, tableInfo); // Return success with tableInfo data
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LogLevel.Error, VOL.Core.Enums.LogEvent.Exception, $"LoadTable 方法异常: TableName={tableName}, TableId={tableId}", new { ParentId = parentId, TableName = tableName, TableId = tableId }, ex);
                return new WebResponseContent().Error($"加载表数据时发生内部错误。");
            }
        }

        /// <summary>
        /// Deletes a tree node (table configuration) if it's an empty node without columns or children.
        /// </summary>
        /// <param name="table_Id">The ID of the table configuration to delete.</param>
        /// <returns>A WebResponseContent indicating success or failure.</returns>
        public async Task<WebResponseContent> DelTree(int table_Id)
        {
            try
            {
                if (table_Id == 0) return new WebResponseContent().Error("没有传入参数");

                // Retrieve the table info, including its columns
                Sys_TableInfo tableInfo = await repository.FindAsIQueryable(x => x.Table_Id == table_Id)
                    .Include(c => c.TableColumns).FirstOrDefaultAsync();

                if (tableInfo == null) return new WebResponseContent().OK("要删除的节点不存在。"); // Not an error if already deleted

                // Validation checks before deletion
                if (tableInfo.TableColumns != null && tableInfo.TableColumns.Count > 0)
                {
                    return new WebResponseContent().Error("当前删除的节点存在表结构信息,只能删除空节点");
                }
                if (await repository.ExistsAsync(x => x.ParentId == table_Id)) // Check for child nodes
                {
                    return new WebResponseContent().Error("当前删除的节点存在子节点，不能删除");
                }

                // Perform deletion and save changes
                repository.Delete(tableInfo, true); // 'true' likely means save changes immediately or mark for saving
                await repository.SaveChangesAsync(); // Explicitly save changes if Delete doesn't do it
                return new WebResponseContent().OK("节点删除成功。");
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LogLevel.Error, VOL.Core.Enums.LogEvent.Delete, $"DelTree 方法异常: Table_Id={table_Id}", new { Table_Id = table_Id }, ex);
                return new WebResponseContent().Error("删除树节点时发生内部错误。");
            }
        }

        /// <summary>
        /// Internal method to generate the content of an entity model class file and write it to disk.
        /// </summary>
        /// <param name="sysColumn">List of column information for the entity.</param>
        /// <param name="tableInfo">Table configuration details (name, namespace, etc.).</param>
        /// <param name="tableColumnInfoList">List of actual database column types and properties.</param>
        /// <param name="createType">Type of model to create (1: DomainModel, 2: ApiInput, 3: ApiOutput).</param>
        /// <returns>An empty string on success, or an error message on failure.</returns>
        private string CreateEntityModel(List<Sys_TableColumn> sysColumn, Sys_TableInfo tableInfo, List<TableColumnInfo> tableColumnInfoList, int createType)
        {
            try
            {
                // Select the appropriate template based on createType
                string template = "";
                if (createType == 1) template = "DomainModel.html";
                else if (createType == 2) template = "ApiInputDomainModel.html";
                else template = "ApiOutputDomainModel.html";

                string domainContent = FileHelper.ReadFile(Path.Combine("Template","DomianModel",template));
                string partialContent = domainContent; // For partial class file
                StringBuilder AttributeBuilder = new StringBuilder(); // To build property attributes
                sysColumn = sysColumn.OrderByDescending(c => c.OrderNo).ToList(); // Ensure consistent order
                bool addIgnore = false; // Flag to add Newtonsoft.Json.JsonIgnore using statement

                // Iterate through each column to generate property definition and attributes
                foreach (Sys_TableColumn column in sysColumn)
                {
                    column.ColumnType = (column.ColumnType ?? "").Trim();
                    // Summary comment
                    AttributeBuilder.Append("/// <summary>"); AttributeBuilder.Append("\r\n");
                    AttributeBuilder.Append("       ///" + column.ColumnCnName + ""); AttributeBuilder.Append("\r\n");
                    AttributeBuilder.Append("       /// </summary>"); AttributeBuilder.Append("\r\n");
                    // Key attribute
                    if (column.IsKey == 1) { AttributeBuilder.Append(@"       [Key]" + ""); AttributeBuilder.Append("\r\n"); }
                    // DisplayName attribute
                    AttributeBuilder.Append("       [Display(Name =\"" + ( string.IsNullOrEmpty(column.ColumnCnName) ? column.ColumnName : column.ColumnCnName ) + "\")]"); AttributeBuilder.Append("\r\n");

                    // Adjust MaxLength for very large varchar/nvarchar (handled by (max) in SQL Server)
                    TableColumnInfo tableColumnInfo = tableColumnInfoList.Where(x => x.ColumnName.ToLower().Trim() == column.ColumnName.ToLower().Trim()).FirstOrDefault();
                    if (tableColumnInfo != null && (tableColumnInfo.ColumnType == "varchar" && column.Maxlength > 8000) || (tableColumnInfo.ColumnType == "nvarchar" && column.Maxlength > 4000)) { column.Maxlength = 0; }

                    // MaxLength attribute
                    if (column.ColumnType == "string" && column.Maxlength > 0 && column.Maxlength < 8000) { AttributeBuilder.Append("       [MaxLength(" + column.Maxlength + ")]"); AttributeBuilder.Append("\r\n"); }

                    // JsonIgnore attribute for non-database columns in domain model
                    if (column.IsColumnData == 0 && createType == 1) { addIgnore = true; AttributeBuilder.Append("       [JsonIgnore]"); AttributeBuilder.Append("\r\n"); }

                    if (tableColumnInfo != null) {
                        // DisplayFormat for decimal precision/scale
                        if (!string.IsNullOrEmpty(tableColumnInfo.Prec_Scale) && !tableColumnInfo.Prec_Scale.EndsWith(",0")) { AttributeBuilder.Append("       [DisplayFormat(DataFormatString=\"" + tableColumnInfo.Prec_Scale + "\")]"); AttributeBuilder.Append("\r\n"); }
                        // Determine if column type should be Guid
                        if ( (DBType.Name.ToLower() == DbCurrentType.Oracle.ToString().ToLower() && (column.Maxlength == 36)) || ((column.IsKey == 1 && (column.ColumnType == "uniqueidentifier")) || tableColumnInfo.ColumnType.ToLower() == "guid" || ((IsMysql() || IsDM()) && column.ColumnType == "string" && column.Maxlength == 36))) { tableColumnInfo.ColumnType = "uniqueidentifier"; }

                        // Column TypeName attribute
                        string maxLength = string.Empty;
                        if (tableColumnInfo.ColumnType != "uniqueidentifier") {
                            if (column.IsKey != 1 && column.ColumnType.ToLower() == "string") {
                                if (column.Maxlength <= 0 || (tableColumnInfo.ColumnType == "varchar" && column.Maxlength > 8000) || (tableColumnInfo.ColumnType == "nvarchar" && column.Maxlength > 4000)) { maxLength = "(max)"; }
                                else { maxLength = "(" + column.Maxlength + ")"; }
                            } else if (column.IsKey == 1 && column.ColumnType.ToLower() == "string" && column.Maxlength != 36) { maxLength = "(" + column.Maxlength + ")"; }
                        }
                        AttributeBuilder.Append("       [Column(TypeName=\"" + tableColumnInfo.ColumnType + maxLength + "\")]"); AttributeBuilder.Append("\r\n");

                        // Adjust C# type based on DB type
                        if (tableColumnInfo.ColumnType == "int" || tableColumnInfo.ColumnType == "bigint" || tableColumnInfo.ColumnType == "long") { column.ColumnType = tableColumnInfo.ColumnType == "int" ? "int" : "long"; }
                        if (tableColumnInfo.ColumnType == "bool") { column.ColumnType = "bit"; }
                    }
                    // Editable attribute
                    if (column.EditRowNo != null) { AttributeBuilder.Append("       [Editable(true)]"); AttributeBuilder.Append("\r\n"); }
                    // Required attribute
                    if (column.IsNull == 0 || (createType == 2 && column.ApiIsNull == 0)) { AttributeBuilder.Append("       [Required(AllowEmptyStrings=false)]"); AttributeBuilder.Append("\r\n"); }

                    // Determine C# property type (nullable, Guid, etc.)
                    string columnType = (column.ColumnType == "Date" ? "DateTime" : column.ColumnType).Trim();
                    if (new string[] { "guid", "uniqueidentifier" }.Contains(tableColumnInfo?.ColumnType?.ToLower())) { columnType = "Guid"; }
                    if (column.ColumnType.ToLower() != "string" && column.IsNull == 1) { columnType = columnType + "?"; } // Nullable value types
                    if ((column.IsKey == 1 && (column.ColumnType == "uniqueidentifier")) || column.ColumnType == "guid" || ((IsMysql() || IsDM() || IsOracle()) && column.ColumnType == "string" && column.Maxlength == 36)) { columnType = "Guid" + (column.IsNull == 1 ? "?" : ""); }

                    AttributeBuilder.Append("       public " + columnType + " " + column.ColumnName + " { get; set; }"); AttributeBuilder.Append("\r\n\r\n       ");
                }
                // Add navigation property for detail table if specified
                if (!string.IsNullOrEmpty(tableInfo.DetailName) && createType == 1) {
                    AttributeBuilder.Append("[Display(Name =\"" + tableInfo.DetailCnName + "\")]"); AttributeBuilder.Append("\r\n       ");
                    AttributeBuilder.Append("[ForeignKey(\"" + sysColumn.FirstOrDefault(x => x.IsKey == 1)?.ColumnName + "\")]"); AttributeBuilder.Append("\r\n       ");
                    AttributeBuilder.Append("public List<" + tableInfo.DetailName + "> " + tableInfo.DetailName + " { get; set; }"); AttributeBuilder.Append("\r\n");
                }
                // Add using for JsonIgnore if needed
                if (addIgnore && createType == 1) { domainContent = "using Newtonsoft.Json;\r\n" + domainContent + "\r\n"; }

                string mapPath = ProjectPath.GetProjectDirectoryInfo()?.FullName;
                if (string.IsNullOrEmpty(mapPath)) return "未找到生成的目录!";

                string[] splitArrr = tableInfo.Namespace.Split('.');
                domainContent = domainContent.Replace("{TableName}", tableInfo.TableName).Replace("{AttributeList}", AttributeBuilder.ToString()).Replace("{StartName}", StratName);

                // Build Entity attribute
                List<string> entityAttribute = new List<string> { $"TableCnName = \"{tableInfo.ColumnCNName}\"" };
                if (!string.IsNullOrEmpty(tableInfo.TableTrueName)) entityAttribute.Add($"TableName = \"{tableInfo.TableTrueName}\"");
                if (!string.IsNullOrEmpty(tableInfo.DetailName) && createType == 1) entityAttribute.Add($"DetailTable =  new Type[] {{ typeof({string.Join("),typeof(", tableInfo.DetailName.Split(','))})}}");
                if (!string.IsNullOrEmpty(tableInfo.DetailCnName)) entityAttribute.Add($"DetailTableCnName = \"{tableInfo.DetailCnName}\"");
                if (!string.IsNullOrEmpty(tableInfo.DBServer) && createType == 1) entityAttribute.Add($"DBServer = \"{tableInfo.DBServer}\"");

                string modelNameSpace = StratName + ".Entity";
                string tableAttr = string.Join(",", entityAttribute);
                if (tableAttr != "") tableAttr = $"[Entity({tableAttr})]";
                // Add Table attribute if alias is used
                if (!string.IsNullOrEmpty(tableInfo.TableTrueName) && tableInfo.TableName != tableInfo.TableTrueName)
                { string tableTrueName = tableInfo.TableTrueName; if (DBType.Name == DbCurrentType.PgSql.ToString()) tableTrueName = tableTrueName.ToLower(); tableAttr = $"{tableAttr}\r\n[Table(\"{tableTrueName}\")]"; }

                domainContent = domainContent.Replace("{AttributeManager}", tableAttr).Replace("{Namespace}", modelNameSpace);

                string folderName = tableInfo.FolderName;
                string currentTableName = tableInfo.TableName;
                // Adjust folder and name for API input/output models
                if (createType == 2) { folderName = Path.Combine("ApiEntity", "Input"); currentTableName = "Api" + tableInfo.TableName + "Input"; }
                else if (createType == 3) { folderName = Path.Combine("ApiEntity", "OutPut"); currentTableName = "Api" + tableInfo.TableName + "Output"; }

                // Write main model file
                string modelPath = Path.Combine(mapPath, modelNameSpace, "DomainModels", folderName);
                FileHelper.WriteFile(modelPath, currentTableName + ".cs", domainContent);

                // Write partial model file if it doesn't exist
                string partialModelPath = Path.Combine(modelPath, "partial");
                if (!FileHelper.FileExists(Path.Combine(partialModelPath, currentTableName + ".cs")))
                {
                    partialContent = partialContent.Replace("{AttributeManager}", "").Replace("{AttributeList}", @"//此处配置字段(字段配置见此model的另一个partial),如果表中没有此字段请加上 [NotMapped]属性，否则会异常").Replace(":BaseEntity", "").Replace("{TableName}", tableInfo.TableName).Replace("{Namespace}", modelNameSpace);
                    FileHelper.WriteFile(partialModelPath, currentTableName + ".cs", partialContent);
                }
                // Write mapping configuration (seems specific to createType 1)
                if (createType == 1) { string mappingConfiguration = FileHelper.ReadFile(Path.Combine("Template","DomianModel","MappingConfiguration.html")).Replace("{TableName}", tableInfo.TableName).Replace("{Namespace}", modelNameSpace).Replace("{StartName}", StratName); }
                return ""; // Success
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LogLevel.Error, VOL.Core.Enums.LogEvent.Exception, $"内部CreateEntityModel失败: TableName={tableInfo?.TableName}, CreateType={createType}", tableInfo?.Serialize(), ex);
                return $"生成实体类(内部)时发生错误: {ex.Message}";
            }
        }

        /// <summary>
        /// Generates the column definitions string for a Vue grid.
        /// </summary>
        private StringBuilder GetGridColumns(List<Sys_TableColumn> list, string expressField, bool detail, bool vue = false, bool app = false)
        {
            totalCol = 0; totalWidth = 0; // Reset global counters (consider making these local or passing as params if thread safety is a concern)
            StringBuilder sb = new StringBuilder();
            Func<Sys_TableColumn, bool> func = x => true;
            bool sort = false; // Seems unused, consider removing

            // Filter columns for App display
            if (app) { func = x => new int[] { 1, 2, 3, 4 }.Any(c => c == x.Enable) && (x.IsDisplay == null || x.IsDisplay == 1); }

            foreach (Sys_TableColumn item in list.Where(func).OrderByDescending(x => x.OrderNo)) {
                if (item.IsColumnData == 0) { continue; } // Skip non-data columns
                sb.Append("{field:'" + item.ColumnName + "',");
                sb.Append("title:'" + (string.IsNullOrEmpty(item.ColumnCnName) ? item.ColumnName : item.ColumnCnName) + "',");

                if (vue) {
                    string colType = item.ColumnType.ToLower();
                    // Determine special column types (img, excel, file, date)
                    if (item.IsImage == 1) colType = "img";
                    else if (item.IsImage == 2) colType = "excel";
                    else if (item.IsImage == 3) colType = "file";
                    else if (item.IsImage == 4) colType = "date";
                    sb.Append("type:'" + colType + "',");
                    if (!string.IsNullOrEmpty(item.DropNo)) sb.Append("bind:{ key:'" + item.DropNo + "',data:[]},"); // Data source binding
                    if (expressField != null && expressField.ToLower() == item.ColumnName.ToLower()) sb.Append("link:true,"); // Link for express field
                    if (item.Sortable == 1 && !app) sb.Append("sort:true,");
                }
                else { sb.Append("datatype:'" + item.ColumnType + "',"); } // Older/non-Vue version?

                if (!app) sb.Append("width:" + (item.ColumnWidth ?? 90) + ","); // Column width
                if (item.IsDisplay == 0) sb.Append("hidden:true,"); // Hidden column
                else { totalCol++; totalWidth += item.ColumnWidth == null ? 0 : Convert.ToInt32(item.ColumnWidth); } // Track visible columns/width

                if (item.IsReadDataset == 1) sb.Append("readonly:true,"); // Readonly column

                // Editable column configuration
                if (item.EditRowNo != null && item.EditRowNo > 0 && detail) {
                    string editText = vue ? "edit" : "editor";
                    if (vue) { sb.Append("edit:{type:'" + item.EditType + "'},"); }
                    else { /* Older editor configuration */ }
                }
                // Custom formatters (non-Vue)
                if (!vue) {
                    if (expressField != null && expressField.ToLower() == item.ColumnName.ToLower()) { /* Express field formatter */ }
                    else if (!string.IsNullOrEmpty(item.Script)) { /* Custom script formatter */ }
                    else if (item.IsImage == 1) { /* Image formatter */ }
                    else if (!string.IsNullOrEmpty(item.DropNo) && !detail) { /* Dropdown formatter */ }
                }
                if (item.IsNull == 0 && !app) sb.Append("require:true,"); // Required field indicator
                if (!app && (item.ColumnType.ToLower() == "datetime" || (item.IsDisplay == 1 & !sort))) { sb.Append("align:'left'},"); }
                else { if (!app) sb.Append("align:'left'},"); } // Default alignment

                if (app) { sb.Append("},").Replace(",},", "},"); } // Cleanup for App
                sb.AppendLine(); sb.Append("                       "); // Formatting
            } return sb;
        }

        // Array of numeric types that might default to "number" or "decimal" display type
        private static string[] formType = new string[] { "bigint", "int", "decimal", "float", "byte" };

        /// <summary>
        /// Determines the display type for a form field based on search/edit type and column type.
        /// </summary>
        private string GetDisplayType(bool search, string searchType, string editType, string columnType)
        {
            string type = "";
            if (search) { type = searchType == "无" ? "" : searchType ?? ""; } // "无" means no specific type
            else { type = editType == "无" ? "" : editType ?? ""; }
            if (type == "" && formType.Contains(columnType)) { // Default for numeric types
                if (columnType == "decimal" || columnType == "float") { type = "decimal"; }
                else { type = "number"; }
            }
            return type;
        }

        /// <summary>
        /// Gets the string representation for a dropdown data source.
        /// </summary>
        private string GetDropString(string dropNo, bool vue)
        {
            if (string.IsNullOrEmpty(dropNo)) return vue ? "''" : "__[]__"; // Empty/default
            if (vue) return dropNo; // For Vue, it's the key name
            return "__" + "optionConfig" + dropNo + "__"; // For older, it's a variable name
        }

        /// <summary>
        /// Populates a list of panel HTML configurations for form generation.
        /// </summary>
        private void GetPanelData(List<Sys_TableColumn> list, List<List<PanelHtml>> panelHtml, Func<Sys_TableColumn, int?> keySelector, Func<Sys_TableColumn, bool> predicate, bool search, Func<Sys_TableColumn, int?> orderBy, bool vue = false, bool app = false)
        {
            if (app) {
                list.ForEach(x => { if (x.EditRowNo == 0) x.EditRowNo = 99999; }); // Default EditRowNo for App
                var arr = search ? new int[] { 1, 3, 5, 6 } : new int[] { 1, 2, 5, 7 }; // Enable flags for App
                predicate = x => arr.Any(c => c == x.Enable);
            }
            var whereReslut = list.Where(predicate).OrderBy(orderBy).ThenByDescending(c => c.OrderNo).ToList();
            foreach (var item in whereReslut.GroupBy(keySelector)) { // Group by row number
                panelHtml.Add(item.OrderBy(c => search ? c.SearchColNo : c.EditColNo).Select( x => new PanelHtml {
                    text = x.ColumnCnName ?? x.ColumnName,
                    id = x.ColumnName,
                    displayType = GetDisplayType(search, x.SearchType, x.EditType, x.ColumnType),
                    require = !search && x.IsNull == 0 ? true : false,
                    columnType = vue && x.IsImage == 1 ? "img" : (x.ColumnType ?? "string").ToLower(),
                    disabled = !search && x.IsReadDataset == 1 ? true : false,
                    dataSource = GetDropString(x.DropNo, vue),
                    colSize = search && x.SearchType != "checkbox" ? 0 : (x.ColSize ?? 0) // Column size for layout
                }).ToList());
            }
        }

        // Helper methods to check current DB type
        private static bool IsOracle() { return DBType.Name.ToLower() == DbCurrentType.Oracle.ToString().ToLower(); }
        private static bool IsMysql() { return DBType.Name.ToLower() == DbCurrentType.MySql.ToString().ToLower(); }
        private static bool IsDM() { return DBType.Name.ToLower() == DbCurrentType.DM.ToString().ToLower(); }

        /// <summary>
        /// Validates column string configuration, especially for master-detail relationships.
        /// Checks if the foreign key in the detail table matches the primary key in the main table.
        /// </summary>
        /// <param name="tableInfo">The table information containing column details.</param>
        /// <returns>A WebResponseContent indicating success or an error message if validation fails.</returns>
        private WebResponseContent ValidColumnString(Sys_TableInfo tableInfo)
        {
            WebResponseContent webResponse = new WebResponseContent(true);
            try
            {
                if (tableInfo.TableColumns == null || tableInfo.TableColumns.Count == 0) return webResponse; // No columns to validate

                // Validation for master-detail setup
                if (!string.IsNullOrEmpty(tableInfo.DetailName))
                {
                    // Find primary key of the main table
                    Sys_TableColumn mainTableColumn = tableInfo.TableColumns.FirstOrDefault(x => x.IsKey == 1);
                    if (mainTableColumn == null)
                        return webResponse.Error($"请勾选表[{tableInfo.TableName}]的主键");

                    string key = mainTableColumn.ColumnName;

                    // Find corresponding foreign key in the detail table (DB call)
                    Sys_TableColumn tableColumn = repository.Find<Sys_TableColumn>(x => x.TableName == tableInfo.DetailName && x.ColumnName == key)
                                                    .FirstOrDefault();

                    if (tableColumn == null)
                        return webResponse.Error($"明细表必须包括[{tableInfo.TableName}]主键字段[{key}]");

                    // Validate that key types match
                    if (mainTableColumn.ColumnType?.ToLower() != tableColumn.ColumnType?.ToLower())
                    {
                        return webResponse.Error($"明细表的字段[{tableColumn.ColumnName}]类型必须与主表的主键的类型相同");
                    }
                    // Specific validation for Guid string keys in MySQL/DM
                    if ((IsMysql() || IsDM()) && mainTableColumn.ColumnType?.ToLower() == "string" && mainTableColumn.Maxlength == 36)
                    {
                        if (tableColumn.Maxlength != 36)
                        {
                            return webResponse.Error($"主表主键类型为Guid字符串，明细表[{tableInfo.DetailName}]配置的字段[{key}]长度必须是36，请将其长度设置为36。");
                        }
                    }
                }
                return webResponse; // Validation passed
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LogLevel.Error, VOL.Core.Enums.LogEvent.Exception, $"ValidColumnString方法异常: TableName={tableInfo?.TableName}", tableInfo?.Serialize(), ex);
                return webResponse.Error("验证表列信息时发生内部错误。");
            }
        }

        // 此辅助方法用于扫描字符串内容，检测是否存在潜在的危险JavaScript模式，
        // 如 eval(), new Function(), setTimeout/setInterval 的字符串参数, 或 <script> 标签。
        // 目的是防止在代码生成过程中将用户提供的、可能包含恶意脚本的元数据直接注入到客户端代码中。
        // (This helper method is used to scan string content for potentially dangerous JavaScript patterns,
        // such as eval(), new Function(), string arguments for setTimeout/setInterval, or <script> tags.
        // The purpose is to prevent user-provided metadata, which might contain malicious scripts,
        // from being directly injected into client-side code during code generation.)
        private static bool ContainsPotentiallyDangerousScript(string scriptContent)
        {
            if (string.IsNullOrEmpty(scriptContent))
            {
                return false;
            }
            // Regex patterns for potentially dangerous JS
            var patterns = new[]
            {
                @"eval\s*\(", // eval(...)
                @"new\s+Function\s*\(", // new Function(...)
                @"setTimeout\s*\(\s*['""]", // setTimeout('...') or setTimeout("...")
                @"setInterval\s*\(\s*['""]", // setInterval('...') or setInterval("...")
                @"<\s*script\s*>" // <script>
                // Consider adding more patterns like on<event> attributes, javascript: urls etc. if metadata can go into HTML attributes
            };

            foreach (var pattern in patterns)
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(scriptContent, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// Represents the HTML configuration for a panel item (form field) in the UI generator.
    /// </summary>
    public class PanelHtml
    {
        public string text { get; set; }
        public string id { get; set; }
        public string displayType { get; set; }
        public string dataSource { get; set; }
        public string columnType { get; set; }
        public bool require { get; set; }
        public bool disabled { get; set; }
        public int colSize { get; set; }
        public int fileMaxCount { get; set; } // Max number of files for file upload type
    }
}
