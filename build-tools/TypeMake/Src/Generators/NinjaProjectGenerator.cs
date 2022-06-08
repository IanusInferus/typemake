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
        private OperatingSystemType HostOperatingSystem;
        private ArchitectureType HostArchitecture;
        private OperatingSystemType TargetOperatingSystem;
        private ArchitectureType TargetArchitecture;
        private WindowsRuntimeType? WindowsRuntime;
        private ToolchainType Toolchain;
        private CompilerType Compiler;
        private CLibraryType CLibrary;
        private CLibraryForm CLibraryForm;
        private CppLibraryType CppLibrary;
        private CppLibraryForm CppLibraryForm;
        private ConfigurationType ConfigurationType;

        public NinjaProjectGenerator(Project Project, List<ProjectReference> ProjectReferences, PathString InputDirectory, PathString OutputDirectory, OperatingSystemType HostOperatingSystem, ArchitectureType HostArchitecture, OperatingSystemType TargetOperatingSystem, ArchitectureType TargetArchitecture, WindowsRuntimeType? WindowsRuntime, ToolchainType Toolchain, CompilerType Compiler, CLibraryType CLibrary, CLibraryForm CLibraryForm, CppLibraryType CppLibrary, CppLibraryForm CppLibraryForm, ConfigurationType ConfigurationType)
        {
            this.Project = Project;
            this.ProjectReferences = ProjectReferences;
            this.InputDirectory = InputDirectory.FullPath;
            this.OutputDirectory = OutputDirectory.FullPath;
            this.HostOperatingSystem = HostOperatingSystem;
            this.HostArchitecture = HostArchitecture;
            this.TargetOperatingSystem = TargetOperatingSystem;
            this.TargetArchitecture = TargetArchitecture;
            this.WindowsRuntime = WindowsRuntime;
            this.Toolchain = Toolchain;
            this.Compiler = Compiler;
            this.CLibrary = CLibrary;
            this.CLibraryForm = CLibraryForm;
            this.CppLibrary = CppLibrary;
            this.CppLibraryForm = CppLibraryForm;
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
            var conf = Project.Configurations.Merged(Project.TargetType, HostOperatingSystem, HostArchitecture, TargetOperatingSystem, TargetArchitecture, WindowsRuntime, Toolchain, Compiler, CLibrary, CLibraryForm, CppLibrary, CppLibraryForm, ConfigurationType);

            yield return "ninja_required_version = 1.3";
            yield return "";

            var ExecutingOperatingSystem = (HostOperatingSystem == OperatingSystemType.Windows) && (TargetOperatingSystem == OperatingSystemType.Linux) ? TargetOperatingSystem : HostOperatingSystem;
            Func<String, String> NinjaEscape = s => s.Replace("$", "$$").Replace(":", "$:").Replace(" ", "$ ");
            var CommandArgumentEscape = ExecutingOperatingSystem == OperatingSystemType.Windows ? (Func<String, String>)(arg => NinjaEscape(Shell.EscapeArgument(arg, Shell.ArgumentStyle.Windows))) : (Func<String, String>)(arg => NinjaEscape(Shell.EscapeArgumentForShell(arg, Shell.ShellArgumentStyle.Bash)));

            var PathStyle = ExecutingOperatingSystem == OperatingSystemType.Windows ? PathStringStyle.Windows : PathStringStyle.Unix;
            var EnableConvertToWsl = (HostOperatingSystem == OperatingSystemType.Windows) && (TargetOperatingSystem == OperatingSystemType.Linux);

            Func<PathString, String> GetFinalPath = (PathString p) =>
            {
                if (EnableConvertToWsl)
                {
                    return p.RelativeTo(BaseDirPath).ToWslPath().ToString(PathStyle);
                }
                return p.RelativeTo(BaseDirPath).ToString(PathStyle);
            };

            var CommonFlags = new List<String>();
            CommonFlags.AddRange(conf.IncludeDirectories.Select(d => GetFinalPath(d.FullPath)).Select(d => "-I" + d));
            if ((Compiler == CompilerType.VisualCpp) || (Compiler == CompilerType.clangcl))
            {
                CommonFlags.AddRange(conf.SystemIncludeDirectories.Select(d => GetFinalPath(d.FullPath)).SelectMany(d => new String[] { "/external:I", d }));
            }
            else
            {
                CommonFlags.AddRange(conf.SystemIncludeDirectories.Select(d => GetFinalPath(d.FullPath)).SelectMany(d => new String[] { "-isystem", d }));
            }
            CommonFlags.AddRange(conf.Defines.Select(d => "-D" + d.Key + (d.Value == null ? "" : "=" + d.Value)));
            CommonFlags.AddRange(conf.CommonFlags);

            var CFlags = conf.CFlags.ToList();
            var CppFlags = conf.CppFlags.ToList();
            var LinkerFlags = new List<String>();
            var PostLinkerFlags = new List<String>();
            var Libs = new List<String>();
            var Dependencies = new List<String>();
            if ((Project.TargetType == TargetType.Executable) || (Project.TargetType == TargetType.DynamicLibrary))
            {
                if (Project.TargetType == TargetType.DynamicLibrary)
                {
                    LinkerFlags.Add("-shared");
                    if (TargetOperatingSystem == OperatingSystemType.MacOS)
                    {
                        LinkerFlags.Add("-Wl,-install_name," + "lib" + (Project.TargetName ?? Project.Name) + ".dylib");
                    }
                    else if ((TargetOperatingSystem == OperatingSystemType.Linux) || (TargetOperatingSystem == OperatingSystemType.Android))
                    {
                        LinkerFlags.Add("-Wl,-soname=" + "lib" + (Project.TargetName ?? Project.Name) + ".so");
                    }
                }
                var LibrarySearchPath = GetFinalPath(OutputDirectory / ".." / $"{ConfigurationType}");
                LinkerFlags.Add($"-L{LibrarySearchPath}");
                LinkerFlags.AddRange(conf.LibDirectories.Select(d => GetFinalPath(d.FullPath)).Select(d => "-L" + (d.Contains(" ") ? "\"" + d + "\"" : d)));
                LinkerFlags.AddRange(conf.LinkerFlags);
                PostLinkerFlags.AddRange(conf.PostLinkerFlags);
                if ((TargetOperatingSystem == OperatingSystemType.Linux) || (TargetOperatingSystem == OperatingSystemType.Android))
                {
                    Libs.Add("-Wl,--start-group");
                }
                foreach (var Lib in conf.Libs)
                {
                    if (Lib.Parts.Count == 1)
                    {
                        if ((Lib.Extension == "") || (TargetOperatingSystem == OperatingSystemType.Windows))
                        {
                            Libs.Add("-l" + Lib.ToString(PathStyle));
                        }
                        else
                        {
                            Libs.Add("-l:" + Lib.ToString(PathStyle));
                        }
                    }
                    else
                    {
                        Libs.Add(GetFinalPath(Lib));
                    }
                }
                foreach (var p in ProjectReferences)
                {
                    if (p.OutputFilePath.ContainsKey(ConfigurationType))
                    {
                        var LibPath = p.OutputFilePath[ConfigurationType];
                        if ((TargetOperatingSystem == OperatingSystemType.Windows) && (p.TargetType == TargetType.DynamicLibrary))
                        {
                            Libs.Add(GetFinalPath(LibPath.ChangeExtension("lib")));
                        }
                        else
                        {
                            Libs.Add(GetFinalPath(LibPath));
                        }
                        Dependencies.Add(GetFinalPath(LibPath));
                    }
                    else
                    {
                        Libs.Add("-l" + p.Name);
                        if (TargetOperatingSystem == OperatingSystemType.Windows)
                        {
                            Dependencies.Add((LibrarySearchPath.AsPath() / p.Name + ".lib").ToString(PathStyle));
                        }
                        else
                        {
                            Dependencies.Add((LibrarySearchPath.AsPath() / "lib" + p.Name + ".a").ToString(PathStyle));
                        }
                    }
                }
                if ((TargetOperatingSystem == OperatingSystemType.Linux) || (TargetOperatingSystem == OperatingSystemType.Android))
                {
                    Libs.Add("-Wl,--end-group");
                }
            }

            yield return "commonflags = " + String.Join(" ", CommonFlags.Select(f => CommandArgumentEscape(f)));
            yield return "cflags = " + String.Join(" ", CFlags.Select(f => CommandArgumentEscape(f)));
            yield return "cxxflags = " + String.Join(" ", CppFlags.Select(f => CommandArgumentEscape(f)));
            yield return "ldflags = " + String.Join(" ", LinkerFlags.Select(f => CommandArgumentEscape(f)));
            yield return "post_ldflags = " + String.Join(" ", PostLinkerFlags.Select(f => CommandArgumentEscape(f)));
            yield return "libs = " + String.Join(" ", Libs.Select(f => CommandArgumentEscape(f)));

            yield return "";

            var ObjectFilePaths = new List<String>();
            foreach (var File in conf.Files)
            {
                if ((File.Type != FileType.CSource) && (File.Type != FileType.CppSource) && (File.Type != FileType.ObjectiveCSource) && (File.Type != FileType.ObjectiveCppSource)) { continue; }

                var FileConf = File.Configurations.Merged(Project.TargetType, HostOperatingSystem, HostArchitecture, TargetOperatingSystem, TargetArchitecture, WindowsRuntime, Toolchain, Compiler, CLibrary, CLibraryForm, CppLibrary, CppLibraryForm, ConfigurationType);

                var FileFlags = new List<String>();
                FileFlags.AddRange(FileConf.Defines.Select(d => "-D" + d.Key + (d.Value == null ? "" : "=" + d.Value)));
                FileFlags.AddRange(FileConf.CommonFlags);

                if ((File.Type == FileType.CSource) || (File.Type == FileType.ObjectiveCSource))
                {
                    FileFlags.AddRange(FileConf.CFlags);
                }
                else if ((File.Type == FileType.CppSource) || (File.Type == FileType.ObjectiveCppSource))
                {
                    FileFlags.AddRange(FileConf.CppFlags);
                }

                var FilePath = GetFinalPath(File.Path.FullPath);
                var ObjectFilePath = (Project.Name.AsPath() / (File.Path.FullPath.RelativeTo(InputDirectory).ToString(PathStyle).Replace("..", "__").Replace(":", "_") + ".o")).ToString(PathStyle);
                if ((File.Type == FileType.CSource) || (File.Type == FileType.ObjectiveCSource))
                {
                    yield return $"build {NinjaEscape(ObjectFilePath)}: cc {NinjaEscape(FilePath)}";
                }
                else if ((File.Type == FileType.CppSource) || (File.Type == FileType.ObjectiveCppSource))
                {
                    yield return $"build {NinjaEscape(ObjectFilePath)}: cxx {NinjaEscape(FilePath)}";
                }
                ObjectFilePaths.Add(ObjectFilePath);

                if (FileFlags.Count > 0)
                {
                    yield return $"  fileflags = {String.Join(" ", FileFlags.Select(f => CommandArgumentEscape(f)))}";
                }
            }

            var TargetName = "";
            var RuleName = "";
            if (Project.TargetType == TargetType.Executable)
            {
                if (TargetOperatingSystem == OperatingSystemType.Windows)
                {
                    TargetName = (Project.TargetName ?? Project.Name) + ".exe";
                }
                else
                {
                    TargetName = Project.TargetName ?? Project.Name;
                }
                RuleName = "link";
            }
            else if (Project.TargetType == TargetType.StaticLibrary)
            {
                if (TargetOperatingSystem == OperatingSystemType.Windows)
                {
                    TargetName = (Project.TargetName ?? Project.Name) + ".lib";
                }
                else
                {
                    TargetName = "lib" + (Project.TargetName ?? Project.Name) + ".a";
                }
                RuleName = "ar";
            }
            else if (Project.TargetType == TargetType.IntermediateStaticLibrary)
            {
                if (TargetOperatingSystem == OperatingSystemType.Windows)
                {
                    TargetName = (Project.TargetName ?? Project.Name) + ".lib";
                }
                else
                {
                    TargetName = "lib" + (Project.TargetName ?? Project.Name) + ".a";
                }
                if ((TargetOperatingSystem == OperatingSystemType.Linux) || (TargetOperatingSystem == OperatingSystemType.Android))
                {
                    RuleName = "arthin";
                }
                else
                {
                    RuleName = "ar";
                }
            }
            else if (Project.TargetType == TargetType.DynamicLibrary)
            {
                if (TargetOperatingSystem == OperatingSystemType.Windows)
                {
                    TargetName = (Project.TargetName ?? Project.Name) + ".dll";
                }
                else if ((TargetOperatingSystem == OperatingSystemType.MacOS) || (TargetOperatingSystem == OperatingSystemType.iOS))
                {
                    TargetName = "lib" + (Project.TargetName ?? Project.Name) + ".dylib";
                }
                else
                {
                    TargetName = "lib" + (Project.TargetName ?? Project.Name) + ".so";
                }
                RuleName = "link";
            }
            else
            {
                throw new NotSupportedException("NotSupportedTargetType: " + Project.TargetType.ToString());
            }

            yield return "";

            var TargetPath = GetFinalPath((conf.OutputDirectory != null ? conf.OutputDirectory : (OutputDirectory / ".." / $"{ConfigurationType}")) / TargetName);
            if (((Project.TargetType == TargetType.Executable) || (Project.TargetType == TargetType.DynamicLibrary)) && (ConfigurationType == ConfigurationType.Release) && ((TargetOperatingSystem == OperatingSystemType.Linux) || (TargetOperatingSystem == OperatingSystemType.MacOS) || (TargetOperatingSystem == OperatingSystemType.Android)))
            {
                var SymbolPath = GetFinalPath(((conf.OutputDirectory != null ? conf.OutputDirectory : (OutputDirectory / ".." / $"{ConfigurationType}")) + "_symbol") / TargetName);
                yield return $"build {NinjaEscape(SymbolPath)}: {RuleName} {String.Join(" ", ObjectFilePaths.Select(p => NinjaEscape(p)))}" + (Dependencies.Count > 0 ? " | " + String.Join(" ", Dependencies.Select(p => NinjaEscape(p))) : "");
                if ((Project.TargetType == TargetType.DynamicLibrary) && (TargetOperatingSystem == OperatingSystemType.MacOS))
                {
                    yield return $"build {NinjaEscape(TargetPath)}: stripx {NinjaEscape(SymbolPath)}";
                }
                else
                {
                    yield return $"build {NinjaEscape(TargetPath)}: strip {NinjaEscape(SymbolPath)}";
                }
            }
            else
            {
                yield return $"build {NinjaEscape(TargetPath)}: {RuleName} {String.Join(" ", ObjectFilePaths.Select(p => NinjaEscape(p)))}" + (Dependencies.Count > 0 ? " | " + String.Join(" ", Dependencies.Select(p => NinjaEscape(p))) : "");
            }

            yield return "";
        }
    }
}
