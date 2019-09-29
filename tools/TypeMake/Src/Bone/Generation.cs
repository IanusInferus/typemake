using System;
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

            if (v.TargetOperatingSystem == Cpp.OperatingSystemType.Windows)
            {
                BuildScript.GenerateBuildScriptWindows(v.Toolchain, v.BuildDirectory, r.SolutionName, Cpp.ArchitectureType.x86, Cpp.ConfigurationType.Debug, v.VSDir, v.VSVersion, v.Ninja, v.ForceRegenerate);
                BuildScript.GenerateBuildScriptWindows(v.Toolchain, v.BuildDirectory, r.SolutionName, Cpp.ArchitectureType.x86, Cpp.ConfigurationType.Release, v.VSDir, v.VSVersion, v.Ninja, v.ForceRegenerate);
                BuildScript.GenerateBuildScriptWindows(v.Toolchain, v.BuildDirectory, r.SolutionName, Cpp.ArchitectureType.x64, Cpp.ConfigurationType.Debug, v.VSDir, v.VSVersion, v.Ninja, v.ForceRegenerate);
                BuildScript.GenerateBuildScriptWindows(v.Toolchain, v.BuildDirectory, r.SolutionName, Cpp.ArchitectureType.x64, Cpp.ConfigurationType.Release, v.VSDir, v.VSVersion, v.Ninja, v.ForceRegenerate);
                BuildScript.GenerateBuildScriptWindows(v.Toolchain, v.BuildDirectory, r.SolutionName, Cpp.ArchitectureType.armv7a, Cpp.ConfigurationType.Debug, v.VSDir, v.VSVersion, v.Ninja, v.ForceRegenerate);
                BuildScript.GenerateBuildScriptWindows(v.Toolchain, v.BuildDirectory, r.SolutionName, Cpp.ArchitectureType.armv7a, Cpp.ConfigurationType.Release, v.VSDir, v.VSVersion, v.Ninja, v.ForceRegenerate);
                BuildScript.GenerateBuildScriptWindows(v.Toolchain, v.BuildDirectory, r.SolutionName, Cpp.ArchitectureType.arm64, Cpp.ConfigurationType.Debug, v.VSDir, v.VSVersion, v.Ninja, v.ForceRegenerate);
                BuildScript.GenerateBuildScriptWindows(v.Toolchain, v.BuildDirectory, r.SolutionName, Cpp.ArchitectureType.arm64, Cpp.ConfigurationType.Release, v.VSDir, v.VSVersion, v.Ninja, v.ForceRegenerate);
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
                BuildScript.GenerateBuildScriptLinux(v.TargetOperatingSystemDistribution, v.Toolchain, v.HostOperatingSystem, v.BuildDirectory, v.Configuration.Value, v.CMake, v.Make, v.Ninja, v.ForceRegenerate, r.NeedInstallStrip);
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
                if (v.Toolchain == Cpp.ToolchainType.XCode)
                {
                    BuildScript.GenerateBuildScriptXCode(v.HostOperatingSystem, v.BuildDirectory, r.SortedProjects, v.ForceRegenerate);
                }
                else
                {
                    BuildScript.GenerateBuildScriptLinux("Mac", v.Toolchain, v.HostOperatingSystem, v.BuildDirectory, v.Configuration.Value, v.CMake, v.Make, v.Ninja, v.ForceRegenerate, r.NeedInstallStrip);
                }
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
                BuildScript.GenerateBuildScriptXCode(v.HostOperatingSystem, v.BuildDirectory, r.SortedProjects, v.ForceRegenerate);
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
                BuildScript.GenerateBuildScriptAndroid(v.SelectedProjects.Values.Where(p => (p.Definition.TargetType == Cpp.TargetType.GradleApplication) || (p.Definition.TargetType == Cpp.TargetType.GradleLibrary)).Select(p => p.Reference).ToList(), v.Toolchain, v.HostOperatingSystem, v.BuildDirectory, v.TargetArchitecture.Value, v.Configuration.Value, v.AndroidNdk, v.CMake, v.Make, v.Ninja, 17, v.ForceRegenerate, v.EnableJava, r.NeedInstallStrip);
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

        private static void WriteLineError(String Line)
        {
            Shell.SetForegroundColor(ConsoleColor.Red);
            Console.Error.WriteLine(Line);
            Shell.ResetColor();
        }
    }
}
