using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

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
                        if (Directory.Exists("/data/data/com.termux/files"))
                        {
                            return VariableSpec.CreateFixed(VariableValue.CreateString(Cpp.OperatingSystemType.Android.ToString()));
                        }
                        else
                        {
                            return VariableSpec.CreateFixed(VariableValue.CreateString(Cpp.OperatingSystemType.Linux.ToString()));
                        }
                    }
                    else if (Shell.OperatingSystem == Shell.OperatingSystemType.MacOS)
                    {
                        return VariableSpec.CreateFixed(VariableValue.CreateString(Cpp.OperatingSystemType.MacOS.ToString()));
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
                DependentVariableNames = new List<String> { nameof(Variables.HostOperatingSystem) },
                GetVariableSpec = () =>
                {
                    if (Variables.HostOperatingSystem == Cpp.OperatingSystemType.Android)
                    {
                        return VariableSpec.CreateFixed(VariableValue.CreateString(Cpp.ArchitectureType.arm64.ToString()));
                    }
                    else
                    {
                        if (Shell.OperatingSystemArchitecture == Shell.OperatingSystemArchitectureType.x86_64)
                        {
                            return VariableSpec.CreateFixed(VariableValue.CreateString(Cpp.ArchitectureType.x64.ToString()));
                        }
                        else if (Shell.OperatingSystemArchitecture == Shell.OperatingSystemArchitectureType.x86)
                        {
                            return VariableSpec.CreateFixed(VariableValue.CreateString(Cpp.ArchitectureType.x86.ToString()));
                        }
                        else if (Shell.OperatingSystemArchitecture == Shell.OperatingSystemArchitectureType.arm64)
                        {
                            return VariableSpec.CreateFixed(VariableValue.CreateString(Cpp.ArchitectureType.arm64.ToString()));
                        }
                        else
                        {
                            throw new InvalidOperationException("UnknownHostArchitecture");
                        }
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
                        var Distributions = new HashSet<String>(p.Value.Replace("\r", "").Split('\n').Skip(1).Where(Line => Line.Trim(' ') != "").Select(Line => Line.Split(' ').First()).ToArray(), StringComparer.OrdinalIgnoreCase);
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
                DependentVariableNames = new List<String> { nameof(Variables.TargetOperatingSystem), nameof(Variables.HostArchitecture) },
                GetVariableSpec = () =>
                {
                    if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Windows)
                    {
                        return VariableSpecCreateEnumSelection(Cpp.ArchitectureType.x64, new HashSet<Cpp.ArchitectureType> { Cpp.ArchitectureType.x86, Cpp.ArchitectureType.x64, Cpp.ArchitectureType.armv7a, Cpp.ArchitectureType.arm64 });
                    }
                    else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Linux)
                    {
                        return VariableSpecCreateEnumSelection(Cpp.ArchitectureType.x64);
                    }
                    else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.MacOS)
                    {
                        return VariableSpecCreateEnumSelection(Variables.HostArchitecture, new HashSet<Cpp.ArchitectureType> { Cpp.ArchitectureType.x64, Cpp.ArchitectureType.arm64 });
                    }
                    else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.iOS)
                    {
                        return VariableSpecCreateEnumSelection(Cpp.ArchitectureType.arm64, new HashSet<Cpp.ArchitectureType> { Cpp.ArchitectureType.x64, Cpp.ArchitectureType.armv7a, Cpp.ArchitectureType.arm64 });
                    }
                    else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Android)
                    {
                        return VariableSpecCreateEnumSelection(Cpp.ArchitectureType.arm64, new HashSet<Cpp.ArchitectureType> { Cpp.ArchitectureType.x86, Cpp.ArchitectureType.x64, Cpp.ArchitectureType.armv7a, Cpp.ArchitectureType.arm64, Cpp.ArchitectureType.riscv64 });
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                },
                SetVariableValue = v => Variables.TargetArchitecture = UnwrapEnum<Cpp.ArchitectureType>(v.String)
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.WindowsRuntime),
                DependentVariableNames = new List<String> { nameof(Variables.TargetOperatingSystem) },
                GetVariableSpec = () =>
                {
                    if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Windows)
                    {
                        return VariableSpecCreateEnumSelection(Cpp.WindowsRuntimeType.Win32);
                    }
                    else
                    {
                        return VariableSpec.CreateFixed(VariableValue.CreateString(null));
                    }
                },
                SetVariableValue = v => Variables.WindowsRuntime = UnwrapNullableEnum<Cpp.WindowsRuntimeType>(v.String)
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.EnableiOSSimulator),
                DependentVariableNames = new List<String> { nameof(Variables.TargetOperatingSystem), nameof(Variables.TargetArchitecture) },
                GetVariableSpec = () =>
                {
                    if ((Variables.TargetOperatingSystem == Cpp.OperatingSystemType.iOS) && ((Variables.TargetArchitecture == Cpp.ArchitectureType.arm64) || (Variables.TargetArchitecture == Cpp.ArchitectureType.x64)))
                    {
                        return VariableSpec.CreateBoolean(new BooleanSpec { DefaultValue = false });
                    }
                    else
                    {
                        return VariableSpec.CreateNotApply(VariableValue.CreateBoolean(false));
                    }
                },
                SetVariableValue = v => Variables.EnableiOSSimulator = v.Boolean
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.EnableMacCatalyst),
                DependentVariableNames = new List<String> { nameof(Variables.TargetOperatingSystem), nameof(Variables.TargetArchitecture) },
                GetVariableSpec = () =>
                {
                    if ((Variables.TargetOperatingSystem == Cpp.OperatingSystemType.iOS) && ((Variables.TargetArchitecture == Cpp.ArchitectureType.arm64) || (Variables.TargetArchitecture == Cpp.ArchitectureType.x64)))
                    {
                        return VariableSpec.CreateBoolean(new BooleanSpec { DefaultValue = false });
                    }
                    else
                    {
                        return VariableSpec.CreateNotApply(VariableValue.CreateBoolean(false));
                    }
                },
                SetVariableValue = v => Variables.EnableMacCatalyst = v.Boolean
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.Toolchain),
                DependentVariableNames = new List<String> { nameof(Variables.TargetOperatingSystem), nameof(Variables.TargetArchitecture), nameof(Variables.WindowsRuntime) },
                GetVariableSpec = () =>
                {
                    if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Windows)
                    {
                        if (Variables.WindowsRuntime == Cpp.WindowsRuntimeType.Win32)
                        {
                            if ((Variables.TargetArchitecture == Cpp.ArchitectureType.x86) || (Variables.TargetArchitecture == Cpp.ArchitectureType.x64) || (Variables.TargetArchitecture == Cpp.ArchitectureType.arm64))
                            {
                                return VariableSpecCreateEnumSelection(Cpp.ToolchainType.VisualStudio, new HashSet<Cpp.ToolchainType> { Cpp.ToolchainType.VisualStudio, Cpp.ToolchainType.Ninja });
                            }
                            else
                            {
                                return VariableSpec.CreateFixed(VariableValue.CreateString(Cpp.ToolchainType.VisualStudio.ToString()));
                            }
                        }
                        else
                        {
                            return VariableSpec.CreateFixed(VariableValue.CreateString(Cpp.ToolchainType.VisualStudio.ToString()));
                        }
                    }
                    else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Linux)
                    {
                        if (Variables.TargetArchitecture == Cpp.ArchitectureType.x64)
                        {
                            return VariableSpecCreateEnumSelection(Cpp.ToolchainType.Ninja, new HashSet<Cpp.ToolchainType> { Cpp.ToolchainType.Ninja, Cpp.ToolchainType.VisualStudio });
                        }
                        else if (Variables.TargetArchitecture == Cpp.ArchitectureType.x86)
                        {
                            return VariableSpecCreateEnumSelection(Cpp.ToolchainType.Ninja, new HashSet<Cpp.ToolchainType> { Cpp.ToolchainType.Ninja });
                        }
                        else
                        {
                            return VariableSpec.CreateFixed(VariableValue.CreateString(Cpp.ToolchainType.Ninja.ToString()));
                        }
                    }
                    else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.MacOS)
                    {
                        if (Variables.TargetArchitecture == Cpp.ArchitectureType.x64)
                        {
                            return VariableSpecCreateEnumSelection(Cpp.ToolchainType.XCode, new HashSet<Cpp.ToolchainType> { Cpp.ToolchainType.XCode, Cpp.ToolchainType.Ninja });
                        }
                        else
                        {
                            return VariableSpec.CreateFixed(VariableValue.CreateString(Cpp.ToolchainType.XCode.ToString()));
                        }
                    }
                    else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.iOS)
                    {
                        return VariableSpec.CreateFixed(VariableValue.CreateString(Cpp.ToolchainType.XCode.ToString()));
                    }
                    else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Android)
                    {
                        return VariableSpec.CreateFixed(VariableValue.CreateString(Cpp.ToolchainType.Ninja.ToString()));
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
                DependentVariableNames = new List<String> { nameof(Variables.TargetOperatingSystem), nameof(Variables.TargetArchitecture), nameof(Variables.WindowsRuntime) },
                GetVariableSpec = () =>
                {
                    if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Windows)
                    {
                        if (Variables.Toolchain == Cpp.ToolchainType.VisualStudio)
                        {
                            if (Variables.WindowsRuntime == Cpp.WindowsRuntimeType.Win32)
                            {
                                if ((Variables.TargetArchitecture == Cpp.ArchitectureType.x86) || (Variables.TargetArchitecture == Cpp.ArchitectureType.x64) || (Variables.TargetArchitecture == Cpp.ArchitectureType.arm64))
                                {
                                    return VariableSpecCreateEnumSelection(Cpp.CompilerType.VisualCpp, new HashSet<Cpp.CompilerType> { Cpp.CompilerType.VisualCpp, Cpp.CompilerType.clangcl });
                                }
                                else
                                {
                                    return VariableSpec.CreateFixed(VariableValue.CreateString(Cpp.CompilerType.VisualCpp.ToString()));
                                }
                            }
                            else
                            {
                                return VariableSpec.CreateFixed(VariableValue.CreateString(Cpp.CompilerType.VisualCpp.ToString()));
                            }
                        }
                        else if (Variables.Toolchain == Cpp.ToolchainType.Ninja)
                        {
                            return VariableSpec.CreateFixed(VariableValue.CreateString(Cpp.CompilerType.clang.ToString()));
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }
                    }
                    else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Linux)
                    {
                        return VariableSpecCreateEnumSelection(Cpp.CompilerType.clang, new HashSet<Cpp.CompilerType> { Cpp.CompilerType.gcc, Cpp.CompilerType.clang });
                    }
                    else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.MacOS)
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
                VariableName = nameof(Variables.CLibrary),
                DependentVariableNames = new List<String> { nameof(Variables.TargetOperatingSystem), nameof(Variables.Toolchain) },
                GetVariableSpec = () =>
                {
                    if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Windows)
                    {
                        return VariableSpec.CreateFixed(VariableValue.CreateString(Cpp.CLibraryType.VisualCRuntime.ToString()));
                    }
                    else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Linux)
                    {
                        return VariableSpecCreateEnumSelection(Cpp.CLibraryType.glibc, new HashSet<Cpp.CLibraryType> { Cpp.CLibraryType.glibc, Cpp.CLibraryType.musl });
                    }
                    else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.MacOS)
                    {
                        return VariableSpec.CreateFixed(VariableValue.CreateString(Cpp.CLibraryType.libSystem.ToString()));
                    }
                    else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.iOS)
                    {
                        return VariableSpec.CreateFixed(VariableValue.CreateString(Cpp.CLibraryType.libSystem.ToString()));
                    }
                    else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Android)
                    {
                        return VariableSpec.CreateFixed(VariableValue.CreateString(Cpp.CLibraryType.Bionic.ToString()));
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                },
                SetVariableValue = v => Variables.CLibrary = UnwrapEnum<Cpp.CLibraryType>(v.String)
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.CLibraryForm),
                DependentVariableNames = new List<String> { nameof(Variables.TargetOperatingSystem), nameof(Variables.Toolchain), nameof(Variables.CLibrary) },
                GetVariableSpec = () =>
                {
                    if (((Variables.WindowsRuntime == Cpp.WindowsRuntimeType.Win32) && (Variables.CLibrary == Cpp.CLibraryType.VisualCRuntime)) || (Variables.CLibrary == Cpp.CLibraryType.musl))
                    {
                        return VariableSpecCreateEnumSelection(Cpp.CLibraryForm.Dynamic, new HashSet<Cpp.CLibraryForm> { Cpp.CLibraryForm.Static, Cpp.CLibraryForm.Dynamic });
                    }
                    else
                    {
                        return VariableSpec.CreateFixed(VariableValue.CreateString(Cpp.CLibraryForm.Dynamic.ToString()));
                    }
                },
                SetVariableValue = v => Variables.CLibraryForm = UnwrapEnum<Cpp.CLibraryForm>(v.String)
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.CppLibrary),
                DependentVariableNames = new List<String> { nameof(Variables.TargetOperatingSystem), nameof(Variables.Toolchain), nameof(Variables.Compiler) },
                GetVariableSpec = () =>
                {
                    if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Windows)
                    {
                        return VariableSpec.CreateFixed(VariableValue.CreateString(Cpp.CppLibraryType.VisualCppRuntime.ToString()));
                    }
                    else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Linux)
                    {
                        if (Variables.Compiler == Cpp.CompilerType.gcc)
                        {
                            return VariableSpec.CreateFixed(VariableValue.CreateString(Cpp.CppLibraryType.libstdcxx.ToString()));
                        }
                        else if (Variables.Compiler == Cpp.CompilerType.clang)
                        {
                            return VariableSpecCreateEnumSelection(Cpp.CppLibraryType.libcxx, new HashSet<Cpp.CppLibraryType> { Cpp.CppLibraryType.libstdcxx, Cpp.CppLibraryType.libcxx });
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }
                    }
                    else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.MacOS)
                    {
                        return VariableSpec.CreateFixed(VariableValue.CreateString(Cpp.CppLibraryType.libcxx.ToString()));
                    }
                    else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.iOS)
                    {
                        return VariableSpec.CreateFixed(VariableValue.CreateString(Cpp.CppLibraryType.libcxx.ToString()));
                    }
                    else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Android)
                    {
                        return VariableSpec.CreateFixed(VariableValue.CreateString(Cpp.CppLibraryType.libcxx.ToString()));
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                },
                SetVariableValue = v => Variables.CppLibrary = UnwrapEnum<Cpp.CppLibraryType>(v.String)
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.CppLibraryForm),
                DependentVariableNames = new List<String> { nameof(Variables.CLibrary), nameof(Variables.CLibraryForm), nameof(Variables.CppLibrary) },
                GetVariableSpec = () =>
                {
                    if (Variables.CppLibrary == Cpp.CppLibraryType.VisualCppRuntime)
                    {
                        if (Variables.CLibraryForm == Cpp.CLibraryForm.Static)
                        {
                            return VariableSpec.CreateFixed(VariableValue.CreateString(Cpp.CppLibraryForm.Static.ToString()));
                        }
                        else
                        {
                            return VariableSpec.CreateFixed(VariableValue.CreateString(Cpp.CppLibraryForm.Dynamic.ToString()));
                        }
                    }
                    else if ((Variables.CppLibrary == Cpp.CppLibraryType.libstdcxx) || (Variables.CppLibrary == Cpp.CppLibraryType.libcxx))
                    {
                        if (Variables.CLibraryForm == Cpp.CLibraryForm.Static)
                        {
                            return VariableSpec.CreateFixed(VariableValue.CreateString(Cpp.CppLibraryForm.Static.ToString()));
                        }
                        else
                        {
                            return VariableSpecCreateEnumSelection(Cpp.CppLibraryForm.Dynamic, new HashSet<Cpp.CppLibraryForm> { Cpp.CppLibraryForm.Static, Cpp.CppLibraryForm.Dynamic });
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                },
                SetVariableValue = v => Variables.CppLibraryForm = UnwrapEnum<Cpp.CppLibraryForm>(v.String)
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.Configuration),
                DependentVariableNames = new List<String> { },
                GetVariableSpec = () =>
                {
                    return VariableSpecCreateEnumSelection(Cpp.ConfigurationType.Debug);
                },
                SetVariableValue = v => Variables.Configuration = UnwrapEnum<Cpp.ConfigurationType>(v.String)
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.EnableModule),
                DependentVariableNames = new List<String> { nameof(Variables.Compiler) },
                GetVariableSpec = () =>
                {
                    if (Variables.Compiler == Cpp.CompilerType.VisualCpp)
                    {
                        return VariableSpec.CreateBoolean(new BooleanSpec { DefaultValue = false });
                    }
                    else
                    {
                        return VariableSpec.CreateNotApply(VariableValue.CreateBoolean(false));
                    }
                },
                SetVariableValue = v => Variables.EnableModule = v.Boolean
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.EnableCustomSysroot),
                DependentVariableNames = new List<String> { nameof(Variables.TargetOperatingSystem), nameof(Variables.Compiler) },
                GetVariableSpec = () =>
                {
                    if ((Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Linux) && (Variables.Compiler == Cpp.CompilerType.clang))
                    {
                        return VariableSpec.CreateBoolean(new BooleanSpec { DefaultValue = false });
                    }
                    else
                    {
                        return VariableSpec.CreateNotApply(VariableValue.CreateBoolean(false));
                    }
                },
                SetVariableValue = v => Variables.EnableCustomSysroot = v.Boolean
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.CustomSysroot),
                DependentVariableNames = new List<String> { nameof(Variables.EnableCustomSysroot) },
                GetVariableSpec = () =>
                {
                    if (Variables.EnableCustomSysroot)
                    {
                        return VariableSpec.CreatePath(new PathStringSpec
                        {
                            DefaultValue = null,
                            IsDirectory = true,
                            Validator = p => new KeyValuePair<bool, String>(true, "")
                        });
                    }
                    else
                    {
                        return VariableSpec.CreateNotApply(VariableValue.CreatePath(null));
                    }
                },
                SetVariableValue = v => Variables.CustomSysroot = v.Path
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.EnableLibcxxCompilation),
                DependentVariableNames = new List<String> { nameof(Variables.TargetOperatingSystem), nameof(Variables.CppLibrary), nameof(Variables.CppLibraryForm), nameof(Variables.Configuration) },
                GetVariableSpec = () =>
                {
                    if ((Variables.CppLibrary == Cpp.CppLibraryType.libcxx) && (Variables.CppLibraryForm == Cpp.CppLibraryForm.Static))
                    {
                        if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Android)
                        {
                            return VariableSpec.CreateBoolean(new BooleanSpec
                            {
                                DefaultValue = Variables.Configuration == Cpp.ConfigurationType.Release
                            });
                        }
                        else
                        {
                            return VariableSpec.CreateBoolean(new BooleanSpec
                            {
                                DefaultValue = true
                            });
                        }
                    }
                    else
                    {
                        return VariableSpec.CreateNotApply(VariableValue.CreateBoolean(false));
                    }
                },
                SetVariableValue = v => Variables.EnableLibcxxCompilation = v.Boolean
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
                        Validator = p => Directory.Exists(p / "build-tools/TypeMake") ? new KeyValuePair<bool, String>(true, "") : new KeyValuePair<bool, string>(false, "build-tools/TypeMake not exist.")
                    });
                },
                SetVariableValue = v => Variables.SourceDirectory = v.Path
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.BuildDirectory),
                DependentVariableNames = new List<String> { nameof(Variables.TargetOperatingSystem), nameof(Variables.TargetArchitecture), nameof(Variables.WindowsRuntime), nameof(Variables.Toolchain), nameof(Variables.Compiler), nameof(Variables.Configuration), nameof(Variables.SourceDirectory) },
                GetVariableSpec = () =>
                {
                    String DefaultBuildDir = null;
                    if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Windows)
                    {
                        if (Variables.WindowsRuntime == Cpp.WindowsRuntimeType.Win32)
                        {
                            if (Variables.Toolchain == Cpp.ToolchainType.VisualStudio)
                            {
                                if (Variables.Compiler == Cpp.CompilerType.VisualCpp)
                                {
                                    DefaultBuildDir = Variables.SourceDirectory / $"build/windows_{Variables.TargetArchitecture}";
                                }
                                else
                                {
                                    DefaultBuildDir = Variables.SourceDirectory / $"build/windows_{Variables.TargetArchitecture}_{Variables.Compiler}";
                                }
                            }
                            else
                            {
                                DefaultBuildDir = Variables.SourceDirectory / $"build/windows_{Variables.TargetArchitecture}_{Variables.Toolchain}_{Variables.Compiler}_{Variables.Configuration}";
                            }
                        }
                        else if (Variables.WindowsRuntime == Cpp.WindowsRuntimeType.WinRT)
                        {
                            if (Variables.Toolchain == Cpp.ToolchainType.VisualStudio)
                            {
                                DefaultBuildDir = Variables.SourceDirectory / $"build/winrt_{Variables.TargetArchitecture}";
                            }
                            else
                            {
                                DefaultBuildDir = Variables.SourceDirectory / $"build/winrt_{Variables.TargetArchitecture}_{Variables.Toolchain}_{Variables.Compiler}_{Variables.Configuration}";
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }
                    }
                    else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Linux)
                    {
                        DefaultBuildDir = Variables.SourceDirectory / $"build/linux_{Variables.TargetArchitecture}_{Variables.Toolchain}_{Variables.Compiler}_{Variables.CLibrary}_{Variables.Configuration}";
                    }
                    else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.MacOS)
                    {
                        if (Variables.Toolchain == Cpp.ToolchainType.XCode)
                        {
                            DefaultBuildDir = Variables.SourceDirectory / $"build/mac_{Variables.TargetArchitecture}";
                        }
                        else
                        {
                            DefaultBuildDir = Variables.SourceDirectory / $"build/mac_{Variables.TargetArchitecture}_{Variables.Toolchain}_{Variables.Configuration}";
                        }
                    }
                    else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.iOS)
                    {
                        DefaultBuildDir = Variables.SourceDirectory / $"build/ios_{Variables.TargetArchitecture}";
                    }
                    else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Android)
                    {
                        DefaultBuildDir = Variables.SourceDirectory / $"build/android_{Variables.TargetArchitecture}_{Variables.Toolchain}_{Variables.Configuration}";
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
                VariableName = nameof(Variables.MaxProcessCount),
                DependentVariableNames = new List<String> { },
                GetVariableSpec = () =>
                {
                    return VariableSpec.CreateInteger(new IntegerSpec
                    {
                        DefaultValue = Environment.ProcessorCount
                    });
                },
                SetVariableValue = v => Variables.MaxProcessCount = v.Integer
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
                VariableName = nameof(Variables.XCodeDevelopmentTeam),
                DependentVariableNames = new List<String> { nameof(Variables.TargetOperatingSystem), nameof(Variables.Toolchain) },
                GetVariableSpec = () =>
                {
                    if (((Variables.TargetOperatingSystem == Cpp.OperatingSystemType.MacOS) && (Variables.Toolchain == Cpp.ToolchainType.XCode)) || (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.iOS))
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
                SetVariableValue = v => Variables.XCodeDevelopmentTeam = v.String
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.XCodeProvisioningProfileSpecifier),
                DependentVariableNames = new List<String> { nameof(Variables.TargetOperatingSystem), nameof(Variables.Toolchain) },
                GetVariableSpec = () =>
                {
                    if (((Variables.TargetOperatingSystem == Cpp.OperatingSystemType.MacOS) && (Variables.Toolchain == Cpp.ToolchainType.XCode)) || (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.iOS))
                    {
                        return VariableSpec.CreateString(new StringSpec
                        {
                            DefaultValue = "",
                            InputDisplay = "(optional)"
                        });
                    }
                    else
                    {
                        return VariableSpec.CreateNotApply(VariableValue.CreateString(null));
                    }
                },
                SetVariableValue = v => Variables.XCodeProvisioningProfileSpecifier = v.String
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
                DependentVariableNames = new List<String> { nameof(Variables.HostOperatingSystem), nameof(Variables.Toolchain), nameof(PathValidator) },
                GetVariableSpec = () =>
                {
                    if (Variables.Toolchain == Cpp.ToolchainType.VisualStudio)
                    {
                        String DefaultVSDir = "";
                        if (Variables.HostOperatingSystem == Cpp.OperatingSystemType.Windows)
                        {
                            ((Action)(() =>
                            {
                                foreach (var ProgramFiles in new String[] { Environment.GetEnvironmentVariable("ProgramFiles"), Environment.GetEnvironmentVariable("ProgramFiles(x86)") })
                                {
                                    if (ProgramFiles != "")
                                    {
                                        foreach (var Version in new int[] { 2022 })
                                        {
                                            foreach (var d in new String[] { "Enterprise", "Professional", "Community", "BuildTools" })
                                            {
                                                var p = ProgramFiles.AsPath() / $"Microsoft Visual Studio/{Version}" / d;
                                                if (Directory.Exists(p))
                                                {
                                                    DefaultVSDir = p;
                                                    return;
                                                }
                                            }
                                        }
                                    }
                                }
                            }))();
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
                DependentVariableNames = new List<String> { nameof(Variables.TargetOperatingSystem), nameof(Variables.Toolchain), nameof(Variables.VSDir) },
                GetVariableSpec = () =>
                {
                    if (Variables.Toolchain == Cpp.ToolchainType.VisualStudio)
                    {
                        var VSVersion = Variables.VSDir.ToString().Contains("2022") ? 2022 : 2022;
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
                VariableName = nameof(Variables.XCodeDir),
                DependentVariableNames = new List<String> { nameof(Variables.HostOperatingSystem), nameof(Variables.Toolchain), nameof(PathValidator) },
                GetVariableSpec = () =>
                {
                    if (Variables.Toolchain == Cpp.ToolchainType.XCode)
                    {
                        String DefaultXCodeDir = "";
                        if (Variables.HostOperatingSystem == Cpp.OperatingSystemType.MacOS)
                        {
                            DefaultXCodeDir = "/Applications/Xcode.app";
                        }
                        return VariableSpec.CreatePath(new PathStringSpec
                        {
                            DefaultValue = DefaultXCodeDir,
                            IsDirectory = true,
                            Validator = PathValidator
                        });
                    }
                    else
                    {
                        return VariableSpec.CreateNotApply(VariableValue.CreatePath(null));
                    }
                },
                SetVariableValue = v => Variables.XCodeDir = v.Path
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.LLVM),
                DependentVariableNames = new List<String> { nameof(Variables.TargetOperatingSystem), nameof(Variables.Toolchain), nameof(Variables.VSDir), nameof(PathValidator) },
                GetVariableSpec = () =>
                {
                    if ((Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Windows) && (Variables.Toolchain == Cpp.ToolchainType.Ninja))
                    {
                        String DefaultLLVMDir = "";
                        ((Action)(() =>
                        {
                            foreach (var ProgramFiles in new String[] { Environment.GetEnvironmentVariable("ProgramFiles"), Environment.GetEnvironmentVariable("ProgramFiles(x86)") })
                            {
                                if (ProgramFiles != "")
                                {
                                    {
                                        var p = ProgramFiles.AsPath() / "LLVM/bin";
                                        if (Directory.Exists(p))
                                        {
                                            DefaultLLVMDir = p;
                                            return;
                                        }
                                    }
                                    foreach (var Version in new int[] { 2022 })
                                    {
                                        foreach (var d in new String[] { "Enterprise", "Professional", "Community", "BuildTools" })
                                        {
                                            var p = ProgramFiles.AsPath() / $"Microsoft Visual Studio/{Version}" / d / "VC/Tools/Llvm/x64/bin";
                                            if (Directory.Exists(p))
                                            {
                                                DefaultLLVMDir = p;
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                        }))();
                        return VariableSpec.CreatePath(new PathStringSpec
                        {
                            DefaultValue = DefaultLLVMDir,
                            IsDirectory = true,
                            Validator = PathValidator
                        });
                    }
                    else
                    {
                        return VariableSpec.CreateNotApply(VariableValue.CreatePath(null));
                    }
                },
                SetVariableValue = v => Variables.LLVM = v.Path
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.EnableJava),
                DependentVariableNames = new List<String> { nameof(Variables.HostOperatingSystem), nameof(Variables.TargetOperatingSystem) },
                GetVariableSpec = () =>
                {
                    if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Android)
                    {
                        return VariableSpec.CreateBoolean(new BooleanSpec
                        {
                            DefaultValue = true
                        });
                    }
                    else
                    {
                        return VariableSpec.CreateFixed(VariableValue.CreateBoolean(false));
                    }
                },
                SetVariableValue = v => Variables.EnableJava = v.Boolean
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.Jdk),
                DependentVariableNames = new List<String> { nameof(Variables.HostOperatingSystem), nameof(Variables.TargetOperatingSystem), nameof(Variables.EnableJava), nameof(PathValidator) },
                GetVariableSpec = () =>
                {
                    if (Variables.EnableJava && (Variables.TargetOperatingSystem != Cpp.OperatingSystemType.iOS))
                    {
                        var DefaultJdk = Environment.GetEnvironmentVariable("JAVA_HOME").AsPath();
                        if (Variables.HostOperatingSystem == Cpp.OperatingSystemType.MacOS)
                        {
                            var o = Shell.ExecuteAndGetOutput("/usr/libexec/java_home");
                            if (o.Key == 0)
                            {
                                DefaultJdk = o.Value.Trim('\r', '\n');
                            }
                            else
                            {
                                DefaultJdk = "/usr";
                            }
                        }
                        else if ((Variables.HostOperatingSystem != Cpp.OperatingSystemType.Windows) && String.IsNullOrEmpty(DefaultJdk))
                        {
                            DefaultJdk = "/usr";
                            try
                            {
                                var JavacPath = Shell.ExecuteAndGetOutput("which", "javac").Value.Trim('\r', '\n');
                                if (JavacPath != "")
                                {
                                    DefaultJdk = Shell.ExecuteAndGetOutput("readlink", "-f", JavacPath).Value.Trim('\r', '\n').AsPath().Parent.Parent;
                                }
                            }
                            catch
                            {
                            }
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
                DependentVariableNames = new List<String> { nameof(Variables.HostOperatingSystem), nameof(Variables.TargetOperatingSystem), nameof(Variables.EnableJava), nameof(PathValidator) },
                GetVariableSpec = () =>
                {
                    if (Variables.EnableJava && (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Android))
                    {
                        return VariableSpec.CreatePath(new PathStringSpec
                        {
                            DefaultValue = Variables.HostOperatingSystem == Cpp.OperatingSystemType.Windows ? (Environment.GetEnvironmentVariable("LocalAppData").AsPath() / "Android/sdk") : "".AsPath(),
                            IsDirectory = true,
                            Validator = PathValidator ?? (p => Directory.Exists(p / "platform-tools") ? new KeyValuePair<bool, String>(true, "") : new KeyValuePair<bool, String>(false, "No tools directory inside."))
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
                        String DefaultValue = null;
                        if ((Variables.AndroidSdk != null) && (Directory.Exists(Variables.AndroidSdk / "ndk")))
                        {
                            var NdkVersions = FileSystemUtils.GetDirectories(Variables.AndroidSdk / "ndk", "*", SearchOption.TopDirectoryOnly).Where(d => File.Exists(d / "source.properties")).Select(d => d.FileName).ToList();
                            if (NdkVersions.Count > 0)
                            {
                                DefaultValue = Variables.AndroidSdk / "ndk" / NdkVersions.Max();
                            }
                        }
                        return VariableSpec.CreatePath(new PathStringSpec
                        {
                            DefaultValue = DefaultValue,
                            IsDirectory = true,
                            Validator = p => new KeyValuePair<bool, String>(true, "")
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
                VariableName = nameof(Variables.Ninja),
                DependentVariableNames = new List<String> { nameof(Variables.HostOperatingSystem), nameof(Variables.TargetOperatingSystem), nameof(Variables.Toolchain), nameof(Variables.SourceDirectory) },
                GetVariableSpec = () =>
                {
                    if (Variables.Toolchain == Cpp.ToolchainType.Ninja)
                    {
                        if (Variables.HostOperatingSystem == Cpp.OperatingSystemType.Windows)
                        {
                            if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Linux)
                            {
                                return VariableSpec.CreateFixed(VariableValue.CreatePath(Variables.SourceDirectory / "build-tools/Ninja/ninja-linux/ninja"));
                            }
                            else
                            {
                                return VariableSpec.CreateFixed(VariableValue.CreatePath(Variables.SourceDirectory / "build-tools/Ninja/ninja-win/ninja.exe"));
                            }
                        }
                        else if (Variables.HostOperatingSystem == Cpp.OperatingSystemType.Linux)
                        {
                            var Ninja = Shell.TryLocate("ninja") ?? Variables.SourceDirectory / "build-tools/Ninja/ninja-linux/ninja";
                            return VariableSpec.CreatePath(new PathStringSpec
                            {
                                DefaultValue = Ninja
                            });
                        }
                        else if (Variables.HostOperatingSystem == Cpp.OperatingSystemType.MacOS)
                        {
                            return VariableSpec.CreateFixed(VariableValue.CreatePath(Variables.SourceDirectory / "build-tools/Ninja/ninja-mac/ninja"));
                        }
                        else if (Variables.HostOperatingSystem == Cpp.OperatingSystemType.Android)
                        {
                            var Ninja = Shell.TryLocate("ninja");
                            return VariableSpec.CreatePath(new PathStringSpec
                            {
                                DefaultValue = Ninja
                            });
                        }
                        else
                        {
                            throw new InvalidOperationException("HostOperatingSystemNotSupported");
                        }
                    }
                    return VariableSpec.CreateNotApply(VariableValue.CreatePath(null));
                },
                SetVariableValue = v => Variables.Ninja = v.Path
            });

            var Host = "";
            var ExeSuffix = "";
            var TargetPrefix = "";
            var ApiLevel = 21;
            var ToolchainPath = "".AsPath();

            l.Add(new VariableItem
            {
                VariableName = "AndroidVariables",
                DependentVariableNames = new List<String> { nameof(Variables.HostOperatingSystem), nameof(Variables.TargetOperatingSystem), nameof(Variables.TargetArchitecture), nameof(Variables.Toolchain), nameof(Variables.AndroidNdk) },
                IsHidden = true,
                GetVariableSpec = () =>
                {
                    if ((Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Android) && (Variables.Toolchain == Cpp.ToolchainType.Ninja))
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
                        else if (Variables.HostOperatingSystem == Cpp.OperatingSystemType.MacOS)
                        {
                            Host = "darwin-x86_64";
                            ExeSuffix = "";
                        }
                        else if (Variables.HostOperatingSystem == Cpp.OperatingSystemType.Android)
                        {
                            Host = "linux-aarch64";
                            ExeSuffix = "";
                        }
                        else
                        {
                            throw new InvalidOperationException("HostOperatingSystemNotSupported");
                        }
                        if (Variables.TargetArchitecture == Cpp.ArchitectureType.x86)
                        {
                            TargetPrefix = "i686";
                        }
                        else if (Variables.TargetArchitecture == Cpp.ArchitectureType.x64)
                        {
                            TargetPrefix = "x86_64";
                        }
                        else if (Variables.TargetArchitecture == Cpp.ArchitectureType.armv7a)
                        {
                            TargetPrefix = "armv7a";
                        }
                        else if (Variables.TargetArchitecture == Cpp.ArchitectureType.arm64)
                        {
                            TargetPrefix = "aarch64";
                        }
                        else if (Variables.TargetArchitecture == Cpp.ArchitectureType.riscv64)
                        {
                            TargetPrefix = "riscv64";
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
                DependentVariableNames = new List<String> { nameof(Variables.TargetOperatingSystem), nameof(Variables.TargetArchitecture), nameof(Variables.Toolchain), nameof(Variables.Compiler), nameof(Variables.LLVM), "AndroidVariables" },
                GetVariableSpec = () =>
                {
                    if ((Variables.Toolchain == Cpp.ToolchainType.Ninja) || ((Variables.Toolchain == Cpp.ToolchainType.VisualStudio) && (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Linux)))
                    {
                        if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Windows)
                        {
                            return VariableSpec.CreateString(new StringSpec
                            {
                                DefaultValue = Variables.LLVM / "clang.exe"
                            });
                        }
                        else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Linux)
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
                        else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.MacOS)
                        {
                            return VariableSpec.CreateString(new StringSpec
                            {
                                DefaultValue = "clang"
                            });
                        }
                        else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Android)
                        {
                            return VariableSpec.CreateString(new StringSpec
                            {
                                DefaultValue = $"{ToolchainPath / "bin/clang"}{ExeSuffix} --target={TargetPrefix}-linux-androideabi{ApiLevel} --sysroot={ToolchainPath / "sysroot"}"
                            });
                        }
                    }
                    return VariableSpec.CreateNotApply(VariableValue.CreateString(null));
                },
                SetVariableValue = v => Variables.CC = v.String
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.CXX),
                DependentVariableNames = new List<String> { nameof(Variables.TargetOperatingSystem), nameof(Variables.TargetArchitecture), nameof(Variables.Toolchain), nameof(Variables.Compiler), nameof(Variables.LLVM), "AndroidVariables" },
                GetVariableSpec = () =>
                {
                    if ((Variables.Toolchain == Cpp.ToolchainType.Ninja) || ((Variables.Toolchain == Cpp.ToolchainType.VisualStudio) && (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Linux)))
                    {
                        if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Windows)
                        {
                            return VariableSpec.CreateString(new StringSpec
                            {
                                DefaultValue = Variables.LLVM / "clang++.exe"
                            });
                        }
                        else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Linux)
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
                        else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.MacOS)
                        {
                            return VariableSpec.CreateString(new StringSpec
                            {
                                DefaultValue = "clang++"
                            });
                        }
                        else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Android)
                        {
                            return VariableSpec.CreateString(new StringSpec
                            {
                                DefaultValue = $"{ToolchainPath / "bin/clang++"}{ExeSuffix} --target={TargetPrefix}-linux-androideabi{ApiLevel} --sysroot={ToolchainPath / "sysroot"}"
                            });
                        }
                    }
                    return VariableSpec.CreateNotApply(VariableValue.CreateString(null));
                },
                SetVariableValue = v => Variables.CXX = v.String
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.AR),
                DependentVariableNames = new List<String> { nameof(Variables.TargetOperatingSystem), nameof(Variables.TargetArchitecture), nameof(Variables.Toolchain), nameof(Variables.Compiler), nameof(Variables.LLVM), "AndroidVariables" },
                GetVariableSpec = () =>
                {
                    if ((Variables.Toolchain == Cpp.ToolchainType.Ninja) || ((Variables.Toolchain == Cpp.ToolchainType.VisualStudio) && (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Linux)))
                    {
                        if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Windows)
                        {
                            return VariableSpec.CreateString(new StringSpec
                            {
                                DefaultValue = Variables.LLVM / "llvm-ar.exe"
                            });
                        }
                        else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Linux)
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
                        else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.MacOS)
                        {
                            return VariableSpec.CreateString(new StringSpec
                            {
                                DefaultValue = "ar"
                            });
                        }
                        else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Android)
                        {
                            return VariableSpec.CreateString(new StringSpec
                            {
                                DefaultValue = $"{ToolchainPath / "bin/llvm-ar"}{ExeSuffix}"
                            });
                        }
                    }
                    return VariableSpec.CreateNotApply(VariableValue.CreateString(null));
                },
                SetVariableValue = v => Variables.AR = v.String
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.STRIP),
                DependentVariableNames = new List<String> { nameof(Variables.TargetOperatingSystem), nameof(Variables.TargetArchitecture), nameof(Variables.Toolchain), nameof(Variables.Compiler), nameof(Variables.LLVM), "AndroidVariables" },
                GetVariableSpec = () =>
                {
                    if (Variables.Toolchain == Cpp.ToolchainType.Ninja)
                    {
                        if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Windows)
                        {
                            return VariableSpec.CreateNotApply(VariableValue.CreateString(""));
                        }
                        else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Linux)
                        {
                            var DefaultSTRIP = "strip";
                            if (Variables.Compiler == Cpp.CompilerType.gcc)
                            {
                                if (Variables.TargetArchitecture == Cpp.ArchitectureType.armv7a)
                                {
                                    DefaultSTRIP = "arm-linux-gnueabihf-strip";
                                }
                                else if (Variables.TargetArchitecture == Cpp.ArchitectureType.arm64)
                                {
                                    DefaultSTRIP = "aarch64-linux-gnu-strip";
                                }
                            }
                            else if (Variables.Compiler == Cpp.CompilerType.clang)
                            {
                                DefaultSTRIP = "llvm-strip";
                            }
                            return VariableSpec.CreateString(new StringSpec
                            {
                                DefaultValue = DefaultSTRIP
                            });
                        }
                        else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.MacOS)
                        {
                            return VariableSpec.CreateString(new StringSpec
                            {
                                DefaultValue = "strip"
                            });
                        }
                        else if (Variables.TargetOperatingSystem == Cpp.OperatingSystemType.Android)
                        {
                            return VariableSpec.CreateString(new StringSpec
                            {
                                DefaultValue = $"{ToolchainPath / "bin/llvm-strip"}{ExeSuffix}"
                            });
                        }
                    }
                    return VariableSpec.CreateNotApply(VariableValue.CreateString(null));
                },
                SetVariableValue = v => Variables.STRIP = v.String
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.EnableAdditionalFlags),
                DependentVariableNames = new List<String> { },
                GetVariableSpec = () =>
                {
                    return VariableSpec.CreateBoolean(new BooleanSpec
                    {
                        DefaultValue = false
                    });
                },
                SetVariableValue = v => Variables.EnableAdditionalFlags = v.Boolean
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.CommonFlags),
                DependentVariableNames = new List<String> { nameof(Variables.EnableAdditionalFlags) },
                GetVariableSpec = () =>
                {
                    if (Variables.EnableAdditionalFlags)
                    {
                        return VariableSpec.CreateString(new StringSpec
                        {
                            DefaultValue = ""
                        });
                    }
                    return VariableSpec.CreateFixed(VariableValue.CreateString(""));
                },
                SetVariableValue = v => Variables.CommonFlags = ParseFlags(v.String)
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.CFlags),
                DependentVariableNames = new List<String> { nameof(Variables.EnableAdditionalFlags) },
                GetVariableSpec = () =>
                {
                    if (Variables.EnableAdditionalFlags)
                    {
                        return VariableSpec.CreateString(new StringSpec
                        {
                            DefaultValue = ""
                        });
                    }
                    return VariableSpec.CreateFixed(VariableValue.CreateString(""));
                },
                SetVariableValue = v => Variables.CFlags = ParseFlags(v.String)
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.CppFlags),
                DependentVariableNames = new List<String> { nameof(Variables.EnableAdditionalFlags) },
                GetVariableSpec = () =>
                {
                    if (Variables.EnableAdditionalFlags)
                    {
                        return VariableSpec.CreateString(new StringSpec
                        {
                            DefaultValue = ""
                        });
                    }
                    return VariableSpec.CreateFixed(VariableValue.CreateString(""));
                },
                SetVariableValue = v => Variables.CppFlags = ParseFlags(v.String)
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.LinkerFlags),
                DependentVariableNames = new List<String> { nameof(Variables.EnableAdditionalFlags) },
                GetVariableSpec = () =>
                {
                    if (Variables.EnableAdditionalFlags)
                    {
                        return VariableSpec.CreateString(new StringSpec
                        {
                            DefaultValue = ""
                        });
                    }
                    return VariableSpec.CreateFixed(VariableValue.CreateString(""));
                },
                SetVariableValue = v => Variables.LinkerFlags = ParseFlags(v.String)
            });

            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.PostLinkerFlags),
                DependentVariableNames = new List<String> { nameof(Variables.EnableAdditionalFlags) },
                GetVariableSpec = () =>
                {
                    if (Variables.EnableAdditionalFlags)
                    {
                        return VariableSpec.CreateString(new StringSpec
                        {
                            DefaultValue = ""
                        });
                    }
                    return VariableSpec.CreateFixed(VariableValue.CreateString(""));
                },
                SetVariableValue = v => Variables.PostLinkerFlags = ParseFlags(v.String)
            });

            Dictionary<String, Build.ProjectDescription> Projects = null;
            l.Add(new VariableItem
            {
                VariableName = nameof(Variables.SelectedProjects),
                DependentVariableNames = new List<String> { nameof(Variables.HostOperatingSystem), nameof(Variables.HostArchitecture), nameof(Variables.TargetOperatingSystem), nameof(Variables.TargetArchitecture), nameof(Variables.WindowsRuntime), nameof(Variables.EnableiOSSimulator), nameof(Variables.EnableMacCatalyst), nameof(Variables.Toolchain), nameof(Variables.Compiler), nameof(Variables.CLibrary), nameof(Variables.CLibraryForm), nameof(Variables.CppLibrary), nameof(Variables.CppLibraryForm), nameof(Variables.Configuration), nameof(Variables.EnableModule), nameof(Variables.EnableCustomSysroot), nameof(Variables.CustomSysroot), nameof(Variables.EnableLibcxxCompilation), nameof(Variables.SourceDirectory), nameof(Variables.BuildDirectory), nameof(Variables.XCodeDevelopmentTeam), nameof(Variables.XCodeProvisioningProfileSpecifier), nameof(Variables.VSDir), nameof(Variables.VSVersion), nameof(Variables.XCodeDir), nameof(Variables.LLVM), nameof(Variables.Jdk), nameof(Variables.AndroidSdk), nameof(Variables.AndroidNdk), nameof(Variables.CC), nameof(Variables.CXX), nameof(Variables.AR), nameof(Variables.STRIP), nameof(Variables.CommonFlags), nameof(Variables.CFlags), nameof(Variables.CppFlags), nameof(Variables.LinkerFlags), nameof(Variables.PostLinkerFlags), nameof(Variables.ForceRegenerate), nameof(Variables.EnableNonTargetingOperatingSystemDummy) },
                GetVariableSpec = () =>
                {
                    var b = new Build(Variables.HostOperatingSystem, Variables.HostArchitecture, Variables.TargetOperatingSystem, Variables.TargetArchitecture, Variables.WindowsRuntime, Variables.EnableiOSSimulator, Variables.EnableMacCatalyst, Variables.Toolchain, Variables.Compiler, Variables.CLibrary, Variables.CLibraryForm, Variables.CppLibrary, Variables.CppLibraryForm, Variables.Configuration, Variables.EnableModule, Variables.EnableCustomSysroot, Variables.CustomSysroot, Variables.EnableLibcxxCompilation, Variables.SourceDirectory, Variables.BuildDirectory, Variables.XCodeDevelopmentTeam, Variables.XCodeProvisioningProfileSpecifier, Variables.VSDir, Variables.VSVersion, Variables.XCodeDir, Variables.LLVM, Variables.EnableJava, Variables.Jdk, Variables.AndroidSdk, Variables.AndroidNdk, Variables.CC, Variables.CXX, Variables.AR, Variables.STRIP, Variables.CommonFlags, Variables.CFlags, Variables.CppFlags, Variables.LinkerFlags, Variables.PostLinkerFlags, Variables.ForceRegenerate, Variables.EnableNonTargetingOperatingSystemDummy);
                    Variables.b = b;
                    Projects = b.GetAvailableProjects();
                    var ProjectSet = new HashSet<String>(Projects.Values.Select(t => t.Definition.Name), StringComparer.OrdinalIgnoreCase);
                    return VariableSpec.CreateMultiSelection(new MultiSelectionSpec
                    {
                        DefaultValues = ProjectSet,
                        Selections = ProjectSet,
                        Validator = Parts =>
                        {
                            var Unresolved = b.CheckUnresolvedDependencies(Parts.ToDictionary(Name => Name, Name => Projects[Name]));
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
                        return Variables.b.Execute(Variables.SelectedProjects);
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
        private static Regex rFlag = new Regex(@"([^ ""]|""[^""]*"")+", RegexOptions.ExplicitCapture);
        private static List<String> ParseFlags(String Flags)
        {
            return rFlag.Matches(Flags).Cast<Match>().Select(m => m.Value).ToList();
        }
    }
}
