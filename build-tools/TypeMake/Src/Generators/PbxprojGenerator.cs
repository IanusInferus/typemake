using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static TypeMake.Plist;

namespace TypeMake.Cpp
{
    public class PbxprojGenerator
    {
        private Project Project;
        private List<ProjectReference> ProjectReferences;
        private PathString InputDirectory;
        private PathString OutputDirectory;
        private String PbxprojTemplateText;
        private OperatingSystemType HostOperatingSystem;
        private ArchitectureType HostArchitecture;
        private OperatingSystemType TargetOperatingSystem;
        private ArchitectureType TargetArchitecture;
        private CppLibraryForm CppLibraryForm; //static libc++ requires a custom build
        private String DevelopmentTeam;
        private String ProvisioningProfileSpecifier;

        public PbxprojGenerator(Project Project, List<ProjectReference> ProjectReferences, PathString InputDirectory, PathString OutputDirectory, String PbxprojTemplateText, OperatingSystemType HostOperatingSystem, ArchitectureType HostArchitecture, OperatingSystemType TargetOperatingSystem, ArchitectureType TargetArchitecture, CppLibraryForm CppLibraryForm, String DevelopmentTeam = null, String ProvisioningProfileSpecifier = null)
        {
            this.Project = Project;
            this.ProjectReferences = ProjectReferences;
            this.InputDirectory = InputDirectory.FullPath;
            this.OutputDirectory = OutputDirectory.FullPath;
            this.PbxprojTemplateText = PbxprojTemplateText;
            this.HostOperatingSystem = HostOperatingSystem;
            this.HostArchitecture = HostArchitecture;
            this.TargetOperatingSystem = TargetOperatingSystem;
            this.TargetArchitecture = TargetArchitecture;
            this.CppLibraryForm = CppLibraryForm;
            this.DevelopmentTeam = DevelopmentTeam;
            this.ProvisioningProfileSpecifier = ProvisioningProfileSpecifier;
        }

