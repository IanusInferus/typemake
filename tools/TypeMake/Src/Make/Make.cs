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
        private const String SolutionName = "TypeMakeSample";

        private ToolchainType Toolchain;
        private CompilerType Compiler;
        private OperatingSystemType BuildingOperatingSystem;
        private ArchitectureType BuildingOperatingSystemArchitecture;
        private OperatingSystemType TargetOperatingSystem;
        private ArchitectureType? TargetArchitecture;
        private ConfigurationType? ConfigurationType;
        private PathString SourceDirectory;
        private PathString BuildDirectory;
        private String XCodeDevelopmentTeam;
        private bool ForceRegenerate;
        private bool EnableNonTargetingOperatingSystemDummy;

        private Dictionary<String, String> ProjectIds = new Dictionary<String, String>();

        public Make(ToolchainType Toolchain, CompilerType Compiler, OperatingSystemType BuildingOperatingSystem, ArchitectureType BuildingOperatingSystemArchitecture, OperatingSystemType TargetOperatingSystem, ArchitectureType? TargetArchitecture, ConfigurationType? ConfigurationType, PathString SourceDirectory, PathString BuildDirectory, String XCodeDevelopmentTeam, bool ForceRegenerate, bool EnableNonTargetingOperatingSystemDummy)
        {
            this.Toolchain = Toolchain;
            this.Compiler = Compiler;
            this.BuildingOperatingSystem = BuildingOperatingSystem;
            this.BuildingOperatingSystemArchitecture = BuildingOperatingSystemArchitecture;
            this.TargetOperatingSystem = TargetOperatingSystem;
            this.TargetArchitecture = TargetArchitecture;
            this.ConfigurationType = ConfigurationType;
            this.SourceDirectory = SourceDirectory.FullPath;
            this.BuildDirectory = BuildDirectory.FullPath;
            this.XCodeDevelopmentTeam = XCodeDevelopmentTeam;
            this.ForceRegenerate = ForceRegenerate;
            this.EnableNonTargetingOperatingSystemDummy = EnableNonTargetingOperatingSystemDummy;
        }

        public class ProjectDescription
        {
            public Project Definition;
            public List<Configuration> ExportConfigurations;
            public ProjectReference Reference;

            public PathString PhysicalPath;
            public Dictionary<String, bool> DependentProjectToRequirement;
        }
        public Dictionary<String, ProjectDescription> GetAvailableProjects()
        {
            var Dependencies = new Dictionary<String, Dictionary<String, bool>>();

            void Add(String Depender, String Dependee, bool IsRequired = true)
            {
                if (Dependencies.ContainsKey(Depender))
                {
                    var d = Dependencies[Depender];
                    if (d.ContainsKey(Dependee))
                    {
                        throw new ArgumentException();
                    }
                    d.Add(Dependee, IsRequired);
                }
                else
                {
                    Dependencies.Add(Depender, new Dictionary<String, bool> { [Dependee] = IsRequired });
                }
            }
            void AddRequired(String Depender, params String[] Dependees)
            {
                foreach (var Dependee in Dependees)
                {
                    Add(Depender, Dependee, true);
                }
            }
            void AddOptional(String Depender, params String[] Dependees)
            {
                foreach (var Dependee in Dependees)
                {
                    Add(Depender, Dependee, false);
                }
            }

            var Modules = Directory.EnumerateDirectories(SourceDirectory / "modules", "*", SearchOption.TopDirectoryOnly).Select(p => p.AsPath()).Select(p => new { ModuleName = p.FileName, ModulePath = p, VirtualDir = "modules" }).ToList();
            var Products = Directory.EnumerateDirectories(SourceDirectory / "products", "*", SearchOption.TopDirectoryOnly).Select(p => p.AsPath()).Select(p => new { ProductName = p.FileName, ProductPath = p, VirtualDir = "products" }).ToList();

            var ModuleDict = Modules.ToDictionary(m => m.ModuleName);

            //modules
            AddRequired("math", "core");

            //products
            AddRequired("basic.static", "math");
            AddRequired("standard.dynamic", "math");
            AddRequired("hello", "math");
            AddRequired("hello.ios", "math");
            AddRequired("hello.android", "math");

            var Projects = new List<ProjectDescription>();

            foreach (var m in Modules)
            {
                var ModuleName = m.ModuleName;
                var InputDirectory = m.ModulePath;
                var Extensions = ModuleName.Split('.').Skip(1).ToArray();
                var IsTargetOperatingSystemMatched = IsOperatingSystemMatchExtensions(Extensions, TargetOperatingSystem);
                if (IsTargetOperatingSystemMatched || EnableNonTargetingOperatingSystemDummy)
                {
                    var DependentModuleToRequirement = Dependencies.ContainsKey(ModuleName) ? Dependencies[ModuleName] : new Dictionary<String, bool>();
                    Projects.Add(new ProjectDescription
                    {
                        Definition = new Project
                        {
                            Name = ModuleName,
                            TargetType = TargetType.StaticLibrary,
                            Configurations = (new List<Configuration>
                            {
                                new Configuration
                                {
                                    IncludeDirectories = new List<PathString> { InputDirectory / "include", InputDirectory / "src" },
                                    Files = new List<PathString> { InputDirectory / "include", InputDirectory / "src" }.SelectMany(d => GetFilesInDirectory(d, TargetOperatingSystem, IsTargetOperatingSystemMatched)).ToList()
                                }
                            }).Concat(GetCommonConfigurations()).ToList()
                        },
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
                    if (!((TargetOperatingSystem == OperatingSystemType.Android) || (TargetOperatingSystem == OperatingSystemType.iOS)))
                    {
                        foreach (var TestFile in GetFilesInDirectory(InputDirectory / "test", TargetOperatingSystem, IsTargetOperatingSystemMatched))
                        {
                            if (TestFile.Type != FileType.CppSource) { continue; }
                            var TestName = ModuleName + "_" + Regex.Replace(TestFile.Path.FullPath.RelativeTo(InputDirectory), @"[\\/]", "_").AsPath().FileNameWithoutExtension;
                            var TestDependentModuleToRequirement = DependentModuleToRequirement.ToDictionary(p => p.Key, p => p.Value);
                            TestDependentModuleToRequirement.Add(ModuleName, true);
                            Projects.Add(new ProjectDescription
                            {
                                Definition = new Project
                                {
                                    Name = TestName,
                                    TargetType = TargetType.Executable,
                                    Configurations = (new List<Configuration>
                                    {
                                        new Configuration
                                        {
                                            IncludeDirectories = new List<PathString> { InputDirectory / "include", InputDirectory / "src" },
                                            Files = new List<Cpp.File> { TestFile }
                                        }
                                    }).Concat(GetCommonConfigurations()).ToList()
                                },
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
                    if (TargetOperatingSystem == OperatingSystemType.Mac)
                    {
                        ProductTargetType = TargetType.MacApplication;
                    }
                    else if (TargetOperatingSystem == OperatingSystemType.iOS)
                    {
                        ProductTargetType = TargetType.iOSApplication;
                    }
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
                if (ProductTargetType == TargetType.iOSApplication)
                {
                    IsTargetOperatingSystemMatched = IsOperatingSystemMatchExtensions(Extensions, TargetOperatingSystem, true);
                }
                if (IsTargetOperatingSystemMatched || EnableNonTargetingOperatingSystemDummy)
                {
                    var DependentModuleToRequirement = Dependencies.ContainsKey(ProductName) ? Dependencies[ProductName] : new Dictionary<String, bool>();
                    var Defines = new List<KeyValuePair<String, String>> { };
                    if ((ProductTargetType == TargetType.Executable) || (ProductTargetType == TargetType.MacApplication) || (ProductTargetType == TargetType.iOSApplication) || (ProductTargetType == TargetType.GradleApplication))
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
                            MatchingTargetOperatingSystems = new List<OperatingSystemType> { OperatingSystemType.Mac, OperatingSystemType.iOS },
                            MatchingTargetTypes = new List<TargetType> { TargetType.MacApplication, TargetType.MacBundle, TargetType.iOSApplication, TargetType.iOSStaticFramework, TargetType.iOSSharedFramework },
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
                                Configurations = Configurations.Concat(GetCommonConfigurations()).ToList()
                            },
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
                    if ((TargetOperatingSystem == OperatingSystemType.Mac) && (ProductTargetType == TargetType.DynamicLibrary))
                    {
                        var BundleName = ProductName + ".bundle";
                        Projects.Add(new ProjectDescription
                        {
                            Definition = new Project
                            {
                                Name = BundleName,
                                TargetName = TargetName,
                                TargetType = TargetType.MacBundle,
                                Configurations = Configurations.Concat(GetCommonConfigurations()).ToList()
                            },
                            ExportConfigurations = new List<Configuration> { },
                            Reference = new ProjectReference
                            {
                                Id = GetIdForProject(BundleName),
                                Name = BundleName,
                                VirtualDir = p.VirtualDir,
                                FilePath = BuildDirectory / "projects" / GetProjectFileName(BundleName)
                            },
                            PhysicalPath = InputDirectory,
                            DependentProjectToRequirement = DependentModuleToRequirement
                        });
                    }
                    else if ((TargetOperatingSystem == OperatingSystemType.iOS) && (ProductTargetType == TargetType.DynamicLibrary))
                    {
                        foreach (var FrameworkTargetType in new List<TargetType> { TargetType.iOSStaticFramework, TargetType.iOSSharedFramework })
                        {
                            var Type = FrameworkTargetType.ToString().Replace("iOS", "").Replace("Framework", "");
                            var OutputDirConfigurations = new List<Configuration> { };
                            foreach (var Architecture in Enum.GetValues(typeof(ArchitectureType)).Cast<ArchitectureType>())
                            {
                                foreach (var ConfigurationType in Enum.GetValues(typeof(ConfigurationType)).Cast<ConfigurationType>())
                                {
                                    OutputDirConfigurations.Add(new Configuration
                                    {
                                        MatchingTargetArchitectures = new List<ArchitectureType> { Architecture },
                                        MatchingConfigurationTypes = new List<ConfigurationType> { ConfigurationType },
                                        OutputDirectory = BuildDirectory / $"{Architecture}_{ConfigurationType}/{Type}"
                                    });
                                }
                            }
                            var FrameworkName = ProductName + "." + Type.ToLowerInvariant();
                            Projects.Add(new ProjectDescription
                            {
                                Definition = new Project
                                {
                                    Name = FrameworkName,
                                    TargetName = TargetName,
                                    TargetType = FrameworkTargetType,
                                    Configurations = Configurations.Concat(OutputDirConfigurations).Concat(GetCommonConfigurations()).ToList()
                                },
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
                            ExportConfigurations = new List<Configuration> { },
                            Reference = new ProjectReference
                            {
                                Id = GetIdForProject(ProductName),
                                Name = ProductName + ":" + GradleTargetType.Value.ToString(),
                                VirtualDir = p.VirtualDir,
                                FilePath = BuildDirectory / "gradle" / GetProjectFileName(ProductName)
                            },
                            PhysicalPath = InputDirectory,
                            DependentProjectToRequirement = new Dictionary<String, bool> { [ProductName] = true }
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
        }
        public Result Execute(Dictionary<String, ProjectDescription> SelectedProjects)
        {
            var Dependencies = SelectedProjects.Values.ToDictionary(p => p.Definition.Name, p => p.DependentProjectToRequirement);
            var ProjectNameToDependencies = SelectedProjects.Values.Select(p => p.Reference).ToDictionary(p => p.Name);
            var Projects = new List<KeyValuePair<ProjectReference, List<ProjectReference>>>();
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
                    Configurations = DependentProjectExportConfigurations.Concat(Project.Definition.Configurations).ToList()
                };
                var InputDirectory = Project.PhysicalPath;
                var OutputDirectory = Project.Reference.FilePath.Parent;
                var ProjectTargetType = Project.Definition.TargetType;
                if (Toolchain == ToolchainType.Windows_VisualC)
                {
                    var VcxprojTemplateText = Resource.GetResourceText(@"Templates\vc15\Default.vcxproj");
                    var VcxprojFilterTemplateText = Resource.GetResourceText(@"Templates\vc15\Default.vcxproj.filters");
                    var g = new VcxprojGenerator(p, ProjectReference.Id, ProjectReferences, InputDirectory, OutputDirectory, VcxprojTemplateText, VcxprojFilterTemplateText, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem);
                    g.Generate(ForceRegenerate);
                }
                else if (Toolchain == ToolchainType.Mac_XCode)
                {
                    var PbxprojTemplateText = Resource.GetResourceText(@"Templates\xcode9\Default.xcodeproj\project.pbxproj");
                    var g = new PbxprojGenerator(p, ProjectReferences, InputDirectory, OutputDirectory, PbxprojTemplateText, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem, XCodeDevelopmentTeam);
                    g.Generate(ForceRegenerate);
                }
                else if (Toolchain == ToolchainType.CMake)
                {
                    var g = new CMakeProjectGenerator(p, ProjectReferences, InputDirectory, OutputDirectory, Toolchain, Compiler, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem, TargetArchitecture, ConfigurationType, false);
                    g.Generate(ForceRegenerate);
                }
                else if (Toolchain == ToolchainType.Gradle_CMake)
                {
                    if (ProjectTargetType == TargetType.GradleApplication)
                    {
                        var BuildGradleTemplateText = Resource.GetResourceText(@"Templates\gradle_application\build.gradle");
                        var gGradle = new GradleProjectGenerator(SolutionName, p, ProjectReferences, InputDirectory, OutputDirectory, BuildDirectory, BuildGradleTemplateText, Toolchain, Compiler, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem, TargetArchitecture, ConfigurationType);
                        gGradle.Generate(ForceRegenerate);
                        continue;
                    }
                    else if (ProjectTargetType == TargetType.GradleLibrary)
                    {
                        var BuildGradleTemplateText = Resource.GetResourceText(@"Templates\gradle_library\build.gradle");
                        var gGradle = new GradleProjectGenerator(SolutionName, p, ProjectReferences, InputDirectory, OutputDirectory, BuildDirectory, BuildGradleTemplateText, Toolchain, Compiler, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem, TargetArchitecture, ConfigurationType);
                        gGradle.Generate(ForceRegenerate);
                        continue;
                    }
                    else
                    {
                        var g = new CMakeProjectGenerator(p, ProjectReferences, InputDirectory, OutputDirectory, Toolchain, Compiler, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem, TargetArchitecture, ConfigurationType, BuildingOperatingSystem == OperatingSystemType.Windows);
                        g.Generate(ForceRegenerate);
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
            if (Toolchain == ToolchainType.Windows_VisualC)
            {
                var SlnTemplateText = Resource.GetResourceText(@"Templates\vc15\Default.sln");
                var g = new SlnGenerator(SolutionName, GetIdForProject(SolutionName + ".solution"), Projects.Select(p => p.Key).ToList(), BuildDirectory, SlnTemplateText);
                g.Generate(ForceRegenerate);
            }
            else if (Toolchain == ToolchainType.Mac_XCode)
            {
                var PbxprojTemplateText = Resource.GetResourceText(@"Templates\xcode9\Default.xcodeproj\project.pbxproj");
                var g = new PbxprojSolutionGenerator(SolutionName, Projects.Select(p => p.Key).ToList(), BuildDirectory, PbxprojTemplateText);
                g.Generate(ForceRegenerate);
            }
            else if (Toolchain == ToolchainType.CMake)
            {
                var g = new CMakeSolutionGenerator(SolutionName, SortedProjects, BuildDirectory);
                g.Generate(ForceRegenerate);
            }
            else if (Toolchain == ToolchainType.Gradle_CMake)
            {
                CopyDirectory(System.Reflection.Assembly.GetEntryAssembly().Location.AsPath().Parent / "Templates/gradle", BuildDirectory / "gradle");
                var g = new CMakeSolutionGenerator(SolutionName, SortedProjects, BuildDirectory);
                g.Generate(ForceRegenerate);
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
            return new Result { SolutionName = SolutionName, Projects = Projects, SortedProjects = SortedProjects };
        }

        private List<Configuration> GetCommonConfigurations()
        {
            var Configurations = new List<Configuration>
            {
                new Configuration
                {
                    MatchingCompilers = new List<CompilerType> { CompilerType.VisualC },
                    Defines = ParseDefines("_CRT_SECURE_NO_DEPRECATE;_CRT_NONSTDC_NO_DEPRECATE;_SCL_SECURE_NO_WARNINGS;_CRT_SECURE_NO_WARNINGS"),
                    CommonFlags = new List<String> { "/bigobj" }
                },
                new Configuration
                {
                    MatchingTargetOperatingSystems = new List<OperatingSystemType> { OperatingSystemType.Windows },
                    Defines = ParseDefines("WIN32;_WINDOWS")
                },
                new Configuration
                {
                    MatchingTargetOperatingSystems = new List<OperatingSystemType> { OperatingSystemType.Linux },
                    CommonFlags = new List<String> { "-fPIC" }
                },
                new Configuration
                {
                    MatchingConfigurationTypes = new List<ConfigurationType> { Cpp.ConfigurationType.Debug },
                    Defines = ParseDefines("_DEBUG;DEBUG=1")
                },
                new Configuration
                {
                    MatchingCompilers = new List<CompilerType> { CompilerType.gcc },
                    CppFlags = new List<String>{ "-std=c++14" }
                },
                new Configuration
                {
                    MatchingCompilers = new List<CompilerType> { CompilerType.clang },
                    CppFlags = new List<String>{ "-std=c++14", "-stdlib=libc++" }
                }
            };
            foreach (var Architecture in Enum.GetValues(typeof(ArchitectureType)).Cast<ArchitectureType>())
            {
                foreach (var ConfigurationType in Enum.GetValues(typeof(ConfigurationType)).Cast<ConfigurationType>())
                {

                    Configurations.Add(new Configuration
                    {
                        MatchingCompilers = new List<CompilerType> { CompilerType.VisualC },
                        MatchingTargetArchitectures = new List<ArchitectureType> { Architecture },
                        MatchingConfigurationTypes = new List<ConfigurationType> { ConfigurationType },
                        LibDirectories = new List<PathString> { BuildDirectory / $"{Architecture}_{ConfigurationType}" }
                    });
                }
            }
            return Configurations;
        }

        private static List<Cpp.File> GetFilesInDirectory(PathString d, OperatingSystemType TargetOperatingSystem, bool IsTargetOperatingSystemMatched, bool TopOnly = false)
        {
            if (!Directory.Exists(d)) { return new List<Cpp.File> { }; }
            var l = new List<Cpp.File>();
            foreach (var FilePathRelative in Directory.EnumerateFiles(d, "*", TopOnly ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories))
            {
                var FilePath = FilePathRelative.AsPath().FullPath;
                l.Add(GetFileByPath(FilePath, TargetOperatingSystem, IsTargetOperatingSystemMatched));
            }
            return l;
        }
        private static Cpp.File GetFileByPath(PathString FilePath, OperatingSystemType TargetOperatingSystem, bool IsTargetOperatingSystemMatched)
        {
            var Ext = FilePath.Extension.TrimStart('.').ToLowerInvariant();
            var Extensions = FilePath.FileName.Split('.', '_').Skip(1).ToList();
            if (!IsTargetOperatingSystemMatched || !IsOperatingSystemMatchExtensions(Extensions, TargetOperatingSystem))
            {
                return new Cpp.File { Path = FilePath, Type = FileType.Unknown };
            }
            if ((Ext == "h") || (Ext == "hh") || (Ext == "hpp") || (Ext == "hxx"))
            {
                return new Cpp.File { Path = FilePath, Type = FileType.Header };
            }
            else if (Ext == "c")
            {
                return new Cpp.File { Path = FilePath, Type = FileType.CSource };
            }
            else if ((Ext == "cc") || (Ext == "cpp") || (Ext == "cxx"))
            {
                return new Cpp.File { Path = FilePath, Type = FileType.CppSource };
            }
            else if (Ext == "m")
            {
                return new Cpp.File { Path = FilePath, Type = FileType.ObjectiveCSource };
            }
            else if (Ext == "mm")
            {
                return new Cpp.File { Path = FilePath, Type = FileType.ObjectiveCppSource };
            }
            else if (Ext == "storyboard")
            {
                return new Cpp.File { Path = FilePath, Type = FileType.EmbeddedContent };
            }
            else if (Ext == "xib")
            {
                return new Cpp.File { Path = FilePath, Type = FileType.EmbeddedContent };
            }
            else
            {
                return new Cpp.File { Path = FilePath, Type = FileType.Unknown };
            }
        }
        private static List<KeyValuePair<String, String>> ParseDefines(String Defines)
        {
            return Defines.Split(';').Select(d => d.Split('=')).Select(arr => arr.Length >= 2 ? new KeyValuePair<String, String>(arr[0], (arr[1].Length >= 2) && arr[1].StartsWith("\"") && arr[1].EndsWith("\"") ? arr[1].Substring(1, arr[1].Length - 2).Replace("\"\"", "\"") : arr[1]) : new KeyValuePair<String, String>(arr[0], null)).ToList();
        }
        private String GetIdForProject(String ProjectName)
        {
            if (ProjectIds.ContainsKey(ProjectName))
            {
                return ProjectIds[ProjectName];
            }
            if (Toolchain == ToolchainType.Windows_VisualC)
            {
                var g = Guid.ParseExact(Hash.GetHashForPath(ProjectName, 32), "N").ToString().ToUpper();
                ProjectIds.Add(ProjectName, g);
                return g;
            }
            else if (Toolchain == ToolchainType.Mac_XCode)
            {
                var g = Hash.GetHashForPath(ProjectName, 24);
                ProjectIds.Add(ProjectName, g);
                return g;
            }
            else if ((Toolchain == ToolchainType.CMake) || (Toolchain == ToolchainType.Gradle_CMake))
            {
                return "";
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        private static List<String> GetFullProjectDependencies(String ProjectName, Dictionary<String, Dictionary<String, bool>> Dependencies, out List<String> UnresovledDependencies)
        {
            var Full = new List<String>();
            var Unresovled = new List<String>();
            var Added = new HashSet<String>();
            var Queue = new Queue<KeyValuePair<String, bool>>();
            Queue.Enqueue(new KeyValuePair<String, bool>(ProjectName, true));
            while (Queue.Count > 0)
            {
                var m = Queue.Dequeue();
                if (Added.Contains(m.Key)) { continue; }
                if (!m.Value)
                {
                    if (!Dependencies.ContainsKey(m.Key)) { continue; }
                }
                if (!Dependencies.ContainsKey(m.Key))
                {
                    Unresovled.Add(m.Key);
                    continue;
                }
                if (Added.Count != 0)
                {
                    Full.Add(m.Key);
                }
                Added.Add(m.Key);
                foreach (var d in Dependencies[m.Key])
                {
                    Queue.Enqueue(d);
                }
            }
            UnresovledDependencies = Unresovled;
            return Full;
        }
        private String GetProjectFileName(String ProjectName)
        {
            if (Toolchain == ToolchainType.Windows_VisualC)
            {
                return ProjectName + ".vcxproj";
            }
            else if (Toolchain == ToolchainType.Mac_XCode)
            {
                return ProjectName + ".xcodeproj";
            }
            else if ((Toolchain == ToolchainType.CMake) || (Toolchain == ToolchainType.Gradle_CMake))
            {
                return ProjectName;
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private static bool IsOperatingSystemMatchExtensions(IEnumerable<String> Extensions, OperatingSystemType TargetOperatingSystem, bool IsExact = false)
        {
            var Names = new HashSet<String>(Enum.GetNames(typeof(OperatingSystemType)), StringComparer.OrdinalIgnoreCase);
            var TargetName = Enum.GetName(typeof(OperatingSystemType), TargetOperatingSystem);
            foreach (var e in Extensions)
            {
                if (String.Equals(e, TargetName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                if ((TargetOperatingSystem == OperatingSystemType.Windows) && String.Equals(e, "Win", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                if (Names.Contains(e))
                {
                    return false;
                }
            }
            return !IsExact;
        }
        private static void CopyDirectory(String SourceDirectory, String DestinationDirectory)
        {
            foreach (var f in Directory.EnumerateFiles(SourceDirectory, "*", SearchOption.AllDirectories))
            {
                var fNew = DestinationDirectory / f.AsPath().RelativeTo(SourceDirectory);
                var NewDir = fNew.Parent;
                if ((NewDir != "") && !Directory.Exists(NewDir))
                {
                    Directory.CreateDirectory(NewDir);
                }
                System.IO.File.Copy(f, fNew, true);
            }
        }
    }
}
