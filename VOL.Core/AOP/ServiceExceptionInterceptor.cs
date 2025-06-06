using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Microsoft.AspNetCore.Http;
using VOL.Core.Enums; // For LogLevel, LogEvent
using VOL.Core.Extensions; // For BizException, SysException
using VOL.Core.Services; // For Logger
// Potentially using VOL.Core.Utilities; for HttpContext.Current if needed and safe

namespace VOL.Core.AOP
{
    /// <summary>
    /// Interceptor for logging exceptions and wrapping them in service layer methods.
    /// This interceptor relies on services being registered with Autofac and enabling interception.
    /// It differentiates between BizException, SysException, and other unhandled exceptions,
    /// logging them appropriately and standardizing unhandled exceptions by wrapping them in SysException.
    /// </summary>
    public class ServiceExceptionInterceptor : IInterceptor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceExceptionInterceptor"/> class.
        /// </summary>
        /// <param name="httpContextAccessor">Accessor to get the current HttpContext for TraceId and other contextual information.</param>
        public ServiceExceptionInterceptor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Intercepts a method invocation to provide centralized exception handling and logging.
        /// </summary>
        /// <param name="invocation">The method invocation to intercept.</param>
        public void Intercept(IInvocation invocation)
        {
            string traceId = _httpContextAccessor?.HttpContext?.TraceIdentifier ?? "N/A";
            // TargetType can be null if intercepting an interface method not backed by a class instance (e.g. remoting proxy)
            // MethodInvocationTarget gives the method on the target object if available, otherwise Method gives the invoked interface method
            var methodInfo = invocation.MethodInvocationTarget ?? invocation.Method;
            string fullMethodName = $"{methodInfo.DeclaringType?.FullName}.{methodInfo.Name}";

            try
            {
                // Proceed with the original method call
                invocation.Proceed();

                // Handle async methods (Task and Task<T>)
                if (invocation.Method.ReturnType != null && typeof(Task).IsAssignableFrom(invocation.Method.ReturnType))
                {
                    if (invocation.Method.ReturnType.IsGenericType && invocation.Method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                    {
                        // For Task<TResult>
                        // Dynamically call InterceptAsyncTaskWithResult<TResult>
                        var resultType = invocation.Method.ReturnType.GetGenericArguments()[0];
                        var interceptAsyncMethod = typeof(ServiceExceptionInterceptor)
                            .GetMethod(nameof(InterceptAsyncTaskWithResult), BindingFlags.NonPublic | BindingFlags.Instance)
                            .MakeGenericMethod(resultType);

                        invocation.ReturnValue = interceptAsyncMethod.Invoke(this, new object[] { invocation.ReturnValue, fullMethodName, traceId });
                    }
                    else
                    {
                        // For non-generic Task
                        invocation.ReturnValue = InterceptAsync((Task)invocation.ReturnValue, fullMethodName, traceId);
                    }
                }
                // Synchronous methods' exceptions will be caught by the outer catch blocks if they throw directly.
            }
            catch (BizException bizEx)
            {
                Logger.Log(LogLevel.Warning, LogEvent.BizExceptionLog, $"Method: {fullMethodName}, TraceId: {traceId}", null, bizEx.ToString());
                throw; // Re-throw BizException as it's an expected business error to be handled by higher layers
            }
            catch (SysException sysEx)
            {
                Logger.Log(LogLevel.Error, LogEvent.SysExceptionLog, $"Method: {fullMethodName}, TraceId: {traceId}", null, sysEx.ToString());
                throw; // Re-throw SysException
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, LogEvent.Exception, $"Method: {fullMethodName}, TraceId: {traceId}", null, ex.ToString());
                // Wrap unexpected exceptions in a SysException for standardized error handling
                throw new SysException($"An unexpected error occurred in service method: {fullMethodName}. TraceId: {traceId}. See inner exception.", ex);
            }
        }

        /// <summary>
        /// Handles exceptions for asynchronous methods that return a non-generic Task.
        /// </summary>
        private async Task InterceptAsync(Task task, string fullMethodName, string traceId)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (BizException bizEx)
            {
                Logger.Log(LogLevel.Warning, LogEvent.BizExceptionLog, $"Method: {fullMethodName}, TraceId: {traceId} (async Task)", null, bizEx.ToString());
                throw;
            }
            catch (SysException sysEx)
            {
                Logger.Log(LogLevel.Error, LogEvent.SysExceptionLog, $"Method: {fullMethodName}, TraceId: {traceId} (async Task)", null, sysEx.ToString());
                throw;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, LogEvent.Exception, $"Method: {fullMethodName}, TraceId: {traceId} (async Task)", null, ex.ToString());
                throw new SysException($"An unexpected error occurred in async service method: {fullMethodName}. TraceId: {traceId}. See inner exception.", ex);
            }
        }

        /// <summary>
        /// Handles exceptions for asynchronous methods that return a generic Task&lt;TResult&gt;.
        /// </summary>
        private async Task<T> InterceptAsyncTaskWithResult<T>(Task<T> task, string fullMethodName, string traceId)
        {
            try
            {
                return await task.ConfigureAwait(false);
            }
            catch (BizException bizEx)
            {
                Logger.Log(LogLevel.Warning, LogEvent.BizExceptionLog, $"Method: {fullMethodName}, TraceId: {traceId} (async Task<T>)", null, bizEx.ToString());
                throw;
            }
            catch (SysException sysEx)
            {
                Logger.Log(LogLevel.Error, LogEvent.SysExceptionLog, $"Method: {fullMethodName}, TraceId: {traceId} (async Task<T>)", null, sysEx.ToString());
                throw;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, LogEvent.Exception, $"Method: {fullMethodName}, TraceId: {traceId} (async Task<T>)", null, ex.ToString());
                throw new SysException($"An unexpected error occurred in async service method Task<T>: {fullMethodName}. TraceId: {traceId}. See inner exception.", ex);
            }
        }
    }
}

// Note on advanced async Task<T> handling in IInterceptor:
// The Intercept method above was updated to include a more robust way of handling Task<T>
// by using reflection to call the generic InterceptAsyncTaskWithResult<T> method.
// This is a common pattern for correctly intercepting and handling exceptions from async methods
// that return generic Tasks, ensuring the result is properly awaited and exceptions are caught
// from the task's continuation.
