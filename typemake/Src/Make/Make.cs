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

        private Dictionary<String, List<String>> ModuleDependencies =
            new Dictionary<String, List<String>>
            {
                //Modules
                { "math", new List<String> { "core" } },

                //products
                { "basic.static", new List<String> { "math" } },
                { "standard.dynamic", new List<String> { "math" } },
                { "hello.executable", new List<String> { "math" } },
                { "hello.executable.ios", new List<String> { "math" } },
                { "hello.gradle.android", new List<String> { "math" } },
            };

        private ToolchainType Toolchain;
        private CompilerType Compiler;
        private OperatingSystemType BuildingOperatingSystem;
        private ArchitectureType BuildingOperatingSystemArchitecture;
        private OperatingSystemType TargetOperatingSystem;
        private ArchitectureType? TargetArchitecture;
        private String SourceDirectory;
        private String BuildDirectory;
        private bool ForceRegenerate;
        private bool EnableNonTargetingOperatingSystemDummy;

        private Dictionary<String, String> ProjectIds = new Dictionary<String, String>();

        public Make(ToolchainType Toolchain, CompilerType Compiler, OperatingSystemType BuildingOperatingSystem, ArchitectureType BuildingOperatingSystemArchitecture, OperatingSystemType TargetOperatingSystem, ArchitectureType? TargetArchitecture, String SourceDirectory, String BuildDirectory, bool ForceRegenerate, bool EnableNonTargetingOperatingSystemDummy)
        {
            this.Toolchain = Toolchain;
            this.Compiler = Compiler;
            this.BuildingOperatingSystem = BuildingOperatingSystem;
            this.BuildingOperatingSystemArchitecture = BuildingOperatingSystemArchitecture;
            this.TargetOperatingSystem = TargetOperatingSystem;
            this.TargetArchitecture = TargetArchitecture;
            this.SourceDirectory = Path.GetFullPath(SourceDirectory);
            this.BuildDirectory = Path.GetFullPath(BuildDirectory);
            this.ForceRegenerate = ForceRegenerate;
            this.EnableNonTargetingOperatingSystemDummy = EnableNonTargetingOperatingSystemDummy;
        }

        public class Result
        {
            public String SolutionName;
            public List<KeyValuePair<ProjectReference, List<ProjectReference>>> Projects;
            public List<ProjectReference> SortedProjects;
        }
        public Result Execute()
        {
            var Projects = new List<KeyValuePair<ProjectReference, List<ProjectReference>>>();
            foreach (var ModulePath in Directory.EnumerateDirectories(Path.Combine(SourceDirectory, "modules"), "*", SearchOption.TopDirectoryOnly))
            {
                var ModuleName = Path.GetFileName(ModulePath);
                var Extensions = ModuleName.Split('.').Skip(1).ToArray();
                var IsTargetOperatingSystemMatched = IsOperatingSystemMatchExtensions(Extensions, TargetOperatingSystem);
                if (IsTargetOperatingSystemMatched || EnableNonTargetingOperatingSystemDummy)
                {
                    Projects.Add(GenerateModuleProject(ModuleName, ModulePath, IsTargetOperatingSystemMatched));
                    if (!((TargetOperatingSystem == OperatingSystemType.Android) || (TargetOperatingSystem == OperatingSystemType.iOS)))
                    {
                        foreach (var TestFile in GetFilesInDirectory(Path.Combine(ModulePath, "test"), TargetOperatingSystem, IsTargetOperatingSystemMatched))
                        {
                            if (TestFile.Type != FileType.CppSource) { continue; }
                            var TestName = ModuleName + "_" + Path.GetFileNameWithoutExtension(Regex.Replace(FileNameHandling.GetRelativePath(Path.GetFullPath(TestFile.Path), ModulePath), @"[\\/]", "_"));
                            Projects.Add(GenerateTestProject(ModuleName, ModulePath, TestName, TestFile, IsTargetOperatingSystemMatched));
                        }
                    }
                }
            }
            var GradleProjectNames = new List<String>();
            foreach (var ProductPath in Directory.EnumerateDirectories(Path.Combine(SourceDirectory, "products"), "*", SearchOption.TopDirectoryOnly))
            {
                var ProductName = Path.GetFileName(ProductPath);
                var Extensions = ProductName.Split('.').Skip(1).ToArray();
                var ProductTargetType = TargetType.Executable;
                if (Extensions.Contains("gradle", StringComparer.OrdinalIgnoreCase))
                {
                    if (Extensions.Contains("dynamic", StringComparer.OrdinalIgnoreCase))
                    {
                        ProductTargetType = TargetType.GradleLibrary;
                        GradleProjectNames.Add(ProductName);
                    }
                    else
                    {
                        ProductTargetType = TargetType.GradleApplication;
                        GradleProjectNames.Add(ProductName);
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
                    Projects.Add(GenerateProductProject(ProductName, ProductPath, ProductTargetType, IsTargetOperatingSystemMatched));
                }
            }
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
                CopyDirectory(Path.Combine(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "Templates"), "gradle"), Path.Combine(BuildDirectory, "gradle"));
                var g = new CMakeSolutionGenerator(SolutionName, SortedProjects, BuildDirectory);
                g.Generate(ForceRegenerate);
                TextFile.WriteToFile(Path.Combine(Path.Combine(BuildDirectory, "gradle"), "settings.gradle"), "include " + String.Join(", ", GradleProjectNames.Select(n => "':" + n + "'")), new UTF8Encoding(false), !ForceRegenerate);
            }
            else
            {
                throw new NotSupportedException();
            }
            return new Result { SolutionName = SolutionName, Projects = Projects, SortedProjects = SortedProjects };
        }

        private KeyValuePair<ProjectReference, List<ProjectReference>> GenerateModuleProject(String ModuleName, String ModulePath, bool IsTargetOperatingSystemMatched)
        {
            var InlcudeDirectories = new List<String> { };
            var SourceDirectories = new List<String> { Path.Combine(ModulePath, "include"), Path.Combine(ModulePath, "src") };
            var Libs = new List<String> { };
            var Files = SourceDirectories.SelectMany(d => GetFilesInDirectory(d, TargetOperatingSystem, IsTargetOperatingSystemMatched)).ToList();
            var ProjectReferences = new List<ProjectReference> { };

            var RelativeIncludeDirectories = new List<String>
            {
                "include",
                "src"
            };
            foreach (var RelativeIncludeDirectory in RelativeIncludeDirectories)
            {
                InlcudeDirectories.Add(Path.GetFullPath(Path.Combine(ModulePath, RelativeIncludeDirectory)));
            }
            foreach (var ReferenceModule in GetAllModuleDependencies(ModuleName, false))
            {
                InlcudeDirectories.Add(Path.GetFullPath(Path.Combine(ModulePath, Path.Combine("..", Path.Combine(ReferenceModule, "include")))));
                ProjectReferences.Add(new ProjectReference
                {
                    Id = GetIdForProject(ReferenceModule),
                    Name = ReferenceModule,
                    VirtualDir = "modules/" + ReferenceModule,
                    FilePath = Path.Combine(BuildDirectory, Path.Combine("projects", GetProjectFileName(ReferenceModule)))
                });
            }

            var p = new Project
            {
                Name = ModuleName,
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
            if (Toolchain == ToolchainType.Windows_VisualC)
            {
                var VcxprojTemplateText = Resource.GetResourceText(@"Templates\vc15\Default.vcxproj");
                var VcxprojFilterTemplateText = Resource.GetResourceText(@"Templates\vc15\Default.vcxproj.filters");
                var g = new VcxprojGenerator(p, GetIdForProject(ModuleName), ProjectReferences, ModulePath, Path.Combine(BuildDirectory, "projects"), VcxprojTemplateText, VcxprojFilterTemplateText, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem);
                g.Generate(ForceRegenerate);
            }
            else if (Toolchain == ToolchainType.Mac_XCode)
            {
                var PbxprojTemplateText = Resource.GetResourceText(@"Templates\xcode9\Default.xcodeproj\project.pbxproj");
                var g = new PbxprojGenerator(p, ProjectReferences, ModulePath, Path.Combine(BuildDirectory, "projects"), PbxprojTemplateText, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem);
                g.Generate(ForceRegenerate);
            }
            else if (Toolchain == ToolchainType.CMake)
            {
                var g = new CMakeProjectGenerator(p, ProjectReferences, ModulePath, Path.Combine(BuildDirectory, "projects"), Toolchain, Compiler, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem, TargetArchitecture);
                g.Generate(ForceRegenerate);
            }
            else if (Toolchain == ToolchainType.Gradle_CMake)
            {
                var g = new CMakeProjectGenerator(p, ProjectReferences, ModulePath, Path.Combine(BuildDirectory, "projects"), Toolchain, Compiler, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem, TargetArchitecture);
                g.Generate(ForceRegenerate);
            }
            else
            {
                throw new NotSupportedException();
            }
            return new KeyValuePair<ProjectReference, List<ProjectReference>>(new ProjectReference
            {
                Id = GetIdForProject(ModuleName),
                Name = ModuleName,
                VirtualDir = "modules/" + ModuleName,
                FilePath = Path.Combine(BuildDirectory, Path.Combine("projects", GetProjectFileName(ModuleName)))
            }, ProjectReferences);
        }
        private KeyValuePair<ProjectReference, List<ProjectReference>> GenerateTestProject(String ModuleName, String ModulePath, String TestName, Cpp.File TestFile, bool IsTargetOperatingSystemMatched)
        {
            var InlcudeDirectories = new List<String> { };
            var SourceDirectories = new List<String> { Path.Combine(ModulePath, "include"), Path.Combine(ModulePath, "src") };
            var Libs = new List<String> { };
            var Files = new List<Cpp.File> { TestFile };
            var ProjectReferences = new List<ProjectReference> { };

            var RelativeIncludeDirectories = new List<String>
            {
                "include",
                "src"
            };
            foreach (var RelativeIncludeDirectory in RelativeIncludeDirectories)
            {
                InlcudeDirectories.Add(Path.GetFullPath(Path.Combine(ModulePath, RelativeIncludeDirectory)));
            }
            foreach (var ReferenceModule in GetAllModuleDependencies(ModuleName, true))
            {
                InlcudeDirectories.Add(Path.GetFullPath(Path.Combine(ModulePath, Path.Combine("..", Path.Combine(ReferenceModule, "include")))));
                ProjectReferences.Add(new ProjectReference
                {
                    Id = GetIdForProject(ReferenceModule),
                    Name = ReferenceModule,
                    VirtualDir = "modules/" + ReferenceModule,
                    FilePath = Path.Combine(BuildDirectory, Path.Combine("projects", GetProjectFileName(ReferenceModule)))
                });
            }

            var p = new Project
            {
                Name = TestName,
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
            if (Toolchain == ToolchainType.Windows_VisualC)
            {
                var VcxprojTemplateText = Resource.GetResourceText(@"Templates\vc15\Default.vcxproj");
                var VcxprojFilterTemplateText = Resource.GetResourceText(@"Templates\vc15\Default.vcxproj.filters");
                var g = new VcxprojGenerator(p, GetIdForProject(TestName), ProjectReferences, Path.GetDirectoryName(TestFile.Path), Path.Combine(BuildDirectory, "projects"), VcxprojTemplateText, VcxprojFilterTemplateText, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem);
                g.Generate(ForceRegenerate);
            }
            else if (Toolchain == ToolchainType.Mac_XCode)
            {
                var PbxprojTemplateText = Resource.GetResourceText(@"Templates\xcode9\Default.xcodeproj\project.pbxproj");
                var g = new PbxprojGenerator(p, ProjectReferences, Path.GetDirectoryName(TestFile.Path), Path.Combine(BuildDirectory, "projects"), PbxprojTemplateText, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem);
                g.Generate(ForceRegenerate);
            }
            else if (Toolchain == ToolchainType.CMake)
            {
                var g = new CMakeProjectGenerator(p, ProjectReferences, Path.GetDirectoryName(TestFile.Path), Path.Combine(BuildDirectory, "projects"), Toolchain, Compiler, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem, TargetArchitecture);
                g.Generate(ForceRegenerate);
            }
            else if (Toolchain == ToolchainType.Gradle_CMake)
            {
                var g = new CMakeProjectGenerator(p, ProjectReferences, Path.GetDirectoryName(TestFile.Path), Path.Combine(BuildDirectory, "projects"), Toolchain, Compiler, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem, TargetArchitecture);
                g.Generate(ForceRegenerate);
            }
            else
            {
                throw new NotSupportedException();
            }
            return new KeyValuePair<ProjectReference, List<ProjectReference>>(new ProjectReference
            {
                Id = GetIdForProject(TestName),
                Name = TestName,
                VirtualDir = "modules/" + ModuleName,
                FilePath = Path.Combine(BuildDirectory, Path.Combine("projects", GetProjectFileName(TestName)))
            }, ProjectReferences);
        }
        private KeyValuePair<ProjectReference, List<ProjectReference>> GenerateProductProject(String ProductName, String ProductPath, TargetType ProductTargetType, bool IsTargetOperatingSystemMatched)
        {
            var InlcudeDirectories = new List<String> { ProductPath };
            var Defines = new List<KeyValuePair<String, String>> { };
            var SourceDirectories = new List<String> { ProductPath };
            var Libs = new List<String> { };
            var Files = SourceDirectories.SelectMany(d => GetFilesInDirectory(d, TargetOperatingSystem, IsTargetOperatingSystemMatched)).ToList();
            var ProjectReferences = new List<ProjectReference> { };

            var RelativeIncludeDirectories = new List<String>
            {
                "include",
                "src"
            };
            foreach (var RelativeIncludeDirectory in RelativeIncludeDirectories)
            {
                InlcudeDirectories.Add(Path.GetFullPath(Path.Combine(ProductPath, RelativeIncludeDirectory)));
            }
            foreach (var ReferenceModule in GetAllModuleDependencies(ProductName, false))
            {
                InlcudeDirectories.Add(Path.GetFullPath(Path.Combine(ProductPath, Path.Combine("..", Path.Combine("..", Path.Combine("modules", Path.Combine(ReferenceModule, "include")))))));
                ProjectReferences.Add(new ProjectReference
                {
                    Id = GetIdForProject(ReferenceModule),
                    Name = ReferenceModule,
                    VirtualDir = "modules/" + ReferenceModule,
                    FilePath = Path.Combine(BuildDirectory, Path.Combine("projects", GetProjectFileName(ReferenceModule)))
                });
            }

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
            var TargetName = ProductName.Split('.').Take(1).Single();
            var p = new Project
            {
                Name = ProductName,
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
            if (Toolchain == ToolchainType.Windows_VisualC)
            {
                var VcxprojTemplateText = Resource.GetResourceText(@"Templates\vc15\Default.vcxproj");
                var VcxprojFilterTemplateText = Resource.GetResourceText(@"Templates\vc15\Default.vcxproj.filters");
                var g = new VcxprojGenerator(p, GetIdForProject(ProductName), ProjectReferences, ProductPath, Path.Combine(BuildDirectory, "projects"), VcxprojTemplateText, VcxprojFilterTemplateText, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem);
                g.Generate(ForceRegenerate);
            }
            else if (Toolchain == ToolchainType.Mac_XCode)
            {
                var PbxprojTemplateText = Resource.GetResourceText(@"Templates\xcode9\Default.xcodeproj\project.pbxproj");
                var g = new PbxprojGenerator(p, ProjectReferences, ProductPath, Path.Combine(BuildDirectory, "projects"), PbxprojTemplateText, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem);
                g.Generate(ForceRegenerate);
            }
            else if (Toolchain == ToolchainType.CMake)
            {
                var g = new CMakeProjectGenerator(p, ProjectReferences, ProductPath, Path.Combine(BuildDirectory, "projects"), Toolchain, Compiler, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem, TargetArchitecture);
                g.Generate(ForceRegenerate);
            }
            else if (Toolchain == ToolchainType.Gradle_CMake)
            {
                var g = new CMakeProjectGenerator(p, ProjectReferences, ProductPath, Path.Combine(BuildDirectory, "projects"), Toolchain, Compiler, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem, TargetArchitecture);
                g.Generate(ForceRegenerate);
                if (ProductTargetType == TargetType.GradleApplication)
                {
                    var BuildGradleTemplateText = Resource.GetResourceText(@"Templates\gradle_application\build.gradle");
                    var gGradle = new GradleProjectGenerator(SolutionName, p, ProjectReferences, ProductPath, Path.Combine(BuildDirectory, "gradle"), BuildDirectory, BuildGradleTemplateText, Toolchain, Compiler, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem, TargetArchitecture);
                    gGradle.Generate(ForceRegenerate);
                }
                else if (ProductTargetType == TargetType.GradleLibrary)
                {
                    var BuildGradleTemplateText = Resource.GetResourceText(@"Templates\gradle_library\build.gradle");
                    var gGradle = new GradleProjectGenerator(SolutionName, p, ProjectReferences, ProductPath, Path.Combine(BuildDirectory, "gradle"), BuildDirectory, BuildGradleTemplateText, Toolchain, Compiler, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem, TargetArchitecture);
                    gGradle.Generate(ForceRegenerate);
                }
            }
            else
            {
                throw new NotSupportedException();
            }
            return new KeyValuePair<ProjectReference, List<ProjectReference>>(new ProjectReference
            {
                Id = GetIdForProject(ProductName),
                Name = ProductName,
                VirtualDir = "products",
                FilePath = Path.Combine(BuildDirectory, Path.Combine("projects", GetProjectFileName(ProductName)))
            }, ProjectReferences);
        }

        private List<Configuration> GetCommonConfigurations()
        {
            return new List<Configuration>
            {
                new Configuration
                {
                    Compiler = CompilerType.VisualC,
                    LibDirectories = new List<String> { Path.Combine(BuildDirectory, @"$(PlatformTarget)_$(Configuration)") },
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

        private static List<Cpp.File> GetFilesInDirectory(String d, OperatingSystemType TargetOperatingSystem, bool IsTargetOperatingSystemMatched)
        {
            if (!Directory.Exists(d)) { return new List<Cpp.File> { }; }
            var l = new List<Cpp.File>();
            foreach (var FilePathRelative in Directory.EnumerateFiles(d, "*", SearchOption.AllDirectories))
            {
                var FilePath = Path.GetFullPath(FilePathRelative);
                var Ext = Path.GetExtension(FilePath).TrimStart('.').ToLowerInvariant();
                var Extensions = Path.GetFileName(FilePath).Split('.', '_').Skip(1).ToList();
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
        private List<String> GetAllModuleDependencies(String ModuleName, bool ContainSelf)
        {
            var l = new List<String>();
            var Added = new HashSet<String>();
            var Queue = new Queue<String>();
            Queue.Enqueue(ModuleName);
            while (Queue.Count > 0)
            {
                var m = Queue.Dequeue();
                if (Added.Contains(m)) { continue; }
                if (!((Added.Count == 0) && !ContainSelf))
                {
                    l.Add(m);
                }
                Added.Add(m);
                if (ModuleDependencies.ContainsKey(m))
                {
                    foreach (var d in ModuleDependencies[m])
                    {
                        Queue.Enqueue(d);
                    }
                }
            }
            return l;
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
                var NewDir = Path.GetDirectoryName(Path.Combine(DestinationDirectory, FileNameHandling.GetRelativePath(f, SourceDirectory)));
                if ((NewDir != "") && !Directory.Exists(NewDir))
                {
                    Directory.CreateDirectory(NewDir);
                }
                System.IO.File.Copy(f, Path.Combine(NewDir, Path.GetFileName(f)), true);
            }
        }
    }
}
