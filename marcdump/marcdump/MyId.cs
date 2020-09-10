using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace marcdump
{
    static class MyId
    {
        private static string getMD5HashString(string text)
        {
            byte[] data = Encoding.UTF8.GetBytes(text);
            var algorithm = new MD5CryptoServiceProvider();
            byte[] bs = algorithm.ComputeHash(data);
            // リソースを解放する
            algorithm.Clear();
            var result = new StringBuilder();
            foreach (byte b in bs) result.Append(b.ToString("X2"));
            return result.ToString();
        }
        internal static string CreateId(string title, string date)
        {
            var basestr = title + date;
            return "YBD" + getMD5HashString(basestr);
        }
    }
}
