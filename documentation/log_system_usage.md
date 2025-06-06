# Log System Usage Instructions

## 1. Log Level Explanation

The logging system utilizes standard log levels to categorize the severity and importance of log messages. Understanding and using these levels correctly is crucial for effective debugging, monitoring, and auditing.

The log levels are defined in `VOL.Core.Enums.LogLevel`:
*   **`Trace`**: Very detailed logs, typically used for tracing code execution paths during development. Usually disabled in production.
*   **`Debug`**: Information useful for debugging during development (e.g., variable states, specific parameters). Should be disabled or used sparingly in production.
*   **`Information`**: General information about application flow and key operations (e.g., service started, request processed successfully, user logged in). This is the default active level in production.
*   **`Warning`**: Indicates a potentially harmful situation, a minor error that doesn't stop the current operation, or an unexpected technical issue that is not critical (e.g., API call took too long, failed login attempt, invalid input that was handled).
*   **`Error`**: Runtime errors or unexpected conditions that prevented an operation from completing successfully (e.g., caught exceptions, system issues like database errors that were handled by wrapping in `SysException`).
*   **`Critical`**: Severe errors that might lead to application instability or termination (e.g., unhandled exceptions caught by the global middleware, critical infrastructure failures).

**Enabling/Disabling Log Levels:**
The minimum active log level is configured in `VOL.WebApi/appsettings.json` (and corresponding environment-specific files like `appsettings.Development.json`).
```json
{
  "Logging": {
    "LogLevel": {
      // Sets the minimum level for logs from VOL.Core.Services.Logger
      "Default": "Information"
    }
  },
  // If Serilog were fully active, its settings would also be here, e.g.:
  // "Serilog": {
  //   "MinimumLevel": {
  //     "Default": "Information", // Default for all sources
  //     "Override": { // Specific overrides
  //       "Microsoft": "Warning", // ASP.NET Core internal logs
  //       "System": "Warning"     // .NET system logs
  //     }
  //   }
  // }
}
```
*   To change the active log level for the custom `VOL.Core.Services.Logger`, modify the `Logging:LogLevel:Default` value. For example, set to `"Debug"` in `appsettings.Development.json` to see debug logs during development.
*   Production should typically be set to `"Information"` or `"Warning"`.

## 2. Log Content Specifications

All logs recorded by `VOL.Core.Services.Logger` are stored in the `Sys_Log` database table (and fallback text files in `logs/fallback/` if database logging fails). Each log entry aims to include the following key fields:

*   **`Id`**: Unique identifier for the log entry.
*   **`LogLevel`**: The severity level (e.g., "Information", "Error").
*   **`LogType`** (maps to `LogEvent`): Category or source of the log (e.g., "Login", "Update", "BizExceptionLog").
*   **`TraceId`**: The `HttpContext.TraceIdentifier` for correlating all logs related to a single HTTP request. **Crucial for `ERROR` and `CRITICAL` logs.**
*   **`BeginDate`, `EndDate`, `ElapsedTime`**: Timing information for the logged event or request.
*   **`Url`**: The request URL.
*   **`RequestParameter`**: Request parameters (desensitized).
*   **`ResponseParameter`**: Response parameters or results (desensitized).
*   **`ExceptionInfo`**: Detailed exception information (stack trace) if an exception was logged.
*   **`Success`**: Indicates if the operation associated with the log was successful (1=Success, 2=Error, 3=Info/Other).
*   **`UserIP`, `ServiceIP`, `BrowserType`**: Network and client information.
*   **`User_Id`, `UserName`, `Role_Id`**: Information about the user who performed the action (operator). (Note: `User_Id` and `UserName` might be 0 or empty for anonymous or system-generated logs).
*   **Timestamp fields** (`BeginDate`, `EndDate`): Provide the time of the event.

**Desensitization Rules:**
Sensitive information in `RequestParameter` and `ResponseParameter` is automatically masked by `VOL.Core.Services.Logger.DesensitizeLogParameter` before being stored. Current rules target:
*   **Passwords**: Fields named "password", "pwd", "userPwd", "oldPassword", "newPassword" (JSON-like: `"password":"***"`).
*   **Tokens**: Fields named "token", "accessToken", "access_token", "refreshToken", "authorization" (JSON-like: `"token":"***"`).
*   **Phone Numbers**: Fields named "phone", "mobile", "tel". Masks middle digits (JSON-like: `"phone":"138****1234"` or `"phone":"***"`).
*   **Email Addresses**: Fields named "email", "e_mail", "mailBox". Masks parts of the email (JSON-like: `"email":"***@***.***"` or `"email":"***"`).
*   **ID Numbers**: Fields named "idNumber", "idCard", "nationalId", "identityNo". Masks middle part (JSON-like: `"idNumber":"123***********456"` or `"idNumber":"***"`).
*   *Comments in `Logger.cs` within the `DesensitizationRules` list detail the exact regex patterns used.*

