﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace TypeMake
{
    public static class Generation
    {
        public static void Run(Shell.EnvironmentVariableMemory Memory, bool Quiet, Variables v)
        {
            BuildScript.GenerateRetypemakeScript(v.HostOperatingSystem, v.SourceDirectory, v.BuildDirectory, Memory, v.OverwriteRetypemakeScript);
            var r = v.g();

            var CppSortedProjectNames = r.SortedProjects.Where(p => (p.TargetType == Cpp.TargetType.Executable) || (p.TargetType == Cpp.TargetType.StaticLibrary) || (p.TargetType == Cpp.TargetType.DynamicLibrary) || (p.TargetType == Cpp.TargetType.DarwinApplication) || (p.TargetType == Cpp.TargetType.DarwinStaticFramework) || (p.TargetType == Cpp.TargetType.DarwinSharedFramework) || (p.TargetType == Cpp.TargetType.MacBundle)).Select(p => p.Name).ToList();
            var GradleProjectNames = r.SortedProjects.Where(p => (p.TargetType == Cpp.TargetType.GradleApplication) || (p.TargetType == Cpp.TargetType.GradleLibrary)).Select(p => p.Name).ToList();

            if (v.TargetOperatingSystem == Cpp.OperatingSystemType.Windows)
            {
                BuildScript.GenerateBuildScriptWindows(v.Toolchain, v.BuildDirectory, r.SolutionName, v.TargetArchitecture, Cpp.ConfigurationType.Debug, v.VSDir, v.VSVersion, v.Ninja, v.ForceRegenerate);
                BuildScript.GenerateBuildScriptWindows(v.Toolchain, v.BuildDirectory, r.SolutionName, v.TargetArchitecture, Cpp.ConfigurationType.Release, v.VSDir, v.VSVersion, v.Ninja, v.ForceRegenerate);
                if (v.BuildNow)
                {
                    if (v.HostOperatingSystem == Cpp.OperatingSystemType.Windows)
                    {
                        using (var d = Shell.PushDirectory(v.BuildDirectory))
                        {
                            if (Shell.Execute($"build_{v.Configuration}.cmd") != 0)
                            {
                                throw new InvalidOperationException("ErrorInExecution: " + $"build_{v.Configuration}.cmd");
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
                BuildScript.GenerateBuildScriptLinux(v.TargetOperatingSystemDistribution, v.Toolchain, v.HostOperatingSystem, v.BuildDirectory, v.Configuration, v.CMake, v.Make, v.Ninja, v.ForceRegenerate, r.NeedInstallStrip);
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
            else if (v.TargetOperatingSystem == Cpp.OperatingSystemType.MacOS)
            {
                if (v.Toolchain == Cpp.ToolchainType.XCode)
                {
                    BuildScript.GenerateBuildScriptXCode(v.HostOperatingSystem, v.BuildDirectory, v.Configuration, CppSortedProjectNames, v.ForceRegenerate);
                }
                else
                {
                    BuildScript.GenerateBuildScriptLinux("Mac", v.Toolchain, v.HostOperatingSystem, v.BuildDirectory, v.Configuration, v.CMake, v.Make, v.Ninja, v.ForceRegenerate, r.NeedInstallStrip);
                }
                if (v.BuildNow)
                {
                    using (var d = Shell.PushDirectory(v.BuildDirectory))
                    {
                        if (v.HostOperatingSystem == Cpp.OperatingSystemType.MacOS)
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
                BuildScript.GenerateBuildScriptXCode(v.HostOperatingSystem, v.BuildDirectory, v.Configuration, CppSortedProjectNames, v.ForceRegenerate);
                if (v.BuildNow)
                {
                    using (var d = Shell.PushDirectory(v.BuildDirectory))
                    {
                        if (v.HostOperatingSystem == Cpp.OperatingSystemType.MacOS)
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
                BuildScript.GenerateBuildScriptAndroid(GradleProjectNames, v.Toolchain, v.HostOperatingSystem, v.BuildDirectory, v.TargetArchitecture, v.Configuration, v.AndroidNdk, v.CMake, v.Make, v.Ninja, 17, v.ForceRegenerate, v.EnableJava, r.NeedInstallStrip);
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
                        else if ((v.HostOperatingSystem == Cpp.OperatingSystemType.Linux) || (v.HostOperatingSystem == Cpp.OperatingSystemType.MacOS) || (v.HostOperatingSystem == Cpp.OperatingSystemType.Android))
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

        private static void WriteLineError(String Line)
        {
            Shell.Terminal.WriteLineError(ConsoleColor.Red, Line);
        }
    }
}
