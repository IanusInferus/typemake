using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace TypeMake
{
    public static class BuildScript
    {
        public static void GenerateRetypemakeScript(Cpp.OperatingSystemType HostOperatingSystem, PathString SourceDirectory, PathString BuildDirectory, Shell.EnvironmentVariableMemory Memory, bool OverwriteRetypemakeScript)
        {
            if (HostOperatingSystem == Cpp.OperatingSystemType.Windows)
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
                    if (p.Key == "BuildDirectory")
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
                    if (p.Key == "BuildDirectory")
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
                    if (Shell.Execute("chmod", "+x", RetypemakePath) != 0)
                    {
                        throw new InvalidOperationException("ErrorInExecution: chmod");
                    }
                }
                else
                {
                    WriteLineError("Retypemake script exists, script generation skipped.");
                }
            }
        }
        public static void GenerateBuildScriptWindows(Cpp.ToolchainType Toolchain, PathString BuildDirectory, String SolutionName, Cpp.ArchitectureType TargetArchitecture, Cpp.ConfigurationType Configuration, int MaxProcessCount, PathString VSDir, int VSVersion, PathString Ninja, bool ForceRegenerate)
        {
            if (Toolchain == Cpp.ToolchainType.VisualStudio)
            {
                var MSBuildVersion = VSVersion >= 2019 ? "Current" : "15.0";
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
                Lines.Add($"set MultiProcMaxCount={MaxProcessCount}");
                Lines.Add($@"""{VSDir.ToString(PathStringStyle.Windows)}\MSBuild\{MSBuildVersion}\Bin\MSBuild.exe"" {SolutionName}.sln /p:Configuration={Configuration} /p:Platform={Cpp.SlnGenerator.GetArchitectureString(Cpp.OperatingSystemType.Windows, TargetArchitecture)} /m:{MaxProcessCount} || exit /b 1");
                Lines.Add("");
                var BuildPath = BuildDirectory / $"build_{Configuration}.cmd";
                TextFile.WriteToFile(BuildPath, String.Join("\r\n", Lines), System.Text.Encoding.Default, !ForceRegenerate);
            }
            else if (Toolchain == Cpp.ToolchainType.Ninja)
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
                Lines.Add(Shell.EscapeArgumentForShell(Ninja.RelativeTo(BuildDirectory).ToString(PathStringStyle.Windows), Shell.ShellArgumentStyle.CMD) + $" -j{MaxProcessCount} -C projects -f build.ninja || exit /b 1");
                Lines.Add("");
                var BuildPath = BuildDirectory / "build.cmd";
                TextFile.WriteToFile(BuildPath, String.Join("\r\n", Lines), System.Text.Encoding.Default, !ForceRegenerate);
            }
        }
        public static void GenerateBuildScriptLinux(String TargetOperatingSystemDistribution, Cpp.ToolchainType Toolchain, Cpp.OperatingSystemType HostOperatingSystem, PathString BuildDirectory, Cpp.ConfigurationType Configuration, int MaxProcessCount, PathString Ninja, bool ForceRegenerate)
        {
            if (Toolchain == Cpp.ToolchainType.Ninja)
            {
                if (HostOperatingSystem == Cpp.OperatingSystemType.Windows)
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
                    Lines.Add("wsl -d " + Shell.EscapeArgumentForShell(TargetOperatingSystemDistribution, Shell.ShellArgumentStyle.CMD) + " " + Shell.EscapeArgumentForShell(Ninja.RelativeTo(BuildDirectory).ToWslPath().ToString(PathStringStyle.Unix), Shell.ShellArgumentStyle.CMD) + $" -j{MaxProcessCount} -C projects -f build.ninja || exit /b 1");
                    Lines.Add("");
                    var BuildPath = BuildDirectory / "build.cmd";
                    TextFile.WriteToFile(BuildPath, String.Join("\r\n", Lines), System.Text.Encoding.Default, !ForceRegenerate);
                }
                else
                {
                    var Lines = new List<String>();
                    Lines.Add("#!/bin/bash");
                    Lines.Add("set -e");
                    Lines.Add(Shell.EscapeArgumentForShell(Ninja.RelativeTo(BuildDirectory).ToString(PathStringStyle.Unix), Shell.ShellArgumentStyle.Bash) + $" -j{MaxProcessCount} -C projects -f build.ninja");
                    Lines.Add("");
                    var BuildPath = BuildDirectory / "build.sh";
                    TextFile.WriteToFile(BuildPath, String.Join("\n", Lines), new System.Text.UTF8Encoding(false), !ForceRegenerate);
                    if (Shell.Execute("chmod", "+x", BuildPath) != 0)
                    {
                        throw new InvalidOperationException("ErrorInExecution: chmod");
                    }
                }
            }
        }
        public static void GenerateBuildScriptXCode(Cpp.OperatingSystemType HostOperatingSystem, PathString BuildDirectory, Cpp.ConfigurationType Configuration, int MaxProcessCount, List<String> CppSortedProjectNames, bool ForceRegenerate)
        {
            var Lines = new List<String>();
            Lines.Add("#!/bin/bash");
            Lines.Add("set -e");
            foreach (var CppSortedProjectName in CppSortedProjectNames)
            {
                Lines.Add($"xcodebuild -project projects/{CppSortedProjectName}.xcodeproj -configuration {Configuration} -jobs {MaxProcessCount}");
            }
            Lines.Add("");
            var BuildPath = BuildDirectory / "build.sh";
            TextFile.WriteToFile(BuildPath, String.Join("\n", Lines), new System.Text.UTF8Encoding(false), !ForceRegenerate);
            if (HostOperatingSystem != Cpp.OperatingSystemType.Windows)
            {
                if (Shell.Execute("chmod", "+x", BuildPath) != 0)
                {
                    throw new InvalidOperationException("ErrorInExecution: chmod");
                }
            }
        }
        public static void GenerateBuildScriptAndroid(List<String> GradleProjectNames, Cpp.ToolchainType Toolchain, Cpp.OperatingSystemType HostOperatingSystem, PathString BuildDirectory, Cpp.ArchitectureType TargetArchitecture, Cpp.ConfigurationType Configuration, int MaxProcessCount, PathString AndroidNdk, PathString Ninja, int ApiLevel, bool ForceRegenerate, bool EnableJava)
        {
            if ((Toolchain == Cpp.ToolchainType.Gradle_Ninja) || (Toolchain == Cpp.ToolchainType.Ninja))
            {
                if (HostOperatingSystem == Cpp.OperatingSystemType.Windows)
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
                    Lines.Add(Shell.EscapeArgumentForShell(Ninja.RelativeTo(BuildDirectory).ToString(PathStringStyle.Windows), Shell.ShellArgumentStyle.CMD) + $" -j{MaxProcessCount} -C projects -f build.ninja || exit /b 1");
                    if (EnableJava)
                    {
                        if (Toolchain == Cpp.ToolchainType.Gradle_Ninja)
                        {
                            Lines.Add("pushd gradle || exit /b 1");
                            Lines.Add($@"call .\gradlew.bat --no-daemon assemble{Configuration} || exit /b 1");
                            Lines.Add("popd");
                            Lines.Add("echo To debug a APK, open gradle directory in Android Studio, select Run - Edit Configurations - Debugger - Debug type, and change its value to Dual");
                        }
                        else
                        {
                            foreach (var GradleProjectName in GradleProjectNames)
                            {
                                Lines.Add($"pushd batch\\{Shell.EscapeArgumentForShell(GradleProjectName.Split(':').First(), Shell.ShellArgumentStyle.CMD)} || exit /b 1");
                                Lines.Add("call build.cmd || exit /b 1");
                                Lines.Add("popd");
                            }
                            Lines.Add("echo To debug a APK, open Android Studio, select Profile or debug APK and select the generated APK, close Android Studio and copy the APK debug project under UserDirectory/ApkProjects to where the APK located(ensure the overwrite of APK), open the new project in Android Studio, add debug symbols for .so files, attach sources for Java files, select Run - Edit Configurations - Debugger - Debug type, and change its value to Dual");
                        }
                    }
                    Lines.Add("");
                    var BuildPath = BuildDirectory / "build.cmd";
                    TextFile.WriteToFile(BuildPath, String.Join("\r\n", Lines), System.Text.Encoding.Default, !ForceRegenerate);
                }
                else
                {
                    var Lines = new List<String>();
                    Lines.Add("#!/bin/bash");
                    Lines.Add("set -e");
                    Lines.Add(Shell.EscapeArgumentForShell(Ninja.RelativeTo(BuildDirectory).ToString(PathStringStyle.Unix), Shell.ShellArgumentStyle.Bash) + $" -j{MaxProcessCount} -C projects -f build.ninja");
                    if (EnableJava)
                    {
                        if (Toolchain == Cpp.ToolchainType.Gradle_Ninja)
                        {
                            Lines.Add("pushd gradle");
                            Lines.Add($@"./gradlew --no-daemon assemble{Configuration}");
                            Lines.Add("popd");
                            Lines.Add("echo To debug a APK, open gradle directory in Android Studio, select Run - Edit Configurations - Debugger - Debug type, and change its value to Dual");
                        }
                        else
                        {
                            foreach (var GradleProjectName in GradleProjectNames)
                            {
                                Lines.Add($"pushd batch/{Shell.EscapeArgumentForShell(GradleProjectName.Split(':').First(), Shell.ShellArgumentStyle.Bash)}");
                                Lines.Add("./build.sh");
                                Lines.Add("popd");
                            }
                            Lines.Add(@"echo To debug a APK, open Android Studio, select Profile or debug APK and select the generated APK, close Android Studio and copy the APK debug project under UserDirectory/ApkProjects to where the APK located\(ensure the overwrite of APK\), open the new project in Android Studio, add debug symbols for .so files, attach sources for Java files, select Run - Edit Configurations - Debugger - Debug type, and change its value to Dual");
                        }
                    }
                    Lines.Add("");
                    var BuildPath = BuildDirectory / "build.sh";
                    TextFile.WriteToFile(BuildPath, String.Join("\n", Lines), new System.Text.UTF8Encoding(false), !ForceRegenerate);
                    if (Shell.Execute("chmod", "+x", BuildPath) != 0)
                    {
                        throw new InvalidOperationException("ErrorInExecution: chmod");
                    }
                }
            }
        }

        private static void WriteLineError(String Line)
        {
            Shell.Terminal.WriteLineError(ConsoleColor.Red, Line);
        }
    }
}
