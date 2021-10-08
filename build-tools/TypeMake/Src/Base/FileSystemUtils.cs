using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace TypeMake
{
    public static class FileSystemUtils
    {
        public static bool HasFiles(PathString Path, String SearchPattern, SearchOption SearchOption)
        {
            if (!Directory.Exists(Path)) { return false; }
            return Directory.EnumerateFiles(Path, SearchPattern, SearchOption).Count() > 0;
        }

        public static IEnumerable<PathString> GetFiles(PathString Path, String SearchPattern, SearchOption SearchOption)
        {
            if (!Directory.Exists(Path)) { return new List<PathString>(); }
            return Directory.EnumerateFiles(Path, SearchPattern, SearchOption).Select(f => f.AsPath());
        }
        public static IEnumerable<PathString> GetDirectories(PathString Path, String SearchPattern, SearchOption SearchOption)
        {
            if (!Directory.Exists(Path)) { return new List<PathString>(); }
            return Directory.EnumerateDirectories(Path, SearchPattern, SearchOption).Select(d => d.AsPath());
        }
        public static IEnumerable<PathString> GetFileSystemEntries(PathString Path, String SearchPattern, SearchOption SearchOption)
        {
            if (!Directory.Exists(Path)) { return new List<PathString>(); }
            return Directory.EnumerateFileSystemEntries(Path, SearchPattern, SearchOption).Select(f => f.AsPath());
        }

        public static IEnumerable<KeyValuePair<PathString, FileInfo>> GetRelativeFiles(PathString Path, String SearchPattern, SearchOption SearchOption)
        {
            if (!Directory.Exists(Path)) { return new List<KeyValuePair<PathString, FileInfo>>(); }
            return GetFiles(Path, SearchPattern, SearchOption).Select(f =>
            {
                var fi = new FileInfo(f);
                var RelativePath = f.RelativeTo(Path);
                return new KeyValuePair<PathString, FileInfo>(RelativePath, fi);
            });
        }
        public static IEnumerable<KeyValuePair<PathString, FileInfo>> GetRelativeDirectories(PathString Path, String SearchPattern, SearchOption SearchOption)
        {
            if (!Directory.Exists(Path)) { return new List<KeyValuePair<PathString, FileInfo>>(); }
            return GetDirectories(Path, SearchPattern, SearchOption).Select(d =>
            {
                var fi = new FileInfo(d);
                var RelativePath = d.RelativeTo(Path);
                return new KeyValuePair<PathString, FileInfo>(RelativePath, fi);
            });
        }
        public static IEnumerable<KeyValuePair<PathString, FileInfo>> GetRelativeFileSystemEntries(PathString Path, String SearchPattern, SearchOption SearchOption)
        {
            if (!Directory.Exists(Path)) { return new List<KeyValuePair<PathString, FileInfo>>(); }
            return GetFileSystemEntries(Path, SearchPattern, SearchOption).Select(f =>
            {
                var fi = new FileInfo(f);
                var RelativePath = f.RelativeTo(Path);
                return new KeyValuePair<PathString, FileInfo>(RelativePath, fi);
            });
        }
    }
}
