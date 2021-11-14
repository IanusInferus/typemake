using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TypeMake.Cpp;

namespace TypeMake
{
    public class Build
    {
        public const String SolutionName = "TypeMakeSample";

        private OperatingSystemType HostOperatingSystem;
        private ArchitectureType HostArchitecture;
        private OperatingSystemType TargetOperatingSystem;
        private ArchitectureType TargetArchitecture;
        private WindowsRuntimeType? WindowsRuntime;
        private bool EnableiOSSimulator;
        private bool EnableMacCatalyst;
        private ToolchainType Toolchain;
        private CompilerType Compiler;
        private CLibraryType CLibrary;
        private CLibraryForm CLibraryForm;
        private CppLibraryType CppLibrary;
        private CppLibraryForm CppLibraryForm;
        private ConfigurationType ConfigurationType;
        private bool EnableCustomSysroot;
        private PathString CustomSysroot;
        private bool EnableLibcxxCompilation;
        private PathString SourceDirectory;
        private PathString BuildDirectory;
        private String XCodeDevelopmentTeam;
        private String XCodeProvisioningProfileSpecifier;
        private PathString VSDir;
        private int VSVersion;
        private bool EnableJava;
        private PathString Jdk;
        private PathString AndroidSdk;
        private PathString AndroidNdk;
        private String CC;
        private String CXX;
        private String AR;
        private String STRIP;
        private List<String> CommonFlags;
        private List<String> CFlags;
        private List<String> CppFlags;
        private List<String> LinkerFlags;
        private List<String> PostLinkerFlags;
        private bool ForceRegenerate;
        private bool EnableNonTargetingOperatingSystemDummy;

        private Dictionary<String, String> ProjectIds = new Dictionary<String, String>();

        public Build(OperatingSystemType HostOperatingSystem, ArchitectureType HostArchitecture, OperatingSystemType TargetOperatingSystem, ArchitectureType TargetArchitecture, WindowsRuntimeType? WindowsRuntime, bool EnableiOSSimulator, bool EnableMacCatalyst, ToolchainType Toolchain, CompilerType Compiler, CLibraryType CLibrary, CLibraryForm CLibraryForm, CppLibraryType CppLibrary, CppLibraryForm CppLibraryForm, ConfigurationType ConfigurationType, bool EnableCustomSysroot, PathString CustomSysroot, bool EnableLibcxxCompilation, PathString SourceDirectory, PathString BuildDirectory, String XCodeDevelopmentTeam, String XCodeProvisioningProfileSpecifier, PathString VSDir, int VSVersion, bool EnableJava, PathString Jdk, PathString AndroidSdk, PathString AndroidNdk, String CC, String CXX, String AR, String STRIP, List<String> CommonFlags, List<String> CFlags, List<String> CppFlags, List<String> LinkerFlags, List<String> PostLinkerFlags, bool ForceRegenerate, bool EnableNonTargetingOperatingSystemDummy)
        {
            this.HostOperatingSystem = HostOperatingSystem;
            this.HostArchitecture = HostArchitecture;
            this.TargetOperatingSystem = TargetOperatingSystem;
            this.TargetArchitecture = TargetArchitecture;
            this.WindowsRuntime = WindowsRuntime;
            this.EnableiOSSimulator = EnableiOSSimulator;
            this.EnableMacCatalyst = EnableMacCatalyst;
            this.Toolchain = Toolchain;
            this.Compiler = Compiler;
            this.CLibrary = CLibrary;
            this.CLibraryForm = CLibraryForm;
            this.CppLibrary = CppLibrary;
            this.CppLibraryForm = CppLibraryForm;
            this.ConfigurationType = ConfigurationType;
            this.EnableCustomSysroot = EnableCustomSysroot;
            this.CustomSysroot = CustomSysroot;
            this.EnableLibcxxCompilation = EnableLibcxxCompilation;
            this.SourceDirectory = SourceDirectory.FullPath;
            this.BuildDirectory = BuildDirectory.FullPath;
            this.XCodeDevelopmentTeam = XCodeDevelopmentTeam;
            this.XCodeProvisioningProfileSpecifier = XCodeProvisioningProfileSpecifier;
            this.VSDir = VSDir;
            this.VSVersion = VSVersion;
            this.EnableJava = EnableJava;
            this.Jdk = Jdk;
            this.AndroidSdk = AndroidSdk;
            this.AndroidNdk = AndroidNdk;
            this.CC = CC;
            this.CXX = CXX;
            this.AR = AR;
            this.STRIP = STRIP;
            this.CommonFlags = CommonFlags;
            this.CFlags = CFlags;
            this.CppFlags = CppFlags;
            this.LinkerFlags = LinkerFlags;
            this.PostLinkerFlags = PostLinkerFlags;
            this.ForceRegenerate = ForceRegenerate;
            this.EnableNonTargetingOperatingSystemDummy = EnableNonTargetingOperatingSystemDummy;
        }

        public class ProjectDescription
        {
            public Project Definition;
            public List<Configuration> BaseConfigurations;
            public List<Configuration> ExportConfigurations;

