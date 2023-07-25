using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace UICodeGeneration
{
    public static class MD5Hashing
    {
        private static MD5 _md5;
        private static MD5 Md5
        {
            get
            {
                if (_md5 == null)
                    _md5 = MD5.Create();
                return _md5;
            }
        }

        /// <summary>
        /// utf8编码
        /// </summary>
        /// <param name="sourceString"></param>
        /// <returns></returns>
        public static string HashString(string sourceString)
        {
            return HashString(Encoding.UTF8, sourceString);
        }

        /// <summary>
        /// 指定编码将字符串散列
        /// </summary>
        /// <param name="encode"></param>
        /// <param name="sourceString"></param>
        /// <returns></returns>
        public static string HashString(Encoding encode, string sourceString)
        {
            byte[] source = Md5.ComputeHash(encode.GetBytes(sourceString));
            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < source.Length; i++)
                sBuilder.Append(source[i].ToString("x2"));
            return sBuilder.ToString();
        }

        public static string HashFile(string path)
        {
            try
            {
                byte[] fileBytes = File.ReadAllBytes(path);
                return HashBytes(fileBytes);
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.Message);
                return "";
            }
        }

        public static string HashBytes(byte[] bytes)
        {
            byte[] hashBytes = Md5.ComputeHash(bytes);
            string resule = System.BitConverter.ToString(hashBytes);
            resule = resule.Replace("-", "");
            return resule;
        }

    }
}