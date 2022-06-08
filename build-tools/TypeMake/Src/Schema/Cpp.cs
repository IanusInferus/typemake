using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace TypeMake.Cpp
{
    public enum TargetType
    {
        Executable,
        StaticLibrary,
        IntermediateStaticLibrary,
        DynamicLibrary,
        DarwinApplication,
        DarwinStaticFramework,
        DarwinSharedFramework,
        MacBundle,
        GradleApplication,
        GradleLibrary
    }
    public enum OperatingSystemType
    {
        Windows,
        Linux,
        MacOS,
        Android,
        iOS
    }
    public enum ArchitectureType
    {
        x86,
        x64,
        armv7a,
        arm64,
        riscv64,
        Unknown
    }
    public enum WindowsRuntimeType
    {
        Win32,
        WinRT
    }
    public enum ToolchainType
    {
        VisualStudio,
        XCode,
        Ninja,
        Gradle_Ninja
    }
    public enum CompilerType
    {
        VisualCpp,
        gcc,
        clang,
        clangcl
    }
    public enum CLibraryType
    {
        VisualCRuntime,
        glibc,
        musl,
        libSystem,
        Bionic
    }
    public enum CLibraryForm
    {
        Static,
        Dynamic
    }
    public enum CppLibraryType
    {
        VisualCppRuntime,
        libstdcxx,
        libcxx
    }
    public enum CppLibraryForm
    {
        Static,
        Dynamic
    }
    public enum ConfigurationType
    {
        Debug,
        Release
    }
    public enum FileType
    {
        Unknown,
        Header,
        CSource,
        CppSource,
        ObjectiveCSource,
        ObjectiveCppSource,
        EmbeddedContent,
        NatVis
    }

    public class Configuration
    {
        public List<TargetType> MatchingTargetTypes = null;
        public List<OperatingSystemType> MatchingHostOperatingSystems = null;
        public List<ArchitectureType> MatchingHostArchitectures = null;
        public List<OperatingSystemType> MatchingTargetOperatingSystems = null;
        public List<ArchitectureType> MatchingTargetArchitectures = null;
        public List<WindowsRuntimeType> MatchingWindowsRuntimes = null;
        public List<ToolchainType> MatchingToolchains = null;
        public List<CompilerType> MatchingCompilers = null;
        public List<CLibraryType> MatchingCLibraries = null;
        public List<CLibraryForm> MatchingCLibraryForms = null;
        public List<CppLibraryType> MatchingCppLibraries = null;
        public List<CppLibraryForm> MatchingCppLibraryForms = null;
        public List<ConfigurationType> MatchingConfigurationTypes = null;

        public List<PathString> IncludeDirectories = new List<PathString> { };
        public List<PathString> SystemIncludeDirectories = new List<PathString> { };
        public List<KeyValuePair<String, String>> Defines = new List<KeyValuePair<String, String>> { };
        public List<String> CommonFlags = new List<String> { };
        public List<String> CFlags = new List<String> { };
        public List<String> CppFlags = new List<String> { };
        public Dictionary<String, String> Options = new Dictionary<String, String> { };

        public List<PathString> LibDirectories = new List<PathString> { };
        public List<PathString> Libs = new List<PathString> { };
        public List<String> LinkerFlags = new List<String> { };
        public List<String> PostLinkerFlags = new List<String> { };

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

    [DebuggerDisplay("Id = {Id}, Name = {Name}, TargetName = {TargetName}, VirtualDir = {VirtualDir}, FilePath = {FilePath}, TargetType = {TargetType}, Configurations = ...")]
    public class Project
    {
        public String Id;
        public String Name;
        public PathString VirtualDir;
        public PathString FilePath;
        public TargetType TargetType;
        public String TargetName = null;
        public List<Configuration> Configurations = new List<Configuration> { };
    }

    [DebuggerDisplay("Id = {Id}, Name = {Name}, VirtualDir = {VirtualDir}, FilePath = {FilePath}")]
    public class ProjectReference
    {
        public String Id;
        public String Name;
        public PathString VirtualDir;
        public PathString FilePath;
        public TargetType TargetType;
        public String TargetName;
        public Dictionary<ConfigurationType, PathString> OutputFilePath = new Dictionary<ConfigurationType, PathString> { };
    }
}
