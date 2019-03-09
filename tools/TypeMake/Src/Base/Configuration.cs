using System;
using System.Collections.Generic;
using System.Linq;

namespace TypeMake.Cpp
{
    public static class ConfigurationUtils
    {
        public static IEnumerable<Configuration> Matches(this IEnumerable<Configuration> Configurations, TargetType? TargetType, ToolchainType? Toolchain, CompilerType? Compiler, OperatingSystemType? BuildingOperatingSystem, ArchitectureType? BuildingOperatingSystemArchitecture, OperatingSystemType? TargetOperatingSystem, ArchitectureType? TargetArchitecture, ConfigurationType? ConfigurationType)
        {
            Func<Configuration, bool> Filter = (Configuration c) =>
                ((TargetType == null) || (c.MatchingTargetTypes == null) || (c.MatchingTargetTypes.Contains(TargetType.Value)))
                && ((Toolchain == null) || (c.MatchingToolchains == null) || (c.MatchingToolchains.Contains(Toolchain.Value)))
                && ((Compiler == null) || (c.MatchingCompilers == null) || (c.MatchingCompilers.Contains(Compiler.Value)))
                && ((BuildingOperatingSystem == null) || (c.MatchingBuildingOperatingSystems == null) || (c.MatchingBuildingOperatingSystems.Contains(BuildingOperatingSystem.Value)))
                && ((BuildingOperatingSystemArchitecture == null) || (c.MatchingBuildingOperatingSystemArchitectures == null) || (c.MatchingBuildingOperatingSystemArchitectures.Contains(BuildingOperatingSystemArchitecture.Value)))
                && ((TargetOperatingSystem == null) || (c.MatchingTargetOperatingSystems == null) || (c.MatchingTargetOperatingSystems.Contains(TargetOperatingSystem.Value)))
                && ((TargetArchitecture == null) || (c.MatchingTargetArchitectures == null) || (c.MatchingTargetArchitectures.Contains(TargetArchitecture.Value)))
                && ((ConfigurationType == null) || (c.MatchingConfigurationTypes == null) || (c.MatchingConfigurationTypes.Contains(ConfigurationType.Value)));
            return Configurations.Where(Filter);
        }
        public static Configuration Merged(this IEnumerable<Configuration> Configurations, TargetType? TargetType, ToolchainType? Toolchain, CompilerType? Compiler, OperatingSystemType? BuildingOperatingSystem, ArchitectureType? BuildingOperatingSystemArchitecture, OperatingSystemType? TargetOperatingSystem, ArchitectureType? TargetArchitecture, ConfigurationType? ConfigurationType)
        {
            var Matched = Configurations.Matches(TargetType, Toolchain, Compiler, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem, TargetArchitecture, ConfigurationType).ToList();
            var conf = new Configuration
            {
                MatchingTargetTypes = Toolchain == null ? null : new List<TargetType> { TargetType.Value },
                MatchingToolchains = Toolchain == null ? null : new List<ToolchainType> { Toolchain.Value },
                MatchingCompilers = Compiler == null ? null : new List<CompilerType> { Compiler.Value },
                MatchingBuildingOperatingSystems = BuildingOperatingSystem == null ? null : new List<OperatingSystemType> { BuildingOperatingSystem.Value },
                MatchingBuildingOperatingSystemArchitectures = BuildingOperatingSystemArchitecture == null ? null : new List<ArchitectureType> { BuildingOperatingSystemArchitecture.Value },
                MatchingTargetOperatingSystems = TargetOperatingSystem == null ? null : new List<OperatingSystemType> { TargetOperatingSystem.Value },
                MatchingTargetArchitectures = TargetArchitecture == null ? null : new List<ArchitectureType> { TargetArchitecture.Value },
                MatchingConfigurationTypes = ConfigurationType == null ? null : new List<ConfigurationType> { ConfigurationType.Value },
                IncludeDirectories = Matched.SelectMany(c => c.IncludeDirectories).Distinct().ToList(),
                Defines = Matched.SelectMany(c => c.Defines).ToList(),
                CFlags = Matched.SelectMany(c => c.CFlags).ToList(),
                CppFlags = Matched.SelectMany(c => c.CppFlags).ToList(),
                Options = Matched.SelectMany(c => c.Options).ToDictionary(p => p.Key, p => p.Value),
                LibDirectories = Matched.SelectMany(c => c.LibDirectories).Distinct().ToList(),
                Libs = Matched.SelectMany(c => c.Libs).Distinct().ToList(),
                LinkerFlags = Matched.SelectMany(c => c.LinkerFlags).ToList(),
                Files = Matched.SelectMany(c => c.Files).ToList(),
                OutputDirectory = Matched.Select(c => c.OutputDirectory).Where(v => v != null).LastOrDefault()
            };
            return conf;
        }
    }
}