            public PathString PhysicalPath;
            public HashSet<String> DependentProjectToRequirement;
        }
        public Dictionary<String, ProjectDescription> GetAvailableProjects()
        {
            var Dependencies = new Dictionary<String, HashSet<String>>();

            void Add(String Depender, params String[] Dependees)
            {
                foreach (var Dependee in Dependees)
                {
                    if (Dependencies.ContainsKey(Depender))
                    {
                        var d = Dependencies[Depender];
                        if (!d.Contains(Dependee))
                        {
                            d.Add(Dependee);
                        }
                    }
                    else
                    {
                        Dependencies.Add(Depender, new HashSet<String> { Dependee });
                    }
                }
            }

            var Modules = FileSystemUtils.GetDirectories(SourceDirectory / "modules", "*", SearchOption.TopDirectoryOnly).Select(p => new { ModuleName = p.FileName, ModulePath = p, VirtualDir = "modules" }).ToList();
            var Products = FileSystemUtils.GetDirectories(SourceDirectory / "products", "*", SearchOption.TopDirectoryOnly).Select(p => new { ProductName = p.FileName, ProductPath = p, VirtualDir = "products" }).ToList();

            if ((CLibrary == CLibraryType.musl) && (CLibraryForm == CLibraryForm.Static))
            {
                Products = Products.Where(p => p.ProductName != "hello_dyn").ToList();
            }

            var ModuleDict = Modules.ToDictionary(m => m.ModuleName);

            //modules
            Add("math", "core");

            //products
            Add("basic.static", "math");
            Add("standard.dynamic", "math");
            Add("hello", "math");
            Add("hello.ios", "math");
            Add("hello.android", "math");
            Add("hello_dyn", "standard.dynamic");

            var Projects = new List<ProjectDescription>();

            if (EnableLibcxxCompilation)
            {
                var ModuleName = "libcxx";
                var InputDirectory = BuildDirectory.Parent / "lib" / ModuleName / "generic";
                var DependentModuleToRequirement = Dependencies.ContainsKey(ModuleName) ? Dependencies[ModuleName] : new HashSet<String>();

                var libcxxDirs = FileSystemUtils.GetDirectories(InputDirectory, "libcxx-*.src", SearchOption.TopDirectoryOnly).ToList();
                var libcxxabiDirs = FileSystemUtils.GetDirectories(InputDirectory, "libcxxabi-*.src", SearchOption.TopDirectoryOnly).ToList();
                if (libcxxDirs.Count == 0)
                {
                    throw new InvalidOperationException("SourceNotFound: libcxx");
                }
                if (libcxxabiDirs.Count == 0)
                {
                    throw new InvalidOperationException("SourceNotFound: libcxxabi");
                }
                if (libcxxDirs.Count != 1)
                {
                    throw new InvalidOperationException("SourceAmbiguity: libcxx");
                }
                if (libcxxabiDirs.Count != 1)
                {
                    throw new InvalidOperationException("SourceAmbiguity: libcxxabi");
                }
                var libcxxDir = libcxxDirs.Single();
                var libcxxabiDir = libcxxabiDirs.Single();
                var libcxxSources = GetFilesInDirectory(libcxxDir / "include", TargetOperatingSystem, true).Concat(GetFilesInDirectory(libcxxDir / "src", TargetOperatingSystem, true).Where(f => !f.Path.In(libcxxDir / "src/support") || f.Path.In(libcxxDir / "src/support/runtime"))).ToList();
                var libcxxabiSources = GetFilesInDirectory(libcxxabiDir / "include", TargetOperatingSystem, true).Concat(GetFilesInDirectory(libcxxabiDir / "src", TargetOperatingSystem, true)).ToList();

                Projects.Add(new ProjectDescription
                {
                    Definition = new Project
                    {
                        Id = GetIdForProject(ModuleName),
                        Name = ModuleName,
                        VirtualDir = "lib",
                        FilePath = BuildDirectory / "projects" / GetProjectFileName(ModuleName),
                        TargetType = TargetType.StaticLibrary,
                        Configurations = new List<Configuration>
                        {
                            new Configuration
                            {
                                MatchingTargetOperatingSystems = new List<OperatingSystemType> { OperatingSystemType.MacOS, OperatingSystemType.iOS },
                                IncludeDirectories = new List<PathString> { libcxxDir / "include" },
                                Defines = ParseDefines("_LIBCPP_BUILDING_LIBRARY;_LIBCPP_BUILDING_HAS_NO_ABI_LIBRARY;_LIBCPP_DISABLE_VISIBILITY_ANNOTATIONS;_LIBCPP_DISABLE_AVAILABILITY;_LIBCPP_HIDDEN=__attribute__ ((__visibility__(\"hidden\")))"),
                                CppFlags = new List<String> { "-nostdinc++" },
                                Files = libcxxSources
                            },
                            new Configuration
                            {
                                MatchingTargetOperatingSystems = new List<OperatingSystemType> { OperatingSystemType.Linux },
                                IncludeDirectories = new List<PathString> { libcxxabiDir / "include", libcxxDir / "include" },
                                Defines = ParseDefines("_LIBCPP_BUILDING_LIBRARY;LIBCXX_BUILDING_LIBCXXABI;_LIBCPP_DISABLE_VISIBILITY_ANNOTATIONS;_LIBCPP_DISABLE_AVAILABILITY;_LIBCPP_HIDDEN=__attribute__ ((__visibility__(\"hidden\")))"),
                                CppFlags = new List<String> { "-nostdinc++" },
                                Files = libcxxSources.Concat(libcxxabiSources).ToList()
                            },
                            new Configuration
                            {
                                MatchingCLibraries = new List<CLibraryType> { CLibraryType.musl },
                                Defines = ParseDefines("_LIBCPP_HAS_MUSL_LIBC")
                            }
                        }
                    },
                    BaseConfigurations = GetCommonConfigurations(),
                    ExportConfigurations = new List<Configuration>
                    {
                        new Configuration
                        {
                            MatchingTargetOperatingSystems = new List<OperatingSystemType> { OperatingSystemType.MacOS, OperatingSystemType.iOS },
                            MatchingCppLibraryForms = new List<CppLibraryForm> { CppLibraryForm.Static },
                            IncludeDirectories = new List<PathString> { libcxxDir / "include" },
                            Defines = ParseDefines("_LIBCPP_DISABLE_VISIBILITY_ANNOTATIONS;_LIBCPP_DISABLE_AVAILABILITY;_LIBCPP_HIDDEN=__attribute__ ((__visibility__(\"hidden\")))"),
                            CppFlags = new List<String> { "-nostdinc++" },
                            LinkerFlags = new List<String> { "-nostdlib++", "-lc++abi" }
                        },
                        new Configuration
                        {
                            MatchingTargetOperatingSystems = new List<OperatingSystemType> { OperatingSystemType.Linux },
                            MatchingCppLibraries = new List<CppLibraryType> { CppLibraryType.libcxx },
                            MatchingCppLibraryForms = new List<CppLibraryForm> { CppLibraryForm.Static },
                            IncludeDirectories = new List<PathString> { libcxxabiDir / "include", libcxxDir / "include" },
                            Defines = ParseDefines("_LIBCPP_DISABLE_VISIBILITY_ANNOTATIONS;_LIBCPP_DISABLE_AVAILABILITY;_LIBCPP_HIDDEN=__attribute__ ((__visibility__(\"hidden\")))"),
                            CppFlags = new List<String> { "-nostdinc++" },
                            LinkerFlags = new List<String> { "-nostdlib++" }
                        },
                        new Configuration
                        {
                            MatchingCLibraries = new List<CLibraryType> { CLibraryType.musl },
                            Defines = ParseDefines("_LIBCPP_HAS_MUSL_LIBC")
                        }
                    },
                    PhysicalPath = InputDirectory,
                    DependentProjectToRequirement = DependentModuleToRequirement
                });
            }

            foreach (var m in Modules)
            {
                var ModuleName = m.ModuleName;
                var InputDirectory = m.ModulePath;
                var Extensions = ModuleName.Split('.').Skip(1).ToArray();
                var IsTargetOperatingSystemMatched = IsOperatingSystemMatchExtensions(Extensions, TargetOperatingSystem);
                if (IsTargetOperatingSystemMatched || EnableNonTargetingOperatingSystemDummy)
                {
                    var DependentModuleToRequirement = Dependencies.ContainsKey(ModuleName) ? Dependencies[ModuleName] : new HashSet<String>();
                    Projects.Add(new ProjectDescription
                    {
                        Definition = new Project
                        {
                            Id = GetIdForProject(ModuleName),
                            Name = ModuleName,
                            VirtualDir = m.VirtualDir,
                            FilePath = BuildDirectory / "projects" / GetProjectFileName(ModuleName),
                            TargetType = TargetType.StaticLibrary,
                            Configurations = new List<Configuration>
                            {
                                new Configuration
                                {
                                    IncludeDirectories = new List<PathString> { InputDirectory / "include", InputDirectory / "src" },
                                    Files = new List<PathString> { InputDirectory / "include", InputDirectory / "src" }.SelectMany(d => GetFilesInDirectory(d, TargetOperatingSystem, IsTargetOperatingSystemMatched)).ToList()
                                }
                            }
                        },
                        BaseConfigurations = GetCommonConfigurations(),
                        ExportConfigurations = new List<Configuration>
                        {
                            new Configuration
                            {
                                IncludeDirectories = new List<PathString> { InputDirectory / "include" }
                            }
                        },
                        PhysicalPath = InputDirectory,
                        DependentProjectToRequirement = DependentModuleToRequirement
                    });
                    if (!((TargetOperatingSystem == OperatingSystemType.Android) || (TargetOperatingSystem == OperatingSystemType.iOS) || ((TargetOperatingSystem == OperatingSystemType.Windows) && (WindowsRuntime == WindowsRuntimeType.WinRT))))
                    {
                        foreach (var TestFile in GetFilesInDirectory(InputDirectory / "test", TargetOperatingSystem, IsTargetOperatingSystemMatched))
                        {
                            if (TestFile.Type != FileType.CppSource) { continue; }
                            var TestName = ModuleName + "_" + Regex.Replace(TestFile.Path.FullPath.RelativeTo(InputDirectory), @"[\\/]", "_").AsPath().FileNameWithoutExtension;
                            var TestDependentModuleToRequirement = new HashSet<String>(DependentModuleToRequirement);
                            TestDependentModuleToRequirement.Add(ModuleName);
                            Projects.Add(new ProjectDescription
                            {
                                Definition = new Project
                                {
                                    Id = GetIdForProject(TestName),
                                    Name = TestName,
                                    VirtualDir = m.VirtualDir,
                                    FilePath = BuildDirectory / "projects" / GetProjectFileName(TestName),
                                    TargetType = TargetType.Executable,
                                    Configurations = new List<Configuration>
                                    {
                                        new Configuration
                                        {
                                            IncludeDirectories = new List<PathString> { InputDirectory / "include", InputDirectory / "src" },
                                            Files = new List<Cpp.File> { TestFile }
                                        }
                                    }
                                },
                                BaseConfigurations = GetCommonConfigurations(),
                                ExportConfigurations = new List<Configuration> { },
                                PhysicalPath = InputDirectory,
                                DependentProjectToRequirement = TestDependentModuleToRequirement
                            });
                        }
                    }
                }
            }
            foreach (var p in Products)
            {
                var ProductName = p.ProductName;
                var InputDirectory = p.ProductPath;
                var Extensions = p.ProductPath.FileName.ToString().Split('.').Skip(1).ToArray();
                var ProductTargetType = TargetType.Executable;
                var GradleTargetType = (TargetType?)(null);
                if (Extensions.Contains("dynamic", StringComparer.OrdinalIgnoreCase))
                {
                    ProductTargetType = TargetType.DynamicLibrary;
                }
                else if (Extensions.Contains("static", StringComparer.OrdinalIgnoreCase))
                {
                    ProductTargetType = TargetType.StaticLibrary;
                }
                if ((ProductTargetType == TargetType.Executable) && System.IO.File.Exists(InputDirectory / "Info.plist"))
                {
                    ProductTargetType = TargetType.DarwinApplication;
                }
                if (System.IO.File.Exists(InputDirectory / "AndroidManifest.xml"))
                {
                    if (ProductTargetType == TargetType.DynamicLibrary)
                    {
                        GradleTargetType = TargetType.GradleLibrary;
                    }
                    else
                    {
                        ProductTargetType = TargetType.DynamicLibrary;
                        GradleTargetType = TargetType.GradleApplication;
                    }
                }
                var IsTargetOperatingSystemMatched = IsOperatingSystemMatchExtensions(Extensions, TargetOperatingSystem);
                if ((ProductTargetType == TargetType.DarwinApplication) && (Toolchain != ToolchainType.XCode))
                {
                    IsTargetOperatingSystemMatched = false;
                }
                if ((ProductTargetType == TargetType.Executable) && (TargetOperatingSystem == OperatingSystemType.iOS))
                {
                    IsTargetOperatingSystemMatched = false;
                }
                if ((ProductTargetType == TargetType.Executable) && (TargetOperatingSystem == OperatingSystemType.Windows) && (WindowsRuntime == WindowsRuntimeType.WinRT))
                {
                    IsTargetOperatingSystemMatched = false;
                }
                if (IsTargetOperatingSystemMatched || EnableNonTargetingOperatingSystemDummy)
                {
                    var DependentModuleToRequirement = Dependencies.ContainsKey(ProductName) ? Dependencies[ProductName] : new HashSet<String>();
                    var Defines = new List<KeyValuePair<String, String>> { };
                    if ((ProductTargetType == TargetType.Executable) || (ProductTargetType == TargetType.DarwinApplication) || (ProductTargetType == TargetType.GradleApplication))
                    {
                    }
                    else if (ProductTargetType == TargetType.StaticLibrary)
                    {
                        Defines.Add(new KeyValuePair<String, String>(SolutionName.ToUpperInvariant() + "_BUILD", null));
                        Defines.Add(new KeyValuePair<String, String>(SolutionName.ToUpperInvariant() + "_STATIC", null));
                    }
                    else if ((ProductTargetType == TargetType.DynamicLibrary) || (ProductTargetType == TargetType.GradleLibrary))
                    {
                        Defines.Add(new KeyValuePair<String, String>(SolutionName.ToUpperInvariant() + "_BUILD", null));
                        Defines.Add(new KeyValuePair<String, String>(SolutionName.ToUpperInvariant() + "_DYNAMIC", null));
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                    var TargetName = ProductName.Split('.').Take(1).Single();
                    var Files = new List<PathString> { InputDirectory }.SelectMany(d => GetFilesInDirectory(d, TargetOperatingSystem, IsTargetOperatingSystemMatched)).ToList();
                    if ((ProductTargetType == TargetType.StaticLibrary) || (ProductTargetType == TargetType.DynamicLibrary))
                    {
                        Files = Files.Select(f => f.Path.In(InputDirectory / "include") ? new Cpp.File { Path = f.Path, Type = f.Type, IsExported = true } : f).ToList();
                    }
                    var Configurations = new List<Configuration>
                    {
                        new Configuration
                        {
                            IncludeDirectories = new List<PathString> { InputDirectory / "include", InputDirectory / "src", InputDirectory },
                            Defines = Defines,
                            Files = Files
                        },
                        new Configuration
                        {
                            MatchingCompilers = new List<CompilerType> { CompilerType.gcc, CompilerType.clang },
                            MatchingTargetOperatingSystems = new List<OperatingSystemType> { OperatingSystemType.Linux, OperatingSystemType.Android },
                            MatchingTargetTypes = new List<TargetType> { TargetType.DynamicLibrary },
                            LinkerFlags = new List<string> { "-Wl,--version-script=" + (SourceDirectory / "products/export.version").RelativeTo(GetProjectWorkingDirectory(BuildDirectory, ProductName)).ToString(PathStringStyle.Unix) }
                        },
                        new Configuration
                        {
                            MatchingTargetOperatingSystems = new List<OperatingSystemType> { OperatingSystemType.MacOS, OperatingSystemType.iOS },
                            MatchingTargetTypes = new List<TargetType> { TargetType.DarwinApplication, TargetType.DarwinApplication, TargetType.DarwinStaticFramework, TargetType.DarwinSharedFramework, TargetType.MacBundle },
                            Options = new Dictionary<String, String>
                            {
                                ["xcode.target.PRODUCT_BUNDLE_IDENTIFIER"] = SolutionName + "." + TargetName
                            }
                        }
                    };
                    if ((TargetOperatingSystem != OperatingSystemType.iOS) || (ProductTargetType != TargetType.DynamicLibrary))
                    {
                        Projects.Add(new ProjectDescription
                        {
                            Definition = new Project
                            {
                                Id = GetIdForProject(ProductName),
                                Name = ProductName,
                                VirtualDir = p.VirtualDir,
                                FilePath = BuildDirectory / "projects" / GetProjectFileName(ProductName),
                                TargetType = IsTargetOperatingSystemMatched ? ProductTargetType : TargetType.StaticLibrary,
                                TargetName = TargetName,
                                Configurations = Configurations
                            },
                            BaseConfigurations = GetCommonConfigurations(),
                            ExportConfigurations = (ProductTargetType == TargetType.StaticLibrary) || (ProductTargetType == TargetType.DynamicLibrary) ? new List<Configuration>
                            {
                                new Configuration
                                {
                                    IncludeDirectories = new List<PathString> { InputDirectory / "include" }
                                }
                            } : new List<Configuration> { },
                            PhysicalPath = InputDirectory,
                            DependentProjectToRequirement = DependentModuleToRequirement
                        });
                    }
                    if (((TargetOperatingSystem == OperatingSystemType.MacOS) || (TargetOperatingSystem == OperatingSystemType.iOS)) && (Toolchain == ToolchainType.XCode) && (ProductTargetType == TargetType.DynamicLibrary))
                    {
                        var FrameworkTargetType = TargetType.DarwinSharedFramework;
                        var FrameworkName = ProductName + ".framework";
                        Projects.Add(new ProjectDescription
                        {
                            Definition = new Project
                            {
                                Id = GetIdForProject(FrameworkName),
                                Name = FrameworkName,
                                VirtualDir = p.VirtualDir,
                                FilePath = BuildDirectory / "projects" / GetProjectFileName(FrameworkName),
                                TargetType = FrameworkTargetType,
                                TargetName = TargetName,
                                Configurations = Configurations
                            },
                            BaseConfigurations = GetCommonConfigurations(),
                            ExportConfigurations = new List<Configuration> { },
                            PhysicalPath = InputDirectory,
                            DependentProjectToRequirement = DependentModuleToRequirement
                        });
                    }
                    if ((TargetOperatingSystem == OperatingSystemType.Android) && (GradleTargetType != null))
                    {
                        Projects.Add(new ProjectDescription
                        {
                            Definition = new Project
                            {
                                Id = GetIdForProject(ProductName),
                                Name = ProductName + ":" + GradleTargetType.Value.ToString(),
                                VirtualDir = p.VirtualDir,
                                FilePath = BuildDirectory / "gradle" / GetProjectFileName(ProductName),
                                TargetType = GradleTargetType.Value,
                                TargetName = TargetName,
                                Configurations = new List<Configuration>
                                {
                                    new Configuration
                                    {
                                        Files = new List<Cpp.File>
                                        {
                                            new Cpp.File { Path = InputDirectory / "java", Type = FileType.Unknown },
                                            new Cpp.File { Path = InputDirectory / "include", Type = FileType.Unknown },
                                            new Cpp.File { Path = InputDirectory / "src", Type = FileType.Unknown }
                                        }
                                    }
                                }
                            },
                            BaseConfigurations = new List<Configuration> { },
                            ExportConfigurations = new List<Configuration> { },
                            PhysicalPath = InputDirectory,
                            DependentProjectToRequirement = new HashSet<String> { ProductName }
                        });
                    }
                }
            }

            var DuplicateProjectNames = Projects.GroupBy(Project => Project.Definition.Name, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (DuplicateProjectNames.Count > 0)
            {
                throw new InvalidOperationException("DuplicateProjectNames: " + String.Join(" ", DuplicateProjectNames));
            }
            foreach (var Project in Projects)
            {
                if (EnableLibcxxCompilation)
                {
                    if (Project.Definition.Name != "libcxx")
                    {
                        Project.DependentProjectToRequirement.Add("libcxx");
                    }
                }
            }
            return Projects.ToDictionary(Project => Project.Definition.Name);
        }

        public Dictionary<String, List<String>> CheckUnresolvedDependencies(Dictionary<String, ProjectDescription> SelectedProjects)
        {
            var UnresolvedDependencies = new Dictionary<String, List<String>>();
            var ProjectDependencies = SelectedProjects.Values.ToDictionary(Project => Project.Definition.Name, Project => Project.DependentProjectToRequirement);
            foreach (var Project in SelectedProjects.Values)
            {
                var FullDependentTargetToRequirement = GetFullProjectDependencies(Project.Definition.Name, ProjectDependencies, out var Unresovled);
                if (Unresovled.Count > 0)
                {
                    UnresolvedDependencies.Add(Project.Definition.Name, Unresovled);
                }
            }
            return UnresolvedDependencies;
        }

        public class Result
        {
            public String SolutionName;
            public List<ProjectReference> SortedProjects;
        }
        public Result Execute(Dictionary<String, ProjectDescription> SelectedProjects)
        {
            var Dependencies = SelectedProjects.Values.ToDictionary(p => p.Definition.Name, p => p.DependentProjectToRequirement);
            var ExternalConfigurations = new List<Configuration>
            {
                new Configuration
                {
                    CommonFlags = CommonFlags,
                    CFlags = CFlags,
                    CppFlags = CppFlags,
                    LinkerFlags = LinkerFlags,
                    PostLinkerFlags = PostLinkerFlags
                }
            };
            var ProjectDict = new Dictionary<String, Project>();
            var ProjectNameToFullDependentProjectNames = new Dictionary<String, List<String>>();
            foreach (var Project in SelectedProjects.Values)
            {
                var FullDependentProjectNames = GetFullProjectDependencies(Project.Definition.Name, Dependencies, out var Unresolved);
                if (Unresolved.Count > 0)
                {
                    throw new InvalidOperationException($"UnresolvedDependencies: {Project.Definition.Name} -> {String.Join(" ", Unresolved)}");
                }
                var TransitiveDepedentProjectNames = GetTransitiveProjectDependencies(Project.Definition.Name, Dependencies, ProjectName => SelectedProjects[ProjectName].Definition.TargetType == TargetType.StaticLibrary, out _);
                var DependentProjectExportConfigurations = TransitiveDepedentProjectNames.SelectMany(d => SelectedProjects[d].ExportConfigurations).ToList();
                var p = new Project
                {
                    Id = Project.Definition.Id,
                    Name = Project.Definition.Name,
                    VirtualDir = Project.Definition.VirtualDir,
                    FilePath = Project.Definition.FilePath,
                    TargetType = Project.Definition.TargetType,
                    TargetName = Project.Definition.TargetName,
                    Configurations = Project.BaseConfigurations.Concat(DependentProjectExportConfigurations).Concat(Project.Definition.Configurations).Concat(ExternalConfigurations).ToList()
                };
                ProjectDict.Add(p.Name, p);
                ProjectNameToFullDependentProjectNames.Add(p.Name, FullDependentProjectNames);
            }
            var ProjectNameToReference = new Dictionary<String, ProjectReference>();
            foreach (var p in ProjectDict.Values)
            {
                var OutputFilePath = new Dictionary<ConfigurationType, PathString>();
                foreach (var ConfigurationType in Enum.GetValues(typeof(ConfigurationType)).Cast<ConfigurationType>())
                {
                    var conf = p.Configurations.Merged(p.TargetType, HostOperatingSystem, HostArchitecture, TargetOperatingSystem, TargetArchitecture, WindowsRuntime, Toolchain, Compiler, CLibrary, CLibraryForm, CppLibrary, CppLibraryForm, ConfigurationType);
                    OutputFilePath.Add(ConfigurationType, BuildDirectory / GetProjectOutputFilePath(p.Name, p.TargetName ?? p.Name, p.TargetType, conf.OutputDirectory, ConfigurationType));
                }
                var Reference = new ProjectReference
                {
                    Id = p.Id,
                    Name = p.Name,
                    VirtualDir = p.VirtualDir,
                    FilePath = p.FilePath,
                    TargetType = p.TargetType,
                    TargetName = p.TargetName ?? p.Name,
                    OutputFilePath = OutputFilePath
                };
                ProjectNameToReference.Add(p.Name, Reference);
            }
            foreach (var Project in SelectedProjects.Values)
            {
                var p = ProjectDict[Project.Definition.Name];
                var ProjectReferences = ProjectNameToFullDependentProjectNames[p.Name].Select(d => ProjectNameToReference[d]).ToList();
                var InputDirectory = Project.PhysicalPath;
                var OutputDirectory = Project.Definition.FilePath.Parent;
                var ProjectTargetType = Project.Definition.TargetType;
                if (Toolchain == ToolchainType.VisualStudio)
                {
                    var VcxprojTemplateText = Resource.GetResourceText($@"Templates\{(VSVersion == 2022 ? "vc17" : "vc16")}\{(TargetOperatingSystem == OperatingSystemType.Windows ? "Default" : "Linux")}.vcxproj");
                    var VcxprojFilterTemplateText = Resource.GetResourceText(VSVersion == 2022 ? @"Templates\vc17\Default.vcxproj.filters" : @"Templates\vc16\Default.vcxproj.filters");
                    var g = new VcxprojGenerator(p, Project.Definition.Id, ProjectReferences, BuildDirectory, InputDirectory, OutputDirectory, VcxprojTemplateText, VcxprojFilterTemplateText, HostOperatingSystem, HostArchitecture, TargetOperatingSystem, TargetArchitecture, WindowsRuntime, Compiler, CLibrary, CLibraryForm, CppLibrary, CppLibraryForm, CC, CXX, AR);
                    g.Generate(ForceRegenerate);
                }
                else if (Toolchain == ToolchainType.XCode)
                {
                    var PbxprojTemplateText = Resource.GetResourceText(@"Templates\xcode9\Default.xcodeproj\project.pbxproj");
                    var g = new PbxprojGenerator(p, ProjectReferences, InputDirectory, OutputDirectory, PbxprojTemplateText, HostOperatingSystem, HostArchitecture, TargetOperatingSystem, TargetArchitecture, CppLibraryForm, XCodeDevelopmentTeam, XCodeProvisioningProfileSpecifier);
                    g.Generate(ForceRegenerate);
                }
                else if ((Toolchain == ToolchainType.Ninja) && (TargetOperatingSystem != OperatingSystemType.Android))
                {
                    var g = new NinjaProjectGenerator(p, ProjectReferences, InputDirectory, OutputDirectory, HostOperatingSystem, HostArchitecture, TargetOperatingSystem, TargetArchitecture, WindowsRuntime, Toolchain, Compiler, CLibrary, CLibraryForm, CppLibrary, CppLibraryForm, ConfigurationType);
                    g.Generate(ForceRegenerate);
                }
                else if ((Toolchain == ToolchainType.Gradle_Ninja) || (Toolchain == ToolchainType.Ninja))
                {
                    if (ProjectTargetType == TargetType.GradleApplication)
                    {
                        if (EnableJava)
                        {
                            if (Toolchain == ToolchainType.Ninja)
                            {
                                var Out = OutputDirectory.FileName == "gradle" ? OutputDirectory.Parent / "batch" : OutputDirectory;
                                var gBatch = new AndroidBatchProjectGenerator(SolutionName, p, ProjectReferences, InputDirectory, Out, BuildDirectory, HostOperatingSystem, HostArchitecture, TargetOperatingSystem, TargetArchitecture, Toolchain, Compiler, CLibrary, CLibraryForm, CppLibrary, CppLibraryForm, ConfigurationType, Jdk, AndroidSdk, AndroidNdk, "30.0.3", 15, 28, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).AsPath() / ".android/debug.keystore", "android", "androiddebugkey", "android", true);
                                gBatch.Generate(ForceRegenerate);
                            }
                            else
                            {
                                var BuildGradleTemplateText = Resource.GetResourceText(@"Templates\gradle_application\build.gradle");
                                var gGradle = new GradleProjectGenerator(SolutionName, p, ProjectReferences, InputDirectory, OutputDirectory, BuildDirectory, BuildGradleTemplateText, HostOperatingSystem, HostArchitecture, TargetOperatingSystem, TargetArchitecture, Toolchain, Compiler, CLibrary, CLibraryForm, CppLibrary, CppLibraryForm, ConfigurationType, AndroidNdk);
                                gGradle.Generate(ForceRegenerate);
                            }
                        }
                    }
                    else if (ProjectTargetType == TargetType.GradleLibrary)
                    {
                        if (EnableJava)
                        {
                            if (Toolchain == ToolchainType.Ninja)
                            {
                                var Out = OutputDirectory.FileName == "gradle" ? OutputDirectory.Parent / "batch" : OutputDirectory;
                                var gBatch = new AndroidBatchProjectGenerator(SolutionName, p, ProjectReferences, InputDirectory, Out, BuildDirectory, HostOperatingSystem, HostArchitecture, TargetOperatingSystem, TargetArchitecture, Toolchain, Compiler, CLibrary, CLibraryForm, CppLibrary, CppLibraryForm, ConfigurationType, Jdk, AndroidSdk, AndroidNdk, "30.0.3", 15, 28, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).AsPath() / ".android/debug.keystore", "android", "androiddebugkey", "android", true);
                                gBatch.Generate(ForceRegenerate);
                            }
                            else
                            {
                                var BuildGradleTemplateText = Resource.GetResourceText(@"Templates\gradle_library\build.gradle");
                                var gGradle = new GradleProjectGenerator(SolutionName, p, ProjectReferences, InputDirectory, OutputDirectory, BuildDirectory, BuildGradleTemplateText, HostOperatingSystem, HostArchitecture, TargetOperatingSystem, TargetArchitecture, Toolchain, Compiler, CLibrary, CLibraryForm, CppLibrary, CppLibraryForm, ConfigurationType, AndroidNdk);
                                gGradle.Generate(ForceRegenerate);
                            }
                        }
                    }
                    else
                    {
                        if (Toolchain == ToolchainType.Gradle_Ninja)
                        {
                            var g = new NinjaProjectGenerator(p, ProjectReferences, InputDirectory, OutputDirectory, HostOperatingSystem, HostArchitecture, TargetOperatingSystem, TargetArchitecture, WindowsRuntime, Toolchain, Compiler, CLibrary, CLibraryForm, CppLibrary, CppLibraryForm, ConfigurationType);
                            g.Generate(ForceRegenerate);
                        }
                        else if (Toolchain == ToolchainType.Ninja)
                        {
                            var g = new NinjaProjectGenerator(p, ProjectReferences, InputDirectory, OutputDirectory, HostOperatingSystem, HostArchitecture, TargetOperatingSystem, TargetArchitecture, WindowsRuntime, Toolchain, Compiler, CLibrary, CLibraryForm, CppLibrary, CppLibraryForm, ConfigurationType);
                            g.Generate(ForceRegenerate);
                        }
                    }
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            var GradleProjectNames = SelectedProjects.Values.Where(Project => (Project.Definition.TargetType == TargetType.GradleLibrary) || (Project.Definition.TargetType == TargetType.GradleApplication)).Select(Project => Project.Definition.Name).ToList();
            var ProjectDependencies = ProjectNameToFullDependentProjectNames.ToDictionary(p => ProjectNameToReference[p.Key], p => p.Value.Select(n => ProjectNameToReference[n]).ToList());
            var SortedProjects = ProjectDependencies.Keys.PartialOrderBy(p => ProjectDependencies.ContainsKey(p) ? ProjectDependencies[p] : null).ToList();
            var CppSortedProjects = SortedProjects.Where(p => (p.TargetType == TargetType.Executable) || (p.TargetType == TargetType.StaticLibrary) || (p.TargetType == TargetType.DynamicLibrary) || (p.TargetType == TargetType.DarwinApplication) || (p.TargetType == TargetType.DarwinStaticFramework) || (p.TargetType == TargetType.DarwinSharedFramework) || (p.TargetType == TargetType.MacBundle)).ToList();
            if (Toolchain == ToolchainType.VisualStudio)
            {
                var SlnTemplateText = Resource.GetResourceText(VSVersion == 2022 ? @"Templates\vc17\Default.sln" : @"Templates\vc16\Default.sln");
                var g = new SlnGenerator(SolutionName, GetIdForProject(SolutionName + ".solution"), CppSortedProjects, BuildDirectory, SlnTemplateText, TargetOperatingSystem, TargetArchitecture);
                g.Generate(ForceRegenerate);
            }
            else if (Toolchain == ToolchainType.XCode)
            {
                var g = new XcworkspaceGenerator(SolutionName, CppSortedProjects, BuildDirectory);
                g.Generate(ForceRegenerate);
            }
            else if (Toolchain == ToolchainType.Ninja)
            {
                var g = new NinjaSolutionGenerator(SolutionName, CppSortedProjects, BuildDirectory / "projects", TargetOperatingSystem, CC, CXX, AR, STRIP);
                g.Generate(ForceRegenerate);
            }
            else if (Toolchain == ToolchainType.Gradle_Ninja)
            {
                FileUtils.CopyDirectory(System.Reflection.Assembly.GetEntryAssembly().Location.AsPath().Parent / "Templates/gradle", BuildDirectory / "gradle", !ForceRegenerate);
                if (HostOperatingSystem != Cpp.OperatingSystemType.Windows)
                {
                    if (Shell.Execute("chmod", "+x", BuildDirectory / "gradle/gradlew") != 0)
                    {
                        throw new InvalidOperationException("ErrorInExecution: chmod");
                    }
                }
                else if (Toolchain == ToolchainType.Gradle_Ninja)
                {
                    var g = new NinjaSolutionGenerator(SolutionName, CppSortedProjects, BuildDirectory / "projects", TargetOperatingSystem, CC, CXX, AR, STRIP);
                    g.Generate(ForceRegenerate);
                }
                if (GradleProjectNames.Count > 0)
                {
                    TextFile.WriteToFile(BuildDirectory / "gradle/settings.gradle", "include " + String.Join(", ", GradleProjectNames.Select(n => "':" + n.Split(':').First() + "'")), new UTF8Encoding(false), !ForceRegenerate);
                }
                else
                {
                    TextFile.WriteToFile(BuildDirectory / "gradle/settings.gradle", "", new UTF8Encoding(false), !ForceRegenerate);
                }
            }
            else
            {
                throw new NotSupportedException();
            }
            return new Result { SolutionName = SolutionName, SortedProjects = SortedProjects };
        }

        private List<Configuration> GetCommonConfigurations()
        {
            var Configurations = new List<Configuration>
            {
                new Configuration
                {
                    MatchingCompilers = new List<CompilerType> { CompilerType.clangcl },
                    Options = new Dictionary<String, String>
                    {
                        ["vc.Configuration.PlatformToolset"] = "ClangCL"
                    }
                },
                new Configuration
                {
                    MatchingTargetTypes = new List<TargetType> { TargetType.StaticLibrary },
                    MatchingCompilers = new List<CompilerType> { CompilerType.clangcl },
                    Options = new Dictionary<String, String>
                    {
                        ["vc.Lib.LinkTimeCodeGeneration"] = "false"
                    }
                },
                new Configuration
                {
                    Defines = ParseDefines((Compiler == CompilerType.VisualCpp) && (VSVersion == 2022) && (WindowsRuntime == WindowsRuntimeType.Win32) ? "TYPEMAKESAMPLE_USE_MODULE;TYPEMAKESAMPLE_EXPORT=export" : "TYPEMAKESAMPLE_EXPORT=")
                },
                new Configuration
                {
                    MatchingCompilers = new List<CompilerType> { CompilerType.VisualCpp, CompilerType.clangcl },
                    Options = new Dictionary<String, String>
                    {
                        ["vc.ClCompile.LanguageStandard"] = WindowsRuntime == WindowsRuntimeType.Win32 ? "stdcpp20" : "stdcpp17"
                    }
                },
                new Configuration
                {
                    MatchingTargetOperatingSystems = new List<OperatingSystemType> { OperatingSystemType.Windows },
                    MatchingWindowsRuntimes = new List<WindowsRuntimeType> { WindowsRuntimeType.WinRT },
                    MatchingCompilers = new List<CompilerType> { CompilerType.VisualCpp },
                    Options = new Dictionary<String, String>
                    {
                        ["vc.Globals.DefaultLanguage"] = "zh-CN",
                        ["vc.ClCompile.CompileAsWinRT"] = "false"
                    }
                },
                new Configuration
                {
                    MatchingTargetTypes = new List<TargetType> { TargetType.Executable, TargetType.DynamicLibrary },
                    MatchingTargetOperatingSystems = new List<OperatingSystemType> { OperatingSystemType.Windows },
                    MatchingWindowsRuntimes = new List<WindowsRuntimeType> { WindowsRuntimeType.WinRT },
                    MatchingCompilers = new List<CompilerType> { CompilerType.VisualCpp },
                    Options = new Dictionary<String, String>
                    {
                        ["vc.Link.GenerateWindowsMetadata"] = "false"
                    }
                },
                new Configuration
                {
                    MatchingCompilers = new List<CompilerType> { CompilerType.gcc, CompilerType.clang },
                    CppFlags = ParseFlags("-std=c++20")
                },
                new Configuration
                {
                    MatchingCompilers = new List<CompilerType> { CompilerType.VisualCpp, CompilerType.clangcl },
                    Defines = ParseDefines("_CRT_SECURE_NO_DEPRECATE;_CRT_NONSTDC_NO_DEPRECATE;_SCL_SECURE_NO_WARNINGS;_CRT_SECURE_NO_WARNINGS"),
                    CommonFlags = ParseFlags("/bigobj /JMC"),
                    Options = new Dictionary<String, String>
                    {
                        ["vc.Configuration.UseNativeEnvironment"] = "true"
                    }
                },
                new Configuration
                {
                    MatchingCompilers = new List<CompilerType> { CompilerType.gcc, CompilerType.clang },
                    CommonFlags = ParseFlags("-fsigned-char -fvisibility=hidden"),
                    CppFlags = ParseFlags("-fvisibility-inlines-hidden")
                },
                new Configuration
                {
                    MatchingCompilers = new List<CompilerType> { CompilerType.VisualCpp, CompilerType.clangcl },
                    CommonFlags = ParseFlags("/we4172 /we4715")
                },
                new Configuration
                {
                    MatchingCompilers = new List<CompilerType> { CompilerType.gcc, CompilerType.clang },
                    CommonFlags = ParseFlags("-Wno-unused-function -Wno-comment -Werror=return-type -Werror=address -Werror=sequence-point -Werror=int-to-pointer-cast -Werror=format -Werror=format-security -Werror=init-self -Werror=pointer-arith -Wuninitialized -Wundef -Wformat-nonliteral -Wno-error=uninitialized -Wno-error=undef -Wno-error=format-nonliteral"),
                    CFlags = ParseFlags("-Werror=strict-prototypes -Werror=implicit-int -Werror=implicit-function-declaration -Werror=pointer-to-int-cast"),
                    CppFlags = ParseFlags("-Wsign-promo -Wno-error=sign-promo")
                },
                new Configuration
                {
                    MatchingCompilers = new List<CompilerType> { CompilerType.gcc },
                    CommonFlags = ParseFlags("-Werror=return-local-addr"),
                },
                new Configuration
                {
                    MatchingCompilers = new List<CompilerType> { CompilerType.clang },
                    CommonFlags = ParseFlags("-Werror=return-stack-address -Werror=incomplete-implementation -Werror=mismatched-return-types -Werror=unguarded-availability"),
                },
                new Configuration
                {
                    MatchingTargetOperatingSystems = new List<OperatingSystemType> { OperatingSystemType.Windows },
                    Defines = ParseDefines("WIN32;_WINDOWS")
                },
                new Configuration
                {
                    MatchingTargetOperatingSystems = new List<OperatingSystemType> { OperatingSystemType.Linux, OperatingSystemType.Android },
                    CommonFlags = new List<String> { "-fPIC" }
                },
                new Configuration
                {
                    MatchingCLibraries = new List<CLibraryType> { CLibraryType.glibc },
                    CommonFlags = new List<String> { "-pthread" },
                    LinkerFlags = new List<String> { "-pthread" },
                    Libs = new List<PathString> { "dl", "rt" }
                },
                new Configuration
                {
                    MatchingCompilers = new List<CompilerType> { CompilerType.gcc, CompilerType.clang },
                    MatchingTargetArchitectures = new List<ArchitectureType> { ArchitectureType.armv7a },
                    CommonFlags = ParseFlags("-mfpu=neon")
                },
                new Configuration
                {
                    MatchingCompilers = new List<CompilerType> { CompilerType.gcc, CompilerType.clang },
                    MatchingTargetArchitectures = new List<ArchitectureType> { ArchitectureType.armv7a },
                    CommonFlags = ParseFlags("-fno-omit-frame-pointer") //disable -fomit-frame-pointer and -mthumb for reliable unwinding on armv7a
                },
                new Configuration
                {
                    MatchingCompilers = new List<CompilerType> { CompilerType.clang },
                    MatchingCppLibraries = new List<CppLibraryType> { CppLibraryType.libcxx },
                    CppFlags = ParseFlags("-stdlib=libc++"),
                    LinkerFlags = ParseFlags("-stdlib=libc++")
                },
                new Configuration
                {
                    MatchingCompilers = new List<CompilerType> { CompilerType.VisualCpp, CompilerType.clangcl },
                    MatchingCLibraryForms = new List<CLibraryForm> { CLibraryForm.Static },
                    MatchingConfigurationTypes = new List<ConfigurationType> { ConfigurationType.Debug },
                    Options = new Dictionary<String, String>
                    {
                        ["vc.ClCompile.RuntimeLibrary"] = "MultiThreadedDebug",
                    }
                },
                new Configuration
                {
                    MatchingCompilers = new List<CompilerType> { CompilerType.VisualCpp, CompilerType.clangcl },
                    MatchingCLibraryForms = new List<CLibraryForm> { CLibraryForm.Dynamic },
                    MatchingConfigurationTypes = new List<ConfigurationType> { ConfigurationType.Debug },
                    Options = new Dictionary<String, String>
                    {
                        ["vc.ClCompile.RuntimeLibrary"] = "MultiThreadedDebugDLL",
                    }
                },
                new Configuration
                {
                    MatchingCompilers = new List<CompilerType> { CompilerType.VisualCpp, CompilerType.clangcl },
                    MatchingCLibraryForms = new List<CLibraryForm> { CLibraryForm.Static },
                    MatchingConfigurationTypes = new List<ConfigurationType> { ConfigurationType.Release },
                    Options = new Dictionary<String, String>
                    {
                        ["vc.ClCompile.RuntimeLibrary"] = "MultiThreaded",
                    }
                },
                new Configuration
                {
                    MatchingCompilers = new List<CompilerType> { CompilerType.VisualCpp, CompilerType.clangcl },
                    MatchingCLibraryForms = new List<CLibraryForm> { CLibraryForm.Dynamic },
                    MatchingConfigurationTypes = new List<ConfigurationType> { ConfigurationType.Release },
                    Options = new Dictionary<String, String>
                    {
                        ["vc.ClCompile.RuntimeLibrary"] = "MultiThreadedDLL",
                    }
                },
                new Configuration
                {
                    MatchingCompilers = new List<CompilerType> { CompilerType.gcc, CompilerType.clang },
                    MatchingCLibraries = new List<CLibraryType> { CLibraryType.musl },
                    MatchingCppLibraryForms = new List<CppLibraryForm> { CppLibraryForm.Static },
                    CommonFlags = ParseFlags("-static"),
                    LinkerFlags = ParseFlags("-static -Wl,-static")
                },
                new Configuration
                {
                    MatchingCompilers = new List<CompilerType> { CompilerType.gcc, CompilerType.clang },
                    MatchingCLibraryForms = new List<CLibraryForm> { CLibraryForm.Dynamic },
                    MatchingCppLibraryForms = new List<CppLibraryForm> { CppLibraryForm.Static },
                    MatchingTargetOperatingSystems = new List<OperatingSystemType> { OperatingSystemType.Linux },
                    LinkerFlags = ParseFlags("-static-libgcc")
                },
                new Configuration
                {
                    MatchingCompilers = new List<CompilerType> { CompilerType.gcc, CompilerType.clang },
                    MatchingCLibraryForms = new List<CLibraryForm> { CLibraryForm.Dynamic },
                    MatchingCppLibraries = new List<CppLibraryType> { CppLibraryType.libstdcxx, CppLibraryType.libcxx },
                    MatchingCppLibraryForms = new List<CppLibraryForm> { CppLibraryForm.Static },
                    MatchingTargetOperatingSystems = new List<OperatingSystemType> { OperatingSystemType.Linux, OperatingSystemType.Android },
                    LinkerFlags = ParseFlags("-static-libstdc++")
                },
                new Configuration
                {
                    MatchingConfigurationTypes = new List<ConfigurationType> { ConfigurationType.Debug },
                    Defines = ParseDefines("DEBUG=1")
                },
                new Configuration
                {
                    MatchingConfigurationTypes = new List<ConfigurationType> { ConfigurationType.Release },
                    Defines = ParseDefines("NDEBUG")
                },
                new Configuration
                {
                    MatchingCompilers = new List<CompilerType> { CompilerType.clang },
                    MatchingTargetOperatingSystems = new List<OperatingSystemType> { OperatingSystemType.Windows },
                    MatchingConfigurationTypes = new List<ConfigurationType> { ConfigurationType.Debug },
                    Defines = ParseDefines("_DEBUG;_MT;_DLL"),
                    CommonFlags = ParseFlags("-gcodeview-ghash"),
                    LinkerFlags = ParseFlags("-Wl,/debug -Wl,/nodefaultlib:libucrt -Wl,/nodefaultlib:libvcruntime -Wl,/nodefaultlib:libcmt"), //workaround llvm bug choosing C runtime, https://docs.microsoft.com/en-us/cpp/c-runtime-library/crt-library-features?view=vs-2019
                    Libs = new List<PathString> { "ucrtd", "vcruntimed", "msvcrtd" }
                },
                new Configuration
                {
                    MatchingCompilers = new List<CompilerType> { CompilerType.clang },
                    MatchingTargetOperatingSystems = new List<OperatingSystemType> { OperatingSystemType.Windows },
                    MatchingConfigurationTypes = new List<ConfigurationType> { ConfigurationType.Release },
                    Defines = ParseDefines("_MT"),
                    CommonFlags = ParseFlags("-gcodeview-ghash"),
                    LinkerFlags = ParseFlags("-Wl,/debug")
                },
                new Configuration
                {
                    MatchingCompilers = new List<CompilerType> { CompilerType.gcc, CompilerType.clang },
                    MatchingConfigurationTypes = new List<ConfigurationType> { ConfigurationType.Debug },
                    CommonFlags = ParseFlags("-O0")
                },
                new Configuration
                {
                    MatchingTargetOperatingSystems = new List<OperatingSystemType> { OperatingSystemType.Windows, OperatingSystemType.Linux, OperatingSystemType.MacOS, OperatingSystemType.iOS },
                    MatchingCompilers = new List<CompilerType> { CompilerType.gcc, CompilerType.clang },
                    MatchingConfigurationTypes = new List<ConfigurationType> { ConfigurationType.Release },
                    CommonFlags = ParseFlags("-O3")
                },
                new Configuration
                {
                    MatchingTargetOperatingSystems = new List<OperatingSystemType> { OperatingSystemType.Android },
                    MatchingCompilers = new List<CompilerType> { CompilerType.gcc },
                    MatchingConfigurationTypes = new List<ConfigurationType> { ConfigurationType.Release },
                    CommonFlags = ParseFlags("-O3")
                },
                new Configuration
                {
                    MatchingTargetOperatingSystems = new List<OperatingSystemType> { OperatingSystemType.Android },
                    MatchingTargetArchitectures = new List<ArchitectureType> { ArchitectureType.x86, ArchitectureType.x64, ArchitectureType.arm64 },
                    MatchingCompilers = new List<CompilerType> { CompilerType.clang },
                    MatchingConfigurationTypes = new List<ConfigurationType> { ConfigurationType.Release },
                    CommonFlags = ParseFlags("-O3")
                },
                new Configuration
                {
                    MatchingTargetOperatingSystems = new List<OperatingSystemType> { OperatingSystemType.Android },
                    MatchingTargetArchitectures = new List<ArchitectureType> { ArchitectureType.armv7a },
                    MatchingCompilers = new List<CompilerType> { CompilerType.clang },
                    MatchingConfigurationTypes = new List<ConfigurationType> { ConfigurationType.Release },
                    CommonFlags = ParseFlags("-Oz") // armv7a libraries come with NDK are compiled with -Oz and seem not ABI-compatible with -O2/-O3/-Os on NDK r22
                },
                new Configuration
                {
                    MatchingCompilers = new List<CompilerType> { CompilerType.gcc, CompilerType.clang },
                    CommonFlags = ParseFlags("-g")
                },
                new Configuration
                {
                    MatchingCompilers = new List<CompilerType> { CompilerType.gcc, CompilerType.clang },
                    MatchingHostArchitectures = new List<ArchitectureType> { ArchitectureType.x64 },
                    MatchingTargetArchitectures = new List<ArchitectureType> { ArchitectureType.x86 },
                    CommonFlags = ParseFlags("-m32"),
                    LinkerFlags = ParseFlags("-m32")
                },
                new Configuration
                {
                    MatchingTargetTypes = new List<TargetType> { TargetType.Executable, TargetType.DynamicLibrary },
                    MatchingTargetOperatingSystems = new List<OperatingSystemType> { OperatingSystemType.Linux },
                    LinkerFlags = ParseFlags(@"-Wl,-rpath -Wl,$ORIGIN")
                },
                new Configuration
                {
                    MatchingTargetTypes = new List<TargetType> { TargetType.Executable, TargetType.DynamicLibrary },
                    MatchingTargetOperatingSystems = new List<OperatingSystemType> { OperatingSystemType.MacOS },
                    MatchingToolchains = new List<ToolchainType> { ToolchainType.Ninja },
                    LinkerFlags = ParseFlags(@"-Wl,-rpath -Wl,@executable_path")
                },
                new Configuration
                {
                    MatchingTargetTypes = new List<TargetType> { TargetType.Executable, TargetType.DynamicLibrary },
                    MatchingTargetOperatingSystems = new List<OperatingSystemType> { OperatingSystemType.MacOS },
                    MatchingToolchains = new List<ToolchainType> { ToolchainType.XCode },
                    Options = new Dictionary<String, String>
                    {
                        ["xcode.project.LD_RUNPATH_SEARCH_PATHS"] = "@executable_path"
                    }
                },
                new Configuration
                {
                    MatchingTargetOperatingSystems = new List<OperatingSystemType> { OperatingSystemType.MacOS },
                    Options = new Dictionary<String, String>
                    {
                        ["xcode.project.MACOSX_DEPLOYMENT_TARGET"] = "10.10"
                    }
                },
                new Configuration
                {
                    MatchingTargetOperatingSystems = new List<OperatingSystemType> { OperatingSystemType.iOS },
                    Options = new Dictionary<String, String>
                    {
                        ["xcode.project.IPHONEOS_DEPLOYMENT_TARGET"] = "9.0"
                    }
                },
                new Configuration
                {
                    MatchingToolchains = new List<ToolchainType> { ToolchainType.Ninja, ToolchainType.Gradle_Ninja },
                    MatchingTargetOperatingSystems = new List<OperatingSystemType> { OperatingSystemType.Android },
                    CommonFlags = ParseFlags("-fno-addrsig -fPIE -fPIC -DANDROID -D_FORTIFY_SOURCE=2 -fdata-sections -ffunction-sections -funwind-tables -fstack-protector-strong -no-canonical-prefixes -Wa,--noexecstack -Werror=fortify-source"),
                    // https://android.googlesource.com/platform/ndk/+/master/docs/BuildSystemMaintainers.md
                    LinkerFlags = ParseFlags("-Wl,--no-rosegment -Wl,--exclude-libs,libgcc.a -Wl,--exclude-libs,libgcc_real.a -Wl,--exclude-libs,libatomic.a -Wl,--build-id=sha1 -Wl,--warn-shared-textrel -Wl,--fatal-warnings -Wl,--no-undefined -Wl,-z,noexecstack -Wl,-z,relro -Wl,-z,now")
                },
                new Configuration
                {
                    MatchingToolchains = new List<ToolchainType> { ToolchainType.Ninja, ToolchainType.Gradle_Ninja },
                    MatchingTargetOperatingSystems = new List<OperatingSystemType> { OperatingSystemType.Android },
                    MatchingTargetArchitectures = new List<ArchitectureType> { ArchitectureType.armv7a },
                    LinkerFlags = ParseFlags("-Wl,--exclude-libs,libunwind.a")
                },
                new Configuration
                {
                    MatchingToolchains = new List<ToolchainType> { ToolchainType.Ninja, ToolchainType.Gradle_Ninja },
                    MatchingTargetOperatingSystems = new List<OperatingSystemType> { OperatingSystemType.Android },
                    MatchingTargetArchitectures = new List<ArchitectureType> { ArchitectureType.x86 },
                    CommonFlags = ParseFlags("-mstackrealign")
                },
                new Configuration
                {
                    MatchingTargetOperatingSystems = new List<OperatingSystemType> { OperatingSystemType.Linux, OperatingSystemType.Android },
                    MatchingCompilers = new List<CompilerType> { CompilerType.gcc, CompilerType.clang },
                    MatchingConfigurationTypes = new List<ConfigurationType> { ConfigurationType.Release },
                    LinkerFlags = ParseFlags("-Wl,--gc-sections")
                },
                new Configuration
                {
                    MatchingToolchains = new List<ToolchainType> { ToolchainType.Ninja, ToolchainType.Gradle_Ninja },
                    MatchingCompilers = new List<CompilerType> { CompilerType.gcc },
                    CommonFlags = ParseFlags("-fdiagnostics-color"),
                    LinkerFlags = ParseFlags("-fdiagnostics-color")
                },
                new Configuration
                {
                    MatchingToolchains = new List<ToolchainType> { ToolchainType.Ninja, ToolchainType.Gradle_Ninja },
                    MatchingCompilers = new List<CompilerType> { CompilerType.clang },
                    CommonFlags = ParseFlags("-fcolor-diagnostics -fansi-escape-codes"),
                    LinkerFlags = ParseFlags("-fcolor-diagnostics -fansi-escape-codes")
                }
            };
            if (EnableiOSSimulator)
            {
                Configurations.Add(new Configuration
                {
                    MatchingTargetOperatingSystems = new List<OperatingSystemType> { OperatingSystemType.iOS },
                    Options = new Dictionary<String, String>
                    {
                        ["xcode.target.SDKROOT"] = "iphonesimulator"
                    }
                });
            }
            if (EnableMacCatalyst)
            {
                Configurations.Add(new Configuration
                {
                    MatchingTargetOperatingSystems = new List<OperatingSystemType> { OperatingSystemType.iOS },
                    Options = new Dictionary<String, String>
                    {
                        ["xcode.project.TARGETED_DEVICE_FAMILY"] = "1,2",
                        ["xcode.project.SUPPORTS_MACCATALYST"] = "YES",
                        ["xcode.project.IPHONEOS_DEPLOYMENT_TARGET"] = "9.0",
                        ["xcode.project.MACOSX_DEPLOYMENT_TARGET"] = "10.15",
                        ["xcode.target.TARGETED_DEVICE_FAMILY"] = "" //disable iPhone and iPad
                    }
                });
            }
            if (EnableCustomSysroot)
            {
                // for gcc, a custom sysroot is not enough for cross-compiling, you'll need to configure and build gcc with custom options. run `g++ -v` for configuration used to build the system g++
                // on the other hand, clang is natively a cross-compiler https://clang.llvm.org/docs/CrossCompilation.html
                Configurations.AddRange(new List<Configuration>
                {
                    new Configuration
                    {
                        MatchingCompilers = new List<CompilerType> { CompilerType.clang },
                        MatchingTargetArchitectures = new List<ArchitectureType> { ArchitectureType.x64 },
                        CommonFlags = new List<String> { $"--sysroot={CustomSysroot.ToString(PathStringStyle.Unix)}" },
                        LinkerFlags = new List<String> { $"--sysroot={CustomSysroot.ToString(PathStringStyle.Unix)}" }
                    },
                    new Configuration
                    {
                        MatchingCompilers = new List<CompilerType> { CompilerType.clang },
                        MatchingTargetArchitectures = new List<ArchitectureType> { ArchitectureType.x64 },
                        MatchingCLibraries = new List<CLibraryType> { CLibraryType.glibc },
                        CommonFlags = new List<String> { $"--gcc-toolchain={(CustomSysroot / "usr").ToString(PathStringStyle.Unix)}" },
                        LinkerFlags = new List<String> { $"--gcc-toolchain={(CustomSysroot / "usr").ToString(PathStringStyle.Unix)}" }
                    }
                });
            }
            return Configurations;
        }

        private List<Cpp.File> GetFilesInDirectory(PathString d, OperatingSystemType TargetOperatingSystem, bool IsTargetOperatingSystemMatched, bool TopOnly = false)
        {
            var l = new List<Cpp.File>();
            FillFilesInDirectory(d, TargetOperatingSystem, IsTargetOperatingSystemMatched, TopOnly, l);
            return l;
        }
        private void FillFilesInDirectory(PathString d, OperatingSystemType TargetOperatingSystem, bool IsTargetOperatingSystemMatched, bool TopOnly, List<Cpp.File> Results)
        {
            if (!Directory.Exists(d)) { return; }
            var Extensions = d.FileName.Split('.', '_').Skip(1).ToList();
            var IsTargetOperatingSystemMatchedForCurrentDirectory = IsTargetOperatingSystemMatched;
            if ((!IsTargetOperatingSystemMatched || !IsOperatingSystemMatchExtensions(Extensions, TargetOperatingSystem)) && !((TargetOperatingSystem == OperatingSystemType.iOS) && EnableMacCatalyst && IsOperatingSystemMatchExtensions(Extensions, OperatingSystemType.MacOS, true)))
            {
                IsTargetOperatingSystemMatchedForCurrentDirectory = false;
            }
            foreach (var FilePathRelative in FileSystemUtils.GetDirectories(d, "*", SearchOption.TopDirectoryOnly))
            {
                var FilePath = FilePathRelative.FullPath;
                var Ext = FilePath.Extension.ToLowerInvariant();
                if (Ext == "xcassets")
                {
                    Results.Add(new Cpp.File { Path = FilePath, Type = FileType.EmbeddedContent });
                    continue;
                }
                if (!TopOnly)
                {
                    FillFilesInDirectory(FilePathRelative, TargetOperatingSystem, IsTargetOperatingSystemMatchedForCurrentDirectory, TopOnly, Results);
                }
            }
            foreach (var FilePathRelative in FileSystemUtils.GetFiles(d, "*", SearchOption.TopDirectoryOnly))
            {
                var FilePath = FilePathRelative.FullPath;
                Results.Add(GetFileByPath(FilePath, TargetOperatingSystem, IsTargetOperatingSystemMatchedForCurrentDirectory));
            }
        }
        private Cpp.File GetFileByPath(PathString FilePath, OperatingSystemType TargetOperatingSystem, bool IsTargetOperatingSystemMatched)
        {
            var Ext = FilePath.Extension.ToLowerInvariant();
            var Extensions = FilePath.FileName.Split('.', '_').Skip(1).ToList();
            var Configurations = new List<Configuration> { };
            if ((!IsTargetOperatingSystemMatched || !IsOperatingSystemMatchExtensions(Extensions, TargetOperatingSystem)) && !((TargetOperatingSystem == OperatingSystemType.iOS) && EnableMacCatalyst && IsOperatingSystemMatchExtensions(Extensions, OperatingSystemType.MacOS, true)))
            {
                return new Cpp.File { Path = FilePath, Type = FileType.Unknown, Configurations = Configurations };
            }
            if ((TargetOperatingSystem == OperatingSystemType.iOS) && EnableMacCatalyst)
            {
                if (IsOperatingSystemMatchExtensions(Extensions, TargetOperatingSystem, true) && !IsOperatingSystemMatchExtensions(Extensions, OperatingSystemType.MacOS, true))
                {
                    Configurations.Add(new Configuration { Options = new Dictionary<String, String> { ["xcode.buildFile.platformFilter"] = "ios" } });
                }
                else if (!IsOperatingSystemMatchExtensions(Extensions, TargetOperatingSystem, true) && IsOperatingSystemMatchExtensions(Extensions, OperatingSystemType.MacOS, true))
                {
                    Configurations.Add(new Configuration { Options = new Dictionary<String, String> { ["xcode.buildFile.platformFilter"] = "maccatalyst" } });
                }
            }
            if (((TargetOperatingSystem == OperatingSystemType.MacOS) || (TargetOperatingSystem == OperatingSystemType.iOS)) && Extensions.Contains("mm"))
            {
                return new Cpp.File { Path = FilePath, Type = FileType.ObjectiveCppSource, Configurations = Configurations };
            }
            if ((Ext == "h") || (Ext == "hh") || (Ext == "hpp") || (Ext == "hxx"))
            {
                return new Cpp.File { Path = FilePath, Type = FileType.Header, Configurations = Configurations };
            }
            else if (Ext == "c")
            {
                return new Cpp.File { Path = FilePath, Type = FileType.CSource, Configurations = Configurations };
            }
            else if ((Ext == "cc") || (Ext == "cpp") || (Ext == "cxx"))
            {
                return new Cpp.File { Path = FilePath, Type = FileType.CppSource, Configurations = Configurations };
            }
            else if (Ext == "ixx")
            {
                if ((Compiler == CompilerType.VisualCpp) && (VSVersion == 2022) && (WindowsRuntime == WindowsRuntimeType.Win32))
                {
                    return new Cpp.File { Path = FilePath, Type = FileType.CppSource, Configurations = Configurations };
                }
                else
                {
                    return new Cpp.File { Path = FilePath, Type = FileType.Unknown, Configurations = Configurations };
                }
            }
            else if (Ext == "m")
            {
                if ((TargetOperatingSystem == OperatingSystemType.iOS) || (TargetOperatingSystem == OperatingSystemType.MacOS))
                {
                    return new Cpp.File { Path = FilePath, Type = FileType.ObjectiveCSource, Configurations = Configurations };
                }
                else
                {
                    return new Cpp.File { Path = FilePath, Type = FileType.Unknown, Configurations = Configurations };
                }
            }
            else if (Ext == "mm")
            {
                if ((TargetOperatingSystem == OperatingSystemType.iOS) || (TargetOperatingSystem == OperatingSystemType.MacOS))
                {
                    return new Cpp.File { Path = FilePath, Type = FileType.ObjectiveCppSource, Configurations = Configurations };
                }
                else
                {
                    return new Cpp.File { Path = FilePath, Type = FileType.Unknown, Configurations = Configurations };
                }
            }
            else if (Ext == "storyboard")
            {
                return new Cpp.File { Path = FilePath, Type = FileType.EmbeddedContent, Configurations = Configurations };
            }
            else if (Ext == "xib")
            {
                return new Cpp.File { Path = FilePath, Type = FileType.EmbeddedContent, Configurations = Configurations };
            }
            else if (Ext == "natvis")
            {
                return new Cpp.File { Path = FilePath, Type = FileType.NatVis, Configurations = Configurations };
            }
            else
            {
                return new Cpp.File { Path = FilePath, Type = FileType.Unknown, Configurations = Configurations };
            }
        }
        private static List<KeyValuePair<String, String>> ParseDefines(String Defines)
        {
            if (Defines.Trim(' ') == "") { return new List<KeyValuePair<String, String>> { }; }
            return Defines.Split(';').Select(d => d.Split('=')).Select(arr => arr.Length >= 2 ? new KeyValuePair<String, String>(arr[0], arr[1]) : new KeyValuePair<String, String>(arr[0], null)).ToList();
        }
        private static Regex rFlag = new Regex(@"([^ ""]|""[^""]*"")+", RegexOptions.ExplicitCapture);
        private static List<String> ParseFlags(String Flags)
        {
            return rFlag.Matches(Flags).Cast<Match>().Select(m => m.Value).ToList();
        }
        private String GetIdForProject(String ProjectName)
        {
            if (ProjectIds.ContainsKey(ProjectName))
            {
                return ProjectIds[ProjectName];
            }
            if (Toolchain == ToolchainType.VisualStudio)
            {
                var g = Guid.ParseExact(Hash.GetHashForPath(ProjectName, 32), "N").ToString().ToUpper();
                ProjectIds.Add(ProjectName, g);
                return g;
            }
            else if (Toolchain == ToolchainType.XCode)
            {
                var g = Hash.GetHashForPath(ProjectName, 24);
                ProjectIds.Add(ProjectName, g);
                return g;
            }
            else if ((Toolchain == ToolchainType.Ninja) || (Toolchain == ToolchainType.Gradle_Ninja))
            {
                return "";
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        private static List<String> GetFullProjectDependencies(String ProjectName, Dictionary<String, HashSet<String>> Dependencies, out List<String> UnresovledDependencies)
        {
            return GetTransitiveProjectDependencies(ProjectName, Dependencies, _ => true, out UnresovledDependencies);
        }
        private static List<String> GetTransitiveProjectDependencies(String ProjectName, Dictionary<String, HashSet<String>> Dependencies, Func<String, bool> IsTransitive, out List<String> UnresovledDependencies)
        {
            var Full = new List<String>();
            var Unresovled = new List<String>();
            var Added = new HashSet<String>();
            var Queue = new Queue<String>();
            Queue.Enqueue(ProjectName);
            while (Queue.Count > 0)
            {
                var m = Queue.Dequeue();
                if (Added.Contains(m)) { continue; }
                if (!Dependencies.ContainsKey(m))
                {
                    Unresovled.Add(m);
                    continue;
                }
                if (Added.Count != 0)
                {
                    Full.Add(m);
                }
                if ((Added.Count == 0) || IsTransitive(m))
                {
                    foreach (var d in Dependencies[m])
                    {
                        Queue.Enqueue(d);
                    }
                }
                Added.Add(m);
            }
            UnresovledDependencies = Unresovled.Distinct().ToList();
            return Full;
        }
        private String GetProjectFileName(String ProjectName)
        {
            if (Toolchain == ToolchainType.VisualStudio)
            {
                return ProjectName + "/" + ProjectName + ".vcxproj";
            }
            else if (Toolchain == ToolchainType.XCode)
            {
                return ProjectName + ".xcodeproj";
            }
            else if ((Toolchain == ToolchainType.Ninja) || (Toolchain == ToolchainType.Gradle_Ninja))
            {
                return ProjectName;
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        private PathString GetProjectWorkingDirectory(PathString BuildDirectory, String ProjectName)
        {
            if (Toolchain == ToolchainType.VisualStudio)
            {
                return BuildDirectory / "projects" / ProjectName;
            }
            else if (Toolchain == ToolchainType.XCode)
            {
                return BuildDirectory / "projects";
            }
            else if ((Toolchain == ToolchainType.Ninja) || (Toolchain == ToolchainType.Gradle_Ninja))
            {
                return BuildDirectory / "projects";
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        private String GetProjectOutputFileName(String TargetName, TargetType TargetType)
        {
            if (TargetOperatingSystem == OperatingSystemType.Windows)
            {
                if (TargetType == TargetType.Executable)
                {
                    return TargetName + ".exe";
                }
                else if (TargetType == TargetType.StaticLibrary)
                {
                    return TargetName + ".lib";
                }
                else if (TargetType == TargetType.DynamicLibrary)
                {
                    return TargetName + ".dll";
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            else if (TargetOperatingSystem == OperatingSystemType.Linux)
            {
                if (TargetType == TargetType.Executable)
                {
                    return TargetName;
                }
                else if (TargetType == TargetType.StaticLibrary)
                {
                    return "lib" + TargetName + ".a";
                }
                else if (TargetType == TargetType.DynamicLibrary)
                {
                    return "lib" + TargetName + ".so";
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            else if (TargetOperatingSystem == OperatingSystemType.MacOS)
            {
                if (TargetType == TargetType.Executable)
                {
                    return TargetName;
                }
                else if (TargetType == TargetType.StaticLibrary)
                {
                    return "lib" + TargetName + ".a";
                }
                else if (TargetType == TargetType.DynamicLibrary)
                {
                    return "lib" + TargetName + ".dylib";
                }
                else if (TargetType == TargetType.DarwinApplication)
                {
                    return TargetName + ".app";
                }
                else if (TargetType == TargetType.DarwinStaticFramework)
                {
                    return TargetName + ".framework";
                }
                else if (TargetType == TargetType.DarwinSharedFramework)
                {
                    return TargetName + ".framework";
                }
                else if (TargetType == TargetType.MacBundle)
                {
                    return TargetName + ".bundle";
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            else if (TargetOperatingSystem == OperatingSystemType.Android)
            {
                if (TargetType == TargetType.Executable)
                {
                    return TargetName;
                }
                else if (TargetType == TargetType.StaticLibrary)
                {
                    return "lib" + TargetName + ".a";
                }
                else if (TargetType == TargetType.DynamicLibrary)
                {
                    return "lib" + TargetName + ".so";
                }
                else if (TargetType == TargetType.GradleApplication)
                {
                    return TargetName + ".apk";
                }
                else if (TargetType == TargetType.GradleLibrary)
                {
                    return TargetName + ".aar";
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            else if (TargetOperatingSystem == OperatingSystemType.iOS)
            {
                if (TargetType == TargetType.Executable)
                {
                    return TargetName;
                }
                else if (TargetType == TargetType.StaticLibrary)
                {
                    return "lib" + TargetName + ".a";
                }
                else if (TargetType == TargetType.DynamicLibrary)
                {
                    return "lib" + TargetName + ".dylib";
                }
                else if (TargetType == TargetType.DarwinApplication)
                {
                    return TargetName + ".app";
                }
                else if (TargetType == TargetType.DarwinStaticFramework)
                {
                    return TargetName + ".framework";
                }
                else if (TargetType == TargetType.DarwinSharedFramework)
                {
                    return TargetName + ".framework";
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        private PathString GetProjectOutputFilePath(String ProjectName, String TargetName, TargetType TargetType, PathString OutputDirectory = null, ConfigurationType? oConfigurationType = null)
        {
            var ConfigurationType = oConfigurationType ?? this.ConfigurationType;
            var OutputFileName = GetProjectOutputFileName(TargetName, TargetType);
            if ((TargetType == TargetType.GradleApplication) || (TargetType == TargetType.GradleLibrary))
            {
                if (Toolchain == ToolchainType.Ninja)
                {
                    var SimpleProjectName = ProjectName.Split(':').First();
                    return $"batch/{SimpleProjectName}".AsPath() / OutputFileName;
                }
                else if (Toolchain == ToolchainType.Gradle_Ninja)
                {
                    var SimpleProjectName = ProjectName.Split(':').First();
                    if (TargetType == TargetType.GradleApplication)
                    {
                        return $"gradle/{SimpleProjectName}/build/outputs/apk/{ConfigurationType.ToString().ToLowerInvariant()}/{OutputFileName.AsPath().FileNameWithoutExtension}-{ConfigurationType.ToString().ToLowerInvariant()}.apk".AsPath();
                    }
                    else if (TargetType == TargetType.GradleLibrary)
                    {
                        var Unsigned = ConfigurationType == ConfigurationType.Release ? "-unsigned" : "";
                        return $"gradle/{SimpleProjectName}/build/outputs/aar/{OutputFileName.AsPath().FileNameWithoutExtension}-{ConfigurationType.ToString().ToLowerInvariant()}{Unsigned}.aar".AsPath();
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            if (OutputDirectory != null)
            {
                return OutputDirectory / OutputFileName;
            }
            if (Toolchain == ToolchainType.VisualStudio)
            {
                return $"{ConfigurationType}".AsPath() / OutputFileName;
            }
            else if (Toolchain == ToolchainType.XCode)
            {
                if (TargetOperatingSystem == OperatingSystemType.MacOS)
                {
                    return $"{ConfigurationType}".AsPath() / OutputFileName;
                }
                else
                {
                    if (EnableMacCatalyst)
                    {
                        return $"{ConfigurationType}-maccatalyst".AsPath() / OutputFileName;
                    }
                    else
                    {
                        return $"{ConfigurationType}-iphoneos".AsPath() / OutputFileName;
                    }
                }
            }
            else if ((Toolchain == ToolchainType.Ninja) || (Toolchain == ToolchainType.Gradle_Ninja))
            {
                return $"{ConfigurationType}".AsPath() / OutputFileName;
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private static Dictionary<String, OperatingSystemType> OperatingSystemAlias = new Dictionary<String, OperatingSystemType>(StringComparer.OrdinalIgnoreCase)
        {
            ["Win"] = OperatingSystemType.Windows,
            ["WinRT"] = OperatingSystemType.Windows,
            ["Mac"] = OperatingSystemType.MacOS
        };
        private static bool IsOperatingSystemMatchExtensions(IEnumerable<String> Extensions, OperatingSystemType TargetOperatingSystem, bool IsExact = false)
        {
            var Names = new HashSet<String>(Enum.GetNames(typeof(OperatingSystemType)), StringComparer.OrdinalIgnoreCase);
            var TargetName = Enum.GetName(typeof(OperatingSystemType), TargetOperatingSystem);
            var MatchedAnyOperatingSystem = false;
            foreach (var e in Extensions)
            {
                if (String.Equals(e, TargetName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                if (OperatingSystemAlias.ContainsKey(e))
                {
                    if (OperatingSystemAlias[e] == TargetOperatingSystem)
                    {
                        return true;
                    }
                    else
                    {
                        MatchedAnyOperatingSystem = true;
                    }
                }
                if (Names.Contains(e))
                {
                    MatchedAnyOperatingSystem = true;
                }
            }
            if (MatchedAnyOperatingSystem)
            {
                return false;
            }
            return !IsExact;
        }
    }
}
