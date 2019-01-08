using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

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
            var psi = CreateExecuteStartInfo(ProgramPath, Arguments);
            var CommandLine = Arguments.Length == 0 ? EscapeArgumentForShell(ProgramPath, OperatingSystem) : EscapeArgumentForShell(ProgramPath, OperatingSystem) + " " + Arguments;
            Console.WriteLine(CommandLine);
            var p = Process.Start(psi);
            p.WaitForExit();
            return p.ExitCode;
        }
        public static ProcessStartInfo CreateExecuteStartInfo(String ProgramPath, params String[] Arguments)
        {
            return CreateExecuteLineStartInfo(ProgramPath, String.Join(" ", Arguments.Select(arg => EscapeArgument(arg))));
        }
        public static ProcessStartInfo CreateExecuteLineStartInfo(String ProgramPath, String Arguments)
        {
            if (OperatingSystem == BuildingOperatingSystemType.Windows)
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
            return EscapeArgument(Argument, OperatingSystem);
        }
        private static Regex rBackslashBeforeDoubleQuotes = new Regex(@"\\+((?="")|$)", RegexOptions.ExplicitCapture);
        private static Regex rComplexChars = new Regex(@"[\s!""#$%&'()*+,/;<=>?@\[\\\]^`{|}~]", RegexOptions.ExplicitCapture);
        public static String EscapeArgument(String Argument, BuildingOperatingSystemType OperatingSystem)
        {
            //\0 \r \n can not be escaped
            if (Argument.Any(c => c == '\0' || c == '\r' || c == '\n')) { throw new ArgumentException("InvalidChar"); }
            if (OperatingSystem == BuildingOperatingSystemType.Windows)
            {
                //https://docs.microsoft.com/en-us/cpp/cpp/parsing-cpp-command-line-arguments?view=vs-2017
                //http://csharptest.net/529/how-to-correctly-escape-command-line-arguments-in-c/index.html
                //backslashes before double quotes must be doubled
                return rComplexChars.IsMatch(Argument) ? "\"" + rBackslashBeforeDoubleQuotes.Replace(Argument, s => s.Value + s.Value).Replace("\"", "\\\"") + "\"" : Argument;
            }
            else
            {
                //in mono it was originally implemented using g_shell_parse_argv
                //https://bugzilla.xamarin.com/show_bug.cgi?id=19296
                //https://developer.gnome.org/glib/stable/glib-Shell-related-Utilities.html
                //but upon testing it is found that backslash need to be double in single quotes
                return rComplexChars.IsMatch(Argument) ? "'" + Argument.Replace("\\", "\\\\").Replace("'", "'\\''") + "'" : Argument;
            }
        }
        private static Regex rCmdComplexChars = new Regex(@"[%^&<>|]", RegexOptions.ExplicitCapture);
        public static String EscapeArgumentForShell(String Argument, BuildingOperatingSystemType OperatingSystem)
        {
            //\0 \r \n can not be escaped
            if (Argument.Any(c => c == '\0' || c == '\r' || c == '\n')) { throw new ArgumentException("InvalidChar"); }
            if (OperatingSystem == BuildingOperatingSystemType.Windows)
            {
                //CMD style(without EnableDelayedExpansion)
                return rCmdComplexChars.Replace(EscapeArgument(Argument, OperatingSystem), s => "^" + s.Value);
            }
            else
            {
                //bash style
                return rComplexChars.IsMatch(Argument) ? "'" + Argument.Replace("'", "'\\''") + "'" : Argument;
            }
        }

        public class EnvironmentVariableMemory
        {
            public Dictionary<String, String> Variables = new Dictionary<String, String>();
            public Dictionary<String, List<String>> VariableSelections = new Dictionary<String, List<String>>();
        }
        public class EnvironmentVariableReadOptions
        {
            public bool Quiet { get; set; } = false;
            public Func<String, int, bool, bool, String> Suggester { get; set; } = null;
            public Func<String, bool> Validator { get; set; } = null;
            public Func<String, String> PostMapper { get; set; } = null;
            public String DefaultValue { get; set; } = null;
            public String InputDisplay { get; set; } = null;
            public bool IsPassword { get; set; } = false;
            public EnvironmentVariableReadOptions()
            {
                Suggester = (v, ConfirmedLength, Cycle, CyclePrevious) => String.IsNullOrEmpty(DefaultValue) ? v : GetCaseInsensitiveDefaultValueSuggester(DefaultValue)(v, ConfirmedLength, Cycle, CyclePrevious);
            }
        }
        public static String RequireEnvironmentVariable(EnvironmentVariableMemory Memory, String Name)
        {
            return RequireEnvironmentVariable(Memory, Name, new EnvironmentVariableReadOptions());
        }
        public static String RequireEnvironmentVariable(EnvironmentVariableMemory Memory, String Name, EnvironmentVariableReadOptions Options)
        {
            var Top = Console.CursorTop;
            var Left = Console.CursorLeft;
            var cps = GetConsolePositionState();
            var d = Options.InputDisplay ?? (!String.IsNullOrEmpty(Options.DefaultValue) ? "[" + Options.DefaultValue + "]" : "");
            var v = Environment.GetEnvironmentVariable(Name);
            if (v == null)
            {
                if (Options.Quiet) { throw new InvalidOperationException("Variable '" + Name + "' not exist."); }
                Console.Write("'" + Name + "' not exist, input" + (d == "" ? "" : " " + d) + ": ");
                v = Options.IsPassword ? ReadLinePassword() : ReadLineWithSuggestion(Options.Suggester);
                if (v == "")
                {
                    v = Options.DefaultValue ?? "";
                }
                if (OperatingSystem == BuildingOperatingSystemType.Windows)
                {
                    var cpsNew = GetConsolePositionState();
                    SetConsolePositionState(cps);
                    BackspaceCursorToPosition(Top, Left);
                    SetConsolePositionState(cpsNew);
                }
            }
            while ((Options.Validator != null) && !Options.Validator(v))
            {
                if (Options.Quiet) { throw new InvalidOperationException("Variable '" + Name + "' invalid."); }
                Console.Write("'" + Name + "' invalid, input" + (d == "" ? "" : " " + d) + ": ");
                v = Options.IsPassword ? ReadLinePassword() : ReadLineWithSuggestion(Options.Suggester);
                if (v == "")
                {
                    v = Options.DefaultValue ?? "";
                }
                if (OperatingSystem == BuildingOperatingSystemType.Windows)
                {
                    var cpsNew = GetConsolePositionState();
                    SetConsolePositionState(cps);
                    BackspaceCursorToPosition(Top, Left);
                    SetConsolePositionState(cpsNew);
                }
            }
            if (Options.PostMapper != null)
            {
                v = Options.PostMapper(v);
            }
            if (Options.IsPassword)
            {
                Console.WriteLine(Name + "=[***]");
            }
            else
            {
                Console.WriteLine(Name + "=" + v);
            }
            if (Memory.Variables.ContainsKey(Name))
            {
                Memory.Variables[Name] = v;
            }
            else
            {
                Memory.Variables.Add(Name, v);
            }
            return v;
        }
        public static T RequireEnvironmentVariableEnum<T>(EnvironmentVariableMemory Memory, String Name, bool Quiet, HashSet<T> Selections, T DefaultValue = default(T)) where T : struct
        {
            var InputDisplay = String.Join("|", Selections.Select(e => e.Equals(DefaultValue) ? "[" + e.ToString() + "]" : e.ToString()));
            T Output = default(T);
            var s = RequireEnvironmentVariable(Memory, Name, new EnvironmentVariableReadOptions
            {
                Quiet = Quiet,
                Suggester = GetCaseInsensitiveSelectionSuggester(Selections.Select(e => e.ToString())),
                Validator = v =>
                {
                    T o;
                    var b = Enum.TryParse<T>(v, true, out o);
                    if (!Selections.Contains(o)) { return false; }
                    Output = o;
                    return b;
                },
                PostMapper = v => Output.ToString(),
                DefaultValue = DefaultValue.ToString(),
                InputDisplay = InputDisplay
            });
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
        public static T RequireEnvironmentVariableEnum<T>(EnvironmentVariableMemory Memory, String Name, bool Quiet, T DefaultValue = default(T)) where T : struct
        {
            return RequireEnvironmentVariableEnum<T>(Memory, Name, Quiet, new HashSet<T>(Enum.GetValues(typeof(T)).Cast<T>()), DefaultValue);
        }
        public static String RequireEnvironmentVariableSelection(EnvironmentVariableMemory Memory, String Name, bool Quiet, HashSet<String> Selections, String DefaultValue = "")
        {
            var InputDisplay = String.Join("|", Selections.Select(c => c.Equals(DefaultValue) ? "[" + c.ToString() + "]" : c.ToString()));
            var s = RequireEnvironmentVariable(Memory, Name, new EnvironmentVariableReadOptions
            {
                Quiet = Quiet,
                Suggester = GetCaseInsensitiveSelectionSuggester(Selections),
                Validator = v => Selections.Contains(v),
                DefaultValue = DefaultValue.ToString(),
                InputDisplay = InputDisplay
            });
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
        public static bool RequireEnvironmentVariableBoolean(EnvironmentVariableMemory Memory, String Name, bool Quiet, bool DefaultValue = false)
        {
            var Selections = new List<bool> { false, true };
            var InputDisplay = String.Join("|", Selections.Select(c => c.Equals(DefaultValue) ? "[" + c.ToString() + "]" : c.ToString()));
            bool Output = false;
            var s = RequireEnvironmentVariable(Memory, Name, new EnvironmentVariableReadOptions
            {
                Quiet = Quiet,
                Suggester = GetCaseInsensitiveSelectionSuggester(new List<String> { "False", "True" }),
                Validator = v =>
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
                },
                PostMapper = v => Output.ToString(),
                DefaultValue = DefaultValue.ToString(),
                InputDisplay = InputDisplay
            });
            return Output;
        }
        public static String RequireEnvironmentVariableFilePath(EnvironmentVariableMemory Memory, String Name, bool Quiet, String DefaultValue = null, Func<String, bool> Validator = null)
        {
            var s = RequireEnvironmentVariable(Memory, Name, new EnvironmentVariableReadOptions
            {
                Quiet = Quiet,
                Suggester = GetPathSuggester(true, true, DefaultValue),
                Validator = Validator ?? (p => File.Exists(p)),
                PostMapper = p => Path.GetFullPath(p),
                DefaultValue = DefaultValue
            });
            return s;
        }
        public static String RequireEnvironmentVariableDirectoryPath(EnvironmentVariableMemory Memory, String Name, bool Quiet, String DefaultValue = null, Func<String, bool> Validator = null)
        {
            var s = RequireEnvironmentVariable(Memory, Name, new EnvironmentVariableReadOptions
            {
                Quiet = Quiet,
                Suggester = GetPathSuggester(false, true, DefaultValue),
                Validator = Validator ?? (p => Directory.Exists(p)),
                PostMapper = p => Path.GetFullPath(p),
                DefaultValue = DefaultValue
            });
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
                return v;
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
        private static Func<String, int, bool, bool, String> GetPathSuggester(bool EnableFile, bool EnableDirectory, String DefaultValue)
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
                String Parent;
                try
                {
                    Parent = Path.GetDirectoryName(vConfirmed);
                }
                catch (ArgumentException)
                {
                    return vConfirmed;
                }
                if ((Parent != "") && !Directory.Exists(Parent)) { return vConfirmed; }
                var FileSelections = EnableFile ? Directory.EnumerateFiles(Parent == "" ? "." : Parent, "*", SearchOption.TopDirectoryOnly).Select(f => Path.GetFileName(f)).ToList() : new List<string> { };
                var DirectorySelections = EnableDirectory ? Directory.EnumerateDirectories(Parent == "" ? "." : Parent, "*", SearchOption.TopDirectoryOnly).Select(d => Path.GetFileName(d)).ToList() : new List<string> { };
                var Selections = FileSelections.Concat(DirectorySelections).Select(s => Path.Combine(Parent, s)).ToList();
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
        public static String ReadLinePassword()
        {
            var l = new LinkedList<Char>();
            while (true)
            {
                var ki = Console.ReadKey(true);
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
        public static String ReadLineWithSuggestion(Func<String, int, bool, bool, String> Suggester)
        {
            if (OperatingSystem == BuildingOperatingSystemType.Windows)
            {
                var Confirmed = new LinkedList<KeyValuePair<Char, KeyValuePair<int, int>>>();
                var Suggested = new LinkedList<KeyValuePair<Char, KeyValuePair<int, int>>>();
                LinkedListNode<KeyValuePair<Char, KeyValuePair<int, int>>> CurrentCharNode = null;
                int ConfirmedLastTop = Console.CursorTop;
                int ConfirmedLastLeft = Console.CursorLeft;
                int SuggestedLastTop = Console.CursorTop;
                int SuggestedLastLeft = Console.CursorLeft;
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
                    Console.BackgroundColor = ConsoleColor.Blue;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    while (Next != null)
                    {
                        Next.Value = new KeyValuePair<Char, KeyValuePair<int, int>>(Next.Value.Key, new KeyValuePair<int, int>(Console.CursorTop, Console.CursorLeft));
                        Console.Write(Next.Value.Key);
                        Next = Next.Next;
                    }
                    Console.ResetColor();
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
                        if (CurrentCharNode != null)
                        {
                            if (CurrentCharNode.Previous != null)
                            {
                                MoveCursorToPosition(CurrentCharNode.Previous.Value.Value);
                                Confirmed.Remove(CurrentCharNode.Previous);
                                RefreshSuggestion();
                                RefreshCharsAfterCursor();
                            }
                        }
                        else
                        {
                            if (Confirmed.Last != null)
                            {
                                MoveCursorToPosition(Confirmed.Last.Value.Value);
                                Confirmed.RemoveLast();
                                RefreshSuggestion();
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
                    Console.Write("\b");
                    Console.Write(" ");
                    Console.Write("\b");
                }
            }
            MoveCursorToPosition(Top, Left);
        }
        private static void MoveCursorToPosition(int Top, int Left)
        {
            Console.SetCursorPosition(Left, Top);
        }
        private static void MoveCursorToPosition(KeyValuePair<int, int> Pair)
        {
            Console.SetCursorPosition(Pair.Value, Pair.Key);
        }
        private class ConsolePositionState
        {
            public int BufferWidth = Console.BufferWidth;
            public int BufferHeight = Console.BufferHeight;
            public int WindowWidth = Console.WindowWidth;
            public int WindowHeight = Console.WindowHeight;
            public int WindowTop = Console.WindowTop;
            public int WindowLeft = Console.WindowLeft;
        }
        private static ConsolePositionState GetConsolePositionState()
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
        private static void SetConsolePositionState(ConsolePositionState s)
        {
            Console.SetWindowSize(s.WindowWidth, s.WindowHeight);
            Console.SetWindowPosition(s.WindowLeft, s.WindowTop);
            Console.SetBufferSize(s.BufferWidth, s.BufferHeight);
        }
    }
}
