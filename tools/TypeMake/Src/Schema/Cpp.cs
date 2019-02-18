using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace TypeMake.Cpp
{
    public enum TargetType
    {
        Executable,
        StaticLibrary,
        DynamicLibrary,
        MacBundle,
        GradleApplication,
        GradleLibrary,
        iOSStaticFramework,
        iOSSharedFramework
    }
    public enum ToolchainType
    {
        Windows_VisualC,
        Mac_XCode,
        CMake,
        Gradle_CMake
    }
    public enum CompilerType
    {
        VisualC,
        gcc,
        clang
    }
    public enum OperatingSystemType
    {
        Windows,
        Linux,
        Mac,
        Android,
        iOS
    }
    public enum ConfigurationType
    {
        Debug,
        Release
    }
    public enum ArchitectureType
    {
        x86,
        x86_64,
        armeabi_v7a,
        arm64_v8a
    }
    public enum FileType
    {
        Unknown,
        Header,
        CSource,
        CppSource,
        ObjectiveCSource,
        ObjectiveCppSource
    }

    [DebuggerDisplay("Path = {Path}, Type = {Type}")]
    public class File
    {
        public PathString Path;
        public FileType Type;
    }
    public class Configuration
    {
        public List<TargetType> MatchingTargetTypes = null;
        public List<ToolchainType> MatchingToolchains = null;
        public List<CompilerType> MatchingCompilers = null;
        public List<OperatingSystemType> MatchingBuildingOperatingSystems = null;
        public List<ArchitectureType> MatchingBuildingOperatingSystemArchitectures = null;
        public List<OperatingSystemType> MatchingTargetOperatingSystems = null;
        public List<ArchitectureType> MatchingTargetArchitectures = null;
        public List<ConfigurationType> MatchingConfigurationTypes = null;

        public List<PathString> IncludeDirectories = new List<PathString> { };
        public List<KeyValuePair<String, String>> Defines = new List<KeyValuePair<String, String>> { };
        public List<String> CFlags = new List<String> { };
        public List<String> CppFlags = new List<String> { };
        public Dictionary<String, String> Options = new Dictionary<String, String> { };

        public List<PathString> LibDirectories = new List<PathString> { };
        public List<PathString> Libs = new List<PathString> { };
        public List<String> LinkerFlags = new List<String> { };

        public List<File> Files = new List<File> { };

        public PathString OutputDirectory = null;
    }

    [DebuggerDisplay("Name = {Name}, TargetName = {TargetName}, TargetType = {TargetType}, Configurations = ...")]
    public class Project
    {
        public String Name;
        public String TargetName = null;
        public TargetType TargetType;
        public List<Configuration> Configurations = new List<Configuration> { };
    }
}
