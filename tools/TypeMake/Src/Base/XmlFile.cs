using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace TypeMake
{
    public static class XmlFile
    {
        public static XElement FromString(String Text)
        {
            return XElement.Parse(Text, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
        }
        public static String ToString(XElement e)
        {
            return @"<?xml version=""1.0"" encoding=""utf-8""?>" + "\r\n" + e.ToString(SaveOptions.OmitDuplicateNamespaces);
        }
    }
}
