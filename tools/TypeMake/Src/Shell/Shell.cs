using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TypeMake
{
    public static class Shell
    {
        private class PushDirectoryDisposee : IDisposable
        {
            public PathString OriginalDir;
            public void Dispose()
            {
                Environment.CurrentDirectory = OriginalDir;
            }
        }

        public static IDisposable PushDirectory(PathString Dir)
        {
            var d = new PushDirectoryDisposee { OriginalDir = Environment.CurrentDirectory };
            Environment.CurrentDirectory = Dir.FullPath;
            return d;
        }

        public enum OperatingSystemType
        {
            Windows,
            Linux,
            Mac,
            Unknown
        }
        public enum OperatingSystemArchitectureType
        {
            x86,
            x86_64,
            Unknown
        }

        private static Object OperatingSystemLockee = new Object();
        private static OperatingSystemType? OperatingSystemValue = null;
        public static OperatingSystemType OperatingSystem
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
                            OperatingSystemValue = OperatingSystemType.Windows;
                        }
                        else if (p == PlatformID.Unix)
                        {
                            if (File.Exists("/usr/lib/libc.dylib"))
                            {
                                OperatingSystemValue = OperatingSystemType.Mac;
                            }
                            else
                            {
                                OperatingSystemValue = OperatingSystemType.Linux;
                            }
                        }
                        else if (p == PlatformID.MacOSX)
                        {
                            OperatingSystemValue = OperatingSystemType.Mac;
                        }
                        else
                        {
                            OperatingSystemValue = OperatingSystemType.Unknown;
                        }
                    }
                    return OperatingSystemValue.Value;
                }
            }
        }

        private static Object OperatingSystemArchitectureLockee = new Object();
        private static OperatingSystemArchitectureType? OperatingSystemArchitectureValue = null;
        public static OperatingSystemArchitectureType OperatingSystemArchitecture
        {
            get
            {
                lock (OperatingSystemArchitectureLockee)
                {
                    if (OperatingSystemArchitectureValue == null)
                    {
                        if (Environment.Is64BitOperatingSystem)
                        {
                            OperatingSystemArchitectureValue = OperatingSystemArchitectureType.x86_64;
                        }
                        else
                        {
                            OperatingSystemArchitectureValue = OperatingSystemArchitectureType.x86;
                        }
                        //other architecture not supported now
                    }
                    return OperatingSystemArchitectureValue.Value;
                }
            }
        }

        public static PathString TryLocate(String ProgramName)
        {
            foreach (var Dir in Environment.GetEnvironmentVariable("PATH").Split(Path.PathSeparator))
            {
                var p = Dir.AsPath() / ProgramName;
                if (File.Exists(p))
                {
                    return ResolvePathFromSystem(p.FullPath);
                }
                if (OperatingSystem == OperatingSystemType.Windows)
                {
                    if (File.Exists(p + ".exe"))
                    {
                        return ResolvePathFromSystem((p + ".exe").FullPath);
                    }
                    else if (File.Exists(p + ".cmd"))
                    {
                        return ResolvePathFromSystem((p + ".cmd").FullPath);
                    }
                    else if (File.Exists(p + ".bat"))
                    {
                        return ResolvePathFromSystem((p + ".bat").FullPath);
                    }
                }
            }
            return null;
        }
        private static PathString ResolvePathFromSystem(PathString p)
        {
            //do resolving only on Windows, as other operating systems are mostly case-sensitive and have problems when lacking permissions
            if (OperatingSystem == OperatingSystemType.Windows)
            {
                var Remaining = new LinkedList<String>(p.Parts);
                if (Remaining.Count == 0) { return p; }
                var CurrentPath = Remaining.First.Value.AsPath();
                Remaining.RemoveFirst();
                while (Remaining.Count > 0)
                {
                    var l = Directory.GetFileSystemEntries(CurrentPath, Remaining.First.Value);
                    if (l.Length == 0)
                    {
                        break;
                    }
                    CurrentPath = l.First();
                    Remaining.RemoveFirst();
                }
                while (Remaining.Count > 0)
                {
                    CurrentPath /= Remaining.First.Value;
                    Remaining.RemoveFirst();
                }
                if (!String.Equals(CurrentPath, p, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException();
                }
                return CurrentPath;
            }
            else
            {
                return p;
            }
        }

        public static int Execute(String ProgramPath, params String[] Arguments)
        {
            var psi = CreateExecuteStartInfo(ProgramPath, Arguments);
            psi.CreateNoWindow = true;
            var Style = OperatingSystem == OperatingSystemType.Windows ? ShellArgumentStyle.CMD : ShellArgumentStyle.Bash;
            var CommandLine = Arguments.Length == 0 ? EscapeArgumentForShell(ProgramPath, Style) : EscapeArgumentForShell(ProgramPath, Style) + " " + String.Join(" ", Arguments.Select(a => EscapeArgumentForShell(a, Style)));
            Console.WriteLine(CommandLine);
            var p = Process.Start(psi);
            p.WaitForExit();
            return p.ExitCode;
        }
        public static KeyValuePair<int, String> ExecuteAndGetOutput(String ProgramPath, params String[] Arguments)
        {
            var psi = CreateExecuteStartInfo(ProgramPath, Arguments);
            psi.CreateNoWindow = true;
            psi.RedirectStandardOutput = true;
            var Style = OperatingSystem == OperatingSystemType.Windows ? ShellArgumentStyle.CMD : ShellArgumentStyle.Bash;
            var CommandLine = Arguments.Length == 0 ? EscapeArgumentForShell(ProgramPath, Style) : EscapeArgumentForShell(ProgramPath, Style) + " " + String.Join(" ", Arguments.Select(a => EscapeArgumentForShell(a, Style)));
            Console.WriteLine(CommandLine);
            var p = Process.Start(psi);
            using (var Reader = p.StandardOutput)
            {
                var sb = new StringBuilder();
                var Buffer = new Char[256];
                while (true)
                {
                    var Count = Reader.Read(Buffer, 0, Buffer.Length);
                    if (Count == 0)
                    {
                        break;
                    }
                    sb.Append(Buffer, 0, Count);
                }
                p.WaitForExit();
                return new KeyValuePair<int, String>(p.ExitCode, sb.ToString());
            }
        }
        public static KeyValuePair<int, String> ExecuteAndGetOutput(String ProgramPath, Encoding Encoding, params String[] Arguments)
        {
            var psi = CreateExecuteStartInfo(ProgramPath, Arguments);
            psi.CreateNoWindow = true;
            psi.RedirectStandardOutput = true;
            psi.StandardOutputEncoding = Encoding;
            var Style = OperatingSystem == OperatingSystemType.Windows ? ShellArgumentStyle.CMD : ShellArgumentStyle.Bash;
            var CommandLine = Arguments.Length == 0 ? EscapeArgumentForShell(ProgramPath, Style) : EscapeArgumentForShell(ProgramPath, Style) + " " + String.Join(" ", Arguments.Select(a => EscapeArgumentForShell(a, Style)));
            Console.WriteLine(CommandLine);
            var p = Process.Start(psi);
            using (var Reader = p.StandardOutput)
            {
                var sb = new StringBuilder();
                var Buffer = new Char[256];
                while (true)
                {
                    var Count = Reader.Read(Buffer, 0, Buffer.Length);
                    if (Count == 0)
                    {
                        break;
                    }
                    sb.Append(Buffer, 0, Count);
                }
                p.WaitForExit();
                return new KeyValuePair<int, String>(p.ExitCode, sb.ToString());
            }
        }
        public static ProcessStartInfo CreateExecuteStartInfo(String ProgramPath, params String[] Arguments)
        {
            return CreateExecuteLineStartInfo(ProgramPath, String.Join(" ", Arguments.Select(arg => EscapeArgument(arg))));
        }
        public static ProcessStartInfo CreateExecuteLineStartInfo(String ProgramPath, String Arguments)
        {
            if (OperatingSystem == OperatingSystemType.Windows)
            {
                if (ProgramPath.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase) || ProgramPath.EndsWith(".bat", StringComparison.OrdinalIgnoreCase))
                {
                    return CreateExecuteLineStartInfoInner("cmd", "/C " + EscapeArgument(ProgramPath) + (Arguments == "" ? "" : " " + Arguments));
                }
            }
            else
            {
                if (ProgramPath.EndsWith(".sh", StringComparison.Ordinal))
                {
                    var BashPath = TryLocate("bash");
                    if (BashPath == null)
                    {
                        throw new InvalidOperationException("BashNotFound");
                    }
                    return CreateExecuteLineStartInfoInner(BashPath, "-c " + EscapeArgument(ProgramPath) + (Arguments == "" ? "" : " " + Arguments));
                }
            }
            return CreateExecuteLineStartInfoInner(ProgramPath, Arguments);
        }
        private static ProcessStartInfo CreateExecuteLineStartInfoInner(String ProgramPath, String Arguments)
        {
            var psi = new ProcessStartInfo()
            {
                FileName = ProgramPath,
                Arguments = Arguments,
                UseShellExecute = false
            };
            return psi;
        }
        public static String EscapeArgument(String Argument)
        {
            return EscapeArgument(Argument, OperatingSystem == OperatingSystemType.Windows ? ArgumentStyle.Windows : ArgumentStyle.Unix);
        }
        public enum ArgumentStyle
        {
            Windows,
            Unix
        }
        private static Regex rBackslashBeforeDoubleQuotes = new Regex(@"\\+((?="")|$)", RegexOptions.ExplicitCapture);
        private static Regex rComplexChars = new Regex(@"[\s!""#$%&'()*,;<>?@\[\\\]^`{|}~]", RegexOptions.ExplicitCapture);
        public static String EscapeArgument(String Argument, ArgumentStyle ArgumentStyle)
        {
            //\0 \r \n can not be escaped
            if (Argument.Any(c => c == '\0' || c == '\r' || c == '\n')) { throw new ArgumentException("InvalidChar"); }
            if (ArgumentStyle == ArgumentStyle.Windows)
            {
                //https://docs.microsoft.com/en-us/cpp/cpp/parsing-cpp-command-line-arguments?view=vs-2017
                //http://csharptest.net/529/how-to-correctly-escape-command-line-arguments-in-c/index.html
                //backslashes before double quotes must be doubled
                return rComplexChars.IsMatch(Argument) ? "\"" + rBackslashBeforeDoubleQuotes.Replace(Argument, s => s.Value + s.Value).Replace("\"", "\\\"") + "\"" : Argument;
            }
            else if (ArgumentStyle == ArgumentStyle.Unix)
            {
                //in mono it was originally implemented using g_shell_parse_argv
                //https://bugzilla.xamarin.com/show_bug.cgi?id=19296
                //https://developer.gnome.org/glib/stable/glib-Shell-related-Utilities.html
                //but upon testing it is found that backslash need to be double in single quotes
                return rComplexChars.IsMatch(Argument) ? "'" + Argument.Replace("\\", "\\\\").Replace("'", "'\\''") + "'" : Argument;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
        public enum ShellArgumentStyle
        {
            CMD,
            CMD_Interactive,
            Bash
        }
        public static String EscapeArgumentForShell(String Argument, ShellArgumentStyle ShellArgumentStyle)
        {
            //\0 \r \n can not be escaped
            if (Argument.Any(c => c == '\0' || c == '\r' || c == '\n')) { throw new ArgumentException("InvalidChar"); }
            if (ShellArgumentStyle == ShellArgumentStyle.CMD)
            {
                //CMD style(batch, without EnableDelayedExpansion)
                return EscapeArgument(Argument, ArgumentStyle.Windows).Replace("%", "%%");
            }
            else if (ShellArgumentStyle == ShellArgumentStyle.CMD_Interactive)
            {
                //CMD terminal window
                return Regex.Replace(EscapeArgument(Argument, ArgumentStyle.Windows), @"[%]+", s => "\"" + String.Join("", s.Value.Select(c => "^" + c)) + "\"");
            }
            else if (ShellArgumentStyle == ShellArgumentStyle.Bash)
            {
                //bash style
                return rComplexChars.IsMatch(Argument) ? "'" + Argument.Replace("'", "'\\''") + "'" : Argument;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public class EnvironmentVariableMemory
        {
            public Dictionary<String, String> Variables = new Dictionary<String, String>();
            public Dictionary<String, List<String>> VariableSelections = new Dictionary<String, List<String>>();
            public Dictionary<String, List<String>> VariableMultipleSelections = new Dictionary<String, List<String>>();
        }
        public class EnvironmentVariableReadOptions
        {
            public bool Quiet { get; set; } = false;
            public Func<String, int, bool, bool, String> Suggester { get; set; } = null;
            public Func<String, KeyValuePair<bool, String>> Validator { get; set; } = null;
            public Func<String, String> PostMapper { get; set; } = null;
            public String DefaultValue { get; set; } = null;
            public String InputDisplay { get; set; } = null;
            public bool IsPassword { get; set; } = false;
            public bool EnableCancellation { get; set; } = true;
            public ConsoleColor? ForegroundColor { get; set; } = null;
            public Action OnInteraction { get; set; } = null;
            public EnvironmentVariableReadOptions()
            {
                Suggester = (v, ConfirmedLength, Cycle, CyclePrevious) => String.IsNullOrEmpty(DefaultValue) ? v : GetCaseInsensitiveDefaultValueSuggester(DefaultValue)(v, ConfirmedLength, Cycle, CyclePrevious);
            }
        }
        public class UserCancelledException : Exception
        {
        }
        public static String RequireEnvironmentVariable(EnvironmentVariableMemory Memory, String Name, EnvironmentVariableReadOptions Options)
        {
            var OriginalForegroundColor = GetForegroundColor();
            var Top = Console.CursorTop;
            var cps = GetConsolePositionState();
            var d = Options.InputDisplay ?? (!String.IsNullOrEmpty(Options.DefaultValue) ? "[" + Options.DefaultValue + "]" : "");
            var v = Environment.GetEnvironmentVariable(Name);
            if (v == "_EMPTY_")
            {
                v = "";
            }
            if (v == null)
            {
                if (Options.Quiet) { throw new InvalidOperationException("Variable '" + Name + "' not exist."); }
                if (Options.OnInteraction != null) { Options.OnInteraction(); }
                if (Options.ForegroundColor != null) { SetForegroundColor(Options.ForegroundColor.Value); }
                Console.Write("'" + Name + "' not exist. Input" + (d == "" ? "" : " " + d) + ": ");
                if (OriginalForegroundColor == null) {  }
                SetForegroundColor(OriginalForegroundColor);
                try
                {
                    v = Options.IsPassword ? ReadLinePassword(Options.EnableCancellation) : ReadLineWithSuggestion(Options.Suggester, Options.EnableCancellation);
                    if (v == "")
                    {
                        v = Options.DefaultValue ?? "";
                    }
                }
                finally
                {
                    if (Shell.OperatingSystem == Shell.OperatingSystemType.Windows)
                    {
                        var cpsNew = GetConsolePositionState();
                        SetConsolePositionState(cps);
                        BackspaceCursorToLine(Top);
                        SetConsolePositionState(cpsNew);
                    }
                }
            }
            while (true)
            {
                if (Options.Validator == null) { break; }
                var ValidationResult = Options.Validator(v);
                if (ValidationResult.Key) { break; }
                var ValidationMessage = ValidationResult.Value == "" ? "Variable '" + Name + "' invalid." : "Variable '" + Name + "' invalid. " + ValidationResult.Value;
                if (Options.Quiet) { throw new InvalidOperationException(ValidationMessage); }
                if (Options.OnInteraction != null) { Options.OnInteraction(); }
                if (Options.ForegroundColor != null) { SetForegroundColor(Options.ForegroundColor.Value); }
                Console.Write(ValidationMessage + " Input" + (d == "" ? "" : " " + d) + ": ");
                SetForegroundColor(OriginalForegroundColor);
                try
                {
                    v = Options.IsPassword ? ReadLinePassword(Options.EnableCancellation) : ReadLineWithSuggestion(Options.Suggester, Options.EnableCancellation);
                    if (v == "")
                    {
                        v = Options.DefaultValue ?? "";
                    }
                }
                finally
                {
                    if (Shell.OperatingSystem == Shell.OperatingSystemType.Windows)
                    {
                        var cpsNew = GetConsolePositionState();
                        SetConsolePositionState(cps);
                        BackspaceCursorToLine(Top);
                        SetConsolePositionState(cpsNew);
                    }
                }
            }
            if (Options.PostMapper != null)
            {
                v = Options.PostMapper(v);
            }
            if (Options.ForegroundColor != null) { SetForegroundColor(Options.ForegroundColor.Value); }
            if (Options.IsPassword)
            {
                Console.WriteLine(Name + "=[***]");
            }
            else
            {
                Console.WriteLine(Name + "=" + v);
            }
            SetForegroundColor(OriginalForegroundColor);
            Memory.Variables[Name] = v == "" ? "_EMPTY_" : v;
            return v;
        }
        public static T RequireEnvironmentVariableEnum<T>(EnvironmentVariableMemory Memory, String Name, bool Quiet, HashSet<T> Selections, T DefaultValue = default(T), Action<EnvironmentVariableReadOptions> OnOptionCustomization = null) where T : struct
        {
            var InputDisplay = String.Join("|", Selections.Select(e => e.Equals(DefaultValue) ? "[" + e.ToString() + "]" : e.ToString()));
            T Output = default(T);
            var Options = new EnvironmentVariableReadOptions
            {
                Quiet = Quiet,
                Suggester = GetCaseInsensitiveSelectionSuggester(Selections.Select(e => e.ToString())),
                Validator = v =>
                {
                    T o;
                    var b = Enum.TryParse<T>(v, true, out o);
                    if (!Selections.Contains(o)) { return new KeyValuePair<bool, String>(false, ""); }
                    Output = o;
                    return new KeyValuePair<bool, String>(b, "");
                },
                PostMapper = v => Output.ToString(),
                DefaultValue = DefaultValue.ToString(),
                InputDisplay = InputDisplay
            };
            if (OnOptionCustomization != null) { OnOptionCustomization(Options); }
            var s = RequireEnvironmentVariable(Memory, Name, Options);
            if (Memory.VariableSelections.ContainsKey(Name))
            {
                Memory.VariableSelections[Name] = Selections.Select(v => v.ToString()).ToList();
            }
            else
            {
                Memory.VariableSelections.Add(Name, Selections.Select(v => v.ToString()).ToList());
            }
            return Output;
        }
        public static T RequireEnvironmentVariableEnum<T>(EnvironmentVariableMemory Memory, String Name, bool Quiet, T DefaultValue = default(T), Action<EnvironmentVariableReadOptions> OnOptionCustomization = null) where T : struct
        {
            return RequireEnvironmentVariableEnum<T>(Memory, Name, Quiet, new HashSet<T>(Enum.GetValues(typeof(T)).Cast<T>()), DefaultValue, OnOptionCustomization);
        }
        public static String RequireEnvironmentVariableSelection(EnvironmentVariableMemory Memory, String Name, bool Quiet, HashSet<String> Selections, String DefaultValue = "", Action<EnvironmentVariableReadOptions> OnOptionCustomization = null)
        {
            var InputDisplay = String.Join("|", Selections.Select(c => c.Equals(DefaultValue) ? "[" + c + "]" : c));
            var Options = new EnvironmentVariableReadOptions
            {
                Quiet = Quiet,
                Suggester = GetCaseInsensitiveSelectionSuggester(Selections),
                Validator = v => new KeyValuePair<bool, String>(Selections.Contains(v), ""),
                DefaultValue = DefaultValue.ToString(),
                InputDisplay = InputDisplay
            };
            if (OnOptionCustomization != null) { OnOptionCustomization(Options); }
            var s = RequireEnvironmentVariable(Memory, Name, Options);
            if (Memory.VariableSelections.ContainsKey(Name))
            {
                Memory.VariableSelections[Name] = Selections.ToList();
            }
            else
            {
                Memory.VariableSelections.Add(Name, Selections.ToList());
            }
            return s;
        }
        public static List<String> RequireEnvironmentVariableMultipleSelection(EnvironmentVariableMemory Memory, String Name, bool Quiet, HashSet<String> Selections, HashSet<String> DefaultSelections = null, Func<List<String>, KeyValuePair<bool, String>> Validator = null, Action<EnvironmentVariableReadOptions> OnOptionCustomization = null)
        {
            var InputDisplay = String.Join(" ", Selections.Select(v => (DefaultSelections != null) && DefaultSelections.Contains(v) ? "[" + v + "]" : v));
            var Options = new EnvironmentVariableReadOptions
            {
                Quiet = Quiet,
                Suggester = GetCaseInsensitiveMultipleSelectionSuggester(Selections),
                Validator = v =>
                {
                    var Parts = v.Split(' ').Where(Part => Part != "").ToList();
                    var UnknownSelections = Parts.Where(Part => !Selections.Contains(Part)).ToList();
                    if (UnknownSelections.Count > 0) { return new KeyValuePair<bool, String>(false, $"Unknown selection: {String.Join(" ", UnknownSelections)}."); }
                    var DuplicateSelections = Parts.GroupBy(Part => Part).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
                    if (DuplicateSelections.Count > 0) { return new KeyValuePair<bool, String>(false, $"Duplicate selection: {String.Join(" ", DuplicateSelections)}."); }
                    if (Validator != null) { return Validator(Parts); }
                    return new KeyValuePair<bool, String>(true, "");
                },
                DefaultValue = String.Join(" ", Selections.Intersect(DefaultSelections)),
                InputDisplay = InputDisplay
            };
            if (OnOptionCustomization != null) { OnOptionCustomization(Options); }
            var s = RequireEnvironmentVariable(Memory, Name, Options);
            if (Memory.VariableMultipleSelections.ContainsKey(Name))
            {
                Memory.VariableMultipleSelections[Name] = Selections.ToList();
            }
            else
            {
                Memory.VariableMultipleSelections.Add(Name, Selections.ToList());
            }
            return s.Split(' ').Where(Part => Part != "").ToList();
        }
        public static bool RequireEnvironmentVariableBoolean(EnvironmentVariableMemory Memory, String Name, bool Quiet, bool DefaultValue = false, Action<EnvironmentVariableReadOptions> OnOptionCustomization = null)
        {
            var Selections = new List<bool> { false, true };
            var InputDisplay = String.Join("|", Selections.Select(c => c.Equals(DefaultValue) ? "[" + c.ToString() + "]" : c.ToString()));
            bool Output = false;
            var Options = new EnvironmentVariableReadOptions
            {
                Quiet = Quiet,
                Suggester = GetCaseInsensitiveSelectionSuggester(new List<String> { "False", "True" }),
                Validator = v =>
                {
                    if (String.Equals(v, "False", StringComparison.OrdinalIgnoreCase))
                    {
                        Output = false;
                        return new KeyValuePair<bool, String>(true, "");
                    }
                    else if (String.Equals(v, "True", StringComparison.OrdinalIgnoreCase))
                    {
                        Output = true;
                        return new KeyValuePair<bool, String>(true, "");
                    }
                    else
                    {
                        return new KeyValuePair<bool, String>(false, "");
                    }
                },
                PostMapper = v => Output.ToString(),
                DefaultValue = DefaultValue.ToString(),
                InputDisplay = InputDisplay
            };
            if (OnOptionCustomization != null) { OnOptionCustomization(Options); }
            var s = RequireEnvironmentVariable(Memory, Name, Options);
            return Output;
        }
        public static PathString RequireEnvironmentVariableFilePath(EnvironmentVariableMemory Memory, String Name, bool Quiet, PathString DefaultValue = null, Func<PathString, KeyValuePair<bool, String>> Validator = null, Action<EnvironmentVariableReadOptions> OnOptionCustomization = null)
        {
            Func<String, KeyValuePair<bool, String>> ValidatorWrapper = p => Validator(p);
            var Options = new EnvironmentVariableReadOptions
            {
                Quiet = Quiet,
                Suggester = GetPathSuggester(true, true, DefaultValue),
                Validator = Validator != null ? ValidatorWrapper : (p => File.Exists(p) ? new KeyValuePair<bool, String>(true, "") : new KeyValuePair<bool, String>(false, "File not found.")),
                PostMapper = p => p.AsPath().FullPath,
                DefaultValue = DefaultValue
            };
            if (OnOptionCustomization != null) { OnOptionCustomization(Options); }
            var s = RequireEnvironmentVariable(Memory, Name, Options);
            return s;
        }
        public static PathString RequireEnvironmentVariableDirectoryPath(EnvironmentVariableMemory Memory, String Name, bool Quiet, PathString DefaultValue = null, Func<PathString, KeyValuePair<bool, String>> Validator = null, Action<EnvironmentVariableReadOptions> OnOptionCustomization = null)
        {
            Func<String, KeyValuePair<bool, String>> ValidatorWrapper = p => Validator(p);
            var Options = new EnvironmentVariableReadOptions
            {
                Quiet = Quiet,
                Suggester = GetPathSuggester(false, true, DefaultValue),
                Validator = Validator != null ? ValidatorWrapper : (p => Directory.Exists(p) ? new KeyValuePair<bool, String>(true, "") : new KeyValuePair<bool, String>(false, "Directory not found.")),
                PostMapper = p => p.AsPath().FullPath,
                DefaultValue = DefaultValue
            };
            if (OnOptionCustomization != null) { OnOptionCustomization(Options); }
            var s = RequireEnvironmentVariable(Memory, Name, Options);
            return s;
        }
        private static Func<String, int, bool, bool, String> GetCaseInsensitiveDefaultValueSuggester(String DefaultValue)
        {
            return (v, ConfirmedLength, Cycle, CyclePrevious) =>
            {
                var vConfirmed = v.Substring(0, ConfirmedLength);
                if (!Cycle && (vConfirmed == ""))
                {
                    return vConfirmed;
                }
                if (DefaultValue.StartsWith(vConfirmed, StringComparison.OrdinalIgnoreCase))
                {
                    return DefaultValue;
                }
                return vConfirmed;
            };
        }
        private static Func<String, int, bool, bool, String> GetCaseInsensitiveSelectionSuggester(IEnumerable<String> Selections)
        {
            return (v, ConfirmedLength, Cycle, CyclePrevious) =>
            {
                var vConfirmed = v.Substring(0, ConfirmedLength);
                if (!Cycle && (vConfirmed == ""))
                {
                    return vConfirmed;
                }
                String FirstMatched = null;
                String PreviousMatched = null;
                bool HasExactMatch = false;
                //if there is an exact match and in cycle mode, the next or previous match is returned
                //if there is an exact match and not in cycle mode, the exact match is returned
                //otherwise, the first match is returned
                //if there is no match and in cycle mode, the original suggestion is returned
                //if there is no match and not in cycle mode, the original input is returned
                foreach (var s in Selections.Where(s => s.StartsWith(vConfirmed, StringComparison.OrdinalIgnoreCase)))
                {
                    if (v.Equals(s, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!Cycle)
                        {
                            return s;
                        }
                        if (CyclePrevious)
                        {
                            return PreviousMatched ?? s;
                        }
                        HasExactMatch = true;
                        continue;
                    }
                    if (FirstMatched == null)
                    {
                        FirstMatched = s;
                    }
                    PreviousMatched = s;
                    if (HasExactMatch)
                    {
                        return s;
                    }
                }
                return FirstMatched ?? (Cycle ? v : vConfirmed);
            };
        }
        private static Func<String, int, bool, bool, String> GetCaseInsensitiveMultipleSelectionSuggester(IEnumerable<String> Selections)
        {
            var Inner = GetCaseInsensitiveSelectionSuggester(Selections);
            return (v, ConfirmedLength, Cycle, CyclePrevious) =>
            {
                var StartOfLastSelection = 0;
                if (ConfirmedLength > 0)
                {
                    var StartOfLastSpace = v.LastIndexOf(' ', ConfirmedLength - 1);
                    if (StartOfLastSpace >= 0)
                    {
                        StartOfLastSelection = StartOfLastSpace + 1;
                    }
                }
                var vLast = v.Substring(StartOfLastSelection);
                return v.Substring(0, StartOfLastSelection) + Inner(vLast, ConfirmedLength - StartOfLastSelection, Cycle, CyclePrevious);
            };
        }
        private static Func<String, int, bool, bool, String> GetPathSuggester(bool EnableFile, bool EnableDirectory, PathString DefaultValue)
        {
            return (v, ConfirmedLength, Cycle, CyclePrevious) =>
            {
                var vConfirmed = v.Substring(0, ConfirmedLength);
                if (!Cycle && (vConfirmed == ""))
                {
                    return vConfirmed;
                }
                if (vConfirmed == "")
                {
                    return DefaultValue ?? "";
                }
                PathString ConfirmedPath;
                PathString ConfirmedParts;
                try
                {
                    ConfirmedPath = vConfirmed.AsPath();
                    if (vConfirmed.EndsWith("\\") || vConfirmed.EndsWith("/"))
                    {
                        ConfirmedParts = ConfirmedPath;
                    }
                    else
                    {
                        var Parts = ConfirmedPath.Parts;
                        if (Parts.Count <= 1)
                        {
                            ConfirmedParts = "";
                        }
                        else
                        {
                            ConfirmedParts = PathString.Join(Parts.AsEnumerable().Reverse().Skip(1).Reverse());
                        }
                    }
                }
                catch (ArgumentException)
                {
                    return vConfirmed;
                }
                if ((ConfirmedParts != "") && !Directory.Exists(ConfirmedParts)) { return vConfirmed; }
                if (EnableFile && File.Exists(ConfirmedPath) && !Cycle) { return vConfirmed; }
                if (EnableDirectory && Directory.Exists(ConfirmedPath) && !Cycle) { return vConfirmed; }
                var FileSelections = EnableFile ? Directory.EnumerateFiles(ConfirmedParts, "*", SearchOption.TopDirectoryOnly).Select(f => f.AsPath().FileName).ToList() : new List<string> { };
                var DirectorySelections = EnableDirectory ? Directory.EnumerateDirectories(ConfirmedParts, "*", SearchOption.TopDirectoryOnly).Select(d => d.AsPath().FileName).ToList() : new List<string> { };
                var Selections = FileSelections.Concat(DirectorySelections).Select(s => ConfirmedParts / s).ToList();
                String FirstMatched = null;
                String PreviousMatched = null;
                bool HasExactMatch = false;
                //if there is an exact match and in cycle mode, the next or previous match is returned
                //if there is an exact match and not in cycle mode, the exact match is returned
                //otherwise, the first match is returned
                //if there is no match and in cycle mode, the original suggestion is returned
                //if there is no match and not in cycle mode, the original input is returned
                foreach (var s in Selections.Where(s => s.ToString().StartsWith(vConfirmed, StringComparison.OrdinalIgnoreCase)))
                {
                    if (v.Equals(s, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!Cycle)
                        {
                            return s;
                        }
                        if (CyclePrevious)
                        {
                            return PreviousMatched ?? s;
                        }
                        HasExactMatch = true;
                        continue;
                    }
                    if (FirstMatched == null)
                    {
                        FirstMatched = s;
                    }
                    PreviousMatched = s;
                    if (HasExactMatch)
                    {
                        return s;
                    }
                }
                return FirstMatched ?? (Cycle ? v : vConfirmed);
            };
        }
        public static String ReadLinePassword(bool EnableCancellation)
        {
            var l = new LinkedList<Char>();
            while (true)
            {
                var ki = Console.ReadKey(true);
                if (EnableCancellation && (ki.Key == ConsoleKey.Escape))
                {
                    throw new UserCancelledException();
                }
                if (ki.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }
                if (ki.Key == ConsoleKey.Backspace)
                {
                    l.RemoveLast();
                }
                else
                {
                    var c = ki.KeyChar;
                    if (Char.IsControl(c)) { continue; }
                    l.AddLast(ki.KeyChar);
                }
            }
            return new String(l.ToArray());
        }
        public static String ReadLineWithSuggestion(Func<String, int, bool, bool, String> Suggester, bool EnableCancellation)
        {
            if (OperatingSystem == OperatingSystemType.Windows)
            {
                var Confirmed = new LinkedList<KeyValuePair<Char, KeyValuePair<int, int>>>();
                var Suggested = new LinkedList<KeyValuePair<Char, KeyValuePair<int, int>>>();
                LinkedListNode<KeyValuePair<Char, KeyValuePair<int, int>>> CurrentCharNode = null;
                int ConfirmedLastTop = Console.CursorTop;
                int ConfirmedLastLeft = Console.CursorLeft;
                int SuggestedLastTop = Console.CursorTop;
                int SuggestedLastLeft = Console.CursorLeft;
                void ClearSuggestion()
                {
                    Suggested = new LinkedList<KeyValuePair<Char, KeyValuePair<int, int>>>();
                }
                void RefreshSuggestion()
                {
                    if (Suggester == null) { return; }
                    var v = new String(Confirmed.Select(p => p.Key).Concat(Suggested.Select(p => p.Key)).ToArray());
                    var vSuggested = Suggester(v, Confirmed.Count, false, false).Substring(Confirmed.Count);
                    Suggested = new LinkedList<KeyValuePair<Char, KeyValuePair<int, int>>>(vSuggested.Select(c => new KeyValuePair<Char, KeyValuePair<int, int>>(c, new KeyValuePair<int, int>(SuggestedLastTop, SuggestedLastLeft))));
                }
                void CycleSuggestion(bool CyclePrevious)
                {
                    if (Suggester == null) { return; }
                    var v = new String(Confirmed.Select(p => p.Key).Concat(Suggested.Select(p => p.Key)).ToArray());
                    var vSuggested = Suggester(v, Confirmed.Count, true, CyclePrevious).Substring(Confirmed.Count);
                    Suggested = new LinkedList<KeyValuePair<Char, KeyValuePair<int, int>>>(vSuggested.Select(c => new KeyValuePair<Char, KeyValuePair<int, int>>(c, new KeyValuePair<int, int>(SuggestedLastTop, SuggestedLastLeft))));
                }
                void MoveSuggestionToConfirmed()
                {
                    foreach (var n in Suggested.ToList())
                    {
                        Console.Write(n.Key);
                        Confirmed.AddLast(n);
                    }
                    Suggested.Clear();
                    ConfirmedLastTop = SuggestedLastTop;
                    ConfirmedLastLeft = SuggestedLastLeft;
                    RefreshCharsAfterCursor();
                }
                void RefreshCharsAfterCursor()
                {
                    var Top = Console.CursorTop;
                    var Left = Console.CursorLeft;
                    MoveCursorToPosition(SuggestedLastTop, SuggestedLastLeft);
                    BackspaceCursorToPosition(Top, Left);
                    var Next = CurrentCharNode;
                    while (Next != null)
                    {
                        Next.Value = new KeyValuePair<Char, KeyValuePair<int, int>>(Next.Value.Key, new KeyValuePair<int, int>(Console.CursorTop, Console.CursorLeft));
                        Console.Write(Next.Value.Key);
                        Next = Next.Next;
                    }
                    ConfirmedLastTop = Console.CursorTop;
                    ConfirmedLastLeft = Console.CursorLeft;
                    Next = Suggested.First;
                    SetBackgroundColor(ConsoleColor.Blue);
                    SetForegroundColor(ConsoleColor.Yellow);
                    while (Next != null)
                    {
                        Next.Value = new KeyValuePair<Char, KeyValuePair<int, int>>(Next.Value.Key, new KeyValuePair<int, int>(Console.CursorTop, Console.CursorLeft));
                        Console.Write(Next.Value.Key);
                        Next = Next.Next;
                    }
                    ResetColor();
                    SuggestedLastTop = Console.CursorTop;
                    SuggestedLastLeft = Console.CursorLeft;
                    MoveCursorToPosition(Top, Left);
                }
                while (true)
                {
                    var Top = Console.CursorTop;
                    var Left = Console.CursorLeft;
                    var cps = GetConsolePositionState();
                    var ki = Console.ReadKey(true);
                    if (Console.BufferWidth != cps.BufferWidth)
                    {
                        var BackupTop = Console.CursorTop;
                        var BackupLeft = Console.CursorLeft;
                        var cpsBackup = GetConsolePositionState();
                        var BackupCharNode = CurrentCharNode;
                        SetConsolePositionState(cps);
                        if ((SuggestedLastTop > Top) || ((SuggestedLastTop == Top) && (SuggestedLastLeft > Left)))
                        {
                            MoveCursorToPosition(SuggestedLastTop, SuggestedLastLeft);
                        }
                        CurrentCharNode = Confirmed.First;
                        if (CurrentCharNode != null)
                        {
                            BackspaceCursorToPosition(CurrentCharNode.Value.Value.Key, CurrentCharNode.Value.Value.Value);
                        }
                        else
                        {
                            BackspaceCursorToPosition(SuggestedLastTop, SuggestedLastLeft);
                        }
                        SetConsolePositionState(cpsBackup);
                        SuggestedLastTop = Console.CursorTop;
                        SuggestedLastLeft = Console.CursorLeft;
                        RefreshCharsAfterCursor();
                        CurrentCharNode = BackupCharNode;
                        MoveCursorToPosition(BackupTop, BackupLeft);
                        Top = Console.CursorTop;
                        Left = Console.CursorLeft;
                    }
                    if (EnableCancellation && (ki.Key == ConsoleKey.Escape))
                    {
                        ClearSuggestion();
                        RefreshCharsAfterCursor();
                        throw new UserCancelledException();
                    }
                    if (ki.Key == ConsoleKey.Enter)
                    {
                        Console.WriteLine();
                        break;
                    }
                    if (ki.Key == ConsoleKey.LeftArrow)
                    {
                        if (CurrentCharNode == null)
                        {
                            CurrentCharNode = Confirmed.Last;
                        }
                        else
                        {
                            if (CurrentCharNode.Previous == null) { continue; }
                            CurrentCharNode = CurrentCharNode.Previous;
                        }
                        if (CurrentCharNode == null)
                        {
                            MoveCursorToPosition(ConfirmedLastTop, ConfirmedLastLeft);
                        }
                        else
                        {
                            MoveCursorToPosition(CurrentCharNode.Value.Value);
                        }
                    }
                    else if (ki.Key == ConsoleKey.RightArrow)
                    {
                        if (CurrentCharNode == null)
                        {
                            MoveSuggestionToConfirmed();
                            MoveCursorToPosition(ConfirmedLastTop, ConfirmedLastLeft);
                            continue;
                        }
                        else
                        {
                            CurrentCharNode = CurrentCharNode.Next;
                        }
                        if (CurrentCharNode == null)
                        {
                            MoveCursorToPosition(ConfirmedLastTop, ConfirmedLastLeft);
                        }
                        else
                        {
                            MoveCursorToPosition(CurrentCharNode.Value.Value);
                        }
                    }
                    else if (ki.Key == ConsoleKey.Home)
                    {
                        CurrentCharNode = Confirmed.First;
                        if (CurrentCharNode == null)
                        {
                            MoveCursorToPosition(ConfirmedLastTop, ConfirmedLastLeft);
                        }
                        else
                        {
                            MoveCursorToPosition(CurrentCharNode.Value.Value);
                        }
                    }
                    else if (ki.Key == ConsoleKey.End)
                    {
                        CurrentCharNode = null;
                        MoveCursorToPosition(ConfirmedLastTop, ConfirmedLastLeft);
                        MoveSuggestionToConfirmed();
                        MoveCursorToPosition(ConfirmedLastTop, ConfirmedLastLeft);
                    }
                    else if (ki.Key == ConsoleKey.Backspace)
                    {
                        if (Suggested.Count > 0)
                        {
                            ClearSuggestion();
                            RefreshCharsAfterCursor();
                        }
                        else if (CurrentCharNode != null)
                        {
                            if (CurrentCharNode.Previous != null)
                            {
                                MoveCursorToPosition(CurrentCharNode.Previous.Value.Value);
                                Confirmed.Remove(CurrentCharNode.Previous);
                                RefreshCharsAfterCursor();
                            }
                        }
                        else
                        {
                            if (Confirmed.Last != null)
                            {
                                MoveCursorToPosition(Confirmed.Last.Value.Value);
                                Confirmed.RemoveLast();
                                RefreshCharsAfterCursor();
                            }
                        }
                    }
                    else if (ki.Key == ConsoleKey.Delete)
                    {
                        if (CurrentCharNode != null)
                        {
                            var Next = CurrentCharNode.Next;
                            Confirmed.Remove(CurrentCharNode);
                            CurrentCharNode = Next;
                            RefreshSuggestion();
                            RefreshCharsAfterCursor();
                        }
                    }
                    else if (ki.Key == ConsoleKey.Tab)
                    {
                        if (ki.Modifiers == ConsoleModifiers.Shift)
                        {
                            CycleSuggestion(true);
                            RefreshCharsAfterCursor();
                        }
                        else
                        {
                            CycleSuggestion(false);
                            RefreshCharsAfterCursor();
                        }
                    }
                    else
                    {
                        var c = ki.KeyChar;
                        if (Char.IsControl(c)) { continue; }
                        if (CurrentCharNode != null)
                        {
                            Confirmed.AddBefore(CurrentCharNode, new KeyValuePair<Char, KeyValuePair<int, int>>(ki.KeyChar, new KeyValuePair<int, int>(Top, Left)));
                        }
                        else
                        {
                            Confirmed.AddLast(new KeyValuePair<Char, KeyValuePair<int, int>>(ki.KeyChar, new KeyValuePair<int, int>(Top, Left)));
                            CurrentCharNode = null;
                        }
                        Console.Write(c);
                        RefreshSuggestion();
                        RefreshCharsAfterCursor();
                    }
                }
                return new String(Confirmed.Select(p => p.Key).Concat(Suggested.Select(p => p.Key)).ToArray());
            }
            else
            {
                return Console.ReadLine();
            }
        }
        public static void BackspaceCursorToLine(int Top)
        {
            BackspaceCursorToPosition(Top, 0);
        }
        private static void BackspaceCursorToPosition(int Top, int Left)
        {
            while (true)
            {
                if (Console.CursorTop < Top) { break; }
                if (Console.CursorTop == Top)
                {
                    if (Console.CursorLeft <= Left) { break; }
                }
                if ((Console.CursorLeft == 0) && (Console.CursorTop > 0))
                {
                    var PrevLeft = Console.BufferWidth - 1;
                    var PrevTop = Console.CursorTop - 1;
                    MoveCursorToPosition(PrevTop, PrevLeft);
                    Console.Write(" ");
                    MoveCursorToPosition(PrevTop, PrevLeft);
                }
                else
                {
                    var Count = Console.CursorLeft - (Console.CursorTop == Top ? Left : 0);
                    Console.Write(new String('\b', Count) + new String(' ', Count) + new String('\b', Count));
                }
            }
            MoveCursorToPosition(Top, Left);
        }
        private static void MoveCursorToPosition(int Top, int Left)
        {
            Console.SetCursorPosition(Math.Max(0, Math.Min(Left, Console.BufferWidth - 1)), Math.Max(0, Math.Min(Top, Console.BufferHeight - 1)));
        }
        private static void MoveCursorToPosition(KeyValuePair<int, int> Pair)
        {
            MoveCursorToPosition(Pair.Key, Pair.Value);
        }
        private static int GetColorCode(ConsoleColor Color, bool Back)
        {
            if (!Back)
            {
                if (Color == ConsoleColor.Black) { return 30; }
                if (Color == ConsoleColor.Red) { return 31; }
                if (Color == ConsoleColor.Green) { return 32; }
                if (Color == ConsoleColor.Yellow) { return 33; }
                if (Color == ConsoleColor.Blue) { return 34; }
                if (Color == ConsoleColor.Magenta) { return 35; }
                if (Color == ConsoleColor.Cyan) { return 36; }
                if (Color == ConsoleColor.White) { return 37; }
                if (Color == ConsoleColor.DarkGray) { return 30; }
                if (Color == ConsoleColor.DarkRed) { return 31; }
                if (Color == ConsoleColor.DarkGreen) { return 32; }
                if (Color == ConsoleColor.DarkYellow) { return 33; }
                if (Color == ConsoleColor.DarkBlue) { return 34; }
                if (Color == ConsoleColor.DarkMagenta) { return 35; }
                if (Color == ConsoleColor.DarkCyan) { return 36; }
                if (Color == ConsoleColor.Gray) { return 37; }
                throw new InvalidOperationException();
            }
            else
            {
                if (Color == ConsoleColor.Black) { return 40; }
                if (Color == ConsoleColor.Red) { return 41; }
                if (Color == ConsoleColor.Green) { return 42; }
                if (Color == ConsoleColor.Yellow) { return 43; }
                if (Color == ConsoleColor.Blue) { return 44; }
                if (Color == ConsoleColor.Magenta) { return 45; }
                if (Color == ConsoleColor.Cyan) { return 46; }
                if (Color == ConsoleColor.White) { return 47; }
                if (Color == ConsoleColor.DarkGray) { return 40; }
                if (Color == ConsoleColor.DarkRed) { return 41; }
                if (Color == ConsoleColor.DarkGreen) { return 42; }
                if (Color == ConsoleColor.DarkYellow) { return 43; }
                if (Color == ConsoleColor.DarkBlue) { return 44; }
                if (Color == ConsoleColor.DarkMagenta) { return 45; }
                if (Color == ConsoleColor.DarkCyan) { return 46; }
                if (Color == ConsoleColor.Gray) { return 47; }
                throw new InvalidOperationException();
            }
        }
        private static ConsoleColor? CurrentBackgroundColor = null;
        private static ConsoleColor? CurrentForegroundColor = null;
        public static ConsoleColor? GetBackgroundColor()
        {
            return CurrentBackgroundColor;
        }
        public static ConsoleColor? GetForegroundColor()
        {
            return CurrentForegroundColor;
        }
        public static void SetBackgroundColor(ConsoleColor? Color)
        {
            CurrentBackgroundColor = Color;
            if (OperatingSystem == OperatingSystemType.Windows)
            {
                if (Color == null)
                {
                    Console.ResetColor();
                }
                else
                {
                    Console.BackgroundColor = Color.Value;
                }
            }
            else
            {
                if (Color == null)
                {
                    Console.Write($"\x1B[0m");
                }
                else
                {
                    Console.Write($"\x1B[{GetColorCode(Color.Value, true)}m");
                }
            }
        }
        public static void SetForegroundColor(ConsoleColor? Color)
        {
            CurrentForegroundColor = Color;
            if (OperatingSystem == OperatingSystemType.Windows)
            {
                if (Color == null)
                {
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = Color.Value;
                }
            }
            else
            {
                if (Color == null)
                {
                    Console.Write($"\x1B[0m");
                }
                else
                {
                    Console.Write($"\x1B[{GetColorCode(Color.Value, false)}m");
                }
            }
        }
        public static void ResetColor()
        {
            CurrentBackgroundColor = null;
            CurrentForegroundColor = null;
            if (OperatingSystem == OperatingSystemType.Windows)
            {
                Console.ResetColor();
            }
            else
            {
                Console.Write("\x1B[0m");
            }
        }
        public class ConsolePositionState
        {
            public int BufferWidth = Console.BufferWidth;
            public int BufferHeight = Console.BufferHeight;
            public int WindowWidth = Console.WindowWidth;
            public int WindowHeight = Console.WindowHeight;
            public int WindowTop = Console.WindowTop;
            public int WindowLeft = Console.WindowLeft;
        }
        public static ConsolePositionState GetConsolePositionState()
        {
            return new ConsolePositionState
            {
                BufferWidth = Console.BufferWidth,
                BufferHeight = Console.BufferHeight,
                WindowWidth = Console.WindowWidth,
                WindowHeight = Console.WindowHeight,
                WindowTop = Console.WindowTop,
                WindowLeft = Console.WindowLeft
            };
        }
        public static void SetConsolePositionState(ConsolePositionState s)
        {
            if (OperatingSystem == OperatingSystemType.Windows)
            {
                Console.SetWindowSize(s.WindowWidth, s.WindowHeight);
                Console.SetWindowPosition(s.WindowLeft, s.WindowTop);
                Console.SetBufferSize(s.BufferWidth, s.BufferHeight);
            }
        }
    }
}
