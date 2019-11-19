using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TypeMake.Cpp.CMakeProjectGeneratorDetail;

namespace TypeMake.Cpp
{
    public class CMakeProjectGenerator
    {
        private Project Project;
        private List<ProjectReference> ProjectReferences;
        private PathString InputDirectory;
        private PathString OutputDirectory;
        private OperatingSystemType HostOperatingSystem;
        private ArchitectureType HostArchitecture;
        private OperatingSystemType TargetOperatingSystem;
        private ArchitectureType? TargetArchitectureType;
        private ConfigurationType? ConfigurationType;
        private ToolchainType Toolchain;
        private CompilerType Compiler;
        private CLibraryType CLibrary;
        private CLibraryForm CLibraryForm;
        private CppLibraryType CppLibrary;
        private CppLibraryForm CppLibraryForm;
        private bool EnableAbsolutePath;

        public CMakeProjectGenerator(Project Project, List<ProjectReference> ProjectReferences, PathString InputDirectory, PathString OutputDirectory, OperatingSystemType HostOperatingSystem, ArchitectureType HostArchitecture, OperatingSystemType TargetOperatingSystem, ArchitectureType? TargetArchitectureType, ToolchainType Toolchain, CompilerType Compiler, CLibraryType CLibrary, CLibraryForm CLibraryForm, CppLibraryType CppLibrary, CppLibraryForm CppLibraryForm, ConfigurationType? ConfigurationType, bool EnableAbsolutePath)
        {
            this.Project = Project;
            this.ProjectReferences = ProjectReferences;
            this.InputDirectory = InputDirectory.FullPath;
            this.OutputDirectory = OutputDirectory.FullPath;
            this.HostOperatingSystem = HostOperatingSystem;
            this.HostArchitecture = HostArchitecture;
            this.TargetOperatingSystem = TargetOperatingSystem;
            this.TargetArchitectureType = TargetArchitectureType;
            this.Toolchain = Toolchain;
            this.Compiler = Compiler;
            this.CLibrary = CLibrary;
            this.CLibraryForm = CLibraryForm;
            this.CppLibrary = CppLibrary;
            this.CppLibraryForm = CppLibraryForm;
            this.ConfigurationType = ConfigurationType;
            this.EnableAbsolutePath = EnableAbsolutePath;
        }

        public void Generate(bool ForceRegenerate, out bool NeedInstallStrip)
        {
            var CMakeListsPath = OutputDirectory / Project.Name / "CMakeLists.txt";
            var BaseDirPath = CMakeListsPath.Parent;

            var Lines = GenerateLines(CMakeListsPath, BaseDirPath).ToList();
            TextFile.WriteToFile(CMakeListsPath, String.Join("\n", Lines), new UTF8Encoding(false), !ForceRegenerate);
            NeedInstallStrip = ((Project.TargetType == TargetType.Executable) || (Project.TargetType == TargetType.DynamicLibrary)) && (ConfigurationType == Cpp.ConfigurationType.Release) && ((TargetOperatingSystem == OperatingSystemType.Linux) || (TargetOperatingSystem == OperatingSystemType.MacOS) || (TargetOperatingSystem == OperatingSystemType.Android));
        }

