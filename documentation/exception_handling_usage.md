# Exception Handling Usage Instructions

## 1. Functional Overview

The unified exception handling mechanism in this project aims to standardize how errors are processed and returned to API clients, and to ensure that all exceptions are appropriately logged for traceability and debugging. Key design goals include:

*   **Centralized Handling**: Replace scattered `try-catch` blocks in business logic with centralized handlers.
*   **Standardized Responses**: Provide a consistent JSON error response format for all API errors.
*   **Differentiated Error Types**: Clearly distinguish between business logic errors (expected, potentially client-correctable) and unexpected system errors.
*   **Improved Traceability**: Ensure all exceptions are logged with sufficient context, including a `traceId`.
*   **Reduced Boilerplate**: Minimize repetitive exception handling code in controllers and services through AOP techniques.

The system uses a combination of:
*   **Global Exception Handling Middleware** (`VOL.Core/Middleware/ExceptionHandlerMiddleWare.cs`): Catches any unhandled exceptions that propagate to the top of the middleware pipeline.
*   **Service-Level Interceptors** (`VOL.Core/AOP/ServiceExceptionInterceptor.cs`): Wraps calls to service layer methods, logs exceptions, and standardizes them by re-throwing them as `BizException` or `SysException`.
*   **(Future) Controller-Level Action Filters**: While an initial attempt to create a specific controller exception filter was deferred due to tooling issues, the global middleware adequately covers controller exceptions for now. Future enhancements might add specific filters for more granular controller-level error handling if needed.

## 2. Unified API Error Response Format

All errors returned by the API will conform to the following JSON structure, represented by the `VOL.Core.Extensions.Response.ApiErrorResponse` class:

```json
{
  "errorCode": "string", // Application-specific error code
  "message": "string",   // Human-readable error description
  "timestamp": "string", // ISO 8601 UTC timestamp (e.g., "2024-07-18T10:30:00.123Z")
  "traceId": "string"    // Unique ID for tracing the request, matches HttpContext.TraceIdentifier
}
```

*   `errorCode`: For business exceptions, this will be a specific code (e.g., "USER_NOT_FOUND", "VALIDATION_ERROR"). For system exceptions, it might be a more generic code like "SYSTEM_ERROR" or "UNHANDLED_SYSTEM_ERROR".
*   `message`: A clear description of the error. For business errors, this can be shown to the user. For system errors in production, a generic message is typically returned to avoid leaking sensitive details, while the full error is logged.
*   `timestamp`: The UTC time when the error was processed.
*   `traceId`: This ID can be used to correlate the error response with server-side logs for debugging.

## 3. Exception Type List & Handling Strategy

The system defines a hierarchy of custom exceptions and handles standard .NET exceptions:

| Exception Class             | Base Class        | `errorCode` Examples                                  | HTTP Status | Client Message                                       | Logging Level | Notes                                                                                                |
| :-------------------------- | :---------------- | :---------------------------------------------------- | :---------- | :--------------------------------------------------- | :------------ | :--------------------------------------------------------------------------------------------------- |
| `BizException`              | `BaseAppException`| User-defined (e.g., "INVENTORY_LOW", "INVALID_INPUT") | 400         | Specific message from exception                      | Warning       | Represents expected business rule violations or client errors. Should be explicitly thrown by services. |
| `SysException`              | `BaseAppException`| "SYSTEM_ERROR", "DB_CONNECTION_FAILED" (internal use) | 500         | "A system error occurred. Please contact support." | Error         | Wraps unexpected system/infrastructure issues not directly resolvable by the client.                 |
| `SqlException` (example)    | `DbException`     | "SYSTEM_ERROR" (via `SysException` wrapper)           | 500         | "A system error occurred. Please contact support." | Error         | Handled by service interceptor, logged in detail, wrapped in `SysException`.                       |
| Other `System.Exception`    | `System.Exception`| "UNHANDLED_SYSTEM_ERROR"                              | 500         | Dev: Full exception. Prod: "An unexpected error occurred." | Critical      | Caught by global middleware, logged in detail.                                                     |
| `ValidationException` (example) | `Exception`     | "VALIDATION_ERROR" (via `BizException` wrapper)     | 400         | Specific validation messages                         | Warning       | Should ideally be caught and re-thrown as `BizException` by services or validation aspects.      |

**Key Handling Points:**

*   **`VOL.Core/AOP/ServiceExceptionInterceptor.cs`**:
    *   Catches exceptions from service layer methods.
    *   `BizException` is logged (Warning) and re-thrown.
    *   `SysException` (if thrown by service) is logged (Error) and re-thrown.
    *   Any other `System.Exception` is logged (Error) and wrapped in a new `SysException` before being thrown. This ensures that exceptions leaving the service layer are either `BizException` or `SysException`.
*   **`VOL.Core/Middleware/ExceptionHandlerMiddleWare.cs`**:
    *   Catches all exceptions not handled by lower layers.
    *   If `BizException`: Returns HTTP 400 with `bizEx.ErrorCode` and `bizEx.Message`. Logs as Warning.
    *   If `SysException`: Returns HTTP 500 with `sysEx.ErrorCode` (or a generic one) and a standard system error message. Logs as Error.
    *   If other `Exception`: Returns HTTP 500 with a generic "UNHANDLED_SYSTEM_ERROR" code and a generic message (detailed in dev). Logs as Critical.
    *   All responses include `traceId` and `timestamp`.