**Adding Log Statements in Business Code:**
When adding log statements in your business code (e.g., in services), always include a comment explaining the purpose of the log recording for auditability or debugging context.
```csharp
// Example in a service:
// using VOL.Core.Services;
// using VOL.Core.Enums;

// Record successful creation of a production order for auditing
Logger.Log(
    LogLevel.Information,
    LogEvent.Create, // Assuming a generic 'Create' or a more specific 'ProductionOrderCreated' LogEvent
    requestParam: $"OrderNo: {order.OrderNumber}, Product: {order.ProductName}",
    responseParam: $"New Order ID: {order.Id}", // Can be null if no specific response data to log
    ex: null,
    status: LoggerStatus.Success // Or LoggerStatus.Info if it's not strictly a success/failure state
);
// Comment: Operator: UserContext.Current.UserInfo.User_Id (if available, or captured automatically by Logger)
// Comment: Time: DateTime.UtcNow (captured automatically by Logger)
```

## 3. Log Query Guide

**Primary Log Store: `Sys_Log` Database Table**
The primary destination for logs is the `Sys_Log` table in the application database. You can query this table using standard SQL tools.

**Key fields for querying:**
*   **`TraceId`**: Use this to find all logs related to a specific problematic HTTP request. This is the most effective way to debug errors.
    ```sql
    SELECT * FROM Sys_Log WHERE TraceId = 'your_trace_id_here' ORDER BY BeginDate;
    ```
*   **`LogLevel`**: Filter by severity.
    ```sql
    SELECT * FROM Sys_Log WHERE LogLevel = 'Error' OR LogLevel = 'Critical' ORDER BY BeginDate DESC;
    ```
*   **`LogType`** (maps to `LogEvent`): Filter by event category.
    ```sql
    SELECT * FROM Sys_Log WHERE LogType = 'Login' ORDER BY BeginDate DESC;
    ```
*   **`User_Id` or `UserName`**: Find logs related to a specific user.
    ```sql
    SELECT * FROM Sys_Log WHERE UserName = 'john.doe' ORDER BY BeginDate DESC;
    ```
*   **`RequestParameter` / `ResponseParameter` / `ExceptionInfo`**: Use `LIKE` for keyword searches (be mindful of performance on large tables).
    ```sql
    SELECT * FROM Sys_Log WHERE RequestParameter LIKE '%specific_value%' ORDER BY BeginDate DESC;
    ```
*   **`BeginDate` / `EndDate`**: Filter by time range.

**Fallback Log Files:**
If the database logging fails, `VOL.Core.Services.Logger.WriteText` writes logs to local text files.
*   **Location**: `[Application_Root]/logs/fallback/` (The `logs` directory is typically in `VOL.WebApi/bin/Debug/netX.X/` during development or the deployed application's root). The `_loggerPath` in `Logger.cs` is derived from `AppSetting.DownLoadPath + "Logger\Queue\";` so the fallback is relative to that.
*   **Naming**: `fallback_{yyyyMMdd}.txt` (e.g., `fallback_20240718.txt`).
*   **Content**: Plain text, each line is a log entry with a timestamp.
*   **Querying**: Use text search tools (grep, text editors) to search these files by keyword, timestamp, or message content. `TraceId` will be part of the message if it was available during the fallback.

**Log Platforms (Future - ELK, etc.):**
If/when the system integrates with a centralized logging platform like ELK (Elasticsearch, Logstash, Kibana), that platform will provide its own powerful query language and interface for searching and analyzing logs. The structured nature of the logs (especially if Serilog were fully implemented) would make this very effective.

## 4. Configuration Examples (appsettings.json)

Configuration for the custom `VOL.Core.Services.Logger` is primarily for setting the minimum log level.

**File: `VOL.WebApi/appsettings.json` (or environment specific, e.g., `appsettings.Production.json`)**
```json
{
  // ... other configurations ...

  "Logging": {
    "LogLevel": {
      // Valid values: Trace, Debug, Information, Warning, Error, Critical
      // This controls the minimum level of logs processed by VOL.Core.Services.Logger
      "Default": "Information"
    }
  },

  // Example for Serilog (if it were fully active and configured via appsettings.json)
  // "Serilog": {
  //   "MinimumLevel": {
  //     "Default": "Information",
  //     "Override": {
  //       "Microsoft": "Warning",
  //       "System": "Warning"
  //     }
  //   },
  //   "WriteTo": [
  //     { "Name": "Console" },
  //     {
  //       "Name": "File",
  //       "Args": {
  //         "path": "logs/vol-webapi-.txt", // Main application log file if Serilog file sink is used
  //         "rollingInterval": "Day",
  //         "retainedFileCountLimit": 7,    // Retain 7 days of these primary log files
  //         "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] [{TraceId}] {Message:lj}{NewLine}{Exception}"
  //       }
  //     }
  //     // Potentially a database sink could be configured here too for Serilog
  //   ],
  //   "Enrich": [ "FromLogContext", "WithMachineName", "WithThreadId", "WithEnvironmentUserName" ]
  // }
}
```
**Fallback Log Rotation (Internal to `Logger.cs`):**
*   The fallback file logger in `VOL.Core.Services.Logger.cs` (method `WriteText`) has an internal rotation policy:
    *   Files stored in: `[AppSetting.DownLoadPath]/Logger/Queue/fallback/`
    *   Filename pattern: `fallback_{yyyyMMdd}.txt`
    *   Retention: 7 days (older files are automatically deleted).
    *   This is not configured via `appsettings.json` but is hardcoded in `Logger.cs`.

---
*This document should be kept up-to-date as logging strategies and configurations evolve.*
