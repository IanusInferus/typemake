using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace TypeMake
{
    public class Hash
    {
        public static String GetHashForPath(String Path, int Length)
        {
            var Bytes = Encoding.UTF8.GetBytes(Path);
            Byte[] Hash;
            using (var sha256 = new SHA256Managed())
            {
                Hash = sha256.ComputeHash(Bytes);
            }
            return String.Join("", Hash.Select(b => b.ToString("X2"))).Substring(0, Length);
        }
    }
}
