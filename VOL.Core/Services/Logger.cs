using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using VOL.Core.Configuration;
using VOL.Core.Const;
using VOL.Core.DBManager;
using VOL.Core.Enums;
using VOL.Core.Extensions;
using VOL.Core.ManageUser;
using VOL.Entity.DomainModels;

namespace VOL.Core.Services
{
    /// <summary>
    /// 通过内置队列异步定时写日志
    /// </summary>
    public static class Logger
    {
        private const int _maxQueueSize = 10000; // Max queue size
        public static BlockingCollection<Sys_Log> loggerQueueData = new BlockingCollection<Sys_Log>(_maxQueueSize);
        private static DateTime lastClearFileDT = DateTime.Now.AddDays(-1);
        private static string _loggerPath = AppSetting.DownLoadPath + "Logger\\Queue\\";
        private static readonly LogLevel _minimumLogLevel; // Added field for minimum log level

        static Logger()
        {
            // Read and parse the minimum log level from configuration
            string configuredLevel = AppSetting.Configuration.GetValue<string>("Logging:LogLevel:Default");
            if (string.IsNullOrEmpty(configuredLevel) || !Enum.TryParse(configuredLevel, true, out _minimumLogLevel))
            {
                _minimumLogLevel = LogLevel.Information; // Default if not set or invalid
                Console.WriteLine($"日志级别未配置或配置错误，默认为: {_minimumLogLevel}"); // Log to console if logger itself can't be used yet
            }
            else
            {
                Console.WriteLine($"日志级别配置为: {_minimumLogLevel}");
            }

            Task.Run(() =>
            {
                Start();
                //if (DBType.Name != "MySql")
                //{
                //    return;
                //}
                //try
                //{ 
                //    DBServerProvider.SqlDapper.ExcuteNonQuery("set global local_infile = 'ON';", null);
                //}
                //catch (Exception ex)
                //{
                //    Console.WriteLine($"日志启动调用mysql数据库异常：{ex.Message},{ex.StackTrace}");
                //}
            });
        }

        public static void Info(string message)
        {
            Info(LogEvent.Info, message);
        }
        public static void Info(LogEvent logEvent, string message = null)
        {
            Info(LogLevel.Information, logEvent, message, null, null);
        }
        public static void Info(LogEvent logEvent, string requestParam, string resposeParam, string ex = null)
        {
            Add(LogLevel.Information, logEvent, requestParam, resposeParam, ex, LoggerStatus.Info);
        }
        // Overload for Info that accepts LogLevel directly
        public static void Info(LogLevel logLevel, LogEvent logEvent, string requestParam, string resposeParam, string ex = null)
        {
            Add(logLevel, logEvent, requestParam, resposeParam, ex, LoggerStatus.Info);
        }

        public static void OK(string message)
        {
            OK(LogEvent.Success, message);
        }
        public static void OK(LogEvent logEvent, string message = null)
        {
            OK(LogLevel.Information, logEvent, message, null, null);
        }
        public static void OK(LogEvent logEvent, string requestParam, string resposeParam, string ex = null)
        {
            Add(LogLevel.Information, logEvent, requestParam, resposeParam, ex, LoggerStatus.Success);
        }
        // Overload for OK that accepts LogLevel directly (though typically Information for OK/Success)
        public static void OK(LogLevel logLevel, LogEvent logEvent, string requestParam, string resposeParam, string ex = null)
        {
            Add(logLevel, logEvent, requestParam, resposeParam, ex, LoggerStatus.Success);
        }

        public static void Error(string message)
        {
            Error(LogEvent.Error, message);
        }
        public static void Error(LogEvent logEvent, string message)
        {
            Error(LogLevel.Error, logEvent, message, null, null);
        }
        public static void Error(LogEvent logEvent, string requestParam, string resposeParam, string ex = null)
        {
            Add(LogLevel.Error, logEvent, requestParam, resposeParam, ex, LoggerStatus.Error);
        }
        // Overload for Error that accepts LogLevel directly
        public static void Error(LogLevel logLevel, LogEvent logEvent, string requestParam, string resposeParam, string ex = null)
        {
            Add(logLevel, logEvent, requestParam, resposeParam, ex, LoggerStatus.Error);
        }

        // Warning methods
        public static void Warning(string message)
        {
            Warning(LogEvent.Info, message); // Default LogEvent to Info if not specified for a simple warning message
        }
        public static void Warning(LogEvent logEvent, string message)
        {
            Warning(LogLevel.Warning, logEvent, message, null, null);
        }
        public static void Warning(LogEvent logEvent, string requestParam, string resposeParam, string ex = null)
        {
            Add(LogLevel.Warning, logEvent, requestParam, resposeParam, ex, LoggerStatus.Info); // LoggerStatus might need a 'Warning' state too
        }
        public static void Warning(LogLevel logLevel, LogEvent logEvent, string requestParam, string resposeParam, string ex = null)
        {
            Add(logLevel, logEvent, requestParam, resposeParam, ex, LoggerStatus.Info); // LoggerStatus might need a 'Warning' state too
        }


