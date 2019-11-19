using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace TypeMake
{
    public class PathString
    {
        private static readonly String Slash = Convert.ToString(Path.DirectorySeparatorChar);
        private readonly String Value;
        public PathString(String Value)
        {
            if (Value == null) { throw new ArgumentNullException(); }
            if ((Value == "/") || (Value == "\\"))
            {
                this.Value = Slash;
                return;
            }
            var v = Value.TrimEnd('\\', '/').Replace('\\', '/').Replace('/', Path.DirectorySeparatorChar);
            if (v.EndsWith(":") && !v.Contains(Path.DirectorySeparatorChar))
            {
                v += Path.DirectorySeparatorChar;
            }
            this.Value = v;
        }
        public bool IsFullPath
        {
            get
            {
                return (Parts.Count >= 1) && Parts.First().EndsWith(Slash);
            }
        }
        public PathString FullPath
        {
            get
            {
                var Parts = this.Parts;
                if ((Parts.Count >= 1) && Parts.First().EndsWith(Slash)) { return this; }
                return Path.GetFullPath(ToString()).AsPath();
            }
        }
        public String FileName
        {
            get
            {
                return Path.GetFileName(ToString());
            }
        }
        public String Extension
        {
            get
            {
                return Path.GetExtension(ToString()).TrimStart('.');
            }
        }
        public String FileNameWithoutExtension
        {
            get
            {
                return Path.GetFileNameWithoutExtension(ToString());
            }
        }
        public PathString ChangeExtension(String Extension)
        {
            return Path.ChangeExtension(Value, Extension).AsPath();
        }
        public PathString Parent
        {
            get
            {
                return (this / "..").Reduced;
            }
        }
        public PathString GetAccestor(int n)
        {
            if (n < 0) { throw new InvalidOperationException(); }
            var p = this;
            for (int k = 0; k < n; k += 1)
            {
                p = p.Parent;
            }
            return p;
        }
        public List<String> Parts
        {
            get
            {
                var p = ToString().TrimEnd(Path.DirectorySeparatorChar).Split(Path.DirectorySeparatorChar).ToList();
                if (p.Count > 0)
                {
                    if (p[0] == "")
                    {
                        p[0] = Slash;
                    }
                    else if (p[0].EndsWith(":"))
                    {
                        p[0] += Slash;
                    }
                }
                return p;
            }
        }
        public static PathString Join(IEnumerable<String> Parts)
        {
            var s = (PathString)(null);
            foreach (var p in Parts)
            {
                if (s == null)
                {
                    s = p;
                }
                else
                {
                    s /= p;
                }
            }
            return s;
        }
        public PathString Reduced
        {
            get
            {
                var s = new Stack<String>();
                foreach (var d in this.Parts)
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
                            else if (Parent.EndsWith(Slash))
                            {
                                s.Push(Parent);
                            }
                        }
                        else
                        {
                            s.Push(d);
                        }
                        continue;
                    }
                    if (d.EndsWith(Slash))
                    {
                        s.Clear();
                    }
                    s.Push(d);
                }
                if (s.Count == 0) { return ".".AsPath(); }
                return Join(s.AsEnumerable().Reverse());
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
                else if (DirectorySeparatorCharStart == 0)
                {
                    var ret = p.Substring(0, DirectorySeparatorCharStart + 1);
                    p = p.Substring(DirectorySeparatorCharStart + 1);
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
                    while (true)
                    {
                        var v = PopFirstDir(ref b);
                        if (v == "..") { throw new InvalidOperationException(); }
                        if (v == "") { break; }
                        a = "..".AsPath() / a;
                    }
                    return a;
                }
                if ((c == "") && (a == "") && (b == "")) { return new PathString("."); }
            }
        }
        public bool Equals(PathString Right, bool CaseSensitive = true)
        {
            var LeftParts = this.Reduced.Parts.Where(p => p != ".").ToList();
            var RightParts = Right.Reduced.Parts.Where(p => p != ".").ToList();
            return (LeftParts.Count == RightParts.Count) && LeftParts.Zip(RightParts, (a, b) => a.Equals(b, CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase)).All(b => b);
        }
        public bool In(PathString Right, bool CaseSensitive = true)
        {
            return !Equals(Right, CaseSensitive) && EqualsOrIn(Right, CaseSensitive);
        }
        public bool EqualsOrIn(PathString Right, bool CaseSensitive = true)
        {
            var LeftParts = this.Reduced.Parts.Where(p => p != ".").ToList();
            var RightParts = Right.Reduced.Parts.Where(p => p != ".").ToList();
            var MinCount = Math.Min(LeftParts.Count, RightParts.Count);
            var NotSameIndex = MinCount;
            for (int k = 0; k < MinCount; k += 1)
            {
                if (!LeftParts[k].Equals(RightParts[k], CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase))
                {
                    NotSameIndex = k;
                    break;
                }
            }
            if (RightParts.Skip(NotSameIndex).All(p => p == "..") && (LeftParts.Skip(NotSameIndex).Count(p => p == "..") <= RightParts.Count - NotSameIndex))
            {
                return true;
            }
            return false;
        }
        public override bool Equals(object obj)
        {
            if (obj is PathString ps)
            {
                return Equals(ps, true);
            }
            else if (obj is String s)
            {
                return Equals(s.AsPath(), true);
            }
            else
            {
                return false;
            }
        }
        public override int GetHashCode()
        {
            return Reduced.Value.GetHashCode();
        }
        public static bool operator ==(PathString Left, PathString Right)
        {
            if ((Left is null) && (Right is null)) { return true; }
            if ((Left is null) || (Right is null)) { return false; }
            return Left.Equals(Right, true);
        }
        public static bool operator !=(PathString Left, PathString Right)
        {
            if ((Left is null) && (Right is null)) { return false; }
            if ((Left is null) || (Right is null)) { return true; }
            return !Left.Equals(Right, true);
        }

        public static PathString operator /(PathString Left, PathString Right)
        {
            if (Left.Value == "") { return Right; }
            if (Right.Value == "") { return Left; }
            if (Right.Value.StartsWith(Slash)) { return Right; }
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
            return new PathString(Left) / Right;
        }
        public static PathString operator +(PathString Left, String Right)
        {
            if (Right.Contains('/') || Right.Contains('\\')) { throw new ArgumentException(); }
            return new PathString(Left.Value + Right);
        }
        public static implicit operator String(PathString p)
        {
            return p != null ? p.ToString() : null;
        }
        public static implicit operator PathString(String s)
        {
            return s != null ? new PathString(s) : null;
        }

        public override string ToString()
        {
            return Value == "" ? "." : Value;
        }

        public string ToString(PathStringStyle Style)
        {
            if (Style == PathStringStyle.Windows)
            {
                return ToString().Replace('/', '\\');
            }
            else if (Style == PathStringStyle.Unix)
            {
                return ToString().Replace('\\', '/');
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
        public PathString ToWslPath()
        {
            var Parts = this.Parts;
            if ((Parts.Count >= 1) && Parts.First().EndsWith(Slash) && (Parts.First().Length == 3))
            {
                var NewParts = (new List<String> { "/", "mnt", Parts.First().First().ToString().ToLowerInvariant() }).Concat(Parts.Skip(1)).ToList();
                return Join(NewParts);
            }
            return this;
        }
    }
    public enum PathStringStyle
    {
        Windows,
        Unix
    }
    public static class PathExt
    {
        public static PathString AsPath(this String s)
        {
            if (s == null) { return null; }
            return new PathString(s);
        }
    }
}
