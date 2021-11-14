using System;
using System.Collections.Generic;

namespace TypeMake
{
    public class Variables
    {
        public Cpp.OperatingSystemType HostOperatingSystem;
        public Cpp.ArchitectureType HostArchitecture;
        public Cpp.OperatingSystemType TargetOperatingSystem;
        public String TargetOperatingSystemDistribution;
        public Cpp.ArchitectureType TargetArchitecture;
        public Cpp.WindowsRuntimeType? WindowsRuntime;
        public bool EnableiOSSimulator;
        public bool EnableMacCatalyst;
        public Cpp.ToolchainType Toolchain;
        public Cpp.CompilerType Compiler;
        public Cpp.CLibraryType CLibrary;
        public Cpp.CLibraryForm CLibraryForm;
        public Cpp.CppLibraryType CppLibrary;
        public Cpp.CppLibraryForm CppLibraryForm;
        public Cpp.ConfigurationType Configuration;
        public bool EnableCustomSysroot;
        public PathString CustomSysroot;
        public bool OverwriteRetypemakeScript;
        public bool ForceRegenerate;
        public bool EnablePathCheck;
        public bool EnableNonTargetingOperatingSystemDummy;
        public PathString SourceDirectory;
        public PathString BuildDirectory;
        public int MaxProcessCount;
        public String XCodeDevelopmentTeam;
        public String XCodeProvisioningProfileSpecifier;
        public PathString VSDir;
        public int VSVersion;
        public PathString LLVM;
        public PathString Ninja;
        public bool EnableJava;
        public PathString Jdk;
        public PathString AndroidSdk;
        public PathString AndroidNdk;
        public String CC;
        public String CXX;
        public String AR;
        public String STRIP;
        public bool EnableAdditionalFlags;
        public List<String> CommonFlags;
        public List<String> CFlags;
        public List<String> CppFlags;
        public List<String> LinkerFlags;
        public List<String> PostLinkerFlags;

        public Build b;
        public Func<Build.Result> g;
        public Dictionary<String, Build.ProjectDescription> SelectedProjects;

        public bool BuildNow;
    }
}
