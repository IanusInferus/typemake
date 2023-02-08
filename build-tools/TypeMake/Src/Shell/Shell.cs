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
                Console.WriteLine("popd");
                Environment.CurrentDirectory = OriginalDir;
            }
        }

        public static IDisposable PushDirectory(PathString Dir)
        {
            var Style = OperatingSystem == OperatingSystemType.Windows ? ShellArgumentStyle.CMD : ShellArgumentStyle.Bash;
            var Arguments = new List<String> { "pushd", Dir.FullPath };
            var CommandLine = String.Join(" ", Arguments.Select(a => EscapeArgumentForShell(a, Style)));
            Console.WriteLine(CommandLine);

            var d = new PushDirectoryDisposee { OriginalDir = Environment.CurrentDirectory };
            Environment.CurrentDirectory = Dir.FullPath;
            return d;
        }

        public enum OperatingSystemType
        {
            Windows,
            Linux,
            MacOS,
            Unknown
        }
        public enum OperatingSystemArchitectureType
        {
            x86,
            x86_64,
            arm64,
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
                            if (File.Exists("/usr/lib/dyld"))
                            {
                                OperatingSystemValue = OperatingSystemType.MacOS;
                            }
                            else
                            {
                                OperatingSystemValue = OperatingSystemType.Linux;
                            }
                        }
                        else if (p == PlatformID.MacOSX)
                        {
                            OperatingSystemValue = OperatingSystemType.MacOS;
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
                var os = OperatingSystem;
                lock (OperatingSystemArchitectureLockee)
                {
                    if (OperatingSystemArchitectureValue == null)
                    {
                        if (os == OperatingSystemType.MacOS)
                        {
                            var p = ExecuteAndGetOutput("uname", "-m");
                            if (p.Key == 0)
                            {
                                var v = p.Value.Trim('\n');
                                if (v == "x86_64")
                                {
                                    var p2 = ExecuteAndGetOutput("sysctl", "-in", "sysctl.proc_translated");
                                    var v2 = p2.Value.Trim('\n');
                                    if ((p2.Key == 0) && int.TryParse(v2, out var v2i) && (v2i == 1))
                                    {
                                        OperatingSystemArchitectureValue = OperatingSystemArchitectureType.arm64;
                                    }
                                    else
                                    {
                                        OperatingSystemArchitectureValue = OperatingSystemArchitectureType.x86_64;
                                    }
                                }
                                else if (v == "arm64")
                                {
                                    OperatingSystemArchitectureValue = OperatingSystemArchitectureType.arm64;
                                }
                                else
                                {
                                    throw new NotSupportedException();
                                }
                            }
                            else
                            {
                                throw new NotSupportedException();
                            }
                        }
                        else if (Environment.Is64BitOperatingSystem)
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

        private static Object ProcessArchitectureLockee = new Object();
        private static OperatingSystemArchitectureType? ProcessArchitectureValue = null;
        public static OperatingSystemArchitectureType ProcessArchitecture
        {
            get
            {
                var os = OperatingSystem;
                lock (ProcessArchitectureLockee)
                {
                    if (ProcessArchitectureValue == null)
                    {
                        if (os == OperatingSystemType.MacOS)
                        {
                            var p = ExecuteAndGetOutput("uname", "-m");
                            if (p.Key == 0)
                            {
                                var v = p.Value.Trim('\n');
                                if (v == "x86_64")
                                {
                                    ProcessArchitectureValue = OperatingSystemArchitectureType.x86_64;
                                }
                                else if (v == "arm64")
                                {
                                    ProcessArchitectureValue = OperatingSystemArchitectureType.arm64;
                                }
                                else
                                {
                                    throw new NotSupportedException();
                                }
                            }
                            else
                            {
                                throw new NotSupportedException();
                            }
                        }
                        else if (Environment.Is64BitProcess)
                        {
                            ProcessArchitectureValue = OperatingSystemArchitectureType.x86_64;
                        }
                        else
                        {
                            ProcessArchitectureValue = OperatingSystemArchitectureType.x86;
                        }
                        //other architecture not supported now
                    }
                    return ProcessArchitectureValue.Value;
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
                    var l = FileSystemUtils.GetFileSystemEntries(CurrentPath, Remaining.First.Value, SearchOption.TopDirectoryOnly).ToList();
                    if (l.Count == 0)
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

        private static long NewWindowSuppressionValue = 0;
        public static bool NewWindowSuppression
        {
            get
            {
                return System.Threading.Interlocked.Read(ref NewWindowSuppressionValue) != 0;
            }
            set
            {
                System.Threading.Interlocked.Exchange(ref NewWindowSuppressionValue, value ? 1 : 0);
            }
        }

        public static int Execute(String ProgramPath, params String[] Arguments)
        {
            var psi = CreateExecuteStartInfo(ProgramPath, Arguments);
            if (NewWindowSuppression)
            {
                psi.CreateNoWindow = true;
            }
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
            if (NewWindowSuppression)
            {
                psi.CreateNoWindow = true;
            }
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
            if (NewWindowSuppression)
            {
                psi.CreateNoWindow = true;
            }
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
                    return CreateExecuteLineStartInfoInner(BashPath, "-c " + EscapeArgument(EscapeArgument(ProgramPath) + (Arguments == "" ? "" : " " + Arguments)));
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
        private static Regex rComplexChars = new Regex(@"[\s!""#$%&'()*,;<>?@\[\\\]^`{|}~\r\n]", RegexOptions.ExplicitCapture);
        public static String EscapeArgument(String Argument, ArgumentStyle ArgumentStyle)
        {
            //\0 \r \n can not be escaped
            if (Argument.Any(c => c == '\0')) { throw new ArgumentException("InvalidChar"); }
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
                //but upon testing it is found that backslash need to be double in single quotes, before mono 6.6
                //it is now fixed, https://github.com/mono/mono/issues/14724
                return rComplexChars.IsMatch(Argument) ? "'" + String.Join("", Argument.SelectMany(c => c == '\\' ? @"'\\'" : c == '\'' ? @"'\''" : new String(c, 1))) + "'" : Argument;
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
            //\0 can not be escaped
            if (Argument.Any(c => c == '\0')) { throw new ArgumentException("InvalidChar"); }
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
            public bool UseSystemEnvironmentVariable = true;
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
            var d = Options.InputDisplay ?? (!String.IsNullOrEmpty(Options.DefaultValue) ? "[" + Options.DefaultValue + "]" : "");
            String v = null;
            if (Memory.Variables.ContainsKey(Name))
            {
                v = Memory.Variables[Name];
            }
            else
            {
                if (Memory.UseSystemEnvironmentVariable)
                {
                    v = Environment.GetEnvironmentVariable(Name);
                    if (v == "_EMPTY_")
                    {
                        v = "";
                    }
                }
            }
            if ((v == null) && Options.Quiet)
            {
                v = Options.DefaultValue;
            }
            if (v == null)
            {
                if (Options.Quiet) { throw new InvalidOperationException("Variable '" + Name + "' not exist."); }
                if (Options.OnInteraction != null) { Options.OnInteraction(); }
                var PromptText = "'" + Name + "' not exist. Input" + (d == "" ? "" : " " + d) + ": ";
                v = Options.IsPassword ? Terminal.ReadLinePassword(Options.ForegroundColor, PromptText, Options.EnableCancellation) : Terminal.ReadLineWithSuggestion(Options.ForegroundColor, PromptText, Options.Suggester, Options.EnableCancellation);
                if (v == "")
                {
                    v = Options.DefaultValue ?? "";
                }
            }
            while (true)
            {
                if (Options.Validator == null) { break; }
                var ValidationResult = Options.Validator(v);
                if (ValidationResult.Key) { break; }
                var ValidationMessage = "Variable '" + Name + "=" + (Options.IsPassword ? "[***]" : v) + "' invalid." + (ValidationResult.Value == "" ? "" : " " + ValidationResult.Value);
                if (Options.Quiet) { throw new InvalidOperationException(ValidationMessage); }
                if (Options.OnInteraction != null) { Options.OnInteraction(); }
                var PromptText = ValidationMessage + " Input" + (d == "" ? "" : " " + d) + ": ";
                v = Options.IsPassword ? Terminal.ReadLinePassword(Options.ForegroundColor, PromptText, Options.EnableCancellation) : Terminal.ReadLineWithSuggestion(Options.ForegroundColor, PromptText, Options.Suggester, Options.EnableCancellation);
                if (v == "")
                {
                    v = Options.DefaultValue ?? "";
                }
            }
            if (Options.PostMapper != null)
            {
                v = Options.PostMapper(v);
            }
            if (Options.IsPassword)
            {
                Terminal.WriteLine(Options.ForegroundColor, Name + "=[***]");
            }
            else
            {
                Terminal.WriteLine(Options.ForegroundColor, Name + "=" + v);
            }
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
                PostMapper = v => Selections.First(Selection => Selections.Comparer.Equals(v, Selection)),
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
                    var Selected = new HashSet<String>(Parts, Selections.Comparer);
                    if (Validator != null) { return Validator(Selections.Where(Selection => Selected.Contains(Selection)).ToList()); }
                    return new KeyValuePair<bool, String>(true, "");
                },
                PostMapper = v =>
                {
                    var Selected = new HashSet<String>(v.Split(' ').Where(Part => Part != ""), Selections.Comparer);
                    var Result = Selections.Where(Selection => Selected.Contains(Selection)).ToList();
                    return String.Join(" ", Result);
                },
                DefaultValue = DefaultSelections != null ? String.Join(" ", Selections.Intersect(DefaultSelections)) : "",
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
                var FileSelections = new List<string> { };
                if (EnableFile)
                {
                    try
                    {
                        FileSelections = FileSystemUtils.GetFiles(ConfirmedParts, "*", SearchOption.TopDirectoryOnly).Select(f => f.FileName).ToList();
                    }
                    catch (UnauthorizedAccessException)
                    {
                    }
                }
                var DirectorySelections = new List<string> { };
                if (EnableDirectory)
                {
                    try
                    {
                        DirectorySelections = FileSystemUtils.GetDirectories(ConfirmedParts, "*", SearchOption.TopDirectoryOnly).Select(d => d.FileName).ToList();
                    }
                    catch (UnauthorizedAccessException)
                    {
                    }
                }
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

        private static Object TerminalLock = new Object();
        private static ITerminal TerminalValue;
        public static ITerminal Terminal
        {
            get
            {
                lock (TerminalLock)
                {
                    if (TerminalValue == null)
                    {
                        if (OperatingSystem == OperatingSystemType.Windows)
                        {
                            TerminalValue = new WindowsTerminal();
                        }
                        else
                        {
                            TerminalValue = new EscapeTerminal();
                        }
                    }
                    return TerminalValue;
                }
            }
        }
        public static void UseSimpleTerminal()
        {
            lock (TerminalLock)
            {
                TerminalValue = new SimpleTerminal();
            }
        }
    }
}