        public void Generate(bool ForceRegenerate)
        {
            var PbxprojPath = OutputDirectory / (Project.Name + ".xcodeproj") / "project.pbxproj";
            var BaseDirPath = PbxprojPath.Parent.Parent;

            var ProductName = !String.IsNullOrEmpty(Project.TargetName) ? Project.TargetName : Project.Name;

            var p = Plist.FromString(PbxprojTemplateText);

            var Objects = p.Dict["objects"].Dict;
            var RootObjectKey = p.Dict["rootObject"].String;
            var RootObject = Objects[RootObjectKey].Dict;
            ObjectReferenceValidityTest(Objects, RootObjectKey);

            RemoveFiles(Objects, RootObject["mainGroup"].String);

            var Targets = RootObject["targets"].Array;
            foreach (var TargetKey in Targets)
            {
                foreach (var PhaseKey in Objects[TargetKey.String].Dict["buildPhases"].Array)
                {
                    var Phase = Objects[PhaseKey.String].Dict;
                    var Type = Phase["isa"].String;
                    if (Type == "PBXSourcesBuildPhase")
                    {
                        var Files = Phase["files"];
                        var ToBeRemoved = new HashSet<String>();
                        foreach (var FileKey in Files.Array)
                        {
                            var File = Objects[FileKey.String].Dict;
                            var FileType = File["isa"].String;
                            if (FileType == "PBXBuildFile")
                            {
                                var FileRef = File["fileRef"].String;
                                if (!Objects.ContainsKey(FileRef))
                                {
                                    ToBeRemoved.Add(FileKey.String);
                                    Objects.Remove(FileKey.String);
                                }
                            }
                        }
                        if (ToBeRemoved.Count > 0)
                        {
                            Files.Array = Files.Array.Where(FileKey => !ToBeRemoved.Contains(FileKey.String)).ToList();
                        }
                    }
                }
            }

            ObjectReferenceValidityTest(Objects, RootObjectKey);

            var RelativePathToObjects = new Dictionary<String, String>();
            foreach (var conf in Project.Configurations.Matches(Project.TargetType, HostOperatingSystem, HostArchitecture, TargetOperatingSystem, TargetArchitecture, null, ToolchainType.XCode, CompilerType.clang, CLibraryType.libSystem, CLibraryForm.Dynamic, CppLibraryType.libcxx, CppLibraryForm, null))
            {
                foreach (var f in conf.Files)
                {
                    var VirtualPath = PathString.Join(f.Path.RelativeTo(InputDirectory).Parts.Where(v => v != ".."));
                    if (RelativePathToObjects.ContainsKey(f.Path.RelativeTo(OutputDirectory).ToString(PathStringStyle.Unix))) { continue; }
                    var Added = AddFile(Objects, RootObject["mainGroup"].String, "", OutputDirectory, OutputDirectory, VirtualPath, f.Path, f, RelativePathToObjects);
                    if (!Added)
                    {
                        throw new InvalidOperationException();
                    }
                }
            }

            var ProjectDependencies = new List<Value>();
            foreach (var Project in ProjectReferences)
            {
                var VirtualPath = ("Frameworks/" + Project.Name).AsPath();
                AddProjectReference(Objects, RootObject["mainGroup"].String, "", VirtualPath, Project, RelativePathToObjects);

                if (this.Project.TargetType == TargetType.DarwinStaticFramework)
                {
                    var TargetFileProxyHash = GetHashOfPath("PBXReferenceProxy:PBXContainerItemProxy:" + Project.Name);
                    var TargetFileProxy = new Dictionary<String, Value>();
                    TargetFileProxy["isa"] = Value.CreateString("PBXContainerItemProxy");
                    TargetFileProxy["containerPortal"] = Value.CreateString(GetHashOfPath(VirtualPath.ToString(PathStringStyle.Unix)));
                    TargetFileProxy["proxyType"] = Value.CreateString("2");
                    TargetFileProxy["remoteGlobalIDString"] = Value.CreateString(Objects[Targets.First().String].Dict["productReference"].String);
                    TargetFileProxy["remoteInfo"] = Value.CreateString(Project.Name);
                    Objects.Add(TargetFileProxyHash, Value.CreateDict(TargetFileProxy));

                    var FileHash = GetHashOfPath("PBXReferenceProxy:" + VirtualPath.ToString(PathStringStyle.Unix));
                    var FileObject = new Dictionary<string, Value>();
                    FileObject.Add("isa", Value.CreateString("PBXReferenceProxy"));
                    if (Project.TargetType == TargetType.StaticLibrary)
                    {
                        FileObject.Add("fileType", Value.CreateString("archive.ar"));
                        FileObject.Add("path", Value.CreateString("lib" + Project.TargetName + ".a"));
                    }
                    else if (Project.TargetType == TargetType.DynamicLibrary)
                    {
                        FileObject.Add("explicitFileType", Value.CreateString("compiled.mach-o.dylib"));
                        FileObject.Add("path", Value.CreateString("lib" + Project.TargetName + ".dylib"));
                    }
                    else if ((Project.TargetType == TargetType.DarwinStaticFramework) || (Project.TargetType == TargetType.DarwinSharedFramework))
                    {
                        FileObject.Add("explicitFileType", Value.CreateString("wrapper.framework"));
                        FileObject.Add("path", Value.CreateString(Project.TargetName + ".framework"));
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                    FileObject.Add("remoteRef", Value.CreateString(TargetFileProxyHash));
                    FileObject.Add("sourceTree", Value.CreateString("BUILT_PRODUCTS_DIR"));
                    Objects.Add(FileHash, Value.CreateDict(FileObject));

                    var ProductsHash = GetHashOfPath("Products:" + Project.Name);
                    var Products = new Dictionary<String, Value>();
                    Products["isa"] = Value.CreateString("PBXGroup");
                    Products["children"] = Value.CreateArray(new List<Value> { Value.CreateString(FileHash) });
                    Products["name"] = Value.CreateString("Products");
                    Products["sourceTree"] = Value.CreateString("<group>");
                    Objects.Add(ProductsHash, Value.CreateDict(Products));

                    ProjectDependencies.Add(Value.CreateDict(new Dictionary<String, Value>
                    {
                        ["ProductGroup"] = Value.CreateString(ProductsHash),
                        ["ProjectRef"] = Value.CreateString(GetHashOfPath(VirtualPath.ToString(PathStringStyle.Unix)))
                    }));
                }
            }
            RootObject["projectReferences"] = Value.CreateArray(ProjectDependencies);

            foreach (var TargetKey in Targets)
            {
                var Target = Objects[TargetKey.String].Dict;
                var TargetName = Target["name"].String;

                foreach (var BuildConfigurationKey in Objects[Target["buildConfigurationList"].String].Dict["buildConfigurations"].Array)
                {
                    var BuildConfiguration = Objects[BuildConfigurationKey.String].Dict;
                    var ConfigurationType = (ConfigurationType)(Enum.Parse(typeof(ConfigurationType), BuildConfiguration["name"].String));
                    var BuildSettings = BuildConfiguration["buildSettings"].Dict;
                    var conf = Project.Configurations.Merged(Project.TargetType, HostOperatingSystem, HostArchitecture, TargetOperatingSystem, TargetArchitecture, null, ToolchainType.XCode, CompilerType.clang, CLibraryType.libSystem, CLibraryForm.Dynamic, CppLibraryType.libcxx, CppLibraryForm, ConfigurationType);

                    BuildSettings["PRODUCT_NAME"] = Value.CreateString(ProductName);
                    if ((Project.TargetType == TargetType.DarwinApplication) || (Project.TargetType == TargetType.DarwinStaticFramework) || (Project.TargetType == TargetType.DarwinSharedFramework) || (Project.TargetType == TargetType.MacBundle))
                    {
                        //bundle and frameworks don't need to be signed https://stackoverflow.com/questions/30963294/creating-ios-osx-frameworks-is-it-necessary-to-codesign-them-before-distributin
                        if (Project.TargetType == TargetType.DarwinApplication)
                        {
                            if (String.IsNullOrEmpty(ProvisioningProfileSpecifier))
                            {
                                BuildSettings["CODE_SIGN_STYLE"] = Value.CreateString("Automatic");
                                BuildSettings["DEVELOPMENT_TEAM"] = Value.CreateString(DevelopmentTeam);
                                BuildSettings["PROVISIONING_PROFILE_SPECIFIER"] = Value.CreateString("");
                            }
                            else
                            {
                                BuildSettings["CODE_SIGN_STYLE"] = Value.CreateString("Manual");
                                BuildSettings["DEVELOPMENT_TEAM"] = Value.CreateString(DevelopmentTeam);
                                BuildSettings["PROVISIONING_PROFILE_SPECIFIER"] = Value.CreateString(ProvisioningProfileSpecifier);
                            }
                        }
                        else
                        {
                            BuildSettings["CODE_SIGN_STYLE"] = Value.CreateString("Automatic");
                            BuildSettings["DEVELOPMENT_TEAM"] = Value.CreateString("");
                            BuildSettings["PROVISIONING_PROFILE_SPECIFIER"] = Value.CreateString("");
                        }
                    }
                    else
                    {
                        BuildSettings["CODE_SIGN_IDENTITY"] = Value.CreateString("");
                        BuildSettings["CODE_SIGN_STYLE"] = Value.CreateString("Automatic");
                        BuildSettings["DEVELOPMENT_TEAM"] = Value.CreateString("");
                        BuildSettings["PROVISIONING_PROFILE_SPECIFIER"] = Value.CreateString("");
                    }
                    if ((ConfigurationType == ConfigurationType.Release) && ((Project.TargetType == TargetType.DynamicLibrary) || (Project.TargetType == TargetType.DarwinApplication) || (Project.TargetType == TargetType.DarwinStaticFramework) || (Project.TargetType == TargetType.DarwinSharedFramework) || (Project.TargetType == TargetType.MacBundle)))
                    {
                        BuildSettings["DEPLOYMENT_POSTPROCESSING"] = Value.CreateString("YES");
                        BuildSettings["COPY_PHASE_STRIP"] = Value.CreateString("YES");
                        BuildSettings["STRIP_STYLE"] = Value.CreateString("non-global");
                    }
                    if ((Project.TargetType == TargetType.DarwinApplication) || (Project.TargetType == TargetType.DarwinStaticFramework) || (Project.TargetType == TargetType.DarwinSharedFramework) || (Project.TargetType == TargetType.MacBundle))
                    {
                        var InfoPlistPath = InputDirectory / "Info.plist";
                        if (System.IO.File.Exists(InfoPlistPath))
                        {
                            BuildSettings["INFOPLIST_FILE"] = Value.CreateString(InfoPlistPath.RelativeTo(BaseDirPath).ToString(PathStringStyle.Unix));
                        }
                    }
                    if (Project.TargetType == TargetType.DynamicLibrary)
                    {
                        BuildSettings["EXECUTABLE_PREFIX"] = Value.CreateString("lib");
                    }
                    if ((Project.TargetType == TargetType.DynamicLibrary) || (Project.TargetType == TargetType.MacBundle) || (Project.TargetType == TargetType.DarwinSharedFramework))
                    {
                        BuildSettings["DYLIB_INSTALL_NAME_BASE"] = Value.CreateString("@rpath");
                        BuildSettings["SKIP_INSTALL"] = Value.CreateString("YES");
                    }
                    if (Project.TargetType == TargetType.DarwinSharedFramework)
                    {
                        BuildSettings["DYLIB_COMPATIBILITY_VERSION"] = Value.CreateString("1");
                        BuildSettings["DYLIB_CURRENT_VERSION"] = Value.CreateString("1");
                        BuildSettings["INSTALL_PATH"] = Value.CreateString("$(LOCAL_LIBRARY_DIR)/Frameworks");
                        if (TargetOperatingSystem == OperatingSystemType.MacOS)
                        {
                            BuildSettings["LD_RUNPATH_SEARCH_PATHS"] = Value.CreateString("@executable_path/../Frameworks @loader_path/Frameworks");
                        }
                        else
                        {
                            BuildSettings["LD_RUNPATH_SEARCH_PATHS"] = Value.CreateString("@executable_path/Frameworks @loader_path/Frameworks");
                        }
                    }
                    if (Project.TargetType == TargetType.DarwinStaticFramework)
                    {
                        BuildSettings["MACH_O_TYPE"] = Value.CreateString("staticlib");
                        BuildSettings["GENERATE_MASTER_OBJECT_FILE"] = Value.CreateString("YES");

                        var LinkerFlags = new List<String> { "-L\"$BUILT_PRODUCTS_DIR\"" }.Concat(conf.LibDirectories.Select(d => "-L" + d.FullPath.RelativeTo(BaseDirPath).ToString(PathStringStyle.Unix))).Concat(ProjectReferences.Select(r => "-l" + r.Name)).Concat(conf.Libs.Select(Lib => Lib.Parts.Count == 1 ? "-l" + Lib.ToString(PathStringStyle.Unix) : Lib.RelativeTo(BaseDirPath).ToString(PathStringStyle.Unix))).ToList();
                        BuildSettings["PRELINK_FLAGS"] = Value.CreateString(String.Join(" ", LinkerFlags));
                    }
                    else if (Project.TargetType == TargetType.DarwinSharedFramework)
                    {
                        BuildSettings["MACH_O_TYPE"] = Value.CreateString("mh_dylib");
                    }
                    if (TargetOperatingSystem == OperatingSystemType.iOS)
                    {
                        if ((Project.TargetType == TargetType.DynamicLibrary) || (Project.TargetType == TargetType.DarwinApplication) || (Project.TargetType == TargetType.DarwinStaticFramework) || (Project.TargetType == TargetType.DarwinSharedFramework))
                        {
                            BuildSettings["TARGETED_DEVICE_FAMILY"] = Value.CreateString("1,2");
                        }
                    }

                    String OutputDir;
                    if (conf.OutputDirectory != null)
                    {
                        OutputDir = ("$(SRCROOT)".AsPath() / conf.OutputDirectory.RelativeTo(BaseDirPath)).ToString(PathStringStyle.Unix);
                    }
                    else
                    {
                        OutputDir = $"$(SRCROOT)/../{ConfigurationType}$(EFFECTIVE_PLATFORM_NAME)";
                    }
                    BuildSettings["TARGET_BUILD_DIR"] = Value.CreateString(OutputDir);
                    BuildSettings["DWARF_DSYM_FOLDER_PATH"] = Value.CreateString(OutputDir);

                    foreach (var o in conf.Options)
                    {
                        var Prefix = "xcode.target.";
                        if (o.Key.StartsWith(Prefix))
                        {
                            BuildSettings[o.Key.Substring(Prefix.Length)] = Value.CreateString(o.Value);
                        }
                    }
                }

                var confF = Project.Configurations.Merged(Project.TargetType, HostOperatingSystem, HostArchitecture, TargetOperatingSystem, TargetArchitecture, null, ToolchainType.XCode, CompilerType.clang, CLibraryType.libSystem, CLibraryForm.Dynamic, CppLibraryType.libcxx, CppLibraryForm, null);
                foreach (var PhaseKey in Target["buildPhases"].Array)
                {
                    var Phase = Objects[PhaseKey.String].Dict;
                    var Type = Phase["isa"].String;
                    if (Type == "PBXSourcesBuildPhase")
                    {
                        var Files = Phase["files"];
                        foreach (var f in confF.Files)
                        {
                            if ((f.Type == FileType.CSource) || (f.Type == FileType.CppSource) || (f.Type == FileType.ObjectiveCSource) || (f.Type == FileType.ObjectiveCppSource))
                            {
                                var RelativePath = f.Path.FullPath.RelativeTo(OutputDirectory).ToString(PathStringStyle.Unix);
                                var File = new Dictionary<String, Value>();
                                File.Add("fileRef", Value.CreateString(RelativePathToObjects[RelativePath]));
                                File.Add("isa", Value.CreateString("PBXBuildFile"));

                                var FileConf = f.Configurations.Merged(Project.TargetType, HostOperatingSystem, HostArchitecture, TargetOperatingSystem, TargetArchitecture, null, ToolchainType.XCode, CompilerType.clang, CLibraryType.libSystem, CLibraryForm.Dynamic, CppLibraryType.libcxx, CppLibraryForm, null);

                                var FileFlags = FileConf.CommonFlags;
                                if ((f.Type == FileType.CSource) || (f.Type == FileType.ObjectiveCSource))
                                {
                                    FileFlags = FileFlags.Concat(FileConf.CFlags).ToList();
                                }
                                else if ((f.Type == FileType.CppSource) || (f.Type == FileType.ObjectiveCppSource))
                                {
                                    FileFlags = FileFlags.Concat(FileConf.CppFlags).ToList();
                                }
                                FileFlags = FileFlags.Concat(FileConf.Defines.Select(d => d.Value == null ? d.Key : Regex.IsMatch(d.Value, @"^[A-Za-z0-9]+$") ? "-D" + d.Key + "=" + d.Value : "'-D" + d.Key + "=" + d.Value + "'")).ToList();

                                if (FileFlags.Count > 0)
                                {
                                    File.Add("settings", Value.CreateDict(new Dictionary<String, Value> { ["COMPILER_FLAGS"] = Value.CreateString(String.Join(" ", FileFlags)) }));
                                }

                                foreach (var o in FileConf.Options)
                                {
                                    var Prefix = "xcode.buildFile.";
                                    if (o.Key.StartsWith(Prefix))
                                    {
                                        File[o.Key.Substring(Prefix.Length)] = Value.CreateString(o.Value);
                                    }
                                }

                                var Hash = GetHashOfPath(TargetName + ":PBXSourcesBuildPhase:" + RelativePath);
                                Objects.Add(Hash, Value.CreateDict(File));
                                Files.Array.Add(Value.CreateString(Hash));
                            }
                        }
                    }
                    else if (Type == "PBXFrameworksBuildPhase")
                    {
                        if ((Project.TargetType == TargetType.Executable) || (Project.TargetType == TargetType.DynamicLibrary) || (Project.TargetType == TargetType.DarwinApplication) || (Project.TargetType == TargetType.DarwinSharedFramework) || (Project.TargetType == TargetType.MacBundle))
                        {
                            var Files = Phase["files"];
                            foreach (var Project in ProjectReferences)
                            {
                                var RelativePath = "Frameworks/" + Project.Name;
                                var File = new Dictionary<String, Value>();
                                File.Add("fileRef", Value.CreateString(RelativePathToObjects[RelativePath]));
                                File.Add("isa", Value.CreateString("PBXBuildFile"));
                                var Hash = GetHashOfPath(TargetName + ":PBXFrameworksBuildPhase:" + RelativePath);
                                Objects.Add(Hash, Value.CreateDict(File));
                                Files.Array.Add(Value.CreateString(Hash));
                            }
                        }
                    }
                    else if (Type == "PBXHeadersBuildPhase")
                    {
                        var Files = Phase["files"];
                        foreach (var f in confF.Files)
                        {
                            if ((f.Type == FileType.Header) && f.IsExported)
                            {
                                var RelativePath = f.Path.FullPath.RelativeTo(OutputDirectory).ToString(PathStringStyle.Unix);
                                var File = new Dictionary<String, Value>();
                                File.Add("fileRef", Value.CreateString(RelativePathToObjects[RelativePath]));
                                File.Add("isa", Value.CreateString("PBXBuildFile"));
                                File.Add("settings", Value.CreateDict(new Dictionary<String, Value> { ["ATTRIBUTES"] = Value.CreateArray(new List<Value> { Value.CreateString("Public") }) }));
                                var Hash = GetHashOfPath(TargetName + ":PBXHeadersBuildPhase:" + RelativePath);
                                Objects.Add(Hash, Value.CreateDict(File));
                                Files.Array.Add(Value.CreateString(Hash));
                            }
                        }
                    }
                    else if (Type == "PBXResourcesBuildPhase")
                    {
                        var Files = Phase["files"];
                        foreach (var f in confF.Files)
                        {
                            if (f.Type == FileType.EmbeddedContent)
                            {
                                var RelativePath = f.Path.FullPath.RelativeTo(OutputDirectory).ToString(PathStringStyle.Unix);
                                var File = new Dictionary<String, Value>();
                                File.Add("fileRef", Value.CreateString(RelativePathToObjects[RelativePath]));
                                File.Add("isa", Value.CreateString("PBXBuildFile"));
                                var Hash = GetHashOfPath(TargetName + ":PBXResourcesBuildPhase:" + RelativePath);
                                Objects.Add(Hash, Value.CreateDict(File));
                                Files.Array.Add(Value.CreateString(Hash));
                            }
                        }
                    }
                }

                var TargetDependencies = new List<Value>();
                foreach (var Project in ProjectReferences)
                {
                    if (this.Project.TargetType == TargetType.DarwinStaticFramework)
                    {
                        var VirtualPath = ("Frameworks/" + Project.Name).AsPath();

                        var TargetProxyHash = GetHashOfPath(Target + ":PBXTargetDependency:PBXContainerItemProxy:" + Project.Name);
                        var TargetProxy = new Dictionary<String, Value>();
                        TargetProxy["isa"] = Value.CreateString("PBXContainerItemProxy");
                        TargetProxy["containerPortal"] = Value.CreateString(GetHashOfPath(VirtualPath.ToString(PathStringStyle.Unix)));
                        TargetProxy["proxyType"] = Value.CreateString("1");
                        TargetProxy["remoteGlobalIDString"] = Value.CreateString(Targets.First().String);
                        TargetProxy["remoteInfo"] = Value.CreateString(Project.Name);
                        Objects.Add(TargetProxyHash, Value.CreateDict(TargetProxy));

                        var TargetDependencyHash = GetHashOfPath(Target + ":PBXTargetDependency:" + Project.Name);
                        TargetDependencies.Add(Value.CreateString(TargetDependencyHash));
                        var TargetDependency = new Dictionary<String, Value>();
                        TargetDependency["isa"] = Value.CreateString("PBXTargetDependency");
                        TargetDependency["name"] = Value.CreateString(Project.Name);
                        TargetDependency["targetProxy"] = Value.CreateString(TargetProxyHash);
                        Objects.Add(TargetDependencyHash, Value.CreateDict(TargetDependency));
                    }
                }
                Target["dependencies"] = Value.CreateArray(TargetDependencies);

                Target["name"] = Value.CreateString(Project.Name);
                Target["productName"] = Value.CreateString(ProductName);
                var TargetFile = Objects[Target["productReference"].String];

                if (Project.TargetType == TargetType.Executable)
                {
                    Target["productType"] = Value.CreateString("com.apple.product-type.tool");
                    TargetFile.Dict["explicitFileType"] = Value.CreateString("compiled.mach-o.executable");
                    TargetFile.Dict["path"] = Value.CreateString(ProductName);
                }
                else if (Project.TargetType == TargetType.StaticLibrary)
                {
                    Target["productType"] = Value.CreateString("com.apple.product-type.library.static");
                    TargetFile.Dict["explicitFileType"] = Value.CreateString("archive.ar");
                    TargetFile.Dict["path"] = Value.CreateString("lib" + ProductName + ".a");
                }
                else if (Project.TargetType == TargetType.DynamicLibrary)
                {
                    Target["productType"] = Value.CreateString("com.apple.product-type.library.dynamic");
                    TargetFile.Dict["explicitFileType"] = Value.CreateString("compiled.mach-o.dylib");
                    TargetFile.Dict["path"] = Value.CreateString("lib" + ProductName + ".dylib");
                }
                else if (Project.TargetType == TargetType.DarwinApplication)
                {
                    Target["productType"] = Value.CreateString("com.apple.product-type.application");
                    TargetFile.Dict["explicitFileType"] = Value.CreateString("wrapper.application");
                    TargetFile.Dict["path"] = Value.CreateString(ProductName + ".app");
                }
                else if (Project.TargetType == TargetType.DarwinStaticFramework)
                {
                    Target["productType"] = Value.CreateString("com.apple.product-type.framework");
                    TargetFile.Dict["explicitFileType"] = Value.CreateString("wrapper.framework");
                    TargetFile.Dict["path"] = Value.CreateString(ProductName + ".framework");
                }
                else if (Project.TargetType == TargetType.DarwinSharedFramework)
                {
                    Target["productType"] = Value.CreateString("com.apple.product-type.framework");
                    TargetFile.Dict["explicitFileType"] = Value.CreateString("wrapper.framework");
                    TargetFile.Dict["path"] = Value.CreateString(ProductName + ".framework");
                }
                else if (Project.TargetType == TargetType.MacBundle)
                {
                    Target["productType"] = Value.CreateString("com.apple.product-type.bundle");
                    TargetFile.Dict["explicitFileType"] = Value.CreateString("wrapper.cfbundle");
                    TargetFile.Dict["path"] = Value.CreateString(ProductName + ".bundle");
                }
                else
                {
                    throw new NotSupportedException("NotSupportedTargetType: " + Project.TargetType.ToString());
                }
            }

            foreach (var BuildConfigurationKey in Objects[RootObject["buildConfigurationList"].String].Dict["buildConfigurations"].Array)
            {
                var BuildConfiguration = Objects[BuildConfigurationKey.String].Dict;
                var ConfigurationType = (ConfigurationType)(Enum.Parse(typeof(ConfigurationType), BuildConfiguration["name"].String));
                var BuildSettings = BuildConfiguration["buildSettings"].Dict;

                var conf = Project.Configurations.Merged(Project.TargetType, HostOperatingSystem, HostArchitecture, TargetOperatingSystem, TargetArchitecture, null, ToolchainType.XCode, CompilerType.clang, CLibraryType.libSystem, CLibraryForm.Dynamic, CppLibraryType.libcxx, CppLibraryForm, ConfigurationType);

                var IncludeDirectories = conf.IncludeDirectories.Select(d => d.FullPath.RelativeTo(BaseDirPath).ToString(PathStringStyle.Unix)).ToList();
                if (IncludeDirectories.Count != 0)
                {
                    BuildSettings["HEADER_SEARCH_PATHS"] = Value.CreateArray(IncludeDirectories.Select(d => Value.CreateString(d)).ToList());
                }
                var Defines = conf.Defines;
                if (Defines.Count != 0)
                {
                    BuildSettings["GCC_PREPROCESSOR_DEFINITIONS"] = Value.CreateArray(Defines.Select(d => d.Value == null ? d.Key : Regex.IsMatch(d.Value, @"^[A-Za-z0-9]+$") ? d.Key + "=" + d.Value : "'" + d.Key + "=" + d.Value + "'").Select(d => Value.CreateString(d)).ToList());
                }
                var CFlags = conf.CommonFlags.Concat(conf.CFlags).ToList();
                if (CFlags.Count != 0)
                {
                    BuildSettings["OTHER_CFLAGS"] = Value.CreateArray(CFlags.Select(d => Value.CreateString(d)).ToList());
                }
                var CppFlags = conf.CommonFlags.Concat(conf.CppFlags).ToList();
                if (CppFlags.Count != 0)
                {
                    BuildSettings["OTHER_CPLUSPLUSFLAGS"] = Value.CreateArray(CppFlags.Select(d => Value.CreateString(d)).ToList());
                }

                if ((Project.TargetType == TargetType.Executable) || (Project.TargetType == TargetType.DynamicLibrary) || (Project.TargetType == TargetType.DarwinApplication) || (Project.TargetType == TargetType.DarwinSharedFramework) || (Project.TargetType == TargetType.MacBundle))
                {
                    var LibDirectories = conf.LibDirectories.Select(d => d.FullPath.RelativeTo(BaseDirPath).ToString(PathStringStyle.Unix)).ToList();
                    foreach (var Project in ProjectReferences)
                    {
                        if (Project.TargetType == TargetType.DynamicLibrary)
                        {
                            if (Project.OutputFilePath.ContainsKey(ConfigurationType))
                            {
                                var LibPath = Project.OutputFilePath[ConfigurationType];
                                LibDirectories.Add(LibPath.Parent.FullPath.RelativeTo(BaseDirPath).ToString(PathStringStyle.Unix));
                            }
                        }
                    }
                    LibDirectories = LibDirectories.Distinct().ToList();
                    if (LibDirectories.Count != 0)
                    {
                        BuildSettings["LIBRARY_SEARCH_PATHS"] = Value.CreateArray(LibDirectories.Select(d => Value.CreateString(d)).ToList());
                    }

                    var FrameworkDirectories = new List<String>();
                    foreach (var Project in ProjectReferences)
                    {
                        if ((Project.TargetType == TargetType.DarwinStaticFramework) || (Project.TargetType == TargetType.DarwinSharedFramework))
                        {
                            if (Project.OutputFilePath.ContainsKey(ConfigurationType))
                            {
                                var FrameworkPath = Project.OutputFilePath[ConfigurationType];
                                FrameworkDirectories.Add(FrameworkPath.Parent.FullPath.RelativeTo(BaseDirPath).ToString(PathStringStyle.Unix));
                            }
                        }
                    }
                    FrameworkDirectories = FrameworkDirectories.Distinct().ToList();
                    if (FrameworkDirectories.Count != 0)
                    {
                        BuildSettings["FRAMEWORK_SEARCH_PATHS"] = Value.CreateArray(FrameworkDirectories.Select(d => Value.CreateString(d)).ToList());
                    }

                    var LinkerFlags = conf.LinkerFlags.Concat(conf.Libs.Select(Lib => Lib.Parts.Count == 1 ? Lib.Extension == "" ? "-l" + Lib.ToString(PathStringStyle.Unix) : "-l:" + Lib.ToString(PathStringStyle.Unix) : Lib.RelativeTo(BaseDirPath).ToString(PathStringStyle.Unix))).Concat(conf.PostLinkerFlags).ToList();
                    if (LinkerFlags.Count != 0)
                    {
                        BuildSettings["OTHER_LDFLAGS"] = Value.CreateArray(LinkerFlags.Select(d => Value.CreateString(d)).ToList());
                    }
                }

                if (TargetOperatingSystem == OperatingSystemType.MacOS)
                {
                    BuildSettings["SDKROOT"] = Value.CreateString("macosx");
                }
                else if (TargetOperatingSystem == OperatingSystemType.iOS)
                {
                    BuildSettings["SDKROOT"] = Value.CreateString("iphoneos");
                }
                else
                {
                    throw new NotSupportedException("NotSupportedTargetOperatingSystem: " + TargetOperatingSystem.ToString());
                }

                BuildSettings["CONFIGURATION_TEMP_DIR"] = Value.CreateString($"$(SRCROOT)/../{ConfigurationType}$(EFFECTIVE_PLATFORM_NAME)");
                BuildSettings["CONFIGURATION_BUILD_DIR"] = Value.CreateString($"$(SRCROOT)/../{ConfigurationType}$(EFFECTIVE_PLATFORM_NAME)");

                BuildSettings["ARCHS"] = Value.CreateString(GetArchitectureString(TargetArchitecture));
                BuildSettings["VALID_ARCHS"] = Value.CreateString(GetArchitectureString(TargetArchitecture));

                foreach (var o in conf.Options)
                {
                    var Prefix = "xcode.project.";
                    if (o.Key.StartsWith(Prefix))
                    {
                        BuildSettings[o.Key.Substring(Prefix.Length)] = Value.CreateString(o.Value);
                    }
                }
            }

            ObjectReferenceValidityTest(Objects, RootObjectKey);
            TextFile.WriteToFile(PbxprojPath, Plist.ToString(p), new UTF8Encoding(false), !ForceRegenerate);
        }

        private bool AddFile(Dictionary<String, Value> Objects, String GroupKey, PathString ParentGroupVirtualPath, PathString ParentGroupPhysicalPath, PathString RootGroupPhysicalPath, PathString FileVirtualPath, PathString FilePhysicalPath, File File, Dictionary<String, String> RelativePathToFileObjectKey, bool Top = true)
        {
            var GroupOrFile = Objects[GroupKey].Dict;
            var Type = GroupOrFile["isa"].String;
            if (Type != "PBXGroup") { return false; }
            var GroupVirtualPath = ParentGroupVirtualPath / (GroupOrFile.ContainsKey("name") ? GroupOrFile["name"].String : GroupOrFile.ContainsKey("path") ? GroupOrFile["path"].String : "");
            var GroupPhysicalPath = ParentGroupPhysicalPath / (GroupOrFile.ContainsKey("path") ? GroupOrFile["path"].String : "");
            var Children = GroupOrFile["children"];
            if (!FileVirtualPath.In(GroupVirtualPath)) { return false; }
            var Added = false;
            foreach (var Child in Children.Array)
            {
                Added = AddFile(Objects, Child.String, GroupVirtualPath, GroupPhysicalPath, RootGroupPhysicalPath, FileVirtualPath, FilePhysicalPath, File, RelativePathToFileObjectKey, false);
                if (Added)
                {
                    break;
                }
            }
            if (Added) { return true; }
            if (!Top && (GroupVirtualPath == ParentGroupVirtualPath)) { return false; }

            var RelativePath = FileVirtualPath.RelativeTo(GroupVirtualPath);
            var RelativePathParts = RelativePath.Parts;

            if (RelativePathParts.Count == 0)
            {
                throw new InvalidOperationException();
            }
            else if (RelativePathParts.Count == 1)
            {
                var FileName = FileVirtualPath.FileName;
                var Hash = GetHashOfPath(FilePhysicalPath.RelativeTo(RootGroupPhysicalPath).ToString(PathStringStyle.Unix));

                var FileObject = new Dictionary<String, Value>();
                FileObject.Add("fileEncoding", Value.CreateInteger(4));
                FileObject.Add("isa", Value.CreateString("PBXFileReference"));
                string LastKnownFileType = "";
                if (File.Type == FileType.Header)
                {
                    if (FileName.EndsWith(".hpp", StringComparison.OrdinalIgnoreCase) || FileName.EndsWith(".hh", StringComparison.OrdinalIgnoreCase) || FileName.EndsWith(".hxx", StringComparison.OrdinalIgnoreCase))
                    {
                        LastKnownFileType = "sourcecode.c.h";
                    }
                    else
                    {
                        LastKnownFileType = "sourcecode.cpp.h";
                    }
                }
                else if (File.Type == FileType.CSource)
                {
                    LastKnownFileType = "sourcecode.c.c";
                }
                else if (File.Type == FileType.CppSource)
                {
                    LastKnownFileType = "sourcecode.cpp.cpp";
                }
                else if (File.Type == FileType.ObjectiveCSource)
                {
                    LastKnownFileType = "sourcecode.c.objc";
                }
                else if (File.Type == FileType.ObjectiveCppSource)
                {
                    LastKnownFileType = "sourcecode.cpp.objcpp";
                }
                else if ((File.Type == FileType.Unknown) || (File.Type == FileType.EmbeddedContent))
                {
                    if (FileName.EndsWith(".plist", StringComparison.OrdinalIgnoreCase))
                    {
                        LastKnownFileType = "text.plist.xml";
                    }
                    else if (FileName.EndsWith(".storyboard", StringComparison.OrdinalIgnoreCase))
                    {
                        LastKnownFileType = "file.storyboard";
                    }
                    else if (FileName.EndsWith(".xib", StringComparison.OrdinalIgnoreCase))
                    {
                        LastKnownFileType = "file.xib";
                    }
                    else if (FileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || FileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || FileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) || FileName.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) || FileName.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase))
                    {
                        LastKnownFileType = "image";
                    }
                    else if (FileName.EndsWith(".xcassets", StringComparison.OrdinalIgnoreCase))
                    {
                        LastKnownFileType = "folder.assetcatalog";
                    }
                }
                if (LastKnownFileType != "")
                {
                    FileObject.Add("lastKnownFileType", Value.CreateString(LastKnownFileType));
                }
                else if (File.Type == FileType.EmbeddedContent)
                {
                    FileObject.Add("explicitFileType", Value.CreateString("sourcecode"));
                }
                if (FilePhysicalPath != GroupPhysicalPath / FileName)
                {
                    FileObject.Add("name", Value.CreateString(FileName));
                    FileObject.Add("path", Value.CreateString(FilePhysicalPath.RelativeTo(GroupPhysicalPath).ToString(PathStringStyle.Unix)));
                }
                else
                {
                    FileObject.Add("path", Value.CreateString(FileName));
                }
                FileObject.Add("sourceTree", Value.CreateString("<group>"));
                Objects.Add(Hash, Value.CreateDict(FileObject));
                RelativePathToFileObjectKey.Add(FilePhysicalPath.RelativeTo(RootGroupPhysicalPath).ToString(PathStringStyle.Unix), Hash);

