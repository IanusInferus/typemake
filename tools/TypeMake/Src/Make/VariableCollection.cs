using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TypeMake
{
    public class Variables
    {
        public Cpp.OperatingSystemType HostOperatingSystem;
        public Cpp.ArchitectureType HostArchitecture;
        public Cpp.OperatingSystemType TargetOperatingSystem;
        public Cpp.ArchitectureType? TargetArchitecture;
        public Cpp.ToolchainType Toolchain;
        public Cpp.CompilerType Compiler;
        public Cpp.ConfigurationType? Configuration;
        public bool OverwriteRetypemakeScript;
        public bool ForceRegenerate;
        public bool EnableNonTargetingOperatingSystemDummy;
        public PathString SourceDirectory;
        public PathString BuildDirectory;
        public String DevelopmentTeam;
        public PathString VSDir;
        public PathString CMake;
        public PathString Make;
        public PathString Ninja;
        public PathString AndroidSdk;
        public PathString AndroidNdk;
        public String CC;
        public String CXX;
        public String AR;

        public Make m;
        public Dictionary<String, Make.ProjectDescription> SelectedProjects;

        public bool BuildAfterGenerate;
    }
    public static class VariableCollection
    {
        public static Variables Execute(Shell.EnvironmentVariableMemory Memory, bool Quiet)
        {
            var v = new Variables();
            var vc = new VariableCollector();

            vc.AddVariableFetch((Action OnInteraction) =>
            {
                if (Shell.OperatingSystem == Shell.OperatingSystemType.Windows)
                {
                    v.HostOperatingSystem = Cpp.OperatingSystemType.Windows;
                }
                else if (Shell.OperatingSystem == Shell.OperatingSystemType.Linux)
                {
                    v.HostOperatingSystem = Cpp.OperatingSystemType.Linux;
                }
                else if (Shell.OperatingSystem == Shell.OperatingSystemType.Mac)
                {
                    v.HostOperatingSystem = Cpp.OperatingSystemType.Mac;
                }
                else
                {
                    throw new InvalidOperationException("UnknownHostOperatingSystem");
                }
            });

            vc.AddVariableFetch((Action OnInteraction) =>
            {
                if (Shell.OperatingSystemArchitecture == Shell.OperatingSystemArchitectureType.x86_64)
                {
                    v.HostArchitecture = Cpp.ArchitectureType.x86_64;
                }
                else if (Shell.OperatingSystemArchitecture == Shell.OperatingSystemArchitectureType.x86)
                {
                    v.HostArchitecture = Cpp.ArchitectureType.x86;
                }
                else
                {
                    throw new InvalidOperationException("UnknownHostArchitecture");
                }
                //process architecture is supposed to be the same as the operating system architecture
            });

            vc.AddVariableFetch((Action OnInteraction) =>
            {
                v.TargetOperatingSystem = Shell.RequireEnvironmentVariableEnum<Cpp.OperatingSystemType>(Memory, "TargetOperatingSystem", Quiet, v.HostOperatingSystem, Options => Options.OnInteraction = OnInteraction);
            });

            vc.AddVariableFetch((Action OnInteraction) =>
            {
                if (v.TargetOperatingSystem == Cpp.OperatingSystemType.Windows)
                {
                    v.TargetArchitecture = Shell.RequireEnvironmentVariableEnum(Memory, "TargetArchitecture", Quiet, new HashSet<Cpp.ArchitectureType> { Cpp.ArchitectureType.x86, Cpp.ArchitectureType.x86_64, Cpp.ArchitectureType.armeabi_v7a, Cpp.ArchitectureType.arm64_v8a }, Cpp.ArchitectureType.x86_64, Options => Options.OnInteraction = OnInteraction);
                }
                else if (v.TargetOperatingSystem == Cpp.OperatingSystemType.Linux)
                {
                    v.TargetArchitecture = Cpp.ArchitectureType.x86_64;
                }
                else if (v.TargetOperatingSystem == Cpp.OperatingSystemType.Mac)
                {
                    v.TargetArchitecture = null;
                }
                else if (v.TargetOperatingSystem == Cpp.OperatingSystemType.iOS)
                {
                    v.TargetArchitecture = null;
                }
                else if (v.TargetOperatingSystem == Cpp.OperatingSystemType.Android)
                {
                    v.TargetArchitecture = Shell.RequireEnvironmentVariableEnum(Memory, "TargetArchitecture", Quiet, Cpp.ArchitectureType.armeabi_v7a, Options => Options.OnInteraction = OnInteraction);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            });

            vc.AddVariableFetch((Action OnInteraction) =>
            {
                if (v.TargetOperatingSystem == Cpp.OperatingSystemType.Windows)
                {
                    v.Toolchain = Cpp.ToolchainType.Windows_VisualC;
                }
                else if (v.TargetOperatingSystem == Cpp.OperatingSystemType.Linux)
                {
                    v.Toolchain = Shell.RequireEnvironmentVariableEnum(Memory, "Toolchain", Quiet, new HashSet<Cpp.ToolchainType> { Cpp.ToolchainType.Ninja, Cpp.ToolchainType.CMake }, Cpp.ToolchainType.Ninja, Options => Options.OnInteraction = OnInteraction);
                }
                else if (v.TargetOperatingSystem == Cpp.OperatingSystemType.Mac)
                {
                    v.Toolchain = Cpp.ToolchainType.Mac_XCode;
                }
                else if (v.TargetOperatingSystem == Cpp.OperatingSystemType.iOS)
                {
                    v.Toolchain = Cpp.ToolchainType.Mac_XCode;
                }
                else if (v.TargetOperatingSystem == Cpp.OperatingSystemType.Android)
                {
                    v.Toolchain = Shell.RequireEnvironmentVariableEnum(Memory, "Toolchain", Quiet, new HashSet<Cpp.ToolchainType> { Cpp.ToolchainType.Gradle_Ninja, Cpp.ToolchainType.Gradle_CMake }, Cpp.ToolchainType.Gradle_Ninja, Options => Options.OnInteraction = OnInteraction);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            });

            vc.AddVariableFetch((Action OnInteraction) =>
            {
                if (v.TargetOperatingSystem == Cpp.OperatingSystemType.Windows)
                {
                    v.Compiler = Cpp.CompilerType.VisualC;
                }
                else if (v.TargetOperatingSystem == Cpp.OperatingSystemType.Linux)
                {
                    v.Compiler = Cpp.CompilerType.gcc;
                }
                else if (v.TargetOperatingSystem == Cpp.OperatingSystemType.Mac)
                {
                    v.Compiler = Cpp.CompilerType.clang;
                }
                else if (v.TargetOperatingSystem == Cpp.OperatingSystemType.iOS)
                {
                    v.Compiler = Cpp.CompilerType.clang;
                }
                else if (v.TargetOperatingSystem == Cpp.OperatingSystemType.Android)
                {
                    v.Compiler = Cpp.CompilerType.clang;
                }
                else
                {
                    throw new InvalidOperationException();
                }
            });

            vc.AddVariableFetch((Action OnInteraction) =>
            {
                if (v.TargetOperatingSystem == Cpp.OperatingSystemType.Windows)
                {
                    v.Configuration = Shell.RequireEnvironmentVariableEnum(Memory, "Configuration", Quiet, Cpp.ConfigurationType.Debug, Options => Options.OnInteraction = OnInteraction);
                }
                else if (v.TargetOperatingSystem == Cpp.OperatingSystemType.Linux)
                {
                    v.Configuration = Shell.RequireEnvironmentVariableEnum(Memory, "Configuration", Quiet, Cpp.ConfigurationType.Debug, Options => Options.OnInteraction = OnInteraction);
                }
                else if (v.TargetOperatingSystem == Cpp.OperatingSystemType.Mac)
                {
                    v.Configuration = null;
                }
                else if (v.TargetOperatingSystem == Cpp.OperatingSystemType.iOS)
                {
                    v.Configuration = null;
                }
                else if (v.TargetOperatingSystem == Cpp.OperatingSystemType.Android)
                {
                    v.Configuration = Shell.RequireEnvironmentVariableEnum(Memory, "Configuration", Quiet, Cpp.ConfigurationType.Debug, Options => Options.OnInteraction = OnInteraction);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            });

            vc.AddVariableFetch((Action OnInteraction) =>
            {
                v.SourceDirectory = Shell.RequireEnvironmentVariableDirectoryPath(Memory, "SourceDirectory", Quiet, null, null, Options => Options.OnInteraction = OnInteraction);
            });

            vc.AddVariableFetch((Action OnInteraction) =>
            {
                String DefaultBuildDir = null;
                if (v.TargetOperatingSystem == Cpp.OperatingSystemType.Windows)
                {
                    DefaultBuildDir = "build/windows";
                }
                else if (v.TargetOperatingSystem == Cpp.OperatingSystemType.Linux)
                {
                    DefaultBuildDir = $"build/linux_{v.Toolchain}_{v.TargetArchitecture}_{v.Configuration}";
                }
                else if (v.TargetOperatingSystem == Cpp.OperatingSystemType.Mac)
                {
                    DefaultBuildDir = "build/mac";
                }
                else if (v.TargetOperatingSystem == Cpp.OperatingSystemType.iOS)
                {
                    DefaultBuildDir = "build/ios";
                }
                else if (v.TargetOperatingSystem == Cpp.OperatingSystemType.Android)
                {
                    DefaultBuildDir = $"build/android_{v.Toolchain.ToString().Replace("Gradle_", "")}_{v.TargetArchitecture}_{v.Configuration}";
                }
                else
                {
                    throw new InvalidOperationException();
                }
                v.BuildDirectory = Shell.RequireEnvironmentVariableDirectoryPath(Memory, "BuildDirectory", Quiet, DefaultBuildDir, p => !File.Exists(p) ? new KeyValuePair<bool, String>(true, "") : new KeyValuePair<bool, String>(false, "Exist as a file."), Options => Options.OnInteraction = OnInteraction);
            });

            vc.AddVariableFetch((Action OnInteraction) =>
            {
                v.OverwriteRetypemakeScript = Shell.RequireEnvironmentVariableBoolean(Memory, "OverwriteRetypemakeScript", Quiet, true, Options => Options.OnInteraction = OnInteraction);
            });

            vc.AddVariableFetch((Action OnInteraction) =>
            {
                v.ForceRegenerate = Shell.RequireEnvironmentVariableBoolean(Memory, "ForceRegenerate", Quiet, false, Options => Options.OnInteraction = OnInteraction);
            });

            vc.AddVariableFetch((Action OnInteraction) =>
            {
                v.EnableNonTargetingOperatingSystemDummy = Shell.RequireEnvironmentVariableBoolean(Memory, "EnableNonTargetingOperatingSystemDummy", Quiet, false, Options => Options.OnInteraction = OnInteraction);
            });

            vc.AddVariableFetch((Action OnInteraction) =>
            {
                if ((v.TargetOperatingSystem == Cpp.OperatingSystemType.Mac) || (v.TargetOperatingSystem == Cpp.OperatingSystemType.iOS))
                {
                    v.DevelopmentTeam = Shell.RequireEnvironmentVariable(Memory, "DevelopmentTeam", new Shell.EnvironmentVariableReadOptions { Quiet = Quiet, InputDisplay = "(optional, find by searching an existing pbxproj file with DEVELOPMENT_TEAM)", OnInteraction = OnInteraction });
                }
            });

            var PathValidator = v.BuildAfterGenerate ? null : (Func<PathString, KeyValuePair<bool, String>>)(p => new KeyValuePair<bool, String>(true, ""));

            vc.AddVariableFetch((Action OnInteraction) =>
            {
                if (v.TargetOperatingSystem == Cpp.OperatingSystemType.Windows)
                {
                    String DefaultVSDir = "";
                    if (v.HostOperatingSystem == Cpp.OperatingSystemType.Windows)
                    {
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
                    }
                    v.VSDir = Shell.RequireEnvironmentVariableDirectoryPath(Memory, "VSDir", Quiet, DefaultVSDir, PathValidator, Options => Options.OnInteraction = OnInteraction);
                }
            });

            vc.AddVariableFetch((Action OnInteraction) =>
            {
                if (v.TargetOperatingSystem == Cpp.OperatingSystemType.Android)
                {
                    v.AndroidSdk = Shell.RequireEnvironmentVariableDirectoryPath(Memory, "AndroidSdk", Quiet, v.HostOperatingSystem == Cpp.OperatingSystemType.Windows ? (Environment.GetEnvironmentVariable("LocalAppData").AsPath() / "Android/sdk").ToString() : "", PathValidator ?? (p => Directory.Exists(p / "platform-tools") ? new KeyValuePair<bool, String>(true, "") : new KeyValuePair<bool, String>(false, "No platform-tools directory inside.")), Options => Options.OnInteraction = OnInteraction);
                }
            });

            vc.AddVariableFetch((Action OnInteraction) =>
            {
                if (v.TargetOperatingSystem == Cpp.OperatingSystemType.Android)
                {
                    v.AndroidNdk = Shell.RequireEnvironmentVariableDirectoryPath(Memory, "AndroidNdk", Quiet, v.AndroidSdk / "ndk-bundle", PathValidator ?? (p => Directory.Exists(p / "build") ? new KeyValuePair<bool, String>(true, "") : new KeyValuePair<bool, String>(false, "No build directory inside.")), Options => Options.OnInteraction = OnInteraction);
                }
            });

            vc.AddVariableFetch((Action OnInteraction) =>
            {
                if (v.TargetOperatingSystem == Cpp.OperatingSystemType.Linux)
                {
                    if (v.Toolchain == Cpp.ToolchainType.CMake)
                    {
                        if (v.HostOperatingSystem == Cpp.OperatingSystemType.Windows)
                        {
                            v.CMake = Shell.RequireEnvironmentVariable(Memory, "CMake", new Shell.EnvironmentVariableReadOptions { Quiet = Quiet, DefaultValue = "cmake", OnInteraction = OnInteraction });
                        }
                        else if (v.HostOperatingSystem == Cpp.OperatingSystemType.Linux)
                        {
                            v.CMake = Shell.RequireEnvironmentVariableFilePath(Memory, "CMake", Quiet, Shell.TryLocate("cmake") ?? "", PathValidator, Options => Options.OnInteraction = OnInteraction);
                        }
                    }
                }
                else if (v.TargetOperatingSystem == Cpp.OperatingSystemType.Android)
                {
                    if (v.Toolchain == Cpp.ToolchainType.Gradle_CMake)
                    {
                        if (v.HostOperatingSystem == Cpp.OperatingSystemType.Windows)
                        {
                            v.CMake = Shell.RequireEnvironmentVariableFilePath(Memory, "CMake", Quiet, Environment.GetEnvironmentVariable("ProgramFiles").AsPath() / @"CMake\bin\cmake.exe", PathValidator, Options => Options.OnInteraction = OnInteraction);
                        }
                        else if (v.HostOperatingSystem == Cpp.OperatingSystemType.Linux)
                        {
                            v.CMake = Shell.RequireEnvironmentVariableFilePath(Memory, "CMake", Quiet, Shell.TryLocate("cmake") ?? "", PathValidator, Options => Options.OnInteraction = OnInteraction);
                        }
                    }
                }
            });

            vc.AddVariableFetch((Action OnInteraction) =>
            {
                if (v.TargetOperatingSystem == Cpp.OperatingSystemType.Linux)
                {
                    if (v.Toolchain == Cpp.ToolchainType.CMake)
                    {
                        if (v.HostOperatingSystem == Cpp.OperatingSystemType.Windows)
                        {
                            v.Make = Shell.RequireEnvironmentVariable(Memory, "Make", new Shell.EnvironmentVariableReadOptions { Quiet = Quiet, DefaultValue = "make", OnInteraction = OnInteraction });
                        }
                        else if (v.HostOperatingSystem == Cpp.OperatingSystemType.Linux)
                        {
                            v.Make = Shell.RequireEnvironmentVariableFilePath(Memory, "Make", Quiet, Shell.TryLocate("make") ?? "", PathValidator, Options => Options.OnInteraction = OnInteraction);
                        }
                    }
                }
                else if (v.TargetOperatingSystem == Cpp.OperatingSystemType.Android)
                {
                    if (v.Toolchain == Cpp.ToolchainType.Gradle_CMake)
                    {
                        String DefaultMake = null;
                        if (v.HostArchitecture == Cpp.ArchitectureType.x86_64)
                        {
                            if (v.HostOperatingSystem == Cpp.OperatingSystemType.Windows)
                            {
                                DefaultMake = v.AndroidNdk / @"prebuilt\windows-x86_64\bin\make.exe";
                            }
                            else if (v.HostOperatingSystem == Cpp.OperatingSystemType.Linux)
                            {
                                DefaultMake = v.AndroidNdk / "prebuilt/linux-x86_64/bin/make";
                            }
                            else if (v.HostOperatingSystem == Cpp.OperatingSystemType.Mac)
                            {
                                DefaultMake = v.AndroidNdk / "prebuilt/darwin-x86_64/bin/make";
                            }
                        }
                        v.Make = Shell.RequireEnvironmentVariableFilePath(Memory, "Make", Quiet, DefaultMake, null, Options => Options.OnInteraction = OnInteraction);
                    }
                }
            });

            vc.AddVariableFetch((Action OnInteraction) =>
            {
                if ((v.TargetOperatingSystem == Cpp.OperatingSystemType.Linux) || (v.TargetOperatingSystem == Cpp.OperatingSystemType.Android))
                {
                    if ((v.Toolchain == Cpp.ToolchainType.Ninja) || (v.Toolchain == Cpp.ToolchainType.Gradle_Ninja))
                    {
                        if (v.HostOperatingSystem == Cpp.OperatingSystemType.Windows)
                        {
                            if (v.TargetOperatingSystem == Cpp.OperatingSystemType.Linux)
                            {
                                v.Ninja = v.SourceDirectory / "tools/Ninja/ninja-linux/ninja";
                            }
                            else
                            {
                                v.Ninja = v.SourceDirectory / "tools/Ninja/ninja-win/ninja.exe";
                            }
                        }
                        else if (v.HostOperatingSystem == Cpp.OperatingSystemType.Linux)
                        {
                            v.Ninja = v.SourceDirectory / "tools/Ninja/ninja-linux/ninja";
                        }
                        else if (v.HostOperatingSystem == Cpp.OperatingSystemType.Mac)
                        {
                            v.Ninja = v.SourceDirectory / "tools/Ninja/ninja-mac/ninja";
                        }
                        else
                        {
                            throw new InvalidOperationException("HostOperatingSystemNotSupported");
                        }
                    }
                }
            });

            var Host = "";
            var ExeSuffix = "";
            var TargetPrefix = "";
            var ApiLevel = 17;
            var ToolchainPath = "".AsPath();

            vc.AddVariableFetch((Action OnInteraction) =>
            {
                if ((v.TargetOperatingSystem == Cpp.OperatingSystemType.Android) && (v.Toolchain == Cpp.ToolchainType.Gradle_Ninja))
                {
                    if (v.HostOperatingSystem == Cpp.OperatingSystemType.Windows)
                    {
                        Host = "windows-x86_64";
                        ExeSuffix = ".exe";
                    }
                    else if (v.HostOperatingSystem == Cpp.OperatingSystemType.Linux)
                    {
                        Host = "linux-x86_64";
                        ExeSuffix = "";
                    }
                    else if (v.HostOperatingSystem == Cpp.OperatingSystemType.Mac)
                    {
                        Host = "darwin-x86_64";
                        ExeSuffix = "";
                    }
                    else
                    {
                        throw new InvalidOperationException("HostOperatingSystemNotSupported");
                    }
                    if (v.TargetArchitecture == Cpp.ArchitectureType.x86)
                    {
                        TargetPrefix = "i686";
                        ApiLevel = 21;
                    }
                    else if (v.TargetArchitecture == Cpp.ArchitectureType.x86_64)
                    {
                        TargetPrefix = "x86_64";
                        ApiLevel = 21;
                    }
                    else if (v.TargetArchitecture == Cpp.ArchitectureType.armeabi_v7a)
                    {
                        TargetPrefix = "armv7a";
                        ApiLevel = 17;
                    }
                    else if (v.TargetArchitecture == Cpp.ArchitectureType.arm64_v8a)
                    {
                        TargetPrefix = "aarch64";
                        ApiLevel = 21;
                    }
                    ToolchainPath = v.AndroidNdk / $"toolchains/llvm/prebuilt/{Host}";
                }

                //https://developer.android.com/ndk/guides/standalone_toolchain
                //https://android.googlesource.com/platform/ndk/+/ndk-release-r19/docs/BuildSystemMaintainers.md
            });

            vc.AddVariableFetch((Action OnInteraction) =>
            {
                if ((v.Toolchain == Cpp.ToolchainType.Ninja) || (v.Toolchain == Cpp.ToolchainType.Gradle_Ninja))
                {
                    if (v.TargetOperatingSystem == Cpp.OperatingSystemType.Linux)
                    {
                        v.CC = Shell.RequireEnvironmentVariable(Memory, "CC", new Shell.EnvironmentVariableReadOptions { Quiet = Quiet, DefaultValue = "gcc", OnInteraction = OnInteraction });
                    }
                    else if (v.TargetOperatingSystem == Cpp.OperatingSystemType.Android)
                    {
                        v.CC = $"{ToolchainPath / "bin/clang"}{ExeSuffix} --target={TargetPrefix}-linux-androideabi{ApiLevel} --sysroot={ToolchainPath / "sysroot"}";
                    }
                }
            });

            vc.AddVariableFetch((Action OnInteraction) =>
            {
                if ((v.Toolchain == Cpp.ToolchainType.Ninja) || (v.Toolchain == Cpp.ToolchainType.Gradle_Ninja))
                {
                    if (v.TargetOperatingSystem == Cpp.OperatingSystemType.Linux)
                    {
                        v.CXX = Shell.RequireEnvironmentVariable(Memory, "CXX", new Shell.EnvironmentVariableReadOptions { Quiet = Quiet, DefaultValue = "g++", OnInteraction = OnInteraction });
                    }
                    else if (v.TargetOperatingSystem == Cpp.OperatingSystemType.Android)
                    {
                        v.CXX = $"{ToolchainPath / "bin/clang++"}{ExeSuffix} --target={TargetPrefix}-linux-androideabi{ApiLevel} --sysroot={ToolchainPath / "sysroot"}";
                    }
                }
            });

            vc.AddVariableFetch((Action OnInteraction) =>
            {
                if ((v.Toolchain == Cpp.ToolchainType.Ninja) || (v.Toolchain == Cpp.ToolchainType.Gradle_Ninja))
                {
                    if (v.TargetOperatingSystem == Cpp.OperatingSystemType.Linux)
                    {
                        v.AR = Shell.RequireEnvironmentVariable(Memory, "AR", new Shell.EnvironmentVariableReadOptions { Quiet = Quiet, DefaultValue = "ar", OnInteraction = OnInteraction });
                    }
                    else if (v.TargetOperatingSystem == Cpp.OperatingSystemType.Android)
                    {
                        v.AR = $"{ToolchainPath / "bin/llvm-ar"}{ExeSuffix}";
                    }
                }
            });

            vc.AddVariableFetch((Action OnInteraction) =>
            {
                var m = new Make(v.Toolchain, v.Compiler, v.HostOperatingSystem, v.HostArchitecture, v.TargetOperatingSystem, v.TargetArchitecture, v.Configuration, v.SourceDirectory, v.BuildDirectory, v.DevelopmentTeam, v.CC, v.CXX, v.AR, v.ForceRegenerate, v.EnableNonTargetingOperatingSystemDummy);
                v.m = m;
                var Projects = m.GetAvailableProjects();
                var ProjectSet = new HashSet<String>(Projects.Values.Select(t => t.Definition.Name));
                var SelectedProjectNames = Shell.RequireEnvironmentVariableMultipleSelection(Memory, "SelectedProjects", Quiet, ProjectSet, ProjectSet, Parts =>
                {
                    var Unresolved = m.CheckUnresolvedDependencies(Parts.ToDictionary(Name => Name, Name => Projects[Name]));
                    if (Unresolved.Count > 0)
                    {
                        return new KeyValuePair<bool, String>(false, "Unresolved dependencies: " + String.Join("; ", Unresolved.Select(p => p.Key + " -> " + String.Join(" ", p.Value))) + ".");
                    }
                    else
                    {
                        return new KeyValuePair<bool, String>(true, "");
                    }
                }, Options => Options.OnInteraction = OnInteraction);
                v.SelectedProjects = SelectedProjectNames.ToDictionary(Name => Name, Name => Projects[Name]);
            });

            vc.AddVariableFetch((Action OnInteraction) =>
            {
                v.BuildAfterGenerate = Shell.RequireEnvironmentVariableBoolean(Memory, "BuildAfterGenerate", Quiet, true, Options => { Options.ForegroundColor = ConsoleColor.Cyan; Options.OnInteraction = OnInteraction; });
            });

            vc.Execute();
            return v;
        }
    }
}