## 4. Usage Examples

### 4.1. Throwing Exceptions in Services

**Throwing a Business Exception:**
```csharp
// In a service method (e.g., VOL.Sys/Services/System/Sys_UserService.cs)
// using VOL.Core.Extensions; // For BizException

public WebResponseContent UpdateUserStatus(int userId, UserStatus newStatus)
{
    var user = _repository.FindById(userId);
    if (user == null)
    {
        // Throw BizException for a known business rule violation / scenario
        throw new BizException("User not found with the provided ID.", "USER_NOT_FOUND");
    }

    if (user.Status == newStatus)
    {
        throw new BizException($"User is already in status: {newStatus}.", "USER_STATUS_UNCHANGED");
    }

    // ... logic to update user status ...
    // If another business rule is violated, throw BizException accordingly.

    _repository.Update(user);
    _repository.SaveChanges();
    return WebResponseContent.Instance.OK("User status updated successfully.");
}
```

**Corresponding API Response (if `USER_NOT_FOUND` is thrown):**
```json
// HTTP Status: 400 Bad Request
{
  "errorCode": "USER_NOT_FOUND",
  "message": "User not found with the provided ID.",
  "timestamp": "2024-07-18T12:00:00.000Z",
  "traceId": "0HMABCDEF1234567:00000001"
}
```

**Handling a Database Exception (implicitly via Service Interceptor):**
If a `SqlException` occurs during `_repository.SaveChanges()`, the `ServiceExceptionInterceptor` will catch it:
1.  Log the `SqlException` with `LogLevel.Error`, including method name and `traceId`.
2.  Wrap it in a `new SysException("An unexpected error occurred in service: UpdateUserStatus...", sqlEx)`.
3.  This `SysException` is then caught by `ExceptionHandlerMiddleWare`.

**Corresponding API Response (for the wrapped `SqlException`):**
```json
// HTTP Status: 500 Internal Server Error
{
  "errorCode": "SYSTEM_ERROR", // Or the ErrorCode from SysException
  "message": "A system error occurred. Please contact support.",
  "timestamp": "2024-07-18T12:05:00.000Z",
  "traceId": "0HMABCDEF1234568:00000002"
}
```
The detailed `SqlException` information would be in the server logs, correlated by `traceId`.

### 4.2. Throwing Exceptions in Controllers (Generally Discouraged)

It is generally discouraged to throw exceptions directly from controllers. Controllers should call services, and services should throw `BizException` or let system exceptions be wrapped by the service interceptor. If a controller *must* return an error state not covered by a service call (e.g., invalid request model before a service is called), it should ideally return an appropriate `IActionResult` (like `BadRequestObjectResult`) using the `ApiErrorResponse` structure.

If an unexpected exception were to occur directly in a controller and not be caught by any specific filter, the `ExceptionHandlerMiddleWare` would handle it as a generic unhandled exception.

## 5. Extension Guide

### 5.1. Adding New Custom Business Exception Types

If you need a more specific type of business exception beyond the generic `BizException` (though `BizException` with a unique `errorCode` is often sufficient):

1.  **Create a new exception class**:
    ```csharp
    // Example: VOL.Core/Extensions/InventoryException.cs
    using System;

    namespace VOL.Core.Extensions
    {
        public class InventoryException : BizException
        {
            public InventoryException(string message, string errorCode = "INVENTORY_ERROR")
                : base(message, errorCode)
            {
            }

            public InventoryException(string message, string errorCode, Exception innerException)
                : base(message, errorCode, innerException)
            {
            }
        }
    }
    ```
2.  **Use the new exception in your service**:
    ```csharp
    // In an inventory-related service
    if (product.Stock < requestedQuantity)
    {
        throw new InventoryException("Insufficient stock for product.", "INSUFFICIENT_STOCK");
    }
    ```
3.  **Handling**:
    *   The `ServiceExceptionInterceptor` will catch `InventoryException` (as it inherits from `BizException`) and log it appropriately.
    *   The `ExceptionHandlerMiddleWare` will also handle it as a `BizException`, returning the specified `errorCode` and `message` with an HTTP 400 status. No changes are needed in the middleware or interceptor if the new exception inherits from `BizException` or `SysException`.

### 5.2. Registering New Exception Types with Aspects (If Not Inheriting)

If you create a custom exception that does *not* inherit from `BizException` or `SysException` but still needs special handling by the middleware or interceptors, you would need to modify:
*   `VOL.Core/AOP/ServiceExceptionInterceptor.cs`: Add a new `catch` block for your custom exception type if it needs different logging or re-wrapping behavior than the generic `catch (Exception ex)`.
*   `VOL.Core/Middleware/ExceptionHandlerMiddleWare.cs`: Add a new `else if (exception is YourCustomException customEx)` block to handle it with a specific `ApiErrorResponse` format and HTTP status code.

However, the recommended approach is to use `BizException` with specific error codes for most business scenarios or inherit from `BizException` or `SysException` for clarity and to leverage existing handling logic.

---
*This document should be kept up-to-date as exception handling strategies evolve.*