                Children.Array.Add(Value.CreateString(Hash));

                return true;
            }
            else
            {
                var pp = FilePhysicalPath.GetAccestor(RelativePathParts.Count - 1);
                var VirtualDirName = RelativePathParts[0];
                var PhysicalDirName = pp.FileName;
                var Hash = GetHashOfPath(pp.RelativeTo(RootGroupPhysicalPath).ToString(PathStringStyle.Unix));

                var GroupObject = new Dictionary<String, Value>();
                GroupObject.Add("children", Value.CreateArray(new List<Value> { }));
                GroupObject.Add("isa", Value.CreateString("PBXGroup"));
                if (pp != GroupPhysicalPath / VirtualDirName)
                {
                    GroupObject.Add("name", Value.CreateString(VirtualDirName));
                    GroupObject.Add("path", Value.CreateString(pp.RelativeTo(GroupPhysicalPath).ToString(PathStringStyle.Unix)));
                }
                else
                {
                    GroupObject.Add("path", Value.CreateString(VirtualDirName));
                }
                GroupObject.Add("sourceTree", Value.CreateString("<group>"));
                Objects.Add(Hash, Value.CreateDict(GroupObject));

                Children.Array.Add(Value.CreateString(Hash));

                return AddFile(Objects, Hash, GroupVirtualPath, GroupPhysicalPath, RootGroupPhysicalPath, FileVirtualPath, FilePhysicalPath, File, RelativePathToFileObjectKey, false);
            }
        }

        private bool AddProjectReference(Dictionary<String, Value> Objects, String GroupKey, PathString ParentGroupVirtualPath, PathString ProjectVirtualPath, ProjectReference Project, Dictionary<String, String> RelativePathToFileObjectKey, bool Top = true)
        {
            var GroupOrFile = Objects[GroupKey].Dict;
            var Type = GroupOrFile["isa"].String;
            if (Type != "PBXGroup") { return false; }
            var GroupVirtualPath = ParentGroupVirtualPath / (GroupOrFile.ContainsKey("name") ? GroupOrFile["name"].String : GroupOrFile.ContainsKey("path") ? GroupOrFile["path"].String : "");
            var Children = GroupOrFile["children"];
            if (!ProjectVirtualPath.In(GroupVirtualPath)) { return false; }
            var Added = false;
            foreach (var Child in Children.Array)
            {
                Added = AddProjectReference(Objects, Child.String, GroupVirtualPath, ProjectVirtualPath, Project, RelativePathToFileObjectKey, false);
                if (Added)
                {
                    break;
                }
            }
            if (Added) { return true; }
            if (!Top && (GroupVirtualPath == ParentGroupVirtualPath)) { return false; }

            var RelativePath = ProjectVirtualPath.RelativeTo(GroupVirtualPath);
            var RelativePathParts = RelativePath.Parts;

            if (RelativePathParts.Count == 0)
            {
                throw new InvalidOperationException();
            }
            else if (RelativePathParts.Count == 1)
            {
                var FileName = ProjectVirtualPath.FileName;
                var Hash = GetHashOfPath(ProjectVirtualPath.ToString(PathStringStyle.Unix));

                var FileObject = new Dictionary<string, Value>();
                if (this.Project.TargetType == TargetType.DarwinStaticFramework)
                {
                    FileObject.Add("isa", Value.CreateString("PBXFileReference"));
                    FileObject.Add("explicitFileType", Value.CreateString("wrapper.pb-project"));
                    FileObject.Add("path", Value.CreateString(Project.Name + ".xcodeproj"));
                    FileObject.Add("sourceTree", Value.CreateString("<group>"));
                }
                else
                {
                    FileObject.Add("isa", Value.CreateString("PBXFileReference"));
                    if (Project.TargetType == TargetType.StaticLibrary)
                    {
                        FileObject.Add("fileType", Value.CreateString("archive.ar"));
                        FileObject.Add("path", Value.CreateString("lib" + Project.TargetName + ".a"));
                    }
                    else if (Project.TargetType == TargetType.DynamicLibrary)
                    {
                        FileObject.Add("explicitFileType", Value.CreateString("compiled.mach-o.dylib"));
                        FileObject.Add("path", Value.CreateString("lib" + Project.TargetName + ".dylib"));
                    }
                    else if ((Project.TargetType == TargetType.DarwinStaticFramework) || (Project.TargetType == TargetType.DarwinSharedFramework))
                    {
                        FileObject.Add("explicitFileType", Value.CreateString("wrapper.framework"));
                        FileObject.Add("path", Value.CreateString(Project.TargetName + ".framework"));
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                    FileObject.Add("sourceTree", Value.CreateString("BUILT_PRODUCTS_DIR"));
                }
                Objects.Add(Hash, Value.CreateDict(FileObject));
                RelativePathToFileObjectKey.Add(ProjectVirtualPath.ToString(PathStringStyle.Unix), Hash);

                Children.Array.Add(Value.CreateString(Hash));

                return true;
            }
            else
            {
                var VirtualDirName = RelativePathParts[0];
                var vp = GroupVirtualPath / VirtualDirName;
                var Hash = GetHashOfPath(vp.ToString(PathStringStyle.Unix));

                var GroupObject = new Dictionary<String, Value>();
                GroupObject.Add("children", Value.CreateArray(new List<Value> { }));
                GroupObject.Add("isa", Value.CreateString("PBXGroup"));
                GroupObject.Add("name", Value.CreateString(VirtualDirName));
                GroupObject.Add("sourceTree", Value.CreateString("<group>"));
                Objects.Add(Hash, Value.CreateDict(GroupObject));

                Children.Array.Add(Value.CreateString(Hash));

                return AddProjectReference(Objects, Hash, GroupVirtualPath, ProjectVirtualPath, Project, RelativePathToFileObjectKey, false);
            }
        }

        private static void RemoveFiles(Dictionary<String, Value> Objects, String GroupOrFileKey, bool Top = true)
        {
            var GroupOrFile = Objects[GroupOrFileKey].Dict;
            var Type = GroupOrFile["isa"].String;
            if (Type == "PBXGroup")
            {
                var Children = GroupOrFile["children"];
                foreach (var Child in Children.Array)
                {
                    RemoveFiles(Objects, Child.String, false);
                }
                Children.Array = Children.Array.Where(Child => Objects.ContainsKey(Child.String)).ToList();
                if (Children.Array.Count == 0)
                {
                    if (Top) { return; }
                    Objects.Remove(GroupOrFileKey);
                }
            }
            else if (Type == "PBXFileReference")
            {
                if (GroupOrFile.ContainsKey("explicitFileType")) { return; }
                Objects.Remove(GroupOrFileKey);
            }
        }
        private String GetHashOfPath(String Path)
        {
            return Hash.GetHashForPath(Project.Name + "/" + Path, 24);
        }

        public static String GetArchitectureString(ArchitectureType Architecture)
        {
            if (Architecture == ArchitectureType.x86)
            {
                return "i386";
            }
            else if (Architecture == ArchitectureType.x64)
            {
                return "x86_64";
            }
            else if (Architecture == ArchitectureType.armv7a)
            {
                return "armv7";
            }
            else if (Architecture == ArchitectureType.arm64)
            {
                return "arm64";
            }
            else
            {
                throw new NotSupportedException("NotSupportedArchitecture: " + Architecture.ToString());
            }
        }
    }
}
