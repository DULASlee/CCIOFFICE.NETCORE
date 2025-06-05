using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using VOL.Core.Services;

namespace VOL.Core.Utilities
{
    public class SecurityEncDecrypt
    {
        #region 
        private static byte[] Keys = { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F };
        /// <summary> 
        /// DES加密字符串 
        /// </summary> 
        /// <param name="encryptString">待加密的字符串</param> 
        /// <param name="encryptKey">加密密钥,要求为16位</param> 
        /// <returns>加密成功返回加密后的字符串，失败返回源串</returns> 

        public static string EncryptDES(string encryptString, string encryptKey)
        {
            try
            {
                byte[] rgbKey = Encoding.UTF8.GetBytes(encryptKey.Substring(0, 16));
                byte[] rgbIV = Keys;
                byte[] inputByteArray = Encoding.UTF8.GetBytes(encryptString);
                using (var DCSP = Aes.Create()) // Aes.Create() returns an IDisposable object
                using (MemoryStream mStream = new MemoryStream())
                using (CryptoStream cStream = new CryptoStream(mStream, DCSP.CreateEncryptor(rgbKey, rgbIV), CryptoStreamMode.Write))
                {
                    cStream.Write(inputByteArray, 0, inputByteArray.Length);
                    cStream.FlushFinalBlock();
                    return Convert.ToBase64String(mStream.ToArray());
                }
            }
            catch (Exception ex)
            {
                // Consider logging the exception here before throwing a new one, or rethrow original
                Logger.Error(Enums.LogEvent.Exception, "数据库密码加密异常", encryptString?.Length.ToString(), null, ex); // Log original exception
                throw new Exception("数据库密码加密异常: " + ex.Message, ex); // Wrap original exception
            }
        }

        /// <summary> 
        /// DES解密字符串 
        /// </summary> 
        /// <param name="decryptString">待解密的字符串</param> 
        /// <param name="decryptKey">解密密钥,要求为16位,和加密密钥相同</param> 
        /// <returns>解密成功返回解密后的字符串，失败返源串</returns> 

        public static string DecryptDES(string decryptString, string decryptKey)
        {
            try
            {
                byte[] rgbKey = Encoding.UTF8.GetBytes(decryptKey.Substring(0, 16));
                byte[] rgbIV = Keys;
                byte[] inputByteArray = Convert.FromBase64String(decryptString);
                using (var DCSP = Aes.Create()) // Aes.Create() returns an IDisposable object
                using (MemoryStream mStream = new MemoryStream())
                using (CryptoStream cStream = new CryptoStream(mStream, DCSP.CreateDecryptor(rgbKey, rgbIV), CryptoStreamMode.Write))
                {
                    // Byte[] inputByteArrays = new byte[inputByteArray.Length]; // This line seems unused
                    cStream.Write(inputByteArray, 0, inputByteArray.Length);
                    cStream.FlushFinalBlock();
                    return Encoding.UTF8.GetString(mStream.ToArray());
                }
            }
            catch (Exception ex)
            {
               // Avoid logging the key. Log length of string if it's not sensitive, or a generic message.
               Logger.Error(Enums.LogLevel.Error, Enums.LogEvent.Exception, "DES解密失败", $"StringLength: {decryptString?.Length}", null, ex);
               // Returning null on decrypt failure can be problematic. Consider throwing an exception
               // or having the method return a TryDecrypt pattern (bool success, out string result).
               // For now, preserving existing behavior of returning null.
               return null;
            }
        }
        #endregion
    }
}
