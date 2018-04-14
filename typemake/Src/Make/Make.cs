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
                { "math", new List<String> { "core" } },
                { "hello", new List<String> { "core", "math" } },
                { "hello.ios", new List<String> { "core", "math" } },
            };

        private ToolchainType Toolchain;
        private CompilerType Compiler;
        private OperatingSystemType TargetOperationSystem;
        private String SourceDirectory;
        private String BuildDirectory;
        private bool EnableRebuild;

        private OperatingSystemType BuildingOperatingSystem;
        private Dictionary<String, String> ProjectIds = new Dictionary<String, String>();

        public Make(ToolchainType Toolchain, CompilerType Compiler, OperatingSystemType TargetOperationSystem, String SourceDirectory, String BuildDirectory, bool EnableRebuild)
        {
            this.Toolchain = Toolchain;
            this.Compiler = Compiler;
            this.TargetOperationSystem = TargetOperationSystem;
            this.SourceDirectory = SourceDirectory;
            this.BuildDirectory = BuildDirectory;
            this.EnableRebuild = EnableRebuild;
            this.BuildingOperatingSystem = OperatingSystemType.Windows;
        }

        public void Execute()
        {
            var Projects = new List<ProjectReference>();
            foreach (var ModulePath in Directory.EnumerateDirectories(Path.Combine(SourceDirectory, "modules"), "*", SearchOption.TopDirectoryOnly))
            {
                var ModuleName = Path.GetFileName(ModulePath);
                Projects.Add(GenerateModuleProject(ModuleName, ModulePath));
                if (!((TargetOperationSystem == OperatingSystemType.Android) || (TargetOperationSystem == OperatingSystemType.iOS)))
                {
                    foreach (var TestFile in GetFilesInDirectory(Path.Combine(ModulePath, "test")))
                    {
                        if (TestFile.Type != FileType.CppSource) { continue; }
                        var TestName = ModuleName + "_" + Path.GetFileNameWithoutExtension(Regex.Replace(FileNameHandling.GetRelativePath(TestFile.Path, ModulePath), @"[\\/]", "_"));
                        Projects.Add(GenerateTestProject(ModuleName, ModulePath, TestName, TestFile));
                    }
                }
            }
            foreach (var SamplePath in Directory.EnumerateDirectories(Path.Combine(SourceDirectory, "samples"), "*", SearchOption.TopDirectoryOnly))
            {
                var SampleName = Path.GetFileName(SamplePath);
                var Extension = Path.GetExtension(SampleName).TrimStart('.');
                if (Extension.Equals("android", StringComparison.OrdinalIgnoreCase))
                {
                    if (TargetOperationSystem == OperatingSystemType.Android)
                    {
                        //TODO
                    }
                }
                else if (Extension.Equals("ios", StringComparison.OrdinalIgnoreCase))
                {
                    if (TargetOperationSystem == OperatingSystemType.iOS)
                    {
                        Projects.Add(GenerateSampleProject(SampleName, SamplePath));
                    }
                }
                else
                {
                    Projects.Add(GenerateSampleProject(SampleName, SamplePath));
                }
            }
            if (Toolchain == ToolchainType.Windows_VisualC)
            {
                var SlnTemplateText = Resource.GetResourceText(@"Templates\vc15\Default.sln");
                var g = new SlnGenerator(SolutionName, GetIdForProject(SolutionName + ".solution"), Projects, BuildDirectory, SlnTemplateText);
                g.Generate(EnableRebuild);
            }
            else if (Toolchain == ToolchainType.Mac_XCode)
            {
                var PbxprojTemplateText = Resource.GetResourceText(@"Templates\xcode9\Default.xcodeproj\project.pbxproj");
                var g = new PbxprojSolutionGenerator(SolutionName, Projects, BuildDirectory, PbxprojTemplateText);
                g.Generate(EnableRebuild);
            }
            else if (Toolchain == ToolchainType.CMake)
            {
                var g = new CMakeSolutionGenerator(SolutionName, Projects, BuildDirectory);
                g.Generate(EnableRebuild);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private ProjectReference GenerateModuleProject(String ModuleName, String ModulePath)
        {
            var InlcudeDirectories = new List<String> { };
            var SourceDirectories = new List<String> { Path.Combine(ModulePath, "include"), Path.Combine(ModulePath, "src") };
            var Libs = new List<String> { };
            var Files = SourceDirectories.SelectMany(d => GetFilesInDirectory(d)).ToList();
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
                var g = new VcxprojGenerator(p, GetIdForProject(ModuleName), ProjectReferences, ModulePath, Path.Combine(BuildDirectory, "projects"), VcxprojTemplateText, VcxprojFilterTemplateText, BuildingOperatingSystem, TargetOperationSystem);
                g.Generate(EnableRebuild);
            }
            else if (Toolchain == ToolchainType.Mac_XCode)
            {
                var PbxprojTemplateText = Resource.GetResourceText(@"Templates\xcode9\Default.xcodeproj\project.pbxproj");
                var g = new PbxprojGenerator(p, ProjectReferences, ModulePath, Path.Combine(BuildDirectory, "projects"), PbxprojTemplateText, BuildingOperatingSystem, TargetOperationSystem);
                g.Generate(EnableRebuild);
            }
            else if (Toolchain == ToolchainType.CMake)
            {
                var g = new CMakeProjectGenerator(p, ProjectReferences, ModulePath, Path.Combine(BuildDirectory, "projects"), Toolchain, Compiler, BuildingOperatingSystem, TargetOperationSystem);
                g.Generate(EnableRebuild);
            }
            else
            {
                throw new NotSupportedException();
            }
            return new ProjectReference
            {
                Id = GetIdForProject(ModuleName),
                Name = ModuleName,
                VirtualDir = "modules/" + ModuleName,
                FilePath = Path.Combine(BuildDirectory, Path.Combine("projects", GetProjectFileName(ModuleName)))
            };
        }
        private ProjectReference GenerateTestProject(String ModuleName, String ModulePath, String TestName, Cpp.File TestFile)
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
                var g = new VcxprojGenerator(p, GetIdForProject(TestName), ProjectReferences, Path.GetDirectoryName(TestFile.Path), Path.Combine(BuildDirectory, "projects"), VcxprojTemplateText, VcxprojFilterTemplateText, BuildingOperatingSystem, TargetOperationSystem);
                g.Generate(EnableRebuild);
            }
            else if (Toolchain == ToolchainType.Mac_XCode)
            {
                var PbxprojTemplateText = Resource.GetResourceText(@"Templates\xcode9\Default.xcodeproj\project.pbxproj");
                var g = new PbxprojGenerator(p, ProjectReferences, Path.GetDirectoryName(TestFile.Path), Path.Combine(BuildDirectory, "projects"), PbxprojTemplateText, BuildingOperatingSystem, TargetOperationSystem);
                g.Generate(EnableRebuild);
            }
            else if (Toolchain == ToolchainType.CMake)
            {
                var g = new CMakeProjectGenerator(p, ProjectReferences, Path.GetDirectoryName(TestFile.Path), Path.Combine(BuildDirectory, "projects"), Toolchain, Compiler, BuildingOperatingSystem, TargetOperationSystem);
                g.Generate(EnableRebuild);
            }
            else
            {
                throw new NotSupportedException();
            }
            return new ProjectReference
            {
                Id = GetIdForProject(TestName),
                Name = TestName,
                VirtualDir = "modules/" + ModuleName,
                FilePath = Path.Combine(BuildDirectory, Path.Combine("projects", GetProjectFileName(TestName)))
            };
        }
        private ProjectReference GenerateSampleProject(String SampleName, String SamplePath)
        {
            var InlcudeDirectories = new List<String> { SamplePath };
            var SourceDirectories = new List<String> { SamplePath };
            var Libs = new List<String> { };
            var Files = SourceDirectories.SelectMany(d => GetFilesInDirectory(d)).ToList();
            var ProjectReferences = new List<ProjectReference> { };

            if (ModuleDependencies.ContainsKey(SampleName))
            {
                foreach (var ReferenceModule in GetAllModuleDependencies(SampleName, false))
                {
                    InlcudeDirectories.Add(Path.GetFullPath(Path.Combine(SamplePath, Path.Combine("..", Path.Combine("..", Path.Combine("modules", Path.Combine(ReferenceModule, "include")))))));
                    ProjectReferences.Add(new ProjectReference
                    {
                        Id = GetIdForProject(ReferenceModule),
                        Name = ReferenceModule,
                        VirtualDir = "modules/" + ReferenceModule,
                        FilePath = Path.Combine(BuildDirectory, Path.Combine("projects", GetProjectFileName(ReferenceModule)))
                    });
                }
            }

            var p = new Project
            {
                Name = SampleName,
                Configurations = (new List<Configuration>
                {
                    new Configuration
                    {
                        TargetType = TargetType.Executable,
                        IncludeDirectories = InlcudeDirectories,
                        Libs = Libs,
                        Files = Files
                    },
                    new Configuration
                    {
                        TargetOperatingSystem = OperatingSystemType.iOS,
                        BundleIdentifier = SolutionName + "." + SampleName
                    }
                }).Concat(GetCommonConfigurations()).ToList()
            };
            if (Toolchain == ToolchainType.Windows_VisualC)
            {
                var VcxprojTemplateText = Resource.GetResourceText(@"Templates\vc15\Default.vcxproj");
                var VcxprojFilterTemplateText = Resource.GetResourceText(@"Templates\vc15\Default.vcxproj.filters");
                var g = new VcxprojGenerator(p, GetIdForProject(SampleName), ProjectReferences, SamplePath, Path.Combine(BuildDirectory, "projects"), VcxprojTemplateText, VcxprojFilterTemplateText, BuildingOperatingSystem, TargetOperationSystem);
                g.Generate(EnableRebuild);
            }
            else if (Toolchain == ToolchainType.Mac_XCode)
            {
                var PbxprojTemplateText = Resource.GetResourceText(@"Templates\xcode9\Default.xcodeproj\project.pbxproj");
                var g = new PbxprojGenerator(p, ProjectReferences, SamplePath, Path.Combine(BuildDirectory, "projects"), PbxprojTemplateText, BuildingOperatingSystem, TargetOperationSystem);
                g.Generate(EnableRebuild);
            }
            else if (Toolchain == ToolchainType.CMake)
            {
                var g = new CMakeProjectGenerator(p, ProjectReferences, SamplePath, Path.Combine(BuildDirectory, "projects"), Toolchain, Compiler, BuildingOperatingSystem, TargetOperationSystem);
                g.Generate(EnableRebuild);
            }
            else
            {
                throw new NotSupportedException();
            }
            return new ProjectReference
            {
                Id = GetIdForProject(SampleName),
                Name = SampleName,
                VirtualDir = "samples",
                FilePath = Path.Combine(BuildDirectory, Path.Combine("projects", GetProjectFileName(SampleName)))
            };
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

        private static List<Cpp.File> GetFilesInDirectory(String d)
        {
            if (!Directory.Exists(d)) { return new List<Cpp.File> { }; }
            var l = new List<Cpp.File>();
            foreach (var FilePath in Directory.EnumerateFiles(d, "*", SearchOption.AllDirectories))
            {
                var Ext = Path.GetExtension(FilePath).TrimStart('.').ToLowerInvariant();
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
                    if (Regex.IsMatch(Path.GetFileNameWithoutExtension(FilePath), @"(_linux|_mac|_android|_ios)$", RegexOptions.ExplicitCapture))
                    {
                        l.Add(new Cpp.File { Path = FilePath, Type = FileType.Unknown });
                    }
                    else
                    {
                        l.Add(new Cpp.File { Path = FilePath, Type = FileType.CppSource });
                    }
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
            else if (Toolchain == ToolchainType.CMake)
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
                if (Added.Count != 0 || ContainSelf)
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
            else if (Toolchain == ToolchainType.CMake)
            {
                return ProjectName;
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
