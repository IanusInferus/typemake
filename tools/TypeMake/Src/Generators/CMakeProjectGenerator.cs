using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TypeMake.Cpp
{
    public class CMakeProjectGenerator
    {
        private Project Project;
        private List<ProjectReference> ProjectReferences;
        private PathString InputDirectory;
        private PathString OutputDirectory;
        private ToolchainType Toolchain;
        private CompilerType Compiler;
        private OperatingSystemType BuildingOperatingSystem;
        private ArchitectureType BuildingOperatingSystemArchitecture;
        private OperatingSystemType TargetOperatingSystem;
        private ArchitectureType? TargetArchitectureType;
        private ConfigurationType? ConfigurationType;
        private bool EnableAbsolutePath;

        public CMakeProjectGenerator(Project Project, List<ProjectReference> ProjectReferences, PathString InputDirectory, PathString OutputDirectory, ToolchainType Toolchain, CompilerType Compiler, OperatingSystemType BuildingOperatingSystem, ArchitectureType BuildingOperatingSystemArchitecture, OperatingSystemType TargetOperatingSystem, ArchitectureType? TargetArchitectureType, ConfigurationType? ConfigurationType, bool EnableAbsolutePath)
        {
            this.Project = Project;
            this.ProjectReferences = ProjectReferences;
            this.InputDirectory = InputDirectory.FullPath;
            this.OutputDirectory = OutputDirectory.FullPath;
            this.Toolchain = Toolchain;
            this.Compiler = Compiler;
            this.BuildingOperatingSystem = BuildingOperatingSystem;
            this.BuildingOperatingSystemArchitecture = BuildingOperatingSystemArchitecture;
            this.TargetOperatingSystem = TargetOperatingSystem;
            this.TargetArchitectureType = TargetArchitectureType;
            this.ConfigurationType = ConfigurationType;
            this.EnableAbsolutePath = EnableAbsolutePath;
        }

        public void Generate(bool ForceRegenerate)
        {
            var CMakeListsPath = OutputDirectory / Project.Name / "CMakeLists.txt";
            var BaseDirPath = CMakeListsPath.Parent;

            var Lines = GenerateLines(CMakeListsPath, BaseDirPath).ToList();
            TextFile.WriteToFile(CMakeListsPath, String.Join("\n", Lines), new UTF8Encoding(false), !ForceRegenerate);
        }

        private IEnumerable<String> GenerateLines(String CMakeListsPath, String BaseDirPath)
        {
            var conf = Project.Configurations.Merged(Project.TargetType, Toolchain, Compiler, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem, TargetArchitectureType, ConfigurationType);

            yield return @"cmake_minimum_required(VERSION 3.3.2)";
            yield return $@"project({Project.Name})";

            if ((Project.TargetType == TargetType.Executable) || (Project.TargetType == TargetType.DynamicLibrary))
            {
                var LibDirectories = conf.LibDirectories.Select(d => d.FullPath.RelativeTo(BaseDirPath, EnableAbsolutePath).ToString(PathStringStyle.Unix)).ToList();
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
            if (conf.OutputDirectory != null)
            {
                var OutDir = conf.OutputDirectory.RelativeTo(BaseDirPath, EnableAbsolutePath).ToString(PathStringStyle.Unix);
                yield return $@"set_property(TARGET ${{PROJECT_NAME}} PROPERTY ARCHIVE_OUTPUT_DIRECTORY {OutDir})";
                yield return $@"set_property(TARGET ${{PROJECT_NAME}} PROPERTY LIBRARY_OUTPUT_DIRECTORY {OutDir})";
                yield return $@"set_property(TARGET ${{PROJECT_NAME}} PROPERTY RUNTIME_OUTPUT_DIRECTORY {OutDir})";
            }
            else if (TargetArchitectureType.HasValue)
            {
                var Architecture = TargetArchitectureType.Value;
                yield return $@"set_property(TARGET ${{PROJECT_NAME}} PROPERTY ARCHIVE_OUTPUT_DIRECTORY ${{CMAKE_CURRENT_BINARY_DIR}}/../../{Architecture}_${{CMAKE_BUILD_TYPE}})";
                yield return $@"set_property(TARGET ${{PROJECT_NAME}} PROPERTY LIBRARY_OUTPUT_DIRECTORY ${{CMAKE_CURRENT_BINARY_DIR}}/../../{Architecture}_${{CMAKE_BUILD_TYPE}})";
                yield return $@"set_property(TARGET ${{PROJECT_NAME}} PROPERTY RUNTIME_OUTPUT_DIRECTORY ${{CMAKE_CURRENT_BINARY_DIR}}/../../{Architecture}_${{CMAKE_BUILD_TYPE}})";
            }
            else
            {
                yield return $@"set_property(TARGET ${{PROJECT_NAME}} PROPERTY ARCHIVE_OUTPUT_DIRECTORY ${{CMAKE_CURRENT_BINARY_DIR}}/../../${{CMAKE_BUILD_TYPE}})";
                yield return $@"set_property(TARGET ${{PROJECT_NAME}} PROPERTY LIBRARY_OUTPUT_DIRECTORY ${{CMAKE_CURRENT_BINARY_DIR}}/../../${{CMAKE_BUILD_TYPE}})";
                yield return $@"set_property(TARGET ${{PROJECT_NAME}} PROPERTY RUNTIME_OUTPUT_DIRECTORY ${{CMAKE_CURRENT_BINARY_DIR}}/../../${{CMAKE_BUILD_TYPE}})";
            }

            yield return @"target_sources(${PROJECT_NAME} PRIVATE";
            foreach (var f in conf.Files)
            {
                if ((f.Type == FileType.CSource) || (f.Type == FileType.CppSource) || (f.Type == FileType.ObjectiveCSource) || (f.Type == FileType.ObjectiveCppSource))
                {
                    yield return "  " + f.Path.FullPath.RelativeTo(BaseDirPath, EnableAbsolutePath).ToString(PathStringStyle.Unix);
                }
            }
            yield return @")";

            foreach (var g in conf.Files.GroupBy(f => f.Path.FullPath.Parent))
            {
                var Name = g.Key.RelativeTo(InputDirectory).ToString(PathStringStyle.Windows).Replace(@"\", @"\\");
                yield return $@"source_group({Name} FILES";
                foreach (var f in g)
                {
                    yield return "  " + f.Path.FullPath.RelativeTo(BaseDirPath, EnableAbsolutePath).ToString(PathStringStyle.Unix);
                }
                yield return @")";
            }

            foreach (var f in conf.Files)
            {
                if (f.Configurations != null)
                {
                    var FilePath = f.Path.FullPath.RelativeTo(BaseDirPath, EnableAbsolutePath).ToString(PathStringStyle.Unix);
                    var FileConf = f.Configurations.Merged(Project.TargetType, Toolchain, Compiler, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem, TargetArchitectureType, ConfigurationType);
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
            }

            var IncludeDirectories = conf.IncludeDirectories.Select(d => d.FullPath.RelativeTo(BaseDirPath, EnableAbsolutePath).ToString(PathStringStyle.Unix)).ToList();
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
                if (LinkerFlags.Count != 0)
                {
                    var LinkerFlagStr = String.Join(" ", LinkerFlags.Select(f => f.Replace("\\", "\\\\").Replace("\"", "\\\"")));
                    yield return @"set_target_properties(${PROJECT_NAME} PROPERTIES LINK_FLAGS """ + LinkerFlagStr + @""")";
                }

                if (ProjectReferences.Count + conf.Libs.Count > 0)
                {
                    yield return @"target_link_libraries(${PROJECT_NAME} PRIVATE";
                    if ((Compiler == CompilerType.gcc) || (Compiler == CompilerType.clang))
                    {
                        yield return @"  -Wl,--start-group";
                    }
                    foreach (var p in ProjectReferences)
                    {
                        yield return "  " + p.Name;
                    }
                    foreach (var lib in conf.Libs)
                    {
                        yield return "  " + lib.ToString(PathStringStyle.Unix);
                    }
                    if ((Compiler == CompilerType.gcc) || (Compiler == CompilerType.clang))
                    {
                        yield return @"  -Wl,--end-group";
                    }
                    yield return @")";
                }
            }

            yield return "";
        }
    }

    internal static class CMakeProjectGeneratorUtils
    {
        public static PathString RelativeTo(this PathString p, PathString BaseDirectory, bool EnableAbsolutePath)
        {
            if (EnableAbsolutePath) { return p; }
            return p.RelativeTo(BaseDirectory);
        }
    }
}
