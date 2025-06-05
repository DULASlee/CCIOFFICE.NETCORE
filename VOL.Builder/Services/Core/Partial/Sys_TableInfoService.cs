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
    public partial class Sys_TableInfoService
    {
        private int totalWidth = 0;
        private int totalCol = 0;
        private string webProject = null;
        private string apiNameSpace = null;
        private string startName = "";
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
        private string WebProject
        {
            get
            {
                if (webProject != null)
                    return webProject;
                webProject = ProjectPath.GetLastIndexOfDirectoryName(".WebApi") ?? ProjectPath.GetLastIndexOfDirectoryName("Api") ?? ProjectPath.GetLastIndexOfDirectoryName(".Web");
                if (webProject == null)
                {
                    VOL.Core.Services.Logger.Error(LogLevel.Critical, LogEvent.Exception, "关键配置错误: 未获取到以.WebApi结尾的项目名称,无法创建页面");
                    throw new Exception("未获取到以.WebApi结尾的项目名称,无法创建页面");
                }
                return webProject;
            }
        }
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

        public async Task<(string, string)> GetTableTree()
        {
            try
            {
                var treeData = await repository.FindAsIQueryable(x => 1 == 1)
                    .Select(c => new
                    {
                        id = c.Table_Id,
                        pId = c.ParentId,
                        parentId = c.ParentId,
                        name = c.ColumnCNName,
                        orderNo = c.OrderNo
                    }).OrderByDescending(c => c.orderNo).ToListAsync();
                var treeList = treeData.Select(a => new
                {
                    a.id,
                    a.pId,
                    a.parentId,
                    a.name,
                    isParent = treeData.Select(x => x.pId).Contains(a.id)
                });
                string startsWith = WebProject.Substring(0, WebProject.IndexOf('.'));
                return (treeList.Count() == 0 ? "[]" : treeList.Serialize() ?? "", ProjectPath.GetProjectFileName(startsWith));
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(LogLevel.Error, LogEvent.Exception, "GetTableTree方法异常",null, ex);
                throw new Exception("获取表结构树时发生错误。", ex);
            }
        }

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
                return "";
            }
        }

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
                return "";
            }
        }

        private string GetMySqlModelInfo() { /* ... SQL string ... */ return $@"SELECT DISTINCT CONCAT(NUMERIC_PRECISION,',',NUMERIC_SCALE) as Prec_Scale, CASE WHEN data_type IN( 'BIT', 'BOOL','bit', 'bool') THEN 'bool' WHEN data_type in('smallint','SMALLINT') THEN 'short' WHEN data_type in('tinyint', 'TINYINT') THEN 'sbyte' WHEN data_type IN('MEDIUMINT','mediumint', 'int','INT','year', 'Year') THEN 'int' WHEN data_type in ( 'BIGINT','bigint') THEN 'bigint' WHEN data_type IN('FLOAT',  'DECIMAL','float', 'decimal') THEN 'decimal' WHEN data_type IN( 'DOUBLE', 'double') THEN 'double' WHEN data_type IN('CHAR', 'VARCHAR', 'TINY TEXT', 'TEXT', 'MEDIUMTEXT', 'LONGTEXT', 'TINYBLOB', 'BLOB', 'MEDIUMBLOB', 'LONGBLOB', 'Time','char', 'varchar', 'tiny text', 'text', 'mediumtext', 'longtext', 'tinyblob', 'blob', 'mediumblob', 'longblob', 'time') THEN 'nvarchar' WHEN data_type IN('Date', 'DateTime', 'TimeStamp','date', 'datetime', 'timestamp') THEN 'datetime' ELSE 'nvarchar' END AS ColumnType, Column_Name AS ColumnName FROM information_schema.COLUMNS WHERE table_name = ?tableName {GetMysqlTableSchema()};"; }
        private string GetDMModelInfo() { /* ... SQL string ... */ return $@"SELECT DISTINCT IF(DATA_PRECISION IS NOT NULL, CONCAT(DATA_PRECISION,',',DATA_SCALE),'') as Prec_Scale, CASE WHEN data_type IN( 'BIT', 'BOOL','bit', 'bool') THEN 'bool' WHEN data_type in('smallint','SMALLINT') THEN 'short' WHEN data_type in('tinyint', 'TINYINT') THEN 'sbyte' WHEN data_type IN('MEDIUMINT','mediumint', 'int','INT','year', 'Year') THEN 'int' WHEN data_type in ( 'BIGINT','bigint') THEN 'bigint' WHEN data_type IN('FLOAT',  'DECIMAL','float', 'decimal') THEN 'decimal' WHEN data_type IN( 'DOUBLE', 'double') THEN 'double' WHEN data_type IN('CHAR', 'VARCHAR', 'TINY TEXT', 'TEXT', 'MEDIUMTEXT', 'LONGTEXT', 'TINYBLOB', 'BLOB', 'MEDIUMBLOB', 'LONGBLOB', 'Time','char', 'varchar', 'tiny text', 'text', 'mediumtext', 'longtext', 'tinyblob', 'blob', 'mediumblob', 'longblob', 'time') THEN 'nvarchar' WHEN data_type IN('Date', 'DateTime', 'TimeStamp','date', 'datetime', 'timestamp') THEN 'datetime' ELSE 'nvarchar' END AS ColumnType, Column_Name AS ColumnName FROM user_tab_columns WHERE table_name = :tableName "; }
        private string GetSqlServerModelInfo() { /* ... SQL string ... */ return $@" SELECT CASE WHEN t.ColumnType IN ('DECIMAL','smallmoney','money') THEN CONVERT(VARCHAR(30),t.Prec)+','+CONVERT(VARCHAR(30),t.Scale) ELSE '' END AS Prec_Scale,t.ColumnType,t.ColumnName FROM ( SELECT col.prec AS 'Prec',col.scale AS 'Scale',t.name AS ColumnType,col.name AS ColumnName FROM dbo.syscolumns col LEFT JOIN dbo.systypes t ON col.xtype = t.xusertype INNER JOIN dbo.sysobjects obj ON col.id = obj.id AND obj.xtype IN ('U','V') AND obj.status >= 0 LEFT JOIN dbo.syscomments comm ON col.cdefault = comm.id LEFT JOIN sys.extended_properties ep ON col.id = ep.major_id AND col.colid = ep.minor_id AND ep.name = 'MS_Description' LEFT JOIN sys.extended_properties epTwo ON obj.id = epTwo.major_id AND epTwo.minor_id = 0 AND epTwo.name = 'MS_Description' WHERE obj.name =@tableName) AS t"; }
        private string GetOracleModelInfo(string tableName) { /* ... SQL string ... */ return $@"SELECT c.TABLE_NAME TableName , cc.COLUMN_NAME COLUMNNAME, cc.COMMENTS as ColumnCNName, CASE WHEN c.DATA_TYPE IN('smallint', 'INT') or (c.DATA_TYPE='NUMBER' and c.DATA_LENGTH=0) THEN 'int' WHEN c.DATA_TYPE IN('NUMBER') THEN 'decimal' WHEN c.DATA_TYPE IN('CHAR', 'VARCHAR', 'NVARCHAR','VARCHAR2', 'NVARCHAR2','text', 'image') THEN 'nvarchar' WHEN c.DATA_TYPE IN('DATE') THEN 'date' ELSE 'nvarchar' end as ColumnType, c.DATA_LENGTH as Maxlength, case WHEN c.NULLABLE='Y' THEN 1 ELSE 0 end as ISNULL FROM ALL_tab_columns c LEFT JOIN ALL_col_comments cc ON c.table_name = cc.table_name AND c.column_name = cc.column_name LEFT JOIN ALL_tab_comments t ON c.table_name = t.table_name WHERE c.table_name='{tableName.ToUpper()}'"; }
        private string GetPgSqlModelInfo() { /* ... SQL string ... */ StringBuilder stringBuilder = new StringBuilder(); stringBuilder.Append(" SELECT "); stringBuilder.Append(" col.COLUMN_NAME AS \"ColumnName\", "); stringBuilder.Append(" CASE "); stringBuilder.Append(" WHEN col.udt_name = 'uuid' THEN 'guid' "); stringBuilder.Append(" WHEN col.udt_name IN ( 'int2') THEN 'short' "); stringBuilder.Append(" WHEN col.udt_name IN ( 'int4' ) THEN 'int' "); stringBuilder.Append(" WHEN col.udt_name = 'int8' THEN 'long' "); stringBuilder.Append(" WHEN col.udt_name IN ( 'char', 'varchar', 'text', 'xml', 'bytea' ) THEN 'string' "); stringBuilder.Append(" WHEN col.udt_name IN ( 'bool' ) THEN 'bool' "); stringBuilder.Append(" WHEN col.udt_name IN ( 'date','timestamp' ) THEN 'DateTime' "); stringBuilder.Append(" WHEN col.udt_name IN ( 'decimal', 'money','numeric' ) THEN 'decimal' "); stringBuilder.Append(" WHEN col.udt_name IN ( 'float4', 'float8' ) THEN 'float' ELSE'string ' "); stringBuilder.Append(" END  as ColumnType "); stringBuilder.Append("from information_schema.COLUMNS col  "); stringBuilder.Append("WHERE \"lower\" ( TABLE_NAME ) = \"lower\" (@tableName )  "); return stringBuilder.ToString(); }

        private WebResponseContent ExistsTable(string tableName, string tableTrueName)
        {
            WebResponseContent webResponse = new WebResponseContent(true);
            var compilationLibrary = DependencyContext.Default.CompileLibraries.Where(x => !x.Serviceable && x.Type == "project");
            foreach (var _compilation in compilationLibrary)
            {
                try
                {
                    foreach (var entity in AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName(_compilation.Name))
                        .GetTypes().Where(x => x.GetTypeInfo().BaseType != null && x.BaseType == typeof(BaseEntity)))
                    {
                        if (entity.Name == tableTrueName && !string.IsNullOrEmpty(tableName) && tableName != tableTrueName)
                            return webResponse.Error($"实际表名【{tableTrueName}】已创建实体，不能创建别名【{tableName}】实体");

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
                    VOL.Core.Services.Logger.Error(VOL.Core.Enums.LogLevel.Error, VOL.Core.Enums.LogEvent.Exception, $"反射加载程序集查找表是否存在时异常: Assembly={_compilation?.Name}", null, ex);
                }
            }
            return webResponse;
        }

        public string CreateEntityModel(Sys_TableInfo sysTableInfo)
        {
            if (sysTableInfo == null || sysTableInfo.TableColumns == null || sysTableInfo.TableColumns.Count == 0)
                return "提交的配置数据不完整";

            WebResponseContent webResponse = ValidColumnString(sysTableInfo);
            if (!webResponse.Status) return webResponse.Message;

            string currentTableName = sysTableInfo.TableName;
            webResponse = ExistsTable(currentTableName, sysTableInfo.TableTrueName);
            if (!webResponse.Status) return webResponse.Message;

            if (!string.IsNullOrEmpty(sysTableInfo.TableTrueName) && sysTableInfo.TableTrueName != currentTableName)
            {
                currentTableName = sysTableInfo.TableTrueName;
            }

            try
            {
                string sql = "";
                switch (DBType.Name)
                {
                    case "MySql": sql = GetMySqlModelInfo(); break;
                    case "PgSql": sql = GetPgSqlModelInfo(); break;
                    case "Oracle": sql = GetOracleModelInfo(currentTableName); break;
                    case "DM": sql = GetDMModelInfo(); break;
                    default: sql = GetSqlServerModelInfo(); break;
                }
                List<TableColumnInfo> tableColumnInfoList = repository.DapperContext.QueryList<TableColumnInfo>(sql, new { tableName = currentTableName });
                if (tableColumnInfoList == null || !tableColumnInfoList.Any())
                {
                    return $"未能获取表 '{currentTableName}' 的列信息，请检查表是否存在或数据库连接。";
                }

                List<Sys_TableColumn> list = sysTableInfo.TableColumns;
                string msg = CreateEntityModel(list, sysTableInfo, tableColumnInfoList, 1);
                if (msg != "") return msg;

                return "Model创建成功!";
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LogLevel.Error, VOL.Core.Enums.LogEvent.Exception, $"生成实体模型失败: TableName={sysTableInfo?.TableName}", sysTableInfo?.Serialize(), ex);
                return $"生成实体模型时发生错误: {ex.Message}";
            }
        }

        public WebResponseContent SaveEidt(Sys_TableInfo sysTableInfo)
        {
            try
            {
                WebResponseContent webResponse = ValidColumnString(sysTableInfo);
                if (!webResponse.Status) return webResponse;

                if (sysTableInfo.Table_Id == sysTableInfo.ParentId && sysTableInfo.Table_Id != 0) // ParentId can be 0 for root
                {
                    return WebResponseContent.Instance.Error($"父级id不能为自己");
                }
                if (sysTableInfo.TableColumns != null && sysTableInfo.TableColumns.Any(x => !string.IsNullOrEmpty(x.DropNo) && x.ColumnName == sysTableInfo.ExpressField))
                {
                    return WebResponseContent.Instance.Error($"不能将字段【{sysTableInfo.ExpressField}】设置为快捷编辑,因为已经设置了数据源");
                }
                if (sysTableInfo.TableColumns != null)
                {
                    sysTableInfo.TableColumns.ForEach(x => { x.TableName = sysTableInfo.TableName; });
                }
                sysTableInfo.TableColumns?.ForEach(x => { if (x.IsReadDataset == null) x.IsReadDataset = 0; });

                return repository.UpdateRange<Sys_TableColumn>(sysTableInfo, true, true, null, null, true);
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LogLevel.Error, VOL.Core.Enums.LogEvent.Update, $"保存代码生成配置失败: TableName={sysTableInfo?.TableName}", sysTableInfo?.Serialize(), ex);
                return WebResponseContent.Instance.Error("保存配置信息时发生内部错误。");
            }
        }

        private string GetCurrentSql(string tableName)
        {
            string sql;
            if (DBType.Name.ToLower() == DbCurrentType.MySql.ToString().ToLower()) sql = GetMySqlStructure(tableName);
            else if (DBType.Name.ToLower() == DbCurrentType.PgSql.ToString().ToLower()) sql = GetPgSqlStructure(tableName);
            else if (DBType.Name.ToLower() == DbCurrentType.DM.ToString().ToLower()) sql = GetDMStructure(tableName);
            else if (DBType.Name.ToLower() == DbCurrentType.Oracle.ToString().ToLower()) sql = GetOracleStructure(tableName);
            else sql = GetSqlServerStructure(tableName);
            return sql;
        }

        public async Task<WebResponseContent> SyncTable(string tableName)
        {
            WebResponseContent webResponse = new WebResponseContent();
            try
            {
                if (string.IsNullOrEmpty(tableName)) return webResponse.OK("表名不能为空");

                string originalTableName = tableName;
                Sys_TableInfo tableInfo = await repository.FindAsIQueryable(x => x.TableName == originalTableName)
                                                     .Include(o => o.TableColumns)
                                                     .FirstOrDefaultAsync();

                if (tableInfo == null)
                    return webResponse.Error("未获取到【" + originalTableName + "】的配置信息，请使用新建功能");

                string actualDbTableName = (!string.IsNullOrEmpty(tableInfo.TableTrueName) && tableInfo.TableTrueName != originalTableName)
                                           ? tableInfo.TableTrueName
                                           : originalTableName;

                string sql = GetCurrentSql(actualDbTableName);
                List<Sys_TableColumn> columnsFromDb = await repository.DapperContext.QueryListAsync<Sys_TableColumn>(sql, new { tableName = actualDbTableName });

                if (columnsFromDb == null || !columnsFromDb.Any())
                    return webResponse.Error("未获取到【" + actualDbTableName + "】表结构信息，请确认表是否存在");

                List<Sys_TableColumn> existingConfigColumns = tableInfo.TableColumns ?? new List<Sys_TableColumn>();
                List<Sys_TableColumn> addColumns = new List<Sys_TableColumn>();
                List<Sys_TableColumn> updateColumns = new List<Sys_TableColumn>();

                foreach (Sys_TableColumn dbCol in columnsFromDb)
                {
                    Sys_TableColumn existingCol = existingConfigColumns.FirstOrDefault(x => x.ColumnName == dbCol.ColumnName);
                    if (existingCol == null)
                    {
                        dbCol.TableName = tableInfo.TableName;
                        dbCol.Table_Id = tableInfo.Table_Id;
                        addColumns.Add(dbCol);
                    }
                    else
                    {
                        if (dbCol.ColumnType != existingCol.ColumnType || dbCol.Maxlength != existingCol.Maxlength || (dbCol.IsNull ?? 0) != (existingCol.IsNull ?? 0))
                        {
                            existingCol.ColumnType = dbCol.ColumnType;
                            existingCol.Maxlength = dbCol.Maxlength;
                            existingCol.IsNull = dbCol.IsNull;
                            updateColumns.Add(existingCol);
                        }
                    }
                }
                List<Sys_TableColumn> delColumns = existingConfigColumns.Where(a => !columnsFromDb.Any(c => c.ColumnName == a.ColumnName)).ToList();

                if (!addColumns.Any() && !delColumns.Any() && !updateColumns.Any())
                {
                    return webResponse.OK("【" + actualDbTableName + "】表结构未发生变化,无需同步"); // Changed to OK as it's not an error
                }

                if (addColumns.Any()) repository.AddRange(addColumns);
                if (delColumns.Any()) repository.DbContext.Set<Sys_TableColumn>().RemoveRange(delColumns);
                if (updateColumns.Any()) repository.UpdateRange(updateColumns, x => new { x.ColumnType, x.Maxlength, x.IsNull });

                await repository.DbContext.SaveChangesAsync();

                return webResponse.OK($"新加字段【{addColumns.Count}】个,删除字段【{delColumns.Count}】,修改字段【{updateColumns.Count}】");
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LogLevel.Error, VOL.Core.Enums.LogEvent.Exception, $"同步表结构失败: TableName={tableName}", new { TableName = tableName }, ex);
                return webResponse.Error($"同步表结构时发生错误: {ex.Message}");
            }
        }

        public string CreateServices(string tableName, string nameSpace, string foldername, bool webController, bool apiController)
        {
            try
            {
                var tableColumn = repository.FindAsyncFirst<Sys_TableColumn>(x => x.TableName == tableName).Result;
                if (tableColumn == null) return $"没有查到{tableName}表信息";
                if (string.IsNullOrEmpty(nameSpace) || string.IsNullOrEmpty(foldername)) return $"命名空间、项目文件夹都不能为空";

                string domainContent = "";
                string frameworkFolder = ProjectPath.GetProjectDirectoryInfo()?.FullName;
                if (string.IsNullOrEmpty(frameworkFolder)) return "无法确定项目框架根目录。";

                string[] splitArr = nameSpace.Split('.');
                string projectName = splitArr.Length > 1 ? splitArr[splitArr.Length - 1] : splitArr[0];
                string baseOptions = $"\"{projectName}\",\"{foldername}\",\"{tableName}\"";

                if (apiController)
                {
                    string apiPath = ProjectPath.GetProjectDirectoryInfo().GetDirectories().FirstOrDefault(x => x.Name.ToLower().EndsWith(".webapi"))?.FullName;
                    if (string.IsNullOrEmpty(apiPath)) return "未找到webapi类库,请确认是存在weiapi类库命名以.webapi结尾";

                    string controllerDirPath = Path.Combine(apiPath, "Controllers", projectName);
                    string partialControllerPath = Path.Combine(controllerDirPath, "Partial", tableName + "Controller.cs");
                    string mainControllerPath = Path.Combine(controllerDirPath, tableName + "Controller.cs");

                    if (!FileHelper.FileExists(partialControllerPath))
                    {
                        string partialControllerContent = FileHelper.ReadFile(Path.Combine("Template","Controller","ControllerApiPartial.html"))
                           .Replace("{Namespace}", nameSpace).Replace("{TableName}", tableName).Replace("{StartName}", StratName);
                        FileHelper.WriteFile(Path.GetDirectoryName(partialControllerPath), Path.GetFileName(partialControllerPath), partialControllerContent);
                    }
                    domainContent = FileHelper.ReadFile(Path.Combine("Template","Controller","ControllerApi.html"))
                        .Replace("{Namespace}", nameSpace).Replace("{TableName}", tableName).Replace("{StartName}", StratName).Replace("{BaseOptions}", baseOptions);
                    FileHelper.WriteFile(Path.GetDirectoryName(mainControllerPath), Path.GetFileName(mainControllerPath), domainContent);
                }

                string repoDir = Path.Combine(frameworkFolder, nameSpace, "Repositories", foldername);
                string iRepoDir = Path.Combine(frameworkFolder, nameSpace, "IRepositories", foldername);
                domainContent = FileHelper.ReadFile(Path.Combine("Template","Repositorys","BaseRepository.html")).Replace("{Namespace}", nameSpace).Replace("{TableName}", tableName).Replace("{StartName}", StratName);
                FileHelper.WriteFile(repoDir, tableName + "Repository.cs", domainContent);
                domainContent = FileHelper.ReadFile(Path.Combine("Template","IRepositorys","BaseIRepositorie.html")).Replace("{Namespace}", nameSpace).Replace("{TableName}", tableName).Replace("{StartName}", StratName);
                FileHelper.WriteFile(iRepoDir, "I" + tableName + "Repository.cs", domainContent);

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

                if (webController)
                {
                    string webCtrlPathDir = Path.Combine(frameworkFolder, nameSpace, "Controllers", foldername);
                    string webCtrlFileName = tableName + "Controller.cs";
                    string partialWebCtrlPath = Path.Combine(webCtrlPathDir, "Partial", webCtrlFileName);
                    if (!FileHelper.FileExists(partialWebCtrlPath))
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

        private List<object> GetSearchData(List<List<PanelHtml>> panelHtml, List<Sys_TableColumn> sysColumnList, bool vue = false, bool edit = false, bool app = false)
        {
            if (edit) { GetPanelData(sysColumnList, panelHtml, x => x.EditRowNo, c => c.EditRowNo != null && c.EditRowNo > 0, false, q => q.EditRowNo, vue, app: app); } else { GetPanelData(sysColumnList, panelHtml, x => x.SearchRowNo, c => c.SearchRowNo != null, true, q => q.SearchRowNo, vue, app: app); }
            List<object> list = new List<object>(); int index = 0; bool group = panelHtml.Exists(x => x.Count > 1);
            panelHtml.ForEach(x => { index++; List<Dictionary<string, object>> keyValuePairs = new List<Dictionary<string, object>>();
                x.ForEach(s => { Dictionary<string, object> keyValues = new Dictionary<string, object>();
                    if (vue) { if (!string.IsNullOrEmpty(s.dataSource) && s.dataSource != "''") { if (app) { keyValues.Add("key", s.dataSource); } else { keyValues.Add("dataKey", s.dataSource); } keyValues.Add("data", new string[] { }); } keyValues.Add("title", s.text); if (s.require) { keyValues.Add("required", s.require); } keyValues.Add("field", s.id); if (s.disabled) { keyValues.Add("disabled", true); } if (s.colSize > 0 && !app) { keyValues.Add("colSize", s.colSize); } if (!string.IsNullOrEmpty(s.displayType) && s.displayType != "''") { keyValues.Add("type", s.columnType == "img" ? s.columnType : s.displayType); } }
                    else { keyValues.Add("columnType", s.columnType); if (!string.IsNullOrEmpty(s.dataSource)) { keyValues.Add("dataSource", s.dataSource); } keyValues.Add("text", s.text); if (s.require) { keyValues.Add("require", s.require); } keyValues.Add("id", s.id); }
                    if (!app) { keyValuePairs.Add(keyValues); } else { list.Add(keyValues); } });
                if (!app) { list.Add(keyValuePairs); } else { if (index != panelHtml.Count() && group) { list.Add(new { type = "group" }); } } });
            return list;
        }

        public string CreateVuePage(Sys_TableInfo sysTableInfo, string vuePath)
        {
            try
            {
                bool isVite = HttpContext.Current.Request.Query["vite"].GetInt() > 0;
                bool isApp = HttpContext.Current.Request.Query["app"].GetInt() > 0;
                if (string.IsNullOrEmpty(vuePath)) return isApp ? "请设置App路径" : "请设置Vue所在Views的绝对路径!";
                if (!FileHelper.DirectoryExists(vuePath)) return $"未找项目路径{vuePath}!";
                if (sysTableInfo == null || sysTableInfo.TableColumns == null || !sysTableInfo.TableColumns.Any()) return "提交的配置数据不完整";
                vuePath = vuePath.Trim().TrimEnd('/').TrimEnd('\\');
                List<Sys_TableColumn> sysColumnList = sysTableInfo.TableColumns;
                string[] eidtTye = { "select", "selectList", "drop", "dropList", "checkbox" };
                if (sysColumnList.Exists(x => eidtTye.Contains(x.EditType) && string.IsNullOrEmpty(x.DropNo))) return $"编辑类型为[{string.Join(',', eidtTye)}]时必须选择数据源";
                if (sysColumnList.Exists(x => eidtTye.Contains(x.SearchType) && string.IsNullOrEmpty(x.DropNo))) return $"查询类型为[{string.Join(',', eidtTye)}]时必须选择数据源";
                if (isApp && !sysColumnList.Exists(x => x.Enable > 0)) return $"请设置[app列]";

                bool editLine = false;
                StringBuilder sb = GetGridColumns(sysColumnList, sysTableInfo.ExpressField, detail: editLine, true, app: isApp);
                if (sb.Length == 0) return "未获取到数据!";
                string columns = sb.ToString().Trim().TrimEnd(',');
                string key = sysColumnList.FirstOrDefault(c => c.IsKey == 1)?.ColumnName ?? "";

                Func<Sys_TableColumn, bool> editFunc = c => c.EditRowNo != null && c.EditRowNo > 0;
                if (isApp) editFunc = x => new int[] { 1, 2, 5, 7 }.Any(c => c == x.Enable);
                var formFileds = sysColumnList.Where(editFunc).OrderBy(o => o.EditRowNo).ThenByDescending(t => t.OrderNo)
                    .Select(x => new KeyValuePair<string, object>(x.ColumnName, (x.EditType == "checkbox" || x.EditType == "selectList" || x.EditType == "cascader") ? new string[0] : "" as object))
                    .ToList().ToDictionary(x => x.Key, x => x.Value).Serialize();

                List<List<PanelHtml>> panelHtml = new List<List<PanelHtml>>();
                List<object> searchDataList = GetSearchData(panelHtml, sysColumnList, true, app: isApp);

                string pageContent, editOptions = "", vueOptions = "";
                if (isApp) pageContent = FileHelper.ReadFile(Path.Combine("Template", "Page", "app", "options.html"));
                else if (HttpContext.Current.Request.Query.ContainsKey("v3"))
                { pageContent = FileHelper.ReadFile(Path.Combine("Template", "Page", "Vue3SearchPage.html")); editOptions = FileHelper.ReadFile(Path.Combine("Template", "Page", "EditOptions.html")); vueOptions = FileHelper.ReadFile(Path.Combine("Template", "Page", "VueOptions.html")); }
                else pageContent = FileHelper.ReadFile(Path.Combine("Template", "Page", "VueSearchPage.html"));

                if (string.IsNullOrEmpty(pageContent)) return "未找到Template模板文件";

                Func<Sys_TableColumn, bool> searchFunc = c => c.SearchRowNo != null && c.SearchRowNo > 0;
                if (isApp) { searchFunc = x => new int[] { 1, 3, 5, 6 }.Any(c => c == x.Enable); vueOptions = pageContent; }
                var searchFormFileds = sysColumnList.Where(searchFunc)
                    .Select(x => new KeyValuePair<string, object>(x.ColumnName, (x.SearchType == "checkbox" || x.SearchType == "selectList" || x.EditType == "cascader") ? new string[0] : x.SearchType == "range" ? new string[] { null, null } : "" as object))
                    .ToList().ToDictionary(x => x.Key, x => x.Value).Serialize();

                vueOptions = vueOptions.Replace("#searchFormFileds", searchFormFileds)
                    .Replace("#searchFormOptions", searchDataList.Serialize() ?? "".Replace("},{", "},\r\n                               {").Replace("],[", "],\r\n                              ["));
                panelHtml = new List<List<PanelHtml>>();
                string formOptions = GetSearchData(panelHtml, sysColumnList.Where(editFunc).ToList(), true, true, app: isApp).Serialize() ?? "";

                string[] arr = sysTableInfo.Namespace.Split('.');
                string spaceFolder = (arr.Length > 1 ? arr[arr.Length - 1] : arr[0]).ToLower();

                if (editLine) vueOptions = vueOptions.Replace("'#key'", "'#key',\r\n                editTable:true ");
                vueOptions = vueOptions.Replace("#columns", columns)
                                .Replace("#SortName", string.IsNullOrEmpty(sysTableInfo.SortName) ? key : sysTableInfo.SortName)
                                .Replace("#key", key).Replace("#Foots", " ").Replace("#TableName", sysTableInfo.TableName)
                                .Replace("#cnName", sysTableInfo.ColumnCNName).Replace("#url", $"/{sysTableInfo.TableName}/")
                                .Replace("#folder", spaceFolder).Replace("#editFormFileds", formFileds)
                                .Replace("#editFormOptions", formOptions.Replace("},{", "},\r\n                               {").Replace("],[", "],\r\n                              ["));

                string currentVuePath = vuePath;

                bool hasSubDetail = false;
                List<string> detailItems = new List<string>();
                if (!string.IsNullOrEmpty(sysTableInfo.DetailName))
                {
                    var tables = sysTableInfo.DetailName.Replace("，", ",").Split(',');
                    var detailTables = repository.FindAsIQueryable(x => tables.Contains(x.TableName)).Include(x => x.TableColumns).ToList();
                    if (detailTables.Count != tables.Length) return $"请将明细表生成代码!";
                    var obj = detailTables.FirstOrDefault(c => c.TableColumns == null || !c.TableColumns.Any());
                    if (obj != null) return $"明细表{obj.TableName}没有列的信息,请确认是否有列数据或列数据是否被删除!";
                    var tableCNNameArr = sysTableInfo.DetailCnName?.Replace("，", ",")?.Split(',');
                    if (tableCNNameArr == null || tableCNNameArr.Length != tables.Length) return $"明细表中文名与明细表数量不一致，请以逗号隔开数量也一致!";
                    List<Sys_TableInfo> tables2 = tables.Select(name => detailTables.First(x => x.TableName == name)).ToList();
                    detailTables = tables2;
                    hasSubDetail = detailTables.Exists(c => !string.IsNullOrEmpty(c.DetailName)) || detailTables.Count > 1;
                    int tableIndex = 0;
                    foreach (var detailTable in detailTables)
                    {
                        string tableCNName = tableCNNameArr[tableIndex++];
                        string detailItemFormat = hasSubDetail ?
                            @"  { cnName: '#detailCnName', table: '#detailTable', columns: [#detailColumns], sortName: '#detailSortName', key: '#detailKey', buttons:[], delKeys:[], detail:null }" :
                            @"  { cnName: '#detailCnName', table: '#detailTable', columns: [#detailColumns], sortName: '#detailSortName', key: '#detailKey' }";
                        List<Sys_TableColumn> detailList = detailTable.TableColumns;
                        StringBuilder sbDetail = GetGridColumns(detailList, detailTable.ExpressField, true, true);
                        string detailKey = detailList.First(c => c.IsKey == 1).ColumnName;
                        string detailCols = sbDetail.ToString().Trim().TrimEnd(',');
                        detailItemFormat = detailItemFormat.Replace("#detailColumns", detailCols).Replace("#detailCnName", tableCNName)
                                           .Replace("#detailTable", detailTable.TableName).Replace("#detailKey", detailKey)
                                           .Replace("#detailSortName", string.IsNullOrEmpty(detailTable.SortName) ? detailKey : detailTable.SortName);
                        detailItems.Add(detailItemFormat);
                        if (detailTables.Count == 1)
                        {
                             editOptions = editOptions.Replace("#detailColumns", detailCols).Replace("#detailCnName", detailTable.ColumnCNName)
                                .Replace("#detailTable", detailTable.TableName).Replace("#detailKey", detailKey)
                                .Replace("#detailSortName", string.IsNullOrEmpty(detailTable.SortName) ? detailKey : detailTable.SortName);
                        }
                    }
                    vueOptions = vueOptions.Replace("#tables1", $"{detailItems[0]}");
                    vueOptions = vueOptions.Replace("#tables2", hasSubDetail && detailItems.Count > 1 ? $"[{string.Join(",\r                  ", detailItems.Skip(1))}]" : "[]");
                }
                else
                {
                    string emptyReplacement = isApp ? "[]" : "{columns:[]}";
                    vueOptions = vueOptions.Replace("#tables1", emptyReplacement).Replace("#tables2", "[]");
                    string[]phs = { "#detailColumns", "#detailKey", "#detailSortName", "#detailCnName", "#detailTable" };
                    foreach(var p in phs) { vueOptions = vueOptions.Replace(p, ""); editOptions = editOptions.Replace(p, ""); }
                    vueOptions = vueOptions.Replace("api/#TableName/getDetailPage", "");
                    editOptions = editOptions.Replace("api/#TableName/getDetailPage", "");
                }

                string srcPath = new DirectoryInfo(currentVuePath.MapPath()).Parent.FullName;
                string spaceFolderForPath = spaceFolder.Replace("/", Path.DirectorySeparatorChar.ToString());
                string extensionPath = isApp ? Path.Combine(srcPath, "pages", spaceFolderForPath) : Path.Combine(srcPath, "extension", spaceFolderForPath);
                string exFileName = sysTableInfo.TableName + ".js" + (isVite ? "x" : "");
                string currentTableNameForPath = sysTableInfo.TableName;

                if (!isApp)
                {
                    string specificFolderPath = sysTableInfo.FolderName?.ToLower();
                    if (!string.IsNullOrEmpty(specificFolderPath))
                    {
                         extensionPath = Path.Combine(srcPath, "extension", spaceFolderForPath, specificFolderPath);
                         spaceFolderForPath = Path.Combine(spaceFolderForPath, specificFolderPath);
                    }
                    pageContent = pageContent.Replace("#folder", spaceFolderForPath.Replace(Path.DirectorySeparatorChar, '/'));
                }

                if (!isApp && !FileHelper.FileExists(Path.Combine(extensionPath, exFileName)))
                {
                    string exContent = FileHelper.ReadFile(Path.Combine("Template", "Page", "VueExtension.html"));
                    exContent = exContent.Replace("#TableName", sysTableInfo.TableName);
                    FileHelper.WriteFile(extensionPath, exFileName, exContent);
                }
                pageContent = pageContent.Replace("#TableName", currentTableNameForPath);

                if (isApp)
                {
                    pageContent = vueOptions;
                    pageContent = pageContent.Replace("#TableName", currentTableNameForPath);
                    pageContent = pageContent.Replace("#titleField", sysTableInfo.ExpressField).Replace("{#table}", currentTableNameForPath);
                    string appTablePath = Path.Combine(currentVuePath, spaceFolderForPath, currentTableNameForPath);
                    FileHelper.WriteFile(appTablePath, currentTableNameForPath + "Options.js", pageContent);
                    string appEditPath = $"pages/{spaceFolderForPath.Replace(Path.DirectorySeparatorChar, '/')}/{currentTableNameForPath}/{currentTableNameForPath}Edit";
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
                    string pagesJsonPath = Path.Combine(srcPath, "pages.json");
                    string pagesJsonContent = FileHelper.ReadFile(pagesJsonPath);
                    string appPathForPagesJson = $"pages/{spaceFolderForPath.Replace(Path.DirectorySeparatorChar, '/')}/{currentTableNameForPath}/{currentTableNameForPath}";
                    StringBuilder newEntries = new StringBuilder();
                    Action<string, string> addPageJsonEntry = (pPath, title) => {
                        if (!pagesJsonContent.Contains($"\"{pPath}\"")) {
                            newEntries.AppendLine("		,{");
                            newEntries.AppendLine($"			\"path\": \"{pPath}\",");
                            newEntries.AppendLine( "			\"style\": {");
                            newEntries.AppendLine($"				\"navigationBarTitleText\": \"{title}\"");
                            newEntries.AppendLine( "			}");
                            newEntries.AppendLine("		}");
                        }
                    };
                    addPageJsonEntry(appPathForPagesJson, sysTableInfo.ColumnCNName);
                    addPageJsonEntry(appEditPath, sysTableInfo.ColumnCNName);
                    if (newEntries.Length > 0)
                    {
                        int closingBracketIndex = pagesJsonContent.LastIndexOf("]");
                        if (closingBracketIndex != -1)
                        {
                            string insertPoint = pagesJsonContent.Substring(0, closingBracketIndex).TrimEnd();
                            if (insertPoint.EndsWith("}")) newEntries.Insert(0, ",\r\n");
                            pagesJsonContent = pagesJsonContent.Insert(closingBracketIndex, newEntries.ToString());
                            FileHelper.WriteFile(srcPath, "pages.json", pagesJsonContent);
                        }
                    }
                }
                else // Not App
                {
                    if (isVite && !pageContent.Contains(currentTableNameForPath + ".jsx")) pageContent = pageContent.Replace(currentTableNameForPath + ".js", currentTableNameForPath + ".jsx");
                    pageContent = pageContent.Replace(".jsxx", ".jsx");
                    string viewFilePath = Path.Combine(currentVuePath, spaceFolderForPath, currentTableNameForPath + ".vue");
                    if (!FileHelper.FileExists(viewFilePath) || (FileHelper.ReadFile(viewFilePath)?.Contains("setup()") == true))
                    {
                         pageContent = pageContent.Replace("#folder", spaceFolderForPath.Replace(Path.DirectorySeparatorChar, '/'));
                         FileHelper.WriteFile(Path.Combine(currentVuePath, spaceFolderForPath), currentTableNameForPath + ".vue", pageContent);
                    }
                    if (hasSubDetail) vueOptions = vueOptions.Replace("#tables2", $"[{string.Join(",\r                  ", detailItems)}]");
                    vueOptions = vueOptions.Replace("[[]]", "[]");
                    FileHelper.WriteFile(Path.Combine(currentVuePath, spaceFolderForPath, currentTableNameForPath), "options.js", vueOptions);
                    string routerPath = Path.Combine(srcPath, "router", "viewGird.js");
                    string routerContent = FileHelper.ReadFile(routerPath);
                    if (!routerContent.Contains($"path: '/{currentTableNameForPath}'"))
                    {
                        string routerTemplate = FileHelper.ReadFile(Path.Combine("Template", "Page", "router.html"))
                         .Replace("#TableName", currentTableNameForPath).Replace("#folder", spaceFolderForPath.Replace(Path.DirectorySeparatorChar, '/'));
                        routerContent = routerContent.Replace("]", routerTemplate + "\n]");
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

        private int InitTable(int parentId, string tableName, string columnCNName, string nameSpace, string foldername, int tableId, bool isTreeLoad)
        {
            try
            {
                if (isTreeLoad) return tableId;
                if (string.IsNullOrEmpty(tableName)) return -1;

                var existingTable = repository.FindAsIQueryable(x => x.TableName == tableName)
                                         .Select(s => new { s.Table_Id }).FirstOrDefault();
                if (existingTable != null && existingTable.Table_Id > 0) return existingTable.Table_Id;

                Sys_TableInfo tableInfo = new Sys_TableInfo()
                {
                    ParentId = parentId, ColumnCNName = columnCNName, CnName = columnCNName, TableName = tableName,
                    Namespace = nameSpace, FolderName = foldername, Enable = 1
                };
                List<Sys_TableColumn> columns = repository.DapperContext.QueryList<Sys_TableColumn>(GetCurrentSql(tableName), new { tableName });

                if (columns == null || !columns.Any())
                {
                    VOL.Core.Services.Logger.Warning(VOL.Core.Enums.LogLevel.Warning, VOL.Core.Enums.LogEvent.Exception, $"InitTable: 表结构未找到或为空: TableName={tableName}", new { TableName = tableName });
                    throw new Exception($"表结构未找到或为空: {tableName}");
                }

                int orderNo = (columns.Count + 10) * 50;
                for (int i = 0; i < columns.Count; i++) { columns[i].OrderNo = orderNo; orderNo -= 50; columns[i].EditRowNo = 0; }
                SetMaxLength(columns);

                var result = base.Add<Sys_TableColumn>(tableInfo, columns, false);
                if (!result.Status)
                {
                    VOL.Core.Services.Logger.Error(VOL.Core.Enums.LogLevel.Error, VOL.Core.Enums.LogEvent.Add, $"InitTable: base.Add<Sys_TableColumn>失败: TableName={tableName}", new { TableName = tableName, ErrorMessage = result.Message });
                    throw new Exception($"加载表结构写入异常：{result.Message}");
                }
                return tableInfo.Table_Id;
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LogLevel.Error, VOL.Core.Enums.LogEvent.Exception, $"InitTable 方法异常: TableName={tableName}", new { TableName = tableName }, ex);
                throw new Exception($"初始化表信息时发生内部错误: {ex.Message}", ex);
            }
        }

        public object LoadTable(int parentId, string tableName, string columnCNName, string nameSpace, string foldername, int tableId, bool isTreeLoad)
        {
            try
            {
                if (!UserContext.Current.IsSuperAdmin && !isTreeLoad)
                {
                    return new WebResponseContent().Error("只有超级管理员才能进行此操作");
                }
                tableId = InitTable(parentId, tableName?.Trim(), columnCNName, nameSpace, foldername, tableId, isTreeLoad);
                if (tableId == -1 && !isTreeLoad)
                {
                     return new WebResponseContent().Error($"表名 {(tableName?.Trim())} 为空或无效。");
                }

                Sys_TableInfo tableInfo = repository.FindAsIQueryable(x => x.Table_Id == tableId)
                    .Include(c => c.TableColumns).FirstOrDefault();

                if (tableInfo == null)
                {
                    VOL.Core.Services.Logger.Warning(VOL.Core.Enums.LogLevel.Warning, VOL.Core.Enums.LogEvent.Select, $"LoadTable: 未找到TableInfo: TableId={tableId}", new {TableId = tableId});
                    return new WebResponseContent().Error($"未找到ID为 {tableId} 的表配置信息。");
                }

                if (tableInfo.TableColumns != null)
                {
                    tableInfo.TableColumns = tableInfo.TableColumns.OrderByDescending(x => x.OrderNo).ToList();
                }
                return new WebResponseContent().OK(null, tableInfo);
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LogLevel.Error, VOL.Core.Enums.LogEvent.Exception, $"LoadTable 方法异常: TableName={tableName}, TableId={tableId}", new { ParentId = parentId, TableName = tableName, TableId = tableId }, ex);
                return new WebResponseContent().Error($"加载表数据时发生内部错误。");
            }
        }

        public async Task<WebResponseContent> DelTree(int table_Id)
        {
            try
            {
                if (table_Id == 0) return new WebResponseContent().Error("没有传入参数");

                Sys_TableInfo tableInfo = await repository.FindAsIQueryable(x => x.Table_Id == table_Id)
                    .Include(c => c.TableColumns).FirstOrDefaultAsync();

                if (tableInfo == null) return new WebResponseContent().OK("要删除的节点不存在。");

                if (tableInfo.TableColumns != null && tableInfo.TableColumns.Count > 0)
                {
                    return new WebResponseContent().Error("当前删除的节点存在表结构信息,只能删除空节点");
                }
                if (await repository.ExistsAsync(x => x.ParentId == table_Id))
                {
                    return new WebResponseContent().Error("当前删除的节点存在子节点，不能删除");
                }
                repository.Delete(tableInfo, true);
                await repository.SaveChangesAsync();
                return new WebResponseContent().OK("节点删除成功。");
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LogLevel.Error, VOL.Core.Enums.LogEvent.Delete, $"DelTree 方法异常: Table_Id={table_Id}", new { Table_Id = table_Id }, ex);
                return new WebResponseContent().Error("删除树节点时发生内部错误。");
            }
        }

        private string CreateEntityModel(List<Sys_TableColumn> sysColumn, Sys_TableInfo tableInfo, List<TableColumnInfo> tableColumnInfoList, int createType)
        {
            try
            {
                string template = "";
                if (createType == 1) template = "DomainModel.html";
                else if (createType == 2) template = "ApiInputDomainModel.html";
                else template = "ApiOutputDomainModel.html";

                string domainContent = FileHelper.ReadFile(Path.Combine("Template","DomianModel",template));
                string partialContent = domainContent;
                StringBuilder AttributeBuilder = new StringBuilder();
                sysColumn = sysColumn.OrderByDescending(c => c.OrderNo).ToList();
                bool addIgnore = false;
                foreach (Sys_TableColumn column in sysColumn)
                {
                    column.ColumnType = (column.ColumnType ?? "").Trim(); AttributeBuilder.Append("/// <summary>"); AttributeBuilder.Append("\r\n"); AttributeBuilder.Append("       ///" + column.ColumnCnName + ""); AttributeBuilder.Append("\r\n"); AttributeBuilder.Append("       /// </summary>"); AttributeBuilder.Append("\r\n"); if (column.IsKey == 1) { AttributeBuilder.Append(@"       [Key]" + ""); AttributeBuilder.Append("\r\n"); } AttributeBuilder.Append("       [Display(Name =\"" + ( string.IsNullOrEmpty(column.ColumnCnName) ? column.ColumnName : column.ColumnCnName ) + "\")]"); AttributeBuilder.Append("\r\n");
                    TableColumnInfo tableColumnInfo = tableColumnInfoList.Where(x => x.ColumnName.ToLower().Trim() == column.ColumnName.ToLower().Trim()).FirstOrDefault(); if (tableColumnInfo != null && (tableColumnInfo.ColumnType == "varchar" && column.Maxlength > 8000) || (tableColumnInfo.ColumnType == "nvarchar" && column.Maxlength > 4000)) { column.Maxlength = 0; }
                    if (column.ColumnType == "string" && column.Maxlength > 0 && column.Maxlength < 8000) { AttributeBuilder.Append("       [MaxLength(" + column.Maxlength + ")]"); AttributeBuilder.Append("\r\n"); }
                    if (column.IsColumnData == 0 && createType == 1) { addIgnore = true; AttributeBuilder.Append("       [JsonIgnore]"); AttributeBuilder.Append("\r\n"); }
                    if (tableColumnInfo != null) { if (!string.IsNullOrEmpty(tableColumnInfo.Prec_Scale) && !tableColumnInfo.Prec_Scale.EndsWith(",0")) { AttributeBuilder.Append("       [DisplayFormat(DataFormatString=\"" + tableColumnInfo.Prec_Scale + "\")]"); AttributeBuilder.Append("\r\n"); } if ( (DBType.Name.ToLower() == DbCurrentType.Oracle.ToString().ToLower() && (column.Maxlength == 36)) || ((column.IsKey == 1 && (column.ColumnType == "uniqueidentifier")) || tableColumnInfo.ColumnType.ToLower() == "guid" || ((IsMysql() || IsDM()) && column.ColumnType == "string" && column.Maxlength == 36))) { tableColumnInfo.ColumnType = "uniqueidentifier"; }
                        string maxLength = string.Empty; if (tableColumnInfo.ColumnType != "uniqueidentifier") { if (column.IsKey != 1 && column.ColumnType.ToLower() == "string") { if (column.Maxlength <= 0 || (tableColumnInfo.ColumnType == "varchar" && column.Maxlength > 8000) || (tableColumnInfo.ColumnType == "nvarchar" && column.Maxlength > 4000)) { maxLength = "(max)"; } else { maxLength = "(" + column.Maxlength + ")"; } } else if (column.IsKey == 1 && column.ColumnType.ToLower() == "string" && column.Maxlength != 36) { maxLength = "(" + column.Maxlength + ")"; } } AttributeBuilder.Append("       [Column(TypeName=\"" + tableColumnInfo.ColumnType + maxLength + "\")]"); AttributeBuilder.Append("\r\n");
                        if (tableColumnInfo.ColumnType == "int" || tableColumnInfo.ColumnType == "bigint" || tableColumnInfo.ColumnType == "long") { column.ColumnType = tableColumnInfo.ColumnType == "int" ? "int" : "long"; } if (tableColumnInfo.ColumnType == "bool") { column.ColumnType = "bit"; } }
                    if (column.EditRowNo != null) { AttributeBuilder.Append("       [Editable(true)]"); AttributeBuilder.Append("\r\n"); }
                    if (column.IsNull == 0 || (createType == 2 && column.ApiIsNull == 0)) { AttributeBuilder.Append("       [Required(AllowEmptyStrings=false)]"); AttributeBuilder.Append("\r\n"); }
                    string columnType = (column.ColumnType == "Date" ? "DateTime" : column.ColumnType).Trim(); if (new string[] { "guid", "uniqueidentifier" }.Contains(tableColumnInfo?.ColumnType?.ToLower())) { columnType = "Guid"; } if (column.ColumnType.ToLower() != "string" && column.IsNull == 1) { columnType = columnType + "?"; }
                    if ((column.IsKey == 1 && (column.ColumnType == "uniqueidentifier")) || column.ColumnType == "guid" || ((IsMysql() || IsDM() || IsOracle()) && column.ColumnType == "string" && column.Maxlength == 36)) { columnType = "Guid" + (column.IsNull == 1 ? "?" : ""); }
                    AttributeBuilder.Append("       public " + columnType + " " + column.ColumnName + " { get; set; }"); AttributeBuilder.Append("\r\n\r\n       ");
                }
                if (!string.IsNullOrEmpty(tableInfo.DetailName) && createType == 1) { AttributeBuilder.Append("[Display(Name =\"" + tableInfo.DetailCnName + "\")]"); AttributeBuilder.Append("\r\n       "); AttributeBuilder.Append("[ForeignKey(\"" + sysColumn.FirstOrDefault(x => x.IsKey == 1)?.ColumnName + "\")]"); AttributeBuilder.Append("\r\n       "); AttributeBuilder.Append("public List<" + tableInfo.DetailName + "> " + tableInfo.DetailName + " { get; set; }"); AttributeBuilder.Append("\r\n"); }
                if (addIgnore && createType == 1) { domainContent = "using Newtonsoft.Json;\r\n" + domainContent + "\r\n"; }
                string mapPath = ProjectPath.GetProjectDirectoryInfo()?.FullName; if (string.IsNullOrEmpty(mapPath)) return "未找到生成的目录!";
                string[] splitArrr = tableInfo.Namespace.Split('.'); domainContent = domainContent.Replace("{TableName}", tableInfo.TableName).Replace("{AttributeList}", AttributeBuilder.ToString()).Replace("{StartName}", StratName);
                List<string> entityAttribute = new List<string> { $"TableCnName = \"{tableInfo.ColumnCNName}\"" };
                if (!string.IsNullOrEmpty(tableInfo.TableTrueName)) entityAttribute.Add($"TableName = \"{tableInfo.TableTrueName}\"");
                if (!string.IsNullOrEmpty(tableInfo.DetailName) && createType == 1) entityAttribute.Add($"DetailTable =  new Type[] {{ typeof({string.Join("),typeof(", tableInfo.DetailName.Split(','))})}}");
                if (!string.IsNullOrEmpty(tableInfo.DetailCnName)) entityAttribute.Add($"DetailTableCnName = \"{tableInfo.DetailCnName}\"");
                if (!string.IsNullOrEmpty(tableInfo.DBServer) && createType == 1) entityAttribute.Add($"DBServer = \"{tableInfo.DBServer}\"");
                string modelNameSpace = StratName + ".Entity"; string tableAttr = string.Join(",", entityAttribute); if (tableAttr != "") tableAttr = $"[Entity({tableAttr})]";
                if (!string.IsNullOrEmpty(tableInfo.TableTrueName) && tableInfo.TableName != tableInfo.TableTrueName)
                { string tableTrueName = tableInfo.TableTrueName; if (DBType.Name == DbCurrentType.PgSql.ToString()) tableTrueName = tableTrueName.ToLower(); tableAttr = $"{tableAttr}\r\n[Table(\"{tableTrueName}\")]"; }
                domainContent = domainContent.Replace("{AttributeManager}", tableAttr).Replace("{Namespace}", modelNameSpace);
                string folderName = tableInfo.FolderName; string currentTableName = tableInfo.TableName;
                if (createType == 2) { folderName = Path.Combine("ApiEntity", "Input"); currentTableName = "Api" + tableInfo.TableName + "Input"; }
                else if (createType == 3) { folderName = Path.Combine("ApiEntity", "OutPut"); currentTableName = "Api" + tableInfo.TableName + "Output"; }
                string modelPath = Path.Combine(mapPath, modelNameSpace, "DomainModels", folderName);
                FileHelper.WriteFile(modelPath, currentTableName + ".cs", domainContent);
                string partialModelPath = Path.Combine(modelPath, "partial");
                if (!FileHelper.FileExists(Path.Combine(partialModelPath, currentTableName + ".cs")))
                { partialContent = partialContent.Replace("{AttributeManager}", "").Replace("{AttributeList}", @"//此处配置字段(字段配置见此model的另一个partial),如果表中没有此字段请加上 [NotMapped]属性，否则会异常").Replace(":BaseEntity", "").Replace("{TableName}", tableInfo.TableName).Replace("{Namespace}", modelNameSpace); FileHelper.WriteFile(partialModelPath, currentTableName + ".cs", partialContent); }
                if (createType == 1) { string mappingConfiguration = FileHelper.ReadFile(Path.Combine("Template","DomianModel","MappingConfiguration.html")).Replace("{TableName}", tableInfo.TableName).Replace("{Namespace}", modelNameSpace).Replace("{StartName}", StratName); }
                return "";
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LogLevel.Error, VOL.Core.Enums.LogEvent.Exception, $"内部CreateEntityModel失败: TableName={tableInfo?.TableName}, CreateType={createType}", tableInfo?.Serialize(), ex);
                return $"生成实体类(内部)时发生错误: {ex.Message}";
            }
        }
        private StringBuilder GetGridColumns(List<Sys_TableColumn> list, string expressField, bool detail, bool vue = false, bool app = false)
        {
            totalCol = 0; totalWidth = 0; StringBuilder sb = new StringBuilder();
            Func<Sys_TableColumn, bool> func = x => true; bool sort = false;
            if (app) { func = x => new int[] { 1, 2, 3, 4 }.Any(c => c == x.Enable) && (x.IsDisplay == null || x.IsDisplay == 1); }
            foreach (Sys_TableColumn item in list.Where(func).OrderByDescending(x => x.OrderNo)) {
                if (item.IsColumnData == 0) { continue; } sb.Append("{field:'" + item.ColumnName + "',"); sb.Append("title:'" + (string.IsNullOrEmpty(item.ColumnCnName) ? item.ColumnName : item.ColumnCnName) + "',");
                if (vue) { string colType = item.ColumnType.ToLower(); if (item.IsImage == 1) colType = "img"; else if (item.IsImage == 2) colType = "excel"; else if (item.IsImage == 3) colType = "file"; else if (item.IsImage == 4) colType = "date";
                    sb.Append("type:'" + colType + "',"); if (!string.IsNullOrEmpty(item.DropNo)) sb.Append("bind:{ key:'" + item.DropNo + "',data:[]},"); if (expressField != null && expressField.ToLower() == item.ColumnName.ToLower()) sb.Append("link:true,"); if (item.Sortable == 1 && !app) sb.Append("sort:true,"); }
                else { sb.Append("datatype:'" + item.ColumnType + "',"); }
                if (!app) sb.Append("width:" + (item.ColumnWidth ?? 90) + ",");
                if (item.IsDisplay == 0) sb.Append("hidden:true,"); else { totalCol++; totalWidth += item.ColumnWidth == null ? 0 : Convert.ToInt32(item.ColumnWidth); }
                if (item.IsReadDataset == 1) sb.Append("readonly:true,");
                if (item.EditRowNo != null && item.EditRowNo > 0 && detail) { string editText = vue ? "edit" : "editor"; if (vue) { sb.Append("edit:{type:'" + item.EditType + "'},"); } else { switch (item.EditType) { case "date": sb.Append("editor:'datebox',"); break; case "datetime": sb.Append("editor:'datetimebox',"); break; case "drop": case "dropList": case "select": case "selectList": if (!vue && !string.IsNullOrEmpty(item.DropNo)) { sb.Append(editText + ": { type: 'combobox', options: optionConfig" + item.DropNo + " },"); } else { sb.Append(editText + ": 'text',"); } break; default: sb.Append(editText + ":'text',"); break; } } }
                if (!vue) { if (expressField != null && expressField.ToLower() == item.ColumnName.ToLower()) { sb.Append("formatter:function (val, row, index) { return $.fn.layOut('createViewField',{row:row,val:val,index:index})},"); } else if (!string.IsNullOrEmpty(item.Script)) { sb.Append("formatter:" + item.Script + ","); } else if (item.IsImage == 1) { sb.Append("formatter:function (val, row, index) { return $.fn.layOut('createImageUrl',{row:row,key:'" + item.ColumnName + "'})},"); } else if (!string.IsNullOrEmpty(item.DropNo) && !detail) { sb.AppendLine("formatter: function (val, row, index) {"); sb.AppendLine(string.Format("    return dataSource{0}.textFormatter(optionConfig{0}, val, row, index);", item.DropNo)); sb.AppendLine("    },"); } }
                if (item.IsNull == 0 && !app) sb.Append("require:true,");
                if (!app && (item.ColumnType.ToLower() == "datetime" || (item.IsDisplay == 1 & !sort))) { sb.Append("align:'left'},"); } else { if (!app) sb.Append("align:'left'},"); }
                if (app) { sb.Append("},").Replace(",},", "},"); }
                sb.AppendLine(); sb.Append("                       ");
            } return sb;
        }
        private static string[] formType = new string[] { "bigint", "int", "decimal", "float", "byte" };
        private string GetDisplayType(bool search, string searchType, string editType, string columnType) { string type = ""; if (search) { type = searchType == "无" ? "" : searchType ?? ""; } else { type = editType == "无" ? "" : editType ?? ""; } if (type == "" && formType.Contains(columnType)) { if (columnType == "decimal" || columnType == "float") { type = "decimal"; } else { type = "number"; } } return type; }
        private string GetDropString(string dropNo, bool vue) { if (string.IsNullOrEmpty(dropNo)) return vue ? "''" : "__[]__"; if (vue) return dropNo; return "__" + "optionConfig" + dropNo + "__"; }
        private void GetPanelData(List<Sys_TableColumn> list, List<List<PanelHtml>> panelHtml, Func<Sys_TableColumn, int?> keySelector, Func<Sys_TableColumn, bool> predicate, bool search, Func<Sys_TableColumn, int?> orderBy, bool vue = false, bool app = false) { if (app) { list.ForEach(x => { if (x.EditRowNo == 0) x.EditRowNo = 99999; }); var arr = search ? new int[] { 1, 3, 5, 6 } : new int[] { 1, 2, 5, 7 }; predicate = x => arr.Any(c => c == x.Enable); } var whereReslut = list.Where(predicate).OrderBy(orderBy).ThenByDescending(c => c.OrderNo).ToList(); foreach (var item in whereReslut.GroupBy(keySelector)) { panelHtml.Add(item.OrderBy(c => search ? c.SearchColNo : c.EditColNo).Select( x => new PanelHtml { text = x.ColumnCnName ?? x.ColumnName, id = x.ColumnName, displayType = GetDisplayType(search, x.SearchType, x.EditType, x.ColumnType), require = !search && x.IsNull == 0 ? true : false, columnType = vue && x.IsImage == 1 ? "img" : (x.ColumnType ?? "string").ToLower(), disabled = !search && x.IsReadDataset == 1 ? true : false, dataSource = GetDropString(x.DropNo, vue), colSize = search && x.SearchType != "checkbox" ? 0 : (x.ColSize ?? 0) }).ToList()); } }
        private static bool IsOracle() { return DBType.Name.ToLower() == DbCurrentType.Oracle.ToString().ToLower(); }
        private static bool IsMysql() { return DBType.Name.ToLower() == DbCurrentType.MySql.ToString().ToLower(); }
        private static bool IsDM() { return DBType.Name.ToLower() == DbCurrentType.DM.ToString().ToLower(); }

        private WebResponseContent ValidColumnString(Sys_TableInfo tableInfo)
        {
            WebResponseContent webResponse = new WebResponseContent(true);
            try
            {
                if (tableInfo.TableColumns == null || tableInfo.TableColumns.Count == 0) return webResponse;

                if (!string.IsNullOrEmpty(tableInfo.DetailName))
                {
                    Sys_TableColumn mainTableColumn = tableInfo.TableColumns.FirstOrDefault(x => x.IsKey == 1);
                    if (mainTableColumn == null)
                        return webResponse.Error($"请勾选表[{tableInfo.TableName}]的主键");

                    string key = mainTableColumn.ColumnName;

                    // DB Call
                    Sys_TableColumn tableColumn = repository.Find<Sys_TableColumn>(x => x.TableName == tableInfo.DetailName && x.ColumnName == key)
                                                    .FirstOrDefault();

                    if (tableColumn == null)
                        return webResponse.Error($"明细表必须包括[{tableInfo.TableName}]主键字段[{key}]");

                    if (mainTableColumn.ColumnType?.ToLower() != tableColumn.ColumnType?.ToLower())
                    {
                        return webResponse.Error($"明细表的字段[{tableColumn.ColumnName}]类型必须与主表的主键的类型相同");
                    }
                    if ((IsMysql() || IsDM()) && mainTableColumn.ColumnType?.ToLower() == "string" && mainTableColumn.Maxlength == 36)
                    {
                        if (tableColumn.Maxlength != 36)
                        {
                            return webResponse.Error($"主表主键类型为Guid字符串，明细表[{tableInfo.DetailName}]配置的字段[{key}]长度必须是36，请将其长度设置为36。");
                        }
                    }
                }
                return webResponse;
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LogLevel.Error, VOL.Core.Enums.LogEvent.Exception, $"ValidColumnString方法异常: TableName={tableInfo?.TableName}", tableInfo?.Serialize(), ex);
                return webResponse.Error("验证表列信息时发生内部错误。");
            }
        }
    }
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
        public int fileMaxCount { get; set; }
    }
}
