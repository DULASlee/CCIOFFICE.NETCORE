using System;

namespace VOL.Core.Extensions
{
    /// <summary>
    /// Represents system-level errors or unexpected exceptions that are not classified as business rule violations.
    /// These exceptions typically wrap underlying infrastructure issues (e.g., database errors, network errors)
    /// when they cannot be handled specifically or translated into a <see cref="BizException"/>.
    /// These should generally result in a generic error message to the end-user but detailed logging on the server.
    /// </summary>
    public class SysException : BaseAppException
    {
        /// <summary>
        /// Default system error code used when a more specific one is not provided.
        /// </summary>
        public const string DefaultSystemErrorCode = "SYSTEM_ERROR";

        /// <summary>
        /// Initializes a new instance of the <see cref="SysException"/> class
        /// with a specified message and the default system error code.
        /// </summary>
        /// <param name="message">A message that describes the system error. This message is primarily for logging
        /// and may not be suitable for direct display to the end-user in production environments.</param>
        public SysException(string message) : base(message, DefaultSystemErrorCode)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SysException"/> class
        /// with a specified error message and a specific system error code.
        /// </summary>
        /// <param name="message">A message that describes the system error.</param>
        /// <param name="errorCode">A specific system error code, if applicable (e.g., "DB_CONNECTION_FAILED"). Defaults to <see cref="DefaultSystemErrorCode"/> if null.</param>
        public SysException(string message, string errorCode) : base(message, errorCode ?? DefaultSystemErrorCode)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SysException"/> class
        /// with a specified message, the default system error code, and a reference to the inner exception
        /// that is the cause of this exception.
        /// </summary>
        /// <param name="message">A message that describes the system error.</param>
        /// <param name="innerException">The exception that is the cause of the current system exception.</param>
        public SysException(string message, Exception innerException) : base(message, DefaultSystemErrorCode, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SysException"/> class
        /// with a specified error message, a specific system error code, and a reference to the inner exception.
        /// </summary>
        /// <param name="message">A message that describes the system error.</param>
        /// <param name="errorCode">A specific system error code. Defaults to <see cref="DefaultSystemErrorCode"/> if null.</param>
        /// <param name="innerException">The exception that is the cause of the current system exception.</param>
        public SysException(string message, string errorCode, Exception innerException) : base(message, errorCode ?? DefaultSystemErrorCode, innerException)
        {
        }
    }
}
