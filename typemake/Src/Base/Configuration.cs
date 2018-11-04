using System;
using System.Collections.Generic;
using System.Linq;

namespace TypeMake.Cpp
{
    public static class ConfigurationUtils
    {
        public static Configuration GetMergedConfiguration(ToolchainType? Toolchain, CompilerType? Compiler, OperatingSystemType? BuildingOperatingSystem, ArchitectureType? BuildingOperatingSystemArchitecture, OperatingSystemType? TargetOperatingSystem, ConfigurationType? ConfigurationType, ArchitectureType? TargetArchitecture, List<Configuration> Configurations)
        {
            Func<Configuration, bool> Filter = (Configuration c) =>
                ((c.Toolchain == null) || (c.Toolchain == Toolchain))
                && ((c.Compiler == null) || (c.Compiler == Compiler))
                && ((c.BuildingOperatingSystem == null) || (c.BuildingOperatingSystem == BuildingOperatingSystem))
                && ((c.BuildingOperatingSystemArchitecture == null) || (c.BuildingOperatingSystemArchitecture == BuildingOperatingSystemArchitecture))
                && ((c.TargetOperatingSystem == null) || (c.TargetOperatingSystem == TargetOperatingSystem))
                && ((c.TargetArchitecture == null) || (c.TargetArchitecture == TargetArchitecture))
                && ((c.ConfigurationType == null) || (c.ConfigurationType == ConfigurationType));
            var conf = new Configuration
            {
                Toolchain = Toolchain,
                Compiler = Compiler,
                BuildingOperatingSystem = BuildingOperatingSystem,
                BuildingOperatingSystemArchitecture = BuildingOperatingSystemArchitecture,
                TargetOperatingSystem = TargetOperatingSystem,
                TargetArchitecture = TargetArchitecture,
                ConfigurationType = ConfigurationType,
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
