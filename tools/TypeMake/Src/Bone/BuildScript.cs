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
        public static void GenerateBuildScriptWindows(Cpp.ToolchainType Toolchain, PathString BuildDirectory, String SolutionName, Cpp.ArchitectureType TargetArchitecture, Cpp.ConfigurationType Configuration, PathString VSDir, int VSVersion, PathString Ninja, bool ForceRegenerate)
        {
            if (Toolchain == Cpp.ToolchainType.VisualStudio)
            {
                var MSBuildVersion = VSVersion == 2019 ? "Current" : "15.0";
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
                Lines.Add($@"""{VSDir.ToString(PathStringStyle.Windows)}\MSBuild\{MSBuildVersion}\Bin\MSBuild.exe"" {SolutionName}.sln /p:Configuration={Configuration} /p:Platform={SlnGenerator.GetArchitectureString(TargetArchitecture)} /m:{Environment.ProcessorCount.ToString()} || exit /b 1");
                Lines.Add("");
                var BuildPath = BuildDirectory / $"build_{TargetArchitecture}_{Configuration}.cmd";
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
                Lines.Add(Shell.EscapeArgumentForShell(Ninja.RelativeTo(BuildDirectory).ToString(PathStringStyle.Windows), Shell.ShellArgumentStyle.CMD) + " -j" + Environment.ProcessorCount.ToString() + " -C projects -f build.ninja || exit /b 1");
                Lines.Add("");
                var BuildPath = BuildDirectory / "build.cmd";
                TextFile.WriteToFile(BuildPath, String.Join("\r\n", Lines), System.Text.Encoding.Default, !ForceRegenerate);
            }
        }
        public static void GenerateBuildScriptLinux(String TargetOperatingSystemDistribution, Cpp.ToolchainType Toolchain, Cpp.OperatingSystemType HostOperatingSystem, PathString BuildDirectory, Cpp.ConfigurationType Configuration, PathString CMake, PathString Make, PathString Ninja, bool ForceRegenerate, bool NeedInstallStrip)
        {
            if (Toolchain == Cpp.ToolchainType.CMake)
            {
                var CMakeArguments = new List<String>();
                CMakeArguments.Add(".");
                CMakeArguments.Add($"-DCMAKE_BUILD_TYPE={Configuration}");

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
                    Lines.Add("wsl -d " + Shell.EscapeArgumentForShell(TargetOperatingSystemDistribution, Shell.ShellArgumentStyle.CMD) + " " + Shell.EscapeArgumentForShell(CMake, Shell.ShellArgumentStyle.CMD) + " " + String.Join(" ", CMakeArguments.Select(a => Shell.EscapeArgumentForShell(a, Shell.ShellArgumentStyle.CMD))) + " || exit /b 1");
                    Lines.Add("wsl -d " + Shell.EscapeArgumentForShell(TargetOperatingSystemDistribution, Shell.ShellArgumentStyle.CMD) + " " + Shell.EscapeArgumentForShell(Make, Shell.ShellArgumentStyle.CMD) + (NeedInstallStrip ? " install/strip" : "") + " -j" + Environment.ProcessorCount.ToString() + " || exit /b 1");
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
                    Lines.Add(Shell.EscapeArgumentForShell(Make, Shell.ShellArgumentStyle.Bash) + (NeedInstallStrip ? " install/strip" : "") + " -j" + Environment.ProcessorCount.ToString());
                    Lines.Add("");
                    var BuildPath = BuildDirectory / "build.sh";
                    TextFile.WriteToFile(BuildPath, String.Join("\n", Lines), new System.Text.UTF8Encoding(false), !ForceRegenerate);
                    if (Shell.Execute("chmod", "+x", BuildPath) != 0)
                    {
                        throw new InvalidOperationException("ErrorInExecution: chmod");
                    }
                }
            }
            else if (Toolchain == Cpp.ToolchainType.Ninja)
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
                    Lines.Add("wsl -d " + Shell.EscapeArgumentForShell(TargetOperatingSystemDistribution, Shell.ShellArgumentStyle.CMD) + " " + Shell.EscapeArgumentForShell(Ninja.RelativeTo(BuildDirectory).ToString(PathStringStyle.Unix), Shell.ShellArgumentStyle.CMD) + " -j" + Environment.ProcessorCount.ToString() + " -C projects -f build.ninja || exit /b 1");
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
                    if (Shell.Execute("chmod", "+x", BuildPath) != 0)
                    {
                        throw new InvalidOperationException("ErrorInExecution: chmod");
                    }
                }
            }
        }
        public static void GenerateBuildScriptXCode(Cpp.OperatingSystemType HostOperatingSystem, PathString BuildDirectory, Cpp.ConfigurationType Configuration, List<ProjectReference> SortedProjects, bool ForceRegenerate)
        {
            var Lines = new List<String>();
            Lines.Add("#!/bin/bash");
            Lines.Add("set -e");
            foreach (var p in SortedProjects)
            {
                Lines.Add($"xcodebuild -project projects/{p.Name}.xcodeproj -configuration {Configuration}");
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
        public static void GenerateBuildScriptAndroid(List<ProjectReference> GradleProjects, Cpp.ToolchainType Toolchain, Cpp.OperatingSystemType HostOperatingSystem, PathString BuildDirectory, Cpp.ArchitectureType TargetArchitecture, Cpp.ConfigurationType Configuration, PathString AndroidNdk, PathString CMake, PathString Make, PathString Ninja, int ApiLevel, bool ForceRegenerate, bool EnableJava, bool NeedInstallStrip)
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
                CMakeArguments.Add($"-DANDROID_PLATFORM=android-{ApiLevel}");
                CMakeArguments.Add($"-DANDROID_ABI={Cpp.GradleProjectGenerator.GetArchitectureString(TargetArchitecture)}");
                if (TargetArchitecture == Cpp.ArchitectureType.armv7a)
                {
                    CMakeArguments.Add($"-DANDROID_ARM_NEON=ON");
                }

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
                    Lines.Add(Shell.EscapeArgumentForShell(CMake, Shell.ShellArgumentStyle.CMD) + " " + String.Join(" ", CMakeArguments.Select(a => Shell.EscapeArgumentForShell(a, Shell.ShellArgumentStyle.CMD))) + " || exit /b 1");
                    if (NeedInstallStrip)
                    {
                        //https://gitlab.kitware.com/cmake/cmake/issues/16859
                        Lines.Add("wsl find projects -name cmake_install.cmake ^| xargs sed -i -e 's:$ENV{DESTDIR}/::g'");
                    }
                    Lines.Add(Shell.EscapeArgumentForShell(Make, Shell.ShellArgumentStyle.CMD) + (NeedInstallStrip ? " install/strip" : "") + " -j" + Environment.ProcessorCount.ToString() + " || exit /b 1");
                    if (EnableJava)
                    {
                        Lines.Add("pushd gradle || exit /b 1");
                        Lines.Add($@"call .\gradlew.bat --no-daemon assemble{Configuration} || exit /b 1");
                        Lines.Add("popd");
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
                    Lines.Add(Shell.EscapeArgumentForShell(CMake, Shell.ShellArgumentStyle.Bash) + " " + String.Join(" ", CMakeArguments.Select(a => Shell.EscapeArgumentForShell(a, Shell.ShellArgumentStyle.Bash))));
                    Lines.Add(Shell.EscapeArgumentForShell(Make, Shell.ShellArgumentStyle.Bash) + (NeedInstallStrip ? " install/strip" : "") + " -j" + Environment.ProcessorCount.ToString());
                    if (EnableJava)
                    {
                        Lines.Add("pushd gradle");
                        Lines.Add($@"./gradlew --no-daemon assemble{Configuration}");
                        Lines.Add("popd");
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
            else if ((Toolchain == Cpp.ToolchainType.Gradle_Ninja) || (Toolchain == Cpp.ToolchainType.Ninja))
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
                    Lines.Add(Shell.EscapeArgumentForShell(Ninja.RelativeTo(BuildDirectory).ToString(PathStringStyle.Windows), Shell.ShellArgumentStyle.CMD) + " -j" + Environment.ProcessorCount.ToString() + " -C projects -f build.ninja || exit /b 1");
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
                            foreach (var p in GradleProjects)
                            {
                                Lines.Add($"pushd batch\\{Shell.EscapeArgumentForShell(p.Name.Split(':').First(), Shell.ShellArgumentStyle.CMD)} || exit /b 1");
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
                    Lines.Add(Shell.EscapeArgumentForShell(Ninja.RelativeTo(BuildDirectory).ToString(PathStringStyle.Unix), Shell.ShellArgumentStyle.Bash) + " -j" + Environment.ProcessorCount.ToString() + " -C projects -f build.ninja");
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
                            foreach (var p in GradleProjects)
                            {
                                Lines.Add($"pushd batch/{Shell.EscapeArgumentForShell(p.Name.Split(':').First(), Shell.ShellArgumentStyle.Bash)}");
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
            Shell.SetForegroundColor(ConsoleColor.Red);
            Console.Error.WriteLine(Line);
            Shell.ResetColor();
        }
    }
}
