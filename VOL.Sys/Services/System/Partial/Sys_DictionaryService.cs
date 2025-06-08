using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VOL.Core.BaseProvider;
using VOL.Core.Const;
using VOL.Core.Enums;
using VOL.Core.Extensions;
using VOL.Core.Infrastructure;
using VOL.Core.Utilities;
using VOL.Entity.DomainModels;
using Serilog;
using Log = Serilog.Log;

namespace VOL.Sys.Services
{
    public partial class Sys_DictionaryService
    {
        public Sys_DictionaryService() {
            Log.ForContext<Sys_DictionaryService>();
        }
        
        /// <summary>
        /// SQL关键字黑名单
        /// </summary>
        private static readonly HashSet<string> SqlBlackList = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "insert", "update", "delete", "drop", "truncate", "declare",
            "xp_cmdshell", "exec", "execute", "net user"
        };

        protected override void Init(IRepository<Sys_Dictionary> repository)
        {
        }

        /// <summary>
        /// 代码生成器获取所有字典项编号(超级管理权限)
        /// </summary>
        /// <returns></returns>
        public async Task<List<string>> GetBuilderDictionary()
        {
            try
            {
                var result = await repository.FindAsync(x => 1 == 1, s => s.DicNo);
                return result ?? new List<string>();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"获取字典编号失败: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// 获取所有字典缓存
        /// </summary>
        public List<Sys_Dictionary> Dictionaries
        {
            get { return DictionaryManager.Dictionaries ?? new List<Sys_Dictionary>(); }
        }

        /// <summary>
        /// 获取Vue字典数据
        /// </summary>
        public object GetVueDictionary(string[] dicNos)
        {
            if (dicNos == null || dicNos.Length == 0)
            {
                return new object[] { };
            }

            try
            {
                // 过滤无效的字典编号
                dicNos = dicNos.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToArray();
                if (dicNos.Length == 0)
                {
                    return new object[] { };
                }

                var dicConfig = DictionaryManager.GetDictionaries(dicNos, false)
                    .Select(s => new
                    {
                        dicNo = s.DicNo,
                        config = s.Config,
                        dbSql = s.DbSql,
                        list = (s.Sys_DictionaryList ?? new List<Sys_DictionaryList>())
                                  .OrderByDescending(o => o.OrderNo)
                                  .Select(item => new { key = item.DicValue, value = item.DicName })
                    }).ToList();

                return dicConfig.Select(item => new
                {
                    item.dicNo,
                    item.config,
                    data = GetSourceData(item.dicNo, item.dbSql, item.list)
                }).ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex ,$"获取Vue字典失败: {ex.Message}");
                return new object[] { };
            }
        }

        /// <summary>
        /// 获取数据源数据
        /// </summary>
        private object GetSourceData(string dicNo, string dbSql, object data)
        {
            try
            {
                // 2020.05.01增加根据用户信息加载字典数据源sql
                dbSql = DictionaryHandler.GetCustomDBSql(dicNo, dbSql);
                if (string.IsNullOrEmpty(dbSql))
                {
                    return data ?? new object[] { };
                }

                // 验证SQL安全性
                if (!IsSqlSafe(dbSql))
                {
                    Log.Error( $"字典SQL包含危险关键字: DicNo={dicNo}, SQL={dbSql}");
                    return data ?? new object[] { };
                }

                return repository.DapperContext.QueryList<object>(dbSql, null) ?? new List<object>();
            }
            catch (Exception ex)
            {
                Log.Error( $"执行字典SQL失败: DicNo={dicNo}, {ex.Message}", ex);
                return data ?? new object[] { };
            }
        }

        /// <summary>
        /// 通过远程搜索
        /// </summary>
        /// <param name="dicNo"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public object GetSearchDictionary(string dicNo, string value)
        {
            if (string.IsNullOrWhiteSpace(dicNo) || string.IsNullOrWhiteSpace(value))
            {
                return new object[] { };
            }

            try
            {
                // 防止SQL注入，限制搜索值长度
                if (value.Length > 50)
                {
                    value = value.Substring(0, 50);
                }

                // 2020.05.01增加根据用户信息加载字典数据源sql
                var dictionary = Dictionaries.FirstOrDefault(x => x.DicNo == dicNo);
                if (dictionary == null)
                {
                    return new object[] { };
                }

                string sql = DictionaryHandler.GetCustomDBSql(dicNo, dictionary.DbSql);
                if (string.IsNullOrEmpty(sql))
                {
                    return new object[] { };
                }

                // 验证SQL安全性
                if (!IsSqlSafe(sql))
                {
                    Log.Error( $"字典SQL包含危险关键字: DicNo={dicNo}");
                    return new object[] { };
                }

                sql = $"SELECT * FROM ({sql}) AS t WHERE value LIKE @value";
                return repository.DapperContext.QueryList<object>(sql, new { value = $"%{value}%" }) ?? new List<object>();
            }
            catch (Exception ex)
            {
                Log.Error($"搜索字典失败: DicNo={dicNo}, Value={value}, {ex.Message}", ex);
                return new object[] { };
            }
        }

        /// <summary>
        /// 表单设置为远程查询，重置或第一次添加表单时，获取字典的key、value
        /// </summary>
        /// <param name="dicNo"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<object> GetRemoteDefaultKeyValue(string dicNo, string key)
        {
            // 暂时返回默认值
            return await Task.FromResult(1);

            // 以下为预留的实现代码
            /*
            if (string.IsNullOrWhiteSpace(dicNo) || string.IsNullOrWhiteSpace(key))
            {
                return null;
            }
            
            try
            {
                var dictionary = Dictionaries.FirstOrDefault(x => x.DicNo == dicNo);
                if (dictionary == null || string.IsNullOrEmpty(dictionary.DbSql))
                {
                    return null;
                }
                
                string sql = DictionaryHandler.GetCustomDBSql(dicNo, dictionary.DbSql);
                if (string.IsNullOrEmpty(sql) || !IsSqlSafe(sql))
                {
                    return null;
                }
                
                sql = $"SELECT * FROM ({sql}) AS t WHERE t.key = @key";
                return await Task.FromResult(repository.DapperContext.QueryFirst<object>(sql, new { key }));
            }
            catch (Exception ex)
            {
                Logger.Error(LoggerType.System, $"获取远程默认值失败: DicNo={dicNo}, Key={key}, {ex.Message}", ex);
                return null;
            }
            */
        }

        /// <summary>
        /// table加载数据后刷新当前table数据的字典项(适用字典数据量比较大的情况)
        /// </summary>
        /// <param name="keyData"></param>
        /// <returns></returns>
        public object GetTableDictionary(Dictionary<string, object[]> keyData)
        {
            if (keyData == null || keyData.Count == 0)
            {
                return new List<object>();
            }

            try
            {
                var dicInfo = Dictionaries
                    .Where(x => keyData.ContainsKey(x.DicNo) && !string.IsNullOrEmpty(x.DbSql))
                    .Select(x => new { x.DicNo, x.DbSql })
                    .ToList();

                if (dicInfo.Count == 0)
                {
                    return new List<object>();
                }

                List<object> list = new List<object>();

                // 根据数据库类型选择合适的SQL语法
                bool isPgSql = DBType.Name == DbCurrentType.PgSql.ToString();

                foreach (var item in dicInfo)
                {
                    if (!keyData.TryGetValue(item.DicNo, out object[] data) || data == null || data.Length == 0)
                    {
                        continue;
                    }

                    try
                    {
                        // 2020.05.01增加根据用户信息加载字典数据源sql
                        string sql = DictionaryHandler.GetCustomDBSql(item.DicNo, item.DbSql);
                        if (string.IsNullOrEmpty(sql) || !IsSqlSafe(sql))
                        {
                            continue;
                        }

                        object result;
                        if (isPgSql)
                        {
                            // PostgreSQL 特殊处理
                            sql = $"SELECT * FROM ({sql}) AS t WHERE t.key=any(@data)";
                            result = repository.DapperContext.QueryList<object>(
                                sql,
                                new { data = data.Select(s => s?.ToString() ?? "").ToList() }
                            );
                        }
                        else
                        {
                            // MySQL和SQL Server处理
                            string keySql = DBType.Name == DbCurrentType.MySql.ToString() ? "t.key" : "t.[key]";
                            sql = $"SELECT * FROM ({sql}) AS t WHERE {keySql} in @data";
                            result = repository.DapperContext.QueryList<object>(sql, new { data });
                        }

                        list.Add(new { key = item.DicNo, data = result ?? new List<object>() });
                    }
                    catch (Exception ex)
                    {
                        Log.Error( $"获取表格字典数据失败: DicNo={item.DicNo}, {ex.Message}", ex);
                        list.Add(new { key = item.DicNo, data = new List<object>() });
                    }
                }

                return list;
            }
            catch (Exception ex)
            {
                Log.Error( $"获取表格字典失败: {ex.Message}", ex);
                return new List<object>();
            }
        }

        /// <summary>
        /// 获取分页数据
        /// </summary>
        public override PageGridData<Sys_Dictionary> GetPageData(PageDataOptions pageData)
        {
            try
            {
                // 增加查询条件
                base.QueryRelativeExpression = (IQueryable<Sys_Dictionary> queryable) =>
                {
                    if (queryable == null) return queryable;
                    return queryable.Where(x => 1 == 1);
                };

                return base.GetPageData(pageData);
            }
            catch (Exception ex)
            {
                Log.Error( $"获取字典分页数据失败: {ex.Message}", ex);
                return new PageGridData<Sys_Dictionary>
                {
                    rows = new List<Sys_Dictionary>(),
                    total = 0,
                    msg = "获取数据失败"
                };
            }
        }

        /// <summary>
        /// 更新字典
        /// </summary>
        public override WebResponseContent Update(SaveModel saveDataModel)
        {
            if (saveDataModel == null || saveDataModel.MainData == null)
            {
                return new WebResponseContent().Error("参数不能为空");
            }

            try
            {
                // 如果没有字典编号或ID，则执行新增
                if (saveDataModel.MainData.DicKeyIsNullOrEmpty("DicNo")
                    || saveDataModel.MainData.DicKeyIsNullOrEmpty("Dic_ID"))
                {
                    return base.Add(saveDataModel);
                }

                // 判断修改的字典编号是否在其他ID存在
                string dicNo = saveDataModel.MainData["DicNo"]?.ToString()?.Trim();
                if (string.IsNullOrWhiteSpace(dicNo))
                {
                    return new WebResponseContent().Error("字典编号不能为空");
                }

                int dicId = saveDataModel.MainData["Dic_ID"].GetInt();
                if (repository.Exists(x => x.DicNo == dicNo && x.Dic_ID != dicId))
                {
                    return new WebResponseContent().Error($"字典编号:{dicNo}已存在!");
                }

                base.UpdateOnExecuting = (Sys_Dictionary dictionary, object addList, object editList, List<object> obj) =>
                {
                    List<Sys_DictionaryList> listObj = new List<Sys_DictionaryList>();

                    if (addList != null)
                    {
                        listObj.AddRange(addList as List<Sys_DictionaryList> ?? new List<Sys_DictionaryList>());
                    }

                    if (editList != null)
                    {
                        listObj.AddRange(editList as List<Sys_DictionaryList> ?? new List<Sys_DictionaryList>());
                    }

                    WebResponseContent _responseData = CheckKeyValue(listObj);
                    if (!_responseData.Status) return _responseData;

                    // 验证和清理SQL
                    dictionary.DbSql = ValidateAndCleanSql(dictionary.DbSql);

                    return new WebResponseContent(true);
                };

                return RemoveCache(base.Update(saveDataModel));
            }
            catch (Exception ex)
            {
                Log.Error( $"更新字典失败: {ex.Message}", ex);
                return new WebResponseContent().Error("更新字典失败，请稍后重试");
            }
        }

        /// <summary>
        /// 检查字典项的键值是否重复
        /// </summary>
        private WebResponseContent CheckKeyValue(List<Sys_DictionaryList> dictionaryLists)
        {
            WebResponseContent webResponse = new WebResponseContent();

            if (dictionaryLists == null || dictionaryLists.Count == 0)
            {
                return webResponse.OK();
            }

            // 检查字典项名称是否重复
            var duplicateNames = dictionaryLists
                .Where(x => !string.IsNullOrWhiteSpace(x.DicName))
                .GroupBy(g => g.DicName.Trim())
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToList();

            if (duplicateNames.Any())
            {
                return webResponse.Error($"【字典项名称】不能有重复的值: {string.Join(",", duplicateNames)}");
            }

            // 检查字典项Key是否重复
            var duplicateKeys = dictionaryLists
                .Where(x => !string.IsNullOrWhiteSpace(x.DicValue))
                .GroupBy(g => g.DicValue.Trim())
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToList();

            if (duplicateKeys.Any())
            {
                return webResponse.Error($"【字典项Key】不能有重复的值: {string.Join(",", duplicateKeys)}");
            }

            return webResponse.OK();
        }

        /// <summary>
        /// 验证和清理SQL语句
        /// </summary>
        private string ValidateAndCleanSql(string sql)
        {
            if (string.IsNullOrEmpty(sql)) return sql;

            // 只允许SELECT查询
            if (!sql.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            {
                Log.Warning($"字典SQL必须是SELECT语句: {sql}");
                return "";
            }

            // 移除注释
            sql = Regex.Replace(sql, @"--.*$", "", RegexOptions.Multiline);
            sql = Regex.Replace(sql, @"/\*.*?\*/", "", RegexOptions.Singleline);

            return sql;
        }

        /// <summary>
        /// 检查SQL是否安全
        /// </summary>
        private bool IsSqlSafe(string sql)
        {
            if (string.IsNullOrEmpty(sql)) return true;

            // 检查是否包含危险关键字
            string[] words = sql.Split(new[] { ' ', '\t', '\r', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var word in words)
            {
                if (SqlBlackList.Contains(word))
                {
                    return false;
                }
            }

            // 检查是否只是SELECT语句
            return sql.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 新增字典
        /// </summary>
        public override WebResponseContent Add(SaveModel saveDataModel)
        {
            if (saveDataModel == null || saveDataModel.MainData == null)
            {
                return new WebResponseContent().Error("参数不能为空");
            }

            try
            {
                if (saveDataModel.MainData.DicKeyIsNullOrEmpty("DicNo"))
                {
                    return new WebResponseContent().Error("字典编号不能为空");
                }

                string dicNo = saveDataModel.MainData["DicNo"]?.ToString()?.Trim();
                if (string.IsNullOrWhiteSpace(dicNo))
                {
                    return new WebResponseContent().Error("字典编号不能为空");
                }

                if (repository.Exists(x => x.DicNo == dicNo))
                {
                    return new WebResponseContent().Error($"字典编号:{dicNo}已存在");
                }

                base.AddOnExecuting = (Sys_Dictionary dic, object obj) =>
                {
                    if (dic == null)
                    {
                        return new WebResponseContent().Error("字典信息不能为空");
                    }

                    var dictionaryLists = obj as List<Sys_DictionaryList>;
                    WebResponseContent _responseData = CheckKeyValue(dictionaryLists);
                    if (!_responseData.Status) return _responseData;

                    // 验证和清理SQL
                    dic.DbSql = ValidateAndCleanSql(dic.DbSql);

                    return new WebResponseContent(true);
                };

                return RemoveCache(base.Add(saveDataModel));
            }
            catch (Exception ex)
            {
                Log.Error( $"新增字典失败: {ex.Message}", ex);
                return new WebResponseContent().Error("新增字典失败，请稍后重试");
            }
        }

        /// <summary>
        /// 删除字典
        /// </summary>
        public override WebResponseContent Del(object[] keys, bool delList = false)
        {
            if (keys == null || keys.Length == 0)
            {
                return new WebResponseContent().Error("请选择要删除的数据");
            }

            try
            {
                base.DelOnExecuting = (object[] delKeys) =>
                {
                    // 可以在这里添加删除前的验证逻辑
                    return new WebResponseContent(true);
                };

                // true将子表数据同时删除
                return RemoveCache(base.Del(keys, true));
            }
            catch (Exception ex)
            {
                Log.Error( $"删除字典失败: {ex.Message}", ex);
                return new WebResponseContent().Error("删除字典失败，请稍后重试");
            }
        }

        /// <summary>
        /// 清除字典缓存
        /// </summary>
        private WebResponseContent RemoveCache(WebResponseContent webResponse)
        {
            if (webResponse != null && webResponse.Status)
            {
                try
                {
                    CacheContext.Remove(DictionaryManager.Key);
                    // 可以添加刷新静态字典的逻辑
                    // DictionaryManager.Refresh();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "删除字典失败");
                    
                }
            }
            return webResponse ?? new WebResponseContent();
        }
    }
}