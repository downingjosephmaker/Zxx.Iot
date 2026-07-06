using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;

namespace IotWebApi
{
    public class EncryptsHelper
    {
        /// <summary>
        /// 令牌加解密缺省密钥(读取配置 DefaultValues:DesKey,未配置时保持历史默认值兼容存量令牌)
        /// </summary>
        private static readonly string DESKey = AppSetting.GetConfig("DefaultValues:DesKey") ?? "IotWebApi";
        /// <summary> 
        /// 使用缺省密钥字符串加密 
        /// </summary> 
        /// <param name="key">明文</param> 
        /// <returns>密文</returns> 
        public static string Encrypt(string key)
        {
            return MEncrypt(key, DESKey);
        }

        /// <summary>
        /// 加密
        /// </summary>
        /// <param name="key">明文</param>
        /// <param name="secret">密文</param>
        /// <returns></returns>
        public static string Encrypt(string key, string secret)
        {
            return MEncrypt(key, secret);
        }

        /// <summary> 
        /// 使用缺省密钥解密 
        /// </summary> 
        /// <param name="key">密文</param> 
        /// <returns>明文</returns> 
        public static string Decrypt(string key)
        {
            return MDecrypt(key, DESKey);
        }

        /// <summary>
        /// 加密
        /// </summary>
        /// <param name="key">明文</param>
        /// <param name="secret">密文</param>
        /// <returns></returns>
        public static string Decrypt(string key, string secret)
        {
            return MDecrypt(key, secret);
        }

        /// <summary> 
        /// 加密数据 
        /// </summary> 
        /// <param name="Text"></param> 
        /// <param name="sKey"></param> 
        /// <returns></returns> 
        private static string MEncrypt(string Text, string sKey)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(Text);
            using (var des = DES.Create())
            {
                List<byte> keyBytes = new List<byte>();
                var sbuff = ASCIIEncoding.ASCII.GetBytes(sKey).ToList();
                if (sbuff.Count <= 8)
                {
                    keyBytes.AddRange(sbuff);
                    for (int i = sbuff.Count; i < 8; i++)
                    {
                        keyBytes.Add(0);
                    }
                }
                else if (sbuff.Count > 8)
                {
                    keyBytes.AddRange(sbuff.GetRange(0, 8));
                }
                des.Key = keyBytes.ToArray();
                des.IV = des.Key;
                using (var ms = new System.IO.MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(inputBytes, 0, inputBytes.Length);
                        cs.FlushFinalBlock();
                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }

        }

        /// <summary> 
        /// 解密数据 
        /// </summary> 
        /// <param name="text"></param> 
        /// <param name="sKey"></param> 
        /// <returns></returns> 
        private static string MDecrypt(string text, string sKey)
        {
            byte[] inputBytes = Convert.FromBase64String(text);
            using (var des = DES.Create())
            {
                List<byte> keyBytes = new List<byte>();
                var sbuff = ASCIIEncoding.ASCII.GetBytes(sKey).ToList();
                if (sbuff.Count <= 8)
                {
                    keyBytes.AddRange(sbuff);
                    for (int i = sbuff.Count; i < 8; i++)
                    {
                        keyBytes.Add(0);
                    }
                }
                else if (sbuff.Count > 8)
                {
                    keyBytes.AddRange(sbuff.GetRange(0, 8));
                }
                des.Key = keyBytes.ToArray();
                des.IV = des.Key;
                using (var ms = new System.IO.MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(inputBytes, 0, inputBytes.Length);
                        cs.FlushFinalBlock();
                        return Encoding.UTF8.GetString(ms.ToArray());
                    }
                }
            }
        }

        /// <summary> 
        /// 返回16位Md5加密结果
        /// </summary> 
        /// <param name="original">数据源</param> 
        /// <param name="issmall">是否小写(默认是)</param> 
        /// <returns>摘要</returns> 
        public static string MD5Make16(string original, bool issmall = true)
        {
            //将要加密的字符串转换成字节数组
            byte[] strbt = Encoding.UTF8.GetBytes(original);
            var byteArray = MakeMD5(strbt);
            string result = "";
            if (issmall)
            {
                result = BitConverter.ToString(byteArray, 4, 12).Replace("-", "").ToLower();
            }
            else
            {
                result = BitConverter.ToString(byteArray, 4, 12).Replace("-", "").ToUpper();
            }
            return result;
        }

