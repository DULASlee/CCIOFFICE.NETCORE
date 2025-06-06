using System;

namespace VOL.Core.Extensions.Response
{
    /// <summary>
    /// Represents a standardized error response format for APIs.
    /// </summary>
    public class ApiErrorResponse
    {
        /// <summary>
        /// Gets or sets the specific error code.
        /// For business exceptions, this would be the business error code.
        /// For system exceptions, this might be a generic system error code or a more specific one.
        /// </summary>
        public string ErrorCode { get; set; }

        /// <summary>
        /// Gets or sets the human-readable error message.
        /// Should be user-friendly for business exceptions.
        /// Can be a more generic message for system exceptions in production to avoid leaking details.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the error occurred.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the trace ID for correlating this error with logs.
        /// Typically populated using HttpContext.TraceIdentifier.
        /// </summary>
        public string TraceId { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiErrorResponse"/> class.
        /// </summary>
        /// <param name="errorCode">The error code.</param>
        /// <param name="message">The error message.</param>
        /// <param name="traceId">The trace ID.</param>
        public ApiErrorResponse(string errorCode, string message, string traceId)
        {
            ErrorCode = errorCode;
            Message = message;
            TraceId = traceId;
            Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Creates a new ApiErrorResponse with a specific timestamp.
        /// Useful if the error occurrence time needs to be precise and captured earlier.
        /// </summary>
        /// <param name="errorCode">The error code.</param>
        /// <param name="message">The error message.</param>
        /// <param name="traceId">The trace ID.</param>
        /// <param name="timestamp">The specific timestamp of the error.</param>
        public ApiErrorResponse(string errorCode, string message, string traceId, DateTime timestamp)
        {
            ErrorCode = errorCode;
            Message = message;
            TraceId = traceId;
            Timestamp = timestamp;
        }
    }
}
