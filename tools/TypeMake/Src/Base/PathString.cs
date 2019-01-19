using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace TypeMake
{
    public class PathString
    {
        public readonly String Value;
        public PathString(String Value)
        {
            var v = Value.TrimEnd('\\', '/').Replace('\\', '/').Replace('/', Path.DirectorySeparatorChar);
            if (v == "")
            {
                v = ".";
            }
            this.Value = v;
        }
        public PathString FullPath
        {
            get
            {
                return new PathString(Path.GetFullPath(Value));
            }
        }
        public String FileName
        {
            get
            {
                return Path.GetFileName(Value);
            }
        }
        public String Extension
        {
            get
            {
                return Path.GetExtension(Value);
            }
        }
        public PathString ChnageExtension(String Extension)
        {
            return new PathString(Path.ChangeExtension(Value, Extension));
        }
        public PathString Directory
        {
            get
            {
                return new PathString(Path.GetDirectoryName(Value));
            }
        }
        public PathString Reduced
        {
            get
            {
                var s = new Stack<String>();
                foreach (var d in Value.Split(Path.DirectorySeparatorChar))
                {
                    if (d == ".") { continue; }
                    if (d == "..")
                    {
                        if (s.Count > 0)
                        {
                            var Parent = s.Pop();
                            if (Parent == "..")
                            {
                                s.Push(Parent);
                                s.Push(d);
                            }
                        }
                        else
                        {
                            s.Push(d);
                        }
                        continue;
                    }
                    if (d.EndsWith(":"))
                    {
                        s.Clear();
                    }
                    s.Push(d);
                }
                return new PathString(String.Join(Convert.ToString(Path.DirectorySeparatorChar), s.AsEnumerable().Reverse()));
            }
        }
        public PathString RelativeTo(PathString BaseDirectory)
        {
            String PopFirstDir(ref String p)
            {
                if (p == "") { return ""; }
                var DirectorySeparatorCharStart = p.IndexOf(Path.DirectorySeparatorChar);
                if (DirectorySeparatorCharStart < 0)
                {
                    var ret = p;
                    p = "";
                    return ret;
                }
                else
                {
                    var ret = p.Substring(0, DirectorySeparatorCharStart);
                    p = p.Substring(DirectorySeparatorCharStart + 1);
                    return ret;
                }
            }

            var r = Reduced.Value;
            var a = r;
            var b = BaseDirectory.Reduced.Value;
            if ((a == ".") || (b == ".")) { return r; }

            while (true)
            {
                var c = PopFirstDir(ref a);
                var d = PopFirstDir(ref b);
                if (c != d)
                {
                    a = c.AsPath() / a;
                    b = d.AsPath() / b;
                    if (a == ".") { a = ""; }
                    if (b == ".") { b = ""; }
                    while (PopFirstDir(ref b) != "")
                    {
                        a = "..".AsPath() / a;
                    }
                    return a;
                }
                if ((c == "") && (a == "") && (b == "")) { return new PathString("."); }
            }
        }
        public bool Equals(PathString Right, bool CaseSensitive = true)
        {
            if (Value.Equals(Right.Value, CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase)) { return true; }
            return Reduced.Value.Equals(Right.Reduced.Value, CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
        }
        public bool In(PathString Right, bool CaseSensitive = true)
        {
            var sLeft = Reduced.Value;
            var sRight = Right.Reduced.Value;
            if (sLeft.StartsWith(sRight + Path.DirectorySeparatorChar)) { return true; }
            if ((sRight == ".") && !sLeft.StartsWith("..")) { return true; }
            return false;
        }
        public bool EqualsOrIn(PathString Right, bool CaseSensitive = true)
        {
            return Equals(Right) || In(Right);
        }

        public static PathString operator /(PathString Left, PathString Right)
        {
            if (Left.Value == ".") { return Right; }
            if (Right.Value == ".") { return Left; }
            if (Right.Value.StartsWith(Convert.ToString(Path.DirectorySeparatorChar))) { return Right; }
            var DirectorySeparatorStart = Right.Value.IndexOf(Path.DirectorySeparatorChar);
            if (DirectorySeparatorStart > 0)
            {
                if (Right.Value[DirectorySeparatorStart - 1] == ':') { return Right; }
            }
            return new PathString(Path.Combine(Left.Value, Right.Value));
        }
        public static PathString operator /(PathString Left, String Right)
        {
            return Left / new PathString(Right);
        }
        public static PathString operator /(String Left, PathString Right)
        {
            return new PathString(Left) / Right.Value;
        }
        public static implicit operator String(PathString p)
        {
            return p.Value;
        }
        public static implicit operator PathString(String s)
        {
            return new PathString(s);
        }

        public override string ToString()
        {
            return Value;
        }
    }
    public static class PathExt
    {
        public static PathString AsPath(this String s)
        {
            return new PathString(s);
        }
    }
}
