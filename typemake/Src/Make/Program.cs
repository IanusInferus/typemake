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
            if (Shell.OperatingSystem == Shell.BuildingOperatingSystemType.Windows)
            {
                BuildingOperatingSystem = Cpp.OperatingSystemType.Windows;
            }
            else if (Shell.OperatingSystem == Shell.BuildingOperatingSystemType.Linux)
            {
                BuildingOperatingSystem = Cpp.OperatingSystemType.Linux;
            }
            else if (Shell.OperatingSystem == Shell.BuildingOperatingSystemType.Mac)
            {
                BuildingOperatingSystem = Cpp.OperatingSystemType.Mac;
            }
            else
            {
                throw new InvalidOperationException("UnknownBuildingOperatingSystem");
            }
            var BuildingOperatingSystemArchitecture = Cpp.ArchitectureType.x86_64;
            if (Shell.OperatingSystemArchitecture == Shell.BuildingOperatingSystemArchitectureType.x86_64)
            {
                BuildingOperatingSystemArchitecture = Cpp.ArchitectureType.x86_64;
            }
            else if (Shell.OperatingSystemArchitecture == Shell.BuildingOperatingSystemArchitectureType.x86)
            {
                BuildingOperatingSystemArchitecture = Cpp.ArchitectureType.x86;
            }
            else
            {
                throw new InvalidOperationException("UnknownBuildingOperatingSystemArchitecture");
            }
            //process architecture is supposed to be the same as the operating system architecture

            var argv = args.Where(arg => !arg.StartsWith("--")).ToArray();
            var options = args.Where(arg => arg.StartsWith("--")).Select(arg => arg.Substring(2).Split(new Char[] { ':' }, 2)).GroupBy(p => p[0]).ToDictionary(g => g.Key, g => g.Last().Skip(1).SingleOrDefault(), StringComparer.OrdinalIgnoreCase);
            var Help = options.ContainsKey("help");
            if (Help)
            {
                DisplayInfo();
                return 0;
            }
            if (argv.Length == 1)
            {
                var RetypemakeScriptPath = argv[0];
                String[] Lines;
                Regex r;
                if (RetypemakeScriptPath.EndsWith(".cmd"))
                {
                    Lines = File.ReadAllLines(RetypemakeScriptPath, System.Text.Encoding.Default);
                    r = new Regex(@"^set\s+(?<Key>[^=]+)=(?<Value>.*)\s*$");
                }
                else if (RetypemakeScriptPath.EndsWith(".sh"))
                {
                    Lines = File.ReadAllLines(RetypemakeScriptPath, new System.Text.UTF8Encoding(false));
                    r = new Regex(@"^export\s+(?<Key>[^=]+)=(?<Value>.*)\s*$");
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
                            Environment.SetEnvironmentVariable(Key, Path.GetDirectoryName(Path.GetFullPath(RetypemakeScriptPath)));
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
            var ForceRegenerate = options.ContainsKey("regen");
            var EnableNonTargetingOperatingSystemDummy = options.ContainsKey("dummy");
            var Quiet = options.ContainsKey("quiet");

            var Memory = new Shell.EnvironmentVariableMemory();

            bool BuildAfterGenerate;
            Shell.RequireEnvironmentVariableBoolean(Memory, "BuildAfterGenerate", out BuildAfterGenerate, Quiet, true);

            String SourceDirectory;
            String BuildDirectory;
            Shell.RequireEnvironmentVariable(Memory, "SourceDirectory", out SourceDirectory, Quiet, p => Directory.Exists(p), p => Path.GetFullPath(p));

            Cpp.OperatingSystemType TargetOperatingSystem;
            Shell.RequireEnvironmentVariableEnum<Cpp.OperatingSystemType>(Memory, "TargetOperatingSystem", out TargetOperatingSystem, Quiet, BuildingOperatingSystem);

            if (TargetOperatingSystem == Cpp.OperatingSystemType.Windows)
            {
                Shell.RequireEnvironmentVariable(Memory, "BuildDirectory", out BuildDirectory, Quiet, p => !File.Exists(p), p => Path.GetFullPath(p), "build/windows");
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
                            var p = Path.Combine(Path.Combine(ProgramFiles, @"Microsoft Visual Studio\2017"), d);
                            if (Directory.Exists(p))
                            {
                                DefaultVSDir = p;
                                break;
                            }
                        }
                    }
                    Shell.RequireEnvironmentVariable(Memory, "VSDir", out VSDir, Quiet, p => Directory.Exists(p), p => Path.GetFullPath(p), DefaultVSDir);
                    Shell.RequireEnvironmentVariableEnum(Memory, "TargetArchitecture", out TargetArchitecture, Quiet, new HashSet<Cpp.ArchitectureType> { Cpp.ArchitectureType.x86, Cpp.ArchitectureType.x86_64 }, Cpp.ArchitectureType.x86_64);
                    Shell.RequireEnvironmentVariableEnum(Memory, "Configuration", out Configuration, Quiet, Cpp.ConfigurationType.Debug);
                }
                var m = new Make(Cpp.ToolchainType.Windows_VisualC, Cpp.CompilerType.VisualC, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem, null, SourceDirectory, BuildDirectory, null, ForceRegenerate, EnableNonTargetingOperatingSystemDummy);
                var r = m.Execute();
                GenerateRetypemakeScript(BuildingOperatingSystem, SourceDirectory, BuildDirectory, Memory, ForceRegenerate);
                if (BuildAfterGenerate && (BuildingOperatingSystem == Cpp.OperatingSystemType.Windows))
                {
                    GenerateBuildScriptWindows(BuildDirectory, r.SolutionName, Cpp.ArchitectureType.x86, Cpp.ConfigurationType.Debug, VSDir, ForceRegenerate);
                    GenerateBuildScriptWindows(BuildDirectory, r.SolutionName, Cpp.ArchitectureType.x86, Cpp.ConfigurationType.Release, VSDir, ForceRegenerate);
                    GenerateBuildScriptWindows(BuildDirectory, r.SolutionName, Cpp.ArchitectureType.x86_64, Cpp.ConfigurationType.Debug, VSDir, ForceRegenerate);
                    GenerateBuildScriptWindows(BuildDirectory, r.SolutionName, Cpp.ArchitectureType.x86_64, Cpp.ConfigurationType.Release, VSDir, ForceRegenerate);
                    using (var d = Shell.PushDirectory(BuildDirectory))
                    {
                        MergeExitCode(Shell.Execute($"build_{TargetArchitecture}_{Configuration}.cmd"));
                    }
                }
            }
            else if (TargetOperatingSystem == Cpp.OperatingSystemType.Linux)
            {
                Cpp.ConfigurationType Configuration;
                Shell.RequireEnvironmentVariableEnum(Memory, "Configuration", out Configuration, Quiet, Cpp.ConfigurationType.Debug);
                Shell.RequireEnvironmentVariable(Memory, "BuildDirectory", out BuildDirectory, Quiet, p => !File.Exists(p), p => Path.GetFullPath(p), $"build/linux_{Configuration}");
                var CMake = "";
                if (BuildingOperatingSystem == Cpp.OperatingSystemType.Linux)
                {
                    Shell.RequireEnvironmentVariable(Memory, "CMake", out CMake, Quiet, p => File.Exists(p), p => Path.GetFullPath(p), Shell.TryLocate("cmake") ?? "");
                }
                var Make = "";
                if (BuildAfterGenerate && (BuildingOperatingSystem == Cpp.OperatingSystemType.Linux))
                {
                    Shell.RequireEnvironmentVariable(Memory, "Make", out Make, Quiet, p => File.Exists(p), p => Path.GetFullPath(p), Shell.TryLocate("make") ?? "");
                }
                var m = new Make(Cpp.ToolchainType.CMake, Cpp.CompilerType.gcc, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem, null, SourceDirectory, BuildDirectory, null, ForceRegenerate, EnableNonTargetingOperatingSystemDummy);
                m.Execute();
                if (BuildingOperatingSystem == Cpp.OperatingSystemType.Linux)
                {
                    using (var d = Shell.PushDirectory(BuildDirectory))
                    {
                        var Arguments = new List<String>();
                        Arguments.Add(BuildDirectory);
                        Arguments.Add($"-DCMAKE_BUILD_TYPE={Configuration}");
                        MergeExitCode(Shell.Execute(CMake, Arguments.ToArray()));
                    }
                }
                GenerateRetypemakeScript(BuildingOperatingSystem, SourceDirectory, BuildDirectory, Memory, ForceRegenerate);
                if (BuildAfterGenerate && (BuildingOperatingSystem == Cpp.OperatingSystemType.Linux))
                {
                    using (var d = Shell.PushDirectory(BuildDirectory))
                    {
                        MergeExitCode(Shell.Execute(Make));
                    }
                }
            }
            else if (TargetOperatingSystem == Cpp.OperatingSystemType.Mac)
            {
                Shell.RequireEnvironmentVariable(Memory, "BuildDirectory", out BuildDirectory, Quiet, p => !File.Exists(p), p => Path.GetFullPath(p), "build/mac");
                var m = new Make(Cpp.ToolchainType.Mac_XCode, Cpp.CompilerType.clang, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem, null, SourceDirectory, BuildDirectory, null, ForceRegenerate, EnableNonTargetingOperatingSystemDummy);
                var r = m.Execute();
                GenerateRetypemakeScript(BuildingOperatingSystem, SourceDirectory, BuildDirectory, Memory, ForceRegenerate);
                GenerateBuildScriptXCode(BuildingOperatingSystem, BuildDirectory, r, ForceRegenerate);
                if (BuildAfterGenerate && (BuildingOperatingSystem == Cpp.OperatingSystemType.Mac))
                {
                    using (var d = Shell.PushDirectory(BuildDirectory))
                    {
                        MergeExitCode(Shell.Execute("./build.sh"));
                    }
                }
            }
            else if (TargetOperatingSystem == Cpp.OperatingSystemType.iOS)
            {
                Shell.RequireEnvironmentVariable(Memory, "BuildDirectory", out BuildDirectory, Quiet, p => !File.Exists(p), p => Path.GetFullPath(p), "build/ios");
                String DevelopmentTeam;
                Shell.RequireEnvironmentVariable(Memory, "DevelopmentTeam", out DevelopmentTeam, Quiet, null, null, null, "(find by search an existing pbxproj file with DEVELOPMENT_TEAM)");
                var m = new Make(Cpp.ToolchainType.Mac_XCode, Cpp.CompilerType.clang, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem, null, SourceDirectory, BuildDirectory, DevelopmentTeam, ForceRegenerate, EnableNonTargetingOperatingSystemDummy);
                m.Execute();
                var r = m.Execute();
                GenerateRetypemakeScript(BuildingOperatingSystem, SourceDirectory, BuildDirectory, Memory, ForceRegenerate);
                GenerateBuildScriptXCode(BuildingOperatingSystem, BuildDirectory, r, ForceRegenerate);
                if (BuildAfterGenerate && (BuildingOperatingSystem == Cpp.OperatingSystemType.Mac))
                {
                    using (var d = Shell.PushDirectory(BuildDirectory))
                    {
                        MergeExitCode(Shell.Execute("./build.sh"));
                    }
                }
            }
            else if (TargetOperatingSystem == Cpp.OperatingSystemType.Android)
            {
                String AndroidSdk;
                Shell.RequireEnvironmentVariable(Memory, "AndroidSdk", out AndroidSdk, Quiet, p => Directory.Exists(Path.Combine(p, "platform-tools")), p => Path.GetFullPath(p), BuildingOperatingSystem == Cpp.OperatingSystemType.Windows ? Path.Combine(Environment.GetEnvironmentVariable("LocalAppData"), @"Android\sdk") : "");
                String AndroidNdk;
                Shell.RequireEnvironmentVariable(Memory, "AndroidNdk", out AndroidNdk, Quiet, p => Directory.Exists(Path.Combine(p, "build")), p => Path.GetFullPath(p), Path.Combine(AndroidSdk, "ndk-bundle"));
                String CMake;
                Shell.RequireEnvironmentVariable(Memory, "CMake", out CMake, Quiet, p => File.Exists(p), p => Path.GetFullPath(p), Shell.TryLocate("cmake") ?? (BuildingOperatingSystem == Cpp.OperatingSystemType.Windows ? Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles"), @"CMake\bin\cmake.exe") : ""));
                Cpp.ArchitectureType TargetArchitecture;
                Shell.RequireEnvironmentVariableEnum(Memory, "TargetArchitecture", out TargetArchitecture, Quiet, Cpp.ArchitectureType.armeabi_v7a);
                Cpp.ConfigurationType Configuration;
                Shell.RequireEnvironmentVariableEnum(Memory, "Configuration", out Configuration, Quiet, Cpp.ConfigurationType.Debug);
                Shell.RequireEnvironmentVariable(Memory, "BuildDirectory", out BuildDirectory, Quiet, p => !File.Exists(p), p => Path.GetFullPath(p), $"build/android_{TargetArchitecture}_{Configuration}");
                var m = new Make(Cpp.ToolchainType.Gradle_CMake, Cpp.CompilerType.clang, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem, TargetArchitecture, SourceDirectory, BuildDirectory, null, ForceRegenerate, EnableNonTargetingOperatingSystemDummy);
                m.Execute();
                TextFile.WriteToFile(Path.Combine(BuildDirectory, Path.Combine("gradle", "local.properties")), $"sdk.dir={AndroidSdk.Replace("\\", "/")}", new System.Text.UTF8Encoding(false), !ForceRegenerate);
                GenerateRetypemakeScript(BuildingOperatingSystem, SourceDirectory, BuildDirectory, Memory, ForceRegenerate);
                var Make = "";
                if (BuildingOperatingSystemArchitecture == Cpp.ArchitectureType.x86_64)
                {
                    if (BuildingOperatingSystem == Cpp.OperatingSystemType.Windows)
                    {
                        Make = $@"{AndroidNdk}\prebuilt\windows-x86_64\bin\make.exe";
                    }
                    else if (BuildingOperatingSystem == Cpp.OperatingSystemType.Linux)
                    {
                        Make = $"{AndroidNdk}/prebuilt/linux-x86_64/bin/make";
                    }
                    else if (BuildingOperatingSystem == Cpp.OperatingSystemType.Mac)
                    {
                        Make = $"{AndroidNdk}/prebuilt/darwin-x86_64/bin/make";
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }
                else
                {
                    Console.WriteLine("UnsupportedBuildingOperatingSystemArchitecture: " + Shell.OperatingSystemArchitecture.ToString());
                    return 1;
                }
                GenerateBuildScriptAndroid(BuildingOperatingSystem, BuildDirectory, Make, ForceRegenerate);
                using (var d = Shell.PushDirectory(BuildDirectory))
                {
                    var Arguments = new List<String>();
                    Arguments.Add(BuildDirectory.Replace("\\", "/"));
                    Arguments.Add("-G");
                    Arguments.Add("Unix Makefiles");
                    Arguments.Add($"-DCMAKE_BUILD_TYPE={Configuration}");
                    Arguments.Add($"-DCMAKE_MAKE_PROGRAM={Make.Replace("\\", "/")}");
                    Arguments.Add($"-DANDROID_NDK={AndroidNdk.Replace("\\", "/")}");
                    Arguments.Add($"-DCMAKE_TOOLCHAIN_FILE={AndroidNdk.Replace("\\", "/")}/build/cmake/android.toolchain.cmake");
                    Arguments.Add($"-DANDROID_STL=c++_static");
                    Arguments.Add($"-DANDROID_PLATFORM=android-17");
                    Arguments.Add($"-DANDROID_ABI={Cpp.GradleProjectGenerator.GetArchitectureString(TargetArchitecture)}");
                    if (TargetArchitecture == Cpp.ArchitectureType.armeabi_v7a)
                    {
                        Arguments.Add($"-DANDROID_ARM_NEON=ON");
                    }
                    MergeExitCode(Shell.Execute(CMake, Arguments.ToArray()));
                }
                if (BuildAfterGenerate)
                {
                    using (var d = Shell.PushDirectory(BuildDirectory))
                    {
                        if (BuildingOperatingSystem == Cpp.OperatingSystemType.Windows)
                        {
                            MergeExitCode(Shell.Execute(@".\build.cmd"));
                        }
                        else
                        {
                            MergeExitCode(Shell.Execute("./build.sh"));
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

        private static void GenerateRetypemakeScript(Cpp.OperatingSystemType BuildingOperatingSystem, String SourceDirectory, String BuildDirectory, Shell.EnvironmentVariableMemory Memory, bool ForceRegenerate)
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
                Lines.Add("");
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
                        Lines.Add($"set {p.Key}={p.Value}");
                    }
                }
                Lines.Add("pushd \"%SourceDirectory%\"");
                Lines.Add(@"call .\typemake.cmd --quiet %*");
                Lines.Add("popd");
                Lines.Add("");
                Lines.Add("if not \"%NO_PAUSE_SYMBOL%\"==\"1\" pause");
                Lines.Add("exit /b %EXIT_CODE%");
                Lines.Add("");
                var RetypemakePath = Path.Combine(BuildDirectory, "retypemake.cmd");
                if (ForceRegenerate || !File.Exists(RetypemakePath))
                {
                    TextFile.WriteToFile(RetypemakePath, String.Join("\r\n", Lines), System.Text.Encoding.Default, false);
                }
            }
            else
            {
                var Lines = new List<String>();
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
                        Lines.Add($"export {p.Key}={p.Value}");
                    }
                }
                Lines.Add("pushd \"${SourceDirectory}\"");
                Lines.Add("./typemake.sh --quiet \"$@\"");
                Lines.Add("popd");
                Lines.Add("");
                var RetypemakePath = Path.Combine(BuildDirectory, "retypemake.sh");
                if (ForceRegenerate || !File.Exists(RetypemakePath))
                {
                    TextFile.WriteToFile(RetypemakePath, String.Join("\n", Lines), new System.Text.UTF8Encoding(false), false);
                    MergeExitCode(Shell.Execute("chmod", "+x", RetypemakePath));
                }
            }
        }
        private static void GenerateBuildScriptWindows(String BuildDirectory, String SolutionName, Cpp.ArchitectureType TargetArchitecture, Cpp.ConfigurationType Configuration, String VSDir, bool ForceRegenerate)
        {
            var Lines = new List<String>();
            Lines.Add("@echo off");
            Lines.Add("");
            Lines.Add("setlocal");
            Lines.Add("if \"%SUB_NO_PAUSE_SYMBOL%\"==\"1\" set NO_PAUSE_SYMBOL=1");
            Lines.Add("if /I \"%COMSPEC%\" == %CMDCMDLINE% set NO_PAUSE_SYMBOL=1");
            Lines.Add("set SUB_NO_PAUSE_SYMBOL=1");
            Lines.Add("");
            Lines.Add($@"""{VSDir}\MSBuild\15.0\Bin\MSBuild.exe"" {SolutionName}.sln /p:Configuration={Configuration} /p:Platform={SlnGenerator.GetArchitectureString(TargetArchitecture)}");
            Lines.Add("");
            Lines.Add("if not \"%NO_PAUSE_SYMBOL%\"==\"1\" pause");
            Lines.Add("exit /b %EXIT_CODE%");
            Lines.Add("");
            var BuildPath = Path.Combine(BuildDirectory, $"build_{TargetArchitecture}_{Configuration}.cmd");
            TextFile.WriteToFile(BuildPath, String.Join("\r\n", Lines), System.Text.Encoding.Default, !ForceRegenerate);
        }
        private static void GenerateBuildScriptXCode(Cpp.OperatingSystemType BuildingOperatingSystem, String BuildDirectory, Make.Result Result, bool ForceRegenerate)
        {
            var Lines = new List<String>();
            foreach (var p in Result.SortedProjects)
            {
                Lines.Add($"xcodebuild -project projects/{p.Name}.xcodeproj");
            }
            Lines.Add("");
            var BuildPath = Path.Combine(BuildDirectory, "build.sh");
            TextFile.WriteToFile(BuildPath, String.Join("\n", Lines), new System.Text.UTF8Encoding(false), !ForceRegenerate);
            if (BuildingOperatingSystem != Cpp.OperatingSystemType.Windows)
            {
                MergeExitCode(Shell.Execute("chmod", "+x", BuildPath));
            }
        }
        private static void GenerateBuildScriptAndroid(Cpp.OperatingSystemType BuildingOperatingSystem, String BuildDirectory, String Make, bool ForceRegenerate)
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
                Lines.Add("");
                Lines.Add($@"""{Make}""");
                Lines.Add("pushd gradle");
                Lines.Add(@"call .\gradlew.bat build");
                Lines.Add("popd");
                Lines.Add("");
                Lines.Add("if not \"%NO_PAUSE_SYMBOL%\"==\"1\" pause");
                Lines.Add("exit /b %EXIT_CODE%");
                Lines.Add("");
                var BuildPath = Path.Combine(BuildDirectory, $"build.cmd");
                TextFile.WriteToFile(BuildPath, String.Join("\r\n", Lines), System.Text.Encoding.Default, !ForceRegenerate);
            }
            else
            {
                var Lines = new List<String>();
                Lines.Add($@"""{Make}""");
                Lines.Add("pushd gradle");
                Lines.Add(@"./gradlew build");
                Lines.Add("popd");
                Lines.Add("");
                var BuildPath = Path.Combine(BuildDirectory, "build.sh");
                TextFile.WriteToFile(BuildPath, String.Join("\n", Lines), new System.Text.UTF8Encoding(false), !ForceRegenerate);
                MergeExitCode(Shell.Execute("chmod", "+x", BuildPath));
            }
        }

        public static void DisplayInfo()
        {
            Console.WriteLine(@"TypeMake");
            Console.WriteLine(@"Usage:");
            Console.WriteLine(@"TypeMake [<RetypemakeScript>] [--regen] [--dummy] [--quiet] [--help]");
            Console.WriteLine(@"RetypemakeScript batch or bash file to get environment variables for diagnostics");
            Console.WriteLine(@"--regen forcely regenerate project files");
            Console.WriteLine(@"--dummy generate dummy projects for non-targeting operating systems");
            Console.WriteLine(@"--quiet no interactive variable input, all variables must be input from environment variables");
        }
    }
}
