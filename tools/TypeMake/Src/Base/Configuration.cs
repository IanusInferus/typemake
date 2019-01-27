using System;
using System.Collections.Generic;
using System.Linq;

namespace TypeMake.Cpp
{
    public static class ConfigurationUtils
    {
        public static IEnumerable<Configuration> Matches(this IEnumerable<Configuration> Configurations, ToolchainType? Toolchain, CompilerType? Compiler, OperatingSystemType? BuildingOperatingSystem, ArchitectureType? BuildingOperatingSystemArchitecture, OperatingSystemType? TargetOperatingSystem, ConfigurationType? ConfigurationType, ArchitectureType? TargetArchitecture)
        {
            Func<Configuration, bool> Filter = (Configuration c) =>
                ((Toolchain == null) || (c.MatchingToolchains == null) || (c.MatchingToolchains.Contains(Toolchain.Value)))
                && ((Compiler == null) || (c.MatchingCompilers == null) || (c.MatchingCompilers.Contains(Compiler.Value)))
                && ((BuildingOperatingSystem == null) || (c.MatchingBuildingOperatingSystems == null) || (c.MatchingBuildingOperatingSystems.Contains(BuildingOperatingSystem.Value)))
                && ((BuildingOperatingSystemArchitecture == null) || (c.MatchingBuildingOperatingSystemArchitectures == null) || (c.MatchingBuildingOperatingSystemArchitectures.Contains(BuildingOperatingSystemArchitecture.Value)))
                && ((TargetOperatingSystem == null) || (c.MatchingTargetOperatingSystems == null) || (c.MatchingTargetOperatingSystems.Contains(TargetOperatingSystem.Value)))
                && ((TargetArchitecture == null) || (c.MatchingTargetArchitectures == null) || (c.MatchingTargetArchitectures.Contains(TargetArchitecture.Value)))
                && ((ConfigurationType == null) || (c.MatchingConfigurationTypes == null) || (c.MatchingConfigurationTypes.Contains(ConfigurationType.Value)));
            return Configurations.Where(Filter);
        }
        public static Configuration Merged(this IEnumerable<Configuration> Configurations, ToolchainType? Toolchain, CompilerType? Compiler, OperatingSystemType? BuildingOperatingSystem, ArchitectureType? BuildingOperatingSystemArchitecture, OperatingSystemType? TargetOperatingSystem, ConfigurationType? ConfigurationType, ArchitectureType? TargetArchitecture)
        {
            Func<Configuration, bool> Filter = (Configuration c) =>
                ((Toolchain == null) || (c.MatchingToolchains == null) || (c.MatchingToolchains.Contains(Toolchain.Value)))
                && ((Compiler == null) || (c.MatchingCompilers == null) || (c.MatchingCompilers.Contains(Compiler.Value)))
                && ((BuildingOperatingSystem == null) || (c.MatchingBuildingOperatingSystems == null) || (c.MatchingBuildingOperatingSystems.Contains(BuildingOperatingSystem.Value)))
                && ((BuildingOperatingSystemArchitecture == null) || (c.MatchingBuildingOperatingSystemArchitectures == null) || (c.MatchingBuildingOperatingSystemArchitectures.Contains(BuildingOperatingSystemArchitecture.Value)))
                && ((TargetOperatingSystem == null) || (c.MatchingTargetOperatingSystems == null) || (c.MatchingTargetOperatingSystems.Contains(TargetOperatingSystem.Value)))
                && ((TargetArchitecture == null) || (c.MatchingTargetArchitectures == null) || (c.MatchingTargetArchitectures.Contains(TargetArchitecture.Value)))
                && ((ConfigurationType == null) || (c.MatchingConfigurationTypes == null) || (c.MatchingConfigurationTypes.Contains(ConfigurationType.Value)));
            var conf = new Configuration
            {
                MatchingToolchains = Toolchain == null ? null : new List<ToolchainType> { Toolchain.Value },
                MatchingCompilers = Compiler == null ? null : new List<CompilerType> { Compiler.Value },
                MatchingBuildingOperatingSystems = BuildingOperatingSystem == null ? null : new List<OperatingSystemType> { BuildingOperatingSystem.Value },
                MatchingBuildingOperatingSystemArchitectures = BuildingOperatingSystemArchitecture == null ? null : new List<ArchitectureType> { BuildingOperatingSystemArchitecture.Value },
                MatchingTargetOperatingSystems = TargetOperatingSystem == null ? null : new List<OperatingSystemType> { TargetOperatingSystem.Value },
                MatchingTargetArchitectures = TargetArchitecture == null ? null : new List<ArchitectureType> { TargetArchitecture.Value },
                MatchingConfigurationTypes = ConfigurationType == null ? null : new List<ConfigurationType> { ConfigurationType.Value },
                TargetType = Configurations.Where(Filter).Select(c => c.TargetType).Where(t => t != null).LastOrDefault(),
                IncludeDirectories = Configurations.Where(Filter).SelectMany(c => c.IncludeDirectories).ToList(),
                Defines = Configurations.Where(Filter).SelectMany(c => c.Defines).ToList(),
                CFlags = Configurations.Where(Filter).SelectMany(c => c.CFlags).ToList(),
                CppFlags = Configurations.Where(Filter).SelectMany(c => c.CppFlags).ToList(),
                LibDirectories = Configurations.Where(Filter).SelectMany(c => c.LibDirectories).ToList(),
                Libs = Configurations.Where(Filter).SelectMany(c => c.Libs).ToList(),
                LinkerFlags = Configurations.Where(Filter).SelectMany(c => c.LinkerFlags).ToList(),
                Files = Configurations.Where(Filter).SelectMany(c => c.Files).ToList(),
                BundleIdentifier = Configurations.Where(Filter).Select(c => c.BundleIdentifier).Where(v => v != null).LastOrDefault()
            };
            return conf;
        }
    }
}
