using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TypeMake
{
    public static class VariableCollection
    {
        public static KeyValuePair<Variables, List<VariableItem>> GetVariableItems()
        {
            var Variables = new Variables();
            var l = new List<VariableItem>();

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.HostOperatingSystem),
                DependentVariableNames = new List<String> { },
                GetVariableSpec = () =>
                {
                    if (Shell.OperatingSystem == Shell.OperatingSystemType.Windows)
                    {
                        return VariableSpec.CreateFixed(VariableValue.CreateString(Cpp.OperatingSystemType.Windows.ToString()));
                    }
                    else if (Shell.OperatingSystem == Shell.OperatingSystemType.Linux)
                    {
                        return VariableSpec.CreateFixed(VariableValue.CreateString(Cpp.OperatingSystemType.Linux.ToString()));
                    }
                    else if (Shell.OperatingSystem == Shell.OperatingSystemType.Mac)
                    {
                        return VariableSpec.CreateFixed(VariableValue.CreateString(Cpp.OperatingSystemType.Mac.ToString()));
                    }
                    else
                    {
                        throw new InvalidOperationException("UnknownHostOperatingSystem");
                    }
                },
                SetVariableValue = v => Variables.HostOperatingSystem = UnwrapEnum<Cpp.OperatingSystemType>(v.String)
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.HostArchitecture),
                DependentVariableNames = new List<String> { },
                GetVariableSpec = () =>
                {
                    if (Shell.OperatingSystemArchitecture == Shell.OperatingSystemArchitectureType.x86_64)
                    {
                        return VariableSpec.CreateFixed(VariableValue.CreateString(Cpp.ArchitectureType.x64.ToString()));
                    }
                    else if (Shell.OperatingSystemArchitecture == Shell.OperatingSystemArchitectureType.x86)
                    {
                        return VariableSpec.CreateFixed(VariableValue.CreateString(Cpp.ArchitectureType.x86.ToString()));
                    }
                    else
                    {
                        throw new InvalidOperationException("UnknownHostArchitecture");
                    }
                    //process architecture is supposed to be the same as the operating system architecture
                },
                SetVariableValue = v => Variables.HostArchitecture = UnwrapEnum<Cpp.ArchitectureType>(v.String)
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.TargetOperatingSystem),
                DependentVariableNames = new List<String> { },
                GetVariableSpec = () =>
                {
                    return VariableSpecCreateEnumSelection(Variables.HostOperatingSystem);
                },
                SetVariableValue = v => Variables.TargetOperatingSystem = UnwrapEnum<Cpp.OperatingSystemType>(v.String)
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.TargetOperatingSystemDistribution),
                DependentVariableNames = new List<String> { nameof(Variables.HostOperatingSystem), nameof(Variables.TargetOperatingSystem) },
                GetVariableSpec = () =>
                {
                    if ((Variables.HostOperatingSystem == Cpp.OperatingSystemType.Windows) && (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Linux))
                    {
                        var p = Shell.ExecuteAndGetOutput("wslconfig", System.Text.Encoding.Unicode, "/list");
                        if (p.Key != 0) { throw new InvalidOperationException("WslConfigFailed"); }
                        var Distributions = new HashSet<String>(p.Value.Replace("\r", "").Split('\n').Skip(1).Where(Line => Line.Trim(' ') != "").Select(Line => Line.Split(' ').First()).ToArray());
                        var DefaultDistribution = Distributions.Count > 0 ? Distributions.First() : "";
                        return VariableSpec.CreateSelection(new StringSelectionSpec
                        {
                            DefaultValue = DefaultDistribution,
                            Selections = Distributions
                        });
                    }
                    else
                    {
                        return VariableSpec.CreateNotApply(VariableValue.CreateString(null));
                    }
                },
                SetVariableValue = v => Variables.TargetOperatingSystemDistribution = v.String
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.TargetArchitecture),
                DependentVariableNames = new List<String> { nameof(Variables.TargetOperatingSystem) },
                GetVariableSpec = () =>
                {
                    if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Windows)
                    {
                        return VariableSpecCreateEnumSelection(Cpp.ArchitectureType.x64);
                    }
                    else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Linux)
                    {
                        return VariableSpecCreateEnumSelection(Cpp.ArchitectureType.x64);
                    }
                    else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Mac)
                    {
                        return VariableSpec.CreateNotApply(VariableValue.CreateString(null));
                    }
                    else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.iOS)
                    {
                        return VariableSpec.CreateNotApply(VariableValue.CreateString(null));
                    }
                    else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Android)
                    {
                        return VariableSpecCreateEnumSelection(Cpp.ArchitectureType.armv7a);
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                },
                SetVariableValue = v => Variables.TargetArchitecture = UnwrapNullableEnum<Cpp.ArchitectureType>(v.String)
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.Toolchain),
                DependentVariableNames = new List<String> { nameof(Variables.TargetOperatingSystem), nameof(Variables.TargetArchitecture) },
                GetVariableSpec = () =>
                {
                    if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Windows)
                    {
                        return VariableSpec.CreateFixed(VariableValue.CreateString(Cpp.ToolchainType.Windows_VisualC.ToString()));
                    }
                    else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Linux)
                    {
                        if ((Variables.TargetArchitecture == Cpp.ArchitectureType.x86) || (Variables.TargetArchitecture == Cpp.ArchitectureType.x64))
                        {
                            return VariableSpecCreateEnumSelection(Cpp.ToolchainType.Ninja, new HashSet<Cpp.ToolchainType> { Cpp.ToolchainType.Ninja, Cpp.ToolchainType.CMake });
                        }
                        else
                        {
                            return VariableSpec.CreateFixed(VariableValue.CreateString(Cpp.ToolchainType.Ninja.ToString()));
                        }
                    }
                    else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Mac)
                    {
                        return VariableSpec.CreateFixed(VariableValue.CreateString(Cpp.ToolchainType.Mac_XCode.ToString()));
                    }
                    else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.iOS)
                    {
                        return VariableSpec.CreateFixed(VariableValue.CreateString(Cpp.ToolchainType.Mac_XCode.ToString()));
                    }
                    else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Android)
                    {
                        return VariableSpecCreateEnumSelection(Cpp.ToolchainType.Ninja, new HashSet<Cpp.ToolchainType> { Cpp.ToolchainType.Ninja, Cpp.ToolchainType.Gradle_Ninja, Cpp.ToolchainType.Gradle_CMake });
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                },
                SetVariableValue = v => Variables.Toolchain = UnwrapEnum<Cpp.ToolchainType>(v.String)
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.Compiler),
                DependentVariableNames = new List<String> { nameof(Variables.TargetOperatingSystem) },
                GetVariableSpec = () =>
                {
                    if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Windows)
                    {
                        return VariableSpec.CreateFixed(VariableValue.CreateString(Cpp.CompilerType.VisualC.ToString()));
                    }
                    else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Linux)
                    {
                        return VariableSpecCreateEnumSelection(Cpp.CompilerType.gcc, new HashSet<Cpp.CompilerType> { Cpp.CompilerType.gcc, Cpp.CompilerType.clang });
                    }
                    else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Mac)
                    {
                        return VariableSpec.CreateFixed(VariableValue.CreateString(Cpp.CompilerType.clang.ToString()));
                    }
                    else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.iOS)
                    {
                        return VariableSpec.CreateFixed(VariableValue.CreateString(Cpp.CompilerType.clang.ToString()));
                    }
                    else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Android)
                    {
                        return VariableSpec.CreateFixed(VariableValue.CreateString(Cpp.CompilerType.clang.ToString()));
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                },
                SetVariableValue = v => Variables.Compiler = UnwrapEnum<Cpp.CompilerType>(v.String)
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.Configuration),
                DependentVariableNames = new List<String> { nameof(Variables.TargetOperatingSystem) },
                GetVariableSpec = () =>
                {
                    if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Windows)
                    {
                        return VariableSpecCreateEnumSelection(Cpp.ConfigurationType.Debug);
                    }
                    else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Linux)
                    {
                        return VariableSpecCreateEnumSelection(Cpp.ConfigurationType.Debug);
                    }
                    else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Mac)
                    {
                        return VariableSpec.CreateNotApply(VariableValue.CreateString(null));
                    }
                    else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.iOS)
                    {
                        return VariableSpec.CreateNotApply(VariableValue.CreateString(null));
                    }
                    else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Android)
                    {
                        return VariableSpecCreateEnumSelection(Cpp.ConfigurationType.Debug);
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                },
                SetVariableValue = v => Variables.Configuration = UnwrapNullableEnum<Cpp.ConfigurationType>(v.String)
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.SourceDirectory),
                DependentVariableNames = new List<String> { },
                GetVariableSpec = () =>
                {
                    return VariableSpec.CreatePath(new PathStringSpec
                    {
                        DefaultValue = null,
                        IsDirectory = true,
                        Validator = p => Directory.Exists(p / "modules") && Directory.Exists(p / "products") ? new KeyValuePair<bool, String>(true, "") : new KeyValuePair<bool, string>(false, "modules or products not exist.")
                    });
                },
                SetVariableValue = v => Variables.SourceDirectory = v.Path
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.BuildDirectory),
                DependentVariableNames = new List<String> { nameof(Variables.TargetOperatingSystem), nameof(Variables.Toolchain), nameof(Variables.Compiler), nameof(Variables.TargetArchitecture), nameof(Variables.Configuration), nameof(Variables.SourceDirectory) },
                GetVariableSpec = () =>
                {
                    String DefaultBuildDir = null;
                    if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Windows)
                    {
                        DefaultBuildDir = Variables.SourceDirectory / "build/windows";
                    }
                    else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Linux)
                    {
                        DefaultBuildDir = Variables.SourceDirectory / $"build/linux_{Variables.Toolchain}_{Variables.Compiler}_{Variables.TargetArchitecture}_{Variables.Configuration}";
                    }
                    else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Mac)
                    {
                        DefaultBuildDir = Variables.SourceDirectory / "build/mac";
                    }
                    else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.iOS)
                    {
                        DefaultBuildDir = Variables.SourceDirectory / "build/ios";
                    }
                    else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Android)
                    {
                        DefaultBuildDir = Variables.SourceDirectory / $"build/android_{Variables.Toolchain.ToString().Replace("Gradle_", "G")}_{Variables.TargetArchitecture}_{Variables.Configuration}";
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                    return VariableSpec.CreatePath(new PathStringSpec
                    {
                        DefaultValue = DefaultBuildDir,
                        IsDirectory = true,
                        Validator = p => !File.Exists(p) ? new KeyValuePair<bool, String>(true, "") : new KeyValuePair<bool, String>(false, "Exist as a file.")
                    });
                },
                SetVariableValue = v => Variables.BuildDirectory = v.Path
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.OverwriteRetypemakeScript),
                DependentVariableNames = new List<String> { },
                GetVariableSpec = () =>
                {
                    return VariableSpec.CreateBoolean(new BooleanSpec
                    {
                        DefaultValue = true
                    });
                },
                SetVariableValue = v => Variables.OverwriteRetypemakeScript = v.Boolean
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.ForceRegenerate),
                DependentVariableNames = new List<String> { },
                GetVariableSpec = () =>
                {
                    return VariableSpec.CreateBoolean(new BooleanSpec
                    {
                        DefaultValue = false
                    });
                },
                SetVariableValue = v => Variables.ForceRegenerate = v.Boolean
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.EnablePathCheck),
                DependentVariableNames = new List<String> { },
                GetVariableSpec = () =>
                {
                    return VariableSpec.CreateBoolean(new BooleanSpec
                    {
                        DefaultValue = true
                    });
                },
                SetVariableValue = v => Variables.EnablePathCheck = v.Boolean
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.EnableNonTargetingOperatingSystemDummy),
                DependentVariableNames = new List<String> { },
                GetVariableSpec = () =>
                {
                    return VariableSpec.CreateBoolean(new BooleanSpec
                    {
                        DefaultValue = false
                    });
                },
                SetVariableValue = v => Variables.EnableNonTargetingOperatingSystemDummy = v.Boolean
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.DevelopmentTeam),
                DependentVariableNames = new List<String> { nameof(Variables.TargetOperatingSystem) },
                GetVariableSpec = () =>
                {
                    if ((Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Mac) || (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.iOS))
                    {
                        return VariableSpec.CreateString(new StringSpec
                        {
                            InputDisplay = "(optional, find by searching an existing pbxproj file with DEVELOPMENT_TEAM)"
                        });
                    }
                    else
                    {
                        return VariableSpec.CreateNotApply(VariableValue.CreateString(null));
                    }
                },
                SetVariableValue = v => Variables.DevelopmentTeam = v.String
            });

            Func<PathString, KeyValuePair<bool, String>> PathValidator = null;
            l.Add(new VariableItem
            {
                VariableName = nameof(PathValidator),
                DependentVariableNames = new List<String> { nameof(Variables.EnablePathCheck) },
                IsHidden = true,
                GetVariableSpec = () =>
                {
                    PathValidator = Variables.EnablePathCheck ? null : (Func<PathString, KeyValuePair<bool, String>>)(p => new KeyValuePair<bool, String>(true, ""));
                    return VariableSpec.CreateNotApply(VariableValue.CreateBoolean(false));
                },
                SetVariableValue = v => { }
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.VSDir),
                DependentVariableNames = new List<String> { nameof(Variables.HostOperatingSystem), nameof(Variables.TargetOperatingSystem), nameof(PathValidator) },
                GetVariableSpec = () =>
                {
                    if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Windows)
                    {
                        String DefaultVSDir = "";
                        if (Variables.HostOperatingSystem == Cpp.OperatingSystemType.Windows)
                        {
                            var ProgramFiles = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
                            if (ProgramFiles != null)
                            {
                                foreach (var Version in new int[] { 2019, 2018 })
                                {
                                    foreach (var d in new String[] { "Enterprise", "Professional", "Community", "BuildTools" })
                                    {
                                        var p = ProgramFiles.AsPath() / $"Microsoft Visual Studio/{Version}" / d;
                                        if (Directory.Exists(p))
                                        {
                                            DefaultVSDir = p;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        return VariableSpec.CreatePath(new PathStringSpec
                        {
                            DefaultValue = DefaultVSDir,
                            IsDirectory = true,
                            Validator = PathValidator
                        });
                    }
                    else
                    {
                        return VariableSpec.CreateNotApply(VariableValue.CreatePath(null));
                    }
                },
                SetVariableValue = v => Variables.VSDir = v.Path
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.VSVersion),
                DependentVariableNames = new List<String> { nameof(Variables.TargetOperatingSystem), nameof(Variables.VSDir) },
                GetVariableSpec = () =>
                {
                    if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Windows)
                    {
                        var VSVersion = Variables.VSDir.ToString().Contains("2019") ? 2019 : 2017;
                        return VariableSpec.CreateFixed(VariableValue.CreateInteger(VSVersion));
                    }
                    else
                    {
                        return VariableSpec.CreateNotApply(VariableValue.CreatePath(null));
                    }
                },
                SetVariableValue = v => Variables.VSVersion = v.Integer
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.Jdk),
                DependentVariableNames = new List<String> { nameof(Variables.HostOperatingSystem), nameof(Variables.TargetOperatingSystem), nameof(PathValidator) },
                GetVariableSpec = () =>
                {
                    if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Android)
                    {
                        var DefaultJdk = Environment.GetEnvironmentVariable("JAVA_HOME").AsPath();
                        if ((Variables.HostOperatingSystem != Cpp.OperatingSystemType.Windows) && String.IsNullOrEmpty(DefaultJdk))
                        {
                            DefaultJdk = "/usr";
                        }
                        return VariableSpec.CreatePath(new PathStringSpec
                        {
                            DefaultValue = DefaultJdk,
                            IsDirectory = true,
                            Validator = PathValidator ?? (p => File.Exists(p / (Variables.HostOperatingSystem == Cpp.OperatingSystemType.Windows ? "bin/javac.exe" : "bin/javac")) ? new KeyValuePair<bool, String>(true, "") : new KeyValuePair<bool, String>(false, "No bin/javac inside."))
                        });
                    }
                    else
                    {
                        return VariableSpec.CreateNotApply(VariableValue.CreatePath(null));
                    }
                },
                SetVariableValue = v => Variables.Jdk = v.Path
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.AndroidSdk),
                DependentVariableNames = new List<String> { nameof(Variables.HostOperatingSystem), nameof(Variables.TargetOperatingSystem), nameof(PathValidator) },
                GetVariableSpec = () =>
                {
                    if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Android)
                    {
                        return VariableSpec.CreatePath(new PathStringSpec
                        {
                            DefaultValue = Variables.HostOperatingSystem == Cpp.OperatingSystemType.Windows ? (Environment.GetEnvironmentVariable("LocalAppData").AsPath() / "Android/sdk").ToString() : "",
                            IsDirectory = true,
                            Validator = PathValidator ?? (p => Directory.Exists(p / "tools") ? new KeyValuePair<bool, String>(true, "") : new KeyValuePair<bool, String>(false, "No tools directory inside."))
                        });
                    }
                    else
                    {
                        return VariableSpec.CreateNotApply(VariableValue.CreatePath(null));
                    }
                },
                SetVariableValue = v => Variables.AndroidSdk = v.Path
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.AndroidNdk),
                DependentVariableNames = new List<String> { nameof(Variables.TargetOperatingSystem), nameof(Variables.AndroidSdk), nameof(PathValidator) },
                GetVariableSpec = () =>
                {
                    if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Android)
                    {
                        return VariableSpec.CreatePath(new PathStringSpec
                        {
                            DefaultValue = Variables.AndroidSdk / "ndk-bundle",
                            IsDirectory = true,
                            Validator = PathValidator ?? (p => Directory.Exists(p / "build") ? new KeyValuePair<bool, String>(true, "") : new KeyValuePair<bool, String>(false, "No build directory inside."))
                        });
                    }
                    else
                    {
                        return VariableSpec.CreateNotApply(VariableValue.CreatePath(null));
                    }
                },
                SetVariableValue = v => Variables.AndroidNdk = v.Path
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.CMake),
                DependentVariableNames = new List<String> { nameof(Variables.HostOperatingSystem), nameof(Variables.TargetOperatingSystem), nameof(Variables.Toolchain), nameof(PathValidator) },
                GetVariableSpec = () =>
                {
                    if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Linux)
                    {
                        if (Variables.Toolchain == Cpp.ToolchainType.CMake)
                        {
                            if (Variables.HostOperatingSystem == Cpp.OperatingSystemType.Windows)
                            {
                                return VariableSpec.CreateString(new StringSpec
                                {
                                    DefaultValue = "cmake"
                                });
                            }
                            else if (Variables.HostOperatingSystem == Cpp.OperatingSystemType.Linux)
                            {
                                return VariableSpec.CreatePath(new PathStringSpec
                                {
                                    DefaultValue = Shell.TryLocate("cmake") ?? null,
                                    Validator = PathValidator
                                });
                            }
                        }
                    }
                    else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Android)
                    {
                        if (Variables.Toolchain == Cpp.ToolchainType.Gradle_CMake)
                        {
                            if (Variables.HostOperatingSystem == Cpp.OperatingSystemType.Windows)
                            {
                                return VariableSpec.CreatePath(new PathStringSpec
                                {
                                    DefaultValue = Environment.GetEnvironmentVariable("ProgramFiles").AsPath() / @"CMake\bin\cmake.exe",
                                    Validator = PathValidator
                                });
                            }
                            else if (Variables.HostOperatingSystem == Cpp.OperatingSystemType.Linux)
                            {
                                return VariableSpec.CreatePath(new PathStringSpec
                                {
                                    DefaultValue = Shell.TryLocate("cmake") ?? "",
                                    Validator = PathValidator
                                });
                            }
                        }
                    }
                    return VariableSpec.CreateNotApply(VariableValue.CreatePath(null));
                },
                SetVariableValue = v => Variables.CMake = v.OnPath ? v.Path : v.String.AsPath()
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.Make),
                DependentVariableNames = new List<String> { nameof(Variables.HostOperatingSystem), nameof(Variables.HostArchitecture), nameof(Variables.TargetOperatingSystem), nameof(Variables.Toolchain), nameof(Variables.AndroidNdk), nameof(PathValidator) },
                GetVariableSpec = () =>
                {
                    if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Linux)
                    {
                        if (Variables.Toolchain == Cpp.ToolchainType.CMake)
                        {
                            if (Variables.HostOperatingSystem == Cpp.OperatingSystemType.Windows)
                            {
                                return VariableSpec.CreateString(new StringSpec
                                {
                                    DefaultValue = "make"
                                });
                            }
                            else if (Variables.HostOperatingSystem == Cpp.OperatingSystemType.Linux)
                            {
                                return VariableSpec.CreatePath(new PathStringSpec
                                {
                                    DefaultValue = Shell.TryLocate("make") ?? null,
                                    Validator = PathValidator
                                });
                            }
                        }
                    }
                    else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Android)
                    {
                        if (Variables.Toolchain == Cpp.ToolchainType.Gradle_CMake)
                        {
                            String DefaultMake = null;
                            if (Variables.HostArchitecture == Cpp.ArchitectureType.x64)
                            {
                                if (Variables.HostOperatingSystem == Cpp.OperatingSystemType.Windows)
                                {
                                    DefaultMake = Variables.AndroidNdk / @"prebuilt\windows-x86_64\bin\make.exe";
                                }
                                else if (Variables.HostOperatingSystem == Cpp.OperatingSystemType.Linux)
                                {
                                    DefaultMake = Variables.AndroidNdk / "prebuilt/linux-x86_64/bin/make";
                                }
                                else if (Variables.HostOperatingSystem == Cpp.OperatingSystemType.Mac)
                                {
                                    DefaultMake = Variables.AndroidNdk / "prebuilt/darwin-x86_64/bin/make";
                                }
                            }
                            return VariableSpec.CreatePath(new PathStringSpec
                            {
                                DefaultValue = DefaultMake
                            });
                        }
                    }
                    return VariableSpec.CreateNotApply(VariableValue.CreatePath(null));
                },
                SetVariableValue = v => Variables.Make = v.OnPath ? v.Path : v.String.AsPath()
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.Ninja),
                DependentVariableNames = new List<String> { nameof(Variables.HostOperatingSystem), nameof(Variables.TargetOperatingSystem), nameof(Variables.Toolchain), nameof(Variables.SourceDirectory) },
                GetVariableSpec = () =>
                {
                    if ((Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Linux) || (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Android))
                    {
                        if ((Variables.Toolchain == Cpp.ToolchainType.Ninja) || (Variables.Toolchain == Cpp.ToolchainType.Gradle_Ninja))
                        {
                            if (Variables.HostOperatingSystem == Cpp.OperatingSystemType.Windows)
                            {
                                if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Linux)
                                {
                                    return VariableSpec.CreateFixed(VariableValue.CreatePath(Variables.SourceDirectory / "tools/Ninja/ninja-linux/ninja"));
                                }
                                else
                                {
                                    return VariableSpec.CreateFixed(VariableValue.CreatePath(Variables.SourceDirectory / "tools/Ninja/ninja-win/ninja.exe"));
                                }
                            }
                            else if (Variables.HostOperatingSystem == Cpp.OperatingSystemType.Linux)
                            {
                                return VariableSpec.CreateFixed(VariableValue.CreatePath(Variables.SourceDirectory / "tools/Ninja/ninja-linux/ninja"));
                            }
                            else if (Variables.HostOperatingSystem == Cpp.OperatingSystemType.Mac)
                            {
                                return VariableSpec.CreateFixed(VariableValue.CreatePath(Variables.SourceDirectory / "tools/Ninja/ninja-mac/ninja"));
                            }
                            else
                            {
                                throw new InvalidOperationException("HostOperatingSystemNotSupported");
                            }
                        }
                    }
                    return VariableSpec.CreateNotApply(VariableValue.CreatePath(null));
                },
                SetVariableValue = v => Variables.Ninja = v.Path
            });

            var Host = "";
            var ExeSuffix = "";
            var TargetPrefix = "";
            var ApiLevel = 17;
            var ToolchainPath = "".AsPath();

            l.Add(new VariableItem
            {
                VariableName = "AndroidVariables",
                DependentVariableNames = new List<String> { nameof(Variables.HostOperatingSystem), nameof(Variables.TargetOperatingSystem), nameof(Variables.TargetArchitecture), nameof(Variables.Toolchain), nameof(Variables.AndroidNdk) },
                IsHidden = true,
                GetVariableSpec = () =>
                {
                    if ((Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Android) && ((Variables.Toolchain == Cpp.ToolchainType.Gradle_Ninja) || (Variables.Toolchain == Cpp.ToolchainType.Ninja)))
                    {
                        if (Variables.HostOperatingSystem == Cpp.OperatingSystemType.Windows)
                        {
                            Host = "windows-x86_64";
                            ExeSuffix = ".exe";
                        }
                        else if (Variables.HostOperatingSystem == Cpp.OperatingSystemType.Linux)
                        {
                            Host = "linux-x86_64";
                            ExeSuffix = "";
                        }
                        else if (Variables.HostOperatingSystem == Cpp.OperatingSystemType.Mac)
                        {
                            Host = "darwin-x86_64";
                            ExeSuffix = "";
                        }
                        else
                        {
                            throw new InvalidOperationException("HostOperatingSystemNotSupported");
                        }
                        if (Variables.TargetArchitecture == Cpp.ArchitectureType.x86)
                        {
                            TargetPrefix = "i686";
                            ApiLevel = 21;
                        }
                        else if (Variables.TargetArchitecture == Cpp.ArchitectureType.x64)
                        {
                            TargetPrefix = "x86_64";
                            ApiLevel = 21;
                        }
                        else if (Variables.TargetArchitecture == Cpp.ArchitectureType.armv7a)
                        {
                            TargetPrefix = "armv7a";
                            ApiLevel = 17;
                        }
                        else if (Variables.TargetArchitecture == Cpp.ArchitectureType.arm64)
                        {
                            TargetPrefix = "aarch64";
                            ApiLevel = 21;
                        }
                        ToolchainPath = Variables.AndroidNdk / $"toolchains/llvm/prebuilt/{Host}";
                    }

                    //https://developer.android.com/ndk/guides/standalone_toolchain
                    //https://android.googlesource.com/platform/ndk/+/ndk-release-r19/docs/BuildSystemMaintainers.md

                    return VariableSpec.CreateNotApply(VariableValue.CreateBoolean(false));
                },
                SetVariableValue = v => { }
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.CC),
                DependentVariableNames = new List<String> { nameof(Variables.TargetOperatingSystem), nameof(Variables.TargetArchitecture), nameof(Variables.Toolchain), nameof(Variables.Compiler), "AndroidVariables" },
                GetVariableSpec = () =>
                {
                    if ((Variables.Toolchain == Cpp.ToolchainType.Ninja) || (Variables.Toolchain == Cpp.ToolchainType.Gradle_Ninja))
                    {
                        if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Linux)
                        {
                            var DefaultCC = "gcc";
                            if (Variables.Compiler == Cpp.CompilerType.gcc)
                            {
                                DefaultCC = "gcc";
                                if (Variables.TargetArchitecture == Cpp.ArchitectureType.armv7a)
                                {
                                    DefaultCC = "arm-linux-gnueabihf-gcc";
                                }
                                else if (Variables.TargetArchitecture == Cpp.ArchitectureType.arm64)
                                {
                                    DefaultCC = "aarch64-linux-gnu-gcc";
                                }
                            }
                            else if (Variables.Compiler == Cpp.CompilerType.clang)
                            {
                                DefaultCC = "clang";
                            }
                            return VariableSpec.CreateString(new StringSpec
                            {
                                DefaultValue = DefaultCC
                            });
                        }
                        else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Android)
                        {
                            return VariableSpec.CreateFixed(VariableValue.CreateString($"{ToolchainPath / "bin/clang"}{ExeSuffix} --target={TargetPrefix}-linux-androideabi{ApiLevel} --sysroot={ToolchainPath / "sysroot"}"));
                        }
                    }
                    return VariableSpec.CreateNotApply(VariableValue.CreateString(null));
                },
                SetVariableValue = v => Variables.CC = v.String
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.CXX),
                DependentVariableNames = new List<String> { nameof(Variables.TargetOperatingSystem), nameof(Variables.TargetArchitecture), nameof(Variables.Toolchain), nameof(Variables.Compiler), "AndroidVariables" },
                GetVariableSpec = () =>
                {
                    if ((Variables.Toolchain == Cpp.ToolchainType.Ninja) || (Variables.Toolchain == Cpp.ToolchainType.Gradle_Ninja))
                    {
                        if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Linux)
                        {
                            var DefaultCXX = "g++";
                            if (Variables.Compiler == Cpp.CompilerType.gcc)
                            {
                                DefaultCXX = "g++";
                                if (Variables.TargetArchitecture == Cpp.ArchitectureType.armv7a)
                                {
                                    DefaultCXX = "arm-linux-gnueabihf-g++";
                                }
                                else if (Variables.TargetArchitecture == Cpp.ArchitectureType.arm64)
                                {
                                    DefaultCXX = "aarch64-linux-gnu-g++";
                                }
                            }
                            else if (Variables.Compiler == Cpp.CompilerType.clang)
                            {
                                DefaultCXX = "clang++";
                            }
                            return VariableSpec.CreateString(new StringSpec
                            {
                                DefaultValue = DefaultCXX
                            });
                        }
                        else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Android)
                        {
                            return VariableSpec.CreateFixed(VariableValue.CreateString($"{ToolchainPath / "bin/clang++"}{ExeSuffix} --target={TargetPrefix}-linux-androideabi{ApiLevel} --sysroot={ToolchainPath / "sysroot"}"));
                        }
                    }
                    return VariableSpec.CreateNotApply(VariableValue.CreateString(null));
                },
                SetVariableValue = v => Variables.CXX = v.String
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.AR),
                DependentVariableNames = new List<String> { nameof(Variables.TargetOperatingSystem), nameof(Variables.TargetArchitecture), nameof(Variables.Toolchain), nameof(Variables.Compiler), "AndroidVariables" },
                GetVariableSpec = () =>
                {
                    if ((Variables.Toolchain == Cpp.ToolchainType.Ninja) || (Variables.Toolchain == Cpp.ToolchainType.Gradle_Ninja))
                    {
                        if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Linux)
                        {
                            var DefaultAR = "ar";
                            if (Variables.Compiler == Cpp.CompilerType.gcc)
                            {
                                if (Variables.TargetArchitecture == Cpp.ArchitectureType.armv7a)
                                {
                                    DefaultAR = "arm-linux-gnueabihf-ar";
                                }
                                else if (Variables.TargetArchitecture == Cpp.ArchitectureType.arm64)
                                {
                                    DefaultAR = "aarch64-linux-gnu-ar";
                                }
                            }
                            else if (Variables.Compiler == Cpp.CompilerType.clang)
                            {
                                DefaultAR = "llvm-ar";
                            }
                            return VariableSpec.CreateString(new StringSpec
                            {
                                DefaultValue = DefaultAR
                            });
                        }
                        else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Android)
                        {
                            return VariableSpec.CreateFixed(VariableValue.CreateString($"{ToolchainPath / "bin/llvm-ar"}{ExeSuffix}"));
                        }
                    }
                    return VariableSpec.CreateNotApply(VariableValue.CreateString(null));
                },
                SetVariableValue = v => Variables.AR = v.String
            });

            Dictionary<String, Make.ProjectDescription> Projects = null;
            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.SelectedProjects),
                DependentVariableNames = new List<String> { nameof(Variables.Toolchain), nameof(Variables.Compiler), nameof(Variables.HostOperatingSystem), nameof(Variables.HostArchitecture), nameof(Variables.TargetOperatingSystem), nameof(Variables.TargetArchitecture), nameof(Variables.Configuration), nameof(Variables.SourceDirectory), nameof(Variables.BuildDirectory), nameof(Variables.DevelopmentTeam), nameof(Variables.VSVersion), nameof(Variables.Jdk), nameof(Variables.AndroidSdk), nameof(Variables.AndroidNdk), nameof(Variables.CC), nameof(Variables.CXX), nameof(Variables.AR), nameof(Variables.ForceRegenerate), nameof(Variables.EnableNonTargetingOperatingSystemDummy) },
                GetVariableSpec = () =>
                {
                    var m = new Make(Variables.Toolchain, Variables.Compiler, Variables.HostOperatingSystem, Variables.HostArchitecture, Variables.TargetOperatingSystem, Variables.TargetArchitecture, Variables.Configuration, Variables.SourceDirectory, Variables.BuildDirectory, Variables.DevelopmentTeam, Variables.VSVersion, Variables.Jdk, Variables.AndroidSdk, Variables.AndroidNdk, Variables.CC, Variables.CXX, Variables.AR, Variables.ForceRegenerate, Variables.EnableNonTargetingOperatingSystemDummy);
                    Variables.m = m;
                    Projects = m.GetAvailableProjects();
                    var ProjectSet = new HashSet<String>(Projects.Values.Select(t => t.Definition.Name));
                    return VariableSpec.CreateMultiSelection(new MultiSelectionSpec
                    {
                        DefaultValues = ProjectSet,
                        Selections = ProjectSet,
                        Validator = Parts =>
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
                        }
                    });
                },
                SetVariableValue = v => Variables.SelectedProjects = v.StringSet.ToDictionary(Name => Name, Name => Projects[Name])
            });

            l.Add(new VariableItem
            {
                VariableName = "g",
                DependentVariableNames = new List<String> { nameof(Variables.SelectedProjects) },
                IsHidden = true,
                GetVariableSpec = () =>
                {
                    Variables.g = () =>
                    {
                        return Variables.m.Execute(Variables.SelectedProjects);
                    };

                    return VariableSpec.CreateNotApply(VariableValue.CreateBoolean(false));
                },
                SetVariableValue = v => { }
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.BuildNow),
                DependentVariableNames = new List<String> { },
                GetVariableSpec = () =>
                {
                    return VariableSpec.CreateBoolean(new BooleanSpec
                    {
                        DefaultValue = false
                    });
                },
                SetVariableValue = v => Variables.BuildNow = v.Boolean
            });

            return new KeyValuePair<Variables, List<VariableItem>>(Variables, l);
        }
        private static T UnwrapEnum<T>(String s)
        {
            return (T)Enum.Parse(typeof(T), s, true);
        }
        private static T? UnwrapNullableEnum<T>(String s) where T : struct
        {
            if (s == null) { return null; }
            return (T)Enum.Parse(typeof(T), s, true);
        }
        private static VariableSpec VariableSpecCreateEnumSelection<T>(T DefaultValue) where T : struct
        {
            return VariableSpecCreateEnumSelection(DefaultValue, new HashSet<T>(Enum.GetValues(typeof(T)).Cast<T>()));
        }
        private static VariableSpec VariableSpecCreateEnumSelection<T>(T DefaultValue, HashSet<T> Selections) where T : struct
        {
            var InputDisplay = String.Join("|", Selections.Select(e => e.Equals(DefaultValue) ? "[" + e.ToString() + "]" : e.ToString()));
            T Output = default(T);
            return VariableSpec.CreateSelection(new StringSelectionSpec
            {
                DefaultValue = DefaultValue.ToString(),
                InputDisplay = InputDisplay,
                Selections = new HashSet<String>(Selections.Select(v => v.ToString()), StringComparer.OrdinalIgnoreCase),
                Validator = v =>
                {
                    T o;
                    var b = Enum.TryParse<T>(v, true, out o);
                    if (!Selections.Contains(o)) { return new KeyValuePair<bool, String>(false, ""); }
                    Output = o;
                    return new KeyValuePair<bool, String>(b, "");
                },
                PostMapper = v => Output.ToString()
            });
        }
    }
}
