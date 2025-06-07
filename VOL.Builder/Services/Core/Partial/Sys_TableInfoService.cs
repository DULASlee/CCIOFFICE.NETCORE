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
        private static class ColumnEnableStates
        {
            public const int DisplayQueryEdit = 1;
            public const int DisplayEdit = 2;
            public const int DisplayQuery = 3;
            public const int DisplayOnly = 4;
            public const int QueryEdit = 5;
            public const int QueryOnly = 6;
            public const int EditOnly = 7;
        }
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
                    throw new Exception("未获取到以.WebApi,无法创建Api控制器");
                }
                return apiNameSpace;
            }
        }

        public async Task<(string, string)> GetTableTree()
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

            var parentIdSet = new HashSet<int?>(treeData.Where(x => x.pId != null).Select(x => x.pId));
            var treeList = treeData.Select(a => new
            {
                a.id,
                a.pId,
                a.parentId,
                a.name,
                isParent = parentIdSet.Contains(a.id)
            });
            string startsWith = WebProject.Substring(0, WebProject.IndexOf('.'));
            return (treeList.Count() == 0 ? "[]" : treeList.Serialize() ?? "", ProjectPath.GetProjectFileName(startsWith));
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
                Console.WriteLine($"获取mysql数据库名异常:{ex.Message}");
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
                Console.WriteLine($"获取达梦数据库名异常:{ex.Message}");
                return "";
            }
        }

        private string GetMySqlModelInfo()
        {
            return $@"SELECT
DISTINCT
           CONCAT(NUMERIC_PRECISION,',',NUMERIC_SCALE) as Prec_Scale,
        CASE
                 WHEN data_type IN( 'BIT', 'BOOL','bit', 'bool') THEN 'bool'
                 WHEN data_type in('smallint','SMALLINT') THEN 'short'
				WHEN data_type in('tinyint', 'TINYINT') THEN 'sbyte'
                WHEN data_type IN('MEDIUMINT','mediumint', 'int','INT','year', 'Year') THEN 'int'
                WHEN data_type in ( 'BIGINT','bigint') THEN 'bigint'
                WHEN data_type IN('FLOAT',  'DECIMAL','float', 'decimal') THEN 'decimal'
				WHEN data_type IN( 'DOUBLE', 'double') THEN 'double'
                WHEN data_type IN('CHAR', 'VARCHAR', 'TINY TEXT', 'TEXT', 'MEDIUMTEXT', 'LONGTEXT', 'TINYBLOB', 'BLOB', 'MEDIUMBLOB', 'LONGBLOB', 'Time','char', 'varchar', 'tiny text', 'text', 'mediumtext', 'longtext', 'tinyblob', 'blob', 'mediumblob', 'longblob', 'time') THEN 'nvarchar'
                WHEN data_type IN('Date', 'DateTime', 'TimeStamp','date', 'datetime', 'timestamp') THEN 'datetime' ELSE 'nvarchar'
            END AS ColumnType, Column_Name AS ColumnName
            FROM
                information_schema.COLUMNS
            WHERE
                table_name = ?tableName {GetMysqlTableSchema()};";
        }

        private string GetDMModelInfo()
        {
            return $@"SELECT DISTINCT
                        IF(DATA_PRECISION IS NOT NULL, CONCAT(DATA_PRECISION,',',DATA_SCALE),'') as Prec_Scale,
                        CASE
                            WHEN data_type IN( 'BIT', 'BOOL','bit', 'bool') THEN 'bool'
                            WHEN data_type in('smallint','SMALLINT') THEN 'short'
                            WHEN data_type in('tinyint', 'TINYINT') THEN 'sbyte'
                            WHEN data_type IN('MEDIUMINT','mediumint', 'int','INT','year', 'Year') THEN 'int'
                            WHEN data_type in ( 'BIGINT','bigint') THEN 'bigint'
                            WHEN data_type IN('FLOAT',  'DECIMAL','float', 'decimal') THEN 'decimal'
							WHEN data_type IN( 'DOUBLE', 'double') THEN 'double'
                            WHEN data_type IN('CHAR', 'VARCHAR', 'TINY TEXT', 'TEXT', 'MEDIUMTEXT', 'LONGTEXT', 'TINYBLOB', 'BLOB', 'MEDIUMBLOB', 'LONGBLOB', 'Time','char', 'varchar', 'tiny text', 'text', 'mediumtext', 'longtext', 'tinyblob', 'blob', 'mediumblob', 'longblob', 'time') THEN 'nvarchar'
                            WHEN data_type IN('Date', 'DateTime', 'TimeStamp','date', 'datetime', 'timestamp') THEN 'datetime' ELSE 'nvarchar'
                        END AS ColumnType,
                        Column_Name AS ColumnName
                        FROM user_tab_columns 
                        WHERE table_name = :tableName ";
        }

        private string GetSqlServerModelInfo()
        {
            return $@"
	SELECT CASE WHEN t.ColumnType IN ('DECIMAL','smallmoney','money') THEN 
                    CONVERT(VARCHAR(30),t.Prec)+','+CONVERT(VARCHAR(30),t.Scale)  ELSE ''
                     END 
                    AS Prec_Scale,t.ColumnType,t.ColumnName
                      FROM (
                    SELECT col.prec AS  'Prec',col.scale AS 'Scale',t.name AS ColumnType,col.name AS ColumnName FROM          dbo.syscolumns col
                                                LEFT  JOIN dbo.systypes t ON col.xtype = t.xusertype
                                                INNER JOIN dbo.sysobjects obj ON col.id = obj.id
                                                                                 AND obj.xtype IN ('U','V')
                                                                                 AND obj.status >= 0
                                                LEFT  JOIN dbo.syscomments comm ON col.cdefault = comm.id
                                                LEFT  JOIN sys.extended_properties ep ON col.id = ep.major_id
                                                                                  AND col.colid = ep.minor_id
                                                                                  AND ep.name = 'MS_Description'
                                                LEFT  JOIN sys.extended_properties epTwo ON obj.id = epTwo.major_id
                                                                                  AND epTwo.minor_id = 0
                                                                                  AND epTwo.name = 'MS_Description'
                                      WHERE     obj.name =@tableName) AS t";
        }

        private string GetOracleModelInfo(string tableName)
        {
            return $@"SELECT
			c.TABLE_NAME TableName ,
			cc.COLUMN_NAME COLUMNNAME,
			cc.COMMENTS  as  ColumnCNName,
			CASE WHEN   c.DATA_TYPE IN('smallint', 'INT') or (c.DATA_TYPE='NUMBER' and c.DATA_LENGTH=0)   THEN 'int'  
            WHEN  c.DATA_TYPE IN('NUMBER') THEN 'decimal'  
			WHEN c.DATA_TYPE IN('CHAR', 'VARCHAR', 'NVARCHAR','VARCHAR2', 'NVARCHAR2','text', 'image')
			THEN 'nvarchar'
		  WHEN  c.DATA_TYPE IN('DATE') THEN 'date'  
			ELSE 'nvarchar' 
			end    as ColumnType,
			c.DATA_LENGTH  as Maxlength,
			case WHEN 	c.NULLABLE='Y' THEN 1 ELSE 0 end   as ISNULL
			FROM
			ALL_tab_columns c
			LEFT JOIN   ALL_col_comments cc ON c.table_name = cc.table_name 
			AND c.column_name = cc.column_name
			LEFT JOIN   ALL_tab_comments t ON c.table_name = t.table_name 
			WHERE 		   c.table_name='{tableName.ToUpper()}'";
        }

        private string GetPgSqlModelInfo()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("			SELECT ");
            stringBuilder.Append("				col.COLUMN_NAME AS \"ColumnName\", ");
            stringBuilder.Append("			CASE ");
            stringBuilder.Append("					WHEN col.udt_name = 'uuid' THEN 'guid'  ");
            stringBuilder.Append("					WHEN col.udt_name IN ( 'int2') THEN 'short'  ");
            stringBuilder.Append("					WHEN col.udt_name IN ( 'int4' ) THEN 'int'  ");
            stringBuilder.Append("					WHEN col.udt_name = 'int8' THEN 'long'  ");
            stringBuilder.Append("					WHEN col.udt_name IN ( 'char', 'varchar', 'text', 'xml', 'bytea' ) THEN 'string'  ");
            stringBuilder.Append("					WHEN col.udt_name IN ( 'bool' ) THEN 'bool'  ");
            stringBuilder.Append("					WHEN col.udt_name IN ( 'date','timestamp' ) THEN 'DateTime'  ");
            stringBuilder.Append("					WHEN col.udt_name IN ( 'decimal', 'money','numeric' ) THEN 'decimal'  ");
            stringBuilder.Append("					WHEN col.udt_name IN ( 'float4', 'float8' ) THEN 'float' ELSE'string '  ");
            stringBuilder.Append("				END  as ColumnType ");
            stringBuilder.Append("from 	information_schema.COLUMNS col  ");
            stringBuilder.Append("WHERE	\"lower\" ( TABLE_NAME ) = \"lower\" (@tableName )  ");
            return stringBuilder.ToString();
        }

        private WebResponseContent ExistsTable(string tableName, string tableTrueName)
        {
            WebResponseContent webResponse = new WebResponseContent(true);
            var compilationLibrary = DependencyContext
                .Default
                .CompileLibraries
                .Where(x => !x.Serviceable && x.Type == "project");
            foreach (var _compilation in compilationLibrary)
            {
                try
                {
                    foreach (var entity in AssemblyLoadContext.Default
                .LoadFromAssemblyName(new AssemblyName(_compilation.Name))
                .GetTypes().Where(x => x.GetTypeInfo().BaseType != null
                    && x.BaseType == typeof(BaseEntity)))
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
                    Console.WriteLine("查找文件异常：" + ex.Message);
                }
            }
            return webResponse;
        }

        public string CreateEntityModel(Sys_TableInfo sysTableInfo)
        {
            if (sysTableInfo == null
                || sysTableInfo.TableColumns == null
                || sysTableInfo.TableColumns.Count == 0)
                return "提交的配置数据不完整";

            WebResponseContent webResponse = ValidColumnString(sysTableInfo);
            if (!webResponse.Status)
                return webResponse.Message;

            string tableName = sysTableInfo.TableName;
            webResponse = ExistsTable(tableName, sysTableInfo.TableTrueName);
            if (!webResponse.Status)
            {
                return webResponse.Message;
            }
            if (!string.IsNullOrEmpty(sysTableInfo.TableTrueName) && sysTableInfo.TableTrueName != tableName)
            {
                tableName = sysTableInfo.TableTrueName;
            }

            string sql = "";
            switch (DBType.Name)
            {
                case "MySql":
                    sql = GetMySqlModelInfo();
                    break;
                case "PgSql":
                    sql = GetPgSqlModelInfo();
                    break;
                case "Oracle":
                    sql = GetOracleModelInfo(tableName);
                    break;
                case "DM":
                    sql = GetDMModelInfo();
                    break;
                default:
                    sql = GetSqlServerModelInfo();
                    break;
            }
            List<TableColumnInfo> tableColumnInfoList = repository.DapperContext.QueryList<TableColumnInfo>(sql, new { tableName });
            List<Sys_TableColumn> list = sysTableInfo.TableColumns;
            string msg = CreateEntityModel(list, sysTableInfo, tableColumnInfoList, 1);
            if (msg != "")
                return msg;
            return "Model创建成功!";
        }

        public WebResponseContent SaveEidt(Sys_TableInfo sysTableInfo)
        {
            WebResponseContent webResponse = ValidColumnString(sysTableInfo);
            if (!webResponse.Status) return webResponse;
            if (sysTableInfo.Table_Id == sysTableInfo.ParentId)
            {
                return WebResponseContent.Instance.Error($"父级id不能为自己");
            }
            if (sysTableInfo.TableColumns != null && sysTableInfo.TableColumns.Any(x => !string.IsNullOrEmpty(x.DropNo) && x.ColumnName == sysTableInfo.ExpressField))
            {
                return WebResponseContent.Instance.Error($"不能将字段【{sysTableInfo.ExpressField}】设置为快捷编辑,因为已经设置了数据源");
            }
            if (sysTableInfo.TableColumns != null)
            {
                sysTableInfo.TableColumns.ForEach(x =>
                {
                    x.TableName = sysTableInfo.TableName;
                });
            }

            sysTableInfo.TableColumns?.ForEach(x =>
            {
                if (x.IsReadDataset == null)
                {
                    x.IsReadDataset = 0;
                }
            });
            return repository.UpdateRange<Sys_TableColumn>(sysTableInfo, true, true, null, null, true);
        }

        private string GetCurrentSql(string tableName)
        {
            string sql;
            if (DBType.Name.ToLower() == DbCurrentType.MySql.ToString().ToLower())
            {
                sql = GetMySqlStructure(tableName);
            }
            else if (DBType.Name.ToLower() == DbCurrentType.PgSql.ToString().ToLower())
            {
                sql = GetPgSqlStructure(tableName);
            }
            else if (DBType.Name.ToLower() == DbCurrentType.DM.ToString().ToLower())
            {
                sql = GetDMStructure(tableName);
            }
            else if (DBType.Name.ToLower() == DbCurrentType.Oracle.ToString().ToLower())
            {
                sql = GetOracleStructure(tableName);
            }
            else
            {
                sql = GetSqlServerStructure(tableName);
            }
            return sql;
        }

        public async Task<WebResponseContent> SyncTable(string tableName)
        {
            WebResponseContent webResponse = new WebResponseContent();
            if (string.IsNullOrEmpty(tableName)) return webResponse.OK("表名不能为空");

            Sys_TableInfo tableInfo = repository.FindAsIQueryable(x => x.TableName == tableName)
          .Include(o => o.TableColumns).ToList().FirstOrDefault();
            if (tableInfo == null)
                return webResponse.Error("未获取到【" + tableName + "】的配置信息，请使用新建功能");
            if (!string.IsNullOrEmpty(tableInfo.TableTrueName) && tableInfo.TableTrueName != tableName)
            {
                tableName = tableInfo.TableTrueName;
            }

            string sql = GetCurrentSql(tableName);

            List<Sys_TableColumn> columns = repository.DapperContext
                  .QueryList<Sys_TableColumn>(sql, new { tableName });
            if (columns == null || columns.Count == 0)
                return webResponse.Error("未获取到【" + tableName + "】表结构信息，请确认表是否存在");

            List<Sys_TableColumn> detailList = tableInfo.TableColumns ?? new List<Sys_TableColumn>();
            List<Sys_TableColumn> addColumns = new List<Sys_TableColumn>();
            List<Sys_TableColumn> updateColumns = new List<Sys_TableColumn>();
            foreach (Sys_TableColumn item in columns)
            {
                Sys_TableColumn tableColumn = detailList.Where(x => x.ColumnName == item.ColumnName)
                    .FirstOrDefault();
                if (tableColumn == null)
                {
                    item.TableName = tableInfo.TableName;
                    item.Table_Id = tableInfo.Table_Id;
                    addColumns.Add(item);
                    continue;
                }
                if (item.ColumnType != tableColumn.ColumnType || item.Maxlength != tableColumn.Maxlength || (item.IsNull ?? 0) != (tableColumn.IsNull ?? 0))
                {
                    tableColumn.ColumnType = item.ColumnType;
                    tableColumn.Maxlength = item.Maxlength;
                    tableColumn.IsNull = item.IsNull;
                    updateColumns.Add(tableColumn);
                }
            }
            List<Sys_TableColumn> delColumns = detailList.Where(a => !columns.Select(c => c.ColumnName).Contains(a.ColumnName)).ToList();
            if (addColumns.Count + delColumns.Count + updateColumns.Count == 0)
            {
                return webResponse.Error("【" + tableName + "】表结构未发生变化");
            }
            repository.AddRange(addColumns);
            repository.DbContext.Set<Sys_TableColumn>().RemoveRange(delColumns);
            repository.UpdateRange(updateColumns, x => new { x.ColumnType, x.Maxlength, x.IsNull });
            await repository.DbContext.SaveChangesAsync();

            return webResponse.OK($"新加字段【{addColumns.Count}】个,删除字段【{delColumns.Count}】,修改字段【{updateColumns.Count}】");
        }

        public string CreateServices(string tableName, string nameSpace, string foldername, bool webController, bool apiController)
        {
            var tableColumn = repository.FindAsyncFirst<Sys_TableColumn>(x => x.TableName == tableName).Result;

            if (tableColumn == null)
            {
                return $"没有查到{tableName}表信息";
            }

            if (string.IsNullOrEmpty(nameSpace) || string.IsNullOrEmpty(foldername))
            {
                return $"命名空间、项目文件夹都不能为空";
            }

            string domainContent = "";

            string frameworkFolder = ProjectPath.GetProjectDirectoryInfo()?.FullName;
            string[] splitArr = nameSpace.Split('.');
            string projectName = splitArr.Length > 1 ? splitArr[splitArr.Length - 1] : splitArr[0];
            string baseOptions = "\"" + projectName + "\"," + "\"" + foldername + "\"," + "\"" + tableName + "\"";

            if (apiController)
            {
                string apiPath = ProjectPath.GetProjectDirectoryInfo().GetDirectories().Where(x => x.Name.ToLower().EndsWith(".webapi")).FirstOrDefault()?.FullName;
                if (string.IsNullOrEmpty(apiPath))
                {
                    return "未找到webapi类库,请确认是存在weiapi类库命名以.webapi结尾";
                }
                apiPath += $"\\Controllers\\{projectName}\\";
                if (!FileHelper.FileExists($"{apiPath}Partial\\{tableName}Controller.cs"))
                {
                    string partialController = FileHelper.ReadFile(@"Template\\Controller\\ControllerApiPartial.html")
                       .Replace("{Namespace}", nameSpace).Replace("{TableName}", tableName).Replace("{StartName}", StratName);
                    FileHelper.WriteFile($"{apiPath}Partial\\", tableName + "Controller.cs", partialController);
                }
                domainContent = FileHelper.ReadFile(@"Template\\Controller\\ControllerApi.html")
                    .Replace("{Namespace}", nameSpace).Replace("{TableName}", tableName).Replace("{StartName}", StratName).Replace("{BaseOptions}", baseOptions);
                FileHelper.WriteFile(apiPath, tableName + "Controller.cs", domainContent);
            }

            domainContent = FileHelper.ReadFile("Template\\Repositorys\\BaseRepository.html").Replace("{Namespace}", nameSpace).Replace("{TableName}", tableName).Replace("{StartName}", StratName);
            FileHelper.WriteFile(
           frameworkFolder + string.Format("\\{0}\\Repositories\\{1}\\", nameSpace, foldername)
                          , tableName + "Repository.cs", domainContent);
            domainContent = FileHelper.ReadFile("Template\\IRepositorys\\BaseIRepositorie.html").Replace("{Namespace}", nameSpace).Replace("{TableName}", tableName).Replace("{StartName}", StratName);
            FileHelper.WriteFile(
            frameworkFolder + string.Format("\\{0}\\IRepositories\\{1}\\", nameSpace, foldername),
                   "I" + tableName + "Repository.cs", domainContent);

            string path = $"{frameworkFolder}\\{nameSpace}\\IServices\\{foldername}\\";
            string fileName = "I" + tableName + "Service.cs";

            if (!FileHelper.FileExists(path + "Partial\\" + fileName))
            {
                domainContent = FileHelper.ReadFile("Template\\IServices\\IServiceBasePartial.html").Replace("{Namespace}", nameSpace).Replace("{TableName}", tableName).Replace("{StartName}", StratName);
                FileHelper.WriteFile(path + "Partial\\", fileName, domainContent);
            }

            domainContent = FileHelper.ReadFile("Template\\IServices\\IServiceBase.html").Replace("{Namespace}", nameSpace).Replace("{TableName}", tableName).Replace("{StartName}", StratName);
            FileHelper.WriteFile(path, fileName, domainContent);

            path = $"{frameworkFolder}\\{nameSpace}\\Services\\{foldername}\\";
            fileName = tableName + "Service.cs";
            domainContent = FileHelper.ReadFile("Template\\Services\\ServiceBasePartial.html").Replace("{Namespace}", nameSpace).Replace("{TableName}", tableName).Replace("{StartName}", StratName);
            if (!FileHelper.FileExists(path + "Partial\\" + fileName))
            {
                domainContent = FileHelper.ReadFile("Template\\Services\\ServiceBasePartial.html").Replace("{Namespace}", nameSpace).Replace("{TableName}", tableName).Replace("{StartName}", StratName);
                FileHelper.WriteFile(path + "Partial\\", fileName, domainContent);
            }

            domainContent = FileHelper.ReadFile("Template\\Services\\ServiceBase.html")
                .Replace("{Namespace}", nameSpace).Replace("{TableName}", tableName)
                .Replace("{StartName}", StratName);
            FileHelper.WriteFile(path, fileName, domainContent);

            if (webController)
            {
                path = $"{frameworkFolder}\\{nameSpace}\\Controllers\\{foldername}\\";
                fileName = tableName + "Controller.cs";
                if (!FileHelper.FileExists(path + "Partial\\" + fileName))
                {
                    domainContent = FileHelper.ReadFile("Template\\Controller\\ControllerPartial.html").Replace("{Namespace}", nameSpace).Replace("{TableName}", tableName).Replace("{BaseOptions}", baseOptions).Replace("{StartName}", StratName);
                    FileHelper.WriteFile(path + "Partial\\", tableName + "Controller.cs", domainContent);
                }
                domainContent = FileHelper.ReadFile("Template\\Controller\\Controller.html").Replace("{Namespace}", nameSpace).Replace("{TableName}", tableName).Replace("{BaseOptions}", baseOptions).Replace("{StartName}", StratName);
                FileHelper.WriteFile(path, tableName + "Controller.cs", domainContent);
            }
            return "业务类创建成功!";
        }

        /// <summary>
        /// Generates the C# code for service, repository, and controller layers.
        /// </summary>
        /// <param name="tableName">Table name.</param>
        /// <param name="nameSpace">Namespace for the generated files.</param>
        /// <param name="foldername">Folder name within the project structure.</param>
        /// <param name="webController">Whether to generate web controller.</param>
        /// <param name="apiController">Whether to generate API controller.</param>
        /// <param name="frameworkFolder">Base path of the framework project.</param>
        /// <param name="stratName">The start name, usually derived from the web project name.</param>
        /// <param name="baseOptions">Base options string for controllers.</param>
        /// <returns>A dictionary mapping a logical file key to its full intended path and generated code string.</returns>
        internal Dictionary<string, (string FullPath, string Content)> GenerateServiceLayerCode(
            string tableName, string nameSpace, string foldername, bool webController, bool apiController,
            string frameworkFolder, string stratName, string baseOptions)
        {
            var generatedFiles = new Dictionary<string, (string FullPath, string Content)>();
            string domainContent = "";

            // API Controller
            if (apiController)
            {
                string apiPathProjectLevel = ProjectPath.GetProjectDirectoryInfo().GetDirectories().FirstOrDefault(x => x.Name.ToLower().EndsWith(".webapi"))?.FullName;
                if (string.IsNullOrEmpty(apiPathProjectLevel))
                {
                    // This case should ideally be handled by the caller or return an error status
                    // For now, skipping API controller generation if path not found
                }
                else
                {
                    string projectName = nameSpace.Split('.').LastOrDefault() ?? nameSpace;
                    string apiControllersFolder = Path.Combine(apiPathProjectLevel, "Controllers", projectName);
                    string apiPartialControllerPath = Path.Combine(apiControllersFolder, "Partial", tableName + "Controller.cs");
                    string apiControllerPath = Path.Combine(apiControllersFolder, tableName + "Controller.cs");

                    if (!FileHelper.FileExists(apiPartialControllerPath)) // Assuming FileHelper.FileExists can be mocked or handled
                    {
                        string partialControllerContent = FileHelper.ReadFile(@"Template\\Controller\\ControllerApiPartial.html")
                           .Replace("{Namespace}", nameSpace).Replace("{TableName}", tableName).Replace("{StartName}", stratName);
                        generatedFiles["ApiPartialController"] = (apiPartialControllerPath, partialControllerContent);
                    }

                    domainContent = FileHelper.ReadFile(@"Template\\Controller\\ControllerApi.html")
                        .Replace("{Namespace}", nameSpace).Replace("{TableName}", tableName).Replace("{StartName}", stratName).Replace("{BaseOptions}", baseOptions);
                    generatedFiles["ApiController"] = (apiControllerPath, domainContent);
                }
            }

            // Repository
            string repositoryPath = Path.Combine(frameworkFolder, nameSpace, "Repositories", foldername, tableName + "Repository.cs");
            domainContent = FileHelper.ReadFile("Template\\Repositorys\\BaseRepository.html").Replace("{Namespace}", nameSpace).Replace("{TableName}", tableName).Replace("{StartName}", stratName);
            generatedFiles["Repository"] = (repositoryPath, domainContent);

            // IRepository
            string iRepositoryPath = Path.Combine(frameworkFolder, nameSpace, "IRepositories", foldername, "I" + tableName + "Repository.cs");
            domainContent = FileHelper.ReadFile("Template\\IRepositorys\\BaseIRepositorie.html").Replace("{Namespace}", nameSpace).Replace("{TableName}", tableName).Replace("{StartName}", stratName);
            generatedFiles["IRepository"] = (iRepositoryPath, domainContent);

            // IService (Partial)
            string iServicePartialPathDir = Path.Combine(frameworkFolder, nameSpace, "IServices", foldername, "Partial");
            string iServicePartialPath = Path.Combine(iServicePartialPathDir, "I" + tableName + "Service.cs");
            if (!FileHelper.FileExists(iServicePartialPath)) // Assuming FileHelper.FileExists can be mocked or handled
            {
                domainContent = FileHelper.ReadFile("Template\\IServices\\IServiceBasePartial.html").Replace("{Namespace}", nameSpace).Replace("{TableName}", tableName).Replace("{StartName}", stratName);
                generatedFiles["IServicePartial"] = (iServicePartialPath, domainContent);
            }

            // IService
            string iServicePath = Path.Combine(frameworkFolder, nameSpace, "IServices", foldername, "I" + tableName + "Service.cs");
            domainContent = FileHelper.ReadFile("Template\\IServices\\IServiceBase.html").Replace("{Namespace}", nameSpace).Replace("{TableName}", tableName).Replace("{StartName}", stratName);
            generatedFiles["IService"] = (iServicePath, domainContent);

            // Service (Partial)
            string servicePartialPathDir = Path.Combine(frameworkFolder, nameSpace, "Services", foldername, "Partial");
            string servicePartialPath = Path.Combine(servicePartialPathDir, tableName + "Service.cs");
            if (!FileHelper.FileExists(servicePartialPath)) // Assuming FileHelper.FileExists can be mocked or handled
            {
                domainContent = FileHelper.ReadFile("Template\\Services\\ServiceBasePartial.html").Replace("{Namespace}", nameSpace).Replace("{TableName}", tableName).Replace("{StartName}", stratName);
                generatedFiles["ServicePartial"] = (servicePartialPath, domainContent);
            }

            // Service
            string servicePath = Path.Combine(frameworkFolder, nameSpace, "Services", foldername, tableName + "Service.cs");
            domainContent = FileHelper.ReadFile("Template\\Services\\ServiceBase.html")
                .Replace("{Namespace}", nameSpace).Replace("{TableName}", tableName)
                .Replace("{StartName}", stratName);
            generatedFiles["Service"] = (servicePath, domainContent);

            // Web Controller
            if (webController)
            {
                string webControllerPartialPathDir = Path.Combine(frameworkFolder, nameSpace, "Controllers", foldername, "Partial");
                string webControllerPartialPath = Path.Combine(webControllerPartialPathDir, tableName + "Controller.cs");
                string webControllerPath = Path.Combine(frameworkFolder, nameSpace, "Controllers", foldername, tableName + "Controller.cs");

                if (!FileHelper.FileExists(webControllerPartialPath)) // Assuming FileHelper.FileExists can be mocked or handled
                {
                    domainContent = FileHelper.ReadFile("Template\\Controller\\ControllerPartial.html").Replace("{Namespace}", nameSpace).Replace("{TableName}", tableName).Replace("{BaseOptions}", baseOptions).Replace("{StartName}", stratName);
                    generatedFiles["WebControllerPartial"] = (webControllerPartialPath, domainContent);
                }
                domainContent = FileHelper.ReadFile("Template\\Controller\\Controller.html").Replace("{Namespace}", nameSpace).Replace("{TableName}", tableName).Replace("{BaseOptions}", baseOptions).Replace("{StartName}", stratName);
                generatedFiles["WebController"] = (webControllerPath, domainContent);
            }
            return generatedFiles;
        }

        private List<object> GetSearchData(List<List<PanelHtml>> panelHtml, List<Sys_TableColumn> sysColumnList, bool vue = false, bool edit = false, bool app = false)
        {
            if (edit)
            {
                GetPanelData(sysColumnList, panelHtml, x => x.EditRowNo, c => c.EditRowNo != null && c.EditRowNo > 0, false, q => q.EditRowNo, vue, app: app);
            }
            else
            {
                GetPanelData(sysColumnList, panelHtml, x => x.SearchRowNo, c => c.SearchRowNo != null, true, q => q.SearchRowNo, vue, app: app);
            }

            List<object> list = new List<object>();
            int index = 0;
            bool group = panelHtml.Exists(x => x.Count > 1);
            panelHtml.ForEach(x =>
            {
                index++;
                List<Dictionary<string, object>> keyValuePairs = new List<Dictionary<string, object>>();
                x.ForEach(s =>
                {
                    Dictionary<string, object> keyValues = new Dictionary<string, object>();
                    if (vue)
                    {
                        if (!string.IsNullOrEmpty(s.dataSource) && s.dataSource != "''")
                        {
                            if (app)
                            {
                                keyValues.Add("key", s.dataSource);
                            }
                            else
                            {
                                keyValues.Add("dataKey", s.dataSource);
                            }
                            keyValues.Add("data", new string[] { });
                        }
                        keyValues.Add("title", s.text);
                        if (s.require)
                        {
                            keyValues.Add("required", s.require);
                        }
                        keyValues.Add("field", s.id);
                        if (s.disabled)
                        {
                            keyValues.Add("disabled", true);
                        }
                        if (s.colSize > 0 && !app)
                        {
                            keyValues.Add("colSize", s.colSize);
                        }
                        if (!string.IsNullOrEmpty(s.displayType) && s.displayType != "''")
                        {
                            keyValues.Add("type", s.columnType == "img" ? s.columnType : s.displayType);
                        }
                    }
                    else
                    {
                        keyValues.Add("columnType", s.columnType);
                        if (!string.IsNullOrEmpty(s.dataSource))
                        {
                            keyValues.Add("dataSource", s.dataSource);
                        }
                        keyValues.Add("text", s.text);
                        if (s.require)
                        {
                            keyValues.Add("require", s.require);
                        }
                        keyValues.Add("id", s.id);
                    }
                    if (!app)
                    {
                        keyValuePairs.Add(keyValues);
                    }
                    else
                    {
                        list.Add(keyValues);
                    }
                });
                if (!app)
                {
                    list.Add(keyValuePairs);
                }
                else
                {
                    if (index != panelHtml.Count() && group)
                    {
                        list.Add(new { type = "group" });
                    }
                }
            });
            return list;
        }

        /// <summary>
        /// 创建Vue页面
        /// </summary>
        /// <param name="sysTableInfo">系统表信息对象</param>
        /// <param name="vuePath">Vue项目Views目录的绝对路径 (例如: E:/web/myProject/Views)</param>
        /// <param name="isViteParam">是否Vite版本</param>
        /// <param name="isAppParam">是否App版本</param>
        /// <returns>操作结果信息</returns>
        public string CreateVuePage(Sys_TableInfo sysTableInfo, string vuePath, bool isViteParam, bool isAppParam)
        {
            if (string.IsNullOrEmpty(vuePath))
            {
                return isAppParam ? "请设置App路径" : "请设置Vue所在Views的绝对路径!";
            }

            if (!FileHelper.DirectoryExists(vuePath)) return $"未找项目路径{vuePath}!";

            if (sysTableInfo == null
              || sysTableInfo.TableColumns == null
              || sysTableInfo.TableColumns.Count == 0)
                return "提交的配置数据不完整";

            vuePath = vuePath.Trim().TrimEnd('/').TrimEnd('\\');

            List<Sys_TableColumn> sysColumnList = sysTableInfo.TableColumns;
            string[] eidtTye = new string[] { "select", "selectList", "drop", "dropList", "checkbox" };
            if (sysColumnList.Exists(x => eidtTye.Contains(x.EditType) && string.IsNullOrEmpty(x.DropNo)))
            {
                return $"编辑类型为[{string.Join(',', eidtTye)}]时必须选择数据源";
            }
            if (sysColumnList.Exists(x => eidtTye.Contains(x.SearchType) && string.IsNullOrEmpty(x.DropNo)))
            {
                return $"查询类型为[{string.Join(',', eidtTye)}]时必须选择数据源";
            }
            if (isAppParam && !sysColumnList.Exists(x => x.Enable > 0))
            {
                return $"请设置[app列]";
            }
            bool editLine = false;
            StringBuilder sb = GetGridColumns(sysColumnList, sysTableInfo.ExpressField, detail: editLine, true, app: isAppParam);
            if (sb.Length == 0) return "未获取到数据!";
            string columns = sb.ToString().Trim();
            columns = columns.Substring(0, columns.Length - 1);
            string key = sysColumnList.Where(c => c.IsKey == 1).Select(x => x.ColumnName).FirstOrDefault() ?? "";

            Func<Sys_TableColumn, bool> editFunc = c => c.EditRowNo != null && c.EditRowNo > 0;
            if (isAppParam)
            {
                editFunc = x => new int[] { ColumnEnableStates.DisplayQueryEdit, ColumnEnableStates.DisplayEdit, ColumnEnableStates.QueryEdit, ColumnEnableStates.EditOnly }.Any(c => c == x.Enable);
            }
            string formFiledsJson = GenerateFormFieldsJson(sysColumnList, editFunc);

            Func<Sys_TableColumn, bool> searchFunc = c => c.SearchRowNo != null && c.SearchRowNo > 0;
            if (isAppParam)
            {
                searchFunc = x => new int[] { ColumnEnableStates.DisplayQueryEdit, ColumnEnableStates.DisplayQuery, ColumnEnableStates.QueryEdit, ColumnEnableStates.QueryOnly }.Any(c => c == x.Enable);
            }
            string searchFormFiledsJson = GenerateSearchFormFieldsJson(sysColumnList, searchFunc);
            string searchFormOptionsJson = GenerateSearchFormOptionsJson(sysColumnList, isAppParam);
            string formOptionsJson = GenerateEditFormOptionsJson(sysColumnList.Where(editFunc).ToList(), isAppParam);

            string pageContent = null;
            string editOptions = "";
            string vueOptions = "";

            if (isAppParam)
            {
                pageContent = FileHelper.ReadFile("Template\\Page\\app\\options.html");
                vueOptions = pageContent;
            }
            else if (HttpContext.Current.Request.Query.ContainsKey("v3"))
            {
                pageContent = FileHelper.ReadFile("Template\\Page\\Vue3SearchPage.html");
                editOptions = FileHelper.ReadFile("Template\\Page\\EditOptions.html");
                vueOptions = FileHelper.ReadFile("Template\\Page\\VueOptions.html");
            }
            else
            {
                pageContent = FileHelper.ReadFile("Template\\Page\\VueSearchPage.html");
            }

            if (string.IsNullOrEmpty(pageContent))
            {
                return "未找到Template模板文件";
            }

            if (!isAppParam && !string.IsNullOrEmpty(vueOptions))
            {
                vueOptions = vueOptions.Replace("#searchFormFileds", searchFormFiledsJson)
                    .Replace("#searchFormOptions", searchFormOptionsJson);
            } else if (isAppParam)
            {
                 vueOptions = vueOptions.Replace("#searchFormFileds", searchFormFiledsJson)
                    .Replace("#searchFormOptions", searchFormOptionsJson);
            }

            string[] arr = sysTableInfo.Namespace.Split(".");
            string spaceFolder = (arr.Length > 1 ? arr[arr.Length - 1] : arr[0]).ToLower();

            if (!isAppParam && !string.IsNullOrEmpty(vueOptions))
            {
                 if (editLine)
                 {
                    vueOptions = vueOptions.Replace("'#key'", "'#key',\r\n                editTable:true ");
                 }
                 vueOptions = vueOptions.Replace("#columns", columns).
                                Replace("#SortName", string.IsNullOrEmpty(sysTableInfo.SortName) ? key : sysTableInfo.SortName).
                                Replace("#key", key).
                                Replace("#Foots", " ").
                                 Replace("#TableName", sysTableInfo.TableName).
                                Replace("#cnName", sysTableInfo.ColumnCNName).
                                Replace("#url", "/" + sysTableInfo.TableName + "/").
                                Replace("#folder", spaceFolder).
                                Replace("#editFormFileds", formFiledsJson).
                                Replace("#editFormOptions", formOptionsJson);
            } else if (isAppParam) {
                 vueOptions = vueOptions.Replace("#columns", columns).
                                Replace("#SortName", string.IsNullOrEmpty(sysTableInfo.SortName) ? key : sysTableInfo.SortName).
                                Replace("#key", key).
                                Replace("#Foots", " ").
                                 Replace("#TableName", sysTableInfo.TableName).
                                Replace("#cnName", sysTableInfo.ColumnCNName).
                                Replace("#url", "/" + sysTableInfo.TableName + "/").
                                Replace("#folder", spaceFolder).
                                Replace("#editFormFileds", formFiledsJson).
                                Replace("#editFormOptions", formOptionsJson);
            }

            vuePath = vuePath.Replace("//", "\\").Trim('\\');

            bool hasSubDetailOut;
            List<string> detailTableConfigStrings = GenerateDetailTableConfigStrings(sysTableInfo, key, isAppParam, out hasSubDetailOut);

            if (!isAppParam && !string.IsNullOrEmpty(vueOptions)) // For v3, vueOptions is used
            {
                if(hasSubDetailOut) { // Use the out parameter
                    vueOptions = vueOptions.Replace("#tables1", detailTableConfigStrings.Count > 0 ? detailTableConfigStrings[0] : "{columns:[]}");
                    vueOptions = vueOptions.Replace("#tables2", $"[{string.Join(",\r                  ", detailTableConfigStrings.Skip(1))}]");
                } else {
                    vueOptions = vueOptions.Replace("#tables1", detailTableConfigStrings.Count > 0 ? detailTableConfigStrings[0] : "{columns:[]}");
                    vueOptions = vueOptions.Replace("#tables2", "[]");
                }
                 if (detailTableConfigStrings.Count == 0) {
                    vueOptions = vueOptions.Replace("#tables1", "{columns:[]}").Replace("#tables2", "[]")
                                .Replace("#detailColumns", "[]").Replace("#detailKey", "\"\"").Replace("#detailSortName", "\"\"");
                 }
            } else if (isAppParam) {
                 if(hasSubDetailOut) {
                    vueOptions = vueOptions.Replace("#tables1", detailTableConfigStrings.Count > 0 ? detailTableConfigStrings[0] : "{columns:[]}");
                    vueOptions = vueOptions.Replace("#tables2", $"[{string.Join(",\r                  ", detailTableConfigStrings.Skip(1))}]");
                } else {
                    vueOptions = vueOptions.Replace("#tables1", detailTableConfigStrings.Count > 0 ? detailTableConfigStrings[0] : "{columns:[]}");
                    vueOptions = vueOptions.Replace("#tables2", "[]");
                }
                if (detailTableConfigStrings.Count == 0) {
                     vueOptions = vueOptions.Replace("#tables1", "{columns:[]}").Replace("#tables2", "[]")
                                .Replace("#detailColumns", "[]").Replace("#detailKey", "\"\"").Replace("#detailSortName", "\"\"");
                }
            }
             if (!isAppParam && HttpContext.Current.Request.Query.ContainsKey("v3") && detailTableConfigStrings.Count == 0 && !string.IsNullOrEmpty(editOptions) ) {
                  editOptions = editOptions.Replace("#detailColumns", "")
                 .Replace("#detailCnName", "")
                 .Replace("#detailTable", "")
                 .Replace("#detailKey", "")
                 .Replace("#detailSortName", "")
                 .Replace("api/#TableName/getDetailPage", "");
             }

            string srcPath = new DirectoryInfo(vuePath.MapPath()).Parent.FullName;
            string finalSpaceFolder = spaceFolder; // Use a new variable for potentially modified spaceFolder

            if (!isAppParam)
            {
                finalSpaceFolder = WriteVueExtensionFile(sysTableInfo, srcPath, spaceFolder, isViteParam);
                pageContent = BuildMainVueFileContent(sysTableInfo, pageContent, vueOptions, editOptions, columns, key, formFiledsJson, formOptionsJson, searchFormFiledsJson, searchFormOptionsJson, currentSpaceFolder, localTableName, editLine, hasSubDetailOut, detailTableConfigStrings, isViteParam);
                string valuePath = $"{vuePath}\\{finalSpaceFolder}\\{sysTableInfo.TableName}.vue";
                 if (!FileHelper.FileExists(valuePath) || FileHelper.ReadFile(valuePath).Contains("setup()"))
                {
                    FileHelper.WriteFile($"{vuePath}\\{finalSpaceFolder}\\", sysTableInfo.TableName + ".vue", pageContent);
                }
                FileHelper.WriteFile($"{vuePath}\\{finalSpaceFolder}\\{sysTableInfo.TableName}\\", "options.js", vueOptions);
                UpdateViewGridRouter(sysTableInfo, srcPath, finalSpaceFolder);
            }
            else // isAppParam == true
            {
                vueOptions = BuildVuePageOptionsScript(sysTableInfo, columns, formFiledsJson, formOptionsJson, searchFormFiledsJson, searchFormOptionsJson, detailTableConfigStrings, true, false, key, hasSubDetailOut, editLine);
                FileHelper.WriteFile($"{vuePath}\\{spaceFolder}\\{sysTableInfo.TableName}\\", sysTableInfo.TableName + "Options.js", vueOptions);

                string appPagePath = $"pages/{spaceFolder}/{sysTableInfo.TableName}/{sysTableInfo.TableName}";
                string appEditPagePath = $"pages/{spaceFolder}/{sysTableInfo.TableName}/{sysTableInfo.TableName}Edit";
                WriteAppSpecificVueFiles(sysTableInfo, vuePath, spaceFolder, appPagePath, appEditPagePath);
                UpdateAppPagesJson(sysTableInfo, srcPath, appPagePath, appEditPagePath);
            }
            return "页面创建成功!";
        }

        public string CreateExtensionPage(Sys_TableInfo tableInfo)
        {
            return "开发中。。。";
        }

        private string GetMySqlStructure(string tableName)
        {
            return $@"SELECT  DISTINCT
                    Column_Name AS ColumnName,
                     '{tableName}'  as tableName,
	                Column_Comment AS ColumnCnName,
                        CASE
                          WHEN data_type IN( 'BIT', 'BOOL', 'bit', 'bool') THEN 'bool'
		             WHEN data_type in('smallint','SMALLINT') THEN 'short'
								WHEN data_type in('tinyint','TINYINT') THEN 'sbyte'
                        WHEN data_type IN('MEDIUMINT','mediumint', 'int','INT','year', 'Year') THEN 'int'
                    WHEN data_type in ( 'BIGINT','bigint') THEN 'bigint'
                    WHEN data_type IN('FLOAT', 'DOUBLE', 'DECIMAL','float', 'double', 'decimal') THEN 'decimal'
                    WHEN data_type IN('CHAR', 'VARCHAR', 'TINY TEXT', 'TEXT', 'MEDIUMTEXT', 'LONGTEXT', 'TINYBLOB', 'BLOB', 'MEDIUMBLOB', 'LONGBLOB', 'Time','char', 'varchar', 'tiny text', 'text', 'mediumtext', 'longtext', 'tinyblob', 'blob', 'mediumblob', 'longblob', 'time') THEN 'string'
                    WHEN data_type IN('Date', 'DateTime', 'TimeStamp','date', 'datetime', 'timestamp') THEN 'DateTime' ELSE 'string'
                END AS ColumnType,
	              case WHEN CHARACTER_MAXIMUM_LENGTH>8000 THEN 0 ELSE CHARACTER_MAXIMUM_LENGTH end  AS Maxlength,
            CASE
                    WHEN COLUMN_KEY <> '' THEN 1 ELSE 0
                END AS IsKey,
            CASE
                    WHEN Column_Name IN( 'CreateID', 'ModifyID', '' ) 
		            OR COLUMN_KEY<> '' THEN 0 ELSE 1
                        END AS IsDisplay,
		            1 AS IsColumnData,
                    120 AS ColumnWidth,
                    0 AS OrderNo,
                CASE
                        WHEN IS_NULLABLE = 'N' or IS_NULLABLE = 'NO' THEN 0 ELSE 1
                    END AS IsNull,
	            CASE
                        WHEN COLUMN_KEY <> '' THEN 1 ELSE 0
                    END AS IsReadDataset,
                ordinal_position
                FROM
                    information_schema.COLUMNS
                WHERE
                    table_name = ?tableName {GetMysqlTableSchema()}
               order by ordinal_position";
        }
        private string GetOracleStructure(string tableName)
        {
            return $@"SELECT
			c.TABLE_NAME TableName ,
			cc.COLUMN_NAME COLUMNNAME,
			cc.COMMENTS  as  ColumnCNName,
		    CASE WHEN   c.DATA_TYPE IN('smallint', 'INT') or (c.DATA_TYPE='NUMBER' and c.DATA_LENGTH=0)   THEN 'int'  
           WHEN  c.DATA_TYPE IN('NUMBER') THEN 'decimal'  
			WHEN c.DATA_TYPE IN('CHAR', 'VARCHAR', 'NVARCHAR','VARCHAR2', 'NVARCHAR2','text', 'image')
			THEN 'string'
		  WHEN  c.DATA_TYPE IN('DATE') THEN 'DateTime'  
			ELSE 'string' 
			end    as ColumnType,
			c.DATA_LENGTH  as Maxlength,
			case WHEN 	c.NULLABLE='Y' THEN 1 ELSE 0 end   as ISNULL,
			1 IsColumnData,1 IsDisplay
			FROM
			user_tab_columns c
			LEFT JOIN   user_col_comments cc ON c.table_name = cc.table_name 
			AND c.column_name = cc.column_name
			LEFT JOIN   user_tab_comments t ON c.table_name = t.table_name 
	
                WHERE
                c.table_name='{tableName.ToUpper()}'";
        }
        private string GetDMStructure(string tableName)
        {
            return $@"SELECT  DISTINCT
                    tc.COLUMN_NAME AS ColumnName,
                     '{tableName}'  as tableName,
	                IFNULL(col.COMMENTS,'') AS ColumnCnName,
                        CASE
                          WHEN data_type IN( 'BIT', 'BOOL', 'bit', 'bool') THEN 'bool'
		             WHEN data_type in('smallint','SMALLINT') THEN 'short'
								WHEN data_type in('tinyint','TINYINT') THEN 'sbyte'
                        WHEN data_type IN('MEDIUMINT','mediumint', 'int','INT','year', 'Year') THEN 'int'
                    WHEN data_type in ( 'BIGINT','bigint') THEN 'bigint'
                    WHEN data_type IN('FLOAT', 'DOUBLE', 'DECIMAL','float', 'double', 'decimal') THEN 'decimal'
                    WHEN data_type IN('CHAR', 'VARCHAR', 'TINY TEXT', 'TEXT', 'MEDIUMTEXT', 'LONGTEXT', 'TINYBLOB', 'BLOB', 'MEDIUMBLOB', 'LONGBLOB', 'Time','char', 'varchar', 'tiny text', 'text', 'mediumtext', 'longtext', 'tinyblob', 'blob', 'mediumblob', 'longblob', 'time') THEN 'string'
                    WHEN data_type IN('Date', 'DateTime', 'TimeStamp','date', 'datetime', 'timestamp') THEN 'DateTime' ELSE 'string'
                END AS ColumnType,
	              case WHEN DATA_LENGTH>8000 THEN 0 ELSE DATA_LENGTH end  AS Maxlength,
            CASE
                    WHEN c.constraint_type='P' THEN 1 ELSE 0
                END AS IsKey,
            CASE
                    WHEN tc.Column_Name IN( 'CreateID', 'ModifyID', '' ) 
		            OR c.constraint_type='P' THEN 0 ELSE 1
                        END AS IsDisplay,
		            1 AS IsColumnData,
                    120 AS ColumnWidth,
                    0 AS OrderNo,
                CASE
                        WHEN NULLABLE = 'NO' THEN 0 ELSE 1
                    END AS IsNull,
	            CASE
                        WHEN c.constraint_type='P' THEN 1 ELSE 0
                    END AS IsReadDataset
                FROM
                    user_tab_columns tc
                INNER JOIN dba_tables t ON tc.TABLE_NAME=t.TABLE_NAME
                LEFT JOIN dba_cons_columns cons ON tc.COLUMN_NAME=cons.COLUMN_NAME AND tc.TABLE_NAME=cons.TABLE_NAME
                LEFT JOIN dba_constraints c ON c.constraint_name=cons.constraint_name
                LEFT JOIN user_col_comments col ON  tc.TABLE_NAME=col.TABLE_NAME AND tc.COLUMN_NAME=col.COLUMN_NAME 

                WHERE  tc.table_name = :tableName AND t.OWNER='{GetDMOwner()}'";
        }

        private string GetSqlServerStructure(string tableName)
        {
            return $@"
            SELECT TableName,
                LTRIM(RTRIM(ColumnName)) AS ColumnName,
                ColumnCNName,
                CASE WHEN ColumnType = 'uniqueidentifier' THEN 'guid'
                     WHEN ColumnType IN('smallint', 'INT') THEN 'int'
                     WHEN ColumnType = 'BIGINT' THEN 'long'
                     WHEN ColumnType IN('CHAR', 'VARCHAR', 'NVARCHAR',
                                          'text', 'xml', 'varbinary', 'image')
                     THEN 'string'
                     WHEN ColumnType IN('tinyint')
                     THEN 'byte'

                       WHEN ColumnType IN('bit') THEN 'bool'
                     WHEN ColumnType IN('time', 'date', 'DATETIME', 'smallDATETIME')
                     THEN 'DateTime'
                     WHEN ColumnType IN('smallmoney', 'DECIMAL', 'numeric',
                                          'money') THEN 'decimal'
                     WHEN ColumnType = 'float' THEN 'float'
                     ELSE 'string '
                END ColumnType,
                CASE WHEN   ColumnType IN ('NVARCHAR','NCHAR') THEN [Maxlength]/2 ELSE [Maxlength] END  [Maxlength],
                IsKey,
                CASE WHEN ColumnName IN('CreateID', 'ModifyID', '')
                          OR IsKey = 1 THEN 0
                     ELSE 1
                END AS IsDisplay ,
				1 AS IsColumnData,

              CASE   WHEN ColumnType IN('time', 'date', 'DATETIME', 'smallDATETIME') THEN 150
                     WHEN ColumnName IN('Modifier', 'Creator') THEN 130
                     WHEN ColumnType IN('int', 'bigint') OR ColumnName IN('CreateID', 'ModifyID', '') THEN 80
                     WHEN[Maxlength] < 110 AND[Maxlength] > 60 THEN 120
                     WHEN[Maxlength] < 200 AND[Maxlength] >= 110 THEN 180
                     WHEN[Maxlength] > 200 THEN 220
                     ELSE 110
                   END AS ColumnWidth ,
                0 AS OrderNo,
                t.[IsNull] AS [IsNull],
            CASE WHEN IsKey = 1 THEN 1 ELSE 0 END IsReadDataset,
            CASE WHEN IsKey!=1 AND t.[IsNull] = 0 THEN 0 ELSE NULL END AS EditColNo
        FROM    (SELECT obj.name AS TableName ,
                            col.name AS ColumnName ,
                            CONVERT(NVARCHAR(100),ISNULL(ep.[value], '')) AS ColumnCNName,
                            t.name AS ColumnType ,
                           CASE WHEN  col.length<1 THEN 0 ELSE  col.length END  AS[Maxlength],
                            CASE WHEN EXISTS (SELECT   1
                                               FROM dbo.sysindexes si
                                                        INNER JOIN dbo.sysindexkeys sik ON si.id = sik.id
                                                              AND si.indid = sik.indid
                                                        INNER JOIN dbo.syscolumns sc ON sc.id = sik.id
                                                              AND sc.colid = sik.colid
                                                        INNER JOIN dbo.sysobjects so ON so.name = si.name
                                                              AND so.xtype = 'PK'
                                               WHERE sc.id = col.id
                                                        AND sc.colid = col.colid)
                                 THEN 1
                                 ELSE 0
                            END AS IsKey ,
                            CASE WHEN col.isnullable = 1 THEN 1
                                 ELSE 0
                            END AS[IsNull],
                            col.colorder
                  FROM      dbo.syscolumns col
                            LEFT JOIN dbo.systypes t ON col.xtype = t.xusertype
                           INNER JOIN dbo.sysobjects obj ON col.id = obj.id
                                                            AND obj.xtype IN ( 'U','V')
                            LEFT JOIN dbo.syscomments comm ON col.cdefault = comm.id
                            LEFT JOIN sys.extended_properties ep ON col.id = ep.major_id
                                                              AND col.colid = ep.minor_id
                                                              AND ep.name = 'MS_Description'
                            LEFT JOIN sys.extended_properties epTwo ON obj.id = epTwo.major_id
                                                              AND epTwo.minor_id = 0
                                                              AND epTwo.name = 'MS_Description'
                  WHERE obj.name = @tableName
                ) AS t
            ORDER BY t.colorder";
        }

        private string GetPgSqlStructure(string tableName)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("SELECT ");
            stringBuilder.Append("	MM.\"TableName\", ");
            stringBuilder.Append("	MM.\"ColumnName\", ");
            stringBuilder.Append(" 	MM.\"ColumnCNName\", ");
            stringBuilder.Append("	MM.\"ColumnType\", ");
            stringBuilder.Append("	MM.\"Maxlength\", ");
            stringBuilder.Append("	MM.\"IsKey\", ");
            stringBuilder.Append("	MM.\"IsDisplay\", ");
            stringBuilder.Append("	MM.\"IsColumnData\", ");
            stringBuilder.Append("CASE ");
            stringBuilder.Append("		WHEN MM.\"ColumnType\" = 'DateTime' THEN 150  ");
            stringBuilder.Append("		WHEN MM.\"ColumnType\" = 'int' THEN 80  ");
            stringBuilder.Append("		WHEN MM.\"Maxlength\" < 110 AND MM.\"Maxlength\" > 60 THEN 120  ");
            stringBuilder.Append("			WHEN MM.\"Maxlength\" < 200 AND MM.\"Maxlength\" >= 110 THEN 180  ");
            stringBuilder.Append("				WHEN MM.\"Maxlength\" > 200 THEN 220 ELSE 110  ");
            stringBuilder.Append("			END AS \"ColumnWidth\", ");
            stringBuilder.Append("			MM.\"OrderNo\", ");
            stringBuilder.Append("		 case WHEN MM.\"IsKey\"=1 or \"lower\"(MM.\"IsNull\")='no' then 0 else 1 end as 	\"IsNull\" , ");
            stringBuilder.Append("			MM.\"IsReadDataset\", ");
            stringBuilder.Append("			MM.\"EditColNo\"  ");
            stringBuilder.Append("		FROM ");
            stringBuilder.Append("			( ");
            stringBuilder.Append("			SELECT ");
            stringBuilder.Append("				col.TABLE_NAME AS \"TableName\", ");
            stringBuilder.Append("				col.COLUMN_NAME AS \"ColumnName\", ");
            stringBuilder.Append("				attr.description AS \"ColumnCNName\", ");
            stringBuilder.Append("			CASE ");
            stringBuilder.Append("					WHEN col.udt_name = 'uuid' THEN 'guid'  ");
            stringBuilder.Append("					WHEN col.udt_name IN ( 'int2') THEN 'short'  ");
            stringBuilder.Append("					WHEN col.udt_name IN ( 'int4' ) THEN 'int'  ");
            stringBuilder.Append("					WHEN col.udt_name = 'int8' THEN 'long'  ");
            stringBuilder.Append("					WHEN col.udt_name = 'BIGINT' THEN 'long'  ");
            stringBuilder.Append("					WHEN col.udt_name IN ( 'char', 'varchar', 'text', 'xml', 'bytea' ) THEN 'string'  ");
            stringBuilder.Append("					WHEN col.udt_name IN ( 'bool' ) THEN 'bool'  ");
            stringBuilder.Append("					WHEN col.udt_name IN ( 'date','timestamp' ) THEN 'DateTime'  ");
            stringBuilder.Append("					WHEN col.udt_name IN ( 'decimal', 'money','numeric' ) THEN 'decimal'  ");
            stringBuilder.Append("					WHEN col.udt_name IN ( 'float4', 'float8' ) THEN 'float' ELSE'string '  ");
            stringBuilder.Append("				END \"ColumnType\", ");
            stringBuilder.Append("CASE ");
            stringBuilder.Append("	WHEN col.udt_name = 'varchar' THEN col.character_maximum_length  ");
            stringBuilder.Append("	WHEN col.udt_name IN ( 'int2', 'int4', 'int8', 'float4', 'float8' ) THEN col.numeric_precision ELSE 1024  ");
            stringBuilder.Append("	END \"Maxlength\", ");
            stringBuilder.Append("CASE ");
            stringBuilder.Append("	WHEN keyTable.IsKey = 1 THEN 1 ELSE 0  ");
            stringBuilder.Append("	END \"IsKey\", ");
            stringBuilder.Append("CASE ");
            stringBuilder.Append("	WHEN keyTable.IsKey = 1 THEN 0 ELSE 1  ");
            stringBuilder.Append("	END \"IsDisplay\", ");
            stringBuilder.Append("	1 AS \"IsColumnData\", ");
            stringBuilder.Append("	0 AS \"OrderNo\", ");
            stringBuilder.Append("	col.is_nullable AS \"IsNull\", ");
            stringBuilder.Append("CASE ");
            stringBuilder.Append("		WHEN keyTable.IsKey = 1 THEN 1 ELSE 0  ");
            stringBuilder.Append("	END \"IsReadDataset\", ");
            stringBuilder.Append("CASE ");
            stringBuilder.Append("	WHEN keyTable.IsKey IS NULL AND col.is_nullable = 'NO' THEN 0 ELSE NULL  ");
            stringBuilder.Append("	END AS \"EditColNo\"  ");
            stringBuilder.Append("FROM ");
            stringBuilder.Append("	information_schema.COLUMNS col  ");
            stringBuilder.Append("  LEFT JOIN ( ");
            stringBuilder.Append("	SELECT col_description(a.attrelid,a.attnum) as description,a.attname as name ");
            stringBuilder.Append("FROM pg_class as c,pg_attribute as a  ");
            stringBuilder.Append("where \"lower\"(c.relname) = \"lower\"(@tableName) and a.attrelid = c.oid and a.attnum>0 ");
            stringBuilder.Append("	) as attr on attr.name=col.COLUMN_NAME ");
            stringBuilder.Append("	LEFT JOIN ( ");
            stringBuilder.Append("	SELECT ");
            stringBuilder.Append("		pg_attribute.attname AS colname, ");
            stringBuilder.Append("		1 AS IsKey  ");
            stringBuilder.Append("	FROM ");
            stringBuilder.Append("		pg_constraint ");
            stringBuilder.Append("		INNER JOIN pg_class ON pg_constraint.conrelid = pg_class.oid ");
            stringBuilder.Append("		INNER JOIN pg_attribute ON pg_attribute.attrelid = pg_class.oid  ");
            stringBuilder.Append("		AND pg_attribute.attnum = pg_constraint.conkey [1]  ");
            stringBuilder.Append("	WHERE ");
            stringBuilder.Append("		\"lower\" ( pg_class.relname ) = \"lower\" ( @tableName )  ");
            stringBuilder.Append("		AND pg_constraint.contype = 'p'  ");
            stringBuilder.Append("	) keyTable ON col.COLUMN_NAME = keyTable.colname  ");
            stringBuilder.Append("WHERE ");
            stringBuilder.Append("	\"lower\" ( TABLE_NAME ) = \"lower\" ( @tableName )  ");
            stringBuilder.Append("ORDER BY ");
            stringBuilder.Append("	ordinal_position  ");
            stringBuilder.Append("	) MM; ");
            return stringBuilder.ToString();
        }

        private void SetMaxLength(List<Sys_TableColumn> columns)
        {
            columns.ForEach(x =>
            {
                if (x.ColumnType.ToLower() == "datetime")
                {
                    x.ColumnWidth = 150;
                }
                else if (x.ColumnName.ToLower() == "modifier" || x.ColumnName.ToLower() == "creator")
                {
                    x.ColumnWidth = 100;
                }
                else if (x.ColumnName.ToLower() == "modifyid" || x.ColumnName.ToLower() == "createid")
                {
                    x.ColumnWidth = 100;
                }
                else if (x.Maxlength > 200)
                {
                    x.ColumnWidth = 220;
                }
                else if (x.Maxlength > 110 && x.Maxlength <= 200)
                {
                    x.ColumnWidth = 180;
                }
                else if (x.Maxlength > 60 && x.Maxlength <= 110)
                {
                    x.ColumnWidth = 120;
                }
                else
                {
                    x.ColumnWidth = 110;
                }
            });
        }

        private int InitTable(int parentId, string tableName, string columnCNName, string nameSpace, string foldername, int tableId, bool isTreeLoad)
        {
            if (isTreeLoad)
                return tableId;
            if (string.IsNullOrEmpty(tableName))
                return -1;
            tableId = repository.FindAsIQueryable(x => x.TableName == tableName).Select(s => s.Table_Id)
                .ToList().FirstOrDefault();
            if (tableId > 0)
                return tableId;
            bool isMySql = DBType.Name == DbCurrentType.MySql.ToString();
            Sys_TableInfo tableInfo = new Sys_TableInfo()
            {
                ParentId = parentId,
                ColumnCNName = columnCNName,
                CnName = columnCNName,
                TableName = tableName,
                Namespace = nameSpace,
                FolderName = foldername,
                Enable = 1
            };
            List<Sys_TableColumn> columns = repository.DapperContext
                .QueryList<Sys_TableColumn>(GetCurrentSql(tableName), new { tableName });

            int orderNo = (columns.Count + 10) * 50;
            for (int i = 0; i < columns.Count; i++)
            {
                columns[i].OrderNo = orderNo;
                orderNo = orderNo - 50;
                columns[i].EditRowNo = 0;
            }

            SetMaxLength(columns);
            var result = base.Add<Sys_TableColumn>(tableInfo, columns, false);
            if (!result.Status)
            {
                throw new Exception($"加载表结构写入异常：{result.Message}");
            }
            return tableInfo.Table_Id;
        }

        public object LoadTable(int parentId, string tableName, string columnCNName, string nameSpace, string foldername, int tableId, bool isTreeLoad)
        {
            if (!UserContext.Current.IsSuperAdmin && !isTreeLoad)
            {
                return new WebResponseContent().Error("只有超级管理员才能进行此操作");
            }
            tableId = InitTable(parentId, tableName?.Trim(), columnCNName, nameSpace, foldername, tableId, isTreeLoad);
            Sys_TableInfo tableInfo = repository
                .FindAsIQueryable(x => x.Table_Id == tableId)
                .Include(c => c.TableColumns)
                .ToList().FirstOrDefault();
            if (tableInfo.TableColumns != null)
            {
                tableInfo.TableColumns = tableInfo.TableColumns.OrderByDescending(x => x.OrderNo).ToList();
            }
            return new WebResponseContent().OK(null, tableInfo);
        }

        public async Task<WebResponseContent> DelTree(int table_Id)
        {
            if (table_Id == 0) return new WebResponseContent().Error("没有传入参数");
            Sys_TableInfo tableInfo = (await repository.FindAsIQueryable(x => x.Table_Id == table_Id)
                .Include(c => c.TableColumns)
               .ToListAsync()).FirstOrDefault();
            if (tableInfo == null) return new WebResponseContent().OK();
            if (tableInfo.TableColumns != null && tableInfo.TableColumns.Count > 0)
            {
                return new WebResponseContent().Error("当前删除的节点存在表结构信息,只能删除空节点");
            }
            if (repository.Exists(x => x.ParentId == table_Id))
            {
                return new WebResponseContent().Error("当前删除的节点存在子节点，不能删除");
            }
            repository.Delete(tableInfo, true);
            return new WebResponseContent().OK();
        }

        private StringBuilder GetGridColumns(List<Sys_TableColumn> list, string expressField, bool detail, bool vue = false, bool app = false)
        {
            totalCol = 0;
            totalWidth = 0;
            StringBuilder sb = new StringBuilder();

            Func<Sys_TableColumn, bool> func = x => true;
            bool sort = false;
            if (app)
            {
                func = x => new int[] { ColumnEnableStates.DisplayQueryEdit, ColumnEnableStates.DisplayEdit, ColumnEnableStates.DisplayQuery, ColumnEnableStates.DisplayOnly }.Any(c => c == x.Enable) && (x.IsDisplay == null || x.IsDisplay == 1);
            }
            foreach (Sys_TableColumn item in list.Where(func).OrderByDescending(x => x.OrderNo))
            {
                if (item.IsColumnData == 0) { continue; }
                sb.Append("{field:'" + item.ColumnName + "',");
                sb.Append("title:'" + (string.IsNullOrEmpty(item.ColumnCnName) ? item.ColumnName : item.ColumnCnName) + "',");
                if (vue)
                {
                    string colType = item.ColumnType.ToLower();
                    if (item.IsImage == 1)
                    {
                        colType = "img";
                    }
                    else if (item.IsImage == 2)
                    {
                        colType = "excel";
                    }
                    else if (item.IsImage == 3)
                    {
                        colType = "file";
                    }
                    else if (item.IsImage == 4)
                    {
                        colType = "date";
                    }
                    sb.Append("type:'" + colType + "',");
                    if (!string.IsNullOrEmpty(item.DropNo))
                    {
                        sb.Append("bind:{ key:'" + item.DropNo + "',data:[]},");
                    }
                    if (expressField != null && expressField.ToLower() == item.ColumnName.ToLower())
                    {
                        sb.Append("link:true,");
                    }
                    if (item.Sortable == 1 && !app)
                    {
                        sb.Append("sort:true,");
                    }
                }
                else
                {
                    sb.Append("datatype:'" + item.ColumnType + "',");
                }

                if (!app)
                {
                    sb.Append("width:" + (item.ColumnWidth ?? 90) + ",");
                }
                if (item.IsDisplay == 0)
                {
                    sb.Append("hidden:true,");
                }
                else
                {
                    totalCol++;
                    totalWidth += item.ColumnWidth == null ? 0 : Convert.ToInt32(item.ColumnWidth);
                }
                if (item.IsReadDataset == 1)
                {
                    sb.Append("readonly:true,");
                }
                if (item.EditRowNo != null && item.EditRowNo > 0 && detail)
                {
                    string editText = vue ? "edit" : "editor";
                    if (vue)
                    {
                        sb.Append("edit:{type:'" + item.EditType + "'},");
                    }
                    else
                    {
                        switch (item.EditType)
                        {
                            case "date":
                                sb.Append("editor:'datebox',");
                                break;
                            case "datetime":
                                sb.Append("editor:'datetimebox',");
                                break;
                            case "drop":
                            case "dropList":
                            case "select":
                            case "selectList":
                                if (!vue && !string.IsNullOrEmpty(item.DropNo))
                                {
                                    sb.Append(editText + ": { type: 'combobox', options: optionConfig" + item.DropNo + " },");
                                }
                                else
                                {
                                    sb.Append(editText + ": 'text',");
                                }
                                break;
                            default:
                                sb.Append(editText + ":'text',");
                                break;
                        }
                    }
                }
                if (!vue)
                {
                    if (expressField != null && expressField.ToLower() == item.ColumnName.ToLower())
                    {
                        sb.Append("formatter:function (val, row, index) { return $.fn.layOut('createViewField',{row:row,val:val,index:index})},");
                    }
                    else if (!string.IsNullOrEmpty(item.Script))
                    {
                        sb.Append("formatter:" + item.Script + ",");
                    }
                    else if (item.IsImage == 1)
                    {
                        sb.Append("formatter:function (val, row, index) { return $.fn.layOut('createImageUrl',{row:row,key:'" + item.ColumnName + "'})},");
                    }
                    else if (!string.IsNullOrEmpty(item.DropNo) && !detail)
                    {
                        sb.AppendLine("formatter: function (val, row, index) {");
                        sb.AppendLine(string.Format("    return dataSource{0}.textFormatter(optionConfig{0}, val, row, index);", item.DropNo));
                        sb.AppendLine("    },");
                    }
                }

                if (item.IsNull == 0 && !app)
                {
                    sb.Append("require:true,");
                }

                if (!app && (item.ColumnType.ToLower() == "datetime" || (item.IsDisplay == 1 & !sort)))
                {
                    sb.Append("align:'left'},");
                }
                else
                {
                    if (!app)
                    {
                        sb.Append("align:'left'},");
                    }
                }
                if (app)
                {
                    sb.Append("},").Replace(",},", "},");
                }

                sb.AppendLine();
                sb.Append("                       ");
            }
            return sb;
        }

        private string CreateEntityModel(List<Sys_TableColumn> sysColumn, Sys_TableInfo tableInfo, List<TableColumnInfo> tableColumnInfoList, int createType)
        {
            string template = "";
            if (createType == 1)
            {
                template = "DomainModel.html";
            }
            else if (createType == 2)
            {
                template = "ApiInputDomainModel.html";
            }
            else
            {
                template = "ApiOutputDomainModel.html";
            }
            string domainContent = FileHelper.ReadFile("Template\\DomianModel\\" + template);
            string partialContent = domainContent;
            StringBuilder AttributeBuilder = new StringBuilder();
            sysColumn = sysColumn.OrderByDescending(c => c.OrderNo).ToList();
            bool addIgnore = false;
            foreach (Sys_TableColumn column in sysColumn)
            {
                column.ColumnType = (column.ColumnType ?? "").Trim();
                AttributeBuilder.Append("/// <summary>");
                AttributeBuilder.Append("\r\n");
                AttributeBuilder.Append("       ///" + column.ColumnCnName + "");
                AttributeBuilder.Append("\r\n");
                AttributeBuilder.Append("       /// </summary>");
                AttributeBuilder.Append("\r\n");
                if (column.IsKey == 1) { AttributeBuilder.Append(@"       [Key]" + ""); AttributeBuilder.Append("\r\n"); }
                AttributeBuilder.Append("       [Display(Name =\"" + (
                    string.IsNullOrEmpty(column.ColumnCnName) ? column.ColumnName : column.ColumnCnName
                    ) + "\")]");
                AttributeBuilder.Append("\r\n");

                TableColumnInfo tableColumnInfo = tableColumnInfoList.Where(x => x.ColumnName.ToLower().Trim() == column.ColumnName.ToLower().Trim()).FirstOrDefault();
                if (tableColumnInfo != null && (tableColumnInfo.ColumnType == "varchar" && column.Maxlength > 8000)
                             || (tableColumnInfo.ColumnType == "nvarchar" && column.Maxlength > 4000))
                {
                    column.Maxlength = 0;
                }

                if (column.ColumnType == "string" && column.Maxlength > 0 && column.Maxlength < 8000)
                {

                    AttributeBuilder.Append("       [MaxLength(" + column.Maxlength + ")]");
                    AttributeBuilder.Append("\r\n");
                }
                if (column.IsColumnData == 0 && createType == 1)
                {
                    addIgnore = true;
                    AttributeBuilder.Append("       [JsonIgnore]");
                    AttributeBuilder.Append("\r\n");
                }

                if (tableColumnInfo != null)
                {
                    if (!string.IsNullOrEmpty(tableColumnInfo.Prec_Scale) && !tableColumnInfo.Prec_Scale.EndsWith(",0"))
                    {
                        AttributeBuilder.Append("       [DisplayFormat(DataFormatString=\"" + tableColumnInfo.Prec_Scale + "\")]");
                        AttributeBuilder.Append("\r\n");
                    }

                    if (
                        (DBType.Name.ToLower() == DbCurrentType.Oracle.ToString().ToLower() && (column.Maxlength == 36))
                        || ((column.IsKey == 1 && (column.ColumnType == "uniqueidentifier")) ||
                        tableColumnInfo.ColumnType.ToLower() == "guid"
                        || ((IsMysql() || IsDM()) && column.ColumnType == "string" && column.Maxlength == 36)))
                    {
                        tableColumnInfo.ColumnType = "uniqueidentifier";
                    }

                    string maxLength = string.Empty;
                    if (tableColumnInfo.ColumnType != "uniqueidentifier")
                    {
                        if (column.IsKey != 1 && column.ColumnType.ToLower() == "string")
                        {
                            if (column.Maxlength <= 0
                                || (tableColumnInfo.ColumnType == "varchar" && column.Maxlength > 8000)
                                || (tableColumnInfo.ColumnType == "nvarchar" && column.Maxlength > 4000))
                            {
                                maxLength = "(max)";
                            }
                            else
                            {
                                maxLength = "(" + column.Maxlength + ")";
                            }
                        }
                        else if (column.IsKey == 1 && column.ColumnType.ToLower() == "string" && column.Maxlength != 36)
                        {
                            maxLength = "(" + column.Maxlength + ")";
                        }
                    }
                    AttributeBuilder.Append("       [Column(TypeName=\"" + tableColumnInfo.ColumnType + maxLength + "\")]");
                    AttributeBuilder.Append("\r\n");

                    if (tableColumnInfo.ColumnType == "int" || tableColumnInfo.ColumnType == "bigint" || tableColumnInfo.ColumnType == "long")
                    {
                        column.ColumnType = tableColumnInfo.ColumnType == "int" ? "int" : "long";
                    }
                    if (tableColumnInfo.ColumnType == "bool")
                    {
                        column.ColumnType = "bit";
                    }
                }

                if (column.EditRowNo != null)
                {
                    AttributeBuilder.Append("       [Editable(true)]");
                    AttributeBuilder.Append("\r\n");
                }
                if (column.IsNull == 0 || (createType == 2 && column.ApiIsNull == 0))
                {
                    AttributeBuilder.Append("       [Required(AllowEmptyStrings=false)]");
                    AttributeBuilder.Append("\r\n");
                }
                string columnType = (column.ColumnType == "Date" ? "DateTime" : column.ColumnType).Trim();
                if (new string[] { "guid", "uniqueidentifier" }.Contains(tableColumnInfo?.ColumnType?.ToLower()))
                {
                    columnType = "Guid";
                }
                if (column.ColumnType.ToLower() != "string" && column.IsNull == 1)
                {
                    columnType = columnType + "?";
                }
                if ((column.IsKey == 1
                    && (column.ColumnType == "uniqueidentifier"))
                       || column.ColumnType == "guid"
                   || ((IsMysql() || IsDM() || IsOracle()) && column.ColumnType == "string" && column.Maxlength == 36))
                {
                    columnType = "Guid" + (column.IsNull == 1 ? "?" : "");
                }
                AttributeBuilder.Append("       public " + columnType + " " + column.ColumnName + " { get; set; }");
                AttributeBuilder.Append("\r\n\r\n       ");
            }
            if (!string.IsNullOrEmpty(tableInfo.DetailName) && createType == 1)
            {
                AttributeBuilder.Append("[Display(Name =\"" + tableInfo.DetailCnName + "\")]");
                AttributeBuilder.Append("\r\n       ");
                AttributeBuilder.Append("[ForeignKey(\"" + sysColumn.Where(x => x.IsKey == 1).FirstOrDefault().ColumnName + "\")]");
                AttributeBuilder.Append("\r\n       ");
                AttributeBuilder.Append("public List<" + tableInfo.DetailName + "> " + tableInfo.DetailName + " { get; set; }");
                AttributeBuilder.Append("\r\n");
            }
            if (addIgnore && createType == 1)
            {
                domainContent = "using Newtonsoft.Json;\r\n" + domainContent + "\r\n";
            }
            string mapPath = ProjectPath.GetProjectDirectoryInfo()?.FullName;
            if (string.IsNullOrEmpty(mapPath))
            {
                return "未找到生成的目录!";
            }
            string[] splitArrr = tableInfo.Namespace.Split('.');
            domainContent = domainContent.Replace("{TableName}", tableInfo.TableName).Replace("{AttributeList}", AttributeBuilder.ToString()).Replace("{StartName}", StratName);

            List<string> entityAttribute = new List<string>();
            entityAttribute.Add("TableCnName = \"" + tableInfo.ColumnCNName + "\"");
            if (!string.IsNullOrEmpty(tableInfo.TableTrueName))
            {
                entityAttribute.Add("TableName = \"" + tableInfo.TableTrueName + "\"");
            }
            if (!string.IsNullOrEmpty(tableInfo.DetailName) && createType == 1)
            {
                string typeArr = " new Type[] { typeof(" + string.Join("),typeof(", tableInfo.DetailName.Split(',')) + ")}";
                entityAttribute.Add("DetailTable = " + typeArr + "");
            }
            if (!string.IsNullOrEmpty(tableInfo.DetailCnName))
            {
                entityAttribute.Add("DetailTableCnName = \"" + tableInfo.DetailCnName + "\"");
            }
            if (!string.IsNullOrEmpty(tableInfo.DBServer) && createType == 1)
            {
                entityAttribute.Add("DBServer = \"" + tableInfo.DBServer + "\"");
            }

            string modelNameSpace = StratName + ".Entity";
            string tableAttr = string.Join(",", entityAttribute);
            if (tableAttr != "")
            {
                tableAttr = "[Entity(" + tableAttr + ")]";
            }
            if (!string.IsNullOrEmpty(tableInfo.TableTrueName) && tableInfo.TableName != tableInfo.TableTrueName)
            {
                string tableTrueName = tableInfo.TableTrueName;
                if (DBType.Name == DbCurrentType.PgSql.ToString())
                {
                    tableTrueName = tableTrueName.ToLower();
                }
                tableAttr = tableAttr + "\r\n[Table(\"" + tableInfo.TableTrueName + "\")]";
            }
            domainContent = domainContent.Replace("{AttributeManager}", tableAttr).Replace("{Namespace}", modelNameSpace);

            string folderName = tableInfo.FolderName;
            string localTableName = tableInfo.TableName; // Renamed to avoid conflict
            if (createType == 2)
            {
                folderName = "ApiEntity\\Input";
                localTableName = "Api" + tableInfo.TableName + "Input";
            }
            else if (createType == 3)
            {
                folderName = "ApiEntity\\OutPut";
                localTableName = "Api" + tableInfo.TableName + "Output";
            }
            string modelPath = $"{mapPath}\\{modelNameSpace}\\DomainModels\\{folderName}\\";
            FileHelper.WriteFile(modelPath, localTableName + ".cs", domainContent);
            modelPath += "partial\\";
            if (!FileHelper.FileExists(modelPath + localTableName + ".cs"))
            {
                partialContent = partialContent.Replace("{AttributeManager}", "")
                    .Replace("{AttributeList}", @"//此处配置字段(字段配置见此model的另一个partial),如果表中没有此字段请加上 [NotMapped]属性，否则会异常")
                    .Replace(":BaseEntity", "")
                    .Replace("{TableName}", tableInfo.TableName).Replace("{Namespace}", modelNameSpace);
                FileHelper.WriteFile(modelPath, localTableName + ".cs", partialContent);
            }
            if (createType == 1)
            {
                string mappingConfiguration = FileHelper.
              ReadFile("Template\\DomianModel\\MappingConfiguration.html")
              .Replace("{TableName}", tableInfo.TableName).Replace("{Namespace}", modelNameSpace).Replace("{StartName}", StratName);
            }
            return "";
        }

        private static string[] formType = new string[] { "bigint", "int", "decimal", "float", "byte" };
        private string GetDisplayType(bool search, string searchType, string editType, string columnType)
        {
            string type = "";
            if (search)
            {
                type = searchType == "无" ? "" : searchType ?? "";
            }
            else
            {
                type = editType == "无" ? "" : editType ?? "";
            }
            if (type == "" && formType.Contains(columnType))
            {
                if (columnType == "decimal" || columnType == "float")
                {
                    type = "decimal";
                }
                else
                {
                    type = "number";
                }
            }
            return type;
        }

        private string GetDropString(string dropNo, bool vue)
        {
            if (string.IsNullOrEmpty(dropNo))
                return vue ? "''" : "__[]__";
            if (vue)
                return dropNo;
            return "__" + "optionConfig" + dropNo + "__";
        }

        private void GetPanelData(List<Sys_TableColumn> list, List<List<PanelHtml>> panelHtml, Func<Sys_TableColumn, int?> keySelector, Func<Sys_TableColumn, bool> predicate, bool search, Func<Sys_TableColumn, int?> orderBy, bool vue = false, bool app = false)
        {
            if (app)
            {
                list.ForEach(x =>
                {
                    if (x.EditRowNo == 0)
                    {
                        x.EditRowNo = 99999;
                    }
                });
                var arr = search
                    ? new int[] { ColumnEnableStates.DisplayQueryEdit, ColumnEnableStates.DisplayQuery, ColumnEnableStates.QueryEdit, ColumnEnableStates.QueryOnly }
                    : new int[] { ColumnEnableStates.DisplayQueryEdit, ColumnEnableStates.DisplayEdit, ColumnEnableStates.QueryEdit, ColumnEnableStates.EditOnly };
                predicate = x => arr.Any(c => c == x.Enable);
            }

            var whereReslut = list.Where(predicate).OrderBy(orderBy).ThenByDescending(c => c.OrderNo).ToList();
            foreach (var item in whereReslut.GroupBy(keySelector))
            {
                panelHtml.Add(item.OrderBy(c => search ? c.SearchColNo : c.EditColNo).Select(
                    x => new PanelHtml
                    {
                        text = x.ColumnCnName ?? x.ColumnName,
                        id = x.ColumnName,
                        displayType = GetDisplayType(search, x.SearchType, x.EditType, x.ColumnType),
                        require = !search && x.IsNull == 0 ? true : false,
                        columnType = vue && x.IsImage == 1 ? "img" : (x.ColumnType ?? "string").ToLower(),
                        disabled = !search && x.IsReadDataset == 1 ? true : false,
                        dataSource = GetDropString(x.DropNo, vue),
                        colSize = search && x.SearchType != "checkbox" ? 0 : (x.ColSize ?? 0)
                    }).ToList());
            }
        }

        private static bool IsOracle()
        {
            return DBType.Name.ToLower() == DbCurrentType.Oracle.ToString().ToLower();
        }
        private static bool IsMysql()
        {
            return DBType.Name.ToLower() == DbCurrentType.MySql.ToString().ToLower();
        }

        private static bool IsDM()
        {
            return DBType.Name.ToLower() == DbCurrentType.DM.ToString().ToLower();
        }

        internal WebResponseContent ValidColumnString(Sys_TableInfo tableInfo)
        {
            WebResponseContent webResponse = new WebResponseContent(true);
            if (tableInfo.TableColumns == null || tableInfo.TableColumns.Count == 0) return webResponse;

            if (!string.IsNullOrEmpty(tableInfo.DetailName))
            {
                Sys_TableColumn mainTableColumn = tableInfo.TableColumns
                     .Where(x => x.IsKey == 1)
                     .FirstOrDefault();
                if (mainTableColumn == null)
                    return webResponse.Error($"请勾选表[{tableInfo.TableName}]的主键");

                string key = mainTableColumn.ColumnName;

                Sys_TableColumn tableColumn = repository
                    .Find<Sys_TableColumn>(x => x.TableName == tableInfo.DetailName && x.ColumnName == key)
                    .ToList().FirstOrDefault();

                if (tableColumn == null)
                    return webResponse.Error($"明细表必须包括[{tableInfo.TableName}]主键字段[{key}]");

                if (mainTableColumn.ColumnType?.ToLower() != tableColumn.ColumnType?.ToLower())
                {
                    return webResponse.Error($"明细表的字段[{tableColumn.ColumnName}]类型必须与主表的主键的类型相同");
                }

                if (!IsMysql() || !IsDM()) return webResponse;

                if (mainTableColumn.ColumnType?.ToLower() == "string"
                    && tableColumn.Maxlength != 36)
                {
                    return webResponse.Error($"主表主键类型为Guid，明细表[{tableInfo.DetailName}]配置的字段[{key}]长度必须是36，请重将明细表字段[{key}]长度设置为36，点击保存与生成Model");
                }
            }
            return webResponse;
        }

        internal string GenerateFormFieldsJson(List<Sys_TableColumn> columns, Func<Sys_TableColumn, bool> editPredicate)
        {
            return columns.Where(editPredicate)
                .OrderBy(o => o.EditRowNo)
                .ThenByDescending(t => t.OrderNo)
                .Select(x => new KeyValuePair<string, object>(x.ColumnName, x.EditType == "checkbox" || x.EditType == "selectList" || x.EditType == "cascader" ? new string[0] : "" as object))
                .ToList().ToDictionary(x => x.Key, x => x.Value).Serialize();
        }

        internal string GenerateSearchFormFieldsJson(List<Sys_TableColumn> columns, Func<Sys_TableColumn, bool> searchPredicate)
        {
            return columns.Where(searchPredicate)
                .Select(x => new KeyValuePair<string, object>(x.ColumnName, x.SearchType == "checkbox"
                || x.SearchType == "selectList" || x.EditType == "cascader" ? new string[0] : x.SearchType == "range" ? new string[] { null, null } : "" as object))
                .ToList().ToDictionary(x => x.Key, x => x.Value).Serialize();
        }

        internal string GenerateSearchFormOptionsJson(List<Sys_TableColumn> columns, bool isAppParam)
        {
            List<List<PanelHtml>> panelHtml = new List<List<PanelHtml>>();
            List<object> list = GetSearchData(panelHtml, columns, true, false, isAppParam);
            return list.Serialize() ?? ""
                .Replace("},{", "},\r\n                               {")
                .Replace("],[", "],\r\n                              [");
        }
         internal string GenerateEditFormOptionsJson(List<Sys_TableColumn> columns, bool isAppParam)
        {
            List<List<PanelHtml>> panelHtml = new List<List<PanelHtml>>();
            List<object> list = GetSearchData(panelHtml, columns, true, true, isAppParam);
            return list.Serialize() ?? ""
                .Replace("},{", "},\r\n                               {")
                .Replace("],[", "],\r\n                              [");
        }

        internal List<string> GenerateDetailTableConfigStrings(Sys_TableInfo sysTableInfo, string mainTableKeyColumn, bool isAppParam, out bool hasSubDetailOutput)
        {
            hasSubDetailOutput = false;
            List<string> detailItems = new List<string>();

            if (string.IsNullOrEmpty(sysTableInfo.DetailName))
            {
                return detailItems;
            }

            var tables = sysTableInfo.DetailName.Replace("，", ",").Split(",");
            var detailTables = repository.FindAsIQueryable(x => tables.Contains(x.TableName))
                .Include(x => x.TableColumns).ToList();

            if (detailTables.Count != tables.Length)
            {
                // Consider throwing an exception or returning an error indicator
                // For now, returning empty list and setting hasSubDetailOutput to false
                return detailItems;
            }

            var tableCNNameArr = sysTableInfo.DetailCnName?.Replace("，", ",")?.Split(',');
            if (tableCNNameArr == null || tableCNNameArr.Length != tables.Count())
            {
                 // Consider throwing an exception or returning an error indicator
                return detailItems;
            }

            List<Sys_TableInfo> orderedDetailTables = new List<Sys_TableInfo>();
            foreach (var name in tables)
            {
                var table = detailTables.FirstOrDefault(x => x.TableName == name);
                if (table == null || table.TableColumns == null || table.TableColumns.Count == 0) {
                    // Consider throwing an exception or returning an error indicator
                     return detailItems;
                }
                orderedDetailTables.Add(table);
            }
            detailTables = orderedDetailTables;

            hasSubDetailOutput = detailTables.Exists(c => !string.IsNullOrEmpty(c.DetailName)) || detailTables.Count > 1;
            int tableIndex = 0;

            foreach (var detailTable in detailTables)
            {
                string tableCNName = tableCNNameArr[tableIndex++];
                string detailItemTemplate = hasSubDetailOutput ? @"  {
                    cnName: '#detailCnName', table: '#detailTable', columns: [#detailColumns],
                    sortName: '#detailSortName', key: '#detailKey', buttons:[], delKeys:[], detail:null }"
                    : @"  {
                    cnName: '#detailCnName', table: '#detailTable', columns: [#detailColumns],
                    sortName: '#detailSortName', key: '#detailKey' }";

                List<Sys_TableColumn> detailList = detailTable.TableColumns;
                StringBuilder sbDetail = GetGridColumns(detailList, detailTable.ExpressField, true, true, isAppParam);
                string detailKey = detailList.Where(c => c.IsKey == 1).Select(x => x.ColumnName).FirstOrDefault() ?? "Id"; // Default to Id if no key found
                string detailColumnsString = sbDetail.ToString().Trim().TrimEnd(',');

                string currentDetailItem = detailItemTemplate
                    .Replace("#detailColumns", detailColumnsString)
                    .Replace("#detailCnName", tableCNName)
                    .Replace("#detailTable", detailTable.TableName)
                    .Replace("#detailKey", detailKey)
                    .Replace("#detailSortName", string.IsNullOrEmpty(detailTable.SortName) ? detailKey : detailTable.SortName);
                detailItems.Add(currentDetailItem);
            }
            return detailItems;
        }

        private string BuildVuePageOptionsScript(Sys_TableInfo sysTableInfo, string columnsJson, string formFieldsJson, string editFormOptionsJson, string searchFormFieldsJson, string searchFormOptionsJson, List<string> detailTableConfigStrings, bool isAppParam, bool isV3Page, string keyColumn, bool hasSubDetail, bool editLine)
        {
            string vueOptionsContent = "";
            if (isAppParam) {
                vueOptionsContent = FileHelper.ReadFile("Template\\Page\\app\\options.html");
            } else if (isV3Page) {
                vueOptionsContent = FileHelper.ReadFile("Template\\Page\\VueOptions.html");
            } else {
                // For older Vue, options are usually part of the main page template or not used in this separate manner.
                // This method might need to return null or an empty string, and the main CreateVuePage would handle it.
                // For now, assume it might still use a generic options template if one existed or be handled by main page template replacements.
                return ""; // Or load a generic options template if available for older Vue
            }

            vueOptionsContent = vueOptionsContent.Replace("#searchFormFileds", searchFormFiledsJson)
                .Replace("#searchFormOptions", searchFormOptionsJson)
                .Replace("#columns", columnsJson)
                .Replace("#SortName", string.IsNullOrEmpty(sysTableInfo.SortName) ? keyColumn : sysTableInfo.SortName)
                .Replace("#key", keyColumn)
                .Replace("#Foots", " ")
                .Replace("#TableName", sysTableInfo.TableName)
                .Replace("#cnName", sysTableInfo.ColumnCNName)
                .Replace("#url", "/" + sysTableInfo.TableName + "/")
                // Folder replacement is handled in BuildMainVueFileContent or WriteAppSpecificVueFiles
                .Replace("#editFormFileds", formFieldsJson)
                .Replace("#editFormOptions", formOptionsJson);

            if (editLine && !isAppParam) { // editTable seems specific to non-app
                vueOptionsContent = vueOptionsContent.Replace("'#key'", $"'{keyColumn}',\r\n                editTable:true ");
            }

            if (hasSubDetail) {
                vueOptionsContent = vueOptionsContent.Replace("#tables1", detailTableConfigStrings.Count > 0 ? detailTableConfigStrings[0] : "{columns:[]}");
                vueOptionsContent = vueOptionsContent.Replace("#tables2", $"[{string.Join(",\r                  ", detailTableConfigStrings.Skip(1))}]");
                 if (detailTableConfigStrings.Count == 0) vueOptionsContent = vueOptionsContent.Replace("#detailColumns", "[]"); // Clear if no primary detail
            } else {
                vueOptionsContent = vueOptionsContent.Replace("#tables1", detailTableConfigStrings.Count > 0 ? detailTableConfigStrings[0] : "{columns:[]}");
                vueOptionsContent = vueOptionsContent.Replace("#tables2", "[]");
                 if (detailTableConfigStrings.Count == 0) vueOptionsContent = vueOptionsContent.Replace("#detailColumns", "[]");
            }
             if (detailTableConfigStrings.Count == 0) {
                  vueOptionsContent = vueOptionsContent.Replace("#detailKey", "\"\"").Replace("#detailSortName", "\"\"");
             }
            vueOptionsContent = vueOptionsContent.Replace("[[]]", "[]"); // Clean up empty array if #tables2 was empty
            return vueOptionsContent;
        }

        private string BuildMainVueFileContent(Sys_TableInfo sysTableInfo, string initialPageContent, string vueOptionsScript, string editOptionsScript, string spaceFolder, bool isViteBuild)
        {
            string mainContent = initialPageContent;
            mainContent = mainContent.Replace("#options", vueOptionsScript); // For V3
            mainContent = mainContent.Replace("#editOptions", editOptionsScript); // For V3
            mainContent = mainContent.Replace("#folder", spaceFolder.Replace("\\", "/"));
            mainContent = mainContent.Replace("#TableName", sysTableInfo.TableName);

            if (isViteBuild && !mainContent.Contains(sysTableInfo.TableName + ".jsx"))
            {
                mainContent = mainContent.Replace(sysTableInfo.TableName + ".js", sysTableInfo.TableName + ".jsx");
            }
            mainContent = mainContent.Replace(".jsxx", ".jsx");
            return mainContent;
        }

        private void WriteVueExtensionFile(Sys_TableInfo sysTableInfo, string vueSrcDir, string projectSpaceFolder, bool isViteBuild)
        {
            string extensionPath = $"{vueSrcDir}\\extension\\{projectSpaceFolder}\\";
            string exFileName = sysTableInfo.TableName + ".js" + (isViteBuild ? "x" : "");

            if (!FileHelper.FileExists(extensionPath + exFileName) ||
                (FileHelper.FileExists($"{vueSrcDir}\\extension\\{projectSpaceFolder}\\{sysTableInfo.FolderName.ToLower()}\\{exFileName}"))) // Check if it was in a subfolder
            {
                 // Logic to ensure correct extension path even if FolderName was used previously
                string correctExtensionPath = $"{vueSrcDir}\\extension\\{projectSpaceFolder}\\{sysTableInfo.FolderName.ToLower()}\\";
                if(FileHelper.DirectoryExists(Path.GetDirectoryName(correctExtensionPath))) { // Check if subfolder exists
                    extensionPath = correctExtensionPath;
                } else {
                     // If subfolder doesn't exist, default to the main spaceFolder for extension
                     // This handles cases where FolderName might not be consistently used for extension path
                }

                if (!FileHelper.FileExists(extensionPath + exFileName)) {
                    string exContent = FileHelper.ReadFile("Template\\Page\\VueExtension.html");
                    exContent = exContent.Replace("#TableName", sysTableInfo.TableName);
                    FileHelper.WriteFile(extensionPath, exFileName, exContent);
                }
            }
        }

        private void UpdateViewGridRouter(Sys_TableInfo sysTableInfo, string vueSrcDir, string projectSpaceFolder)
        {
            string routerPath = $"{vueSrcDir}\\router\\viewGird.js";
            string routerContent = FileHelper.ReadFile(routerPath);
            if (!routerContent.Contains($"path: '/{sysTableInfo.TableName}'"))
            {
                string routerTemplate = FileHelper.ReadFile("Template\\Page\\router.html")
                 .Replace("#TableName", sysTableInfo.TableName)
                 .Replace("#folder", projectSpaceFolder.Replace("\\", "/")); // Use potentially updated spaceFolder
                routerContent = routerContent.Replace("]", routerTemplate);
                FileHelper.WriteFile($"{vueSrcDir}\\router\\", "viewGird.js", routerContent);
            }
        }

        private void UpdateAppPagesJson(Sys_TableInfo sysTableInfo, string appSrcDir, string appPagePath, string appEditPagePath)
        {
            string pagesJsonPath = Path.Combine(appSrcDir, "pages.json");
            string name = FileHelper.ReadFile(pagesJsonPath);

            Action<string, string> ensurePathExists = (pathKey, pageTitle) => {
                if (!name.Contains($"\"{pathKey}\""))
                {
                    int index = name.LastIndexOf("]");
                    if (index == -1) return; // Should not happen in valid json
                    string fragment1 = name.Substring(0, index);
                    string fragment2 = name.Substring(index);

                    StringBuilder builder = new StringBuilder();
                    builder.AppendLine((fragment1.TrimEnd().EndsWith(",") ? "" : ",") + "\r\n		{"); // Add comma if needed
                    builder.AppendLine($"			\"path\": \"{pathKey}\",");
                    builder.AppendLine("			\"style\": {");
                    builder.AppendLine($"				\"navigationBarTitleText\": \"{pageTitle}\"");
                    builder.AppendLine("			}");
                    builder.AppendLine("		}");
                    name = fragment1 + builder.ToString() + "\r\n	" + fragment2; // Ensure proper formatting
                }
            };

            ensurePathExists(appPagePath, sysTableInfo.ColumnCNName);
            ensurePathExists(appEditPagePath, sysTableInfo.ColumnCNName); // Assuming same title for edit page

            FileHelper.WriteFile(appSrcDir, "pages.json", name);
        }

        private void WriteAppSpecificVueFiles(Sys_TableInfo sysTableInfo, string appViewsPath, string projectSpaceFolder, string appPagePath, string appEditPagePath)
        {
            string targetFolder = $"{appViewsPath}\\{projectSpaceFolder}\\{sysTableInfo.TableName}\\";
            string mainPageVue = $"{targetFolder}{sysTableInfo.TableName}.vue";
            string editPageVue = $"{targetFolder}{sysTableInfo.TableName}Edit.vue";

            if (!FileHelper.FileExists(mainPageVue))
            {
                string appPageContent = FileHelper.ReadFile("Template\\Page\\app\\page.html")
                                        .Replace("#TableName", sysTableInfo.TableName)
                                        .Replace("#path", appEditPagePath); // Path for navigation
                FileHelper.WriteFile(targetFolder, sysTableInfo.TableName + ".vue", appPageContent);
            }

            if (!FileHelper.FileExists(editPageVue))
            {
                string appEditContent = FileHelper.ReadFile("Template\\Page\\app\\edit.html")
                                         .Replace("#TableName", sysTableInfo.TableName);
                FileHelper.WriteFile(targetFolder, sysTableInfo.TableName + "Edit.vue", appEditContent);
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
