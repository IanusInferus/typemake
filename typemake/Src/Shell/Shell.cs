using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace TypeMake
{
    public static class Shell
    {
        private class PushDirectoryDisposee : IDisposable
        {
            public String OriginalDir;
            public void Dispose()
            {
                Environment.CurrentDirectory = Path.GetFullPath(OriginalDir);
            }
        }

        public static IDisposable PushDirectory(String Dir)
        {
            var d = new PushDirectoryDisposee { OriginalDir = Environment.CurrentDirectory };
            Environment.CurrentDirectory = Path.GetFullPath(Dir);
            return d;
        }

        public enum BuildingOperatingSystemType
        {
            Windows,
            Linux,
            Mac,
            Unknown
        }
        public enum BuildingOperatingSystemArchitectureType
        {
            x86,
            x86_64,
            Unknown
        }

        private static Object OperatingSystemLockee = new Object();
        private static BuildingOperatingSystemType? OperatingSystemValue = null;
        public static BuildingOperatingSystemType OperatingSystem
        {
            get
            {
                lock (OperatingSystemLockee)
                {
                    if (OperatingSystemValue == null)
                    {
                        var p = Environment.OSVersion.Platform;
                        if ((p == PlatformID.Win32NT) || (p == PlatformID.Xbox) || (p == PlatformID.WinCE) || (p == PlatformID.Win32Windows) || (p == PlatformID.Win32S))
                        {
                            OperatingSystemValue = BuildingOperatingSystemType.Windows;
                        }
                        else if (p == PlatformID.Unix)
                        {
                            if (File.Exists("/usr/lib/libc.dylib"))
                            {
                                OperatingSystemValue = BuildingOperatingSystemType.Mac;
                            }
                            else
                            {
                                OperatingSystemValue = BuildingOperatingSystemType.Linux;
                            }
                        }
                        else if (p == PlatformID.MacOSX)
                        {
                            OperatingSystemValue = BuildingOperatingSystemType.Mac;
                        }
                        else
                        {
                            OperatingSystemValue = BuildingOperatingSystemType.Unknown;
                        }
                    }
                    return OperatingSystemValue.Value;
                }
            }
        }

        private static Object OperatingSystemArchitectureLockee = new Object();
        private static BuildingOperatingSystemArchitectureType? OperatingSystemArchitectureValue = null;
        public static BuildingOperatingSystemArchitectureType OperatingSystemArchitecture
        {
            get
            {
                lock (OperatingSystemArchitectureLockee)
                {
                    if (OperatingSystemArchitectureValue == null)
                    {
                        if (Environment.Is64BitOperatingSystem)
                        {
                            OperatingSystemArchitectureValue = BuildingOperatingSystemArchitectureType.x86_64;
                        }
                        else
                        {
                            OperatingSystemArchitectureValue = BuildingOperatingSystemArchitectureType.x86;
                        }
                        //other architecture not supported now
                    }
                    return OperatingSystemArchitectureValue.Value;
                }
            }
        }

        public static String TryLocate(String ProgramName)
        {
            foreach (var Dir in Environment.GetEnvironmentVariable("PATH").Split(Path.PathSeparator))
            {
                var p = Path.Combine(Dir, ProgramName);
                if (File.Exists(p))
                {
                    return GetCaseSensitivePath(Path.GetFullPath(p));
                }
                if (OperatingSystem == BuildingOperatingSystemType.Windows)
                {
                    if (File.Exists(p + ".exe"))
                    {
                        return GetCaseSensitivePath(Path.GetFullPath(p + ".exe"));
                    }
                    else if (File.Exists(p + ".cmd"))
                    {
                        return GetCaseSensitivePath(Path.GetFullPath(p + ".cmd"));
                    }
                    else if (File.Exists(p + ".bat"))
                    {
                        return GetCaseSensitivePath(Path.GetFullPath(p + ".bat"));
                    }
                }
            }
            return null;
        }
        private static string GetCaseSensitivePath(string path)
        {
            var root = Path.GetPathRoot(path);
            foreach (var name in path.Substring(root.Length).Split(Path.DirectorySeparatorChar))
            {
                var l = Directory.GetFileSystemEntries(root, name);
                if (l.Length == 0)
                {
                    break;
                }
                root = l.First();
            }
            root += path.Substring(root.Length);
            return root;
        }

        public static int Execute(String ProgramPath, params String[] Arguments)
        {
            return ExecuteLine(ProgramPath, String.Join(" ", Arguments.Select(arg => EscapeArgument(arg))));
        }
        public static int ExecuteLine(String ProgramPath, String Arguments)
        {
            if (OperatingSystem == BuildingOperatingSystemType.Windows)
            {
                if (ProgramPath.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase) || ProgramPath.EndsWith(".bat", StringComparison.OrdinalIgnoreCase))
                {
                    return ExecuteInner("cmd", "/C " + EscapeArgument(ProgramPath) + (Arguments == "" ? "" : " " + Arguments));
                }
            }
            return ExecuteInner(ProgramPath, Arguments);
        }
        private static int ExecuteInner(String ProgramPath, String Arguments)
        {
            Console.WriteLine(Arguments == "" ? EscapeArgument(ProgramPath) : EscapeArgument(ProgramPath) + " " + Arguments);
            var psi = new ProcessStartInfo()
            {
                FileName = ProgramPath,
                Arguments = Arguments,
                UseShellExecute = false
            };
            var p = Process.Start(psi);
            p.WaitForExit();
            return p.ExitCode;
        }
        public static String EscapeArgument(String Argument)
        {
            var arg = Argument.Replace("\"", "\"\"\"");
            return arg.Contains(' ') ? "\"" + arg + "\"" : arg;
        }

        public static void RequireEnvironmentVariable(Dictionary<String, String> Memory, String Name, out String Value, bool Quiet, Func<String, bool> Validator = null, Func<String, String> PostMapper = null, String DefaultValue = "", String InputDisplay = null, bool OutputVariable = true)
        {
            var d = InputDisplay ?? (!String.IsNullOrEmpty(DefaultValue) ? "[" + DefaultValue + "]" : "");
            var v = Environment.GetEnvironmentVariable(Name);
            if (v == null)
            {
                if (Quiet) { throw new InvalidOperationException("Variable '" + Name + "' not exist."); }
                Console.Write("'" + Name + "' not exist, input" + (d == "" ? "" : " " + d) + ": ");
                v = Console.ReadLine();
                if (v == "")
                {
                    v = DefaultValue;
                }
            }
            while ((Validator != null) && !Validator(v))
            {
                if (Quiet) { throw new InvalidOperationException("Variable '" + Name + "' invalid."); }
                Console.Write("'" + Name + "' invalid, input" + (d == "" ? "" : " " + d) + ": ");
                v = Console.ReadLine();
                if (v == "")
                {
                    v = DefaultValue;
                }
            }
            if (PostMapper != null)
            {
                v = PostMapper(v);
            }
            Value = v;
            Console.WriteLine(Name + "=" + v);
            if (Memory.ContainsKey(Name))
            {
                Memory[Name] = v;
            }
            else
            {
                Memory.Add(Name, v);
            }
        }
        public static void RequireEnvironmentVariableEnum<T>(Dictionary<String, String> Memory, String Name, out T Value, bool Quiet, T DefaultValue = default(T), bool OutputVariable = true) where T : struct
        {
            RequireEnvironmentVariableEnum<T>(Memory, Name, out Value, Quiet, new HashSet<T>(Enum.GetValues(typeof(T)).Cast<T>()), DefaultValue, OutputVariable);
        }
        public static void RequireEnvironmentVariableEnum<T>(Dictionary<String, String> Memory, String Name, out T Value, bool Quiet, HashSet<T> Selections, T DefaultValue = default(T), bool OutputVariable = true) where T : struct
        {
            var InputDisplay = String.Join("|", Selections.Select(e => e.Equals(DefaultValue) ? "[" + e.ToString() + "]" : e.ToString()));
            String s;
            T Output = default(T);
            RequireEnvironmentVariable(Memory, Name, out s, Quiet, v =>
            {
                T o;
                var b = Enum.TryParse<T>(v, true, out o);
                if (!Selections.Contains(o)) { return false; }
                Output = o;
                return b;
            }, v => Output.ToString(), DefaultValue.ToString(), InputDisplay, OutputVariable);
            Value = Output;
        }
        public static void RequireEnvironmentVariableSelection(Dictionary<String, String> Memory, String Name, out String Value, bool Quiet, HashSet<String> Selections, String DefaultValue = "", bool OutputVariable = true)
        {
            var InputDisplay = String.Join("|", Selections.Select(c => c.Equals(DefaultValue) ? "[" + c.ToString() + "]" : c.ToString()));
            String s;
            RequireEnvironmentVariable(Memory, Name, out s, Quiet, v => Selections.Contains(v), null, DefaultValue.ToString(), InputDisplay, OutputVariable);
            Value = s;
        }
        public static void RequireEnvironmentVariableBoolean(Dictionary<String, String> Memory, String Name, out bool Value, bool Quiet, bool DefaultValue = false, bool OutputVariable = true)
        {
            var Selections = new List<bool> { false, true };
            var InputDisplay = String.Join("|", Selections.Select(c => c.Equals(DefaultValue) ? "[" + c.ToString() + "]" : c.ToString()));
            String s;
            bool Output = false;
            RequireEnvironmentVariable(Memory, Name, out s, Quiet, v =>
            {
                if (String.Equals(v, "False", StringComparison.OrdinalIgnoreCase))
                {
                    Output = false;
                    return true;
                }
                else if (String.Equals(v, "True", StringComparison.OrdinalIgnoreCase))
                {
                    Output = true;
                    return true;
                }
                else
                {
                    return false;
                }
            }, v => Output.ToString(), DefaultValue.ToString(), InputDisplay, OutputVariable);
            Value = Output;
        }
    }
}
