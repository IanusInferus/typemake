using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Int = System.Int32;

namespace TypeMake
{
    public class AliasAttribute : Attribute { }
    public class RecordAttribute : Attribute { }
    public class TaggedUnionAttribute : Attribute { }
    public class TagAttribute : Attribute { }
    public class TupleAttribute : Attribute { }

    [Record]
    public struct Unit { }


    public static class Plist
    {
        public enum ValueTag
        {
            True = 0,
            False = 1,
            Integer = 2,
            String = 3,
            Dict = 4,
            Array = 5
        }
        [TaggedUnion]
        public sealed class Value
        {
            [Tag] public ValueTag _Tag;

            public Unit True;
            public Unit False;
            public Int Integer;
            public String String;
            public Dictionary<String, Value> Dict;
            public List<Value> Array;

            public static Value CreateTrue() { return new Value { _Tag = ValueTag.True, True = default(Unit) }; }
            public static Value CreateFalse() { return new Value { _Tag = ValueTag.False, False = default(Unit) }; }
            public static Value CreateInteger(Int Value) { return new Value { _Tag = ValueTag.Integer, Integer = Value }; }
            public static Value CreateString(String Value) { return new Value { _Tag = ValueTag.String, String = Value }; }
            public static Value CreateDict(Dictionary<String, Value> Value) { return new Value { _Tag = ValueTag.Dict, Dict = Value }; }
            public static Value CreateArray(List<Value> Value) { return new Value { _Tag = ValueTag.Array, Array = Value }; }

            public Boolean OnTrue { get { return _Tag == ValueTag.True; } }
            public Boolean OnFalse { get { return _Tag == ValueTag.False; } }
            public Boolean OnInteger { get { return _Tag == ValueTag.Integer; } }
            public Boolean OnString { get { return _Tag == ValueTag.String; } }
            public Boolean OnDict { get { return _Tag == ValueTag.Dict; } }
            public Boolean OnArray { get { return _Tag == ValueTag.Array; } }

