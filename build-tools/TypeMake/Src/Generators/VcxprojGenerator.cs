using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace TypeMake.Cpp
{
    public class VcxprojGenerator
    {
        private Project Project;
        private String ProjectId;
        private List<ProjectReference> ProjectReferences;
        private PathString BuildDirectory;
        private PathString InputDirectory;
        private PathString OutputDirectory;
        private String VcxprojTemplateText;
        private String VcxprojFilterTemplateText;
        private String PackagesConfigText;
        private OperatingSystemType HostOperatingSystem;
        private ArchitectureType HostArchitecture;
        private OperatingSystemType TargetOperatingSystem;
        private ArchitectureType? TargetArchitecture;
        private WindowsRuntimeType? WindowsRuntime;
        private CompilerType Compiler;
        private CLibraryType CLibrary;
        private CLibraryForm CLibraryForm;
        private CppLibraryType CppLibrary;
        private CppLibraryForm CppLibraryForm;
        private String CC;
        private String CXX;
        private String AR;

        public VcxprojGenerator(Project Project, String ProjectId, List<ProjectReference> ProjectReferences, PathString BuildDirectory, PathString InputDirectory, PathString OutputDirectory, String VcxprojTemplateText, String VcxprojFilterTemplateText, String PackagesConfigText, OperatingSystemType HostOperatingSystem, ArchitectureType HostArchitecture, OperatingSystemType TargetOperatingSystem, ArchitectureType? TargetArchitecture, WindowsRuntimeType? WindowsRuntime, CompilerType Compiler, CLibraryType CLibrary, CLibraryForm CLibraryForm, CppLibraryType CppLibrary, CppLibraryForm CppLibraryForm, String CC, String CXX, String AR)
        {
            this.Project = new Project { Id = Project.Id, Name = Project.Name, VirtualDir = Project.VirtualDir, FilePath = Project.FilePath, TargetType = Project.TargetType, TargetName = Project.TargetName, Configurations = Project.Configurations.ToList() };
            if (WindowsRuntime == WindowsRuntimeType.WinRT)
            {
                this.Project.Configurations.Add(new Configuration { Files = new List<File> { new File { Path = OutputDirectory / "packages.config", Type = FileType.Unknown } } });
            }
            this.ProjectId = ProjectId;
            this.ProjectReferences = ProjectReferences;
            this.BuildDirectory = BuildDirectory.FullPath;
            this.InputDirectory = InputDirectory.FullPath;
            this.OutputDirectory = OutputDirectory.FullPath;
            this.VcxprojTemplateText = VcxprojTemplateText;
            this.VcxprojFilterTemplateText = VcxprojFilterTemplateText;
            this.PackagesConfigText = PackagesConfigText;
            this.HostOperatingSystem = HostOperatingSystem;
            this.HostArchitecture = HostArchitecture;
            this.TargetOperatingSystem = TargetOperatingSystem;
            this.TargetArchitecture = TargetArchitecture;
            this.WindowsRuntime = WindowsRuntime;
            this.Compiler = Compiler;
            this.CLibrary = CLibrary;
            this.CLibraryForm = CLibraryForm;
            this.CppLibrary = CppLibrary;
            this.CppLibraryForm = CppLibraryForm;
            this.CC = CC;
            this.CXX = CXX;
            this.AR = AR;
        }

        public void Generate(bool ForceRegenerate)
        {
            GenerateVcxproj(ForceRegenerate);
            GenerateVcxprojFilters(ForceRegenerate);
            GeneratePackagesConfig(ForceRegenerate);
        }

        private void GenerateVcxproj(bool ForceRegenerate)
        {
            var VcxprojPath = OutputDirectory / (Project.Name + ".vcxproj");
            var BaseDirPath = VcxprojPath.Parent;

            var xVcxproj = XmlFile.FromString(VcxprojTemplateText);
            Trim(xVcxproj);

            var xn = xVcxproj.Name.Namespace;

            if (TargetArchitecture != null)
            {
                var rProjectConfigurationInclude = new Regex(@"[^|]+\|(?<Architecture>[^|]+)");
                foreach (var e in xVcxproj.Elements(xn + "ItemGroup").Where(e => (e.Attribute("Label") != null) && (e.Attribute("Label").Value == "ProjectConfigurations")).SelectMany(e => e.Elements(xn + "ProjectConfiguration")).ToArray())
                {
                    var m = rProjectConfigurationInclude.Match(e.Attribute("Include").Value);
                    if (m.Success)
                    {
                        var Architecture = m.Result("${Architecture}");
                        if (Architecture != GetArchitectureString(TargetOperatingSystem, TargetArchitecture.Value))
                        {
                            e.Remove();
                        }
                    }
                }

                var rCondition = new Regex(@"'\$\(Configuration\)\|\$\(Platform\)'=='[^|]+\|(?<Architecture>[^|]+)'");
                foreach (var e in xVcxproj.Elements().ToArray())
                {
                    if (e.Attribute("Condition") != null)
                    {
                        var m = rCondition.Match(e.Attribute("Condition").Value);
                        if (m.Success)
                        {
                            var Architecture = m.Result("${Architecture}");
                            if (Architecture != GetArchitectureString(TargetOperatingSystem, TargetArchitecture.Value))
                            {
                                e.Remove();
                            }
                        }
                    }
                }
            }

            foreach (var ig in xVcxproj.Elements(xn + "ItemGroup").ToArray())
            {
                if (ig.Attribute("Label") != null) { continue; }

                var None = ig.Elements().Where(e => e.Name == xn + "None").ToArray();
                var ClInclude = ig.Elements().Where(e => e.Name == xn + "ClInclude").ToArray();
                var ClCompile = ig.Elements().Where(e => e.Name == xn + "ClCompile").ToArray();
                var ProjectReference = ig.Elements().Where(e => e.Name == xn + "ProjectReference").ToArray();
                foreach (var e in None)
                {
                    e.Remove();
                }
                foreach (var e in ClInclude)
                {
                    e.Remove();
                }
                foreach (var e in ClCompile)
                {
                    e.Remove();
                }
                foreach (var e in ProjectReference)
                {
                    e.Remove();
                }
                if (!ig.HasElements && !ig.HasAttributes)
                {
                    ig.Remove();
                }
            }

            var GlobalsPropertyGroup = xVcxproj.Elements(xn + "PropertyGroup").Where(e => e.Attribute("Label") != null && e.Attribute("Label").Value == "Globals").FirstOrDefault();
            if (GlobalsPropertyGroup == null)
            {
                GlobalsPropertyGroup = new XElement(xn + "PropertyGroup", new XAttribute("Label", "Globals"));
                xVcxproj.Add(GlobalsPropertyGroup);
            }
            var g = "{" + ProjectId.ToUpper() + "}";
            GlobalsPropertyGroup.SetElementValue(xn + "ProjectGuid", g);
            GlobalsPropertyGroup.SetElementValue(xn + "RootNamespace", Project.Name);
            if (TargetOperatingSystem == OperatingSystemType.Windows)
            {
                GlobalsPropertyGroup.SetElementValue(xn + "WindowsTargetPlatformVersion", GetWindowsTargetPlatformVersion());
                if (WindowsRuntime == WindowsRuntimeType.Win32)
                {
                    GlobalsPropertyGroup.SetElementValue(xn + "Keyword", "Win32Proj");
                }
                else if (WindowsRuntime == WindowsRuntimeType.WinRT)
                {
                    GlobalsPropertyGroup.SetElementValue(xn + "AppContainerApplication", true);
                    GlobalsPropertyGroup.SetElementValue(xn + "ApplicationType", "Windows Store");
                    GlobalsPropertyGroup.SetElementValue(xn + "ApplicationTypeRevision", "10.0");
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
            else if (TargetOperatingSystem == OperatingSystemType.Linux)
            {
                GlobalsPropertyGroup.SetElementValue(xn + "Keyword", "Linux");
            }
            else
            {
                throw new InvalidOperationException();
            }

            var globalConf = Project.Configurations.Merged(Project.TargetType, HostOperatingSystem, HostArchitecture, TargetOperatingSystem, TargetArchitecture, WindowsRuntime, ToolchainType.VisualStudio, Compiler, CLibrary, CLibraryForm, CppLibrary, CppLibraryForm, null);
            foreach (var o in globalConf.Options)
            {
                var Prefix = "vc.Globals.";
                if (o.Key.StartsWith(Prefix))
                {
                    var Key = o.Key.Substring(Prefix.Length);
                    GlobalsPropertyGroup.SetElementValue(xn + Key, o.Value);
                }
            }

            var ExistingConfigurationTypeAndArchitectures = new Dictionary<KeyValuePair<ConfigurationType, ArchitectureType>, String>();
            var ProjectConfigurations = xVcxproj.Elements(xn + "ItemGroup").Where(e => (e.Attribute("Label") != null) && (e.Attribute("Label").Value == "ProjectConfigurations")).SelectMany(e => e.Elements(xn + "ProjectConfiguration")).Select(e => e.Element(xn + "Configuration").Value + "|" + e.Element(xn + "Platform").Value).ToDictionary(s => s);
            foreach (var Architecture in new ArchitectureType[] { ArchitectureType.x86, ArchitectureType.x64, ArchitectureType.armv7a, ArchitectureType.arm64 })
            {
                foreach (var ConfigurationType in Enum.GetValues(typeof(ConfigurationType)).Cast<ConfigurationType>())
                {
                    var Name = ConfigurationType.ToString() + "|" + GetArchitectureString(TargetOperatingSystem, Architecture);
                    if (ProjectConfigurations.ContainsKey(Name))
                    {
                        ExistingConfigurationTypeAndArchitectures.Add(new KeyValuePair<ConfigurationType, ArchitectureType>(ConfigurationType, Architecture), Name);
                    }
                }
            }

            foreach (var Pair in ExistingConfigurationTypeAndArchitectures)
            {
                var ConfigurationType = Pair.Key.Key;
                var Architecture = Pair.Key.Value;
                var Name = Pair.Value;

                var conf = Project.Configurations.Merged(Project.TargetType, HostOperatingSystem, HostArchitecture, TargetOperatingSystem, Architecture, WindowsRuntime, ToolchainType.VisualStudio, Compiler, CLibrary, CLibraryForm, CppLibrary, CppLibraryForm, ConfigurationType);

                var PropertyGroupConfiguration = xVcxproj.Elements(xn + "PropertyGroup").Where(e => (e.Attribute("Condition") != null) && (e.Attribute("Condition").Value == "'$(Configuration)|$(Platform)'=='" + Name + "'") && (e.Attribute("Label") != null) && (e.Attribute("Label").Value == "Configuration")).LastOrDefault();
                if (PropertyGroupConfiguration == null)
                {
                    PropertyGroupConfiguration = new XElement(xn + "PropertyGroup", new XAttribute("Condition", "'$(Configuration)|$(Platform)'=='" + Name + "'"), new XAttribute("Label", "Configuration"));
                    xVcxproj.Add(PropertyGroupConfiguration);
                }

                if (!String.IsNullOrEmpty(Project.TargetName) && (Project.TargetName != Project.Name))
                {
                    PropertyGroupConfiguration.SetElementValue(xn + "TargetName", Project.TargetName);
                }
                if (Project.TargetType == TargetType.Executable)
                {
                    PropertyGroupConfiguration.SetElementValue(xn + "ConfigurationType", "Application");
                }
                else if ((Project.TargetType == TargetType.StaticLibrary) || (Project.TargetType == TargetType.IntermediateStaticLibrary))
                {
                    PropertyGroupConfiguration.SetElementValue(xn + "ConfigurationType", "StaticLibrary");
                }
                else if (Project.TargetType == TargetType.DynamicLibrary)
                {
                    PropertyGroupConfiguration.SetElementValue(xn + "ConfigurationType", "DynamicLibrary");
                }
                else
                {
                    throw new NotSupportedException("NotSupportedTargetType: " + Project.TargetType.ToString());
                }

                if (TargetOperatingSystem == OperatingSystemType.Linux)
                {
                    if (Compiler == CompilerType.gcc)
                    {
                        PropertyGroupConfiguration.SetElementValue(xn + "PlatformToolset", "WSL_1_0");
                    }
                    else if (Compiler == CompilerType.clang)
                    {
                        PropertyGroupConfiguration.SetElementValue(xn + "PlatformToolset", "WSL_Clang_1_0");
                    }
                }

                foreach (var o in conf.Options)
                {
                    var Prefix = "vc.Configuration.";
                    if (o.Key.StartsWith(Prefix))
                    {
                        var Key = o.Key.Substring(Prefix.Length);
                        PropertyGroupConfiguration.SetElementValue(xn + Key, o.Value);
                    }
                }

                var PropertyGroup = xVcxproj.Elements(xn + "PropertyGroup").Where(e => (e.Attribute("Condition") != null) && (e.Attribute("Condition").Value == "'$(Configuration)|$(Platform)'=='" + Name + "'") && (e.Attribute("Label") == null)).LastOrDefault();
                if (PropertyGroup == null)
                {
                    PropertyGroup = new XElement(xn + "PropertyGroup", new XAttribute("Condition", "'$(Configuration)|$(Platform)'=='" + Name + "'"));
                    xVcxproj.Add(PropertyGroup);
                }

                if (conf.OutputDirectory != null)
                {
                    var RelativeOutDir = conf.OutputDirectory.RelativeTo(BuildDirectory);
                    var OutDir = RelativeOutDir.IsFullPath ? RelativeOutDir.ToString(PathStringStyle.Windows) : "$(SolutionDir)" + RelativeOutDir.ToString(PathStringStyle.Windows);
                    if (!OutDir.EndsWith("\\")) { OutDir += "\\"; }
                    PropertyGroup.SetElementValue(xn + "OutDir", OutDir);
                    PropertyGroup.SetElementValue(xn + "DebuggerWorkingDirectory", OutDir);
                }
                else
                {
                    if (TargetArchitecture == null)
                    {
                        PropertyGroup.SetElementValue(xn + "OutDir", $"$(SolutionDir){Architecture}_{ConfigurationType}\\");
                        PropertyGroup.SetElementValue(xn + "DebuggerWorkingDirectory", $"$(SolutionDir){Architecture}_{ConfigurationType}\\");
                    }
                    else
                    {
                        PropertyGroup.SetElementValue(xn + "OutDir", $"$(SolutionDir){ConfigurationType}\\");
                        PropertyGroup.SetElementValue(xn + "DebuggerWorkingDirectory", $"$(SolutionDir){ConfigurationType}\\");
                    }
                }
                if (TargetArchitecture == null)
                {
                    PropertyGroup.SetElementValue(xn + "IntDir", $"$(SolutionDir)projects\\$(ProjectName)\\{Architecture}_{ConfigurationType}\\");
                }
                else
                {
                    PropertyGroup.SetElementValue(xn + "IntDir", $"$(SolutionDir)projects\\$(ProjectName)\\{ConfigurationType}\\");
                }

                if (TargetOperatingSystem == OperatingSystemType.Linux)
                {
                    if (CC != null)
                    {
                        PropertyGroup.SetElementValue(xn + "RemoteCCompileToolExe", CC);
                    }
                    if (CXX != null)
                    {
                        PropertyGroup.SetElementValue(xn + "RemoteCppCompileToolExe", CXX);
                        PropertyGroup.SetElementValue(xn + "RemoteLdToolExe", CXX);
                    }
                    if (AR != null)
                    {
                        PropertyGroup.SetElementValue(xn + "RemoteArToolExe", AR);
                    }
                    PropertyGroup.SetElementValue(xn + "EnableIncrementalBuild", "WithNinja");
                    if (Project.TargetType == TargetType.Executable)
                    {
                        PropertyGroup.SetElementValue(xn + "TargetExt", "");
                    }
                }

                foreach (var o in conf.Options)
                {
                    var Prefix = "vc.PropertyGroup.";
                    if (o.Key.StartsWith(Prefix))
                    {
                        var Key = o.Key.Substring(Prefix.Length);
                        PropertyGroupConfiguration.SetElementValue(xn + Key, o.Value);
                    }
                }

                var ItemDefinitionGroup = xVcxproj.Elements(xn + "ItemDefinitionGroup").Where(e => (e.Attribute("Condition") != null) && (e.Attribute("Condition").Value == "'$(Configuration)|$(Platform)'=='" + Name + "'")).LastOrDefault();
                if (ItemDefinitionGroup == null)
                {
                    ItemDefinitionGroup = new XElement(xn + "ItemDefinitionGroup", new XAttribute("Condition", "'$(Configuration)|$(Platform)'=='" + Name + "'"));
                    xVcxproj.Add(ItemDefinitionGroup);
                }
                var ClCompile = ItemDefinitionGroup.Element(xn + "ClCompile");
                if (ClCompile == null)
                {
                    ClCompile = new XElement(xn + "ClCompile");
                    ItemDefinitionGroup.Add(ClCompile);
                }
                var IncludeDirectories = conf.IncludeDirectories.Select(d => d.FullPath.RelativeTo(BaseDirPath).ToString(PathStringStyle.Windows)).ToList();
                if (IncludeDirectories.Count != 0)
                {
                    ClCompile.SetElementValue(xn + "AdditionalIncludeDirectories", String.Join(";", IncludeDirectories) + ";%(AdditionalIncludeDirectories)");
                }
                var Defines = conf.Defines;
                if (Defines.Count != 0)
                {
                    ClCompile.SetElementValue(xn + "PreprocessorDefinitions", String.Join(";", Defines.Select(d => d.Key + (d.Value == null ? "" : "=" + d.Value))) + ";%(PreprocessorDefinitions)");
                }
                var CompilerFlags = conf.SystemIncludeDirectories.SelectMany(d => new String[] { "/external:I", d.FullPath.RelativeTo(BaseDirPath).ToString(PathStringStyle.Windows) }).Concat(conf.CommonFlags).Concat(conf.CppFlags).ToList();
                if (CompilerFlags.Count != 0)
                {
                    ClCompile.SetElementValue(xn + "AdditionalOptions", "%(AdditionalOptions) " + String.Join(" ", CompilerFlags.Select(f => (f == null ? "" : Regex.IsMatch(f, @"^[0-9]+$") ? f : "\"" + f.Replace("\"", "\"\"\"") + "\""))));
                }

                foreach (var o in conf.Options)
                {
                    var Prefix = "vc.ClCompile.";
                    if (o.Key.StartsWith(Prefix))
                    {
                        ClCompile.SetElementValue(xn + o.Key.Substring(Prefix.Length), o.Value);
                    }
                }

                if ((Project.TargetType == TargetType.StaticLibrary) || (Project.TargetType == TargetType.IntermediateStaticLibrary))
                {
                    var Lib = ItemDefinitionGroup.Element(xn + "Lib");
                    if (Lib == null)
                    {
                        Lib = new XElement(xn + "Lib");
                        ItemDefinitionGroup.Add(Lib);
                    }

                    foreach (var o in conf.Options)
                    {
                        var Prefix = "vc.Lib.";
                        if (o.Key.StartsWith(Prefix))
                        {
                            Lib.SetElementValue(xn + o.Key.Substring(Prefix.Length), o.Value);
                        }
                    }
                }

                if ((Project.TargetType == TargetType.Executable) || (Project.TargetType == TargetType.DynamicLibrary))
                {
                    var Link = ItemDefinitionGroup.Element(xn + "Link");
                    if (Link == null)
                    {
                        Link = new XElement(xn + "Link");
                        ItemDefinitionGroup.Add(Link);
                    }
                    var LibDirectories = conf.LibDirectories.Select(d => d.FullPath.RelativeTo(BaseDirPath).ToString(PathStringStyle.Windows)).ToList();
                    if (LibDirectories.Count != 0)
                    {
                        Link.SetElementValue(xn + "AdditionalLibraryDirectories", String.Join(";", LibDirectories) + ";%(AdditionalLibraryDirectories)");
                    }
                    if (TargetOperatingSystem == OperatingSystemType.Windows)
                    {
                        var Libs = conf.Libs.Select(Lib => Lib.Parts.Count == 1 ? Lib.ToString(PathStringStyle.Windows) : Lib.RelativeTo(BaseDirPath).ToString(PathStringStyle.Windows)).ToList();
                        if (Libs.Count != 0)
                        {
                            Link.SetElementValue(xn + "AdditionalDependencies", String.Join(";", Libs) + ";%(AdditionalDependencies)");
                        }
                    }
                    else
                    {
                        var Libs = conf.Libs.Select(Lib => Lib.Parts.Count == 1 ? Lib.Extension == "" ? "-l" + Lib.ToString(PathStringStyle.Unix) : "-l:" + Lib.ToString(PathStringStyle.Unix) : Lib.RelativeTo(BaseDirPath).ToString(PathStringStyle.Unix)).ToList();
                        Libs.Add("-Wl,--end-group");
                        if (Libs.Count != 0)
                        {
                            Link.SetElementValue(xn + "AdditionalDependencies", String.Join(";", Libs) + ";%(AdditionalDependencies)");
                        }
                    }
                    var LinkerFlags = conf.LinkerFlags.Select(f => (f == null ? "" : Regex.IsMatch(f, @"^[0-9]+$") ? f : "\"" + f.Replace("\"", "\"\"").Replace("$", "$$") + "\"")).ToList();
                    var PostLinkerFlags = conf.PostLinkerFlags.Select(f => (f == null ? "" : Regex.IsMatch(f, @"^[0-9]+$") ? f : "\"" + f.Replace("\"", "\"\"").Replace("$", "$$") + "\"")).ToList();
                    if (TargetOperatingSystem != OperatingSystemType.Windows)
                    {
                        PostLinkerFlags.Add("-Wl,--start-group");
                    }
                    if (LinkerFlags.Count + PostLinkerFlags.Count != 0)
                    {
                        Link.SetElementValue(xn + "AdditionalOptions", String.Join(" ", LinkerFlags.Concat(new List<String> { "%(AdditionalOptions)" }).Concat(PostLinkerFlags)));
                    }

                    foreach (var o in conf.Options)
                    {
                        var Prefix = "vc.Link.";
                        if (o.Key.StartsWith(Prefix))
                        {
                            Link.SetElementValue(xn + o.Key.Substring(Prefix.Length), o.Value);
                        }
                    }
                }
            }

            var Import = xVcxproj.Elements(xn + "Import").LastOrDefault();

            foreach (var gConf in Project.Configurations.Matches(Project.TargetType, HostOperatingSystem, HostArchitecture, TargetOperatingSystem, TargetArchitecture, WindowsRuntime, ToolchainType.VisualStudio, Compiler, CLibrary, CLibraryForm, CppLibrary, CppLibraryForm, null).GroupBy(conf => Tuple.Create(conf.MatchingConfigurationTypes, conf.MatchingTargetArchitectures?.Intersect(new ArchitectureType[] { ArchitectureType.x86, ArchitectureType.x64, ArchitectureType.armv7a, ArchitectureType.arm64 }).ToList()), new ConfigurationTypesAndArchitecturesComparer()))
            {
                var MatchingConfigurationTypes = gConf.Key.Item1;
                var MatchingTargetArchitectures = gConf.Key.Item2;
                if ((MatchingConfigurationTypes != null) && (MatchingConfigurationTypes.Count == 0)) { continue; }
                if ((MatchingTargetArchitectures != null) && (MatchingTargetArchitectures.Count == 0)) { continue; }
                var Conditions = GetConditions(TargetOperatingSystem, MatchingConfigurationTypes, MatchingTargetArchitectures);

                foreach (var Condition in Conditions)
                {
                    var FileItemGroup = new XElement(xn + "ItemGroup");
                    if (Import != null)
                    {
                        Import.AddBeforeSelf(FileItemGroup);
                    }
                    else
                    {
                        xVcxproj.Add(FileItemGroup);
                    }
                    if (Condition.Item1 != null)
                    {
                        FileItemGroup.Add(new XAttribute("Condition", Condition.Item1));
                    }

                    var ProjectConfMatched = Project.Configurations.Matches(Project.TargetType, HostOperatingSystem, HostArchitecture, TargetOperatingSystem, Condition.Item3 ?? TargetArchitecture, WindowsRuntime, ToolchainType.VisualStudio, Compiler, CLibrary, CLibraryForm, CppLibrary, CppLibraryForm, Condition.Item2).ToList();
                    foreach (var File in gConf.SelectMany(conf => conf.Files))
                    {
                        var RelativePath = File.Path.FullPath.RelativeTo(BaseDirPath);
                        var RelativePathStr = RelativePath.ToString(PathStringStyle.Windows);
                        XElement x;
                        if (File.Type == FileType.Header)
                        {
                            x = new XElement(xn + "ClInclude", new XAttribute("Include", RelativePathStr));
                        }
                        else if ((File.Type == FileType.CSource) || (File.Type == FileType.CppSource))
                        {
                            x = new XElement(xn + "ClCompile", new XAttribute("Include", RelativePathStr));
                            if (Compiler == CompilerType.VisualCpp)
                            {
                                // workaround Visual Studio bug which slows build https://developercommunity.visualstudio.com/idea/586584/vs2017-cc-multi-processor-compilation-does-not-wor.html
                                x.Add(new XElement(xn + "ObjectFileName", "$(IntDir)" + RelativePath.Parent.ToString(PathStringStyle.Windows).Replace("..", "__").Replace(":", "_") + "\\"));
                            }
                            else
                            {
                                x.Add(new XElement(xn + "ObjectFileName", "$(IntDir)" + RelativePath.ToString(PathStringStyle.Windows).Replace("..", "__").Replace(":", "_") + ".o"));
                            }
                        }
                        else if (File.Type == FileType.NatVis)
                        {
                            x = new XElement(xn + "Natvis", new XAttribute("Include", RelativePathStr));
                        }
                        else
                        {
                            x = new XElement(xn + "None", new XAttribute("Include", RelativePathStr));
                        }
                        if ((File.Type == FileType.CSource) || (File.Type == FileType.CppSource))
                        {
                            var FileConfs = File.Configurations.Matches(Project.TargetType, HostOperatingSystem, HostArchitecture, TargetOperatingSystem, Condition.Item3, WindowsRuntime, ToolchainType.VisualStudio, Compiler, CLibrary, CLibraryForm, CppLibrary, CppLibraryForm, Condition.Item2).ToList();
                            if (FileConfs.Count == 0)
                            {
                                FileConfs.Add(new Configuration { MatchingConfigurationTypes = MatchingConfigurationTypes, MatchingTargetArchitectures = MatchingTargetArchitectures });
                            }

                            foreach (var conf in FileConfs)
                            {
                                var FileMatchingConfigurationTypes = conf.MatchingConfigurationTypes;
                                var FileMatchingTargetArchitectures = conf.MatchingTargetArchitectures;
                                if ((FileMatchingConfigurationTypes != null) && (MatchingConfigurationTypes != null))
                                {
                                    FileMatchingConfigurationTypes = FileMatchingConfigurationTypes.Intersect(MatchingConfigurationTypes).ToList();
                                    if (FileMatchingConfigurationTypes.Count == MatchingConfigurationTypes.Count)
                                    {
                                        FileMatchingConfigurationTypes = null;
                                    }
                                }
                                if ((FileMatchingTargetArchitectures != null) && (MatchingTargetArchitectures != null))
                                {
                                    FileMatchingTargetArchitectures = FileMatchingTargetArchitectures.Intersect(MatchingTargetArchitectures).ToList();
                                    if (FileMatchingTargetArchitectures.Count == MatchingTargetArchitectures.Count)
                                    {
                                        FileMatchingTargetArchitectures = null;
                                    }
                                }
                                var FileConditions = GetConditions(TargetOperatingSystem, FileMatchingConfigurationTypes, FileMatchingTargetArchitectures);

                                foreach (var FileCondition in FileConditions)
                                {
                                    var Attributes = new XAttribute[] { };
                                    if (FileCondition.Item1 != null)
                                    {
                                        Attributes = new XAttribute[] { new XAttribute("Condition", FileCondition.Item1) };
                                    }

                                    var IncludeDirectories = conf.IncludeDirectories.Select(d => d.FullPath.RelativeTo(BaseDirPath).ToString(PathStringStyle.Windows)).ToList();
                                    if (IncludeDirectories.Count != 0)
                                    {
                                        x.Add(new XElement(xn + "AdditionalIncludeDirectories", String.Join(";", IncludeDirectories) + ";%(AdditionalIncludeDirectories)", Attributes));
                                    }
                                    var Defines = conf.Defines;
                                    if (Defines.Count != 0)
                                    {
                                        x.Add(new XElement(xn + "PreprocessorDefinitions", String.Join(";", Defines.Select(d => d.Key + (d.Value == null ? "" : "=" + d.Value))) + ";%(PreprocessorDefinitions)", Attributes));
                                    }
                                    var CompilerFlags = conf.SystemIncludeDirectories.SelectMany(d => new String[] { "/external:I", d.FullPath.RelativeTo(BaseDirPath).ToString(PathStringStyle.Windows) }).Concat(conf.CommonFlags).ToList();
                                    if ((File.Type == FileType.CSource) || (File.Type == FileType.ObjectiveCSource))
                                    {
                                        var ProjectConfMatchedMatchedMatched = ProjectConfMatched.Matches(null, null, null, null, FileCondition.Item3, null, null, null, null, null, null, null, FileCondition.Item2).ToList();
                                        var ProjectCFlags = ProjectConfMatchedMatchedMatched.SelectMany(c => c.SystemIncludeDirectories.SelectMany(d => new String[] { "/external:I", d.FullPath.RelativeTo(BaseDirPath).ToString(PathStringStyle.Windows) }).Concat(c.CommonFlags).Concat(c.CFlags)).ToList();
                                        CompilerFlags = ProjectCFlags.Concat(CompilerFlags.Concat(conf.CFlags)).ToList();
                                        x.Add(new XElement(xn + "AdditionalOptions", String.Join(" ", CompilerFlags.Select(f => (f == null ? "" : Regex.IsMatch(f, @"^[0-9]+$") ? f : "\"" + f.Replace("\"", "\"\"\"") + "\""))), Attributes));
                                    }
                                    else if ((File.Type == FileType.CppSource) || (File.Type == FileType.ObjectiveCppSource))
                                    {
                                        CompilerFlags = CompilerFlags.Concat(conf.CppFlags).ToList();
                                        if (CompilerFlags.Count != 0)
                                        {
                                            x.Add(new XElement(xn + "AdditionalOptions", "%(AdditionalOptions) " + String.Join(" ", CompilerFlags.Select(f => (f == null ? "" : Regex.IsMatch(f, @"^[0-9]+$") ? f : "\"" + f.Replace("\"", "\"\"\"") + "\""))), Attributes));
                                        }
                                    }

                                    foreach (var o in conf.Options)
                                    {
                                        var Prefix = "vc.ClCompile.";
                                        if (o.Key.StartsWith(Prefix))
                                        {
                                            x.Add(new XElement(xn + o.Key.Substring(Prefix.Length), o.Value, Attributes));
                                        }
                                    }
                                }
                            }
                        }
                        FileItemGroup.Add(x);
                    }
                    if (!FileItemGroup.HasElements)
                    {
                        FileItemGroup.Remove();
                    }
                }
            }

            var ProjectItemGroup = new XElement(xn + "ItemGroup");
            if (Import != null)
            {
                Import.AddBeforeSelf(ProjectItemGroup);
            }
            else
            {
                xVcxproj.Add(ProjectItemGroup);
            }
            foreach (var p in ProjectReferences)
            {
                var RelativePath = p.FilePath.FullPath.RelativeTo(BaseDirPath).ToString(PathStringStyle.Windows);
                var x = new XElement(xn + "ProjectReference", new XAttribute("Include", RelativePath));
                x.Add(new XElement(xn + "Project", "{" + p.Id.ToUpper() + "}"));
                x.Add(new XElement(xn + "Name", p.Name));
                x.Add(new XElement(xn + "LinkLibraryDependecies", "true"));
                if (WindowsRuntime == WindowsRuntimeType.WinRT)
                {
                    x.Add(new XElement(xn + "ReferenceOutputAssembly", "false"));
                }
                ProjectItemGroup.Add(x);
            }
            if (!ProjectItemGroup.HasElements)
            {
                ProjectItemGroup.Remove();
            }

            //https://developercommunity.visualstudio.com/idea/555602/c-referenced-dlls-copylocal-dosnt-work.html
            if (Project.TargetType == TargetType.DynamicLibrary)
            {
                var OutputCopyItemGroup = new XElement(xn + "ItemGroup");
                xVcxproj.Add(OutputCopyItemGroup);

                OutputCopyItemGroup.Add(new XElement(xn + "Content", new XAttribute("Include", "$(TargetPath)"), new XElement(xn + "Link", "%(Filename)%(Extension)"), new XElement(xn + "CopyToOutputDirectory", "PreserveNewest")));
            }

            var sVcxproj = XmlFile.ToString(xVcxproj);
            TextFile.WriteToFile(VcxprojPath, sVcxproj, Encoding.UTF8, !ForceRegenerate);
        }

        private void GenerateVcxprojFilters(bool ForceRegenerate)
        {
            var FilterPath = OutputDirectory / (Project.Name + ".vcxproj.filters");
            var BaseDirPath = FilterPath.Parent;

            var xFilter = XmlFile.FromString(VcxprojFilterTemplateText);
            Trim(xFilter);

            var xn = xFilter.Name.Namespace;

            foreach (var ig in xFilter.Elements(xn + "ItemGroup").ToArray())
            {
                if (ig.Attribute("Label") != null) { continue; }

                var None = ig.Elements().Where(e => e.Name == xn + "None").ToArray();
                var ClInclude = ig.Elements().Where(e => e.Name == xn + "ClInclude").ToArray();
                var ClCompile = ig.Elements().Where(e => e.Name == xn + "ClCompile").ToArray();
                var Filter = ig.Elements().Where(e => e.Name == xn + "Filter").ToArray();
                foreach (var e in None)
                {
                    e.Remove();
                }
                foreach (var e in ClInclude)
                {
                    e.Remove();
                }
                foreach (var e in ClCompile)
                {
                    e.Remove();
                }
                foreach (var e in Filter)
                {
                    e.Remove();
                }
                if (!ig.HasElements && !ig.HasAttributes)
                {
                    ig.Remove();
                }
            }

            var FilterItemGroup = new XElement(xn + "ItemGroup");
            var FileItemGroup = new XElement(xn + "ItemGroup");
            xFilter.Add(FilterItemGroup);
            xFilter.Add(FileItemGroup);

            var Files = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
            var Filters = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
            foreach (var conf in Project.Configurations.Matches(Project.TargetType, HostOperatingSystem, HostArchitecture, TargetOperatingSystem, null, WindowsRuntime, ToolchainType.VisualStudio, Compiler, CLibrary, CLibraryForm, CppLibrary, CppLibraryForm, null))
            {
                foreach (var f in conf.Files)
                {
                    if (Files.Contains(f.Path)) { continue; }
                    Files.Add(f.Path);

                    var RelativePath = f.Path.FullPath.RelativeTo(BaseDirPath).ToString(PathStringStyle.Windows);
                    var Dir = f.Path.FullPath.RelativeTo(InputDirectory).Parent.ToString(PathStringStyle.Windows);
                    while (Dir.StartsWith(@"..\"))
                    {
                        Dir = Dir.Substring(3);
                    }
                    if (!Filters.Contains(Dir))
                    {
                        var CurrentDir = Dir.AsPath();
                        var CurrentDirFilter = CurrentDir.ToString(PathStringStyle.Windows);
                        while ((CurrentDirFilter != ".") && !Filters.Contains(CurrentDirFilter))
                        {
                            Filters.Add(CurrentDirFilter);
                            CurrentDir = CurrentDir.Parent;
                            CurrentDirFilter = CurrentDir.ToString(PathStringStyle.Windows);
                        }
                    }

                    XElement x;
                    if (f.Type == FileType.Header)
                    {
                        x = new XElement(xn + "ClInclude", new XAttribute("Include", RelativePath));
                    }
                    else if (f.Type == FileType.CSource)
                    {
                        x = new XElement(xn + "ClCompile", new XAttribute("Include", RelativePath));
                    }
                    else if (f.Type == FileType.CppSource)
                    {
                        x = new XElement(xn + "ClCompile", new XAttribute("Include", RelativePath));
                    }
                    else if (f.Type == FileType.NatVis)
                    {
                        x = new XElement(xn + "Natvis", new XAttribute("Include", RelativePath));
                    }
                    else
                    {
                        x = new XElement(xn + "None", new XAttribute("Include", RelativePath));
                    }
                    if (Dir != ".")
                    {
                        x.Add(new XElement(xn + "Filter", Dir));
                    }
                    FileItemGroup.Add(x);
                }
            }

            foreach (var f in Filters.OrderBy(ff => ff, StringComparer.OrdinalIgnoreCase))
            {
                var g = Guid.ParseExact(Hash.GetHashForPath(Project.Name + "/" + f, 32), "N").ToString().ToUpper();
                FilterItemGroup.Add(new XElement(xn + "Filter", new XAttribute("Include", f), new XElement(xn + "UniqueIdentifier", "{" + g + "}")));
            }

            if (!FilterItemGroup.HasElements)
            {
                FilterItemGroup.Remove();
            }
            if (!FileItemGroup.HasElements)
            {
                FileItemGroup.Remove();
            }

            var sFilter = XmlFile.ToString(xFilter);
            TextFile.WriteToFile(FilterPath, sFilter, Encoding.UTF8, !ForceRegenerate);
        }

        private void GeneratePackagesConfig(bool ForceRegenerate)
        {
            if (PackagesConfigText == null) { return; }
            var ConfigPath = OutputDirectory / "packages.config";
            TextFile.WriteToFile(ConfigPath, PackagesConfigText, Encoding.UTF8, !ForceRegenerate);
        }

        private static void Trim(XElement x)
        {
            var TextNodes = x.DescendantNodesAndSelf().Where(n => n.NodeType == XmlNodeType.Text).Select(n => (XText)(n)).ToArray();
            var rWhitespace = new Regex(@"^\s*$", RegexOptions.ExplicitCapture);
            foreach (var tn in TextNodes)
            {
                if (rWhitespace.Match(tn.Value).Success)
                {
                    if (!(tn.Parent != null && !tn.Parent.HasElements))
                    {
                        tn.Remove();
                    }
                }
            }
        }

        public static String GetArchitectureString(OperatingSystemType OperatingSystem, ArchitectureType Architecture)
        {
            if (Architecture == ArchitectureType.x86)
            {
                if (OperatingSystem == OperatingSystemType.Windows)
                {
                    return "Win32";
                }
                else
                {
                    return "x86";
                }
            }
            else if (Architecture == ArchitectureType.x64)
            {
                return "x64";
            }
            else if (Architecture == ArchitectureType.armv7a)
            {
                return "ARM";
            }
            else if (Architecture == ArchitectureType.arm64)
            {
                return "ARM64";
            }
            else
            {
                throw new NotSupportedException("NotSupportedArchitecture: " + Architecture.ToString());
            }
        }

        private static Object WindowsTargetPlatformVersionLock = new Object();
        private static String WindowsTargetPlatformVersion = null;
        private String GetWindowsTargetPlatformVersion()
        {
            if (HostOperatingSystem == OperatingSystemType.Windows)
            {
                lock (WindowsTargetPlatformVersionLock)
                {
                    if (WindowsTargetPlatformVersion == null)
                    {
                        String Value;
                        using (var LocalMachineKey = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, Microsoft.Win32.RegistryView.Registry32))
                        using (var Key = LocalMachineKey.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SDKs\Windows\v10.0", false))
                        {
                            Value = Key.GetValue("ProductVersion") as String;
                        }
                        if (Value == null)
                        {
                            WindowsTargetPlatformVersion = "10.0.18362.0";
                        }
                        else
                        {
                            if (Value.Split('.').Length == 3)
                            {
                                WindowsTargetPlatformVersion = Value + ".0";
                            }
                            else
                            {
                                WindowsTargetPlatformVersion = Value;
                            }
                        }
                    }
                    return WindowsTargetPlatformVersion;
                }
            }
            else
            {
                return "10.0.18362.0";
            }
        }

        private static List<Tuple<String, ConfigurationType?, ArchitectureType?>> GetConditions(OperatingSystemType OperatingSystem, List<ConfigurationType> MatchingConfigurationTypes, List<ArchitectureType> MatchingTargetArchitectures)
        {
            List<Tuple<String, ConfigurationType?, ArchitectureType?>> Conditions;
            if ((MatchingConfigurationTypes != null) || (MatchingTargetArchitectures != null))
            {
                var Keys = "";
                var Values = new List<Tuple<String, ConfigurationType?, ArchitectureType?>> { Tuple.Create<String, ConfigurationType?, ArchitectureType?>("", null, null) };
                if (MatchingConfigurationTypes != null)
                {
                    Keys = (Keys != "" ? Keys + "|" : "") + "$(Configuration)";
                    Values = MatchingConfigurationTypes.SelectMany(t => Values.Select(v => Tuple.Create<String, ConfigurationType?, ArchitectureType?>((v.Item1 != "" ? v.Item1 + "|" : "") + t.ToString(), t, v.Item3))).ToList();
                }
                if (MatchingTargetArchitectures != null)
                {
                    Keys = (Keys != "" ? Keys + "|" : "") + "$(Platform)";
                    Values = MatchingTargetArchitectures.SelectMany(a => Values.Select(v => Tuple.Create<String, ConfigurationType?, ArchitectureType?>((v.Item1 != "" ? v + "|" : "") + GetArchitectureString(OperatingSystem, a), v.Item2, a))).ToList();
                }
                Conditions = Values.Select(v => Tuple.Create<String, ConfigurationType?, ArchitectureType?>("'" + Keys + "' == '" + v.Item1 + "'", v.Item2, v.Item3)).ToList();
            }
            else
            {
                Conditions = new List<Tuple<String, ConfigurationType?, ArchitectureType?>> { Tuple.Create<String, ConfigurationType?, ArchitectureType?>(null, null, null) };
            }

            return Conditions;
        }
    }

    internal class ConfigurationTypesAndArchitecturesComparer : IEqualityComparer<Tuple<List<ConfigurationType>, List<ArchitectureType>>>
    {
        public bool Equals(Tuple<List<ConfigurationType>, List<ArchitectureType>> x, Tuple<List<ConfigurationType>, List<ArchitectureType>> y)
        {
            if ((x.Item1 == null) && (y.Item1 != null)) { return false; }
            if ((x.Item1 != null) && (y.Item1 == null)) { return false; }
            if ((x.Item2 == null) && (y.Item2 != null)) { return false; }
            if ((x.Item2 != null) && (y.Item2 == null)) { return false; }
            return ((x.Item1 == y.Item1) || x.Item1.SequenceEqual(y.Item1)) && ((x.Item2 == y.Item2) || x.Item2.SequenceEqual(y.Item2));
        }

        public int GetHashCode(Tuple<List<ConfigurationType>, List<ArchitectureType>> obj)
        {
            unchecked
            {
                int hash = 17;
                if (obj.Item1 != null)
                {
                    foreach (var v in obj.Item1)
                    {
                        hash = hash * 31 + v.GetHashCode();
                    }
                }
                if (obj.Item2 != null)
                {
                    foreach (var v in obj.Item2)
                    {
                        hash = hash * 31 + v.GetHashCode();
                    }
                }
                return hash;
            }
        }
    }
}