        private IEnumerable<String> GenerateLines(PathString CMakeListsPath, PathString BaseDirPath)
        {
            var conf = Project.Configurations.Merged(Project.TargetType, HostOperatingSystem, HostArchitecture, TargetOperatingSystem, TargetArchitectureType, Toolchain, Compiler, CLibrary, CLibraryForm, CppLibrary, CppLibraryForm, ConfigurationType);

            yield return @"cmake_minimum_required(VERSION 3.3.2)";
            yield return $@"project({Project.Name})";

            var EnableConvertToWsl = (HostOperatingSystem == OperatingSystemType.Windows) && (TargetOperatingSystem == OperatingSystemType.Linux);
            Func<PathString, String> GetFinalPath = (PathString p) =>
            {
                if (EnableConvertToWsl)
                {
                    return p.RelativeTo(BaseDirPath, EnableAbsolutePath).ToWslPath().ToString(PathStringStyle.Unix);
                }
                return p.RelativeTo(BaseDirPath, EnableAbsolutePath).ToString(PathStringStyle.Unix);
            };

            if ((Project.TargetType == TargetType.Executable) || (Project.TargetType == TargetType.DynamicLibrary))
            {
                var LibDirectories = conf.LibDirectories.Select(d => GetFinalPath(d.FullPath)).ToList();
                if (LibDirectories.Count != 0)
                {
                    yield return @"link_directories(";
                    foreach (var d in LibDirectories)
                    {
                        yield return "  " + (d.Contains(" ") ? "\"" + d + "\"" : d);
                    }
                    yield return @")";
                }
            }

            if (Project.TargetType == TargetType.Executable)
            {
                yield return @"add_executable(${PROJECT_NAME} """")";
            }
            else if (Project.TargetType == TargetType.StaticLibrary)
            {
                yield return @"add_library(${PROJECT_NAME} STATIC """")";
            }
            else if (Project.TargetType == TargetType.DynamicLibrary)
            {
                yield return @"add_library(${PROJECT_NAME} SHARED """")";
            }
            else
            {
                throw new NotSupportedException("NotSupportedTargetType: " + Project.TargetType.ToString());
            }
            if (!String.IsNullOrEmpty(Project.TargetName) && (Project.TargetName != Project.Name))
            {
                yield return $@"set_property(TARGET ${{PROJECT_NAME}} PROPERTY OUTPUT_NAME {Project.TargetName})";
            }
            String OutDir;
            if (conf.OutputDirectory != null)
            {
                OutDir = GetFinalPath(conf.OutputDirectory);
            }
            else if (TargetArchitectureType.HasValue)
            {
                var Architecture = TargetArchitectureType.Value;
                OutDir = $@"${{CMAKE_CURRENT_BINARY_DIR}}/../../{Architecture}_${{CMAKE_BUILD_TYPE}}";
            }
            else
            {
                OutDir = $@"${{CMAKE_CURRENT_BINARY_DIR}}/../../${{CMAKE_BUILD_TYPE}}";
            }
            if (((Project.TargetType == TargetType.Executable) || (Project.TargetType == TargetType.DynamicLibrary)) && (ConfigurationType == Cpp.ConfigurationType.Release) && ((TargetOperatingSystem == OperatingSystemType.Linux) || (TargetOperatingSystem == OperatingSystemType.MacOS) || (TargetOperatingSystem == OperatingSystemType.Android)))
            {
                yield return $@"set_property(TARGET ${{PROJECT_NAME}} PROPERTY ARCHIVE_OUTPUT_DIRECTORY {OutDir}_symbol)";
                yield return $@"set_property(TARGET ${{PROJECT_NAME}} PROPERTY LIBRARY_OUTPUT_DIRECTORY {OutDir}_symbol)";
                yield return $@"set_property(TARGET ${{PROJECT_NAME}} PROPERTY RUNTIME_OUTPUT_DIRECTORY {OutDir}_symbol)";
                yield return $@"install(TARGETS ${{PROJECT_NAME}}";
                yield return $@"  DESTINATION {OutDir}";
                yield return $@")";
            }
            else
            {
                yield return $@"set_property(TARGET ${{PROJECT_NAME}} PROPERTY ARCHIVE_OUTPUT_DIRECTORY {OutDir})";
                yield return $@"set_property(TARGET ${{PROJECT_NAME}} PROPERTY LIBRARY_OUTPUT_DIRECTORY {OutDir})";
                yield return $@"set_property(TARGET ${{PROJECT_NAME}} PROPERTY RUNTIME_OUTPUT_DIRECTORY {OutDir})";
            }

            yield return @"target_sources(${PROJECT_NAME} PRIVATE";
            foreach (var f in conf.Files)
            {
                if ((f.Type == FileType.CSource) || (f.Type == FileType.CppSource) || (f.Type == FileType.ObjectiveCSource) || (f.Type == FileType.ObjectiveCppSource))
                {
                    yield return "  " + GetFinalPath(f.Path.FullPath);
                }
            }
            yield return @")";

            foreach (var g in conf.Files.GroupBy(f => f.Path.FullPath.Parent))
            {
                var Name = g.Key.RelativeTo(InputDirectory).ToString(PathStringStyle.Windows).Replace(@"\", @"\\").Replace(":", "");
                yield return $@"source_group({Name} FILES";
                foreach (var f in g)
                {
                    yield return "  " + GetFinalPath(f.Path.FullPath);
                }
                yield return @")";
            }

            foreach (var f in conf.Files)
            {
                var FilePath = GetFinalPath(f.Path.FullPath);
                var FileConf = f.Configurations.Merged(Project.TargetType, HostOperatingSystem, HostArchitecture, TargetOperatingSystem, TargetArchitectureType, Toolchain, Compiler, CLibrary, CLibraryForm, CppLibrary, CppLibraryForm, ConfigurationType);
                var FileDefines = FileConf.Defines;
                if (FileDefines.Count != 0)
                {
                    var FileDefinesStr = String.Join(";", FileDefines.Select(d => (d.Key + (d.Value == null ? "" : "=" + d.Value).Replace("\"", "\\\""))));
                    yield return $@"set_source_files_properties ({FilePath} PROPERTIES COMPILE_DEFINITIONS ""{FileDefinesStr}"")";
                }
                var FileFlags = FileConf.CommonFlags;
                if ((f.Type == FileType.CSource) || (f.Type == FileType.ObjectiveCSource))
                {
                    FileFlags = FileFlags.Concat(FileConf.CFlags).ToList();
                }
                else if ((f.Type == FileType.CppSource) || (f.Type == FileType.ObjectiveCppSource))
                {
                    FileFlags = FileFlags.Concat(FileConf.CppFlags).ToList();
                }
                if (FileFlags.Count != 0)
                {
                    var FileFlagsStr = String.Join(" ", FileFlags).Replace("\"", "\\\"");
                    yield return $@"set_source_files_properties ({FilePath} PROPERTIES COMPILE_FLAGS ""{FileFlagsStr}"")";
                }
            }

            var IncludeDirectories = conf.IncludeDirectories.Select(d => GetFinalPath(d.FullPath)).ToList();
            if (IncludeDirectories.Count != 0)
            {
                yield return @"target_include_directories(${PROJECT_NAME} PRIVATE";
                foreach (var d in IncludeDirectories)
                {
                    yield return "  " + (d.Contains(" ") ? "\"" + d + "\"" : d);
                }
                yield return @")";
            }
            var Defines = conf.Defines;
            if (Defines.Count != 0)
            {
                yield return @"target_compile_definitions(${PROJECT_NAME} PRIVATE";
                foreach (var d in Defines)
                {
                    yield return @"  " + d.Key + (d.Value == null ? "" : "=" + d.Value);
                }
                yield return @")";
            }
            var CommonFlags = conf.CommonFlags.Select(f => (f == null ? "" : Regex.IsMatch(f, @"[ ""^|]") ? "\"" + f.Replace("\"", "\\\"") + "\"" : f)).ToList();
            var CFlags = conf.CFlags.Select(f => (f == null ? "" : Regex.IsMatch(f, @"[ ""^|]") ? "\"" + f.Replace("\"", "\\\"") + "\"" : f)).ToList();
            var CppFlags = conf.CppFlags.Select(f => (f == null ? "" : Regex.IsMatch(f, @"[ ""^|]") ? "\"" + f.Replace("\"", "\\\"") + "\"" : f)).ToList();
            var Flags = String.Join(" ", CommonFlags.Concat(CFlags.Select(f => "$<$<COMPILE_LANGUAGE:C>:" + f + ">")).Concat(CppFlags.Select(f => "$<$<COMPILE_LANGUAGE:CXX>:" + f + ">")));
            if (Flags.Length != 0)
            {
                yield return @"target_compile_options(${PROJECT_NAME} PRIVATE " + Flags + ")";
            }

            if ((Project.TargetType == TargetType.Executable) || (Project.TargetType == TargetType.DynamicLibrary))
            {
                var LinkerFlags = conf.LinkerFlags.Select(f => (f == null ? "" : Regex.IsMatch(f, @"[ ""^|]") ? "\"" + f.Replace("\"", "\"\"") + "\"" : f)).ToList();
                if (LinkerFlags.Count > 0)
                {
                    var LinkerFlagStr = String.Join(" ", LinkerFlags.Select(f => f.Replace("\\", "\\\\").Replace("\"", "\\\"")));
                    yield return @"set_target_properties(${PROJECT_NAME} PROPERTIES LINK_FLAGS """ + LinkerFlagStr + @""")";
                }

                var PostObjectFileLinkerFlags = new List<String>();
                foreach (var p in ProjectReferences)
                {
                    PostObjectFileLinkerFlags.Add(p.Name);
                }
                Func<String, String> WrapLinkerFlag = f =>
                {
                    if (TargetOperatingSystem == OperatingSystemType.MacOS)
                    {
                        return "\"-Wl,-pie " + f + " -Wl,-pie\""; //bypass CMake limitation with the order of linker flags and the addition of '-l' before library with a relative or absolute path
                    }
                    else
                    {
                        return "\"-Wl,--as-needed " + f + " -Wl,--no-as-needed\""; //bypass CMake limitation with the order of linker flags and the addition of '-l' before library with a relative or absolute path
                    }
                };
                foreach (var Lib in conf.Libs)
                {
                    if (Lib.Parts.Count == 1)
                    {
                        PostObjectFileLinkerFlags.Add(Lib.ToString(PathStringStyle.Unix));
                    }
                    else
                    {
                        PostObjectFileLinkerFlags.Add(WrapLinkerFlag(GetFinalPath(Lib)));
                    }
                }
                PostObjectFileLinkerFlags.AddRange(conf.PostLinkerFlags.Select(f => WrapLinkerFlag(f)));

                if (PostObjectFileLinkerFlags.Count > 0)
                {
                    var ExecutingOperatingSystem = (HostOperatingSystem == OperatingSystemType.Windows) && (TargetOperatingSystem == OperatingSystemType.Linux) ? TargetOperatingSystem : HostOperatingSystem;
                    yield return @"target_link_libraries(${PROJECT_NAME} PRIVATE";
                    if ((Compiler == CompilerType.gcc) || (Compiler == CompilerType.clang))
                    {
                        if (ExecutingOperatingSystem != OperatingSystemType.MacOS)
                        {
                            yield return @"  -Wl,--start-group";
                        }
                    }
                    foreach (var f in PostObjectFileLinkerFlags)
                    {
                        yield return "  " + f;
                    }
                    if ((Compiler == CompilerType.gcc) || (Compiler == CompilerType.clang))
                    {
                        if (ExecutingOperatingSystem != OperatingSystemType.MacOS)
                        {
                            yield return @"  -Wl,--end-group";
                        }
                    }
                    yield return @")";
                }
                else
                {
                    yield return @"target_link_libraries(${PROJECT_NAME})";
                }
            }

            yield return "";
        }
    }

    namespace CMakeProjectGeneratorDetail
    {
        internal static class Utils
        {
            public static PathString RelativeTo(this PathString p, PathString BaseDirectory, bool EnableAbsolutePath)
            {
                if (EnableAbsolutePath) { return p.FullPath; }
                return p.RelativeTo(BaseDirectory);
            }
        }
    }
}
