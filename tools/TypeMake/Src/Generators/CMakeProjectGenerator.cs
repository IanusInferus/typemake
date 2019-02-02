﻿using System;
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

        public CMakeProjectGenerator(Project Project, List<ProjectReference> ProjectReferences, PathString InputDirectory, PathString OutputDirectory, ToolchainType Toolchain, CompilerType Compiler, OperatingSystemType BuildingOperatingSystem, ArchitectureType BuildingOperatingSystemArchitecture, OperatingSystemType TargetOperatingSystem, ArchitectureType? TargetArchitectureType, ConfigurationType? ConfigurationType)
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
            var conf = Project.Configurations.Merged(Toolchain, Compiler, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem, TargetArchitectureType, ConfigurationType);

            yield return @"cmake_minimum_required(VERSION 3.0.2)";
            yield return $@"project({Project.Name})";

            if ((conf.TargetType == TargetType.Executable) || (conf.TargetType == TargetType.DynamicLibrary) || (conf.TargetType == TargetType.GradleApplication) || (conf.TargetType == TargetType.GradleLibrary))
            {
                var LibDirectories = conf.LibDirectories.Select(d => d.FullPath.RelativeTo(BaseDirPath).ToString(PathStringStyle.Unix)).ToList();
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

            if (conf.TargetType == TargetType.Executable)
            {
                yield return @"add_executable(${PROJECT_NAME} """")";
            }
            else if (conf.TargetType == TargetType.StaticLibrary)
            {
                yield return @"add_library(${PROJECT_NAME} STATIC """")";
            }
            else if ((conf.TargetType == TargetType.DynamicLibrary) || (conf.TargetType == TargetType.GradleApplication) || (conf.TargetType == TargetType.GradleLibrary))
            {
                yield return @"add_library(${PROJECT_NAME} SHARED """")";
            }
            else
            {
                throw new NotSupportedException("NotSupportedTargetType: " + conf.TargetType.ToString());
            }
            if (!String.IsNullOrEmpty(Project.TargetName) && (Project.TargetName != Project.Name))
            {
                yield return $@"set_property(TARGET ${{PROJECT_NAME}} PROPERTY OUTPUT_NAME {Project.TargetName})";
            }
            if (TargetArchitectureType.HasValue)
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
                    yield return "  " + f.Path.FullPath.RelativeTo(BaseDirPath).ToString(PathStringStyle.Unix);
                }
            }
            yield return @")";

            foreach (var g in conf.Files.GroupBy(f => f.Path.FullPath.Parent))
            {
                var Name = g.Key.RelativeTo(InputDirectory).ToString(PathStringStyle.Windows).Replace(@"\", @"\\");
                yield return $@"source_group({Name} FILES";
                foreach (var f in g)
                {
                    yield return "  " + f.Path.FullPath.RelativeTo(BaseDirPath).ToString(PathStringStyle.Unix);
                }
                yield return @")";
            }

            var IncludeDirectories = conf.IncludeDirectories.Select(d => d.FullPath.RelativeTo(BaseDirPath).ToString(PathStringStyle.Unix)).ToList();
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
                    yield return @"  -D" + d.Key + (d.Value == null ? "" : "=" + (Regex.IsMatch(d.Value, @"["" ^|]") ? "\"" + d.Value.Replace("\"", "") + "\"" : d.Value));
                }
                yield return @")";
            }
            var CFlags = conf.CFlags;
            var CppFlags = conf.CppFlags;
            var CFlagStr = String.Join(" ", CFlags.Select(f => (f == null ? "" : Regex.IsMatch(f, @"[ ""^|]") ? "\"" + f.Replace("\"", "\\\"") + "\"" : f)));
            var CppFlagStr = String.Join(" ", CppFlags.Select(f => (f == null ? "" : Regex.IsMatch(f, @"[ ""^|]") ? "\"" + f.Replace("\"", "\\\"") + "\"" : f)));
            if (CFlags.Count + CppFlags.Count != 0)
            {
                yield return @"target_compile_options(${PROJECT_NAME} PRIVATE " + CFlagStr + ((CFlags.Count > 0) && (CppFlags.Count > 0) ? " " : "") + (CppFlags.Count > 0 ? "$<$<COMPILE_LANGUAGE:CXX>:" + CppFlagStr + ">" : "") + ")";
            }

            if ((conf.TargetType == TargetType.Executable) || (conf.TargetType == TargetType.DynamicLibrary) || (conf.TargetType == TargetType.GradleApplication) || (conf.TargetType == TargetType.GradleLibrary))
            {
                var LinkerFlags = conf.LinkerFlags;
                if (LinkerFlags.Count != 0)
                {
                    var LinkerFlagStr = String.Join(" ", CFlags.Select(f => (f == null ? "" : Regex.IsMatch(f, @"[ ""^|]") ? "\"" + f.Replace("\"", "\"\"") + "\"" : f)));
                    yield return @"set_target_properties(${PROJECT_NAME} PROPERTIES LINK_FLAGS " + LinkerFlagStr + ")";
                }

                if (ProjectReferences.Count + conf.Libs.Count > 0)
                {
                    yield return @"target_link_libraries(${PROJECT_NAME} PRIVATE";
                    foreach (var p in ProjectReferences)
                    {
                        yield return "  " + p.Name;
                    }
                    foreach (var lib in conf.Libs)
                    {
                        yield return "  " + lib.ToString(PathStringStyle.Unix);
                    }
                    yield return @")";
                }
            }

            yield return "";
        }
    }
}