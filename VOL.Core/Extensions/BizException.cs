using System;

namespace VOL.Core.Extensions
{
    /// <summary>
    /// Represents business logic related errors that occur during application execution.
    /// These exceptions are typically caught and translated into user-friendly error messages
    /// with specific business error codes, allowing clients to handle them programmatically if needed.
    /// </summary>
    public class BizException : BaseAppException
    {
        /// <summary>
        /// A general business error code if a more specific one is not provided.
        /// </summary>
        public const string DefaultBusinessErrorCode = "BIZ_ERROR";

        /// <summary>
        /// Initializes a new instance of the <see cref="BizException"/> class
        /// with a specified error message and a default business error code.
        /// </summary>
        /// <param name="message">A message that describes the business error.</param>
        public BizException(string message) : base(message, DefaultBusinessErrorCode)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BizException"/> class
        /// with a specified error message and a specific business error code.
        /// </summary>
        /// <param name="message">A message that describes the business error.</param>
        /// <param name="errorCode">A business-specific error code (e.g., "INSUFFICIENT_STOCK", "USER_NOT_FOUND").</param>
        public BizException(string message, string errorCode) : base(message, errorCode ?? DefaultBusinessErrorCode)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BizException"/> class
        /// with a specified error message, a default business error code, and a reference to the inner exception.
        /// </summary>
        /// <param name="message">A message that describes the business error.</param>
        /// <param name="innerException">The exception that is the cause of the current business exception.</param>
        public BizException(string message, Exception innerException) : base(message, DefaultBusinessErrorCode, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BizException"/> class
        /// with a specified error message, a specific business error code, and a reference to the inner exception.
        /// </summary>
        /// <param name="message">A message that describes the business error.</param>
        /// <param name="errorCode">A business-specific error code.</param>
        /// <param name="innerException">The exception that is the cause of the current business exception.</param>
        public BizException(string message, string errorCode, Exception innerException) : base(message, errorCode ?? DefaultBusinessErrorCode, innerException)
        {
        }
    }
}