        /// <summary> 
        /// 返回32位Md5加密结果
        /// </summary> 
        /// <param name="original">数据源</param> 
        /// <param name="issmall">是否小写(默认是)</param> 
        /// <returns>摘要</returns> 
        public static string MD5Make32(string original, bool issmall = true)
        {
            //将要加密的字符串转换成字节数组
            byte[] strbt = Encoding.UTF8.GetBytes(original);
            var byteArray = MakeMD5(strbt);
            string result = "";
            if (issmall)
            {
                result = BitConverter.ToString(byteArray).Replace("-", "").ToLower();
            }
            else
            {
                result = BitConverter.ToString(byteArray).Replace("-", "").ToUpper();
            }
            return result;
        }

        /// <summary> 
        /// 生成MD5摘要 
        /// </summary> 
        /// <param name="original">数据源</param> 
        /// <returns>摘要</returns> 
        private static byte[] MakeMD5(byte[] original)
        {
            using (MD5 md5 = MD5.Create())
            {
                //对转换后的字节进行MD5加密
                byte[] keyhash = md5.ComputeHash(original);
                return keyhash;
            }
        }

        /**//// <summary>  
            /// 进行C#DES解密。  
            /// </summary>  
            /// <param name="pToDecrypt">要解密的以Base64</param>  
            /// <param name="sKey">密钥，且必须为8位。</param>  
            /// <returns>已解密的字符串。</returns>  
        public static string DecryptNew(string pToDecrypt, string sKey)
        {
            byte[] inputByteArray = Convert.FromBase64String(pToDecrypt);
            string result = System.Text.Encoding.UTF8.GetString(inputByteArray);
            using (DESCryptoServiceProvider des =
            new DESCryptoServiceProvider())
            {
                des.Key = ASCIIEncoding.ASCII.GetBytes(sKey);
                des.IV = ASCIIEncoding.ASCII.GetBytes(sKey);
                System.IO.MemoryStream ms = new System.IO.MemoryStream();
                using (CryptoStream cs = new CryptoStream(ms,
                 des.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(inputByteArray, 0, inputByteArray.Length);
                    cs.FlushFinalBlock();
                    cs.Close();
                }
                string str = Encoding.UTF8.GetString(ms.ToArray());
                ms.Close();
                return str;
            }
        }
        /// <summary>
        /// 获取Des8位密钥
        /// </summary>
        /// <param name="key">Des密钥字符串</param>
        /// <returns>Des8位密钥</returns>
        public static byte[] GetDesKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException("key", "Des密钥不能为空");
            }
            if (key.Length > 8)
            {
                key = key.Substring(0, 8);
            }
            if (key.Length < 8)
            {
                // 不足8补全
                key = key.PadRight(8, '0');
            }
            return Encoding.UTF8.GetBytes(key);
        }
        /// <summary>
        /// Des解密
        /// </summary>
        /// <param name="source">源字符串</param>
        /// <param name="key">des密钥，长度必须8位</param>
        /// <param name="iv">密钥向量</param>
        /// <returns>解密后的字符串</returns>
        public static string DesDecrypt(string source, string key, byte[] iv)
        {
            using (DES des = DES.Create())
            {
                byte[] rgbKeys = GetDesKey(key),
                rgbIvs = iv,
                inputByteArray = Convert.FromBase64String(source);
                if (iv.Length <= 0) { rgbIvs = rgbKeys; }
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, des.CreateDecryptor(rgbKeys, rgbIvs), CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(inputByteArray, 0, inputByteArray.Length);
                        cryptoStream.FlushFinalBlock();
                        return Encoding.UTF8.GetString(memoryStream.ToArray());
                    }
                }
            }
        }

        /// <summary> 
        /// 加密数据 
        /// </summary> 
        /// <param name="Text"></param> 
        /// <param name="sKey"></param> 
        /// <returns></returns> 
        public static string DEVMEncrypt(string Text, string sKey)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(Text);
            using (var des = DES.Create())
            {
                List<byte> keyBytes = new List<byte>();
                var sbuff = ASCIIEncoding.ASCII.GetBytes(sKey).ToList();
                if (sbuff.Count <= 8)
                {
                    keyBytes.AddRange(sbuff);
                    for (int i = sbuff.Count; i < 8; i++)
                    {
                        keyBytes.Add(0);
                    }
                }
                else if (sbuff.Count > 8)
                {
                    keyBytes.AddRange(sbuff.GetRange(0, 8));
                }
                des.Key = keyBytes.ToArray();
                des.IV = des.Key;
                using (var ms = new System.IO.MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(inputBytes, 0, inputBytes.Length);
                        cs.FlushFinalBlock();
                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }

        }
        /// <summary>
        /// Des解密
        /// </summary>
        /// <param name="source">源字符串</param>
        /// <param name="key">des密钥，长度必须8位</param>
        /// <param name="iv">密钥向量</param>
        /// <returns>解密后的字符串</returns>
        public static string DESDecrypt(string source, string key, string iv)
        {
            byte[] byKey = System.Text.ASCIIEncoding.ASCII.GetBytes(key);
            byte[] byIV = System.Text.ASCIIEncoding.ASCII.GetBytes(iv);
            byte[] byEnc;
            try
            {
                byte[] byEncb = Convert.FromBase64String(source);
                string byEncs = System.Text.Encoding.UTF8.GetString(byEncb);
                byEnc = Convert.FromBase64String(byEncs);
            }
            catch { return null; }
            DESCryptoServiceProvider des = new DESCryptoServiceProvider();
            MemoryStream ms = new MemoryStream(byEnc);
            CryptoStream cst = new CryptoStream(ms, des.CreateDecryptor(byKey, byIV), CryptoStreamMode.Read);
            StreamReader sr = new StreamReader(cst);
            return sr.ReadToEnd();
        }

        /// <summary> 
        /// 解密数据 
        /// </summary> 
        /// <param name="data">原始数据</param> 
        /// <param name="sKey">解密密钥</param> 
        /// <returns></returns> 
        public static string DESMDecrypt(string data, string sKey)
        {
            if (sKey.Length > 8) sKey = sKey.Substring(0, 8);
            //string text = Encoding.UTF8.GetString(Convert.FromBase64String(data));
            byte[] inputBytes = Convert.FromBase64String(data);
            using (var des = DES.Create())
            {
                des.Key = Encoding.UTF8.GetBytes(sKey);
                // 设置加密模式为ECB
                des.Mode = CipherMode.ECB;
                // 设置填充模式
                des.Padding = PaddingMode.PKCS7;
                using (var ms = new System.IO.MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(inputBytes, 0, inputBytes.Length);
                        cs.FlushFinalBlock();
                        return Encoding.UTF8.GetString(ms.ToArray());
                    }
                }
            }
        }

        /// <summary> 
        /// 加密数据 
        /// </summary> 
        /// <param name="Text"></param> 
        /// <param name="sKey"></param> 
        /// <returns></returns> 
        public static string DESEncrypt(string Text, string sKey)
        {
            if (sKey.Length > 8) sKey = sKey.Substring(0, 8);
            byte[] inputBytes = Encoding.UTF8.GetBytes(Text);
            using (var des = DES.Create())
            {
                des.Key = Encoding.UTF8.GetBytes(sKey);
                // 设置加密模式为ECB
                des.Mode = CipherMode.ECB;
                // 设置填充模式
                des.Padding = PaddingMode.PKCS7;
                using (var ms = new System.IO.MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(inputBytes, 0, inputBytes.Length);
                        cs.FlushFinalBlock();
                        //string base64 = Convert.ToBase64String(ms.ToArray());
                        //var bytes = Encoding.UTF8.GetBytes(ms.ToArray());
                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }

        }

    }

    /// <summary> 
    /// RSA加密解密及RSA签名和验证
    /// </summary> 
    public class RSAHelper
    {
        #region RSA 的密钥产生 

        /// <summary>
        /// RSA方式生成私钥和公钥
        /// </summary>
        /// <returns></returns>
        public static RSAKeyInfo RSACreateKey()
        {
            RSA rsa = RSA.Create();
            RSAKeyInfo info = new RSAKeyInfo
            {
                PrivateKey = rsa.ToXmlString(true), // 获取私钥
                PublicKey = rsa.ToXmlString(false), // 获取公钥
            };

            return info;
        }

        #endregion

        #region RSA的加密函数 
        //############################################################################## 
        //RSA 方式加密 
        //说明KEY必须是XML的行式,返回的是字符串 
        //在有一点需要说明！！该加密方式有 长度 限制的！！ 
        //############################################################################## 

        /// <summary>
        /// RSA公钥加密
        /// </summary>
        /// <param name="xmlPublicKey">公钥</param>
        /// <param name="m_strEncryptString">字符串</param>
        /// <returns></returns>
        public static string RSAEncrypt(string xmlPublicKey, string m_strEncryptString)
        {
            byte[] PlainTextBArray;
            byte[] CypherTextBArray;
            string Result;
            RSA rsa = RSA.Create();
            rsa.FromXmlString(xmlPublicKey);
            PlainTextBArray = Encoding.UTF8.GetBytes(m_strEncryptString);
            CypherTextBArray = rsa.Encrypt(PlainTextBArray, RSAEncryptionPadding.Pkcs1);
            Result = Convert.ToBase64String(CypherTextBArray);
            return Result;
        }

        public static string RSAEncrypt(RSA rsa, string m_strEncryptString)
        {
            byte[] PlainTextBArray;
            byte[] CypherTextBArray;
            string Result;
            PlainTextBArray = Encoding.UTF8.GetBytes(m_strEncryptString);
            CypherTextBArray = rsa.Encrypt(PlainTextBArray, RSAEncryptionPadding.Pkcs1);
            Result = Convert.ToBase64String(CypherTextBArray);
            return Result;
        }

        //RSA的加密函数 byte[]
        public static string RSAEncrypt(string xmlPublicKey, byte[] EncryptString)
        {
            byte[] CypherTextBArray;
            string Result;
            RSA rsa = RSA.Create();
            rsa.FromXmlString(xmlPublicKey);
            CypherTextBArray = rsa.Encrypt(EncryptString, RSAEncryptionPadding.Pkcs1);
            Result = Convert.ToBase64String(CypherTextBArray);
            return Result;
        }

        #endregion

        #region RSA的解密函数 

        /// <summary>
        /// RSA私钥解密
        /// </summary>
        /// <param name="xmlPrivateKey">解密</param>
        /// <param name="m_strDecryptString">字符串</param>
        /// <returns></returns>
        public static string RSADecrypt(string xmlPrivateKey, string m_strDecryptString)
        {
            byte[] PlainTextBArray;
            byte[] DypherTextBArray;
            string Result;
            RSA rsa = RSA.Create();
            rsa.FromXmlString(xmlPrivateKey);
            PlainTextBArray = Convert.FromBase64String(m_strDecryptString);
            DypherTextBArray = rsa.Decrypt(PlainTextBArray, RSAEncryptionPadding.Pkcs1);
            Result = Encoding.UTF8.GetString(DypherTextBArray);
            return Result;
        }

        public static string RSADecrypt(RSA rsa, string m_strDecryptString)
        {
            byte[] PlainTextBArray;
            byte[] DypherTextBArray;
            string Result;
            PlainTextBArray = Convert.FromBase64String(m_strDecryptString);
            DypherTextBArray = rsa.Decrypt(PlainTextBArray, RSAEncryptionPadding.Pkcs1);
            Result = Encoding.UTF8.GetString(DypherTextBArray);
            return Result;
        }

        //RSA的解密函数  byte
        public static string RSADecrypt(string xmlPrivateKey, byte[] DecryptString)
        {
            byte[] DypherTextBArray;
            string Result;
            RSA rsa = RSA.Create();
            rsa.FromXmlString(xmlPrivateKey);
            DypherTextBArray = rsa.Decrypt(DecryptString, RSAEncryptionPadding.Pkcs1);
            Result = Encoding.UTF8.GetString(DypherTextBArray);
            return Result;
        }

        #endregion
    }

    public class RSAKeyInfo
    {
        /// <summary>
        /// 公钥
        /// </summary>
        public string PublicKey { get; set; }
        /// <summary>
        /// 私钥
        /// </summary>
        public string PrivateKey { get; set; }
    }

    /// <summary>
    /// 将私钥（PKCS1,PKCS8）、公钥、私钥证书、公钥证书转为.NET RSA 对象
    /// </summary>
    public static class RsaUtil
    {
        #region 加载私钥

        /// <summary>
        /// 转换私钥字符串为RSA
        /// </summary>
        /// <param name="privateKeyStr">私钥字符串</param>
        /// <param name="keyFormat">PKCS8,PKCS1</param>
        /// <returns></returns>
        public static RSA LoadPrivateKey(string privateKeyStr, string keyFormat = "PKCS8")
        {
            string signType = "RSA";
            if (privateKeyStr.Length > 1024)
            {
                signType = "RSA2";
            }
            //PKCS8,PKCS1
            if (keyFormat == "PKCS1")
            {
                return LoadPrivateKeyPKCS1(privateKeyStr, signType);
            }
            else
            {
                return LoadPrivateKeyPKCS8(privateKeyStr);
            }
        }

        /// <summary>
        /// PKCS1 .java格式密钥转c#使用的.net格式Rsa
        /// </summary>
        /// <param name="privateKeyPemPkcs1">pcsk1 私钥的文本内容</param>
        /// <param name="signType">RSA 私钥长度1024 ,RSA2 私钥长度2048 </param>
        /// <returns></returns>
        public static RSA LoadPrivateKeyPKCS1(string privateKeyPemPkcs1, string signType)
        {
            try
            {
                privateKeyPemPkcs1 = privateKeyPemPkcs1.Replace("-----BEGIN RSA PRIVATE KEY-----", "").Replace("-----END RSA PRIVATE KEY-----", "").Replace("\r", "").Replace("\n", "").Trim();
                privateKeyPemPkcs1 = privateKeyPemPkcs1.Replace("-----BEGIN PRIVATE KEY-----", "").Replace("-----END PRIVATE KEY-----", "").Replace("\r", "").Replace("\n", "").Trim();

                byte[] data = Convert.FromBase64String(privateKeyPemPkcs1);
                var rsa = DecodeRSAPrivateKey(data, signType);
                return rsa;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// PKCS8 .java格式密钥转c#使用的.net格式密钥
        /// </summary>
        /// <param name="privkey"></param>
        /// <param name="signType"></param>
        /// <returns></returns>
        private static RSA DecodeRSAPrivateKey(byte[] privkey, string signType)
        {
            byte[] MODULUS, E, D, P, Q, DP, DQ, IQ;

            // --------- Set up stream to decode the asn.1 encoded RSA private key ------
            MemoryStream mem = new MemoryStream(privkey);
            BinaryReader binr = new BinaryReader(mem);  //wrap Memory Stream with BinaryReader for easy reading
            byte bt = 0;
            ushort twobytes = 0;
            int elems = 0;
            try
            {
                twobytes = binr.ReadUInt16();
                if (twobytes == 0x8130) //data read as little endian order (actual data order for Sequence is 30 81)
                    binr.ReadByte();    //advance 1 byte
                else if (twobytes == 0x8230)
                    binr.ReadInt16();    //advance 2 bytes
                else
                    return null;

                twobytes = binr.ReadUInt16();
                if (twobytes != 0x0102) //version number
                    return null;
                bt = binr.ReadByte();
                if (bt != 0x00)
                    return null;


                //------ all private key components are Integer sequences ----
                elems = GetIntegerSize(binr);
                MODULUS = binr.ReadBytes(elems);

                elems = GetIntegerSize(binr);
                E = binr.ReadBytes(elems);

                elems = GetIntegerSize(binr);
                D = binr.ReadBytes(elems);

                elems = GetIntegerSize(binr);
                P = binr.ReadBytes(elems);

                elems = GetIntegerSize(binr);
                Q = binr.ReadBytes(elems);

                elems = GetIntegerSize(binr);
                DP = binr.ReadBytes(elems);

                elems = GetIntegerSize(binr);
                DQ = binr.ReadBytes(elems);

                elems = GetIntegerSize(binr);
                IQ = binr.ReadBytes(elems);


                // ------- create RSA instance and initialize with public key -----
                //CspParameters CspParameters = new CspParameters();
                //CspParameters.Flags = CspProviderFlags.UseMachineKeyStore;

                int bitLen = 1024;
                if ("RSA2".Equals(signType))
                {
                    bitLen = 2048;
                }

                RSA _RSA = RSA.Create(bitLen);
                RSAParameters RSAparams = new RSAParameters();
                RSAparams.Modulus = MODULUS;
                RSAparams.Exponent = E;
                RSAparams.D = D;
                RSAparams.P = P;
                RSAparams.Q = Q;
                RSAparams.DP = DP;
                RSAparams.DQ = DQ;
                RSAparams.InverseQ = IQ;
                _RSA.ImportParameters(RSAparams);

                return _RSA;
            }
            catch (Exception ex)
            {
                throw ex;
                // return null;
            }
            finally
            {
                binr.Close();
            }
        }

        private static int GetIntegerSize(BinaryReader binr)
        {
            byte bt = 0;
            byte lowbyte = 0x00;
            byte highbyte = 0x00;
            int count = 0;
            bt = binr.ReadByte();
            if (bt != 0x02)        //expect integer
                return 0;
            bt = binr.ReadByte();

            if (bt == 0x81)
                count = binr.ReadByte();    // data size in next byte
            else
                if (bt == 0x82)
            {
                highbyte = binr.ReadByte(); // data size in next 2 bytes
                lowbyte = binr.ReadByte();
                byte[] modint = { lowbyte, highbyte, 0x00, 0x00 };
                count = BitConverter.ToInt32(modint, 0);
            }
            else
            {
                count = bt;     // we already have the data size
            }

            while (binr.ReadByte() == 0x00)
            {    //remove high order zeros in data
                count -= 1;
            }
            binr.BaseStream.Seek(-1, SeekOrigin.Current);        //last ReadByte wasn't a removed zero, so back up a byte
            return count;
        }

        /// <summary>
        /// PKCS8 .java格式密钥转c#使用的.net格式Rsa
        /// </summary>
        /// <param name="privateKeyPemPkcs8"></param>
        /// <returns></returns>
        public static RSA LoadPrivateKeyPKCS8(string privateKeyPemPkcs8)
        {
            try
            {
                //PKCS8是“BEGIN PRIVATE KEY”
                privateKeyPemPkcs8 = privateKeyPemPkcs8.Replace("-----BEGIN RSA PRIVATE KEY-----", "").Replace("-----END RSA PRIVATE KEY-----", "").Replace("\r", "").Replace("\n", "").Trim();
                privateKeyPemPkcs8 = privateKeyPemPkcs8.Replace("-----BEGIN PRIVATE KEY-----", "").Replace("-----END PRIVATE KEY-----", "").Replace("\r", "").Replace("\n", "").Trim();

                //pkcs8 文本先转为 .NET XML 私钥字符串
                string privateKeyXml = RSAPrivateKeyJava2DotNet(privateKeyPemPkcs8);

                RSA publicRsa = RSA.Create();
                publicRsa.FromXmlString(privateKeyXml);
                return publicRsa;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// PKCS8 .java格式密钥转c#使用的.net格式密钥
        /// </summary>
        /// <param name="privateKeyPemPkcs8"></param>
        /// <returns></returns>
        private static string RSAPrivateKeyJava2DotNet(string privateKeyPemPkcs8)
        {
            RsaPrivateCrtKeyParameters privateKeyParam = (RsaPrivateCrtKeyParameters)PrivateKeyFactory.CreateKey(Convert.FromBase64String(privateKeyPemPkcs8));
            return string.Format("<RSAKeyValue><Modulus>{0}</Modulus><Exponent>{1}</Exponent><P>{2}</P><Q>{3}</Q><DP>{4}</DP><DQ>{5}</DQ><InverseQ>{6}</InverseQ><D>{7}</D></RSAKeyValue>",
            Convert.ToBase64String(privateKeyParam.Modulus.ToByteArrayUnsigned()),
            Convert.ToBase64String(privateKeyParam.PublicExponent.ToByteArrayUnsigned()),
            Convert.ToBase64String(privateKeyParam.P.ToByteArrayUnsigned()),
            Convert.ToBase64String(privateKeyParam.Q.ToByteArrayUnsigned()),
            Convert.ToBase64String(privateKeyParam.DP.ToByteArrayUnsigned()),
            Convert.ToBase64String(privateKeyParam.DQ.ToByteArrayUnsigned()),
            Convert.ToBase64String(privateKeyParam.QInv.ToByteArrayUnsigned()),
            Convert.ToBase64String(privateKeyParam.Exponent.ToByteArrayUnsigned()));
        }

        /// <summary>
        /// c#使用的.net格式密钥转换成.Java格式密钥
        /// </summary>
        /// <param name="cPrivateKey">.net格式密钥</param>
        /// <returns></returns>
        public static string RSAPrivateKeyDotNet2Java(string cPrivateKey)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(cPrivateKey);
            BigInteger m = new BigInteger(1, Convert.FromBase64String(doc.DocumentElement.GetElementsByTagName("Modulus")[0].InnerText));
            BigInteger exp = new BigInteger(1, Convert.FromBase64String(doc.DocumentElement.GetElementsByTagName("Exponent")[0].InnerText));
            BigInteger d = new BigInteger(1, Convert.FromBase64String(doc.DocumentElement.GetElementsByTagName("D")[0].InnerText));
            BigInteger p = new BigInteger(1, Convert.FromBase64String(doc.DocumentElement.GetElementsByTagName("P")[0].InnerText));
            BigInteger q = new BigInteger(1, Convert.FromBase64String(doc.DocumentElement.GetElementsByTagName("Q")[0].InnerText));
            BigInteger dp = new BigInteger(1, Convert.FromBase64String(doc.DocumentElement.GetElementsByTagName("DP")[0].InnerText));
            BigInteger dq = new BigInteger(1, Convert.FromBase64String(doc.DocumentElement.GetElementsByTagName("DQ")[0].InnerText));
            BigInteger qinv = new BigInteger(1, Convert.FromBase64String(doc.DocumentElement.GetElementsByTagName("InverseQ")[0].InnerText));

            RsaPrivateCrtKeyParameters privateKeyParam = new RsaPrivateCrtKeyParameters(m, exp, d, p, q, dp, dq, qinv);

            PrivateKeyInfo privateKeyInfo = PrivateKeyInfoFactory.CreatePrivateKeyInfo(privateKeyParam);
            byte[] serializedPrivateBytes = privateKeyInfo.ToAsn1Object().GetEncoded();
            return Convert.ToBase64String(serializedPrivateBytes);
        }

        #endregion

        #region 加载公钥

        /// <summary>
        /// 加载公钥证书
        /// </summary>
        /// <param name="publicKeyCert">公钥证书文本内容</param>
        /// <returns></returns>
        public static RSA LoadPublicCert(string publicKeyCert)
        {
            publicKeyCert = publicKeyCert.Replace("-----BEGIN CERTIFICATE-----", "").Replace("-----END CERTIFICATE-----", "").Replace("\r", "").Replace("\n", "").Trim();

            byte[] bytesCerContent = Convert.FromBase64String(publicKeyCert);
            X509Certificate2 x509 = new X509Certificate2(bytesCerContent);
            RSA rsaPub = (RSA)x509.PublicKey.Key;
            return rsaPub;
        }

        /// <summary>
        /// Java转.net格式Rsa
        /// </summary>
        /// <param name="publicKeyPem"></param>
        /// <returns></returns>
        public static RSA LoadPublicKey(string publicKeyPem)
        {
            publicKeyPem = publicKeyPem.Replace("-----BEGIN PUBLIC KEY-----", "").Replace("-----END PUBLIC KEY-----", "").Replace("\r", "").Replace("\n", "").Trim();

            //pem 公钥文本 转  .NET XML 公钥文本。
            string publicKeyXml = RSAPublicKeyJava2DotNet(publicKeyPem);

            RSA publicRsa = RSA.Create();
            publicRsa.FromXmlString(publicKeyXml);
            return publicRsa;
        }

        /// <summary>
        /// Java转.net格式
        /// </summary>
        /// <param name="publicKey"></param>
        /// <returns></returns>
        private static string RSAPublicKeyJava2DotNet(string publicKey)
        {
            RsaKeyParameters publicKeyParam = (RsaKeyParameters)PublicKeyFactory.CreateKey(Convert.FromBase64String(publicKey));
            return string.Format("<RSAKeyValue><Modulus>{0}</Modulus><Exponent>{1}</Exponent></RSAKeyValue>",
                Convert.ToBase64String(publicKeyParam.Modulus.ToByteArrayUnsigned()),
                Convert.ToBase64String(publicKeyParam.Exponent.ToByteArrayUnsigned()));
        }

        /// <summary>
        /// .NET格式转Java格式
        /// </summary>
        /// <param name="cPublicKey">c#的.net格式公钥</param>
        /// <returns></returns>
        public static string RSAPublicKeyDotNet2Java(string cPublicKey)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(cPublicKey);
            BigInteger m = new BigInteger(1, Convert.FromBase64String(doc.DocumentElement.GetElementsByTagName("Modulus")[0].InnerText));
            BigInteger p = new BigInteger(1, Convert.FromBase64String(doc.DocumentElement.GetElementsByTagName("Exponent")[0].InnerText));
            RsaKeyParameters pub = new RsaKeyParameters(false, m, p);
            SubjectPublicKeyInfo publicKeyInfo = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(pub);
            byte[] serializedPublicBytes = publicKeyInfo.ToAsn1Object().GetDerEncoded();
            return Convert.ToBase64String(serializedPublicBytes);
        }

        #endregion
    }

}
