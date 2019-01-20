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
        private PathString SourceDirectory;
        private PathString BuildDirectory;
        private String XCodeDevelopmentTeam;
        private bool ForceRegenerate;
        private bool EnableNonTargetingOperatingSystemDummy;

        private Dictionary<String, String> ProjectIds = new Dictionary<String, String>();

        public Make(ToolchainType Toolchain, CompilerType Compiler, OperatingSystemType BuildingOperatingSystem, ArchitectureType BuildingOperatingSystemArchitecture, OperatingSystemType TargetOperatingSystem, ArchitectureType? TargetArchitecture, PathString SourceDirectory, PathString BuildDirectory, String XCodeDevelopmentTeam, bool ForceRegenerate, bool EnableNonTargetingOperatingSystemDummy)
        {
            this.Toolchain = Toolchain;
            this.Compiler = Compiler;
            this.BuildingOperatingSystem = BuildingOperatingSystem;
            this.BuildingOperatingSystemArchitecture = BuildingOperatingSystemArchitecture;
            this.TargetOperatingSystem = TargetOperatingSystem;
            this.TargetArchitecture = TargetArchitecture;
            this.SourceDirectory = SourceDirectory.FullPath;
            this.BuildDirectory = BuildDirectory.FullPath;
            this.XCodeDevelopmentTeam = XCodeDevelopmentTeam;
            this.ForceRegenerate = ForceRegenerate;
            this.EnableNonTargetingOperatingSystemDummy = EnableNonTargetingOperatingSystemDummy;
        }

        public class TargetDefinition
        {
            public String ProjectId;
            public String Name;
            public PathString PhysicalPath;
            public PathString VirtualDir;
            public PathString ProjectFilePath;
            public List<PathString> ExposedIncludeDirectories;
            public Dictionary<String, bool> DependentTargetToRequirement;
            public bool IsModule;
            public bool IsTargetOperatingSystemMatched;
            public TargetType Type;
        }
        public Dictionary<String, TargetDefinition> GetAvailableTargets()
        {
            var TargetDependencies = new Dictionary<String, Dictionary<String, bool>>();

            void Add(String Depender, String Dependee, bool IsRequired = true)
            {
                if (TargetDependencies.ContainsKey(Depender))
                {
                    var d = TargetDependencies[Depender];
                    if (d.ContainsKey(Dependee))
                    {
                        throw new ArgumentException();
                    }
                    d.Add(Dependee, IsRequired);
                }
                else
                {
                    TargetDependencies.Add(Depender, new Dictionary<String, bool> { [Dependee] = IsRequired });
                }
            }

            //modules
            Add("math", "core");

            //products
            Add("basic.static", "math");
            Add("standard.dynamic", "math");
            Add("hello.executable", "math");
            Add("hello.executable.ios", "math");
            Add("hello.gradle.android", "math");

            var Targets = new List<TargetDefinition>();
            foreach (var ModulePath in Directory.EnumerateDirectories(SourceDirectory / "modules", "*", SearchOption.TopDirectoryOnly).Select(p => p.AsPath()))
            {
                var ModuleName = ModulePath.FileName;
                var Extensions = ModuleName.Split('.').Skip(1).ToArray();
                var IsTargetOperatingSystemMatched = IsOperatingSystemMatchExtensions(Extensions, TargetOperatingSystem);
                if (IsTargetOperatingSystemMatched || EnableNonTargetingOperatingSystemDummy)
                {
                    var DependentModuleToRequirement = TargetDependencies.ContainsKey(ModuleName) ? TargetDependencies[ModuleName] : new Dictionary<String, bool>();
                    Targets.Add(new TargetDefinition
                    {
                        ProjectId = GetIdForProject(ModuleName),
                        Name = ModuleName,
                        PhysicalPath = ModulePath,
                        VirtualDir = "modules",
                        ProjectFilePath = BuildDirectory / "projects" / GetProjectFileName(ModuleName),
                        ExposedIncludeDirectories = new List<PathString> { (ModulePath / "include").FullPath },
                        DependentTargetToRequirement = DependentModuleToRequirement,
                        IsModule = true,
                        IsTargetOperatingSystemMatched = IsTargetOperatingSystemMatched,
                        Type = TargetType.StaticLibrary
                    });
                }
            }

            foreach (var ProductPath in Directory.EnumerateDirectories(SourceDirectory / "products", "*", SearchOption.TopDirectoryOnly))
            {
                var ProductName = ProductPath.AsPath().FileName;
                var Extensions = ProductName.Split('.').Skip(1).ToArray();
                var ProductTargetType = TargetType.Executable;
                if (Extensions.Contains("gradle", StringComparer.OrdinalIgnoreCase))
                {
                    if (Extensions.Contains("dynamic", StringComparer.OrdinalIgnoreCase))
                    {
                        ProductTargetType = TargetType.GradleLibrary;
                    }
                    else
                    {
                        ProductTargetType = TargetType.GradleApplication;
                    }
                }
                else if (Extensions.Contains("dynamic", StringComparer.OrdinalIgnoreCase))
                {
                    ProductTargetType = TargetType.DynamicLibrary;
                }
                else if (Extensions.Contains("static", StringComparer.OrdinalIgnoreCase))
                {
                    ProductTargetType = TargetType.StaticLibrary;
                }
                var IsTargetOperatingSystemMatched = IsOperatingSystemMatchExtensions(Extensions, TargetOperatingSystem);
                if ((TargetOperatingSystem != OperatingSystemType.Android) && ((ProductTargetType == TargetType.GradleLibrary) || (ProductTargetType == TargetType.GradleApplication)))
                {
                    IsTargetOperatingSystemMatched = false;
                }
                if ((TargetOperatingSystem == OperatingSystemType.iOS) && (ProductTargetType == TargetType.Executable))
                {
                    IsTargetOperatingSystemMatched = IsOperatingSystemMatchExtensions(Extensions, TargetOperatingSystem, true);
                }
                if (IsTargetOperatingSystemMatched || EnableNonTargetingOperatingSystemDummy)
                {
                    var DependentModuleToRequirement = TargetDependencies.ContainsKey(ProductName) ? TargetDependencies[ProductName] : new Dictionary<String, bool>();
                    Targets.Add(new TargetDefinition
                    {
                        ProjectId = GetIdForProject(ProductName),
                        Name = ProductName,
                        PhysicalPath = ProductPath,
                        VirtualDir = "products",
                        ProjectFilePath = BuildDirectory / "projects" / GetProjectFileName(ProductName),
                        ExposedIncludeDirectories = new List<PathString> { },
                        DependentTargetToRequirement = DependentModuleToRequirement,
                        IsModule = false,
                        IsTargetOperatingSystemMatched = IsTargetOperatingSystemMatched,
                        Type = ProductTargetType
                    });
                }
            }

            var DuplicateTargetNames = Targets.GroupBy(Target => Target.Name, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (DuplicateTargetNames.Count > 0)
            {
                throw new InvalidOperationException("DuplicateTargetNames: " + String.Join(" ", DuplicateTargetNames));
            }
            return Targets.ToDictionary(Target => Target.Name);
        }

        public Dictionary<String, List<String>> CheckUnresolvedDependencies(Dictionary<String, TargetDefinition> SelectedTargets)
        {
            var UnresolvedDependencies = new Dictionary<String, List<String>>();
            var TargetDependencies = SelectedTargets.Values.ToDictionary(Target => Target.Name, Target => Target.DependentTargetToRequirement);
            foreach (var Target in SelectedTargets.Values)
            {
                var FullDependentTargetToRequirement = GetFullTargetDependencies(Target.Name, TargetDependencies, out var Unresovled);
                if (Unresovled.Count > 0)
                {
                    UnresolvedDependencies.Add(Target.Name, Unresovled);
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
        public Result Execute(Dictionary<String, TargetDefinition> SelectedTargets)
        {
            var TargetDependencies = SelectedTargets.Values.ToDictionary(Target => Target.Name, Target => Target.DependentTargetToRequirement);
            var TargetProjectReferences = SelectedTargets.Values.Select(Target => new ProjectReference { Id = Target.ProjectId, Name = Target.Name, VirtualDir = Target.VirtualDir, FilePath = Target.ProjectFilePath }).ToDictionary(p => p.Name);
            var Projects = new List<KeyValuePair<ProjectReference, List<ProjectReference>>>();
            foreach (var Target in SelectedTargets.Values)
            {
                var FullDependentTargetToRequirement = GetFullTargetDependencies(Target.Name, TargetDependencies, out var Unresovled);
                if (Unresovled.Count > 0)
                {
                    throw new InvalidOperationException($"UnresolvedDependencies: {Target.Name} -> {String.Join(" ", Unresovled)}");
                }
                var ProjectRefence = TargetProjectReferences[Target.Name];
                var DependentProjectReferences = FullDependentTargetToRequirement.Select(d => TargetProjectReferences[d]).ToList();
                var DependentProjectIncludeDirectories = FullDependentTargetToRequirement.SelectMany(d => SelectedTargets[d].ExposedIncludeDirectories).ToList();
                if (Target.IsModule)
                {
                    var p = CreateModuleProject(ProjectRefence, DependentProjectIncludeDirectories, Target.PhysicalPath, Target.IsTargetOperatingSystemMatched);
                    GenerateProject(p, ProjectRefence, DependentProjectReferences, Target.PhysicalPath, Target.ProjectFilePath.Parent, Target.Type);
                    Projects.Add(new KeyValuePair<ProjectReference, List<ProjectReference>>(ProjectRefence, DependentProjectReferences));
                    if (!((TargetOperatingSystem == OperatingSystemType.Android) || (TargetOperatingSystem == OperatingSystemType.iOS)))
                    {
                        foreach (var TestFile in GetFilesInDirectory(Target.PhysicalPath / "test", TargetOperatingSystem, Target.IsTargetOperatingSystemMatched))
                        {
                            if (TestFile.Type != FileType.CppSource) { continue; }
                            var TestName = Target.Name + "_" + Regex.Replace(TestFile.Path.FullPath.RelativeTo(Target.PhysicalPath), @"[\\/]", "_").AsPath().FileNameWithoutExtension;
                            var TestProjectReference = new ProjectReference
                            {
                                Id = GetIdForProject(TestName),
                                Name = TestName,
                                VirtualDir = "modules",
                                FilePath = BuildDirectory / "projects" / GetProjectFileName(TestName)
                            };
                            var TestDependentProjectReferences = DependentProjectReferences.Concat(new List<ProjectReference> { ProjectRefence }).ToList();
                            var TestDependentProjectIncludeDirectories = DependentProjectIncludeDirectories.Concat(Target.ExposedIncludeDirectories).ToList();
                            var tp = CreateTestProject(TestProjectReference, DependentProjectIncludeDirectories, Target.PhysicalPath, TestFile, Target.IsTargetOperatingSystemMatched);
                            GenerateProject(tp, TestProjectReference, TestDependentProjectReferences, Target.PhysicalPath, Target.ProjectFilePath.Parent, Target.Type);
                            Projects.Add(new KeyValuePair<ProjectReference, List<ProjectReference>>(TestProjectReference, TestDependentProjectReferences));
                        }
                    }
                }
                else
                {
                    var p = CreateProductProject(ProjectRefence, DependentProjectIncludeDirectories, Target.PhysicalPath, Target.Type, Target.IsTargetOperatingSystemMatched);
                    GenerateProject(p, ProjectRefence, DependentProjectReferences, Target.PhysicalPath, Target.ProjectFilePath.Parent, Target.Type);
                    Projects.Add(new KeyValuePair<ProjectReference, List<ProjectReference>>(ProjectRefence, DependentProjectReferences));
                }
            }
            var GradleProjectNames = SelectedTargets.Values.Where(Target => (Target.Type == TargetType.GradleLibrary) || (Target.Type == TargetType.GradleApplication)).Select(Target => Target.Name).ToList();
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
                TextFile.WriteToFile(BuildDirectory / "gradle/settings.gradle", "include " + String.Join(", ", GradleProjectNames.Select(n => "':" + n + "'")), new UTF8Encoding(false), !ForceRegenerate);
            }
            else
            {
                throw new NotSupportedException();
            }
            return new Result { SolutionName = SolutionName, Projects = Projects, SortedProjects = SortedProjects };
        }

        private Project CreateModuleProject(ProjectReference ProjectRefence, List<PathString> DependentProjectIncludeDirectories, PathString InputDirectory, bool IsTargetOperatingSystemMatched)
        {
            var InlcudeDirectories = new List<PathString> { };
            var SourceDirectories = new List<PathString> { InputDirectory / "include", InputDirectory / "src" };
            var Libs = new List<PathString> { };
            var Files = SourceDirectories.SelectMany(d => GetFilesInDirectory(d, TargetOperatingSystem, IsTargetOperatingSystemMatched)).ToList();

            var RelativeIncludeDirectories = new List<String>
            {
                "include",
                "src"
            };
            foreach (var RelativeIncludeDirectory in RelativeIncludeDirectories)
            {
                InlcudeDirectories.Add((InputDirectory / RelativeIncludeDirectory).FullPath);
            }
            InlcudeDirectories.AddRange(DependentProjectIncludeDirectories);

            return new Project
            {
                Name = ProjectRefence.Name,
                Configurations = (new List<Configuration>
                {
                    new Configuration
                    {
                        TargetType = TargetType.StaticLibrary,
                        IncludeDirectories = InlcudeDirectories,
                        Libs = Libs,
                        Files = Files
                    }
                }).Concat(GetCommonConfigurations()).ToList()
            };
        }
        private Project CreateTestProject(ProjectReference ProjectRefence, List<PathString> DependentProjectIncludeDirectories, PathString InputDirectory, Cpp.File TestFile, bool IsTargetOperatingSystemMatched)
        {
            var InlcudeDirectories = new List<PathString> { };
            var Libs = new List<PathString> { };
            var Files = new List<Cpp.File> { TestFile };

            var RelativeIncludeDirectories = new List<String>
            {
                "include",
                "src"
            };
            foreach (var RelativeIncludeDirectory in RelativeIncludeDirectories)
            {
                InlcudeDirectories.Add((InputDirectory / RelativeIncludeDirectory).FullPath);
            }
            InlcudeDirectories.AddRange(DependentProjectIncludeDirectories);

            return new Project
            {
                Name = ProjectRefence.Name,
                Configurations = (new List<Configuration>
                {
                    new Configuration
                    {
                        TargetType = TargetType.Executable,
                        IncludeDirectories = InlcudeDirectories,
                        Libs = Libs,
                        Files = Files
                    },
                }).Concat(GetCommonConfigurations()).ToList()
            };
        }
        private Project CreateProductProject(ProjectReference ProjectRefence, List<PathString> DependentProjectIncludeDirectories, PathString InputDirectory, TargetType ProductTargetType, bool IsTargetOperatingSystemMatched)
        {
            var InlcudeDirectories = new List<PathString> { InputDirectory };
            var Defines = new List<KeyValuePair<String, String>> { };
            var SourceDirectories = new List<PathString> { InputDirectory };
            var Libs = new List<PathString> { };
            var Files = SourceDirectories.SelectMany(d => GetFilesInDirectory(d, TargetOperatingSystem, IsTargetOperatingSystemMatched)).ToList();
            var ProjectReferences = new List<ProjectReference> { };

            var RelativeIncludeDirectories = new List<String>
            {
                "include",
                "src"
            };
            foreach (var RelativeIncludeDirectory in RelativeIncludeDirectories)
            {
                InlcudeDirectories.Add((InputDirectory / RelativeIncludeDirectory).FullPath);
            }
            foreach (var RelativeIncludeDirectory in RelativeIncludeDirectories)
            {
                InlcudeDirectories.Add((InputDirectory / RelativeIncludeDirectory).FullPath);
            }
            InlcudeDirectories.AddRange(DependentProjectIncludeDirectories);

            if (ProductTargetType == TargetType.Executable)
            {
            }
            else if (ProductTargetType == TargetType.StaticLibrary)
            {
                Defines.Add(new KeyValuePair<String, String>(SolutionName.ToUpperInvariant() + "_BUILD", null));
                Defines.Add(new KeyValuePair<String, String>(SolutionName.ToUpperInvariant() + "_STATIC", null));
            }
            else if (ProductTargetType == TargetType.DynamicLibrary)
            {
                Defines.Add(new KeyValuePair<String, String>(SolutionName.ToUpperInvariant() + "_BUILD", null));
                Defines.Add(new KeyValuePair<String, String>(SolutionName.ToUpperInvariant() + "_DYNAMIC", null));
            }
            else if (ProductTargetType == TargetType.GradleApplication)
            {
            }
            else if (ProductTargetType == TargetType.GradleLibrary)
            {
                Defines.Add(new KeyValuePair<String, String>(SolutionName.ToUpperInvariant() + "_BUILD", null));
                Defines.Add(new KeyValuePair<String, String>(SolutionName.ToUpperInvariant() + "_DYNAMIC", null));
            }
            else
            {
                throw new InvalidOperationException();
            }
            var TargetName = ProjectRefence.Name.Split('.').Take(1).Single();
            return new Project
            {
                Name = ProjectRefence.Name,
                TargetName = TargetName,
                Configurations = (new List<Configuration>
                {
                    new Configuration
                    {
                        TargetType = IsTargetOperatingSystemMatched ? ProductTargetType : TargetType.StaticLibrary,
                        IncludeDirectories = InlcudeDirectories,
                        Defines = Defines,
                        Libs = Libs,
                        Files = Files
                    },
                    new Configuration
                    {
                        TargetOperatingSystem = OperatingSystemType.iOS,
                        BundleIdentifier = SolutionName + "." + TargetName
                    }
                }).Concat(GetCommonConfigurations()).ToList()
            };
        }

        private void GenerateProject(Project p, ProjectReference ProjectRefence, List<ProjectReference> ProjectReferences, PathString InputDirectory, PathString OutputDirectory, TargetType ProjectTargetType)
        {
            if (Toolchain == ToolchainType.Windows_VisualC)
            {
                var VcxprojTemplateText = Resource.GetResourceText(@"Templates\vc15\Default.vcxproj");
                var VcxprojFilterTemplateText = Resource.GetResourceText(@"Templates\vc15\Default.vcxproj.filters");
                var g = new VcxprojGenerator(p, ProjectRefence.Id, ProjectReferences, InputDirectory, OutputDirectory, VcxprojTemplateText, VcxprojFilterTemplateText, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem);
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
                var g = new CMakeProjectGenerator(p, ProjectReferences, InputDirectory, OutputDirectory, Toolchain, Compiler, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem, TargetArchitecture);
                g.Generate(ForceRegenerate);
            }
            else if (Toolchain == ToolchainType.Gradle_CMake)
            {
                var g = new CMakeProjectGenerator(p, ProjectReferences, InputDirectory, OutputDirectory, Toolchain, Compiler, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem, TargetArchitecture);
                g.Generate(ForceRegenerate);
                if (ProjectTargetType == TargetType.GradleApplication)
                {
                    var BuildGradleTemplateText = Resource.GetResourceText(@"Templates\gradle_application\build.gradle");
                    var gGradle = new GradleProjectGenerator(SolutionName, p, ProjectReferences, InputDirectory, OutputDirectory, BuildDirectory, BuildGradleTemplateText, Toolchain, Compiler, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem, TargetArchitecture);
                    gGradle.Generate(ForceRegenerate);
                }
                else if (ProjectTargetType == TargetType.GradleLibrary)
                {
                    var BuildGradleTemplateText = Resource.GetResourceText(@"Templates\gradle_library\build.gradle");
                    var gGradle = new GradleProjectGenerator(SolutionName, p, ProjectReferences, InputDirectory, OutputDirectory, BuildDirectory, BuildGradleTemplateText, Toolchain, Compiler, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem, TargetArchitecture);
                    gGradle.Generate(ForceRegenerate);
                }
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private List<Configuration> GetCommonConfigurations()
        {
            return new List<Configuration>
            {
                new Configuration
                {
                    Compiler = CompilerType.VisualC,
                    LibDirectories = new List<PathString> { BuildDirectory / @"$(PlatformTarget)_$(Configuration)" },
                    Defines = ParseDefines("_CRT_SECURE_NO_DEPRECATE;_CRT_NONSTDC_NO_DEPRECATE;_SCL_SECURE_NO_WARNINGS;_CRT_SECURE_NO_WARNINGS"),
                    CFlags = new List<String> { "/bigobj" }
                },
                new Configuration
                {
                    TargetOperatingSystem = OperatingSystemType.Windows,
                    Defines = ParseDefines("WIN32;_WINDOWS")
                },
                new Configuration
                {
                    TargetOperatingSystem = OperatingSystemType.Linux,
                    CFlags = new List<String> { "-fPIC" }
                },
                new Configuration
                {
                    ConfigurationType = ConfigurationType.Debug,
                    Defines = ParseDefines("_DEBUG;DEBUG=1")
                },
                new Configuration
                {
                    Compiler = CompilerType.gcc,
                    CppFlags = new List<String>{ "-std=c++14" }
                },
                new Configuration
                {
                    Compiler = CompilerType.clang,
                    CppFlags = new List<String>{ "-std=c++14", "-stdlib=libc++" }
                }
            };
        }

        private static List<Cpp.File> GetFilesInDirectory(PathString d, OperatingSystemType TargetOperatingSystem, bool IsTargetOperatingSystemMatched)
        {
            if (!Directory.Exists(d)) { return new List<Cpp.File> { }; }
            var l = new List<Cpp.File>();
            foreach (var FilePathRelative in Directory.EnumerateFiles(d, "*", SearchOption.AllDirectories))
            {
                var FilePath = FilePathRelative.AsPath().FullPath;
                var Ext = FilePath.Extension.TrimStart('.').ToLowerInvariant();
                var Extensions = FilePath.FileName.Split('.', '_').Skip(1).ToList();
                if (!IsTargetOperatingSystemMatched || !IsOperatingSystemMatchExtensions(Extensions, TargetOperatingSystem))
                {
                    l.Add(new Cpp.File { Path = FilePath, Type = FileType.Unknown });
                    continue;
                }
                if ((Ext == "h") || (Ext == "hh") || (Ext == "hpp") || (Ext == "hxx"))
                {
                    l.Add(new Cpp.File { Path = FilePath, Type = FileType.Header });
                }
                else if (Ext == "c")
                {
                    l.Add(new Cpp.File { Path = FilePath, Type = FileType.CSource });
                }
                else if ((Ext == "cc") || (Ext == "cpp") || (Ext == "cxx"))
                {
                    l.Add(new Cpp.File { Path = FilePath, Type = FileType.CppSource });
                }
                else if (Ext == "m")
                {
                    l.Add(new Cpp.File { Path = FilePath, Type = FileType.ObjectiveCSource });
                }
                else if (Ext == "mm")
                {
                    l.Add(new Cpp.File { Path = FilePath, Type = FileType.ObjectiveCppSource });
                }
                else
                {
                    l.Add(new Cpp.File { Path = FilePath, Type = FileType.Unknown });
                }
            }
            return l;
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
        private static List<String> GetFullTargetDependencies(String TargetName, Dictionary<String, Dictionary<String, bool>> TargetDependencies, out List<String> UnresovledDependencies)
        {
            var Full = new List<String>();
            var Unresovled = new List<String>();
            var Added = new HashSet<String>();
            var Queue = new Queue<KeyValuePair<String, bool>>();
            Queue.Enqueue(new KeyValuePair<String, bool>(TargetName, true));
            while (Queue.Count > 0)
            {
                var m = Queue.Dequeue();
                if (Added.Contains(m.Key)) { continue; }
                if (!m.Value)
                {
                    if (!TargetDependencies.ContainsKey(m.Key)) { continue; }
                }
                if (!TargetDependencies.ContainsKey(m.Key))
                {
                    Unresovled.Add(m.Key);
                    continue;
                }
                if (Added.Count != 0)
                {
                    Full.Add(m.Key);
                }
                Added.Add(m.Key);
                foreach (var d in TargetDependencies[m.Key])
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
