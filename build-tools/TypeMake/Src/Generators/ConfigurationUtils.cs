using System;
using System.Collections.Generic;
using System.Linq;

namespace TypeMake.Cpp
{
    public static class ConfigurationUtils
    {
        public static IEnumerable<Configuration> Matches(this IEnumerable<Configuration> Configurations, TargetType? TargetType, OperatingSystemType? HostOperatingSystem, ArchitectureType? HostArchitecture, OperatingSystemType? TargetOperatingSystem, ArchitectureType? TargetArchitecture, WindowsRuntimeType? WindowsRuntime, ToolchainType? Toolchain, CompilerType? Compiler, CLibraryType? CLibrary, CLibraryForm? CLibraryForm, CppLibraryType? CppLibrary, CppLibraryForm? CppLibraryForm, ConfigurationType? ConfigurationType)
        {
            Func<Configuration, bool> Filter = (Configuration c) =>
                ((TargetType == null) || (c.MatchingTargetTypes == null) || (c.MatchingTargetTypes.Contains(TargetType.Value)))
                && ((HostOperatingSystem == null) || (c.MatchingHostOperatingSystems == null) || (c.MatchingHostOperatingSystems.Contains(HostOperatingSystem.Value)))
                && ((HostArchitecture == null) || (c.MatchingHostArchitectures == null) || (c.MatchingHostArchitectures.Contains(HostArchitecture.Value)))
                && ((TargetOperatingSystem == null) || (c.MatchingTargetOperatingSystems == null) || (c.MatchingTargetOperatingSystems.Contains(TargetOperatingSystem.Value)))
                && ((TargetArchitecture == null) || (c.MatchingTargetArchitectures == null) || (c.MatchingTargetArchitectures.Contains(TargetArchitecture.Value)))
                && ((WindowsRuntime == null) || (c.MatchingWindowsRuntimes == null) || (c.MatchingWindowsRuntimes.Contains(WindowsRuntime.Value)))
                && ((Toolchain == null) || (c.MatchingToolchains == null) || (c.MatchingToolchains.Contains(Toolchain.Value)))
                && ((Compiler == null) || (c.MatchingCompilers == null) || (c.MatchingCompilers.Contains(Compiler.Value)))
                && ((CLibrary == null) || (c.MatchingCLibraries == null) || (c.MatchingCLibraries.Contains(CLibrary.Value)))
                && ((CLibraryForm == null) || (c.MatchingCLibraryForms == null) || (c.MatchingCLibraryForms.Contains(CLibraryForm.Value)))
                && ((CppLibrary == null) || (c.MatchingCppLibraries == null) || (c.MatchingCppLibraries.Contains(CppLibrary.Value)))
                && ((CppLibraryForm == null) || (c.MatchingCppLibraryForms == null) || (c.MatchingCppLibraryForms.Contains(CppLibraryForm.Value)))
                && ((ConfigurationType == null) || (c.MatchingConfigurationTypes == null) || (c.MatchingConfigurationTypes.Contains(ConfigurationType.Value)));
            return Configurations.Where(Filter);
        }
        public static IEnumerable<Configuration> StrictMatches(this IEnumerable<Configuration> Configurations, TargetType? TargetType, OperatingSystemType? HostOperatingSystem, ArchitectureType? HostArchitecture, OperatingSystemType? TargetOperatingSystem, ArchitectureType? TargetArchitecture, WindowsRuntimeType? WindowsRuntime, ToolchainType? Toolchain, CompilerType? Compiler, CLibraryType? CLibrary, CLibraryForm? CLibraryForm, CppLibraryType? CppLibrary, CppLibraryForm? CppLibraryForm, ConfigurationType? ConfigurationType)
        {
            Func<Configuration, bool> Filter = (Configuration c) =>
                ((c.MatchingTargetTypes == null) || ((TargetType != null) && c.MatchingTargetTypes.Contains(TargetType.Value)))
                && ((c.MatchingHostOperatingSystems == null) || ((HostOperatingSystem != null) && c.MatchingHostOperatingSystems.Contains(HostOperatingSystem.Value)))
                && ((c.MatchingHostArchitectures == null) || ((HostArchitecture != null) && c.MatchingHostArchitectures.Contains(HostArchitecture.Value)))
                && ((c.MatchingTargetOperatingSystems == null) || ((TargetOperatingSystem != null) && c.MatchingTargetOperatingSystems.Contains(TargetOperatingSystem.Value)))
                && ((c.MatchingTargetArchitectures == null) || ((TargetArchitecture != null) && c.MatchingTargetArchitectures.Contains(TargetArchitecture.Value)))
                && ((c.MatchingWindowsRuntimes == null) || ((WindowsRuntime != null) && c.MatchingWindowsRuntimes.Contains(WindowsRuntime.Value)))
                && ((c.MatchingToolchains == null) || ((Toolchain != null) && c.MatchingToolchains.Contains(Toolchain.Value)))
                && ((c.MatchingCompilers == null) || ((Compiler != null) && c.MatchingCompilers.Contains(Compiler.Value)))
                && ((c.MatchingCLibraries == null) || ((CLibrary != null) && c.MatchingCLibraries.Contains(CLibrary.Value)))
                && ((c.MatchingCLibraryForms == null) || ((CLibraryForm != null) && c.MatchingCLibraryForms.Contains(CLibraryForm.Value)))
                && ((c.MatchingCppLibraries == null) || ((CppLibrary != null) && c.MatchingCppLibraries.Contains(CppLibrary.Value)))
                && ((c.MatchingCppLibraryForms == null) || ((CppLibraryForm != null) && c.MatchingCppLibraryForms.Contains(CppLibraryForm.Value)))
                && ((c.MatchingConfigurationTypes == null) || ((ConfigurationType != null) && c.MatchingConfigurationTypes.Contains(ConfigurationType.Value)));
            return Configurations.Where(Filter);
        }
        public static Configuration Merged(this IEnumerable<Configuration> Configurations, TargetType? TargetType, OperatingSystemType? HostOperatingSystem, ArchitectureType? HostArchitecture, OperatingSystemType? TargetOperatingSystem, ArchitectureType? TargetArchitecture, WindowsRuntimeType? WindowsRuntime, ToolchainType? Toolchain, CompilerType? Compiler, CLibraryType? CLibrary, CLibraryForm? CLibraryForm, CppLibraryType? CppLibrary, CppLibraryForm? CppLibraryForm, ConfigurationType? ConfigurationType)
        {
            var Matched = Configurations.StrictMatches(TargetType, HostOperatingSystem, HostArchitecture, TargetOperatingSystem, TargetArchitecture, WindowsRuntime, Toolchain, Compiler, CLibrary, CLibraryForm, CppLibrary, CppLibraryForm, ConfigurationType).ToList();
            var conf = new Configuration
            {
                MatchingTargetTypes = Toolchain == null ? null : new List<TargetType> { TargetType.Value },
                MatchingHostOperatingSystems = HostOperatingSystem == null ? null : new List<OperatingSystemType> { HostOperatingSystem.Value },
                MatchingHostArchitectures = HostArchitecture == null ? null : new List<ArchitectureType> { HostArchitecture.Value },
                MatchingTargetOperatingSystems = TargetOperatingSystem == null ? null : new List<OperatingSystemType> { TargetOperatingSystem.Value },
                MatchingTargetArchitectures = TargetArchitecture == null ? null : new List<ArchitectureType> { TargetArchitecture.Value },
                MatchingWindowsRuntimes = WindowsRuntime == null ? null : new List<WindowsRuntimeType> { WindowsRuntime.Value },
                MatchingConfigurationTypes = ConfigurationType == null ? null : new List<ConfigurationType> { ConfigurationType.Value },
                MatchingToolchains = Toolchain == null ? null : new List<ToolchainType> { Toolchain.Value },
                MatchingCompilers = Compiler == null ? null : new List<CompilerType> { Compiler.Value },
                MatchingCLibraries = CLibrary == null ? null : new List<CLibraryType> { CLibrary.Value },
                MatchingCLibraryForms = CLibrary == null ? null : new List<CLibraryForm> { CLibraryForm.Value },
                MatchingCppLibraries = CppLibrary == null ? null : new List<CppLibraryType> { CppLibrary.Value },
                MatchingCppLibraryForms = CppLibraryForm == null ? null : new List<CppLibraryForm> { CppLibraryForm.Value },
                IncludeDirectories = Matched.SelectMany(c => c.IncludeDirectories).Distinct().ToList(),
                SystemIncludeDirectories = Matched.SelectMany(c => c.SystemIncludeDirectories).Distinct().ToList(),
                Defines = Matched.SelectMany(c => c.Defines).ToList(),
                CommonFlags = Matched.SelectMany(c => c.CommonFlags).ToList(),
                CFlags = Matched.SelectMany(c => c.CFlags).ToList(),
                CppFlags = Matched.SelectMany(c => c.CppFlags).ToList(),
                Options = Matched.SelectMany(c => c.Options).GroupBy(p => p.Key).Select(g => g.Last()).ToDictionary(p => p.Key, p => p.Value),
                LibDirectories = Matched.SelectMany(c => c.LibDirectories).Distinct().ToList(),
                Libs = Matched.SelectMany(c => c.Libs).Distinct().ToList(),
                LinkerFlags = Matched.SelectMany(c => c.LinkerFlags).ToList(),
                PostLinkerFlags = Matched.SelectMany(c => c.PostLinkerFlags).ToList(),
                Files = Matched.SelectMany(c => c.Files).ToList(),
                OutputDirectory = Matched.Select(c => c.OutputDirectory).Where(v => v != null).LastOrDefault()
            };
            return conf;
        }
    }
}
