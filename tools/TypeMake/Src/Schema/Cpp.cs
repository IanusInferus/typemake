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
        GradleApplication,
        GradleLibrary
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
        public ToolchainType? Toolchain = null;
        public CompilerType? Compiler = null;
        public OperatingSystemType? BuildingOperatingSystem = null;
        public ArchitectureType? BuildingOperatingSystemArchitecture = null;
        public OperatingSystemType? TargetOperatingSystem = null;
        public ArchitectureType? TargetArchitecture = null;
        public ConfigurationType? ConfigurationType = null;

        public TargetType? TargetType = null;

        public List<PathString> IncludeDirectories = new List<PathString> { };
        public List<KeyValuePair<String, String>> Defines = new List<KeyValuePair<String, String>> { };
        public List<String> CFlags = new List<String> { };
        public List<String> CppFlags = new List<String> { };

        public List<PathString> LibDirectories = new List<PathString> { };
        public List<PathString> Libs = new List<PathString> { };
        public List<String> LinkerFlags = new List<String> { };

        public List<File> Files = new List<File> { };

        public String BundleIdentifier = null;
    }

    [DebuggerDisplay("Name = {Name}, TargetName = {TargetName}, ApplicationIdentifier = {ApplicationIdentifier}, Configurations = ...")]
    public class Project
    {
        public String Name;
        public String TargetName;
        public String ApplicationIdentifier;
        public List<Configuration> Configurations;
    }
}
