using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace YY.EventLogReaderAssistant.Services
{
    public sealed class MD5HashGenerator
    {
        #region Public Methods

        public static string GetMD5Hash<T>(T source)
        {
            byte[] objectAsByteArray = ObjectToByteArray<T>(source);

            byte[] dataMD5;
            using (MD5 md5Hash = MD5.Create())
                dataMD5 = md5Hash.ComputeHash(objectAsByteArray);

            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < dataMD5.Length; i++)            
                sBuilder.Append(dataMD5[i].ToString("x2"));

            return sBuilder.ToString(); ;
        }

        #endregion

        #region Private Methods

        private static byte[] ObjectToByteArray<T>(T source)
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream())
            {
                bf.Serialize(ms, source);
                return ms.ToArray();
            }
        }

        #endregion
    }
}
