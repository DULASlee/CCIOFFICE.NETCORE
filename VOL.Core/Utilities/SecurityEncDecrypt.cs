using Microsoft.Extensions.Logging; // Not directly used, consider removing if VOL.Core.Services.Logger is sufficient
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using VOL.Core.Services; // Required for Logger
using VOL.Core.Enums; // Required for LogLevel and LogEvent

namespace VOL.Core.Utilities
{
    /// <summary>
    /// Provides static methods for AES (Advanced Encryption Standard) encryption and decryption of strings.
    /// <para>
    /// IMPORTANT: Although method names (<see cref="EncryptDES"/>, <see cref="DecryptDES"/>) suggest DES (Data Encryption Standard),
    /// the actual implementation uses <see cref="Aes.Create()"/>, which defaults to AES.
    /// </para>
    /// <para>
    /// Security Note: This implementation uses a hardcoded static Initialization Vector (IV).
    /// For robust security, especially in CBC mode (which is default for <see cref="Aes"/>),
    /// a unique, cryptographically random IV should be generated for each encryption operation and stored/transmitted alongside the ciphertext.
    /// Reusing IVs with the same key can severely compromise security.
    /// The key derivation from a string is also basic. Consider using a standard key derivation function like PBKDF2 if keys are password-based.
    /// </para>
    /// </summary>
    public class SecurityEncDecrypt
    {
        #region AES Encryption/Decryption (Named DES in methods)

        // Default static key material used as the Initialization Vector (IV).
        // WARNING: This is a security vulnerability. A unique IV should be generated for each encryption operation.
        // For AES-128 (which is likely the default for Aes.Create() if the key is 16 bytes), the IV should be 16 bytes.
        private static byte[] Keys = { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F };

        /// <summary> 
        /// Encrypts a string using AES.
        /// The encryption key is derived from the first 16 bytes of the UTF-8 representation of the provided <paramref name="encryptKey"/>.
        /// The Initialization Vector (IV) is a hardcoded static value, which is not secure for production use.
        /// </summary> 
        /// <param name="encryptString">The string to encrypt.</param>
        /// <param name="encryptKey">The encryption key. The first 16 characters are used to derive the actual 128-bit AES key.
        /// Ensure this key is strong and managed securely.</param>
        /// <returns>The Base64 encoded encrypted string. Returns an empty string if <paramref name="encryptString"/> is null or empty.</returns>
        /// <exception cref="Exception">Throws a new exception wrapping the original if encryption fails (e.g., invalid key length after processing), after logging the error.
        /// The original sensitive <paramref name="encryptString"/> is not included in the log message.</exception>
        public static string EncryptDES(string encryptString, string encryptKey)
        {
            if (string.IsNullOrEmpty(encryptString)) return ""; // Handle empty input gracefully

            try
            {
                // Derive a 16-byte (128-bit) key from the input encryptKey string.
                // This is a simplistic approach; consider more robust key derivation if the key is, e.g., a user password.
                byte[] rgbKey = Encoding.UTF8.GetBytes(encryptKey.Substring(0, 16));
                // WARNING: Using a static IV (Keys) is insecure. A unique IV should be generated per encryption.
                byte[] rgbIV = Keys;
                byte[] inputByteArray = Encoding.UTF8.GetBytes(encryptString);

                // The 'using' statements ensure that these IDisposable cryptographic objects
                // are correctly disposed of, even if an exception occurs, preventing resource leaks.
                using (var aesAlg = Aes.Create()) // Creates an AES algorithm instance (e.g., AES-256 with CBC mode by default if key allows). Key size is set by aesAlg.Key.
                using (MemoryStream mStream = new MemoryStream()) // Stream to hold the encrypted data in memory.
                // CryptoStream links data streams to cryptographic transformations.
                // It uses the encryptor created from aesAlg with the derived key and static IV.
                using (CryptoStream cStream = new CryptoStream(mStream, aesAlg.CreateEncryptor(rgbKey, rgbIV), CryptoStreamMode.Write))
                {
                    cStream.Write(inputByteArray, 0, inputByteArray.Length);
                    // FlushFinalBlock is crucial as it finalizes the encryption process,
                    // padding the last block if necessary and writing any remaining buffered data.
                    cStream.FlushFinalBlock();
                    return Convert.ToBase64String(mStream.ToArray()); // Return encrypted data as a Base64 string.
                }
            }
            catch (Exception ex)
            {
                // Log the exception. Avoid logging potentially sensitive data like encryptString or encryptKey.
                Logger.Error(LogLevel.Error, LogEvent.Exception, "AES Encryption failed (EncryptDES method)", $"InputStringLength: {encryptString?.Length}", null, ex);
                // Wrap the original exception to provide context to the caller, without losing the original stack trace.
                throw new Exception("数据库密码加密异常 (AES Encryption Error): " + ex.Message, ex);
            }
        }

