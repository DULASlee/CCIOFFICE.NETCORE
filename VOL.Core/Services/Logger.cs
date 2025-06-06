using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Concurrent; // For BlockingCollection
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient; // Specific to SqlBulkCopyOptions, consider if needed for other DBs
using System.IO; // Added for Path, Directory, File operations
using System.Linq;
using System.Text;
using System.Text.RegularExpressions; // Added for Regex
using System.Threading;
using System.Threading.Tasks;
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
    /// Provides a static logging service that asynchronously writes log entries to the database.
    /// It uses a bounded in-memory queue (`BlockingCollection`) to decouple log submission from database writing,
    /// which is performed by a background task processing the queue in batches.
    /// Log entries are filtered based on a minimum log level configured in `appsettings.json`.
    /// Request and response parameters are desensitized before logging.
    /// A fallback mechanism writes logs to date-split text files with rotation if database logging fails or HttpContext is unavailable.
    /// </summary>
    public static class Logger
    {
        private const int _maxQueueSize = 10000;
        public static BlockingCollection<Sys_Log> loggerQueueData = new BlockingCollection<Sys_Log>(_maxQueueSize);
        private static string _loggerPath = AppSetting.DownLoadPath + "Logger\\Queue\\";
        private static readonly LogLevel _minimumLogLevel;

        private static readonly List<(Regex Pattern, string Replacement)> DesensitizationRules = new List<(Regex, string)>
        {
            (new Regex("\"(password|pwd|userPwd|oldPassword|newPassword)\"\\s*:\\s*\".*?\"", RegexOptions.IgnoreCase | RegexOptions.Compiled), "\"$1\":\"***\""),
            (new Regex("\"(token|accessToken|access_token|refreshToken|authorization)\"\\s*:\\s*\".*?\"", RegexOptions.IgnoreCase | RegexOptions.Compiled), "\"$1\":\"***\""),
            (new Regex("\"(phone|mobile|tel|phoneNo)\"\\s*:\\s*\"(\\d{3})\\d*(\\d{4})\"", RegexOptions.IgnoreCase | RegexOptions.Compiled), "\"$1\":\"$2****$3\""),
            (new Regex("\"(phone|mobile|tel|phoneNo)\"\\s*:\\s*\"[^\"]*?\"", RegexOptions.IgnoreCase | RegexOptions.Compiled), "\"$1\":\"***\""),
            (new Regex("\"(email|e_mail|mailBox)\"\\s*:\\s*\"[^@]+@[^.]+\\..*?\"", RegexOptions.IgnoreCase | RegexOptions.Compiled), "\"$1\":\"***@***.***\""),
            (new Regex("\"(email|e_mail|mailBox)\"\\s*:\\s*\"[^\"]*?\"", RegexOptions.IgnoreCase | RegexOptions.Compiled), "\"$1\":\"***\""),
            (new Regex("\"(idNumber|idCard|nationalId|identityNo)\"\\s*:\\s*\"(\\w{3})\\w*(\\w{3})\"", RegexOptions.IgnoreCase | RegexOptions.Compiled), "\"$1\":\"$2***********$3\""),
            (new Regex("\"(idNumber|idCard|nationalId|identityNo)\"\\s*:\\s*\"[^\"]*?\"", RegexOptions.IgnoreCase | RegexOptions.Compiled), "\"$1\":\"***\"")
        };

        static Logger()
        {
            string configuredLevel = AppSetting.Configuration.GetValue<string>("Logging:LogLevel:Default");
            if (string.IsNullOrEmpty(configuredLevel) || !Enum.TryParse(configuredLevel, true, out _minimumLogLevel))
            {
                _minimumLogLevel = LogLevel.Information;
                Console.WriteLine($"VOL.Core.Services.Logger: Log level not configured or invalid. Defaulting to: {_minimumLogLevel}");
            }
            else
            {
                Console.WriteLine($"VOL.Core.Services.Logger: Log level configured to: {_minimumLogLevel}");
            }
            Task.Run(() => Start());
        }

        private static string DesensitizeLogParameter(string content)
        {
            if (string.IsNullOrEmpty(content)) return content;
            string desensitizedContent = content;
            try
            {
                foreach (var rule in DesensitizationRules)
                {
                    desensitizedContent = rule.Pattern.Replace(desensitizedContent, rule.Replacement);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during log desensitization: {ex.Message}. Original content might be logged partially or fully.");
                return $"[DESENSITIZATION_ERROR: Original content suppressed. Error: {ex.Message}]";
            }
            return desensitizedContent;
        }

        public static void Info(string message) { Add(LogLevel.Information, LogEvent.Info, message, null, null, LoggerStatus.Info); }
        public static void Info(LogEvent logEvent, string message = null) { Add(LogLevel.Information, logEvent, message, null, null, LoggerStatus.Info); }
        public static void Info(LogEvent logEvent, string requestParam, string resposeParam, string ex = null) { Add(LogLevel.Information, logEvent, requestParam, resposeParam, ex, LoggerStatus.Info); }
        public static void Info(LogLevel logLevel, LogEvent logEvent, string requestParam, string resposeParam, string ex = null) { Add(logLevel, logEvent, requestParam, resposeParam, ex, LoggerStatus.Info); }

        public static void OK(string message) { Add(LogLevel.Information, LogEvent.Success, message, null, null, LoggerStatus.Success); }
        public static void OK(LogEvent logEvent, string message = null) { Add(LogLevel.Information, logEvent, message, null, null, LoggerStatus.Success); }
        public static void OK(LogEvent logEvent, string requestParam, string resposeParam, string ex = null) { Add(LogLevel.Information, logEvent, requestParam, resposeParam, ex, LoggerStatus.Success); }
        public static void OK(LogLevel logLevel, LogEvent logEvent, string requestParam, string resposeParam, string ex = null) { Add(logLevel, logEvent, requestParam, resposeParam, ex, LoggerStatus.Success); }

        public static void Error(string message) { Add(LogLevel.Error, LogEvent.Error, message, null, null, LoggerStatus.Error); }
        public static void Error(LogEvent logEvent, string message) { Add(LogLevel.Error, logEvent, message, null, null, LoggerStatus.Error); }
        public static void Error(LogEvent logEvent, string requestParam, string resposeParam, string ex = null) { Add(LogLevel.Error, logEvent, requestParam, resposeParam, ex, LoggerStatus.Error); }
        public static void Error(LogLevel logLevel, LogEvent logEvent, string requestParam, string resposeParam, string ex = null) { Add(logLevel, logEvent, requestParam, resposeParam, ex, LoggerStatus.Error); }

        public static void Warning(string message) { Add(LogLevel.Warning, LogEvent.Info, message, null, null, LoggerStatus.Info); }
        public static void Warning(LogEvent logEvent, string message) { Add(LogLevel.Warning, logEvent, message, null, null, LoggerStatus.Info); }
        public static void Warning(LogEvent logEvent, string requestParam, string resposeParam, string ex = null) { Add(LogLevel.Warning, logEvent, requestParam, resposeParam, ex, LoggerStatus.Info); }
        public static void Warning(LogLevel logLevel, LogEvent logEvent, string requestParam, string resposeParam, string ex = null) { Add(logLevel, logEvent, requestParam, resposeParam, ex, LoggerStatus.Info); }

        public static void AddAsync(string message, string ex = null)
        {
            LogLevel logLevel = ex != null ? LogLevel.Error : LogLevel.Information;
            AddAsync(logLevel, LogEvent.Info, null, message, ex, ex != null ? LoggerStatus.Error : LoggerStatus.Info, "N/A_Async_Simple");
        }

        public static void AddAsync(LogLevel logLevel, LogEvent logEvent, string requestParameter, string responseParameter, string ex, LoggerStatus status, string traceId = null)
        {
            if (logLevel < _minimumLogLevel) return;
            string desensitizedRequestParams = DesensitizeLogParameter(requestParameter);
            string desensitizedResponseParams = DesensitizeLogParameter(responseParameter);
            var log = new Sys_Log()
            {
                BeginDate = DateTime.Now, EndDate = DateTime.Now, User_Id = 0, UserName = "",
                LogLevel = logLevel.ToString(), LogType = logEvent.ToString(), ExceptionInfo = ex,
                RequestParameter = desensitizedRequestParams, ResponseParameter = desensitizedResponseParams,
                Success = (int)status, TraceId = traceId ?? "N/A_Async"
            };
            if (!loggerQueueData.TryAdd(log))
            {
                Console.WriteLine($"Warning: Logger queue is full ({_maxQueueSize} items). Log message dropped. Event: {log.LogType}, Level: {log.LogLevel}, URL: {log.Url}, TraceId: {log.TraceId}");
            }
        }

        public static void Add(LogLevel logLevel, LogEvent logEvent, string requestParameter, string responseParameter, string ex, LoggerStatus status)
        {
            Add(logLevel, logEvent.ToString(), requestParameter, responseParameter, ex, status);
        }

        public static void Add(LogLevel logLevel, string logEventName, string requestParameter, string responseParameter, string ex, LoggerStatus status)
        {
            if (logLevel < _minimumLogLevel) return;

            string desensitizedRequestParams = DesensitizeLogParameter(requestParameter);
            string desensitizedResponseParams = DesensitizeLogParameter(responseParameter);
            Sys_Log log = null;
            string traceId = "N/A_Context_Unavailable";

            try
            {
                HttpContext context = Utilities.HttpContext.Current;
                if (context?.Request.Method == "OPTIONS") return;
                ActionObserver actionObserver = context?.RequestServices.GetService(typeof(ActionObserver)) as ActionObserver;
                traceId = context?.TraceIdentifier ?? "N/A_TraceId_Unavailable";

                if (context == null && actionObserver == null)
                {
                    WriteText($"Warning: HttpContext not available for logging. Event: {logEventName}, Level: {logLevel}, Req: {desensitizedRequestParams}, Resp: {desensitizedResponseParams}, Ex: {ex}, Status: {status}, TraceId: {traceId}");
                     log = new Sys_Log()
                     {
                         BeginDate = DateTime.Now, EndDate = DateTime.Now, LogLevel = logLevel.ToString(),
                         LogType = logEventName, RequestParameter = desensitizedRequestParams, ResponseParameter = desensitizedResponseParams,
                         ExceptionInfo = ex, Success = (int)status, UserName = "System", TraceId = traceId
                     };
                }
                else
                {
                    UserInfo userInfo = UserContext.Current?.UserInfo ?? UserInfo.System;
                    log = new Sys_Log()
                    {
                        BeginDate = actionObserver?.RequestDate ?? DateTime.Now, EndDate = DateTime.Now,
                        User_Id = userInfo.User_Id, UserName = userInfo.UserTrueName, Role_Id = userInfo.Role_Id,
                        LogLevel = logLevel.ToString(), LogType = logEventName, ExceptionInfo = ex,
                        RequestParameter = desensitizedRequestParams, ResponseParameter = desensitizedResponseParams,
                        Success = (int)status, TraceId = traceId
                    };
                    if (context != null) SetServicesInfo(log, context);
                }
            }
            catch (Exception exception)
            {
                log = log ?? new Sys_Log() { BeginDate = DateTime.Now, EndDate = DateTime.Now, TraceId = traceId };
                log.LogLevel = LogLevel.Error.ToString(); log.LogType = LogEvent.Exception.ToString();
                log.RequestParameter = desensitizedRequestParams; log.ResponseParameter = desensitizedResponseParams;
                log.Success = (int)LoggerStatus.Error;
                log.ExceptionInfo = $"Error creating log entry: {exception.Message}. Original Ex: {ex}";
                Console.WriteLine($"CRITICAL: Error creating log entry itself. Original Log: Event={logEventName}, Level={logLevel}. Logging Error: {exception.Message}, TraceId: {traceId}");
            }

            if (log != null && !loggerQueueData.TryAdd(log))
            {
                Console.WriteLine($"Warning: Logger queue is full ({_maxQueueSize} items). Log message dropped. Event: {log.LogType}, Level: {log.LogLevel}, URL: {log.Url}, TraceId: {log.TraceId}");
            }
        }

        private static void Start()
        {
            DataTable queueTable = CreateEmptyTable();
            while (true)
            {
                try
                {
                    while (queueTable.Rows.Count < 500)
                    {
                        if (loggerQueueData.TryTake(out Sys_Log log, TimeSpan.FromMilliseconds(50)))
                        { DequeueToTable(queueTable, log); } else { break; }
                    }
                    if (queueTable.Rows.Count == 0) { Thread.Sleep(1000); continue; }
                    DBServerProvider.SqlDapper.BulkInsert(queueTable, "Sys_Log", SqlBulkCopyOptions.KeepIdentity, null, _loggerPath);
                    queueTable.Clear();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"日志批量写入数据时出错:{ex.Message}");
                    WriteText($"日志批量写入数据时出错: {ex.ToString()}");
                    Thread.Sleep(5000);
                }
            }
        }

        /// <summary>
        /// Fallback mechanism to write log messages to a text file if database logging fails or is unavailable.
        /// Log entries are written to a file named `fallback_{current_date_yyyyMMdd}.txt`
        /// within a "fallback" subdirectory of the configured `_loggerPath`.
        /// This method also implements a simple log rotation policy, deleting fallback log files
        /// older than a specified number of days (currently 7 days).
        /// </summary>
        /// <param name="message">The log message to write to the text file.</param>
        private static void WriteText(string message)
        {
            try
            {
                // Define the base directory for fallback logs.
                // _loggerPath is expected to be initialized, e.g., AppSetting.DownLoadPath + "Logger\\Queue\\"
                string fallbackLogDirectory = Path.Combine(_loggerPath, "fallback");

                // Ensure the fallback directory exists. If not, create it.
                if (!Directory.Exists(fallbackLogDirectory))
                {
                    Directory.CreateDirectory(fallbackLogDirectory);
                }

                // Log Rotation: Attempt to delete old log files.
                try
                {
                    int retentionDays = 7; // Define how many days of logs to keep.
                    // Get all files matching the fallback log pattern.
                    var logFiles = Directory.GetFiles(fallbackLogDirectory, "fallback_*.txt");
                    foreach (var logFile in logFiles)
                    {
                        string fileNameWithoutExt = Path.GetFileNameWithoutExtension(logFile);
                        // Check if the filename starts with "fallback_" and has the correct date length.
                        if (fileNameWithoutExt.StartsWith("fallback_") &&
                            fileNameWithoutExt.Length == ("fallback_".Length + "yyyyMMdd".Length))
                        {
                            // Extract the date part from the filename.
                            string dateString = fileNameWithoutExt.Substring("fallback_".Length);
                            // Try to parse the date string.
                            if (DateTime.TryParseExact(dateString,
                                                   "yyyyMMdd",
                                                   System.Globalization.CultureInfo.InvariantCulture, // Use invariant culture for consistent parsing.
                                                   System.Globalization.DateTimeStyles.None,
                                                   out DateTime fileDate))
                            {
                                // If the log file's date is older than the retention period, delete it.
                                if (fileDate < DateTime.Now.Date.AddDays(-retentionDays))
                                {
                                    File.Delete(logFile);
                                }
                            }
                        }
                    }
                }
                catch (Exception exCleanup)
                {
                    // Log cleanup errors to console but don't let them stop the current logging attempt.
                    Console.WriteLine($"Error during fallback log cleanup: {exCleanup.Message}");
                }

                // Actual log writing:
                // Construct the full path to today's fallback log file.
                string logFilePath = Path.Combine(fallbackLogDirectory, $"fallback_{DateTime.Now:yyyyMMdd}.txt");
                // Format the log message with a timestamp.
                string formattedMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} | {message}{Environment.NewLine}";
                // Append the formatted message to the log file. Creates the file if it doesn't exist.
                File.AppendAllText(logFilePath, formattedMessage);
            }
            catch (Exception exWrite)
            {
                // If writing to the fallback log itself fails, write an error to the console.
                // This is a last resort to make critical logging failures visible.
                Console.WriteLine($"Critical error: Failed to write to fallback log: {exWrite.Message}");
            }
        }

        private static void DequeueToTable(DataTable queueTable, Sys_Log log)
        {
            if (log == null) return;
            DataRow row = queueTable.NewRow();
            row["BeginDate"] = log.BeginDate ?? log.EndDate ?? DateTime.Now;
            row["EndDate"] = log.EndDate ?? log.BeginDate ?? DateTime.Now;
            if (log.EndDate.HasValue && log.BeginDate.HasValue) { row["ElapsedTime"] = (log.EndDate.Value - log.BeginDate.Value).TotalMilliseconds; } else { row["ElapsedTime"] = 0; }
            row["LogType"] = log.LogType; row["LogLevel"] = log.LogLevel;
            row["RequestParameter"] = log.RequestParameter; row["ResponseParameter"] = log.ResponseParameter;
            row["ExceptionInfo"] = log.ExceptionInfo; row["Success"] = log.Success ?? -1;
            row["UserIP"] = log.UserIP; row["ServiceIP"] = log.ServiceIP; row["BrowserType"] = log.BrowserType;
            row["Url"] = log.Url; row["User_Id"] = log.User_Id ?? -1; row["UserName"] = log.UserName;
            row["Role_Id"] = log.Role_Id ?? -1; row["TraceId"] = log.TraceId;
            queueTable.Rows.Add(row);
        }

        private static DataTable CreateEmptyTable()
        {
            DataTable queueTable = new DataTable();
            queueTable.Columns.Add("LogType", typeof(string)); queueTable.Columns.Add("LogLevel", typeof(string));
            queueTable.Columns.Add("RequestParameter", typeof(string)); queueTable.Columns.Add("ResponseParameter", typeof(string));
            queueTable.Columns.Add("ExceptionInfo", typeof(string)); queueTable.Columns.Add("Success", Type.GetType("System.Int32"));
            queueTable.Columns.Add("BeginDate", Type.GetType("System.DateTime")); queueTable.Columns.Add("EndDate", Type.GetType("System.DateTime"));
            queueTable.Columns.Add("ElapsedTime", Type.GetType("System.Int32")); queueTable.Columns.Add("UserIP", typeof(string));
            queueTable.Columns.Add("ServiceIP", typeof(string)); queueTable.Columns.Add("BrowserType", typeof(string));
            queueTable.Columns.Add("Url", typeof(string)); queueTable.Columns.Add("User_Id", Type.GetType("System.Int32"));
            queueTable.Columns.Add("UserName", typeof(string)); queueTable.Columns.Add("Role_Id", Type.GetType("System.Int32"));
            queueTable.Columns.Add("TraceId", typeof(string));
            return queueTable;
        }

        public static void SetServicesInfo(Sys_Log log, HttpContext context)
        {
            if (log == null || context == null) return;
            try
            {
                log.Url = context.Request.Scheme + "://" + context.Request.Host + context.Request.PathBase +
                    context.Request.Path + context.Request.QueryString;
                log.UserIP = context.GetUserIp()?.Replace("::ffff:", "");
                if (context.Connection.LocalIpAddress != null)
                {
                    log.ServiceIP = context.Connection.LocalIpAddress.MapToIPv4().ToString() + ":" + context.Connection.LocalPort;
                }
                log.BrowserType = context.Request.Headers["User-Agent"].ToString();
                if (log.BrowserType != null && log.BrowserType.Length > 190)
                {
                    log.BrowserType = log.BrowserType.Substring(0, 190);
                }
                if (string.IsNullOrEmpty(log.RequestParameter))
                {
                    try { log.RequestParameter = context.GetRequestParameters(); }
                    catch (Exception ex) { log.ExceptionInfo = (log.ExceptionInfo ?? "") + $" 日志读取(RequestParameter)参数出错:{ex.Message}"; Console.WriteLine($"日志读取(RequestParameter)参数出错:{ex.Message}"); }
                }
            }
            catch (Exception ex)
            {
                log.ExceptionInfo = (log.ExceptionInfo ?? "") + $" SetServicesInfo发生错误:{ex.Message}";
                Console.WriteLine($"SetServicesInfo发生错误:{ex.Message}");
            }
        }
    }

    public enum LoggerStatus { Success = 1, Error = 2, Info = 3 }
}
