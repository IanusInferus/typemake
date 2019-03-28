using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace TypeMake
{
    public partial class Program
    {
        public static int Main(String[] args)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                return MainInner(args);
            }
            else
            {
                try
                {
                    return MainInner(args);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    return 1;
                }
            }
        }

        public static int MainInner(String[] args)
        {
            var argv = args.Where(arg => !arg.StartsWith("--") && !arg.Contains("=")).ToArray();
            var options = args.Where(arg => arg.StartsWith("--")).Select(arg => arg.Substring(2).Split(new Char[] { ':' }, 2)).GroupBy(p => p[0]).ToDictionary(g => g.Key, g => g.Last().Skip(1).SingleOrDefault(), StringComparer.OrdinalIgnoreCase);
            var optionLists = args.Where(arg => arg.StartsWith("--")).Select(arg => arg.Substring(2).Split(new Char[] { ':' }, 2)).GroupBy(p => p[0]).ToDictionary(g => g.Key, g => g.Select(Value => Value.Skip(1).SingleOrDefault()).ToList(), StringComparer.OrdinalIgnoreCase);

            var Help = options.ContainsKey("help");
            if (Help)
            {
                DisplayInfo();
                return 0;
            }
            if (argv.Length == 1)
            {
                var RetypemakeScriptPath = argv[0].AsPath();
                String[] Lines;
                Regex rVariable;
                if (RetypemakeScriptPath.Extension.Equals(".cmd", StringComparison.OrdinalIgnoreCase))
                {
                    Lines = File.ReadAllLines(RetypemakeScriptPath, System.Text.Encoding.Default);
                    rVariable = new Regex(@"^set\s+(""(?<Key>[^=]+)=(?<Value>.*)""|(?<Key>[^=]+)=(?<Value>.*))\s*$");
                }
                else if (RetypemakeScriptPath.Extension == ".sh")
                {
                    Lines = File.ReadAllLines(RetypemakeScriptPath, new System.Text.UTF8Encoding(false));
                    rVariable = new Regex(@"^export\s+(?<Key>[^=]+)=('(?<Value>.*)'|(?<Value>.*))\s*$");
                }
                else
                {
                    throw new InvalidOperationException("InvalidRetypemakeScript");
                }
                foreach (var Line in Lines)
                {
                    var Match = rVariable.Match(Line);
                    if (Match.Success)
                    {
                        var Key = Match.Result("${Key}");
                        var Value = Match.Result("${Value}");
                        if (Key == "BuildDirectory")
                        {
                            Environment.SetEnvironmentVariable(Key, RetypemakeScriptPath.FullPath.Parent);
                        }
                        else
                        {
                            Environment.SetEnvironmentVariable(Key, Value);
                        }
                    }
                }
            }
            else if (argv.Length != 0)
            {
                DisplayInfo();
                return 1;
            }

            foreach (var p in args.Where(arg => arg.Contains("=")).Select(arg => arg.Split('=')))
            {
                Environment.SetEnvironmentVariable(p[0], p[1]);
            }

            var Quiet = options.ContainsKey("quiet");
            var Memory = new Shell.EnvironmentVariableMemory();
            var v = VariableCollection.Execute(Memory, Quiet);

            GenerateRetypemakeScript(v.HostOperatingSystem, v.SourceDirectory, v.BuildDirectory, Memory, v.OverwriteRetypemakeScript);
            var r = v.m.Execute(v.SelectedProjects);

            if (v.TargetOperatingSystem == Cpp.OperatingSystemType.Windows)
            {
                GenerateBuildScriptWindows(v.BuildDirectory, r.SolutionName, Cpp.ArchitectureType.x86, Cpp.ConfigurationType.Debug, v.VSDir, v.ForceRegenerate);
                GenerateBuildScriptWindows(v.BuildDirectory, r.SolutionName, Cpp.ArchitectureType.x86, Cpp.ConfigurationType.Release, v.VSDir, v.ForceRegenerate);
                GenerateBuildScriptWindows(v.BuildDirectory, r.SolutionName, Cpp.ArchitectureType.x86_64, Cpp.ConfigurationType.Debug, v.VSDir, v.ForceRegenerate);
                GenerateBuildScriptWindows(v.BuildDirectory, r.SolutionName, Cpp.ArchitectureType.x86_64, Cpp.ConfigurationType.Release, v.VSDir, v.ForceRegenerate);
                GenerateBuildScriptWindows(v.BuildDirectory, r.SolutionName, Cpp.ArchitectureType.armeabi_v7a, Cpp.ConfigurationType.Debug, v.VSDir, v.ForceRegenerate);
                GenerateBuildScriptWindows(v.BuildDirectory, r.SolutionName, Cpp.ArchitectureType.armeabi_v7a, Cpp.ConfigurationType.Release, v.VSDir, v.ForceRegenerate);
                GenerateBuildScriptWindows(v.BuildDirectory, r.SolutionName, Cpp.ArchitectureType.arm64_v8a, Cpp.ConfigurationType.Debug, v.VSDir, v.ForceRegenerate);
                GenerateBuildScriptWindows(v.BuildDirectory, r.SolutionName, Cpp.ArchitectureType.arm64_v8a, Cpp.ConfigurationType.Release, v.VSDir, v.ForceRegenerate);
                if (v.BuildAfterGenerate)
                {
                    if (v.HostOperatingSystem == Cpp.OperatingSystemType.Windows)
                    {
                        using (var d = Shell.PushDirectory(v.BuildDirectory))
                        {
                            MergeExitCode(Shell.Execute($"build_{v.TargetArchitecture}_{v.Configuration}.cmd"));
                        }
                    }
                    else
                    {
                        WriteLineError("Cross compiling to Windows is not supported.");
                    }
                }
            }
            else if (v.TargetOperatingSystem == Cpp.OperatingSystemType.Linux)
            {
                GenerateBuildScriptLinux(v.Toolchain, v.HostOperatingSystem, v.BuildDirectory, v.Configuration.Value, v.CMake, v.Make, v.Ninja, v.ForceRegenerate);
                if (v.BuildAfterGenerate)
                {
                    using (var d = Shell.PushDirectory(v.BuildDirectory))
                    {
                        if (v.HostOperatingSystem == Cpp.OperatingSystemType.Windows)
                        {
                            MergeExitCode(Shell.Execute(@".\build.cmd"));
                        }
                        else if (v.HostOperatingSystem == Cpp.OperatingSystemType.Linux)
                        {
                            MergeExitCode(Shell.Execute("./build.sh"));
                        }
                        else
                        {
                            WriteLineError("Cross compiling to Linux is not supported.");
                        }
                    }
                }
            }
            else if (v.TargetOperatingSystem == Cpp.OperatingSystemType.Mac)
            {
                GenerateBuildScriptXCode(v.HostOperatingSystem, v.BuildDirectory, r, v.ForceRegenerate);
                if (v.BuildAfterGenerate)
                {
                    using (var d = Shell.PushDirectory(v.BuildDirectory))
                    {
                        if (v.HostOperatingSystem == Cpp.OperatingSystemType.Mac)
                        {
                            MergeExitCode(Shell.Execute("./build.sh"));
                        }
                        else
                        {
                            WriteLineError("Cross compiling to Mac is not supported.");
                        }
                    }
                }
            }
            else if (v.TargetOperatingSystem == Cpp.OperatingSystemType.iOS)
            {
                GenerateBuildScriptXCode(v.HostOperatingSystem, v.BuildDirectory, r, v.ForceRegenerate);
                if (v.BuildAfterGenerate)
                {
                    using (var d = Shell.PushDirectory(v.BuildDirectory))
                    {
                        if (v.HostOperatingSystem == Cpp.OperatingSystemType.Mac)
                        {
                            MergeExitCode(Shell.Execute("./build.sh"));
                        }
                        else
                        {
                            WriteLineError("Cross compiling to iOS is not supported.");
                        }
                    }
                }
            }
            else if (v.TargetOperatingSystem == Cpp.OperatingSystemType.Android)
            {
                TextFile.WriteToFile(v.BuildDirectory / "gradle/local.properties", $"sdk.dir={v.AndroidSdk.ToString(PathStringStyle.Unix)}", new System.Text.UTF8Encoding(false), !v.ForceRegenerate);
                GenerateBuildScriptAndroid(v.Toolchain, v.HostOperatingSystem, v.BuildDirectory, v.TargetArchitecture.Value, v.Configuration.Value, v.AndroidNdk, v.CMake, v.Make, v.Ninja, v.ForceRegenerate);
                if (v.BuildAfterGenerate)
                {
                    using (var d = Shell.PushDirectory(v.BuildDirectory))
                    {
                        if (v.HostOperatingSystem == Cpp.OperatingSystemType.Windows)
                        {
                            MergeExitCode(Shell.Execute(@".\build.cmd"));
                        }
                        else if ((v.HostOperatingSystem == Cpp.OperatingSystemType.Linux) || (v.HostOperatingSystem == Cpp.OperatingSystemType.Mac))
                        {
                            MergeExitCode(Shell.Execute("./build.sh"));
                        }
                        else
                        {
                            WriteLineError("Cross compiling to Android is not supported.");
                        }
                    }
                }
            }
            else
            {
                DisplayInfo();
                return 1;
            }
            Console.WriteLine("TypeMake successful.");
            return ExitCode;
        }

        public static int ExitCode = 0;
        public static void MergeExitCode(int Code)
        {
            if (Code != 0)
            {
                ExitCode = Code;
            }
        }
        public static void WriteLineError(String Line)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(Line);
            Console.ResetColor();
        }

        public static void DisplayInfo()
        {
            Console.WriteLine(@"TypeMake");
            Console.WriteLine(@"Usage:");
            Console.WriteLine(@"TypeMake [<RetypemakeScript>] <Variable>* [--quiet] [--help]");
            Console.WriteLine(@"RetypemakeScript batch or bash file to get environment variables for diagnostics");
            Console.WriteLine(@"Variable <Key>=<Value> additional environment variables that only take effect in the call");
            Console.WriteLine(@"--quiet no interactive variable input, all variables must be input from environment variables");
        }
    }
}
