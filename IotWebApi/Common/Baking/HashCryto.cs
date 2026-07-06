using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System;
using System.Text;

namespace IotWebApi.Common.Baking
{
    /// <summary>
    /// 哈希算法类型
    /// </summary>
    public enum HashAlgorithmType
    {
        /// <summary>MD5 Hash</summary>
        MD5,
        /// <summary>SHA1 - A 160 bit Secure Algorithm Hash</summary>
        SHA1,
        /// <summary>SHA2 - A 256 bit Secure Algorithm Hash</summary>
        SHA256,
        /// <summary>SHA3 - A 384 bit Secure Algorithm Hash</summary>
        SHA384,
        /// <summary>SHA5 - A 512 bit Secure Algorithm Hash</summary>
        SHA512
    }

    /// <summary>
    /// 哈希加密算法，默认编码Encoding.Default
    /// </summary>
    public static class HashCryto
    {
        #region 计算文件流的哈希值

        /// <summary>
        /// 计算文件流的哈希值
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="hashAlgorithmType"></param>
        /// <returns></returns>
        public static byte[] GetHash(Stream stream, HashAlgorithmType hashAlgorithmType)
        {
            StrCheck.NotNull(stream, "stream");

            HashAlgorithm hashAlgorithm = CreateHashAlgorithmProvider(hashAlgorithmType);
            byte[] result = hashAlgorithm.ComputeHash(stream);
            hashAlgorithm.Clear();
            stream.Close();

            return result;
        }

        /// <summary>
        /// 计算文件流的哈希值并将其转换为字符串
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="hashAlgorithmType"></param>
        /// <returns></returns>
        public static string GetHash2String(Stream stream, HashAlgorithmType hashAlgorithmType)
        {
            StrCheck.NotNull(stream, "fileStream");

            string result = string.Empty;
            byte[] bytes = GetHash(stream, hashAlgorithmType);

            foreach (byte b in bytes)
            {
                result += Convert.ToString(b, 16).ToUpper(CultureInfo.InvariantCulture).PadLeft(2, '0');
            }

            return result;
        }

        /// <summary>
        /// 计算文件流的哈希值并将其转换为使用Base64编码的字符串
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="hashAlgorithmType"></param>
        /// <returns></returns>
        public static string GetHash2Base64(Stream stream, HashAlgorithmType hashAlgorithmType)
        {
            StrCheck.NotNull(stream, "stream");

            byte[] bytes = GetHash(stream, hashAlgorithmType);
            string result = Convert.ToBase64String(bytes);

            return result;
        }

        #endregion

        #region 计算字节数组的哈希值

        /// <summary>
        /// 计算字节数组的哈希值
        /// </summary>
        /// <param name="data"></param>
        /// <param name="hashAlgorithmType"></param>
        /// <returns></returns>
        public static byte[] GetHash(byte[] data, HashAlgorithmType hashAlgorithmType)
        {
            StrCheck.NotNull(data, "data");

            HashAlgorithm hashAlgorithm = CreateHashAlgorithmProvider(hashAlgorithmType);
            byte[] result = hashAlgorithm.ComputeHash(data);
            hashAlgorithm.Clear();

            return result;
        }

        /// <summary>
        /// 计算字节数组的哈希值并将其转换为字符串
        /// </summary>
        /// <param name="data"></param>
        /// <param name="hashAlgorithmType"></param>
        /// <returns></returns>
        public static string GetHash2String(byte[] data, HashAlgorithmType hashAlgorithmType)
        {
            StrCheck.NotNull(data, "data");

            string result = string.Empty;
            byte[] bytes = GetHash(data, hashAlgorithmType);

            foreach (byte b in bytes)
            {
                result += Convert.ToString(b, 16).ToUpper(CultureInfo.InvariantCulture).PadLeft(2, '0');
            }

            return result;
        }

        /// <summary>
        /// 计算字节数组的哈希值并将其转换为使用Base64编码的字符串
        /// </summary>
        /// <param name="data"></param>
        /// <param name="hashAlgorithmType"></param>
        /// <returns></returns>
        public static string GetHash2Base64(byte[] data, HashAlgorithmType hashAlgorithmType)
        {
            StrCheck.NotNull(data, "data");

            byte[] bytes = GetHash(data, hashAlgorithmType);
            string result = Convert.ToBase64String(bytes);

            return result;
        }

