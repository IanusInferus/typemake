using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TypeMake.Cpp
{
    public class NinjaProjectGenerator
    {
        private Project Project;
        private List<ProjectReference> ProjectReferences;
        private PathString InputDirectory;
        private PathString OutputDirectory;
        private ToolchainType Toolchain;
        private CompilerType Compiler;
        private OperatingSystemType HostOperatingSystem;
        private ArchitectureType HostArchitecture;
        private OperatingSystemType TargetOperatingSystem;
        private ArchitectureType TargetArchitectureType;
        private ConfigurationType ConfigurationType;

        public NinjaProjectGenerator(Project Project, List<ProjectReference> ProjectReferences, PathString InputDirectory, PathString OutputDirectory, ToolchainType Toolchain, CompilerType Compiler, OperatingSystemType HostOperatingSystem, ArchitectureType HostArchitecture, OperatingSystemType TargetOperatingSystem, ArchitectureType TargetArchitectureType, ConfigurationType ConfigurationType)
        {
            this.Project = Project;
            this.ProjectReferences = ProjectReferences;
            this.InputDirectory = InputDirectory.FullPath;
            this.OutputDirectory = OutputDirectory.FullPath;
            this.Toolchain = Toolchain;
            this.Compiler = Compiler;
            this.HostOperatingSystem = HostOperatingSystem;
            this.HostArchitecture = HostArchitecture;
            this.TargetOperatingSystem = TargetOperatingSystem;
            this.TargetArchitectureType = TargetArchitectureType;
            this.ConfigurationType = ConfigurationType;
        }

        public void Generate(bool ForceRegenerate)
        {
            var NinjaScriptPath = OutputDirectory / Project.Name + ".ninja";
            var BaseDirPath = NinjaScriptPath.Parent;

            var Lines = GenerateLines(NinjaScriptPath, BaseDirPath).ToList();
            TextFile.WriteToFile(NinjaScriptPath, String.Join("\n", Lines), new UTF8Encoding(false), !ForceRegenerate);
        }

        private IEnumerable<String> GenerateLines(PathString NinjaScriptPath, PathString BaseDirPath)
        {
            var conf = Project.Configurations.Merged(Project.TargetType, Toolchain, Compiler, HostOperatingSystem, HostArchitecture, TargetOperatingSystem, TargetArchitectureType, ConfigurationType);

            yield return "ninja_required_version = 1.3";
            yield return "";

            var ExecutingOperatingSystem = (HostOperatingSystem == OperatingSystemType.Windows) && (TargetOperatingSystem == OperatingSystemType.Linux) ? TargetOperatingSystem : HostOperatingSystem;
            var Escape = ExecutingOperatingSystem == OperatingSystemType.Windows ? (Func<String, String>)(arg => Shell.EscapeArgument(arg, Shell.ArgumentStyle.Windows)) : (Func<String, String>)(arg => Shell.EscapeArgumentForShell(arg, Shell.ShellArgumentStyle.Bash));

            var CommonFlags = new List<String>();
            CommonFlags.AddRange(conf.IncludeDirectories.Select(d => d.FullPath.RelativeTo(BaseDirPath).ToString(PathStringStyle.Unix)).Select(d => "-I" + d));
            CommonFlags.AddRange(conf.Defines.Select(d => "-D" + d.Key + (d.Value == null ? "" : "=" + d.Value)));
            CommonFlags.AddRange(conf.CommonFlags);

            var CFlags = conf.CFlags.ToList();
            var CppFlags = conf.CppFlags.ToList();
            var LinkerFlags = new List<String>();
            var Libs = new List<String>();
            var Dependencies = new List<String>();
            if ((Project.TargetType == TargetType.Executable) || (Project.TargetType == TargetType.DynamicLibrary))
            {
                if (Project.TargetType == TargetType.DynamicLibrary)
                {
                    LinkerFlags.Add("-shared");
                }
                var LibrarySearchPath = (OutputDirectory / ".." / $"{TargetArchitectureType}_{ConfigurationType}").RelativeTo(BaseDirPath).ToString(PathStringStyle.Unix);
                LinkerFlags.Add($"-L{LibrarySearchPath}");
                LinkerFlags.AddRange(conf.LibDirectories.Select(d => d.FullPath.RelativeTo(BaseDirPath).ToString(PathStringStyle.Unix)).Select(d => "-L" + (d.Contains(" ") ? "\"" + d + "\"" : d)));
                LinkerFlags.AddRange(conf.LinkerFlags);
                Libs.Add("-Wl,--start-group");
                foreach (var Lib in conf.Libs)
                {
                    if (Lib.Parts.Count == 1)
                    {
                        Libs.Add("-l" + Lib.ToString(PathStringStyle.Unix));
                    }
                    else
                    {
                        Libs.Add(Lib.RelativeTo(BaseDirPath).ToString(PathStringStyle.Unix));
                    }
                }
                foreach (var p in ProjectReferences)
                {
                    Libs.Add("-l" + p.Name);
                    Dependencies.Add((LibrarySearchPath.AsPath() / "lib" + p.Name + ".a").ToString(PathStringStyle.Unix));
                }
                Libs.Add("-Wl,--end-group");
            }

            yield return "commonflags  = " + String.Join(" ", CommonFlags.Select(f => Escape(f)));
            yield return "cflags  = " + String.Join(" ", CFlags.Select(f => Escape(f)));
            yield return "cxxflags  = " + String.Join(" ", CppFlags.Select(f => Escape(f)));
            yield return "ldflags  = " + String.Join(" ", LinkerFlags.Select(f => Escape(f)));
            yield return "libs  = " + String.Join(" ", Libs.Select(f => Escape(f)));

            yield return "";

            var ObjectFilePaths = new List<String>();
            foreach (var File in conf.Files)
            {
                if ((File.Type != FileType.CSource) && (File.Type != FileType.CppSource)) { continue; }

                var FileConf = File.Configurations.Merged(Project.TargetType, Toolchain, Compiler, HostOperatingSystem, HostArchitecture, TargetOperatingSystem, TargetArchitectureType, ConfigurationType);

                var FileFlags = new List<String>();
                FileFlags.AddRange(FileConf.Defines.Select(d => "-D" + d.Key + (d.Value == null ? "" : "=" + d.Value)));
                FileFlags.AddRange(FileConf.CommonFlags);

                if (File.Type == FileType.CSource)
                {
                    FileFlags.AddRange(FileConf.CFlags);
                }
                else if (File.Type == FileType.CppSource)
                {
                    FileFlags.AddRange(FileConf.CppFlags);
                }

                var FilePath = File.Path.FullPath.RelativeTo(BaseDirPath).ToString(PathStringStyle.Unix);
                var ObjectFilePath = $"{Project.Name}/{File.Path.FullPath.RelativeTo(InputDirectory).ToString(PathStringStyle.Unix).Replace(".", "_")}.o";
                if (File.Type == FileType.CSource)
                {
                    yield return $"build {ObjectFilePath}: cc {FilePath}";
                }
                else if (File.Type == FileType.CppSource)
                {
                    yield return $"build {ObjectFilePath}: cxx {FilePath}";
                }
                ObjectFilePaths.Add(ObjectFilePath);

                if (FileFlags.Count > 0)
                {
                    yield return $"  fileflags = {String.Join(" ", FileFlags.Select(f => Escape(f)))}";
                }
            }

            var TargetName = "";
            var RuleName = "";
            if (Project.TargetType == TargetType.Executable)
            {
                TargetName = Project.TargetName ?? Project.Name;
                RuleName = "link";
            }
            else if (Project.TargetType == TargetType.StaticLibrary)
            {
                TargetName = "lib" + (Project.TargetName ?? Project.Name) + ".a";
                RuleName = "ar";
            }
            else if (Project.TargetType == TargetType.DynamicLibrary)
            {
                TargetName = "lib" + (Project.TargetName ?? Project.Name) + ".so";
                RuleName = "link";
            }
            else
            {
                throw new NotSupportedException("NotSupportedTargetType: " + Project.TargetType.ToString());
            }

            yield return "";

            var TargetPath = ((conf.OutputDirectory != null ? conf.OutputDirectory : (OutputDirectory / ".." / $"{TargetArchitectureType}_{ConfigurationType}")) / TargetName).RelativeTo(BaseDirPath).ToString(PathStringStyle.Unix);

            yield return $"build {TargetPath}: {RuleName} {String.Join(" ", ObjectFilePaths)}" + (Dependencies.Count > 0 ? " | " + String.Join(" ", Dependencies) : "");

            yield return "";
        }
    }
}
