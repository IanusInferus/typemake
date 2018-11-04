﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
            if (argv.Length != 0)
            {
                DisplayInfo();
                return 1;
            }
            var ForceRegenerate = options.ContainsKey("regen");
            var EnableNonTargetingOperatingSystemDummy = options.ContainsKey("dummy");
            var Quiet = options.ContainsKey("quiet");
            String SourceDirectory;
            String BuildDirectory;
            Shell.RequireEnvironmentVariable("SourceDirectory", out SourceDirectory, Quiet, p => Directory.Exists(p), p => Path.GetFullPath(p));

            Cpp.OperatingSystemType TargetOperatingSystem;
            Shell.RequireEnvironmentVariableEnum<Cpp.OperatingSystemType>("TargetOperatingSystem", out TargetOperatingSystem, Quiet, BuildingOperatingSystem);

            //TODO: automatic build after generation
            //TODO: create remake script for all targets, quiet by default

            if (TargetOperatingSystem == Cpp.OperatingSystemType.Windows)
            {
                Shell.RequireEnvironmentVariable("BuildDirectory", out BuildDirectory, Quiet, p => !File.Exists(p), p => Path.GetFullPath(p), "build/windows");
                var m = new Make(Cpp.ToolchainType.Windows_VisualC, Cpp.CompilerType.VisualC, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem, null, SourceDirectory, BuildDirectory, ForceRegenerate, EnableNonTargetingOperatingSystemDummy);
                m.Execute();
                Console.WriteLine("Generation successful.");
                return 0;
            }
            else if (TargetOperatingSystem == Cpp.OperatingSystemType.Linux)
            {
                Cpp.ConfigurationType Configuration;
                Shell.RequireEnvironmentVariableEnum("Configuration", out Configuration, Quiet, Cpp.ConfigurationType.Debug);
                Shell.RequireEnvironmentVariable("BuildDirectory", out BuildDirectory, Quiet, p => !File.Exists(p), p => Path.GetFullPath(p), $"build/linux_{Configuration}");
                var m = new Make(Cpp.ToolchainType.CMake, Cpp.CompilerType.gcc, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem, null, SourceDirectory, BuildDirectory, ForceRegenerate, EnableNonTargetingOperatingSystemDummy);
                m.Execute();
                if (BuildingOperatingSystem == Cpp.OperatingSystemType.Linux)
                {
                    String CMake;
                    Shell.RequireEnvironmentVariable("CMake", out CMake, Quiet, p => File.Exists(p), p => Path.GetFullPath(p), Shell.TryLocate("cmake") ?? (BuildingOperatingSystem == Cpp.OperatingSystemType.Windows ? Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles"), @"CMake\bin\cmake.exe") : ""));
                    using (var d = Shell.PushDirectory(BuildDirectory))
                    {
                        var Arguments = new List<String>();
                        Arguments.Add(BuildDirectory);
                        Arguments.Add($"-DCMAKE_BUILD_TYPE={Configuration}");
                        Shell.Execute(CMake, Arguments.ToArray());
                    }
                }
                Console.WriteLine("Generation successful.");
                return 0;
            }
            else if (TargetOperatingSystem == Cpp.OperatingSystemType.Mac)
            {
                Shell.RequireEnvironmentVariable("BuildDirectory", out BuildDirectory, Quiet, p => !File.Exists(p), p => Path.GetFullPath(p), "build/mac");
                var m = new Make(Cpp.ToolchainType.Mac_XCode, Cpp.CompilerType.clang, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem, null, SourceDirectory, BuildDirectory, ForceRegenerate, EnableNonTargetingOperatingSystemDummy);
                m.Execute();
                Console.WriteLine("Generation successful.");
                return 0;
            }
            else if (TargetOperatingSystem == Cpp.OperatingSystemType.iOS)
            {
                Shell.RequireEnvironmentVariable("BuildDirectory", out BuildDirectory, Quiet, p => !File.Exists(p), p => Path.GetFullPath(p), "build/ios");
                var m = new Make(Cpp.ToolchainType.Mac_XCode, Cpp.CompilerType.clang, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem, null, SourceDirectory, BuildDirectory, ForceRegenerate, EnableNonTargetingOperatingSystemDummy);
                m.Execute();
                Console.WriteLine("Generation successful.");
                return 0;
            }
            else if (TargetOperatingSystem == Cpp.OperatingSystemType.Android)
            {
                String AndroidSdk;
                Shell.RequireEnvironmentVariable("AndroidSdk", out AndroidSdk, Quiet, p => Directory.Exists(Path.Combine(p, "platform-tools")), p => Path.GetFullPath(p), BuildingOperatingSystem == Cpp.OperatingSystemType.Windows ? Path.Combine(Environment.GetEnvironmentVariable("LocalAppData"), @"Android\sdk") : "");
                String AndroidNdk;
                Shell.RequireEnvironmentVariable("AndroidNdk", out AndroidNdk, Quiet, p => Directory.Exists(Path.Combine(p, "build")), p => Path.GetFullPath(p), Path.Combine(AndroidSdk, "ndk-bundle"));
                String CMake;
                Shell.RequireEnvironmentVariable("CMake", out CMake, Quiet, p => File.Exists(p), p => Path.GetFullPath(p), Shell.TryLocate("cmake") ?? (BuildingOperatingSystem == Cpp.OperatingSystemType.Windows ? Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles"), @"CMake\bin\cmake.exe") : ""));
                Cpp.ArchitectureType TargetArchitecture;
                Shell.RequireEnvironmentVariableEnum("TargetArchitecture", out TargetArchitecture, Quiet, Cpp.ArchitectureType.armeabi_v7a);
                Cpp.ConfigurationType Configuration;
                Shell.RequireEnvironmentVariableEnum("Configuration", out Configuration, Quiet, Cpp.ConfigurationType.Debug);
                Shell.RequireEnvironmentVariable("BuildDirectory", out BuildDirectory, Quiet, p => !File.Exists(p), p => Path.GetFullPath(p), $"build/android_{TargetArchitecture}_{Configuration}");
                var m = new Make(Cpp.ToolchainType.Gradle_CMake, Cpp.CompilerType.clang, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem, TargetArchitecture, SourceDirectory, BuildDirectory, ForceRegenerate, EnableNonTargetingOperatingSystemDummy);
                m.Execute();
                using (var d = Shell.PushDirectory(BuildDirectory))
                {
                    var Arguments = new List<String>();
                    Arguments.Add(BuildDirectory.Replace("\\", "/"));
                    Arguments.Add("-G");
                    Arguments.Add("Unix Makefiles");
                    Arguments.Add($"-DCMAKE_BUILD_TYPE={Configuration}");
                    if (BuildingOperatingSystemArchitecture == Cpp.ArchitectureType.x86_64)
                    {
                        if (BuildingOperatingSystem == Cpp.OperatingSystemType.Windows)
                        {
                            Arguments.Add($"-DCMAKE_MAKE_PROGRAM={AndroidNdk.Replace("\\", "/")}/prebuilt/windows-x86_64/bin/make.exe");
                        }
                        else if (BuildingOperatingSystem == Cpp.OperatingSystemType.Linux)
                        {
                            Arguments.Add($"-DCMAKE_MAKE_PROGRAM={AndroidNdk}/prebuilt/linux-x86_64/bin/make");
                        }
                        else if (BuildingOperatingSystem == Cpp.OperatingSystemType.Mac)
                        {
                            Arguments.Add($"-DCMAKE_MAKE_PROGRAM={AndroidNdk}/prebuilt/darwin-x86_64/bin/make");
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
                    Arguments.Add($"-DANDROID_NDK={AndroidNdk.Replace("\\", "/")}");
                    Arguments.Add($"-DCMAKE_TOOLCHAIN_FILE={AndroidNdk.Replace("\\", "/")}/build/cmake/android.toolchain.cmake");
                    Arguments.Add($"-DANDROID_STL=c++_static");
                    Arguments.Add($"-DANDROID_PLATFORM=android-17");
                    Arguments.Add($"-DANDROID_ABI={Cpp.GradleProjectGenerator.GetArchitectureString(TargetArchitecture)}");
                    if (TargetArchitecture == Cpp.ArchitectureType.armeabi_v7a)
                    {
                        Arguments.Add($"-DANDROID_ARM_NEON=ON");
                    }
                    Shell.Execute(CMake, Arguments.ToArray());
                }
                Console.WriteLine("Generation successful.");
                return 0;
            }

            DisplayInfo();
            return 1;
        }

        public static void DisplayInfo()
        {
            Console.WriteLine(@"TypeMake");
            Console.WriteLine(@"Usage:");
            Console.WriteLine(@"TypeMake [--regen] [--dummy] [--quiet] [--help]");
            Console.WriteLine(@"--regen forcely regenerate project files");
            Console.WriteLine(@"--dummy generate dummy projects for non-targeting operating systems");
            Console.WriteLine(@"--quiet no interactive variable input, all variables must be input from environment variables");
        }
    }
}
