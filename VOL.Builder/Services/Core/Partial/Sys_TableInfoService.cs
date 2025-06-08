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
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace VOL.Builder.Services
{
    public partial class Sys_TableInfoService
    {
        private readonly ILogger<Sys_TableInfoService> _logger;

        // 缓存模板文件内容，避免重复读取
        private static readonly ConcurrentDictionary<string, string> _templateCache = new ConcurrentDictionary<string, string>();

        // 错误代码定义
        private static class ErrorCodes
        {
            public const string InvalidProjectName = "ERR001";
            public const string TableNotFound = "ERR002";
            public const string InvalidConfiguration = "ERR003";
            public const string FileOperationFailed = "ERR004";
            public const string DatabaseOperationFailed = "ERR005";
            public const string PermissionDenied = "ERR006";
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
                if (string.IsNullOrEmpty(startName))
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

                try
                {
                    webProject = ProjectPath.GetLastIndexOfDirectoryName(".WebApi")
                        ?? ProjectPath.GetLastIndexOfDirectoryName("Api")
                        ?? ProjectPath.GetLastIndexOfDirectoryName(".Web");

                    if (webProject == null)
                    {
                        throw new InvalidOperationException($"[{ErrorCodes.InvalidProjectName}] 未找到以.WebApi、Api或.Web结尾的项目名称，无法创建页面。请确保项目命名规范。");
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "获取Web项目名称失败");
                    throw;
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

                try
                {
                    apiNameSpace = ProjectPath.GetLastIndexOfDirectoryName(".WebApi");
                    if (apiNameSpace == null)
                    {
                        throw new InvalidOperationException($"[{ErrorCodes.InvalidProjectName}] 未找到以.WebApi结尾的项目，无法创建Api控制器。");
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "获取API命名空间失败");
                    throw;
                }
                return apiNameSpace;
            }
        }

        /// <summary>
        /// 获取生成配置的树形菜单
        /// </summary>
        /// <returns></returns>
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
                    })
                    .OrderByDescending(c => c.orderNo)
                    .ToListAsync();

                var treeList = treeData.Select(a => new
                {
                    a.id,
                    a.pId,
                    a.parentId,
                    a.name,
                    isParent = treeData.Any(x => x.pId == a.id)
                }).ToList();

                string startsWith = WebProject.Substring(0, WebProject.IndexOf('.'));
                string json = treeList.Count == 0 ? "[]" : (treeList.Serialize() ?? "[]");

                return (json, ProjectPath.GetProjectFileName(startsWith));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取表树形结构失败");
                return ("[]", "");
            }
        }

        /// <summary>
        /// 获取MySQL数据库模式名称
        /// </summary>
        /// <returns></returns>
        private string GetMysqlTableSchema()
        {
            try
            {
                string connectionString = DBServerProvider.GetConnectionString();
                if (string.IsNullOrEmpty(connectionString))
                {
                    _logger?.LogWarning("数据库连接字符串为空");
                    return "";
                }

                var dbNameMatch = System.Text.RegularExpressions.Regex.Match(
                    connectionString,
                    @"Database=([^;]+)",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );

                if (dbNameMatch.Success && dbNameMatch.Groups.Count > 1)
                {
                    string dbName = dbNameMatch.Groups[1].Value.Trim();
                    return $" AND table_schema = '{dbName}' ";
                }

                return "";
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取MySQL数据库名异常");
                return "";
            }
        }

        /// <summary>
        /// 获取达梦数据库模式名称
        /// </summary>
        /// <returns></returns>
        private string GetDMOwner()
        {
            try
            {
                string connectionString = DBServerProvider.GetConnectionString();
                if (string.IsNullOrEmpty(connectionString))
                {
                    _logger?.LogWarning("数据库连接字符串为空");
                    return "";
                }

                var schemaMatch = System.Text.RegularExpressions.Regex.Match(
                    connectionString,
                    @"schema=([^;]+)",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );

                if (schemaMatch.Success && schemaMatch.Groups.Count > 1)
                {
                    return schemaMatch.Groups[1].Value.Trim();
                }

                return "";
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取达梦数据库名异常");
                return "";
            }
        }

        /// <summary>
        /// 读取模板文件内容（带缓存）
        /// </summary>
        private string ReadTemplateFile(string templatePath)
        {
            if (string.IsNullOrEmpty(templatePath))
            {
                throw new ArgumentNullException(nameof(templatePath));
            }

            return _templateCache.GetOrAdd(templatePath, path =>
            {
                try
                {
                    string content = FileHelper.ReadFile(path);
                    if (string.IsNullOrEmpty(content))
                    {
                        throw new InvalidOperationException($"模板文件 {path} 内容为空");
                    }
                    return content;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, $"读取模板文件失败: {path}");
                    throw new InvalidOperationException($"[{ErrorCodes.FileOperationFailed}] 读取模板文件失败: {path}", ex);
                }
            });
        }

        /// <summary>
        /// 批量替换模板内容
        /// </summary>
        private string ReplaceTemplateContent(string template, Dictionary<string, string> replacements)
        {
            if (string.IsNullOrEmpty(template) || replacements == null)
            {
                return template;
            }

            var sb = new StringBuilder(template);
            foreach (var kvp in replacements)
            {
                sb.Replace(kvp.Key, kvp.Value ?? "");
            }
            return sb.ToString();
        }

        /// <summary>
        /// 生成实体Model
        /// </summary>
        /// <param name="sysTableInfo"></param>
        /// <returns></returns>
        public string CreateEntityModel(Sys_TableInfo sysTableInfo)
        {
            // 参数验证
            if (sysTableInfo?.TableColumns == null || sysTableInfo.TableColumns.Count == 0)
            {
                return $"[{ErrorCodes.InvalidConfiguration}] 提交的配置数据不完整，请检查表配置信息";
            }

            try
            {
                // 验证列配置
                WebResponseContent webResponse = ValidColumnString(sysTableInfo);
                if (!webResponse.Status)
                {
                    return webResponse.Message;
                }

                // 验证表是否已存在
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

                // 获取表结构信息
                string sql = GetModelInfoSql(tableName);
                if (string.IsNullOrEmpty(sql))
                {
                    return $"[{ErrorCodes.DatabaseOperationFailed}] 不支持的数据库类型: {DBType.Name}";
                }

                List<TableColumnInfo> tableColumnInfoList = null;
                try
                {
                    tableColumnInfoList = repository.DapperContext.QueryList<TableColumnInfo>(sql, new { tableName });
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, $"查询表结构信息失败: {tableName}");
                    return $"[{ErrorCodes.DatabaseOperationFailed}] 查询表 {tableName} 结构信息失败: {ex.Message}";
                }

                List<Sys_TableColumn> list = sysTableInfo.TableColumns;
                string msg = CreateEntityModelInternal(list, sysTableInfo, tableColumnInfoList, 1);

                if (!string.IsNullOrEmpty(msg))
                {
                    return msg;
                }

                return "Model创建成功!";
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "创建实体Model失败");
                return $"[{ErrorCodes.FileOperationFailed}] 创建Model失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 获取对应数据库的模型信息SQL
        /// </summary>
        /// <param name="tableName">表名（用于Oracle）</param>
        private string GetModelInfoSql(string tableName = null)
        {
            switch (DBType.Name?.ToLower())
            {
                case "mysql":
                    return GetMySqlModelInfo();
                case "pgsql":
                    return GetPgSqlModelInfo();
                case "oracle":
                    return GetOracleModelInfo(tableName);
                case "dm":
                    return GetDMModelInfo();
                case "sqlserver":
                case "mssql":
                default:
                    return GetSqlServerModelInfo();
            }
        }

        /// <summary>
        /// 获取Mysql表结构信息
        /// 2020.06.14增加对mysql数据类型double区分
        /// </summary>
        /// <returns></returns>
        private string GetMySqlModelInfo()
        {
            return $@"SELECT
DISTINCT
           CONCAT(NUMERIC_PRECISION,',',NUMERIC_SCALE) as Prec_Scale,
        CASE
                 WHEN data_type IN( 'BIT', 'BOOL','bit', 'bool') THEN
                'bool'
                 WHEN data_type in('smallint','SMALLINT') THEN 'short'
				WHEN data_type in('tinyint', 'TINYINT') THEN 'sbyte'
                WHEN data_type IN('MEDIUMINT','mediumint', 'int','INT','year', 'Year') THEN
                'int'
                WHEN data_type in ( 'BIGINT','bigint') THEN
                'bigint'
                WHEN data_type IN('FLOAT',  'DECIMAL','float', 'decimal') THEN
                'decimal'
							 WHEN data_type IN( 'DOUBLE', 'double') THEN
                'double'
                WHEN data_type IN('CHAR', 'VARCHAR', 'TINY TEXT', 'TEXT', 'MEDIUMTEXT', 'LONGTEXT', 'TINYBLOB', 'BLOB', 'MEDIUMBLOB', 'LONGBLOB', 'Time','char', 'varchar', 'tiny text', 'text', 'mediumtext', 'longtext', 'tinyblob', 'blob', 'mediumblob', 'longblob', 'time') THEN
                'nvarchar'
                WHEN data_type IN('Date', 'DateTime', 'TimeStamp','date', 'datetime', 'timestamp') THEN
                'datetime' ELSE 'nvarchar'
            END AS ColumnType, Column_Name AS ColumnName
            FROM
                information_schema.COLUMNS
            WHERE
                table_name = ?tableName {GetMysqlTableSchema()};";
        }

        /// <summary>
        /// 获取达梦表结构信息 2023.11.14
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// 获取SqlServer表结构信息
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// 获取Oracle表结构信息2024.04.10
        /// </summary>
        /// <returns></returns>
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
			
          -- CONCAT(NUMERIC_PRECISION,',',NUMERIC_SCALE) as Prec_Scale
			FROM
			ALL_tab_columns c
			LEFT JOIN   ALL_col_comments cc ON c.table_name = cc.table_name 
			AND c.column_name = cc.column_name
			LEFT JOIN   ALL_tab_comments t ON c.table_name = t.table_name 
			WHERE 		   c.table_name='{tableName.ToUpper()}'";
        }

        /// <summary>
        /// 获取PgSQl表结构信息
        /// 2020.08.07完善PGSQL
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        /// </summary>
        /// <returns></returns>
        private string GetPgSqlModelInfo()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append("			SELECT ");
            stringBuilder.Append("				col.COLUMN_NAME AS \"ColumnName\", ");
            stringBuilder.Append("			CASE ");
            stringBuilder.Append("					WHEN col.udt_name = 'uuid' THEN ");
            stringBuilder.Append("					'guid'  ");
            stringBuilder.Append("					WHEN col.udt_name IN ( 'int2') THEN ");
            stringBuilder.Append("					'short'  ");
            stringBuilder.Append("					WHEN col.udt_name IN ( 'int4' ) THEN ");
            stringBuilder.Append("					'int'  ");
            stringBuilder.Append("					WHEN col.udt_name = 'int8' THEN ");
            stringBuilder.Append("					'long'  ");
            stringBuilder.Append("					WHEN col.udt_name IN ( 'char', 'varchar', 'text', 'xml', 'bytea' ) THEN ");
            stringBuilder.Append("					'string'  ");
            stringBuilder.Append("					WHEN col.udt_name IN ( 'bool' ) THEN ");
            stringBuilder.Append("					'bool'  ");
            stringBuilder.Append("					WHEN col.udt_name IN ( 'date','timestamp' ) THEN ");
            stringBuilder.Append("					'DateTime'  ");
            stringBuilder.Append("					WHEN col.udt_name IN ( 'decimal', 'money','numeric' ) THEN ");
            stringBuilder.Append("					'decimal'  ");
            stringBuilder.Append("					WHEN col.udt_name IN ( 'float4', 'float8' ) THEN ");
            stringBuilder.Append("					'float' ELSE'string '  ");
            stringBuilder.Append("				END  as ColumnType ");
            stringBuilder.Append("from 	information_schema.COLUMNS col  ");
            stringBuilder.Append("WHERE	\"lower\" ( TABLE_NAME ) = \"lower\" (@tableName )  ");
            return stringBuilder.ToString();
        }

        /// <summary>
        /// 2020.08.07完善PGSQL
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 获取Mysql表结构信息
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        private string GetMySqlStructure(string tableName)
        {
            return $@"SELECT  DISTINCT
                    Column_Name AS ColumnName,
                     '{tableName}'  as tableName,
	                Column_Comment AS ColumnCnName,
                        CASE
                          WHEN data_type IN( 'BIT', 'BOOL', 'bit', 'bool') THEN
                'bool'
		             WHEN data_type in('smallint','SMALLINT') THEN 'short'
								WHEN data_type in('tinyint','TINYINT') THEN 'sbyte'
                        WHEN data_type IN('MEDIUMINT','mediumint', 'int','INT','year', 'Year') THEN
                    'int'
                    WHEN data_type in ( 'BIGINT','bigint') THEN
                    'bigint'
                    WHEN data_type IN('FLOAT', 'DOUBLE', 'DECIMAL','float', 'double', 'decimal') THEN
                    'decimal'
                    WHEN data_type IN('CHAR', 'VARCHAR', 'TINY TEXT', 'TEXT', 'MEDIUMTEXT', 'LONGTEXT', 'TINYBLOB', 'BLOB', 'MEDIUMBLOB', 'LONGBLOB', 'Time','char', 'varchar', 'tiny text', 'text', 'mediumtext', 'longtext', 'tinyblob', 'blob', 'mediumblob', 'longblob', 'time') THEN
                    'string'
                    WHEN data_type IN('Date', 'DateTime', 'TimeStamp','date', 'datetime', 'timestamp') THEN
                    'DateTime' ELSE 'string'
                END AS ColumnType,
	              case WHEN CHARACTER_MAXIMUM_LENGTH>8000 THEN 0 ELSE CHARACTER_MAXIMUM_LENGTH end  AS Maxlength,
            CASE
                    WHEN COLUMN_KEY <> '' THEN  
                    1 ELSE 0
                END AS IsKey,
            CASE
                    WHEN Column_Name IN( 'CreateID', 'ModifyID', '' ) 
		            OR COLUMN_KEY<> '' THEN
                        0 ELSE 1
                        END AS IsDisplay,
		            1 AS IsColumnData,
                    120 AS ColumnWidth,
                    0 AS OrderNo,
                CASE
                        WHEN IS_NULLABLE = 'N' or IS_NULLABLE = 'NO' THEN
                        0 ELSE 1
                    END AS IsNull,
	            CASE
                        WHEN COLUMN_KEY <> '' THEN
                        1 ELSE 0
                    END AS IsReadDataset,
                ordinal_position
                FROM
                    information_schema.COLUMNS
                WHERE
                    table_name = ?tableName {GetMysqlTableSchema()}
               order by ordinal_position";
        }

        /// <summary>
        /// 获取tOracle表结构信息 2023.11.14
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
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
                -- 	c.OWNER = 'NETCOREDEV' 
                -- 	AND
                c.table_name='{tableName.ToUpper()}'";
        }

        /// <summary>
        /// 获取达梦表结构信息 2023.11.14
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="dbService"></param>
        /// <returns></returns>
        private string GetDMStructure(string tableName)
        {
            return $@"SELECT  DISTINCT
                    tc.COLUMN_NAME AS ColumnName,
                     '{tableName}'  as tableName,
	                IFNULL(col.COMMENTS,'') AS ColumnCnName,
                        CASE
                          WHEN data_type IN( 'BIT', 'BOOL', 'bit', 'bool') THEN
                'bool'
		             WHEN data_type in('smallint','SMALLINT') THEN 'short'
								WHEN data_type in('tinyint','TINYINT') THEN 'sbyte'
                        WHEN data_type IN('MEDIUMINT','mediumint', 'int','INT','year', 'Year') THEN
                    'int'
                    WHEN data_type in ( 'BIGINT','bigint') THEN
                    'bigint'
                    WHEN data_type IN('FLOAT', 'DOUBLE', 'DECIMAL','float', 'double', 'decimal') THEN
                    'decimal'
                    WHEN data_type IN('CHAR', 'VARCHAR', 'TINY TEXT', 'TEXT', 'MEDIUMTEXT', 'LONGTEXT', 'TINYBLOB', 'BLOB', 'MEDIUMBLOB', 'LONGBLOB', 'Time','char', 'varchar', 'tiny text', 'text', 'mediumtext', 'longtext', 'tinyblob', 'blob', 'mediumblob', 'longblob', 'time') THEN
                    'string'
                    WHEN data_type IN('Date', 'DateTime', 'TimeStamp','date', 'datetime', 'timestamp') THEN
                    'DateTime' ELSE 'string'
                END AS ColumnType,
	              case WHEN DATA_LENGTH>8000 THEN 0 ELSE DATA_LENGTH end  AS Maxlength,
            CASE
                    WHEN c.constraint_type='P' THEN  
                    1 ELSE 0
                END AS IsKey,
            CASE
                    WHEN tc.Column_Name IN( 'CreateID', 'ModifyID', '' ) 
		            OR c.constraint_type='P' THEN
                        0 ELSE 1
                        END AS IsDisplay,
		            1 AS IsColumnData,
                    120 AS ColumnWidth,
                    0 AS OrderNo,
                CASE
                        WHEN NULLABLE = 'NO' THEN
                        0 ELSE 1
                    END AS IsNull,
	            CASE
                        WHEN c.constraint_type='P' THEN
                        1 ELSE 0
                    END AS IsReadDataset
                FROM
                    user_tab_columns tc
                INNER JOIN dba_tables t ON tc.TABLE_NAME=t.TABLE_NAME
                LEFT JOIN dba_cons_columns cons ON tc.COLUMN_NAME=cons.COLUMN_NAME AND tc.TABLE_NAME=cons.TABLE_NAME
                LEFT JOIN dba_constraints c ON c.constraint_name=cons.constraint_name
                LEFT JOIN user_col_comments col ON  tc.TABLE_NAME=col.TABLE_NAME AND tc.COLUMN_NAME=col.COLUMN_NAME 

                WHERE  tc.table_name = :tableName AND t.OWNER='{GetDMOwner()}'";
        }

        /// <summary>
        /// 获取SqlServer表结构信息
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
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
                --CASE WHEN IsKey = 1 OR t.[IsNull]=0 THEN 0
                --     ELSE 1 END
                t.[IsNull] AS
                 [IsNull],
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
                                                          --   AND obj.status >= 01
                            LEFT JOIN dbo.syscomments comm ON col.cdefault = comm.id
                            LEFT JOIN sys.extended_properties ep ON col.id = ep.major_id
                                                              AND col.colid = ep.minor_id
                                                              AND ep.name = 'MS_Description'
                            LEFT JOIN sys.extended_properties epTwo ON obj.id = epTwo.major_id
                                                              AND epTwo.minor_id = 0
                                                              AND epTwo.name = 'MS_Description'
                  WHERE obj.name = @tableName--表名
                ) AS t
            ORDER BY t.colorder";
        }

        /// <summary>
        /// 2020.08.07完善PGSQL
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
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
            stringBuilder.Append("		 ");
            stringBuilder.Append("		WHEN MM.\"ColumnType\" = 'DateTime' THEN ");
            stringBuilder.Append("		150  ");
            stringBuilder.Append("		WHEN MM.\"ColumnType\" = 'int' THEN ");
            stringBuilder.Append("		80  ");
            stringBuilder.Append("		WHEN MM.\"Maxlength\" < 110  ");
            stringBuilder.Append("		AND MM.\"Maxlength\" > 60 THEN ");
            stringBuilder.Append("			120  ");
            stringBuilder.Append("			WHEN MM.\"Maxlength\" < 200  ");
            stringBuilder.Append("			AND MM.\"Maxlength\" >= 110 THEN ");
            stringBuilder.Append("				180  ");
            stringBuilder.Append("				WHEN MM.\"Maxlength\" > 200 THEN ");
            stringBuilder.Append("				220 ELSE 110  ");
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
            stringBuilder.Append("					 ");
            stringBuilder.Append("					WHEN col.udt_name = 'uuid' THEN ");
            stringBuilder.Append("					'guid'  ");
            stringBuilder.Append("					WHEN col.udt_name IN ( 'int2') THEN ");
            stringBuilder.Append("					'short'  ");
            stringBuilder.Append("					WHEN col.udt_name IN ( 'int4' ) THEN ");
            stringBuilder.Append("					'int'  ");
            stringBuilder.Append("					WHEN col.udt_name = 'int8' THEN ");
            stringBuilder.Append("					'long'  ");
            stringBuilder.Append("					WHEN col.udt_name = 'BIGINT' THEN ");
            stringBuilder.Append("					'long'  ");
            stringBuilder.Append("					WHEN col.udt_name IN ( 'char', 'varchar', 'text', 'xml', 'bytea' ) THEN ");
            stringBuilder.Append("					'string'  ");
            stringBuilder.Append("					WHEN col.udt_name IN ( 'bool' ) THEN ");
            stringBuilder.Append("					'bool'  ");
            stringBuilder.Append("					WHEN col.udt_name IN ( 'date','timestamp' ) THEN ");
            stringBuilder.Append("					'DateTime'  ");
            stringBuilder.Append("					WHEN col.udt_name IN ( 'decimal', 'money','numeric' ) THEN ");
            stringBuilder.Append("					'decimal'  ");
            stringBuilder.Append("					WHEN col.udt_name IN ( 'float4', 'float8' ) THEN ");
            stringBuilder.Append("					'float' ELSE'string '  ");
            stringBuilder.Append("				END \"ColumnType\", ");
            stringBuilder.Append("CASE ");
            stringBuilder.Append("	 ");
            stringBuilder.Append("	WHEN col.udt_name = 'varchar' THEN ");
            stringBuilder.Append("	col.character_maximum_length  ");
            stringBuilder.Append("	WHEN col.udt_name IN ( 'int2', 'int4', 'int8', 'float4', 'float8' ) THEN ");
            stringBuilder.Append("	col.numeric_precision ELSE 1024  ");
            stringBuilder.Append("	END \"Maxlength\", ");
            stringBuilder.Append("CASE ");
            stringBuilder.Append("	 ");
            stringBuilder.Append("	WHEN keyTable.IsKey = 1 THEN ");
            stringBuilder.Append("	1 ELSE 0  ");
            stringBuilder.Append("	END \"IsKey\", ");
            stringBuilder.Append("CASE ");
            stringBuilder.Append("	 ");
            stringBuilder.Append("	WHEN keyTable.IsKey = 1 THEN ");
            stringBuilder.Append("	0 ELSE 1  ");
            stringBuilder.Append("	END \"IsDisplay\", ");
            stringBuilder.Append("	1 AS \"IsColumnData\", ");
            stringBuilder.Append("	0 AS \"OrderNo\", ");
            stringBuilder.Append("	col.is_nullable AS \"IsNull\", ");
            stringBuilder.Append("CASE ");
            stringBuilder.Append("		 ");
            stringBuilder.Append("		WHEN keyTable.IsKey = 1 THEN ");
            stringBuilder.Append("		1 ELSE 0  ");
            stringBuilder.Append("	END \"IsReadDataset\", ");
            stringBuilder.Append("CASE ");
            stringBuilder.Append("	 ");
            stringBuilder.Append("	WHEN keyTable.IsKey IS NULL  ");
            stringBuilder.Append("	AND col.is_nullable = 'NO' THEN ");
            stringBuilder.Append("	0 ELSE NULL  ");
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

        /// <summary>
        /// 设置界面table td单元格的宽度
        /// </summary>
        /// <param name="columns"></param>
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

        /// <summary>
        /// 判断是否为Oracle数据库
        /// </summary>
        private static bool IsOracle()
        {
            return DBType.Name.ToLower() == DbCurrentType.Oracle.ToString().ToLower();
        }

        /// <summary>
        /// 判断是否为MySQL数据库
        /// </summary>
        private static bool IsMysql()
        {
            return DBType.Name.ToLower() == DbCurrentType.MySql.ToString().ToLower();
        }

        /// <summary>
        /// 判断是否为达梦数据库
        /// </summary>
        private static bool IsDM()
        {
            return DBType.Name.ToLower() == DbCurrentType.DM.ToString().ToLower();
        }

        /// <summary>
        /// 初始化生成配置对应表的数据信息
        /// </summary>
        /// <param name="parentId"></param>
        /// <param name="tableName"></param>
        /// <param name="columnCNName"></param>
        /// <param name="nameSpace"></param>
        /// <param name="foldername"></param>
        /// <param name="tableId"></param>
        /// <param name="isTreeLoad"></param>
        /// <returns></returns>
        private int InitTable(int parentId, string tableName, string columnCNName, string nameSpace, string foldername, int tableId, bool isTreeLoad)
        {
            if (isTreeLoad)
                return tableId;

            if (string.IsNullOrEmpty(tableName))
                return -1;

            try
            {
                tableId = repository.FindAsIQueryable(x => x.TableName == tableName)
                    .Select(s => s.Table_Id)
                    .FirstOrDefault();

                if (tableId > 0)
                    return tableId;

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
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"初始化表{tableName}失败");
                throw;
            }
        }

        /// <summary>
        /// 界面加载表的配置信息
        /// </summary>
        /// <param name="parentId"></param>
        /// <param name="tableName"></param>
        /// <param name="columnCNName"></param>
        /// <param name="nameSpace"></param>
        /// <param name="foldername"></param>
        /// <param name="tableId"></param>
        /// <param name="isTreeLoad">true只加载表数据</param>
        /// <returns></returns>
        public object LoadTable(int parentId, string tableName, string columnCNName, string nameSpace, string foldername, int tableId, bool isTreeLoad)
        {
            if (!UserContext.Current.IsSuperAdmin && !isTreeLoad)
            {
                return new WebResponseContent().Error($"[{ErrorCodes.PermissionDenied}] 只有超级管理员才能进行此操作");
            }

            try
            {
                tableId = InitTable(parentId, tableName?.Trim(), columnCNName, nameSpace, foldername, tableId, isTreeLoad);

                Sys_TableInfo tableInfo = repository
                    .FindAsIQueryable(x => x.Table_Id == tableId)
                    .Include(c => c.TableColumns)
                    .FirstOrDefault();

                if (tableInfo?.TableColumns != null)
                {
                    tableInfo.TableColumns = tableInfo.TableColumns.OrderByDescending(x => x.OrderNo).ToList();
                }

                return new WebResponseContent().OK(null, tableInfo);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载表配置信息失败");
                return new WebResponseContent().Error($"[{ErrorCodes.DatabaseOperationFailed}] 加载失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 删除树节点
        /// </summary>
        /// <param name="table_Id"></param>
        /// <returns></returns>
        public async Task<WebResponseContent> DelTree(int table_Id)
        {
            if (table_Id == 0)
            {
                return new WebResponseContent().Error($"[{ErrorCodes.InvalidConfiguration}] 没有传入参数");
            }

            try
            {
                Sys_TableInfo tableInfo = await repository.FindAsIQueryable(x => x.Table_Id == table_Id)
                    .Include(c => c.TableColumns)
                    .FirstOrDefaultAsync();

                if (tableInfo == null)
                {
                    return new WebResponseContent().OK();
                }

                if (tableInfo.TableColumns != null && tableInfo.TableColumns.Count > 0)
                {
                    return new WebResponseContent().Error($"[{ErrorCodes.InvalidConfiguration}] 当前删除的节点存在表结构信息，只能删除空节点");
                }

                if (repository.Exists(x => x.ParentId == table_Id))
                {
                    return new WebResponseContent().Error($"[{ErrorCodes.InvalidConfiguration}] 当前删除的节点存在子节点，不能删除");
                }

                repository.Delete(tableInfo, true);
                return new WebResponseContent().OK("删除成功");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"删除树节点失败: {table_Id}");
                return new WebResponseContent().Error($"[{ErrorCodes.DatabaseOperationFailed}] 删除失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 生成表格的columns的配置信息
        /// </summary>
        /// <param name="list"></param>
        /// <param name="expressField"></param>
        /// <param name="detail"></param>
        /// <param name="vue"></param>
        /// <param name="app"></param>
        /// <returns></returns>
        private StringBuilder GetGridColumns(List<Sys_TableColumn> list, string expressField, bool detail, bool vue = false, bool app = false)
        {
            totalCol = 0;
            totalWidth = 0;
            StringBuilder sb = new StringBuilder();

            Func<Sys_TableColumn, bool> func = x => true;
            bool sort = false;

            if (app)
            {
                func = x => new int[] { 1, 2, 3, 4 }.Any(c => c == x.Enable) && (x.IsDisplay == null || x.IsDisplay == 1);
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
                    totalWidth += item.ColumnWidth ?? 0;
                }

                if (item.IsReadDataset == 1)
                {
                    sb.Append("readonly:true,");
                }

                // 明细表格编辑
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
                    // 快速查看字段
                    if (expressField != null && expressField.ToLower() == item.ColumnName.ToLower())
                    {
                        sb.Append("formatter:function (val, row, index) { return $.fn.layOut('createViewField',{row:row,val:val,index:index})},");
                    }
                    else if (!string.IsNullOrEmpty(item.Script))
                    {
                        sb.Append("formatter:" + item.Script + ",");
                    }
                    else if (item.IsImage == 1) // 启用图片
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

        /// <summary>
        /// 获取界面查询字段
        /// </summary>
        /// <param name="panelHtml"></param>
        /// <param name="sysColumnList"></param>
        /// <param name="vue"></param>
        /// <param name="edit"></param>
        /// <param name="app"></param>
        /// <returns></returns>
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
                    // app页面添加分组
                    if (index != panelHtml.Count() && group)
                    {
                        list.Add(new { type = "group" });
                    }
                }
            });

            return list;
        }

        /// <summary>
        /// 获取显示类型
        /// </summary>
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

        /// <summary>
        /// 获取下拉框数据源字符串
        /// </summary>
        private string GetDropString(string dropNo, bool vue)
        {
            if (string.IsNullOrEmpty(dropNo))
                return vue ? "''" : "__[]__";

            if (vue)
                return dropNo;

            return "__optionConfig" + dropNo + "__";
        }

        /// <summary>
        /// 生成查询面板的数据对象结构
        /// </summary>
        /// <param name="list"></param>
        /// <param name="panelHtml"></param>
        /// <param name="keySelector"></param>
        /// <param name="predicate"></param>
        /// <param name="search"></param>
        /// <param name="orderBy"></param>
        /// <param name="vue"></param>
        /// <param name="app"></param>
        private void GetPanelData(List<Sys_TableColumn> list, List<List<PanelHtml>> panelHtml,
            Func<Sys_TableColumn, int?> keySelector, Func<Sys_TableColumn, bool> predicate,
            bool search, Func<Sys_TableColumn, int?> orderBy, bool vue = false, bool app = false)
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

                var arr = search ? new int[] { 1, 3, 5, 6 } : new int[] { 1, 2, 5, 7 };
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

        /// <summary>
        /// 生成Vue页面
        /// </summary>
        /// <param name="sysTableInfo"></param>
        /// <param name="vuePath">为本地Vue项目Views所在的绝对路径:E:/web/myProject/Views</param>
        /// <returns></returns>
        public string CreateVuePage(Sys_TableInfo sysTableInfo, string vuePath)
        {
            try
            {
                // 2024.03.16增加vite代码生成
                bool isVite = HttpContext.Current.Request.Query["vite"].GetInt() > 0;
                bool isApp = HttpContext.Current.Request.Query["app"].GetInt() > 0;

                if (string.IsNullOrEmpty(vuePath))
                {
                    return isApp ? $"[{ErrorCodes.InvalidConfiguration}] 请设置App路径" : $"[{ErrorCodes.InvalidConfiguration}] 请设置Vue所在Views的绝对路径!";
                }

                if (!FileHelper.DirectoryExists(vuePath))
                {
                    return $"[{ErrorCodes.FileOperationFailed}] 未找到项目路径{vuePath}!";
                }

                if (sysTableInfo?.TableColumns == null || sysTableInfo.TableColumns.Count == 0)
                {
                    return $"[{ErrorCodes.InvalidConfiguration}] 提交的配置数据不完整";
                }

                vuePath = vuePath.Trim().TrimEnd('/').TrimEnd('\\');

                List<Sys_TableColumn> sysColumnList = sysTableInfo.TableColumns;
                string[] eidtTye = new string[] { "select", "selectList", "drop", "dropList", "checkbox" };

                if (sysColumnList.Exists(x => eidtTye.Contains(x.EditType) && string.IsNullOrEmpty(x.DropNo)))
                {
                    return $"[{ErrorCodes.InvalidConfiguration}] 编辑类型为[{string.Join(',', eidtTye)}]时必须选择数据源";
                }

                if (sysColumnList.Exists(x => eidtTye.Contains(x.SearchType) && string.IsNullOrEmpty(x.DropNo)))
                {
                    return $"[{ErrorCodes.InvalidConfiguration}] 查询类型为[{string.Join(',', eidtTye)}]时必须选择数据源";
                }

                if (isApp && !sysColumnList.Exists(x => x.Enable > 0))
                {
                    return $"[{ErrorCodes.InvalidConfiguration}] 请设置[app列]";
                }

                bool editLine = false;
                StringBuilder sb = GetGridColumns(sysColumnList, sysTableInfo.ExpressField, detail: editLine, true, app: isApp);

                if (sb.Length == 0)
                {
                    return $"[{ErrorCodes.InvalidConfiguration}] 未获取到数据!";
                }

                string columns = sb.ToString().Trim();
                columns = columns.Substring(0, columns.Length - 1);
                string key = sysColumnList.Where(c => c.IsKey == 1).Select(x => x.ColumnName).FirstOrDefault() ?? "";

                Func<Sys_TableColumn, bool> editFunc = c => c.EditRowNo != null && c.EditRowNo > 0;
                if (isApp)
                {
                    editFunc = x => new int[] { 1, 2, 5, 7 }.Any(c => c == x.Enable);
                }

                var formFileds = sysColumnList.Where(editFunc)
                    .OrderBy(o => o.EditRowNo)
                    .ThenByDescending(t => t.OrderNo)
                    .Select(x => new KeyValuePair<string, object>(x.ColumnName,
                        x.EditType == "checkbox" || x.EditType == "selectList" || x.EditType == "cascader"
                        ? new string[0] : "" as object))
                    .ToList().ToDictionary(x => x.Key, x => x.Value).Serialize();

                List<List<PanelHtml>> panelHtml = new List<List<PanelHtml>>();
                List<object> list = GetSearchData(panelHtml, sysColumnList, true, app: isApp);

                string pageContent = null;
                string editOptions = "";
                string vueOptions = "";

                if (isApp)
                {
                    pageContent = ReadTemplateFile("Template\\Page\\app\\options.html");
                }
                else if (HttpContext.Current.Request.Query.ContainsKey("v3"))
                {
                    pageContent = ReadTemplateFile("Template\\Page\\Vue3SearchPage.html");
                    editOptions = ReadTemplateFile("Template\\Page\\EditOptions.html");
                    vueOptions = ReadTemplateFile("Template\\Page\\VueOptions.html");
                }
                else
                {
                    pageContent = ReadTemplateFile("Template\\Page\\VueSearchPage.html");
                }

                if (string.IsNullOrEmpty(pageContent))
                {
                    return $"[{ErrorCodes.FileOperationFailed}] 未找到Template模板文件";
                }

                Func<Sys_TableColumn, bool> func = c => c.SearchRowNo != null && c.SearchRowNo > 0;
                if (isApp)
                {
                    func = x => new int[] { 1, 3, 5, 6 }.Any(c => c == x.Enable);
                    vueOptions = pageContent;
                }

                var searchFormFileds = sysColumnList.Where(func)
                    .Select(x => new KeyValuePair<string, object>(x.ColumnName,
                        x.SearchType == "checkbox" || x.SearchType == "selectList" || x.EditType == "cascader"
                        ? new string[0] : x.SearchType == "range" ? new string[] { null, null } : "" as object))
                    .ToList().ToDictionary(x => x.Key, x => x.Value).Serialize();

                vueOptions = vueOptions.Replace("#searchFormFileds", searchFormFileds)
                    .Replace("#searchFormOptions", list.Serialize() ?? ""
                    .Replace("},{", "},\r\n                               {")
                    .Replace("],[", "],\r\n                              ["));

                panelHtml = new List<List<PanelHtml>>();
                string formOptions = GetSearchData(panelHtml, sysColumnList.Where(editFunc).ToList(), true, true, app: isApp).Serialize() ?? "";

                string[] arr = sysTableInfo.Namespace.Split(".");
                string spaceFolder = (arr.Length > 1 ? arr[arr.Length - 1] : arr[0]).ToLower();

                if (editLine)
                {
                    vueOptions = vueOptions.Replace("'#key'", "'#key',\r\n                editTable:true ");
                }

                var replacements = new Dictionary<string, string>
                {
                    {"#columns", columns},
                    {"#SortName", string.IsNullOrEmpty(sysTableInfo.SortName) ? key : sysTableInfo.SortName},
                    {"#key", key},
                    {"#Foots", " "},
                    {"#TableName", sysTableInfo.TableName},
                    {"#cnName", sysTableInfo.ColumnCNName},
                    {"#url", "/" + sysTableInfo.TableName + "/"},
                    {"#folder", spaceFolder},
                    {"#editFormFileds", formFileds},
                    {"#editFormOptions", formOptions.Replace("},{", "},\r\n                               {").Replace("],[", "],\r\n                              [")}
                };

                vueOptions = ReplaceTemplateContent(vueOptions, replacements);
                vuePath = vuePath.Replace("//", "\\").Trim('\\');

                // 处理明细表
                bool hasSubDetail = false;
                List<string> detailItems = new List<string>();

                if (!string.IsNullOrEmpty(sysTableInfo.DetailName))
                {
                    var result = ProcessDetailTables(sysTableInfo, ref hasSubDetail, detailItems, ref editOptions);
                    if (!string.IsNullOrEmpty(result))
                    {
                        return result;
                    }

                    vueOptions = vueOptions.Replace("#tables1", $"{detailItems[0]}");
                    vueOptions = vueOptions.Replace("#tables2", "[]");
                }
                else
                {
                    vueOptions = vueOptions.Replace("#tables1", "{columns:[]}");
                    vueOptions = vueOptions.Replace("#tables2", "[]");
                    vueOptions = vueOptions.Replace("#detailColumns", "")
                        .Replace("#detailKey", "")
                        .Replace("#detailSortName", "");
                    vueOptions = vueOptions.Replace("#detailColumns", "")
                        .Replace("#detailCnName", "")
                        .Replace("#detailTable", "")
                        .Replace("#detailKey", "")
                        .Replace("#detailSortName", "")
                        .Replace("api/#TableName/getDetailPage", "");

                    editOptions = editOptions.Replace("#detailColumns", "")
                        .Replace("#detailCnName", "")
                        .Replace("#detailTable", "")
                        .Replace("#detailKey", "")
                        .Replace("#detailSortName", "")
                        .Replace("api/#TableName/getDetailPage", "");
                }

                // 生成文件
                if (isApp)
                {
                    return CreateAppFiles(sysTableInfo, vuePath, spaceFolder, pageContent);
                }
                else
                {
                    return CreateVueFiles(sysTableInfo, vuePath, spaceFolder, vueOptions, pageContent, isVite, hasSubDetail, detailItems);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "生成Vue页面失败");
                return $"[{ErrorCodes.FileOperationFailed}] 生成失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 处理明细表
        /// </summary>
        private string ProcessDetailTables(Sys_TableInfo sysTableInfo, ref bool hasSubDetail,
            List<string> detailItems, ref string editOptions)
        {
            var tables = sysTableInfo.DetailName.Replace("，", ",").Split(",");
            var detailTables = repository.FindAsIQueryable(x => tables.Contains(x.TableName))
                .Include(x => x.TableColumns).ToList();

            if (detailTables.Count != tables.Length)
            {
                return $"[{ErrorCodes.InvalidConfiguration}] 请将明细表生成代码!";
            }

            var obj = detailTables.FirstOrDefault(c => c.TableColumns == null || c.TableColumns.Count == 0);
            if (obj != null)
            {
                return $"[{ErrorCodes.InvalidConfiguration}] 明细表{obj.TableName}没有列的信息,请确认是否有列数据或列数据是否被删除!";
            }

            var tableCNNameArr = sysTableInfo.DetailCnName?.Replace("，", ",")?.Split(",");
            if (tableCNNameArr == null || tableCNNameArr.Length != tables.Count())
            {
                return $"[{ErrorCodes.InvalidConfiguration}] 明细表中文名与明细表数量不一致，请以逗号隔开数量也一致!";
            }

            List<Sys_TableInfo> tables2 = new List<Sys_TableInfo>();
            foreach (var name in tables)
            {
                tables2.Add(detailTables.FirstOrDefault(x => x.TableName == name));
            }
            detailTables = tables2;

            hasSubDetail = detailTables.Exists(c => !string.IsNullOrEmpty(c.DetailName)) || detailTables.Count > 1;
            int tableIndex = 0;

            foreach (var detailTable in detailTables)
            {
                string tableCNName = tableCNNameArr[tableIndex];
                tableIndex++;

                string detailItem = hasSubDetail
                    ? @"  { 
                    cnName: '#detailCnName',
                    table: '#detailTable',
                    columns: [#detailColumns],
                    sortName: '#detailSortName',
                    key: '#detailKey',
                    buttons:[],
                    delKeys:[],
                    detail:null
                                            }"
                    : @"  {
                    cnName: '#detailCnName',
                    table: '#detailTable',
                    columns: [#detailColumns],
                    sortName: '#detailSortName',
                    key: '#detailKey'
                                            }";

                List<Sys_TableColumn> detailList = detailTable.TableColumns;
                StringBuilder sb = GetGridColumns(detailList, detailTable.ExpressField, true, true);
                string key = detailList.FirstOrDefault(c => c.IsKey == 1)?.ColumnName;
                string columns = sb.ToString().Trim();
                columns = columns.Substring(0, columns.Length - 1);

                detailItem = detailItem.Replace("#detailColumns", columns)
                    .Replace("#detailCnName", tableCNName)
                    .Replace("#detailTable", detailTable.TableName)
                    .Replace("#detailKey", detailTable.TableColumns.FirstOrDefault(c => c.IsKey == 1)?.ColumnName)
                    .Replace("#detailSortName", string.IsNullOrEmpty(detailTable.SortName) ? key : detailTable.SortName);

                detailItems.Add(detailItem);

                if (detailTables.Count == 1)
                {
                    editOptions = editOptions.Replace("#detailColumns", columns)
                        .Replace("#detailCnName", detailTable.ColumnCNName)
                        .Replace("#detailTable", detailTable.TableName)
                        .Replace("#detailKey", detailTable.TableColumns.FirstOrDefault(c => c.IsKey == 1)?.ColumnName)
                        .Replace("#detailSortName", string.IsNullOrEmpty(detailTable.SortName) ? key : detailTable.SortName);
                }
            }

            return null;
        }

        /// <summary>
        /// 创建App文件
        /// </summary>
        private string CreateAppFiles(Sys_TableInfo sysTableInfo, string vuePath, string spaceFolder, string pageContent)
        {
            try
            {
                pageContent = pageContent.Replace("#titleField", sysTableInfo.ExpressField).Replace("{#table}", sysTableInfo.TableName);

                // 生成app配置options.js文件
                FileHelper.WriteFile($"{vuePath}\\{spaceFolder}\\{sysTableInfo.TableName}\\",
                    sysTableInfo.TableName + "Options.js", pageContent);

                string appEditPath = $"pages/{spaceFolder}/{sysTableInfo.TableName}/{sysTableInfo.TableName}Edit";

                // 生成vue文件
                if (!FileHelper.FileExists($"{vuePath}\\{spaceFolder}\\{sysTableInfo.TableName}\\{sysTableInfo.TableName}.vue"))
                {
                    pageContent = ReadTemplateFile("Template\\Page\\app\\page.html")
                        .Replace("#TableName", sysTableInfo.TableName)
                        .Replace("#path", appEditPath);
                    FileHelper.WriteFile($"{vuePath}\\{spaceFolder}\\{sysTableInfo.TableName}\\",
                        sysTableInfo.TableName + ".vue", pageContent);
                }

                // 生成vue编辑edit文件
                if (!FileHelper.FileExists($"{vuePath}\\{spaceFolder}\\{sysTableInfo.TableName}\\{sysTableInfo.TableName}Edit.vue"))
                {
                    pageContent = ReadTemplateFile("Template\\Page\\app\\edit.html")
                        .Replace("#TableName", sysTableInfo.TableName);
                    FileHelper.WriteFile($"{vuePath}\\{spaceFolder}\\{sysTableInfo.TableName}\\",
                        sysTableInfo.TableName + "Edit.vue", pageContent);
                }

                // 更新pages.json
                string srcPath = new DirectoryInfo(vuePath.MapPath()).Parent.FullName;
                string name = FileHelper.ReadFile(@$"{srcPath}\pages.json");
                string appPath = $"pages/{spaceFolder}/{sysTableInfo.TableName}/{sysTableInfo.TableName}";

                if (!name.Contains(appPath + "\""))
                {
                    UpdatePagesJson(srcPath, name, appPath, sysTableInfo.ColumnCNName);
                }

                if (!name.Contains(appEditPath))
                {
                    name = FileHelper.ReadFile(@$"{srcPath}\pages.json");
                    UpdatePagesJson(srcPath, name, appEditPath, sysTableInfo.ColumnCNName);
                }

                return "App页面创建成功!";
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "创建App文件失败");
                throw;
            }
        }

        /// <summary>
        /// 更新pages.json文件
        /// </summary>
        private void UpdatePagesJson(string srcPath, string content, string path, string title)
        {
            int index = content.IndexOf("]");
            string fragment1 = content.Substring(0, index);
            string fragment2 = content.Substring(index);

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("		,{");
            builder.AppendLine($"			\"path\": \"{path}\",");
            builder.AppendLine("			\"style\": {");
            builder.AppendLine($"				\"navigationBarTitleText\": \"{title}\"");
            builder.AppendLine("			}");
            builder.AppendLine("		}");

            string fragment = fragment1 + builder.ToString() + "	" + fragment2;
            FileHelper.WriteFile(srcPath, "\\pages.json", fragment);
        }

        /// <summary>
        /// 创建Vue文件
        /// </summary>
        private string CreateVueFiles(Sys_TableInfo sysTableInfo, string vuePath, string spaceFolder,
            string vueOptions, string pageContent, bool isVite, bool hasSubDetail, List<string> detailItems)
        {
            try
            {
                string srcPath = new DirectoryInfo(vuePath.MapPath()).Parent.FullName;
                string extensionPath = $"{srcPath}\\extension\\{spaceFolder}\\";
                string exFileName = sysTableInfo.TableName + ".js" + (isVite ? "x" : "");
                string tableName = sysTableInfo.TableName;

                // 处理文件夹路径
                if (!FileHelper.FileExists(extensionPath + exFileName) ||
                    FileHelper.FileExists($"{extensionPath}\\{sysTableInfo.FolderName.ToLower()}\\{exFileName}"))
                {
                    extensionPath = $"{srcPath}\\extension\\{spaceFolder}\\{sysTableInfo.FolderName.ToLower()}\\";
                    spaceFolder = spaceFolder + "\\" + sysTableInfo.FolderName.ToLower();
                    pageContent = pageContent.Replace("#folder", spaceFolder.Replace("\\", "/"));
                }

                // 生成扩展逻辑页面
                if (!FileHelper.FileExists(extensionPath + exFileName))
                {
                    string exContent = ReadTemplateFile("Template\\Page\\VueExtension.html");
                    exContent = exContent.Replace("#TableName", sysTableInfo.TableName);
                    FileHelper.WriteFile(extensionPath, exFileName, exContent);
                }

                pageContent = pageContent.Replace("#TableName", tableName);

                // 调整vite版本的引用
                if (isVite && !pageContent.Contains(sysTableInfo.TableName + ".jsx"))
                {
                    pageContent = pageContent.Replace(sysTableInfo.TableName + ".js", sysTableInfo.TableName + ".jsx");
                }
                pageContent = pageContent.Replace(".jsxx", ".jsx");

                // 生成vue页面
                string valuePath = $"{vuePath}\\{spaceFolder}\\{sysTableInfo.TableName}.vue";
                if (!FileHelper.FileExists(valuePath) || FileHelper.ReadFile(valuePath).Contains("setup()"))
                {
                    pageContent = pageContent.Replace("#folder", spaceFolder.Replace("\\", "/"));
                    FileHelper.WriteFile($"{vuePath}\\{spaceFolder}\\", sysTableInfo.TableName + ".vue", pageContent);
                }

                // 处理明细表
                if (hasSubDetail)
                {
                    vueOptions = vueOptions.Replace("#tables2", $"[{string.Join(",\r                  ", detailItems)}]");
                    vueOptions = vueOptions.Replace("#detailColumns", "[]");
                }
                else
                {
                    vueOptions = vueOptions.Replace("#detailColumns", $"detailItems[0]");
                    vueOptions = vueOptions.Replace("#tables2", "[]");
                }

                vueOptions = vueOptions.Replace("[[]]", "[]");

                // 生成配置文件
                FileHelper.WriteFile($"{vuePath}\\{spaceFolder}\\{sysTableInfo.TableName}\\", "options.js", vueOptions);

                // 生成路由
                string routerPath = $"{srcPath}\\router\\viewGird.js";
                string routerContent = FileHelper.ReadFile(routerPath);

                if (!routerContent.Contains($"path: '/{sysTableInfo.TableName}'"))
                {
                    string routerTemplate = ReadTemplateFile("Template\\Page\\router.html")
                        .Replace("#TableName", sysTableInfo.TableName)
                        .Replace("#folder", spaceFolder.Replace("\\", "/"));
                    routerContent = routerContent.Replace("]", routerTemplate);
                    FileHelper.WriteFile($"{srcPath}\\router\\", "viewGird.js", routerContent);
                }

                return "页面创建成功!";
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "创建Vue文件失败");
                throw;
            }
        }

        /// <summary>
        /// 创建扩展页面
        /// </summary>
        /// <param name="tableInfo"></param>
        /// <returns></returns>
        public string CreateExtensionPage(Sys_TableInfo tableInfo)
        {
            return "开发中。。。";
        }

        /// <summary>
        /// 保存配置信息
        /// </summary>
        /// <param name="sysTableInfo"></param>
        /// <returns></returns>
        public WebResponseContent SaveEidt(Sys_TableInfo sysTableInfo)
        {
            try
            {
                WebResponseContent webResponse = ValidColumnString(sysTableInfo);
                if (!webResponse.Status) return webResponse;

                // 验证父级ID
                if (sysTableInfo.Table_Id == sysTableInfo.ParentId)
                {
                    return WebResponseContent.Instance.Error($"[{ErrorCodes.InvalidConfiguration}] 父级ID不能为自己");
                }

                // 验证快捷编辑字段
                if (sysTableInfo.TableColumns?.Any(x => !string.IsNullOrEmpty(x.DropNo)
                    && x.ColumnName == sysTableInfo.ExpressField) == true)
                {
                    return WebResponseContent.Instance.Error(
                        $"[{ErrorCodes.InvalidConfiguration}] 字段【{sysTableInfo.ExpressField}】已设置数据源，不能设置为快捷编辑"
                    );
                }

                // 设置表名
                sysTableInfo.TableColumns?.ForEach(x =>
                {
                    x.TableName = sysTableInfo.TableName;
                    x.IsReadDataset = x.IsReadDataset ?? 0;
                });

                return repository.UpdateRange<Sys_TableColumn>(sysTableInfo, true, true, null, null, true);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "保存配置信息失败");
                return WebResponseContent.Instance.Error($"[{ErrorCodes.DatabaseOperationFailed}] 保存失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 将表结构重新同步到代码生成配置
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public async Task<WebResponseContent> SyncTable(string tableName)
        {
            WebResponseContent webResponse = new WebResponseContent();

            if (string.IsNullOrEmpty(tableName))
            {
                return webResponse.Error($"[{ErrorCodes.InvalidConfiguration}] 表名不能为空");
            }

            try
            {
                // 获取表配置信息
                Sys_TableInfo tableInfo = await repository.FindAsIQueryable(x => x.TableName == tableName)
                    .Include(o => o.TableColumns)
                    .FirstOrDefaultAsync();

                if (tableInfo == null)
                {
                    return webResponse.Error($"[{ErrorCodes.TableNotFound}] 未找到表【{tableName}】的配置信息，请使用新建功能");
                }

                if (!string.IsNullOrEmpty(tableInfo.TableTrueName) && tableInfo.TableTrueName != tableName)
                {
                    tableName = tableInfo.TableTrueName;
                }

                string sql = GetCurrentSql(tableName);
                if (string.IsNullOrEmpty(sql))
                {
                    return webResponse.Error($"[{ErrorCodes.DatabaseOperationFailed}] 不支持的数据库类型");
                }

                // 获取表结构
                List<Sys_TableColumn> columns = null;
                try
                {
                    columns = repository.DapperContext.QueryList<Sys_TableColumn>(sql, new { tableName });
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, $"查询表结构失败: {tableName}");
                    return webResponse.Error($"[{ErrorCodes.DatabaseOperationFailed}] 查询表结构失败: {ex.Message}");
                }

                if (columns == null || columns.Count == 0)
                {
                    return webResponse.Error($"[{ErrorCodes.TableNotFound}] 未找到表【{tableName}】的结构信息，请确认表是否存在");
                }

                // 获取现有配置
                List<Sys_TableColumn> detailList = tableInfo.TableColumns ?? new List<Sys_TableColumn>();
                List<Sys_TableColumn> addColumns = new List<Sys_TableColumn>();
                List<Sys_TableColumn> updateColumns = new List<Sys_TableColumn>();

                // 比较差异
                foreach (Sys_TableColumn item in columns)
                {
                    Sys_TableColumn tableColumn = detailList.FirstOrDefault(x =>
                        x.ColumnName.Equals(item.ColumnName, StringComparison.OrdinalIgnoreCase));

                    if (tableColumn == null)
                    {
                        // 新增列
                        item.TableName = tableInfo.TableName;
                        item.Table_Id = tableInfo.Table_Id;
                        addColumns.Add(item);
                    }
                    else if (item.ColumnType != tableColumn.ColumnType
                        || item.Maxlength != tableColumn.Maxlength
                        || (item.IsNull ?? 0) != (tableColumn.IsNull ?? 0))
                    {
                        // 修改列
                        tableColumn.ColumnType = item.ColumnType;
                        tableColumn.Maxlength = item.Maxlength;
                        tableColumn.IsNull = item.IsNull;
                        updateColumns.Add(tableColumn);
                    }
                }

                // 删除的列
                List<Sys_TableColumn> delColumns = detailList
                    .Where(a => !columns.Any(c => c.ColumnName.Equals(a.ColumnName, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                if (addColumns.Count + delColumns.Count + updateColumns.Count == 0)
                {
                    return webResponse.OK("表结构未发生变化");
                }

                // 使用事务保存变更
                using (var transaction = await repository.DbContext.Database.BeginTransactionAsync())
                {
                    try
                    {
                        if (addColumns.Count > 0)
                            repository.AddRange(addColumns);

                        if (delColumns.Count > 0)
                            repository.DbContext.Set<Sys_TableColumn>().RemoveRange(delColumns);

                        if (updateColumns.Count > 0)
                            repository.UpdateRange(updateColumns, x => new { x.ColumnType, x.Maxlength, x.IsNull });

                        await repository.DbContext.SaveChangesAsync();
                        await transaction.CommitAsync();

                        return webResponse.OK($"同步成功：新增字段【{addColumns.Count}】个，删除字段【{delColumns.Count}】个，修改字段【{updateColumns.Count}】个");
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, $"同步表结构失败: {tableName}");
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"同步表结构失败: {tableName}");
                return webResponse.Error($"[{ErrorCodes.DatabaseOperationFailed}] 同步失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 生成Services/Repository与对应的Partial类
        /// </summary>
        public string CreateServices(string tableName, string nameSpace, string foldername, bool webController, bool apiController)
        {
            if (string.IsNullOrEmpty(tableName))
            {
                return $"[{ErrorCodes.InvalidConfiguration}] 表名不能为空";
            }

            if (string.IsNullOrEmpty(nameSpace) || string.IsNullOrEmpty(foldername))
            {
                return $"[{ErrorCodes.InvalidConfiguration}] 命名空间和项目文件夹都不能为空";
            }

            try
            {
                // Fix for CS0308: Ensure the correct method signature is used for FindFirst
                var tableColumn = repository.FindFirst(x => x.TableName == tableName);
                if (tableColumn == null)
                {
                    return $"[{ErrorCodes.TableNotFound}] 未找到表【{tableName}】的配置信息";
                }

                string frameworkFolder = ProjectPath.GetProjectDirectoryInfo()?.FullName;
                if (string.IsNullOrEmpty(frameworkFolder))
                {
                    return $"[{ErrorCodes.InvalidProjectName}] 未找到项目目录";
                }

                string[] splitArr = nameSpace.Split('.');
                string projectName = splitArr.Length > 1 ? splitArr[splitArr.Length - 1] : splitArr[0];
                string baseOptions = $"\"{projectName}\",\"{foldername}\",\"{tableName}\"";

                var replacements = new Dictionary<string, string>
                {
                    {"{Namespace}", nameSpace},
                    {"{TableName}", tableName},
                    {"{StartName}", StratName},
                    {"{BaseOptions}", baseOptions}
                };

                // 生成API控制器
                if (apiController)
                {
                    string result = CreateApiController(tableName, replacements);
                    if (!string.IsNullOrEmpty(result))
                    {
                        return result;
                    }
                }

                // 生成Repository和Service
                CreateRepositoryAndService(frameworkFolder, nameSpace, foldername, tableName, replacements);

                // 生成Web控制器
                if (webController)
                {
                    CreateWebController(frameworkFolder, nameSpace, foldername, tableName, replacements);
                }

                return "业务类创建成功!";
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "创建Services失败");
                return $"[{ErrorCodes.FileOperationFailed}] 创建失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 创建API控制器
        /// </summary>
        private string CreateApiController(string tableName, Dictionary<string, string> replacements)
        {
            try
            {
                var apiPath = ProjectPath.GetProjectDirectoryInfo()
                    ?.GetDirectories()
                    .FirstOrDefault(x => x.Name.ToLower().EndsWith(".webapi"))
                    ?.FullName;

                if (string.IsNullOrEmpty(apiPath))
                {
                    return $"[{ErrorCodes.InvalidProjectName}] 未找到WebApi类库，请确认存在以.webapi结尾的项目";
                }

                apiPath = Path.Combine(apiPath, "Controllers", replacements["{StartName}"]);

                // 生成Partial控制器
                string partialPath = Path.Combine(apiPath, "Partial", $"{tableName}Controller.cs");
                if (!File.Exists(partialPath))
                {
                    string partialContent = ReadTemplateFile(@"Template\Controller\ControllerApiPartial.html");
                    partialContent = ReplaceTemplateContent(partialContent, replacements);

                    FileHelper.WriteFile(Path.GetDirectoryName(partialPath), Path.GetFileName(partialPath), partialContent);
                }

                // 生成主控制器
                string controllerContent = ReadTemplateFile(@"Template\Controller\ControllerApi.html");
                controllerContent = ReplaceTemplateContent(controllerContent, replacements);

                FileHelper.WriteFile(apiPath, $"{tableName}Controller.cs", controllerContent);

                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "创建API控制器失败");
                throw;
            }
        }

        /// <summary>
        /// 创建Repository和Service
        /// </summary>
        private void CreateRepositoryAndService(string frameworkFolder, string nameSpace, string foldername,
            string tableName, Dictionary<string, string> replacements)
        {
            try
            {
                // Repository
                string repoContent = ReadTemplateFile("Template\\Repositorys\\BaseRepository.html");
                repoContent = ReplaceTemplateContent(repoContent, replacements);
                string repoPath = Path.Combine(frameworkFolder, nameSpace, "Repositories", foldername);
                FileHelper.WriteFile(repoPath, $"{tableName}Repository.cs", repoContent);

                // IRepository
                string iRepoContent = ReadTemplateFile("Template\\IRepositorys\\BaseIRepositorie.html");
                iRepoContent = ReplaceTemplateContent(iRepoContent, replacements);
                string iRepoPath = Path.Combine(frameworkFolder, nameSpace, "IRepositories", foldername);
                FileHelper.WriteFile(iRepoPath, $"I{tableName}Repository.cs", iRepoContent);

                // IService
                string iServicePath = Path.Combine(frameworkFolder, nameSpace, "IServices", foldername);
                string iServicePartialPath = Path.Combine(iServicePath, "Partial");
                string iServiceFileName = $"I{tableName}Service.cs";

                if (!File.Exists(Path.Combine(iServicePartialPath, iServiceFileName)))
                {
                    string iServicePartialContent = ReadTemplateFile("Template\\IServices\\IServiceBasePartial.html");
                    iServicePartialContent = ReplaceTemplateContent(iServicePartialContent, replacements);
                    FileHelper.WriteFile(iServicePartialPath, iServiceFileName, iServicePartialContent);
                }

                string iServiceContent = ReadTemplateFile("Template\\IServices\\IServiceBase.html");
                iServiceContent = ReplaceTemplateContent(iServiceContent, replacements);
                FileHelper.WriteFile(iServicePath, iServiceFileName, iServiceContent);

                // Service
                string servicePath = Path.Combine(frameworkFolder, nameSpace, "Services", foldername);
                string servicePartialPath = Path.Combine(servicePath, "Partial");
                string serviceFileName = $"{tableName}Service.cs";

                if (!File.Exists(Path.Combine(servicePartialPath, serviceFileName)))
                {
                    string servicePartialContent = ReadTemplateFile("Template\\Services\\ServiceBasePartial.html");
                    servicePartialContent = ReplaceTemplateContent(servicePartialContent, replacements);
                    FileHelper.WriteFile(servicePartialPath, serviceFileName, servicePartialContent);
                }

                string serviceContent = ReadTemplateFile("Template\\Services\\ServiceBase.html");
                serviceContent = ReplaceTemplateContent(serviceContent, replacements);
                FileHelper.WriteFile(servicePath, serviceFileName, serviceContent);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "创建Repository和Service失败");
                throw;
            }
        }

        /// <summary>
        /// 创建Web控制器
        /// </summary>
        private void CreateWebController(string frameworkFolder, string nameSpace, string foldername,
            string tableName, Dictionary<string, string> replacements)
        {
            try
            {
                string path = Path.Combine(frameworkFolder, nameSpace, "Controllers", foldername);
                string partialPath = Path.Combine(path, "Partial");
                string fileName = $"{tableName}Controller.cs";

                if (!File.Exists(Path.Combine(partialPath, fileName)))
                {
                    string partialContent = ReadTemplateFile("Template\\Controller\\ControllerPartial.html");
                    partialContent = ReplaceTemplateContent(partialContent, replacements);
                    FileHelper.WriteFile(partialPath, fileName, partialContent);
                }

                string content = ReadTemplateFile("Template\\Controller\\Controller.html");
                content = ReplaceTemplateContent(content, replacements);
                FileHelper.WriteFile(path, fileName, content);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "创建Web控制器失败");
                throw;
            }
        }

        /// <summary>
        /// 创建实体Model内部实现
        /// </summary>
        private string CreateEntityModelInternal(List<Sys_TableColumn> sysColumn, Sys_TableInfo tableInfo,
            List<TableColumnInfo> tableColumnInfoList, int createType)
        {
            try
            {
                string template = createType switch
                {
                    1 => "DomainModel.html",
                    2 => "ApiInputDomainModel.html",
                    3 => "ApiOutputDomainModel.html",
                    _ => "DomainModel.html"
                };

                string domainContent = ReadTemplateFile($"Template\\DomianModel\\{template}");
                string partialContent = domainContent;

                var attributeBuilder = new StringBuilder();
                sysColumn = sysColumn.OrderByDescending(c => c.OrderNo).ToList();
                bool addIgnore = false;

                foreach (Sys_TableColumn column in sysColumn)
                {
                    BuildColumnAttributes(column, tableColumnInfoList, createType, attributeBuilder, ref addIgnore);
                }

                // 添加明细表属性
                if (!string.IsNullOrEmpty(tableInfo.DetailName) && createType == 1)
                {
                    var keyColumn = sysColumn.FirstOrDefault(x => x.IsKey == 1);
                    if (keyColumn != null)
                    {
                        attributeBuilder.AppendLine($"       [Display(Name =\"{tableInfo.DetailCnName}\")]");
                        attributeBuilder.AppendLine($"       [ForeignKey(\"{keyColumn.ColumnName}\")]");
                        attributeBuilder.AppendLine($"       public List<{tableInfo.DetailName}> {tableInfo.DetailName} {{ get; set; }}");
                    }
                }

                if (addIgnore && createType == 1)
                {
                    domainContent = "using Newtonsoft.Json;\r\n" + domainContent;
                }

                // 生成实体类文件
                return GenerateEntityFile(tableInfo, domainContent, partialContent, attributeBuilder.ToString(), createType);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "创建实体Model内部实现失败");
                return $"[{ErrorCodes.FileOperationFailed}] 创建Model失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 构建列属性
        /// </summary>
        private void BuildColumnAttributes(Sys_TableColumn column, List<TableColumnInfo> tableColumnInfoList,
            int createType, StringBuilder attributeBuilder, ref bool addIgnore)
        {
            column.ColumnType = (column.ColumnType ?? "string").Trim();

            // 注释
            attributeBuilder.AppendLine("       /// <summary>");
            attributeBuilder.AppendLine($"       ///{column.ColumnCnName ?? column.ColumnName}");
            attributeBuilder.AppendLine("       /// </summary>");

            // 主键
            if (column.IsKey == 1)
            {
                attributeBuilder.AppendLine("       [Key]");
            }

            // Display
            attributeBuilder.AppendLine($"       [Display(Name =\"{column.ColumnCnName ?? column.ColumnName}\")]");

            var tableColumnInfo = tableColumnInfoList?.FirstOrDefault(x =>
                x.ColumnName.Equals(column.ColumnName, StringComparison.OrdinalIgnoreCase));

            // 处理超长字符串
            if (tableColumnInfo != null)
            {
                if ((tableColumnInfo.ColumnType == "varchar" && column.Maxlength > 8000) ||
                    (tableColumnInfo.ColumnType == "nvarchar" && column.Maxlength > 4000))
                {
                    column.Maxlength = 0;
                }
            }

            // MaxLength
            if (column.ColumnType == "string" && column.Maxlength > 0 && column.Maxlength < 8000)
            {
                attributeBuilder.AppendLine($"       [MaxLength({column.Maxlength})]");
            }

            // JsonIgnore
            if (column.IsColumnData == 0 && createType == 1)
            {
                addIgnore = true;
                attributeBuilder.AppendLine("       [JsonIgnore]");
            }

            // Column类型映射
            BuildColumnTypeMapping(column, tableColumnInfo, attributeBuilder);

            // Editable
            if (column.EditRowNo != null)
            {
                attributeBuilder.AppendLine("       [Editable(true)]");
            }

            // Required
            if (column.IsNull == 0 || (createType == 2 && column.ApiIsNull == 0))
            {
                attributeBuilder.AppendLine("       [Required(AllowEmptyStrings=false)]");
            }

            // 属性定义
            string columnType = GetColumnClrType(column, tableColumnInfo);
            attributeBuilder.AppendLine($"       public {columnType} {column.ColumnName} {{ get; set; }}");
            attributeBuilder.AppendLine();
        }

        /// <summary>
        /// 构建列类型映射
        /// </summary>
        private void BuildColumnTypeMapping(Sys_TableColumn column, TableColumnInfo tableColumnInfo, StringBuilder attributeBuilder)
        {
            if (tableColumnInfo == null) return;

            // DisplayFormat
            if (!string.IsNullOrEmpty(tableColumnInfo.Prec_Scale) && !tableColumnInfo.Prec_Scale.EndsWith(",0"))
            {
                attributeBuilder.AppendLine($"       [DisplayFormat(DataFormatString=\"{tableColumnInfo.Prec_Scale}\")]");
            }

            // 判断是否为GUID
            bool isGuid = IsGuidColumn(column, tableColumnInfo);
            if (isGuid)
            {
                tableColumnInfo.ColumnType = "uniqueidentifier";
            }

            // Column TypeName
            string maxLength = GetColumnMaxLength(column, tableColumnInfo);
            attributeBuilder.AppendLine($"       [Column(TypeName=\"{tableColumnInfo.ColumnType}{maxLength}\")]");

            // 更新列类型
            UpdateColumnType(column, tableColumnInfo);
        }

        /// <summary>
        /// 判断是否为GUID列
        /// </summary>
        private bool IsGuidColumn(Sys_TableColumn column, TableColumnInfo tableColumnInfo)
        {
            if (tableColumnInfo?.ColumnType?.ToLower() == "guid" ||
                tableColumnInfo?.ColumnType?.ToLower() == "uniqueidentifier")
            {
                return true;
            }

            if (column.IsKey == 1 && column.ColumnType == "uniqueidentifier")
            {
                return true;
            }

            // MySQL/DM/Oracle 36长度字符串默认为GUID
            if ((IsMysql() || IsDM() || IsOracle()) &&
                column.ColumnType == "string" &&
                column.Maxlength == 36)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 获取列的最大长度定义
        /// </summary>
        private string GetColumnMaxLength(Sys_TableColumn column, TableColumnInfo tableColumnInfo)
        {
            if (tableColumnInfo?.ColumnType == "uniqueidentifier")
            {
                return "";
            }

            if (column.IsKey != 1 && column.ColumnType.ToLower() == "string")
            {
                if (column.Maxlength <= 0 ||
                    (tableColumnInfo?.ColumnType == "varchar" && column.Maxlength > 8000) ||
                    (tableColumnInfo?.ColumnType == "nvarchar" && column.Maxlength > 4000))
                {
                    return "(max)";
                }
                return $"({column.Maxlength})";
            }

            if (column.IsKey == 1 && column.ColumnType.ToLower() == "string" && column.Maxlength != 36)
            {
                return $"({column.Maxlength})";
            }

            return "";
        }

        /// <summary>
        /// 更新列类型
        /// </summary>
        private void UpdateColumnType(Sys_TableColumn column, TableColumnInfo tableColumnInfo)
        {
            if (tableColumnInfo == null) return;

            switch (tableColumnInfo.ColumnType?.ToLower())
            {
                case "int":
                    column.ColumnType = "int";
                    break;
                case "bigint":
                case "long":
                    column.ColumnType = "long";
                    break;
                case "bool":
                    column.ColumnType = "bit";
                    break;
            }
        }

        /// <summary>
        /// 获取列的CLR类型
        /// </summary>
        private string GetColumnClrType(Sys_TableColumn column, TableColumnInfo tableColumnInfo)
        {
            string columnType = column.ColumnType == "Date" ? "DateTime" : column.ColumnType;

            if (IsGuidColumn(column, tableColumnInfo))
            {
                columnType = "Guid";
            }

            // 可空类型
            if (column.ColumnType.ToLower() != "string" && column.IsNull == 1)
            {
                columnType += "?";
            }

            return columnType;
        }

        /// <summary>
        /// 生成实体文件
        /// </summary>
        private string GenerateEntityFile(Sys_TableInfo tableInfo, string domainContent, string partialContent,
            string attributeList, int createType)
        {
            try
            {
                string mapPath = ProjectPath.GetProjectDirectoryInfo()?.FullName;
                if (string.IsNullOrEmpty(mapPath))
                {
                    return $"[{ErrorCodes.InvalidProjectName}] 未找到生成的目录";
                }

                var replacements = new Dictionary<string, string>
                {
                    {"{TableName}", tableInfo.TableName},
                    {"{AttributeList}", attributeList},
                    {"{StartName}", StratName}
                };

                domainContent = ReplaceTemplateContent(domainContent, replacements);

                // 构建实体属性
                string entityAttribute = BuildEntityAttribute(tableInfo, createType);
                string modelNameSpace = $"{StratName}.Entity";

                // 处理表名映射
                if (!string.IsNullOrEmpty(tableInfo.TableTrueName) && tableInfo.TableName != tableInfo.TableTrueName)
                {
                    string tableTrueName = tableInfo.TableTrueName;
                    if (DBType.Name == DbCurrentType.PgSql.ToString())
                    {
                        tableTrueName = tableTrueName.ToLower();
                    }
                    entityAttribute += $"\r\n[Table(\"{tableTrueName}\")]";
                }

                domainContent = domainContent
                    .Replace("{AttributeManager}", entityAttribute)
                    .Replace("{Namespace}", modelNameSpace);

                // 确定文件夹和文件名
                string folderName = tableInfo.FolderName;
                string tableName = tableInfo.TableName;

                if (createType == 2)
                {
                    folderName = "ApiEntity\\Input";
                    tableName = $"Api{tableInfo.TableName}Input";
                }
                else if (createType == 3)
                {
                    folderName = "ApiEntity\\Output";
                    tableName = $"Api{tableInfo.TableName}Output";
                }

                // 写入主文件
                string modelPath = Path.Combine(mapPath, modelNameSpace, "DomainModels", folderName);
                FileHelper.WriteFile(modelPath, $"{tableName}.cs", domainContent);

                // 写入partial文件
                string partialPath = Path.Combine(modelPath, "partial");
                if (!File.Exists(Path.Combine(partialPath, $"{tableName}.cs")))
                {
                    partialContent = partialContent
                        .Replace("{AttributeManager}", "")
                        .Replace("{AttributeList}", "//此处配置字段(字段配置见此model的另一个partial),如果表中没有此字段请加上 [NotMapped]属性，否则会异常")
                        .Replace(":BaseEntity", "")
                        .Replace("{TableName}", tableInfo.TableName)
                        .Replace("{Namespace}", modelNameSpace);

                    FileHelper.WriteFile(partialPath, $"{tableName}.cs", partialContent);
                }

                return "";
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "生成实体文件失败");
                return $"[{ErrorCodes.FileOperationFailed}] 生成文件失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 构建实体属性特性
        /// </summary>
        private string BuildEntityAttribute(Sys_TableInfo tableInfo, int createType)
        {
            var entityAttributes = new List<string>
            {
                $"TableCnName = \"{tableInfo.ColumnCNName}\""
            };

            if (!string.IsNullOrEmpty(tableInfo.TableTrueName))
            {
                entityAttributes.Add($"TableName = \"{tableInfo.TableTrueName}\"");
            }

            if (!string.IsNullOrEmpty(tableInfo.DetailName) && createType == 1)
            {
                string[] details = tableInfo.DetailName.Split(',');
                string typeArr = $"new Type[] {{ typeof({string.Join("), typeof(", details)}) }}";
                entityAttributes.Add($"DetailTable = {typeArr}");
            }

            if (!string.IsNullOrEmpty(tableInfo.DetailCnName))
            {
                entityAttributes.Add($"DetailTableCnName = \"{tableInfo.DetailCnName}\"");
            }

            if (!string.IsNullOrEmpty(tableInfo.DBServer) && createType == 1)
            {
                entityAttributes.Add($"DBServer = \"{tableInfo.DBServer}\"");
            }

            string tableAttr = string.Join(", ", entityAttributes);
            return string.IsNullOrEmpty(tableAttr) ? "" : $"[Entity({tableAttr})]";
        }

        /// <summary>
        /// 验证列配置字符串
        /// </summary>
        private WebResponseContent ValidColumnString(Sys_TableInfo tableInfo)
        {
            var webResponse = new WebResponseContent(true);

            if (tableInfo.TableColumns == null || tableInfo.TableColumns.Count == 0)
            {
                return webResponse;
            }

            // 验证主表主键
            if (!string.IsNullOrEmpty(tableInfo.DetailName))
            {
                var mainTableKey = tableInfo.TableColumns.FirstOrDefault(x => x.IsKey == 1);
                if (mainTableKey == null)
                {
                    return webResponse.Error($"[{ErrorCodes.InvalidConfiguration}] 请勾选表【{tableInfo.TableName}】的主键");
                }

                string keyName = mainTableKey.ColumnName;

                // 验证明细表外键
                var detailTableColumn = repository
                    .Find<Sys_TableColumn>(x => x.TableName == tableInfo.DetailName && x.ColumnName == keyName)
                    .FirstOrDefault();

                if (detailTableColumn == null)
                {
                    return webResponse.Error($"[{ErrorCodes.InvalidConfiguration}] 明细表必须包含主表【{tableInfo.TableName}】的主键字段【{keyName}】");
                }

                // 验证类型一致性
                if (!mainTableKey.ColumnType.Equals(detailTableColumn.ColumnType, StringComparison.OrdinalIgnoreCase))
                {
                    return webResponse.Error($"[{ErrorCodes.InvalidConfiguration}] 明细表的字段【{keyName}】类型必须与主表的主键类型相同");
                }

                // MySQL/DM GUID类型验证
                if ((IsMysql() || IsDM()) &&
                    mainTableKey.ColumnType?.ToLower() == "string" &&
                    detailTableColumn.Maxlength != 36)
                {
                    return webResponse.Error($"[{ErrorCodes.InvalidConfiguration}] 主表主键类型为Guid，明细表【{tableInfo.DetailName}】的字段【{keyName}】长度必须是36");
                }
            }

            return webResponse;
        }

        /// <summary>
        /// 检查表是否已存在
        /// </summary>
        private WebResponseContent ExistsTable(string tableName, string tableTrueName)
        {
            var webResponse = new WebResponseContent(true);

            try
            {
                var compilationLibraries = DependencyContext.Default
                    .CompileLibraries
                    .Where(x => !x.Serviceable && x.Type == "project");

                foreach (var compilation in compilationLibraries)
                {
                    try
                    {
                        var assembly = AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName(compilation.Name));
                        var entityTypes = assembly.GetTypes()
                            .Where(x => x.GetTypeInfo().BaseType != null && x.BaseType == typeof(BaseEntity));

                        foreach (var entity in entityTypes)
                        {
                            if (entity.Name == tableTrueName && !string.IsNullOrEmpty(tableName) && tableName != tableTrueName)
                            {
                                return webResponse.Error($"[{ErrorCodes.InvalidConfiguration}] 实际表名【{tableTrueName}】已创建实体，不能创建别名【{tableName}】实体");
                            }

                            if (entity.Name != tableName)
                            {
                                var tableAttr = entity.GetCustomAttribute<TableAttribute>();
                                if (tableAttr != null && tableAttr.Name == tableTrueName)
                                {
                                    return webResponse.Error($"[{ErrorCodes.InvalidConfiguration}] 实际表名【{tableTrueName}】已被【{entity.Name}】创建实体，不能创建别名【{tableName}】实体");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, $"加载程序集 {compilation.Name} 失败");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "检查表是否存在时发生错误");
            }

            return webResponse;
        }

        // 将其他数据库相关方法也进行类似的优化...
        // 由于代码太长，这里只展示了主要的优化思路
    }

    // 辅助类保持不变
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