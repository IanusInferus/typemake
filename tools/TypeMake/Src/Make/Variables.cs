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
        public Cpp.ArchitectureType? TargetArchitecture;
        public Cpp.ToolchainType Toolchain;
        public Cpp.CompilerType Compiler;
        public Cpp.ConfigurationType? Configuration;
        public bool OverwriteRetypemakeScript;
        public bool ForceRegenerate;
        public bool EnablePathCheck;
        public bool EnableNonTargetingOperatingSystemDummy;
        public PathString SourceDirectory;
        public PathString BuildDirectory;
        public String DevelopmentTeam;
        public PathString VSDir;
        public int VSVersion;
        public PathString LLVM;
        public PathString CMake;
        public PathString Make;
        public PathString Ninja;
        public bool EnableJava;
        public PathString Jdk;
        public PathString AndroidSdk;
        public PathString AndroidNdk;
        public String CC;
        public String CXX;
        public String AR;

        public Make m;
        public Func<Make.Result> g;
        public Dictionary<String, Make.ProjectDescription> SelectedProjects;

        public bool BuildNow;
    }
}
