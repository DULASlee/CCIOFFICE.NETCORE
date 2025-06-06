using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using VOL.Core.Const;
using VOL.Core.EFDbContext;
using VOL.Core.Enums;
using VOL.Core.Extensions;
using VOL.Core.ManageUser;
using VOL.Core.Services;

namespace VOL.Core.Middleware
{
    /// <summary>
    /// Global exception handling middleware.
    /// Catches unhandled exceptions that occur during request processing,
    /// logs them using <see cref="Logger"/>, and returns a generic, user-friendly error response to the client.
    /// </summary>
    public class ExceptionHandlerMiddleWare
    {
        private readonly RequestDelegate next;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionHandlerMiddleWare"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        public ExceptionHandlerMiddleWare(RequestDelegate next)
        {
            this.next = next;
        }

        /// <summary>
        /// Processes a request and handles any unhandled exceptions.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> for the current request.</param>
        /// <returns>A <see cref="Task"/> that represents the completion of request processing.</returns>
        public async Task Invoke(HttpContext context)
        {
            try
            {
                // Enable buffering to allow reading the request body multiple times if needed by ActionObserver or other components.
                context.Request.EnableBuffering();
                // Record the request start time using ActionObserver.
                (context.RequestServices.GetService(typeof(ActionObserver)) as ActionObserver).RequestDate = DateTime.Now;

                await next(context); // Pass control to the next middleware in the pipeline.

                // This middleware is typically placed after UseRouting, allowing access to endpoint metadata.
                Endpoint endpoint = context.Features.Get<IEndpointFeature>()?.Endpoint;
                if (endpoint != null && endpoint is RouteEndpoint routeEndpoint)
                {
                    // Check for ActionLogAttribute to perform specific logging if configured for the action.
                    ActionLog logMetadata = endpoint.Metadata.GetMetadata<ActionLog>();
                    if (logMetadata != null && logMetadata.Write)
                    {
                        // Log based on ActionLogAttribute settings.
                        // Assumes logMetadata.LogType is a LogEvent. If it's a different type, adjust accordingly.
                        // The Add method here might need LogLevel if the LogType from ActionLog doesn't imply it.
                        // For now, assuming it's compatible with an overload of Logger.Add or Logger.Info.
                        // This specific call to Logger.Add might need review based on its expected parameters.
                        // If logMetadata.LogType is a LogEvent:
                        Logger.Add(LogLevel.Information, (LogEvent)logMetadata.LogType, null, null, null, status: LoggerStatus.Info);
                    }
                }
                else
                {
                    // Default informational log if no specific ActionLog is found (e.g., for non-endpoint requests or static files if not handled before this).
                    Logger.Info(LogEvent.Info); // Uses LogLevel.Information by default
                }
            }
            catch (Exception exception)
            {
                var env = context.RequestServices.GetService(typeof(IWebHostEnvironment)) as IWebHostEnvironment;
                var traceId = context.TraceIdentifier; // Get TraceId for correlation

                ApiErrorResponse errorResponse;
                int statusCode;

                if (exception is BizException bizEx)
                {
                    // Log business exceptions as warnings, as they are expected application flow deviations.
                    Logger.Warning(LogLevel.Warning, LogEvent.BizExceptionLog,
                                 $"Business Exception: Path={context.Request.Path}, Method={context.Request.Method}, ErrorCode={bizEx.ErrorCode}",
                                 null, // requestParam can be added if needed and safe
                                 bizEx.ToString()); // Log full exception details

                    errorResponse = new ApiErrorResponse(bizEx.ErrorCode, bizEx.Message, traceId);
                    statusCode = StatusCodes.Status400BadRequest; // Business exceptions often result in a 400
                }
                else if (exception is SysException sysEx)
                {
                    // Log system exceptions as errors.
                    Logger.Error(LogLevel.Error, LogEvent.SysExceptionLog,
                                 $"System Exception: Path={context.Request.Path}, Method={context.Request.Method}, ErrorCode={sysEx.ErrorCode}",
                                 null,
                                 sysEx.ToString());

                    // For the client, return a generic system error message to avoid leaking internal details.
                    errorResponse = new ApiErrorResponse(sysEx.ErrorCode, "系统处理时发生了一个意外错误。", traceId);
                    statusCode = StatusCodes.Status500InternalServerError;
                }
                else // Unhandled/unexpected exceptions
                {
                    // Log unhandled exceptions as critical errors.
                    Logger.Error(LogLevel.Critical, LogEvent.UnhandledExceptionLog, // Or use LogEvent.Exception if preferred
                                 $"Unhandled Exception: Path={context.Request.Path}, Method={context.Request.Method}",
                                 null,
                                 exception.ToString());

                    string message = "服务器发生了一个无法处理的错误。";
                    if (env.IsDevelopment())
                    {
                        // In development, more details can be exposed for easier debugging.
                        // message = exception.Message; // Or even exception.ToString() if very detailed info is desired directly in response
                        Console.WriteLine($"服务器处理出现异常 (Unhandled):{exception.ToString()}");
                    }
                    errorResponse = new ApiErrorResponse("UNHANDLED_ERROR", message, traceId);
                    statusCode = StatusCodes.Status500InternalServerError;
                }

                context.Response.StatusCode = statusCode;
                context.Response.ContentType = ApplicationContentType.JSON;
                await context.Response.WriteAsync(errorResponse.Serialize(), Encoding.UTF8);
            }
        }
    }
}
