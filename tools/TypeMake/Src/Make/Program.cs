﻿using System;
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
                if (RetypemakeScriptPath.Extension.ToLowerInvariant() == "cmd")
                {
                    Lines = File.ReadAllLines(RetypemakeScriptPath, System.Text.Encoding.Default);
                    rVariable = new Regex(@"^set\s+(""(?<Key>[^=]+)=(?<Value>.*)""|(?<Key>[^=]+)=(?<Value>.*))\s*$");
                }
                else if (RetypemakeScriptPath.Extension.ToLowerInvariant() == "sh")
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
            Generate(Quiet);
            Console.WriteLine("TypeMake successful.");
            return 0;
        }

        private static void Generate(bool Quiet)
        {
            var Memory = new Shell.EnvironmentVariableMemory();
            var VariablesAndVariableItems = VariableCollection.GetVariableItems();
            var vc = new ConsoleVariableCollector(Memory, Quiet, VariablesAndVariableItems.Value);
            vc.Execute();
            var v = VariablesAndVariableItems.Key;

            BuildScript.GenerateRetypemakeScript(v.HostOperatingSystem, v.SourceDirectory, v.BuildDirectory, Memory, v.OverwriteRetypemakeScript);
            var r = v.m.Execute(v.SelectedProjects);

            if (v.TargetOperatingSystem == Cpp.OperatingSystemType.Windows)
            {
                BuildScript.GenerateBuildScriptWindows(v.BuildDirectory, r.SolutionName, Cpp.ArchitectureType.x86, Cpp.ConfigurationType.Debug, v.VSDir, v.VSVersion, v.ForceRegenerate);
                BuildScript.GenerateBuildScriptWindows(v.BuildDirectory, r.SolutionName, Cpp.ArchitectureType.x86, Cpp.ConfigurationType.Release, v.VSDir, v.VSVersion, v.ForceRegenerate);
                BuildScript.GenerateBuildScriptWindows(v.BuildDirectory, r.SolutionName, Cpp.ArchitectureType.x64, Cpp.ConfigurationType.Debug, v.VSDir, v.VSVersion, v.ForceRegenerate);
                BuildScript.GenerateBuildScriptWindows(v.BuildDirectory, r.SolutionName, Cpp.ArchitectureType.x64, Cpp.ConfigurationType.Release, v.VSDir, v.VSVersion, v.ForceRegenerate);
                BuildScript.GenerateBuildScriptWindows(v.BuildDirectory, r.SolutionName, Cpp.ArchitectureType.armv7a, Cpp.ConfigurationType.Debug, v.VSDir, v.VSVersion, v.ForceRegenerate);
                BuildScript.GenerateBuildScriptWindows(v.BuildDirectory, r.SolutionName, Cpp.ArchitectureType.armv7a, Cpp.ConfigurationType.Release, v.VSDir, v.VSVersion, v.ForceRegenerate);
                BuildScript.GenerateBuildScriptWindows(v.BuildDirectory, r.SolutionName, Cpp.ArchitectureType.arm64, Cpp.ConfigurationType.Debug, v.VSDir, v.VSVersion, v.ForceRegenerate);
                BuildScript.GenerateBuildScriptWindows(v.BuildDirectory, r.SolutionName, Cpp.ArchitectureType.arm64, Cpp.ConfigurationType.Release, v.VSDir, v.VSVersion, v.ForceRegenerate);
                if (v.BuildNow)
                {
                    if (v.HostOperatingSystem == Cpp.OperatingSystemType.Windows)
                    {
                        var ArchitectureName = "";
                        if (v.TargetArchitecture == Cpp.ArchitectureType.x86)
                        {
                            ArchitectureName = "x86";
                        }
                        else if (v.TargetArchitecture == Cpp.ArchitectureType.x64)
                        {
                            ArchitectureName = "x64";
                        }
                        else if (v.TargetArchitecture == Cpp.ArchitectureType.armv7a)
                        {
                            ArchitectureName = "ARM32";
                        }
                        else if (v.TargetArchitecture == Cpp.ArchitectureType.arm64)
                        {
                            ArchitectureName = "ARM64";
                        }
                        using (var d = Shell.PushDirectory(v.BuildDirectory))
                        {
                            if (Shell.Execute($"build_{ArchitectureName}_{v.Configuration}.cmd") != 0)
                            {
                                throw new InvalidOperationException("ErrorInExecution: " + $"build_{ArchitectureName}_{v.Configuration}.cmd");
                            }
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
                BuildScript.GenerateBuildScriptLinux(v.TargetOperatingSystemDistribution, v.Toolchain, v.HostOperatingSystem, v.BuildDirectory, v.Configuration.Value, v.CMake, v.Make, v.Ninja, v.ForceRegenerate);
                if (v.BuildNow)
                {
                    using (var d = Shell.PushDirectory(v.BuildDirectory))
                    {
                        if (v.HostOperatingSystem == Cpp.OperatingSystemType.Windows)
                        {
                            if (Shell.Execute(@".\build.cmd") != 0)
                            {
                                throw new InvalidOperationException("ErrorInExecution: " + @".\build.cmd");
                            }
                        }
                        else if (v.HostOperatingSystem == Cpp.OperatingSystemType.Linux)
                        {
                            if (Shell.Execute("./build.sh") != 0)
                            {
                                throw new InvalidOperationException("ErrorInExecution: ./build.sh");
                            }
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
                BuildScript.GenerateBuildScriptXCode(v.HostOperatingSystem, v.BuildDirectory, r, v.ForceRegenerate);
                if (v.BuildNow)
                {
                    using (var d = Shell.PushDirectory(v.BuildDirectory))
                    {
                        if (v.HostOperatingSystem == Cpp.OperatingSystemType.Mac)
                        {
                            if (Shell.Execute("./build.sh") != 0)
                            {
                                throw new InvalidOperationException("ErrorInExecution: ./build.sh");
                            }
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
                BuildScript.GenerateBuildScriptXCode(v.HostOperatingSystem, v.BuildDirectory, r, v.ForceRegenerate);
                if (v.BuildNow)
                {
                    using (var d = Shell.PushDirectory(v.BuildDirectory))
                    {
                        if (v.HostOperatingSystem == Cpp.OperatingSystemType.Mac)
                        {
                            if (Shell.Execute("./build.sh") != 0)
                            {
                                throw new InvalidOperationException("ErrorInExecution: ./build.sh");
                            }
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
                if (v.Toolchain != Cpp.ToolchainType.Ninja)
                {
                    TextFile.WriteToFile(v.BuildDirectory / "gradle/local.properties", $"sdk.dir={v.AndroidSdk.ToString(PathStringStyle.Unix)}", new System.Text.UTF8Encoding(false), !v.ForceRegenerate);
                }
                BuildScript.GenerateBuildScriptAndroid(v.SelectedProjects.Values.Where(p => (p.Definition.TargetType == Cpp.TargetType.GradleApplication) || (p.Definition.TargetType == Cpp.TargetType.GradleLibrary)).Select(p => p.Reference).ToList(), v.Toolchain, v.HostOperatingSystem, v.BuildDirectory, v.TargetArchitecture.Value, v.Configuration.Value, v.AndroidNdk, v.CMake, v.Make, v.Ninja, 17, v.ForceRegenerate);
                if (v.BuildNow)
                {
                    using (var d = Shell.PushDirectory(v.BuildDirectory))
                    {
                        if (v.HostOperatingSystem == Cpp.OperatingSystemType.Windows)
                        {
                            if (Shell.Execute(@".\build.cmd") != 0)
                            {
                                throw new InvalidOperationException("ErrorInExecution: " + @".\build.cmd");
                            }
                        }
                        else if ((v.HostOperatingSystem == Cpp.OperatingSystemType.Linux) || (v.HostOperatingSystem == Cpp.OperatingSystemType.Mac))
                        {
                            if (Shell.Execute("./build.sh") != 0)
                            {
                                throw new InvalidOperationException("ErrorInExecution: ./build.sh");
                            }
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
                throw new InvalidOperationException();
            }
        }

        public static void WriteLineError(String Line)
        {
            Shell.SetForegroundColor(ConsoleColor.Red);
            Console.Error.WriteLine(Line);
            Shell.ResetColor();
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
