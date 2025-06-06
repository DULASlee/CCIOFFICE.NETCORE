using System;

namespace VOL.Core.Extensions
{
    /// <summary>
    /// Base class for custom exceptions in the application.
    /// It includes an application-specific error code.
    /// </summary>
    public abstract class BaseAppException : Exception
    {
        /// <summary>
        /// Gets the application-specific error code.
        /// This code can be used by clients to identify the type of error programmatically.
        /// </summary>
        public string ErrorCode { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseAppException"/> class with a specified error message and error code.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="errorCode">The application-specific error code.</param>
        protected BaseAppException(string message, string errorCode) : base(message)
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseAppException"/> class with a specified error message,
        /// error code, and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="errorCode">The application-specific error code.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference
        /// if no inner exception is specified.</param>
        protected BaseAppException(string message, string errorCode, Exception innerException) : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }
}
