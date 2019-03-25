using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace TypeMake
{
    public class Program
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

        public static int ExitCode = 0;
        public static void MergeExitCode(int Code)
        {
            if (Code != 0)
            {
                ExitCode = Code;
            }
        }
        public static int MainInner(String[] args)
        {
            var BuildingOperatingSystem = Cpp.OperatingSystemType.Windows;
            if (Shell.OperatingSystem == Shell.OperatingSystemType.Windows)
            {
                BuildingOperatingSystem = Cpp.OperatingSystemType.Windows;
            }
            else if (Shell.OperatingSystem == Shell.OperatingSystemType.Linux)
            {
                BuildingOperatingSystem = Cpp.OperatingSystemType.Linux;
            }
            else if (Shell.OperatingSystem == Shell.OperatingSystemType.Mac)
            {
                BuildingOperatingSystem = Cpp.OperatingSystemType.Mac;
            }
            else
            {
                throw new InvalidOperationException("UnknownBuildingOperatingSystem");
            }
            var BuildingOperatingSystemArchitecture = Cpp.ArchitectureType.x86_64;
            if (Shell.OperatingSystemArchitecture == Shell.OperatingSystemArchitectureType.x86_64)
            {
                BuildingOperatingSystemArchitecture = Cpp.ArchitectureType.x86_64;
            }
            else if (Shell.OperatingSystemArchitecture == Shell.OperatingSystemArchitectureType.x86)
            {
                BuildingOperatingSystemArchitecture = Cpp.ArchitectureType.x86;
            }
            else
            {
                throw new InvalidOperationException("UnknownBuildingOperatingSystemArchitecture");
            }
            //process architecture is supposed to be the same as the operating system architecture

            var argv = args.Where(arg => !arg.StartsWith("--") && !arg.Contains("=")).ToArray();
            var options = args.Where(arg => arg.StartsWith("--")).Select(arg => arg.Substring(2).Split(new Char[] { ':' }, 2)).GroupBy(p => p[0]).ToDictionary(g => g.Key, g => g.Last().Skip(1).SingleOrDefault(), StringComparer.OrdinalIgnoreCase);
            var optionLists = args.Where(arg => arg.StartsWith("--")).Select(arg => arg.Substring(2).Split(new Char[] { ':' }, 2)).GroupBy(p => p[0]).ToDictionary(g => g.Key, g => g.Select(v => v.Skip(1).SingleOrDefault()).ToList(), StringComparer.OrdinalIgnoreCase);

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
                Regex r;
                if (RetypemakeScriptPath.Extension.Equals(".cmd", StringComparison.OrdinalIgnoreCase))
                {
                    Lines = File.ReadAllLines(RetypemakeScriptPath, System.Text.Encoding.Default);
                    r = new Regex(@"^set\s+(""(?<Key>[^=]+)=(?<Value>.*)""|(?<Key>[^=]+)=(?<Value>.*))\s*$");
                }
                else if (RetypemakeScriptPath.Extension == ".sh")
                {
                    Lines = File.ReadAllLines(RetypemakeScriptPath, new System.Text.UTF8Encoding(false));
                    r = new Regex(@"^export\s+(?<Key>[^=]+)=('(?<Value>.*)'|(?<Value>.*))\s*$");
                }
                else
                {
                    throw new InvalidOperationException("InvalidRetypemakeScript");
                }
                foreach (var Line in Lines)
                {
                    var m = r.Match(Line);
                    if (m.Success)
                    {
                        var Key = m.Result("${Key}");
                        var Value = m.Result("${Value}");
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

            var OverwriteRetypemakeScript = Shell.RequireEnvironmentVariableBoolean(Memory, "OverwriteRetypemakeScript", Quiet, true);
            var ForceRegenerate = Shell.RequireEnvironmentVariableBoolean(Memory, "ForceRegenerate", Quiet, false);
            var EnableNonTargetingOperatingSystemDummy = Shell.RequireEnvironmentVariableBoolean(Memory, "EnableNonTargetingOperatingSystemDummy", Quiet, false);
            var BuildAfterGenerate = Shell.RequireEnvironmentVariableBoolean(Memory, "BuildAfterGenerate", Quiet, true);

            var SourceDirectory = Shell.RequireEnvironmentVariableDirectoryPath(Memory, "SourceDirectory", Quiet);
            var TargetOperatingSystem = Shell.RequireEnvironmentVariableEnum<Cpp.OperatingSystemType>(Memory, "TargetOperatingSystem", Quiet, BuildingOperatingSystem);

            if (TargetOperatingSystem == Cpp.OperatingSystemType.Windows)
            {
                var VSDir = "";
                var TargetArchitecture = Cpp.ArchitectureType.x86_64;
                var Configuration = Cpp.ConfigurationType.Debug;
                if (BuildAfterGenerate && (BuildingOperatingSystem == Cpp.OperatingSystemType.Windows))
                {
                    String DefaultVSDir = "";
                    var ProgramFiles = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
                    if (ProgramFiles != null)
                    {
                        foreach (var d in new String[] { "Enterprise", "Professional", "Community", "BuildTools" })
                        {
                            var p = ProgramFiles.AsPath() / "Microsoft Visual Studio/2017" / d;
                            if (Directory.Exists(p))
                            {
                                DefaultVSDir = p;
                                break;
                            }
                        }
                    }
                    VSDir = Shell.RequireEnvironmentVariableDirectoryPath(Memory, "VSDir", Quiet, DefaultVSDir);
                    TargetArchitecture = Shell.RequireEnvironmentVariableEnum(Memory, "TargetArchitecture", Quiet, new HashSet<Cpp.ArchitectureType> { Cpp.ArchitectureType.x86, Cpp.ArchitectureType.x86_64, Cpp.ArchitectureType.armeabi_v7a, Cpp.ArchitectureType.arm64_v8a }, Cpp.ArchitectureType.x86_64);
                    Configuration = Shell.RequireEnvironmentVariableEnum(Memory, "Configuration", Quiet, Cpp.ConfigurationType.Debug);
                }
                var BuildDirectory = Shell.RequireEnvironmentVariableDirectoryPath(Memory, "BuildDirectory", Quiet, "build/windows".AsPath(), p => !File.Exists(p) ? new KeyValuePair<bool, String>(true, "") : new KeyValuePair<bool, String>(false, "Exist as a file."));
                var m = new Make(Cpp.ToolchainType.Windows_VisualC, Cpp.CompilerType.VisualC, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem, null, null, SourceDirectory, BuildDirectory, null, null, null, null, ForceRegenerate, EnableNonTargetingOperatingSystemDummy);
                var Projects = m.GetAvailableProjects();
                var SelectedProjects = GetSelectedProjects(Memory, Quiet, Projects, m.CheckUnresolvedDependencies);
                GenerateRetypemakeScript(BuildingOperatingSystem, SourceDirectory, BuildDirectory, Memory, OverwriteRetypemakeScript);
                var r = m.Execute(SelectedProjects);
                if (BuildAfterGenerate)
                {
                    if (BuildingOperatingSystem == Cpp.OperatingSystemType.Windows)
                    {
                        GenerateBuildScriptWindows(BuildDirectory, r.SolutionName, Cpp.ArchitectureType.x86, Cpp.ConfigurationType.Debug, VSDir, ForceRegenerate);
                        GenerateBuildScriptWindows(BuildDirectory, r.SolutionName, Cpp.ArchitectureType.x86, Cpp.ConfigurationType.Release, VSDir, ForceRegenerate);
                        GenerateBuildScriptWindows(BuildDirectory, r.SolutionName, Cpp.ArchitectureType.x86_64, Cpp.ConfigurationType.Debug, VSDir, ForceRegenerate);
                        GenerateBuildScriptWindows(BuildDirectory, r.SolutionName, Cpp.ArchitectureType.x86_64, Cpp.ConfigurationType.Release, VSDir, ForceRegenerate);
                        GenerateBuildScriptWindows(BuildDirectory, r.SolutionName, Cpp.ArchitectureType.armeabi_v7a, Cpp.ConfigurationType.Debug, VSDir, ForceRegenerate);
                        GenerateBuildScriptWindows(BuildDirectory, r.SolutionName, Cpp.ArchitectureType.armeabi_v7a, Cpp.ConfigurationType.Release, VSDir, ForceRegenerate);
                        GenerateBuildScriptWindows(BuildDirectory, r.SolutionName, Cpp.ArchitectureType.arm64_v8a, Cpp.ConfigurationType.Debug, VSDir, ForceRegenerate);
                        GenerateBuildScriptWindows(BuildDirectory, r.SolutionName, Cpp.ArchitectureType.arm64_v8a, Cpp.ConfigurationType.Release, VSDir, ForceRegenerate);
                        using (var d = Shell.PushDirectory(BuildDirectory))
                        {
                            MergeExitCode(Shell.Execute($"build_{TargetArchitecture}_{Configuration}.cmd"));
                        }
                    }
                    else
                    {
                        WriteLineError("Cross compiling to Windows is not supported.");
                    }
                }
            }
            else if (TargetOperatingSystem == Cpp.OperatingSystemType.Linux)
            {
                var TargetArchitecture = Cpp.ArchitectureType.x86_64;
                var Toolchain = Shell.RequireEnvironmentVariableEnum(Memory, "Toolchain", Quiet, new HashSet<Cpp.ToolchainType> { Cpp.ToolchainType.Ninja, Cpp.ToolchainType.CMake }, Cpp.ToolchainType.Ninja);
                var Configuration = Shell.RequireEnvironmentVariableEnum(Memory, "Configuration", Quiet, Cpp.ConfigurationType.Debug);
                var CMake = "".AsPath();
                var Make = "".AsPath();
                var Ninja = "".AsPath();
                if (Toolchain == Cpp.ToolchainType.CMake)
                {
                    if (BuildingOperatingSystem == Cpp.OperatingSystemType.Windows)
                    {
                        CMake = Shell.RequireEnvironmentVariable(Memory, "CMake", new Shell.EnvironmentVariableReadOptions { Quiet = Quiet, DefaultValue = "cmake" });
                        Make = Shell.RequireEnvironmentVariable(Memory, "Make", new Shell.EnvironmentVariableReadOptions { Quiet = Quiet, DefaultValue = "make" });
                    }
                    else if (BuildingOperatingSystem == Cpp.OperatingSystemType.Linux)
                    {
                        CMake = Shell.RequireEnvironmentVariableFilePath(Memory, "CMake", Quiet, Shell.TryLocate("cmake") ?? "");
                        Make = Shell.RequireEnvironmentVariableFilePath(Memory, "Make", Quiet, Shell.TryLocate("make") ?? "");
                    }
                }
                else if (Toolchain == Cpp.ToolchainType.Ninja)
                {
                    if (BuildingOperatingSystem == Cpp.OperatingSystemType.Windows)
                    {
                        Ninja = SourceDirectory / "tools/Ninja/ninja-linux/ninja";
                    }
                    else if (BuildingOperatingSystem == Cpp.OperatingSystemType.Linux)
                    {
                        Ninja = SourceDirectory / "tools/Ninja/ninja-linux/ninja";
                    }
                    else if (BuildingOperatingSystem == Cpp.OperatingSystemType.Mac)
                    {
                        Ninja = SourceDirectory / "tools/Ninja/ninja-mac/ninja";
                    }
                    else
                    {
                        WriteLineError("Current building host operating system is not supported.");
                        return 1;
                    }
                }
                var BuildDirectory = Shell.RequireEnvironmentVariableDirectoryPath(Memory, "BuildDirectory", Quiet, $"build/linux_{Toolchain}_{TargetArchitecture}_{Configuration}".AsPath(), p => !File.Exists(p) ? new KeyValuePair<bool, String>(true, "") : new KeyValuePair<bool, String>(false, "Exist as a file."));
                var m = new Make(Toolchain, Cpp.CompilerType.gcc, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem, TargetArchitecture, Configuration, SourceDirectory, BuildDirectory, null, "gcc", "g++", "ar", ForceRegenerate, EnableNonTargetingOperatingSystemDummy);
                var Projects = m.GetAvailableProjects();
                var SelectedProjects = GetSelectedProjects(Memory, Quiet, Projects, m.CheckUnresolvedDependencies);
                GenerateRetypemakeScript(BuildingOperatingSystem, SourceDirectory, BuildDirectory, Memory, OverwriteRetypemakeScript);
                m.Execute(SelectedProjects);
                GenerateBuildScriptLinux(Toolchain, BuildingOperatingSystem, BuildDirectory, Configuration, CMake, Make, Ninja, ForceRegenerate);
                if (BuildAfterGenerate)
                {
                    using (var d = Shell.PushDirectory(BuildDirectory))
                    {
                        if (BuildingOperatingSystem == Cpp.OperatingSystemType.Windows)
                        {
                            MergeExitCode(Shell.Execute(@".\build.cmd"));
                        }
                        else if (BuildingOperatingSystem == Cpp.OperatingSystemType.Linux)
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
            else if (TargetOperatingSystem == Cpp.OperatingSystemType.Mac)
            {
                var BuildDirectory = Shell.RequireEnvironmentVariableDirectoryPath(Memory, "BuildDirectory", Quiet, "build/mac".AsPath(), p => !File.Exists(p) ? new KeyValuePair<bool, String>(true, "") : new KeyValuePair<bool, String>(false, "Exist as a file."));
                var DevelopmentTeam = Shell.RequireEnvironmentVariable(Memory, "DevelopmentTeam", new Shell.EnvironmentVariableReadOptions { Quiet = Quiet, InputDisplay = "(optional, find by searching an existing pbxproj file with DEVELOPMENT_TEAM)" });
                var m = new Make(Cpp.ToolchainType.Mac_XCode, Cpp.CompilerType.clang, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem, null, null, SourceDirectory, BuildDirectory, DevelopmentTeam, null, null, null, ForceRegenerate, EnableNonTargetingOperatingSystemDummy);
                var Projects = m.GetAvailableProjects();
                var SelectedProjects = GetSelectedProjects(Memory, Quiet, Projects, m.CheckUnresolvedDependencies);
                GenerateRetypemakeScript(BuildingOperatingSystem, SourceDirectory, BuildDirectory, Memory, OverwriteRetypemakeScript);
                var r = m.Execute(SelectedProjects);
                GenerateBuildScriptXCode(BuildingOperatingSystem, BuildDirectory, r, ForceRegenerate);
                if (BuildAfterGenerate)
                {
                    using (var d = Shell.PushDirectory(BuildDirectory))
                    {
                        if (BuildingOperatingSystem == Cpp.OperatingSystemType.Mac)
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
            else if (TargetOperatingSystem == Cpp.OperatingSystemType.iOS)
            {
                var BuildDirectory = Shell.RequireEnvironmentVariableDirectoryPath(Memory, "BuildDirectory", Quiet, "build/ios".AsPath(), p => !File.Exists(p) ? new KeyValuePair<bool, String>(true, "") : new KeyValuePair<bool, String>(false, "Exist as a file."));
                var DevelopmentTeam = Shell.RequireEnvironmentVariable(Memory, "DevelopmentTeam", new Shell.EnvironmentVariableReadOptions { Quiet = Quiet, InputDisplay = "(optional, find by searching an existing pbxproj file with DEVELOPMENT_TEAM)" });
                var m = new Make(Cpp.ToolchainType.Mac_XCode, Cpp.CompilerType.clang, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem, null, null, SourceDirectory, BuildDirectory, DevelopmentTeam, null, null, null, ForceRegenerate, EnableNonTargetingOperatingSystemDummy);
                var Projects = m.GetAvailableProjects();
                var SelectedProjects = GetSelectedProjects(Memory, Quiet, Projects, m.CheckUnresolvedDependencies);
                GenerateRetypemakeScript(BuildingOperatingSystem, SourceDirectory, BuildDirectory, Memory, OverwriteRetypemakeScript);
                var r = m.Execute(SelectedProjects);
                GenerateBuildScriptXCode(BuildingOperatingSystem, BuildDirectory, r, ForceRegenerate);
                if (BuildAfterGenerate)
                {
                    using (var d = Shell.PushDirectory(BuildDirectory))
                    {
                        if (BuildingOperatingSystem == Cpp.OperatingSystemType.Mac)
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
            else if (TargetOperatingSystem == Cpp.OperatingSystemType.Android)
            {
                var AndroidSdk = Shell.RequireEnvironmentVariableDirectoryPath(Memory, "AndroidSdk", Quiet, BuildingOperatingSystem == Cpp.OperatingSystemType.Windows ? (Environment.GetEnvironmentVariable("LocalAppData").AsPath() / "Android/sdk").ToString() : "", p => Directory.Exists(p / "platform-tools") ? new KeyValuePair<bool, String>(true, "") : new KeyValuePair<bool, String>(false, "No platform-tools directory inside."));
                var AndroidNdk = Shell.RequireEnvironmentVariableDirectoryPath(Memory, "AndroidNdk", Quiet, AndroidSdk / "ndk-bundle", p => Directory.Exists(p / "build") ? new KeyValuePair<bool, String>(true, "") : new KeyValuePair<bool, String>(false, "No build directory inside."));
                var Toolchain = Shell.RequireEnvironmentVariableEnum(Memory, "Toolchain", Quiet, new HashSet<Cpp.ToolchainType> { Cpp.ToolchainType.Gradle_Ninja, Cpp.ToolchainType.Gradle_CMake }, Cpp.ToolchainType.Gradle_Ninja);
                var TargetArchitecture = Shell.RequireEnvironmentVariableEnum(Memory, "TargetArchitecture", Quiet, Cpp.ArchitectureType.armeabi_v7a);
                var Configuration = Shell.RequireEnvironmentVariableEnum(Memory, "Configuration", Quiet, Cpp.ConfigurationType.Debug);
                var CMake = "".AsPath();
                var Make = "".AsPath();
                var Ninja = "".AsPath();
                var CC = "";
                var CXX = "";
                var AR = "";
                if (Toolchain == Cpp.ToolchainType.Gradle_CMake)
                {
                    CMake = Shell.RequireEnvironmentVariableFilePath(Memory, "CMake", Quiet, Shell.TryLocate("cmake") ?? (BuildingOperatingSystem == Cpp.OperatingSystemType.Windows ? (Environment.GetEnvironmentVariable("ProgramFiles").AsPath() / @"CMake\bin\cmake.exe") : "".AsPath()));
                    if (BuildingOperatingSystemArchitecture == Cpp.ArchitectureType.x86_64)
                    {
                        if (BuildingOperatingSystem == Cpp.OperatingSystemType.Windows)
                        {
                            Make = AndroidNdk / @"prebuilt\windows-x86_64\bin\make.exe";
                        }
                        else if (BuildingOperatingSystem == Cpp.OperatingSystemType.Linux)
                        {
                            Make = AndroidNdk / "prebuilt/linux-x86_64/bin/make";
                        }
                        else if (BuildingOperatingSystem == Cpp.OperatingSystemType.Mac)
                        {
                            Make = AndroidNdk / "prebuilt/darwin-x86_64/bin/make";
                        }
                    }
                    Make = Shell.RequireEnvironmentVariableFilePath(Memory, "Make", Quiet, Make);
                }
                else if (Toolchain == Cpp.ToolchainType.Gradle_Ninja)
                {
                    var Host = "";
                    var ExeSuffix = "";
                    if (BuildingOperatingSystem == Cpp.OperatingSystemType.Windows)
                    {
                        Ninja = SourceDirectory / "tools/Ninja/ninja-win/ninja.exe";
                        Host = "windows-x86_64";
                        ExeSuffix = ".exe";
                    }
                    else if (BuildingOperatingSystem == Cpp.OperatingSystemType.Linux)
                    {
                        Ninja = SourceDirectory / "tools/Ninja/ninja-linux/ninja";
                        Host = "linux-x86_64";
                        ExeSuffix = "";
                    }
                    else if (BuildingOperatingSystem == Cpp.OperatingSystemType.Mac)
                    {
                        Ninja = SourceDirectory / "tools/Ninja/ninja-mac/ninja";
                        Host = "darwin-x86_64";
                        ExeSuffix = "";
                    }
                    else
                    {
                        WriteLineError("Current building host operating system is not supported.");
                        return 1;
                    }
                    var TargetPrefix = "";
                    if (TargetArchitecture == Cpp.ArchitectureType.x86)
                    {
                        TargetPrefix = "i686";
                    }
                    else if (TargetArchitecture == Cpp.ArchitectureType.x86_64)
                    {
                        TargetPrefix = "x86_64";
                    }
                    else if (TargetArchitecture == Cpp.ArchitectureType.armeabi_v7a)
                    {
                        TargetPrefix = "armv7a";
                    }
                    else if (TargetArchitecture == Cpp.ArchitectureType.arm64_v8a)
                    {
                        TargetPrefix = "aarch64";
                    }
                    var ApiLevel = 17;
                    CC = AndroidNdk / $"toolchains/llvm/prebuilt/{Host}/bin/clang{ExeSuffix} --target={TargetPrefix}-linux-androideabi{ApiLevel} -fno-addrsig -stdlib=libc++ -fPIC";
                    CXX = AndroidNdk / $"toolchains/llvm/prebuilt/{Host}/bin/clang++{ExeSuffix} --target={TargetPrefix}-linux-androideabi{ApiLevel} -fno-addrsig -stdlib=libc++ -fPIC";
                    AR = AndroidNdk / $"toolchains/llvm/prebuilt/{Host}/bin/llvm-ar{ExeSuffix}";
                }
                var BuildDirectory = Shell.RequireEnvironmentVariableDirectoryPath(Memory, "BuildDirectory", Quiet, $"build/android_{Toolchain}_{TargetArchitecture}_{Configuration}".AsPath(), p => !File.Exists(p) ? new KeyValuePair<bool, String>(true, "") : new KeyValuePair<bool, String>(false, "Exist as a file."));
                var m = new Make(Toolchain, Cpp.CompilerType.clang, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem, TargetArchitecture, Configuration, SourceDirectory, BuildDirectory, null, CC, CXX, AR, ForceRegenerate, EnableNonTargetingOperatingSystemDummy);
                var Projects = m.GetAvailableProjects();
                var SelectedProjects = GetSelectedProjects(Memory, Quiet, Projects, m.CheckUnresolvedDependencies);
                GenerateRetypemakeScript(BuildingOperatingSystem, SourceDirectory, BuildDirectory, Memory, OverwriteRetypemakeScript);
                m.Execute(SelectedProjects);
                TextFile.WriteToFile(BuildDirectory / "gradle/local.properties", $"sdk.dir={AndroidSdk.ToString(PathStringStyle.Unix)}", new System.Text.UTF8Encoding(false), !ForceRegenerate);
                GenerateBuildScriptAndroid(Toolchain, BuildingOperatingSystem, BuildDirectory, TargetArchitecture, Configuration, AndroidNdk, CMake, Make, Ninja, ForceRegenerate);
                if (BuildAfterGenerate)
                {
                    using (var d = Shell.PushDirectory(BuildDirectory))
                    {
                        if (BuildingOperatingSystem == Cpp.OperatingSystemType.Windows)
                        {
                            MergeExitCode(Shell.Execute(@".\build.cmd"));
                        }
                        else if ((BuildingOperatingSystem == Cpp.OperatingSystemType.Linux) || (BuildingOperatingSystem == Cpp.OperatingSystemType.Mac))
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

        private static Dictionary<String, Make.ProjectDescription> GetSelectedProjects(Shell.EnvironmentVariableMemory Memory, bool Quiet, Dictionary<String, Make.ProjectDescription> Projects, Func<Dictionary<String, Make.ProjectDescription>, Dictionary<String, List<String>>> CheckUnresolvedDependencies)
        {
            var ProjectSet = new HashSet<String>(Projects.Values.Select(t => t.Definition.Name));
            var SelectedProjectNames = Shell.RequireEnvironmentVariableMultipleSelection(Memory, "SelectedProjects", Quiet, ProjectSet, ProjectSet, Parts =>
            {
                var Unresolved = CheckUnresolvedDependencies(Parts.ToDictionary(Name => Name, Name => Projects[Name]));
                if (Unresolved.Count > 0)
                {
                    return new KeyValuePair<bool, String>(false, "Unresolved dependencies: " + String.Join("; ", Unresolved.Select(p => p.Key + " -> " + String.Join(" ", p.Value))) + ".");
                }
                else
                {
                    return new KeyValuePair<bool, String>(true, "");
                }
            });
            return SelectedProjectNames.ToDictionary(Name => Name, Name => Projects[Name]);
        }

        private static void GenerateRetypemakeScript(Cpp.OperatingSystemType BuildingOperatingSystem, PathString SourceDirectory, PathString BuildDirectory, Shell.EnvironmentVariableMemory Memory, bool OverwriteRetypemakeScript)
        {
            if (BuildingOperatingSystem == Cpp.OperatingSystemType.Windows)
            {
                var Lines = new List<String>();
                Lines.Add("@echo off");
                Lines.Add("");
                Lines.Add("setlocal");
                Lines.Add("if \"%SUB_NO_PAUSE_SYMBOL%\"==\"1\" set NO_PAUSE_SYMBOL=1");
                Lines.Add("if /I \"%COMSPEC%\" == %CMDCMDLINE% set NO_PAUSE_SYMBOL=1");
                Lines.Add("set SUB_NO_PAUSE_SYMBOL=1");
                Lines.Add("call :main");
                Lines.Add("set EXIT_CODE=%ERRORLEVEL%");
                Lines.Add("if not \"%NO_PAUSE_SYMBOL%\"==\"1\" pause");
                Lines.Add("exit /b %EXIT_CODE%");
                Lines.Add("");
                Lines.Add(":main");
                foreach (var p in Memory.Variables)
                {
                    if (p.Key == "BuildAfterGenerate")
                    {
                        Lines.Add("set BuildAfterGenerate=False");
                    }
                    else if (p.Key == "BuildDirectory")
                    {
                        Lines.Add("set BuildDirectory=%~dp0");
                    }
                    else
                    {
                        if (Memory.VariableSelections.ContainsKey(p.Key))
                        {
                            Lines.Add($":: {String.Join("|", Memory.VariableSelections[p.Key])}");
                        }
                        if (Memory.VariableMultipleSelections.ContainsKey(p.Key))
                        {
                            Lines.Add($":: {String.Join(" ", Memory.VariableMultipleSelections[p.Key])}");
                        }
                        Lines.Add($"set " + Shell.EscapeArgumentForShell(p.Key + "=" + p.Value, Shell.ShellArgumentStyle.CMD));
                    }
                }
                Lines.Add("pushd \"%SourceDirectory%\" || exit /b 1");
                Lines.Add("call .\\typemake.cmd %* || exit /b 1 & popd & exit /b 0"); //all commands after typemake need to be in one line; or it may cause trouble when the file is changed by typemake
                Lines.Add("");
                var RetypemakePath = BuildDirectory / "retypemake.cmd";
                if (OverwriteRetypemakeScript || !File.Exists(RetypemakePath))
                {
                    TextFile.WriteToFile(RetypemakePath, String.Join("\r\n", Lines), System.Text.Encoding.Default, false);
                }
                else
                {
                    WriteLineError("Retypemake script exists, script generation skipped.");
                }
            }
            else
            {
                var Lines = new List<String>();
                Lines.Add("#!/bin/bash");
                Lines.Add("set -e");
                foreach (var p in Memory.Variables)
                {
                    if (p.Key == "BuildAfterGenerate")
                    {
                        Lines.Add("export BuildAfterGenerate=False");
                    }
                    else if (p.Key == "BuildDirectory")
                    {
                        Lines.Add("export BuildDirectory=$(cd `dirname \"$0\"`; pwd)");
                    }
                    else
                    {
                        if (Memory.VariableSelections.ContainsKey(p.Key))
                        {
                            Lines.Add($"# {String.Join("|", Memory.VariableSelections[p.Key])}");
                        }
                        if (Memory.VariableMultipleSelections.ContainsKey(p.Key))
                        {
                            Lines.Add($"# {String.Join(" ", Memory.VariableMultipleSelections[p.Key])}");
                        }
                        Lines.Add($"export {p.Key}={Shell.EscapeArgumentForShell(p.Value, Shell.ShellArgumentStyle.Bash)}");
                    }
                }
                Lines.Add("pushd \"${SourceDirectory}\"");
                Lines.Add("./typemake.sh \"$@\"; popd; exit"); //all commands after typemake need to be in one line; or it may cause trouble when the file is changed by typemake
                Lines.Add("");
                var RetypemakePath = BuildDirectory / "retypemake.sh";
                if (OverwriteRetypemakeScript || !File.Exists(RetypemakePath))
                {
                    TextFile.WriteToFile(RetypemakePath, String.Join("\n", Lines), new System.Text.UTF8Encoding(false), false);
                    MergeExitCode(Shell.Execute("chmod", "+x", RetypemakePath));
                }
                else
                {
                    WriteLineError("Retypemake script exists, script generation skipped.");
                }
            }
        }
        private static void GenerateBuildScriptWindows(PathString BuildDirectory, String SolutionName, Cpp.ArchitectureType TargetArchitecture, Cpp.ConfigurationType Configuration, PathString VSDir, bool ForceRegenerate)
        {
            var Lines = new List<String>();
            Lines.Add("@echo off");
            Lines.Add("");
            Lines.Add("setlocal");
            Lines.Add("if \"%SUB_NO_PAUSE_SYMBOL%\"==\"1\" set NO_PAUSE_SYMBOL=1");
            Lines.Add("if /I \"%COMSPEC%\" == %CMDCMDLINE% set NO_PAUSE_SYMBOL=1");
            Lines.Add("set SUB_NO_PAUSE_SYMBOL=1");
            Lines.Add("call :main");
            Lines.Add("set EXIT_CODE=%ERRORLEVEL%");
            Lines.Add("if not \"%NO_PAUSE_SYMBOL%\"==\"1\" pause");
            Lines.Add("exit /b %EXIT_CODE%");
            Lines.Add("");
            Lines.Add(":main");
            Lines.Add($@"""{VSDir.ToString(PathStringStyle.Windows)}\MSBuild\15.0\Bin\MSBuild.exe"" {SolutionName}.sln /p:Configuration={Configuration} /p:Platform={SlnGenerator.GetArchitectureString(TargetArchitecture)} /m:{Environment.ProcessorCount.ToString()} || exit /b 1");
            Lines.Add("");
            var BuildPath = BuildDirectory / $"build_{TargetArchitecture}_{Configuration}.cmd";
            TextFile.WriteToFile(BuildPath, String.Join("\r\n", Lines), System.Text.Encoding.Default, !ForceRegenerate);
        }
        private static void GenerateBuildScriptLinux(Cpp.ToolchainType Toolchain, Cpp.OperatingSystemType BuildingOperatingSystem, PathString BuildDirectory, Cpp.ConfigurationType Configuration, PathString CMake, PathString Make, PathString Ninja, bool ForceRegenerate)
        {
            if (Toolchain == Cpp.ToolchainType.CMake)
            {
                var CMakeArguments = new List<String>();
                CMakeArguments.Add(".");
                CMakeArguments.Add($"-DCMAKE_BUILD_TYPE={Configuration}");

                if (BuildingOperatingSystem == Cpp.OperatingSystemType.Windows)
                {
                    var Lines = new List<String>();
                    Lines.Add("@echo off");
                    Lines.Add("");
                    Lines.Add("setlocal");
                    Lines.Add("if \"%SUB_NO_PAUSE_SYMBOL%\"==\"1\" set NO_PAUSE_SYMBOL=1");
                    Lines.Add("if /I \"%COMSPEC%\" == %CMDCMDLINE% set NO_PAUSE_SYMBOL=1");
                    Lines.Add("set SUB_NO_PAUSE_SYMBOL=1");
                    Lines.Add("call :main");
                    Lines.Add("set EXIT_CODE=%ERRORLEVEL%");
                    Lines.Add("if not \"%NO_PAUSE_SYMBOL%\"==\"1\" pause");
                    Lines.Add("exit /b %EXIT_CODE%");
                    Lines.Add("");
                    Lines.Add(":main");
                    Lines.Add("wsl " + Shell.EscapeArgumentForShell(CMake, Shell.ShellArgumentStyle.CMD) + " " + String.Join(" ", CMakeArguments.Select(a => Shell.EscapeArgumentForShell(a, Shell.ShellArgumentStyle.CMD))) + " || exit /b 1");
                    Lines.Add("wsl " + Shell.EscapeArgumentForShell(Make, Shell.ShellArgumentStyle.CMD) + " -j" + Environment.ProcessorCount.ToString() + " || exit /b 1");
                    Lines.Add("");
                    var BuildPath = BuildDirectory / "build.cmd";
                    TextFile.WriteToFile(BuildPath, String.Join("\r\n", Lines), System.Text.Encoding.Default, !ForceRegenerate);
                }
                else
                {
                    var Lines = new List<String>();
                    Lines.Add("#!/bin/bash");
                    Lines.Add("set -e");
                    Lines.Add(Shell.EscapeArgumentForShell(CMake, Shell.ShellArgumentStyle.Bash) + " " + String.Join(" ", CMakeArguments.Select(a => Shell.EscapeArgumentForShell(a, Shell.ShellArgumentStyle.Bash))));
                    Lines.Add(Shell.EscapeArgumentForShell(Make, Shell.ShellArgumentStyle.Bash) + " -j" + Environment.ProcessorCount.ToString());
                    Lines.Add("");
                    var BuildPath = BuildDirectory / "build.sh";
                    TextFile.WriteToFile(BuildPath, String.Join("\n", Lines), new System.Text.UTF8Encoding(false), !ForceRegenerate);
                    MergeExitCode(Shell.Execute("chmod", "+x", BuildPath));
                }
            }
            else if (Toolchain == Cpp.ToolchainType.Ninja)
            {
                if (BuildingOperatingSystem == Cpp.OperatingSystemType.Windows)
                {
                    var Lines = new List<String>();
                    Lines.Add("@echo off");
                    Lines.Add("");
                    Lines.Add("setlocal");
                    Lines.Add("if \"%SUB_NO_PAUSE_SYMBOL%\"==\"1\" set NO_PAUSE_SYMBOL=1");
                    Lines.Add("if /I \"%COMSPEC%\" == %CMDCMDLINE% set NO_PAUSE_SYMBOL=1");
                    Lines.Add("set SUB_NO_PAUSE_SYMBOL=1");
                    Lines.Add("call :main");
                    Lines.Add("set EXIT_CODE=%ERRORLEVEL%");
                    Lines.Add("if not \"%NO_PAUSE_SYMBOL%\"==\"1\" pause");
                    Lines.Add("exit /b %EXIT_CODE%");
                    Lines.Add("");
                    Lines.Add(":main");
                    Lines.Add("wsl " + Shell.EscapeArgumentForShell(Ninja.RelativeTo(BuildDirectory).ToString(PathStringStyle.Unix), Shell.ShellArgumentStyle.CMD) + " -j" + Environment.ProcessorCount.ToString() + " -C projects -f build.ninja || exit /b 1");
                    Lines.Add("");
                    var BuildPath = BuildDirectory / "build.cmd";
                    TextFile.WriteToFile(BuildPath, String.Join("\r\n", Lines), System.Text.Encoding.Default, !ForceRegenerate);
                }
                else
                {
                    var Lines = new List<String>();
                    Lines.Add("#!/bin/bash");
                    Lines.Add("set -e");
                    Lines.Add(Shell.EscapeArgumentForShell(Ninja.RelativeTo(BuildDirectory).ToString(PathStringStyle.Unix), Shell.ShellArgumentStyle.Bash) + " -j" + Environment.ProcessorCount.ToString() + " -C projects -f build.ninja");
                    Lines.Add("");
                    var BuildPath = BuildDirectory / "build.sh";
                    TextFile.WriteToFile(BuildPath, String.Join("\n", Lines), new System.Text.UTF8Encoding(false), !ForceRegenerate);
                    MergeExitCode(Shell.Execute("chmod", "+x", BuildPath));
                }
            }
        }
        private static void GenerateBuildScriptXCode(Cpp.OperatingSystemType BuildingOperatingSystem, PathString BuildDirectory, Make.Result Result, bool ForceRegenerate)
        {
            var Lines = new List<String>();
            Lines.Add("#!/bin/bash");
            Lines.Add("set -e");
            foreach (var p in Result.SortedProjects)
            {
                Lines.Add($"xcodebuild -project projects/{p.Name}.xcodeproj");
            }
            Lines.Add("");
            var BuildPath = BuildDirectory / "build.sh";
            TextFile.WriteToFile(BuildPath, String.Join("\n", Lines), new System.Text.UTF8Encoding(false), !ForceRegenerate);
            if (BuildingOperatingSystem != Cpp.OperatingSystemType.Windows)
            {
                MergeExitCode(Shell.Execute("chmod", "+x", BuildPath));
            }
        }
        private static void GenerateBuildScriptAndroid(Cpp.ToolchainType Toolchain, Cpp.OperatingSystemType BuildingOperatingSystem, PathString BuildDirectory, Cpp.ArchitectureType TargetArchitecture, Cpp.ConfigurationType Configuration, PathString AndroidNdk, PathString CMake, PathString Make, PathString Ninja, bool ForceRegenerate)
        {
            if (Toolchain == Cpp.ToolchainType.Gradle_CMake)
            {
                var CMakeArguments = new List<String>();
                CMakeArguments.Add(".");
                CMakeArguments.Add("-G");
                CMakeArguments.Add("Unix Makefiles");
                CMakeArguments.Add($"-DCMAKE_BUILD_TYPE={Configuration}");
                CMakeArguments.Add($"-DCMAKE_MAKE_PROGRAM={Make.ToString(PathStringStyle.Unix)}");
                CMakeArguments.Add($"-DANDROID_NDK={AndroidNdk.ToString(PathStringStyle.Unix)}");
                CMakeArguments.Add($"-DCMAKE_TOOLCHAIN_FILE={(AndroidNdk / "build/cmake/android.toolchain.cmake").ToString(PathStringStyle.Unix)}");
                CMakeArguments.Add($"-DANDROID_STL=c++_static");
                CMakeArguments.Add($"-DANDROID_PLATFORM=android-17");
                CMakeArguments.Add($"-DANDROID_ABI={Cpp.GradleProjectGenerator.GetArchitectureString(TargetArchitecture)}");
                if (TargetArchitecture == Cpp.ArchitectureType.armeabi_v7a)
                {
                    CMakeArguments.Add($"-DANDROID_ARM_NEON=ON");
                }

                if (BuildingOperatingSystem == Cpp.OperatingSystemType.Windows)
                {
                    var Lines = new List<String>();
                    Lines.Add("@echo off");
                    Lines.Add("");
                    Lines.Add("setlocal");
                    Lines.Add("if \"%SUB_NO_PAUSE_SYMBOL%\"==\"1\" set NO_PAUSE_SYMBOL=1");
                    Lines.Add("if /I \"%COMSPEC%\" == %CMDCMDLINE% set NO_PAUSE_SYMBOL=1");
                    Lines.Add("set SUB_NO_PAUSE_SYMBOL=1");
                    Lines.Add("call :main");
                    Lines.Add("set EXIT_CODE=%ERRORLEVEL%");
                    Lines.Add("if not \"%NO_PAUSE_SYMBOL%\"==\"1\" pause");
                    Lines.Add("exit /b %EXIT_CODE%");
                    Lines.Add("");
                    Lines.Add(":main");
                    Lines.Add(Shell.EscapeArgumentForShell(CMake, Shell.ShellArgumentStyle.CMD) + " " + String.Join(" ", CMakeArguments.Select(a => Shell.EscapeArgumentForShell(a, Shell.ShellArgumentStyle.CMD))) + " || exit /b 1");
                    Lines.Add(Shell.EscapeArgumentForShell(Make, Shell.ShellArgumentStyle.CMD) + " -j" + Environment.ProcessorCount.ToString() + " || exit /b 1");
                    Lines.Add("pushd gradle || exit /b 1");
                    Lines.Add($@"call .\gradlew.bat assemble{Configuration} || exit /b 1");
                    Lines.Add("popd");
                    Lines.Add("");
                    var BuildPath = BuildDirectory / "build.cmd";
                    TextFile.WriteToFile(BuildPath, String.Join("\r\n", Lines), System.Text.Encoding.Default, !ForceRegenerate);
                }
                else
                {
                    var Lines = new List<String>();
                    Lines.Add("#!/bin/bash");
                    Lines.Add("set -e");
                    Lines.Add(Shell.EscapeArgumentForShell(CMake, Shell.ShellArgumentStyle.Bash) + " " + String.Join(" ", CMakeArguments.Select(a => Shell.EscapeArgumentForShell(a, Shell.ShellArgumentStyle.Bash))));
                    Lines.Add(Shell.EscapeArgumentForShell(Make, Shell.ShellArgumentStyle.Bash) + " -j" + Environment.ProcessorCount.ToString());
                    Lines.Add("pushd gradle");
                    Lines.Add($@"./gradlew assemble{Configuration}");
                    Lines.Add("popd");
                    Lines.Add("");
                    var BuildPath = BuildDirectory / "build.sh";
                    TextFile.WriteToFile(BuildPath, String.Join("\n", Lines), new System.Text.UTF8Encoding(false), !ForceRegenerate);
                    MergeExitCode(Shell.Execute("chmod", "+x", BuildPath));
                }
            }
            else if (Toolchain == Cpp.ToolchainType.Gradle_Ninja)
            {
                if (BuildingOperatingSystem == Cpp.OperatingSystemType.Windows)
                {
                    var Lines = new List<String>();
                    Lines.Add("@echo off");
                    Lines.Add("");
                    Lines.Add("setlocal");
                    Lines.Add("if \"%SUB_NO_PAUSE_SYMBOL%\"==\"1\" set NO_PAUSE_SYMBOL=1");
                    Lines.Add("if /I \"%COMSPEC%\" == %CMDCMDLINE% set NO_PAUSE_SYMBOL=1");
                    Lines.Add("set SUB_NO_PAUSE_SYMBOL=1");
                    Lines.Add("call :main");
                    Lines.Add("set EXIT_CODE=%ERRORLEVEL%");
                    Lines.Add("if not \"%NO_PAUSE_SYMBOL%\"==\"1\" pause");
                    Lines.Add("exit /b %EXIT_CODE%");
                    Lines.Add("");
                    Lines.Add(":main");
                    Lines.Add(Shell.EscapeArgumentForShell(Ninja.RelativeTo(BuildDirectory).ToString(PathStringStyle.Windows), Shell.ShellArgumentStyle.CMD) + " -j" + Environment.ProcessorCount.ToString() + " -C projects -f build.ninja || exit /b 1");
                    Lines.Add("pushd gradle || exit /b 1");
                    Lines.Add($@"call .\gradlew.bat assemble{Configuration} || exit /b 1");
                    Lines.Add("popd");
                    Lines.Add("");
                    var BuildPath = BuildDirectory / "build.cmd";
                    TextFile.WriteToFile(BuildPath, String.Join("\r\n", Lines), System.Text.Encoding.Default, !ForceRegenerate);
                }
                else
                {
                    var Lines = new List<String>();
                    Lines.Add("#!/bin/bash");
                    Lines.Add("set -e");
                    Lines.Add(Shell.EscapeArgumentForShell(Ninja.RelativeTo(BuildDirectory).ToString(PathStringStyle.Unix), Shell.ShellArgumentStyle.Bash) + " -j" + Environment.ProcessorCount.ToString() + " -C projects -f build.ninja");
                    Lines.Add("pushd gradle");
                    Lines.Add($@"./gradlew assemble{Configuration}");
                    Lines.Add("popd");
                    Lines.Add("");
                    var BuildPath = BuildDirectory / "build.sh";
                    TextFile.WriteToFile(BuildPath, String.Join("\n", Lines), new System.Text.UTF8Encoding(false), !ForceRegenerate);
                    MergeExitCode(Shell.Execute("chmod", "+x", BuildPath));
                }
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