            public override String ToString()
            {
                if (OnTrue)
                {
                    return "true";
                }
                else if (OnFalse)
                {
                    return "false";
                }
                else if (OnInteger)
                {
                    return "integer(" + Integer.ToString() + ")";
                }
                else if (OnString)
                {
                    return "string(" + String + ")";
                }
                else if (OnDict)
                {
                    return "dict(...)";
                }
                else if (OnArray)
                {
                    return "array(...)";
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }

        public static void SetItem<TKey, TValue>(this Dictionary<TKey, TValue> d, TKey Key, TValue Value)
        {
            if (d.ContainsKey(Key))
            {
                d[Key] = Value;
            }
            else
            {
                d.Add(Key, Value);
            }
        }

        public static Value Read(String FilePath)
        {
            var x = XDocument.Load(FilePath);
            var xplist = x.Root;
            var xvalue = xplist.Elements().Single();
            return Parse(xvalue);
        }
        public static void Write(String FilePath, Value v)
        {
            File.WriteAllText(FilePath, ToString(v), new UTF8Encoding(false));
        }
        public static Value FromString(String Text)
        {
            var x = XDocument.Parse(Text);
            var xplist = x.Root;
            var xvalue = xplist.Elements().Single();
            return Parse(xvalue);
        }
        public static String ToString(Value v)
        {
            var x = Write(v);
            var sb = new StringBuilder();
            using (var xw = XmlWriter.Create(sb, new XmlWriterSettings { CheckCharacters = true, Indent = true, IndentChars = "\t", NamespaceHandling = NamespaceHandling.OmitDuplicates, NewLineChars = "\n" }))
            {
                x.Save(xw);
            }
            var Lines = sb.ToString().Split('\n');
            var OutputLines = (new String[] { @"<?xml version=""1.0"" encoding=""UTF-8""?>", @"<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">", @"<plist version=""1.0"">" }).Concat(Lines.Skip(1).Select(Line => Line.Replace(" />", "/>"))).Concat(new String[] { @"</plist>", "" }).ToArray();
            return String.Join("\n", OutputLines);
        }

        private static Value Parse(XElement x)
        {
            if (x.Name == "true") { return Value.CreateTrue(); }
            if (x.Name == "false") { return Value.CreateFalse(); }
            if (x.Name == "integer") { return Value.CreateInteger(int.Parse(x.Value)); }
            if (x.Name == "string") { return Value.CreateString(x.Value); }
            if (x.Name == "dict") { return Value.CreateDict(ParseDict(x.Elements().ToList())); }
            if (x.Name == "array") { return Value.CreateArray(ParseArray(x.Elements().ToList())); }
            throw new NotSupportedException();
        }
        private static Dictionary<String, Value> ParseDict(List<XElement> Elements)
        {
            var d = new Dictionary<String, Value>();
            for (int k = 0; k < Elements.Count; k += 2)
            {
                if (Elements[k].Name != "key") { throw new InvalidOperationException(); }
                var Key = Elements[k].Value;
                var Value = Parse(Elements[k + 1]);
                d.Add(Key, Value);
            }
            return d;
        }
        private static List<Value> ParseArray(List<XElement> Elements)
        {
            var l = new List<Value>();
            foreach (var e in Elements)
            {
                l.Add(Parse(e));
            }
            return l;
        }

        private static XElement Write(Value v)
        {
            if (v.OnTrue) { return new XElement("true"); }
            if (v.OnFalse) { return new XElement("false"); }
            if (v.OnInteger) { return new XElement("integer", v.Integer); }
            if (v.OnString) { return new XElement("string", v.String); }
            if (v.OnDict) { return new XElement("dict", WriteDict(v.Dict)); }
            if (v.OnArray) { return new XElement("array", WriteArray(v.Array)); }
            throw new NotSupportedException();
        }
        private static List<XElement> WriteDict(Dictionary<String, Value> d)
        {
            var xl = new List<XElement>();
            foreach (var p in d)
            {
                xl.Add(new XElement("key", p.Key));
                xl.Add(Write(p.Value));
            }
            return xl;
        }
        private static List<XElement> WriteArray(List<Value> l)
        {
            var xl = new List<XElement>();
            foreach (var v in l)
            {
                xl.Add(Write(v));
            }
            return xl;
        }

        public static void ObjectReferenceValidityTest(Dictionary<String, Value> Objects, String RootObjectKey)
        {
            var InvalidReferences = new HashSet<String>();
            var RedundantReferences = new HashSet<String>();
            var UsedReferences = new HashSet<String> { RootObjectKey };
            foreach (var o in Objects)
            {
                ObjectReferenceValidityTestInner(Objects, o.Value, InvalidReferences, UsedReferences);
            }
            foreach (var o in Objects)
            {
                if (!UsedReferences.Contains(o.Key))
                {
                    RedundantReferences.Add(o.Key);
                }
            }
            if (InvalidReferences.Count > 0)
            {
                throw new InvalidOperationException("InvalidReferences: " + String.Join(", ", InvalidReferences));
            }
            if (RedundantReferences.Count > 0)
            {
                throw new InvalidOperationException("RedundantReferences: " + String.Join(", ", RedundantReferences));
            }
        }
        private static Regex rReference = new Regex(@"^[0-9A-F]{24}$");
        private static void ObjectReferenceValidityTestInner(Dictionary<String, Value> Objects, Value v, HashSet<String> InvalidReferences, HashSet<String> UsedReferences)
        {
            if (v.OnString)
            {
                if (rReference.Match(v.String).Success)
                {
                    if (Objects.ContainsKey(v.String))
                    {
                        if (!UsedReferences.Contains(v.String))
                        {
                            UsedReferences.Add(v.String);
                        }
                    }
                    else
                    {
                        if (!InvalidReferences.Contains(v.String))
                        {
                            InvalidReferences.Add(v.String);
                        }
                    }
                }
            }
            else if (v.OnDict)
            {
                foreach (var p in v.Dict)
                {
                    if (p.Key == "remoteGlobalIDString") { continue; }
                    ObjectReferenceValidityTestInner(Objects, p.Value, InvalidReferences, UsedReferences);
                }
            }
            else if (v.OnArray)
            {
                foreach (var e in v.Array)
                {
                    ObjectReferenceValidityTestInner(Objects, e, InvalidReferences, UsedReferences);
                }
            }
        }
    }
}
