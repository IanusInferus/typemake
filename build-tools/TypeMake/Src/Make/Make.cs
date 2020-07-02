using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TypeMake.Cpp;

namespace TypeMake
{
    public class Make
    {
        public const String SolutionName = "TypeMakeSample";

        private OperatingSystemType HostOperatingSystem;
        private ArchitectureType HostArchitecture;
        private OperatingSystemType TargetOperatingSystem;
        private ArchitectureType TargetArchitecture;
        private WindowsRuntimeType? WindowsRuntime;
        private bool EnableMacCatalyst;
        private ConfigurationType ConfigurationType;
        private ToolchainType Toolchain;
        private CompilerType Compiler;
        private CLibraryType CLibrary;
        private CLibraryForm CLibraryForm;
        private CppLibraryType CppLibrary;
        private CppLibraryForm CppLibraryForm;
        private PathString SourceDirectory;
        private PathString BuildDirectory;
        private String XCodeDevelopmentTeam;
        private String XCodeProvisioningProfileSpecifier;
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

        public Make(OperatingSystemType HostOperatingSystem, ArchitectureType HostArchitecture, OperatingSystemType TargetOperatingSystem, ArchitectureType TargetArchitecture, WindowsRuntimeType? WindowsRuntime, bool EnableMacCatalyst, ToolchainType Toolchain, CompilerType Compiler, CLibraryType CLibrary, CLibraryForm CLibraryForm, CppLibraryType CppLibrary, CppLibraryForm CppLibraryForm, ConfigurationType ConfigurationType, PathString SourceDirectory, PathString BuildDirectory, String XCodeDevelopmentTeam, String XCodeProvisioningProfileSpecifier, int VSVersion, bool EnableJava, PathString Jdk, PathString AndroidSdk, PathString AndroidNdk, String CC, String CXX, String AR, String STRIP, List<String> CommonFlags, List<String> CFlags, List<String> CppFlags, List<String> LinkerFlags, List<String> PostLinkerFlags, bool ForceRegenerate, bool EnableNonTargetingOperatingSystemDummy)
        {
            this.HostOperatingSystem = HostOperatingSystem;
            this.HostArchitecture = HostArchitecture;
            this.TargetOperatingSystem = TargetOperatingSystem;
            this.TargetArchitecture = TargetArchitecture;
            this.WindowsRuntime = WindowsRuntime;
            this.EnableMacCatalyst = EnableMacCatalyst;
            this.Toolchain = Toolchain;
            this.Compiler = Compiler;
            this.CLibrary = CLibrary;
            this.CLibraryForm = CLibraryForm;
            this.CppLibrary = CppLibrary;
            this.CppLibraryForm = CppLibraryForm;
            this.ConfigurationType = ConfigurationType;
            this.SourceDirectory = SourceDirectory.FullPath;
            this.BuildDirectory = BuildDirectory.FullPath;
            this.XCodeDevelopmentTeam = XCodeDevelopmentTeam;
            this.XCodeProvisioningProfileSpecifier = XCodeProvisioningProfileSpecifier;
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
            public ProjectReference Reference;

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

            var Modules = Directory.EnumerateDirectories(SourceDirectory / "modules", "*", SearchOption.TopDirectoryOnly).Select(p => p.AsPath()).Select(p => new { ModuleName = p.FileName, ModulePath = p, VirtualDir = "modules" }).ToList();
            var Products = Directory.EnumerateDirectories(SourceDirectory / "products", "*", SearchOption.TopDirectoryOnly).Select(p => p.AsPath()).Select(p => new { ProductName = p.FileName, ProductPath = p, VirtualDir = "products" }).ToList();

            var ModuleDict = Modules.ToDictionary(m => m.ModuleName);

            //modules
            Add("math", "core");

            //products
            Add("basic.static", "math");
            Add("standard.dynamic", "math");
            Add("hello", "math");
            Add("hello.ios", "math");
            Add("hello.android", "math");

            var Projects = new List<ProjectDescription>();

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
                            Name = ModuleName,
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
                        Reference = new ProjectReference
                        {
                            Id = GetIdForProject(ModuleName),
                            Name = ModuleName,
                            VirtualDir = m.VirtualDir,
                            FilePath = BuildDirectory / "projects" / GetProjectFileName(ModuleName)
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
                                    Name = TestName,
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
                                Reference = new ProjectReference
                                {
                                    Id = GetIdForProject(TestName),
                                    Name = TestName,
                                    VirtualDir = m.VirtualDir,
                                    FilePath = BuildDirectory / "projects" / GetProjectFileName(TestName)
                                },
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
                            LinkerFlags = new List<string> { "-Wl,--version-script=" + (SourceDirectory / "products/export.version").RelativeTo((Toolchain == ToolchainType.CMake) || (Toolchain == ToolchainType.Gradle_CMake) ? BuildDirectory / "projects" / GetProjectFileName(ProductName) : BuildDirectory / "projects").ToString(PathStringStyle.Unix) }
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
                                Name = ProductName,
                                TargetName = TargetName,
                                TargetType = IsTargetOperatingSystemMatched ? ProductTargetType : TargetType.StaticLibrary,
                                Configurations = Configurations
                            },
                            BaseConfigurations = GetCommonConfigurations(),
                            ExportConfigurations = new List<Configuration> { },
                            Reference = new ProjectReference
                            {
                                Id = GetIdForProject(ProductName),
                                Name = ProductName,
                                VirtualDir = p.VirtualDir,
                                FilePath = BuildDirectory / "projects" / GetProjectFileName(ProductName)
                            },
                            PhysicalPath = InputDirectory,
                            DependentProjectToRequirement = DependentModuleToRequirement
                        });
                    }
                    if (((TargetOperatingSystem == OperatingSystemType.MacOS) || (TargetOperatingSystem == OperatingSystemType.iOS)) && (ProductTargetType == TargetType.DynamicLibrary))
                    {
                        var FrameworkTargetType = TargetType.DarwinSharedFramework;
                        var FrameworkName = ProductName + ".framework";
                        Projects.Add(new ProjectDescription
                        {
                            Definition = new Project
                            {
                                Name = FrameworkName,
                                TargetName = TargetName,
                                TargetType = FrameworkTargetType,
                                Configurations = Configurations
                            },
                            BaseConfigurations = GetCommonConfigurations(),
                            ExportConfigurations = new List<Configuration> { },
                            Reference = new ProjectReference
                            {
                                Id = GetIdForProject(FrameworkName),
                                Name = FrameworkName,
                                VirtualDir = p.VirtualDir,
                                FilePath = BuildDirectory / "projects" / GetProjectFileName(FrameworkName)
                            },
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
                                Name = ProductName + ":" + GradleTargetType.Value.ToString(),
                                TargetName = TargetName,
                                TargetType = GradleTargetType.Value,
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
                            Reference = new ProjectReference
                            {
                                Id = GetIdForProject(ProductName),
                                Name = ProductName + ":" + GradleTargetType.Value.ToString(),
                                VirtualDir = p.VirtualDir,
                                FilePath = BuildDirectory / "gradle" / GetProjectFileName(ProductName)
                            },
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
            public List<KeyValuePair<ProjectReference, List<ProjectReference>>> Projects;
            public List<ProjectReference> SortedProjects;
            public bool NeedInstallStrip;
        }
        public Result Execute(Dictionary<String, ProjectDescription> SelectedProjects)
        {
            var Dependencies = SelectedProjects.Values.ToDictionary(p => p.Definition.Name, p => p.DependentProjectToRequirement);
            var ProjectNameToDependencies = SelectedProjects.Values.Select(p => p.Reference).ToDictionary(p => p.Name);
            var Projects = new List<KeyValuePair<ProjectReference, List<ProjectReference>>>();
            var NeedInstallStrip = false;
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
            foreach (var Project in SelectedProjects.Values)
            {
                var FullDependentProjectNames = GetFullProjectDependencies(Project.Definition.Name, Dependencies, out var Unresovled);
                if (Unresovled.Count > 0)
                {
                    throw new InvalidOperationException($"UnresolvedDependencies: {Project.Definition.Name} -> {String.Join(" ", Unresovled)}");
                }
                var ProjectReference = Project.Reference;
                var ProjectReferences = FullDependentProjectNames.Select(d => ProjectNameToDependencies[d]).ToList();
                var DependentProjectExportConfigurations = FullDependentProjectNames.SelectMany(d => SelectedProjects[d].ExportConfigurations).ToList();
                var p = new Project
                {
                    Name = Project.Definition.Name,
                    TargetName = Project.Definition.TargetName,
                    TargetType = Project.Definition.TargetType,
                    Configurations = Project.BaseConfigurations.Concat(DependentProjectExportConfigurations).Concat(Project.Definition.Configurations).Concat(ExternalConfigurations).ToList()
                };
                var InputDirectory = Project.PhysicalPath;
                var OutputDirectory = Project.Reference.FilePath.Parent;
                var ProjectTargetType = Project.Definition.TargetType;
                if (Toolchain == ToolchainType.VisualStudio)
                {
                    var VcxprojTemplateText = Resource.GetResourceText(VSVersion == 2019 ? @"Templates\vc16\Default.vcxproj" : @"Templates\vc15\Default.vcxproj");
                    var VcxprojFilterTemplateText = Resource.GetResourceText(VSVersion == 2019 ? @"Templates\vc16\Default.vcxproj.filters" : @"Templates\vc15\Default.vcxproj.filters");
                    var g = new VcxprojGenerator(p, ProjectReference.Id, ProjectReferences, InputDirectory, OutputDirectory, VcxprojTemplateText, VcxprojFilterTemplateText, HostOperatingSystem, HostArchitecture, TargetOperatingSystem, WindowsRuntime.Value, CLibraryForm, CppLibraryForm);
                    g.Generate(ForceRegenerate);
                }
                else if (Toolchain == ToolchainType.XCode)
                {
                    var PbxprojTemplateText = Resource.GetResourceText(@"Templates\xcode9\Default.xcodeproj\project.pbxproj");
                    var g = new PbxprojGenerator(p, ProjectReferences, InputDirectory, OutputDirectory, PbxprojTemplateText, HostOperatingSystem, HostArchitecture, TargetOperatingSystem, CppLibraryForm, XCodeDevelopmentTeam, XCodeProvisioningProfileSpecifier);
                    g.Generate(ForceRegenerate);
                }
                else if (Toolchain == ToolchainType.CMake)
                {
                    var g = new CMakeProjectGenerator(p, ProjectReferences, InputDirectory, OutputDirectory, HostOperatingSystem, HostArchitecture, TargetOperatingSystem, TargetArchitecture, WindowsRuntime, Toolchain, Compiler, CLibrary, CLibraryForm, CppLibrary, CppLibraryForm, ConfigurationType, false);
                    var ProjectNeedInstallStrip = false;
                    g.Generate(ForceRegenerate, out ProjectNeedInstallStrip);
                    NeedInstallStrip = NeedInstallStrip || ProjectNeedInstallStrip;
                }
                else if ((Toolchain == ToolchainType.Ninja) && (TargetOperatingSystem != OperatingSystemType.Android))
                {
                    var g = new NinjaProjectGenerator(p, ProjectReferences, InputDirectory, OutputDirectory, HostOperatingSystem, HostArchitecture, TargetOperatingSystem, TargetArchitecture, WindowsRuntime, Toolchain, Compiler, CLibrary, CLibraryForm, CppLibrary, CppLibraryForm, ConfigurationType);
                    g.Generate(ForceRegenerate);
                }
                else if ((Toolchain == ToolchainType.Gradle_CMake) || (Toolchain == ToolchainType.Gradle_Ninja) || (Toolchain == ToolchainType.Ninja))
                {
                    if (ProjectTargetType == TargetType.GradleApplication)
                    {
                        if (EnableJava)
                        {
                            if (Toolchain == ToolchainType.Ninja)
                            {
                                var Out = OutputDirectory.FileName == "gradle" ? OutputDirectory.Parent / "batch" : OutputDirectory;
                                var gBatch = new AndroidBatchProjectGenerator(SolutionName, p, ProjectReferences, InputDirectory, Out, BuildDirectory, HostOperatingSystem, HostArchitecture, TargetOperatingSystem, TargetArchitecture, Toolchain, Compiler, CLibrary, CLibraryForm, CppLibrary, CppLibraryForm, ConfigurationType, Jdk, AndroidSdk, AndroidNdk, "29.0.3", 15, 29, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).AsPath() / ".android/debug.keystore", "android", "androiddebugkey", "android", true);
                                gBatch.Generate(ForceRegenerate);
                            }
                            else
                            {
                                var BuildGradleTemplateText = Resource.GetResourceText(@"Templates\gradle_application\build.gradle");
                                var gGradle = new GradleProjectGenerator(SolutionName, p, ProjectReferences, InputDirectory, OutputDirectory, BuildDirectory, BuildGradleTemplateText, HostOperatingSystem, HostArchitecture, TargetOperatingSystem, TargetArchitecture, Toolchain, Compiler, CLibrary, CLibraryForm, CppLibrary, CppLibraryForm, ConfigurationType, AndroidNdk);
                                gGradle.Generate(ForceRegenerate);
                            }
                        }
                        continue;
                    }
                    else if (ProjectTargetType == TargetType.GradleLibrary)
                    {
                        if (EnableJava)
                        {
                            if (Toolchain == ToolchainType.Ninja)
                            {
                                var Out = OutputDirectory.FileName == "gradle" ? OutputDirectory.Parent / "batch" : OutputDirectory;
                                var gBatch = new AndroidBatchProjectGenerator(SolutionName, p, ProjectReferences, InputDirectory, Out, BuildDirectory, HostOperatingSystem, HostArchitecture, TargetOperatingSystem, TargetArchitecture, Toolchain, Compiler, CLibrary, CLibraryForm, CppLibrary, CppLibraryForm, ConfigurationType, Jdk, AndroidSdk, AndroidNdk, "29.0.3", 15, 29, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).AsPath() / ".android/debug.keystore", "android", "androiddebugkey", "android", true);
                                gBatch.Generate(ForceRegenerate);
                            }
                            else
                            {
                                var BuildGradleTemplateText = Resource.GetResourceText(@"Templates\gradle_library\build.gradle");
                                var gGradle = new GradleProjectGenerator(SolutionName, p, ProjectReferences, InputDirectory, OutputDirectory, BuildDirectory, BuildGradleTemplateText, HostOperatingSystem, HostArchitecture, TargetOperatingSystem, TargetArchitecture, Toolchain, Compiler, CLibrary, CLibraryForm, CppLibrary, CppLibraryForm, ConfigurationType, AndroidNdk);
                                gGradle.Generate(ForceRegenerate);
                            }
                        }
                        continue;
                    }
                    else
                    {
                        if (Toolchain == ToolchainType.Gradle_CMake)
                        {
                            var g = new CMakeProjectGenerator(p, ProjectReferences, InputDirectory, OutputDirectory, HostOperatingSystem, HostArchitecture, TargetOperatingSystem, TargetArchitecture, WindowsRuntime, Toolchain, Compiler, CLibrary, CLibraryForm, CppLibrary, CppLibraryForm, ConfigurationType, HostOperatingSystem == OperatingSystemType.Windows);
                            var ProjectNeedInstallStrip = false;
                            g.Generate(ForceRegenerate, out ProjectNeedInstallStrip);
                            NeedInstallStrip = NeedInstallStrip || ProjectNeedInstallStrip;
                        }
                        else if (Toolchain == ToolchainType.Gradle_Ninja)
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
                Projects.Add(new KeyValuePair<ProjectReference, List<ProjectReference>>(ProjectReference, ProjectReferences));
            }
            var GradleProjectNames = SelectedProjects.Values.Where(Project => (Project.Definition.TargetType == TargetType.GradleLibrary) || (Project.Definition.TargetType == TargetType.GradleApplication)).Select(Project => Project.Definition.Name).ToList();
            var ProjectDict = Projects.ToDictionary(p => p.Key.Name, p => p.Key);
            var ProjectDependencies = Projects.ToDictionary(p => ProjectDict[p.Key.Name], p => p.Value.Select(n => ProjectDict[n.Name]).ToList());
            var SortedProjects = Projects.Select(p => p.Key).PartialOrderBy(p => ProjectDependencies.ContainsKey(p) ? ProjectDependencies[p] : null).ToList();
            if (Toolchain == ToolchainType.VisualStudio)
            {
                var SlnTemplateText = Resource.GetResourceText(VSVersion == 2019 ? @"Templates\vc16\Default.sln" : @"Templates\vc15\Default.sln");
                var g = new SlnGenerator(SolutionName, GetIdForProject(SolutionName + ".solution"), Projects.Select(p => p.Key).ToList(), BuildDirectory, SlnTemplateText);
                g.Generate(ForceRegenerate);
            }
            else if (Toolchain == ToolchainType.XCode)
            {
                var g = new XcworkspaceGenerator(SolutionName, Projects.Select(p => p.Key).ToList(), BuildDirectory);
                g.Generate(ForceRegenerate);
            }
            else if (Toolchain == ToolchainType.CMake)
            {
                var g = new CMakeSolutionGenerator(SolutionName, SortedProjects, BuildDirectory, CC, CXX, AR, STRIP);
                g.Generate(ForceRegenerate);
            }
            else if (Toolchain == ToolchainType.Ninja)
            {
                var g = new NinjaSolutionGenerator(SolutionName, SortedProjects, BuildDirectory / "projects", CC, CXX, AR, STRIP);
                g.Generate(ForceRegenerate);
            }
            else if ((Toolchain == ToolchainType.Gradle_CMake) || (Toolchain == ToolchainType.Gradle_Ninja))
            {
                FileUtils.CopyDirectory(System.Reflection.Assembly.GetEntryAssembly().Location.AsPath().Parent / "Templates/gradle", BuildDirectory / "gradle", !ForceRegenerate);
                if (HostOperatingSystem != Cpp.OperatingSystemType.Windows)
                {
                    if (Shell.Execute("chmod", "+x", BuildDirectory / "gradle/gradlew") != 0)
                    {
                        throw new InvalidOperationException("ErrorInExecution: chmod");
                    }
                }
                if (Toolchain == ToolchainType.Gradle_CMake)
                {
                    var g = new CMakeSolutionGenerator(SolutionName, SortedProjects, BuildDirectory, CC, CXX, AR, STRIP);
                    g.Generate(ForceRegenerate);
                }
                else if (Toolchain == ToolchainType.Gradle_Ninja)
                {
                    var g = new NinjaSolutionGenerator(SolutionName, SortedProjects, BuildDirectory / "projects", CC, CXX, AR, STRIP);
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
            return new Result { SolutionName = SolutionName, Projects = Projects, SortedProjects = SortedProjects, NeedInstallStrip = NeedInstallStrip };
        }

        private List<Configuration> GetCommonConfigurations()
        {
            var Configurations = new List<Configuration>
            {
                new Configuration
                {
                    MatchingCompilers = new List<CompilerType> { CompilerType.VisualCpp },
                    Options = new Dictionary<String, String>
                    {
                        ["vc.ClCompile.LanguageStandard"] = "stdcpp17"
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
                    CppFlags = ParseFlags("-std=c++17")
                },
                new Configuration
                {
                    MatchingCompilers = new List<CompilerType> { CompilerType.VisualCpp },
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
                    MatchingCompilers = new List<CompilerType> { CompilerType.VisualCpp },
                    CommonFlags = ParseFlags("/we4172 /we4715")
                },
                new Configuration
                {
                    MatchingCompilers = new List<CompilerType> { CompilerType.gcc, CompilerType.clang },
                    CommonFlags = ParseFlags("-Werror=return-type -Werror=address -Werror=sequence-point -Werror=format-security -Werror=int-to-pointer-cast -Werror=pointer-to-int-cast -Wformat -Wuninitialized -Winit-self -Wpointer-arith -Wundef"),
                    CFlags = ParseFlags("-Wstrict-prototypes -Wimplicit-function-declaration"),
                    CppFlags = ParseFlags("-Wsign-promo")
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
                    MatchingTargetOperatingSystems = new List<OperatingSystemType> { OperatingSystemType.MacOS },
                    Options = new Dictionary<String, String>
                    {
                        ["xcode.project.VALID_ARCHS"] = "x86_64"
                    }
                },
                new Configuration
                {
                    MatchingTargetOperatingSystems = new List<OperatingSystemType> { OperatingSystemType.iOS },
                    Options = new Dictionary<String, String>
                    {
                        ["xcode.project.VALID_ARCHS"] = "arm64"
                    }
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
                    MatchingCompilers = new List<CompilerType> { CompilerType.VisualCpp },
                    MatchingCLibraryForms = new List<CLibraryForm> { CLibraryForm.Static },
                    MatchingConfigurationTypes = new List<ConfigurationType> { Cpp.ConfigurationType.Debug },
                    Options = new Dictionary<String, String>
                    {
                        ["vc.ClCompile.RuntimeLibrary"] = "MultiThreadedDebug",
                    }
                },
                new Configuration
                {
                    MatchingCompilers = new List<CompilerType> { CompilerType.VisualCpp },
                    MatchingCLibraryForms = new List<CLibraryForm> { CLibraryForm.Dynamic },
                    MatchingConfigurationTypes = new List<ConfigurationType> { Cpp.ConfigurationType.Debug },
                    Options = new Dictionary<String, String>
                    {
                        ["vc.ClCompile.RuntimeLibrary"] = "MultiThreadedDebugDLL",
                    }
                },
                new Configuration
                {
                    MatchingCompilers = new List<CompilerType> { CompilerType.VisualCpp },
                    MatchingCLibraryForms = new List<CLibraryForm> { CLibraryForm.Static },
                    MatchingConfigurationTypes = new List<ConfigurationType> { Cpp.ConfigurationType.Release },
                    Options = new Dictionary<String, String>
                    {
                        ["vc.ClCompile.RuntimeLibrary"] = "MultiThreaded",
                    }
                },
                new Configuration
                {
                    MatchingCompilers = new List<CompilerType> { CompilerType.VisualCpp },
                    MatchingCLibraryForms = new List<CLibraryForm> { CLibraryForm.Dynamic },
                    MatchingConfigurationTypes = new List<ConfigurationType> { Cpp.ConfigurationType.Release },
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
                    MatchingCompilers = new List<CompilerType> { CompilerType.gcc },
                    MatchingCLibraryForms = new List<CLibraryForm> { CLibraryForm.Dynamic },
                    MatchingCppLibraryForms = new List<CppLibraryForm> { CppLibraryForm.Static },
                    MatchingTargetOperatingSystems = new List<OperatingSystemType> { OperatingSystemType.Linux, OperatingSystemType.Android },
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
                    MatchingConfigurationTypes = new List<ConfigurationType> { Cpp.ConfigurationType.Debug },
                    Defines = ParseDefines("DEBUG=1")
                },
                new Configuration
                {
                    MatchingConfigurationTypes = new List<ConfigurationType> { Cpp.ConfigurationType.Release },
                    Defines = ParseDefines("NDEBUG")
                },
                new Configuration
                {
                    MatchingCompilers = new List<CompilerType> { CompilerType.clang },
                    MatchingTargetOperatingSystems = new List<OperatingSystemType> { OperatingSystemType.Windows },
                    MatchingConfigurationTypes = new List<ConfigurationType> { Cpp.ConfigurationType.Debug },
                    Defines = ParseDefines("_DEBUG;_MT;_DLL"),
                    CommonFlags = ParseFlags("-gcodeview-ghash"),
                    LinkerFlags = ParseFlags("-Wl,/debug -Wl,/nodefaultlib:libucrt -Wl,/nodefaultlib:libvcruntime -Wl,/nodefaultlib:libcmt"), //workaround llvm bug choosing C runtime, https://docs.microsoft.com/en-us/cpp/c-runtime-library/crt-library-features?view=vs-2019
                    Libs = new List<PathString> { "ucrtd", "vcruntimed", "msvcrtd" }
                },
                new Configuration
                {
                    MatchingCompilers = new List<CompilerType> { CompilerType.clang },
                    MatchingTargetOperatingSystems = new List<OperatingSystemType> { OperatingSystemType.Windows },
                    MatchingConfigurationTypes = new List<ConfigurationType> { Cpp.ConfigurationType.Release },
                    Defines = ParseDefines("_MT"),
                    CommonFlags = ParseFlags("-gcodeview-ghash"),
                    LinkerFlags = ParseFlags("-Wl,/debug")
                },
                new Configuration
                {
                    MatchingCompilers = new List<CompilerType> { CompilerType.gcc, CompilerType.clang },
                    MatchingConfigurationTypes = new List<ConfigurationType> { Cpp.ConfigurationType.Debug },
                    CommonFlags = ParseFlags("-O0")
                },
                new Configuration
                {
                    MatchingCompilers = new List<CompilerType> { CompilerType.gcc, CompilerType.clang },
                    MatchingConfigurationTypes = new List<ConfigurationType> { Cpp.ConfigurationType.Release },
                    CommonFlags = ParseFlags("-O3")
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
                        ["xcode.project.IPHONEOS_DEPLOYMENT_TARGET"] = "8.0"
                    }
                },
                new Configuration
                {
                    MatchingToolchains = new List<ToolchainType> { ToolchainType.Ninja, ToolchainType.Gradle_Ninja },
                    MatchingTargetOperatingSystems = new List<OperatingSystemType> { OperatingSystemType.Android },
                    CommonFlags = ParseFlags("-fno-addrsig -fPIE -fPIC -DANDROID -D_FORTIFY_SOURCE=2 -fdata-sections -ffunction-sections -funwind-tables -fstack-protector-strong -no-canonical-prefixes -Wa,--noexecstack -Werror=fortify-source"),
                    // https://android.googlesource.com/platform/ndk/+/master/docs/BuildSystemMaintainers.md#Unwinding
                    LinkerFlags = ParseFlags("-Wl,--exclude-libs,libgcc.a -Wl,--exclude-libs,libatomic.a -Wl,--build-id=sha1 -Wl,--warn-shared-textrel -Wl,--fatal-warnings -Wl,--no-undefined -Wl,-z,noexecstack -Wl,-z,relro -Wl,-z,now")
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
            foreach (var Architecture in Enum.GetValues(typeof(ArchitectureType)).Cast<ArchitectureType>())
            {
                foreach (var ConfigurationType in Enum.GetValues(typeof(ConfigurationType)).Cast<ConfigurationType>())
                {
                    Configurations.Add(new Configuration
                    {
                        MatchingCompilers = new List<CompilerType> { CompilerType.VisualCpp },
                        MatchingTargetArchitectures = new List<ArchitectureType> { Architecture },
                        MatchingConfigurationTypes = new List<ConfigurationType> { ConfigurationType },
                        LibDirectories = new List<PathString> { BuildDirectory / $"{Architecture}_{ConfigurationType}" }
                    });
                }
            }
            if (EnableMacCatalyst)
            {
                Configurations.Add(new Configuration
                {
                    MatchingTargetOperatingSystems = new List<OperatingSystemType> { OperatingSystemType.iOS },
                    Options = new Dictionary<String, String>
                    {
                        ["xcode.project.TARGETED_DEVICE_FAMILY"] = "1,2",
                        ["xcode.project.VALID_ARCHS"] = "arm64 x86_64",
                        ["xcode.project.SUPPORTS_MACCATALYST"] = "YES",
                        ["xcode.project.IPHONEOS_DEPLOYMENT_TARGET"] = "8.0",
                        ["xcode.project.MACOSX_DEPLOYMENT_TARGET"] = "10.15"
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
            foreach (var FilePathRelative in Directory.EnumerateDirectories(d, "*", SearchOption.TopDirectoryOnly))
            {
                var FilePath = FilePathRelative.AsPath().FullPath;
                var Ext = FilePath.Extension.ToLowerInvariant();
                if (Ext == "xcassets")
                {
                    Results.Add(new Cpp.File { Path = FilePath, Type = FileType.EmbeddedContent });
                    continue;
                }
                if (!TopOnly)
                {
                    FillFilesInDirectory(FilePathRelative, TargetOperatingSystem, IsTargetOperatingSystemMatched, TopOnly, Results);
                }
            }
            foreach (var FilePathRelative in Directory.EnumerateFiles(d, "*", SearchOption.TopDirectoryOnly))
            {
                var FilePath = FilePathRelative.AsPath().FullPath;
                Results.Add(GetFileByPath(FilePath, TargetOperatingSystem, IsTargetOperatingSystemMatched));
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
            else if ((Toolchain == ToolchainType.CMake) || (Toolchain == ToolchainType.Ninja) || (Toolchain == ToolchainType.Gradle_CMake) || (Toolchain == ToolchainType.Gradle_Ninja))
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
                Added.Add(m);
                foreach (var d in Dependencies[m])
                {
                    Queue.Enqueue(d);
                }
            }
            UnresovledDependencies = Unresovled;
            return Full;
        }
        private String GetProjectFileName(String ProjectName)
        {
            if (Toolchain == ToolchainType.VisualStudio)
            {
                return ProjectName + ".vcxproj";
            }
            else if (Toolchain == ToolchainType.XCode)
            {
                return ProjectName + ".xcodeproj";
            }
            else if ((Toolchain == ToolchainType.CMake) || (Toolchain == ToolchainType.Ninja) || (Toolchain == ToolchainType.Gradle_CMake) || (Toolchain == ToolchainType.Gradle_Ninja))
            {
                return ProjectName;
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
