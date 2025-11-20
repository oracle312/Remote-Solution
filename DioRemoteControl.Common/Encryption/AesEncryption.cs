using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;

namespace DioRemoteControl.Common.Encryption
{
    /// <summary>
    /// AES-256 암호화/복호화 클래스
    /// </summary>
    public class AesEncryption
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;

        /// <summary>
        /// 생성자 - 키와 IV를 지정
        /// </summary>
        public AesEncryption(string key, string iv)
        {
            _key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32)); // 32 bytes for AES-256
            _iv = Encoding.UTF8.GetBytes(iv.PadRight(16).Substring(0, 16));   // 16 bytes for IV
        }

        /// <summary>
        /// 세션 ID로부터 키와 IV 생성
        /// </summary>
        public static AesEncryption FromSessionId(string sessionId)
        {
            // 세션 ID를 해시하여 키와 IV 생성
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(sessionId));
                var key = Convert.ToBase64String(hash).Substring(0, 32);
                var iv = Convert.ToBase64String(hash).Substring(0, 16);
                return new AesEncryption(key, iv);
            }
        }

        /// <summary>
        /// 문자열 암호화
        /// </summary>
        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = _key;
                    aes.IV = _iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (var encryptor = aes.CreateEncryptor())
                    using (var msEncrypt = new MemoryStream())
                    {
                        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        using (var swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                        return Convert.ToBase64String(msEncrypt.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Encryption failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 문자열 복호화
        /// </summary>
        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = _key;
                    aes.IV = _iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (var decryptor = aes.CreateDecryptor())
                    using (var msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText)))
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    using (var srDecrypt = new StreamReader(csDecrypt))
                    {
                        return srDecrypt.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Decryption failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 바이트 배열 암호화
        /// </summary>
        public byte[] EncryptBytes(byte[] plainBytes)
        {
            if (plainBytes == null || plainBytes.Length == 0)
                return plainBytes;

            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = _key;
                    aes.IV = _iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (var encryptor = aes.CreateEncryptor())
                    using (var msEncrypt = new MemoryStream())
                    {
                        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            csEncrypt.Write(plainBytes, 0, plainBytes.Length);
                            csEncrypt.FlushFinalBlock();
                        }
                        return msEncrypt.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Byte encryption failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 바이트 배열 복호화
        /// </summary>
        public byte[] DecryptBytes(byte[] cipherBytes)
        {
            if (cipherBytes == null || cipherBytes.Length == 0)
                return cipherBytes;

            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = _key;
                    aes.IV = _iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (var decryptor = aes.CreateDecryptor())
                    using (var msDecrypt = new MemoryStream(cipherBytes))
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    using (var msPlain = new MemoryStream())
                    {
                        csDecrypt.CopyTo(msPlain);
                        return msPlain.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Byte decryption failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Base64 인코딩된 데이터 암호화
        /// </summary>
        public string EncryptBase64(string base64Data)
        {
            if (string.IsNullOrEmpty(base64Data))
                return base64Data;

            var bytes = Convert.FromBase64String(base64Data);
            var encrypted = EncryptBytes(bytes);
            return Convert.ToBase64String(encrypted);
        }

        /// <summary>
        /// Base64 인코딩된 암호화 데이터 복호화
        /// </summary>
        public string DecryptBase64(string encryptedBase64)
        {
            if (string.IsNullOrEmpty(encryptedBase64))
                return encryptedBase64;

            var bytes = Convert.FromBase64String(encryptedBase64);
            var decrypted = DecryptBytes(bytes);
            return Convert.ToBase64String(decrypted);
        }
    }
}
