using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

namespace TypeMake
{
    public static class FileNameHandling
    {
        public static String GetRelativePath(String FilePath, String BaseDirectory)
        {
            if ((FilePath == "") || (BaseDirectory == "")) { return FilePath; }
            var a = FilePath.TrimEnd('\\', '/');
            var b = BaseDirectory.TrimEnd('\\', '/');
            var c = PopFirstDir(ref a);
            var d = PopFirstDir(ref b);
            if (c != d) { return FilePath; }
            while (c == d)
            {
                if ((c == "") && (a == "") && (b == "")) { return "."; }
                c = PopFirstDir(ref a);
                d = PopFirstDir(ref b);
            }

            a = (c + "\\" + a).TrimEnd('\\', '/');
            b = (d + "\\" + b).TrimEnd('\\', '/');

            while (PopFirstDir(ref b) != "")
            {
                a = "..\\" + a;
            }
            return a.Replace('\\', Path.DirectorySeparatorChar);
        }
        private static String PopFirstDir(ref String p)
        {
            if (p == "") { return ""; }
            var NameS = p.IndexOfAny(new char[] { '/', '\\' });
            if (NameS < 0)
            {
                var ret = p;
                p = "";
                return ret;
            }
            else
            {
                var ret = p.Substring(0, NameS);
                p = p.Substring(NameS + 1);
                return ret;
            }
        }
    }
}
