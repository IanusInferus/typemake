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
        MacApplication,
        MacBundle,
        GradleApplication,
        GradleLibrary,
        iOSApplication,
        iOSStaticFramework,
        iOSSharedFramework
    }
    public enum ToolchainType
    {
        Windows_VisualC,
        Mac_XCode,
        CMake,
        Ninja,
        Gradle_CMake,
        Gradle_Ninja
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
        x64,
        armv7a,
        arm64
    }
    public enum FileType
    {
        Unknown,
        Header,
        CSource,
        CppSource,
        ObjectiveCSource,
        ObjectiveCppSource,
        EmbeddedContent
    }

    public class Configuration
    {
        public List<TargetType> MatchingTargetTypes = null;
        public List<ToolchainType> MatchingToolchains = null;
        public List<CompilerType> MatchingCompilers = null;
        public List<OperatingSystemType> MatchingHostOperatingSystems = null;
        public List<ArchitectureType> MatchingHostArchitectures = null;
        public List<OperatingSystemType> MatchingTargetOperatingSystems = null;
        public List<ArchitectureType> MatchingTargetArchitectures = null;
        public List<ConfigurationType> MatchingConfigurationTypes = null;

        public List<PathString> IncludeDirectories = new List<PathString> { };
        public List<KeyValuePair<String, String>> Defines = new List<KeyValuePair<String, String>> { };
        public List<String> CommonFlags = new List<String> { };
        public List<String> CFlags = new List<String> { };
        public List<String> CppFlags = new List<String> { };
        public Dictionary<String, String> Options = new Dictionary<String, String> { };

        public List<PathString> LibDirectories = new List<PathString> { };
        public List<PathString> Libs = new List<PathString> { };
        public List<String> LinkerFlags = new List<String> { };

        public List<File> Files = new List<File> { };

        public PathString OutputDirectory = null;
    }

    [DebuggerDisplay("Path = {Path}, Type = {Type}, IsExported = {IsExported}")]
    public class File
    {
        public PathString Path;
        public FileType Type = FileType.Unknown;
        public bool IsExported = false;
        public List<Configuration> Configurations = new List<Configuration> { };
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