        /// <summary> 
        /// Decrypts a Base64 encoded string that was encrypted using AES (via <see cref="EncryptDES"/>).
        /// The decryption key is derived from the first 16 bytes of the UTF-8 representation of the provided <paramref name="decryptKey"/>.
        /// Uses the same static Initialization Vector (IV) as encryption, which is necessary for decryption but highlights the insecurity of a static IV.
        /// </summary> 
        /// <param name="decryptString">The Base64 encoded string to decrypt.</param>
        /// <param name="decryptKey">The decryption key. Must match the key used for encryption (first 16 characters are used).</param>
        /// <returns>The decrypted string. Returns null if decryption fails (e.g., invalid key, corrupted data, padding errors).
        /// Callers should check for null to handle decryption failures.</returns>
        public static string DecryptDES(string decryptString, string decryptKey)
        {
            if (string.IsNullOrEmpty(decryptString)) return null; // Handle empty input gracefully

            try
            {
                // Derive the 16-byte (128-bit) key. Must be identical to the key used for encryption.
                byte[] rgbKey = Encoding.UTF8.GetBytes(decryptKey.Substring(0, 16));
                // WARNING: Uses the same static IV as encryption.
                byte[] rgbIV = Keys;
                byte[] inputByteArray = Convert.FromBase64String(decryptString); // Convert Base64 string back to bytes.

                // Ensure IDisposable cryptographic objects are properly disposed using 'using' statements.
                using (var aesAlg = Aes.Create())
                using (MemoryStream mStream = new MemoryStream()) // Memory stream to hold decrypted bytes.
                // CryptoStream in Read mode would read from an encrypted stream and decrypt.
                // Here, it's in Write mode, writing the ciphertext to be decrypted, and the decrypted plaintext goes to mStream.
                using (CryptoStream cStream = new CryptoStream(mStream, aesAlg.CreateDecryptor(rgbKey, rgbIV), CryptoStreamMode.Write))
                {
                    cStream.Write(inputByteArray, 0, inputByteArray.Length);
                    // FlushFinalBlock is crucial for decryption to process the final block of data and remove padding.
                    cStream.FlushFinalBlock();
                    return Encoding.UTF8.GetString(mStream.ToArray()); // Convert decrypted bytes back to a UTF-8 string.
                }
            }
            catch (Exception ex)
            {
               // Log the decryption failure. Avoid logging the decryptKey for security.
               // The length of the input string might be logged for context if it's not sensitive itself.
               Logger.Error(LogLevel.Error, LogEvent.Exception, "AES Decryption failed (DecryptDES method)", $"InputStringLength: {decryptString?.Length}", null, ex);

               // Original behavior was to return null on failure. This can hide the specific nature of the error.
               // For more robust error handling, callers should be aware that null indicates a failure.
               // Alternatives: throw a specific CryptographicException, or use a TryDecrypt pattern (out bool success).
               // For this refactoring, preserving the existing return null behavior.
               return null;
            }
        }
        #endregion
    }
}