        /// <summary>
        /// 多线程调用日志
        /// </summary>
        /// <param name="message"></param>
        public static void AddAsync(string message, string ex = null)
        {
            LogLevel logLevel = ex != null ? LogLevel.Error : LogLevel.Information;
            AddAsync(logLevel, LogEvent.Info, null, message, ex, ex != null ? LoggerStatus.Error : LoggerStatus.Info);
        }
        public static void AddAsync(LogLevel logLevel, LogEvent logEvent, string requestParameter, string responseParameter, string ex, LoggerStatus status)
        {
            if (logLevel < _minimumLogLevel) // Apply filter
            {
                return;
            }
            var log = new Sys_Log()
            {
                BeginDate = DateTime.Now,
                EndDate = DateTime.Now,
                User_Id = 0,
                UserName = "",
                LogLevel = logLevel.ToString(),
                LogType = logEvent.ToString(), // This is LogEvent now
                ExceptionInfo = ex,
                RequestParameter = requestParameter,
                ResponseParameter = responseParameter,
                Success = (int)status
            };
            if (!loggerQueueData.TryAdd(log))
            {
                // Queue is full, log was not added.
                string consoleMessage = $"Warning: Logger queue is full ({_maxQueueSize} items). Log message dropped. Event: {log.LogType}, Level: {log.LogLevel}, URL: {log.Url}";
                Console.WriteLine(consoleMessage);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="requestParameter">请求参数</param>
        /// <param name="responseParameter">响应参数</param>
        /// <param name="success">响应结果1、成功,2、异常，0、其他</param>
        /// <param name="userInfo">用户数据</param>
        public static void Add(LogLevel logLevel, LogEvent logEvent, string requestParameter, string responseParameter, string ex, LoggerStatus status)
        {
            Add(logLevel, logEvent.ToString(), requestParameter, responseParameter, ex, status);
        }

        public static void Add(LogLevel logLevel, string logEventName, string requestParameter, string responseParameter, string ex, LoggerStatus status)
        {
            if (logLevel < _minimumLogLevel) // Apply filter
            {
                return;
            }

            Sys_Log log = null;
            try
            {
                HttpContext context = Utilities.HttpContext.Current;
                if (context.Request.Method == "OPTIONS") return;
                ActionObserver cctionObserver = (context.RequestServices.GetService(typeof(ActionObserver)) as ActionObserver);
                if (context == null)
                {
                    WriteText($"未获取到httpcontext信息,type:{logEvent},reqParam:{requestParameter},respParam:{responseParameter},ex:{ex},success:{status.ToString()}"); // Used logEvent
                    return;
                }
                UserInfo userInfo = UserContext.Current.UserInfo;
                log = new Sys_Log()
                {
                    //Id = Guid.NewGuid().ToString(),
                    BeginDate = cctionObserver.RequestDate,
                    EndDate = DateTime.Now,
                    User_Id = userInfo.User_Id,
                    UserName = userInfo.UserTrueName,
                    Role_Id = userInfo.Role_Id,
                    LogLevel = logLevel.ToString(),
                    LogType = logEventName, // This was the old loggerType
                    ExceptionInfo = ex,
                    RequestParameter = requestParameter,
                    ResponseParameter = responseParameter,
                    Success = (int)status
                };
                SetServicesInfo(log, context);
            }
            catch (Exception exception)
            {
                log = log ?? new Sys_Log()
                {
                    BeginDate = DateTime.Now,
                    EndDate = DateTime.Now,
                    LogLevel = logLevel.ToString(), // Assign LogLevel
                    LogType = logEvent.ToString(),
                    RequestParameter = requestParameter,
                    ResponseParameter = responseParameter,
                    Success = (int)status,
                    ExceptionInfo = ex + exception.Message
                };
            }
            if (!loggerQueueData.TryAdd(log))
            {
                // Queue is full, log was not added.
                string consoleMessage = $"Warning: Logger queue is full ({_maxQueueSize} items). Log message dropped. Event: {log.LogType}, Level: {log.LogLevel}, URL: {log.Url}";
                Console.WriteLine(consoleMessage);
            }
        }

        private static void Start()
        {
            DataTable queueTable = CreateEmptyTable();
            while (true)
            {
                try
                {
                    // Attempt to fill the batch table up to 500 rows
                    while (queueTable.Rows.Count < 500)
                    {
                        if (loggerQueueData.TryTake(out Sys_Log log, TimeSpan.FromMilliseconds(50))) // Short timeout
                        {
                            DequeueToTable(queueTable, log); // Pass the dequeued log directly
                        }
                        else
                        {
                            // Queue is empty or was empty during the timeout
                            break;
                        }
                    }

                    if (queueTable.Rows.Count == 0)
                    {
                        Thread.Sleep(1000); // Wait if batch is empty
                        continue;
                    }

                    DBServerProvider.SqlDapper.BulkInsert(queueTable, "Sys_Log", SqlBulkCopyOptions.KeepIdentity, null, _loggerPath);
                    queueTable.Clear();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"日志批量写入数据时出错:{ex.Message}");
                    WriteText(ex.Message + ex.StackTrace + ex.Source);
                    // list.Clear();
                }

            }

        }

        private static void WriteText(string message)
        {
            try
            {
                Utilities.FileHelper.WriteFile(_loggerPath + "WriteError\\", $"{DateTime.Now.ToString("yyyyMMdd")}.txt", message + "\r\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"日志写入文件时出错:{ex.Message}");
            }
        }

        private static void DequeueToTable(DataTable queueTable, Sys_Log log) // log is now passed as parameter
        {
            // loggerQueueData.TryDequeue(out Sys_Log log); // Dequeue happens in Start() method now
            if (log == null) return; // Should not happen if called from Start() correctly

            DataRow row = queueTable.NewRow();
            if (log.BeginDate == null)
            {
                log.BeginDate = DateTime.Now;
            }
            //  row["Id"] = log.Id;
            row["LogType"] = log.LogType;
            row["RequestParameter"] = log.RequestParameter;
            row["ResponseParameter"] = log.ResponseParameter;
            row["ExceptionInfo"] = log.ExceptionInfo;
            row["Success"] = log.Success ?? -1;
            row["BeginDate"] = log.BeginDate;
            row["EndDate"] = log.EndDate;
            row["ElapsedTime"] = ((DateTime)log.EndDate - (DateTime)log.BeginDate).TotalMilliseconds;
            row["UserIP"] = log.UserIP;
            row["ServiceIP"] = log.ServiceIP;
            row["BrowserType"] = log.BrowserType;
            row["Url"] = log.Url;
            row["User_Id"] = log.User_Id ?? -1;
            row["UserName"] = log.UserName;
            row["Role_Id"] = log.Role_Id ?? -1;
            row["LogLevel"] = log.LogLevel; // New assignment
            queueTable.Rows.Add(row);
        }
        private static DataTable CreateEmptyTable()
        {
            DataTable queueTable = new DataTable();
            queueTable.Columns.Add("LogType", typeof(string)); // This is LogEvent now
            queueTable.Columns.Add("LogLevel", typeof(string)); // New column
            queueTable.Columns.Add("RequestParameter", typeof(string));
            queueTable.Columns.Add("ResponseParameter", typeof(string));
            queueTable.Columns.Add("ExceptionInfo", typeof(string));
            queueTable.Columns.Add("Success", Type.GetType("System.Int32"));
            queueTable.Columns.Add("BeginDate", Type.GetType("System.DateTime"));
            queueTable.Columns.Add("EndDate", Type.GetType("System.DateTime"));
            queueTable.Columns.Add("ElapsedTime", Type.GetType("System.Int32"));
            queueTable.Columns.Add("UserIP", typeof(string));
            queueTable.Columns.Add("ServiceIP", typeof(string));
            queueTable.Columns.Add("BrowserType", typeof(string));
            queueTable.Columns.Add("Url", typeof(string));
            queueTable.Columns.Add("User_Id", Type.GetType("System.Int32"));
            queueTable.Columns.Add("UserName", typeof(string));
            queueTable.Columns.Add("Role_Id", Type.GetType("System.Int32"));
            return queueTable;
        }

        public static void SetServicesInfo(Sys_Log log, HttpContext context)
        {
            string result = String.Empty;
            log.Url = context.Request.Scheme + "://" + context.Request.Host + context.Request.PathBase +
                context.Request.Path;

            log.UserIP = context.GetUserIp()?.Replace("::ffff:", "");
            log.ServiceIP = context.Connection.LocalIpAddress.MapToIPv4().ToString() + ":" + context.Connection.LocalPort;

            log.BrowserType = context.Request.Headers["User-Agent"];
            if (log.BrowserType != null && log.BrowserType.Length > 190)
            {
                log.BrowserType = log.BrowserType.Substring(0, 190);
            }
            if (string.IsNullOrEmpty(log.RequestParameter))
            {
                try
                {
                    log.RequestParameter = context.GetRequestParameters();
                    //if (log.RequestParameter != null)
                    //{
                    //    log.RequestParameter = HttpUtility.UrlDecode(log.RequestParameter, Encoding.UTF8);
                    //}
                }
                catch (Exception ex)
                {
                    log.ExceptionInfo += $"日志读取参数出错:{ex.Message}";
                    Console.WriteLine($"日志读取参数出错:{ex.Message}");
                }
            }
        }
    }

    public enum LoggerStatus
    {
        Success = 1,
        Error = 2,
        Info = 3
    }
}
