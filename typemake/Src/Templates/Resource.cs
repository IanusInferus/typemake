using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace TypeMake
{
    public static class Resource
    {
        public static String GetResourceText(String Path)
        {
            var p = String.Format("TypeMake.{0}", Regex.Replace(Path, @"[\\/]", "."));
            using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream(p))
            using (var sr = new StreamReader(s, Encoding.UTF8, true))
            {
                if (sr.EndOfStream) { return ""; }
                return sr.ReadToEnd();
            }
        }
    }
}
