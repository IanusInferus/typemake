using System;
using System.Collections.Generic;

namespace TypeMake.Cpp
{
    public enum TargetType
    {
        Executable,
        StaticLibrary,
        DynamicLibrary
    }
    public enum ToolchainType
    {
        Windows_VisualC,
        Windows_NDK,
        Linux_gcc,
        Linux_clang,
        Mac_XCode
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
        aarch64
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

    public class File
    {
        public String Path;
        public FileType Type;
    }
    public class Configuration
    {
        public ToolchainType? Toolchain = null;
        public CompilerType? Compiler = null;
        public OperatingSystemType? BuildingOperatingSystem = null;
        public OperatingSystemType? TargetOperatingSystem = null;
        public ConfigurationType? ConfigurationType = null;
        public ArchitectureType? Architecture = null;

        public TargetType? TargetType = null;

        public List<String> IncludeDirectories = new List<String> { };
        public List<KeyValuePair<String, String>> Defines = new List<KeyValuePair<String, String>> { };
        public List<String> CFlags = new List<String> { };
        public List<String> CppFlags = new List<String> { };
        public List<String> ObjectiveCFlags = new List<String> { };
        public List<String> ObjectiveCppFlags = new List<String> { };

        public List<String> LibDirectories = new List<String> { };
        public List<String> Libs = new List<String> { };
        public List<String> LinkerFlags = new List<String> { };

        public List<File> Files = new List<File> { };

        public String BundleIdentifier = null;
    }
    public class Project
    {
        public String Name;
        public List<Configuration> Configurations;
    }
}