        #endregion

        #region 计算字符串的哈希值

        /// <summary>
        /// 计算字符串的哈希值
        /// </summary>
        /// <param name="s"></param>
        /// <param name="hashAlgorithmType"></param>
        /// <returns></returns>
        public static byte[] GetHash(string s, HashAlgorithmType hashAlgorithmType)
        {
            return GetHash(s, hashAlgorithmType, Encoding.Default);
        }

        /// <summary>
        /// 计算字符串的哈希值
        /// </summary>
        /// <param name="s"></param>
        /// <param name="hashAlgorithmType"></param>
        /// <param name="encoding">指定字符串的编码</param>
        /// <returns></returns>
        public static byte[] GetHash(string s, HashAlgorithmType hashAlgorithmType, Encoding encoding)
        {
            StrCheck.NotNullOrEmpty(s, "s");

            byte[] data = encoding.GetBytes(s);
            return GetHash(data, hashAlgorithmType);
        }

        /// <summary>
        /// 计算字符串的哈希值并将其转换为字符串
        /// </summary>
        /// <param name="s"></param>
        /// <param name="hashAlgorithmType"></param>
        /// <returns></returns>
        public static string GetHash2String(string s, HashAlgorithmType hashAlgorithmType)
        {
            return GetHash2String(s, hashAlgorithmType, Encoding.Default);
        }

        /// <summary>
        /// 计算字符串的哈希值并将其转换为字符串
        /// </summary>
        /// <param name="s"></param>
        /// <param name="hashAlgorithmType"></param>
        /// <param name="encoding">指定字符串的编码</param>
        /// <returns></returns>
        public static string GetHash2String(string s, HashAlgorithmType hashAlgorithmType, Encoding encoding)
        {
            StrCheck.NotNullOrEmpty(s, "s");

            string result = string.Empty;
            byte[] bytes = GetHash(s, hashAlgorithmType, encoding);

            foreach (byte b in bytes)
            {
                result += Convert.ToString(b, 16).ToUpper(CultureInfo.InvariantCulture).PadLeft(2, '0');
            }

            return result;
        }

        /// <summary>
        /// 计算字符串的哈希值并将其转换为使用Base64编码的字符串
        /// </summary>
        /// <param name="s"></param>
        /// <param name="hashAlgorithmType"></param>
        /// <returns></returns>
        public static string GetHash2Base64(string s, HashAlgorithmType hashAlgorithmType)
        {
            return GetHash2Base64(s, hashAlgorithmType, Encoding.Default);
        }

        /// <summary>
        /// 计算字符串的哈希值并将其转换为使用Base64编码的字符串
        /// </summary>
        /// <param name="s"></param>
        /// <param name="hashAlgorithmType"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static string GetHash2Base64(string s, HashAlgorithmType hashAlgorithmType, Encoding encoding)
        {
            StrCheck.NotNullOrEmpty(s, "s");

            byte[] data = GetHash(s, hashAlgorithmType, encoding);
            string result = Convert.ToBase64String(data);

            return result;
        }

        #endregion

        /// <summary>
        /// 创建一个哈希算法提供者实例
        /// </summary>
        /// <param name="hashAlgorithmType"></param>
        /// <returns></returns>
        public static HashAlgorithm CreateHashAlgorithmProvider(HashAlgorithmType hashAlgorithmType)
        {
            HashAlgorithm hashAlgorithm = null;

            switch (hashAlgorithmType)
            {
                case HashAlgorithmType.MD5:
                    hashAlgorithm = MD5.Create();
                    break;
                case HashAlgorithmType.SHA1:
                    hashAlgorithm = SHA1.Create();
                    break;
                case HashAlgorithmType.SHA256:
                    hashAlgorithm = SHA256.Create();
                    break;
                case HashAlgorithmType.SHA384:
                    hashAlgorithm = SHA384.Create();
                    break;
                case HashAlgorithmType.SHA512:
                    hashAlgorithm = SHA512.Create();
                    break;
                default:
                    hashAlgorithm = MD5.Create();
                    break;
            }

            return hashAlgorithm;
        }
    }
}
