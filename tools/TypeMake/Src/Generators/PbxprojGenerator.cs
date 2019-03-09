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
        private OperatingSystemType BuildingOperatingSystem;
        private ArchitectureType BuildingOperatingSystemArchitecture;
        private OperatingSystemType TargetOperatingSystem;
        private ArchitectureType TargetArchitectureType;
        private String DevelopmentTeam;

        public PbxprojGenerator(Project Project, List<ProjectReference> ProjectReferences, PathString InputDirectory, PathString OutputDirectory, String PbxprojTemplateText, OperatingSystemType BuildingOperatingSystem, ArchitectureType BuildingOperatingSystemArchitecture, OperatingSystemType TargetOperatingSystem, String DevelopmentTeam = null)
        {
            this.Project = Project;
            this.ProjectReferences = ProjectReferences;
            this.InputDirectory = InputDirectory.FullPath;
            this.OutputDirectory = OutputDirectory.FullPath;
            this.PbxprojTemplateText = PbxprojTemplateText;
            this.BuildingOperatingSystem = BuildingOperatingSystem;
            this.BuildingOperatingSystemArchitecture = BuildingOperatingSystemArchitecture;
            this.TargetOperatingSystem = TargetOperatingSystem;
            this.TargetArchitectureType = TargetOperatingSystem == OperatingSystemType.iOS ? ArchitectureType.arm64_v8a : ArchitectureType.x86_64; //TODO: need better handling
            this.DevelopmentTeam = DevelopmentTeam;
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
            foreach (var conf in Project.Configurations.Matches(Project.TargetType, ToolchainType.Mac_XCode, CompilerType.clang, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem, TargetArchitectureType, null))
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

            foreach (var Project in ProjectReferences)
            {
                var VirtualPath = ("Frameworks/" + Project.Name).AsPath();
                AddProjectReference(Objects, RootObject["mainGroup"].String, "", VirtualPath, Project, RelativePathToObjects);
            }

            foreach (var TargetKey in Targets)
            {
                var Target = Objects[TargetKey.String].Dict;
                var TargetName = Target["name"].String;

                foreach (var BuildConfigurationKey in Objects[Target["buildConfigurationList"].String].Dict["buildConfigurations"].Array)
                {
                    var BuildConfiguration = Objects[BuildConfigurationKey.String].Dict;
                    var ConfigurationType = (ConfigurationType)(Enum.Parse(typeof(ConfigurationType), BuildConfiguration["name"].String));
                    var BuildSettings = BuildConfiguration["buildSettings"].Dict;
                    var conf = Project.Configurations.Merged(Project.TargetType, ToolchainType.Mac_XCode, CompilerType.clang, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem, TargetArchitectureType, ConfigurationType);

                    BuildSettings["PRODUCT_NAME"] = Value.CreateString(ProductName);
                    if (TargetOperatingSystem == OperatingSystemType.Mac)
                    {
                        if (Project.TargetType == TargetType.DynamicLibrary)
                        {
                            BuildSettings["EXECUTABLE_PREFIX"] = Value.CreateString("lib");
                        }
                        if ((Project.TargetType == TargetType.DynamicLibrary) || (Project.TargetType == TargetType.MacBundle))
                        {
                            BuildSettings["DYLIB_INSTALL_NAME_BASE"] = Value.CreateString("@rpath");
                            BuildSettings["SKIP_INSTALL"] = Value.CreateString("YES");
                        }
                    }
                    else if (TargetOperatingSystem == OperatingSystemType.iOS)
                    {
                        if (DevelopmentTeam != null)
                        {
                            if ((Project.TargetType == TargetType.Executable) || (Project.TargetType == TargetType.DynamicLibrary) || (Project.TargetType == TargetType.iOSStaticFramework) || (Project.TargetType == TargetType.iOSSharedFramework))
                            {
                                BuildSettings["CODE_SIGN_IDENTITY"] = Value.CreateString("iPhone Developer");
                                BuildSettings["CODE_SIGN_STYLE"] = Value.CreateString("Automatic");
                                if (Project.TargetType == TargetType.Executable)
                                {
                                    BuildSettings["DEVELOPMENT_TEAM"] = Value.CreateString(DevelopmentTeam);
                                }
                                BuildSettings["PROVISIONING_PROFILE_SPECIFIER"] = Value.CreateString("");
                            }
                        }
                        if ((Project.TargetType == TargetType.DynamicLibrary) || (Project.TargetType == TargetType.iOSSharedFramework))
                        {
                            BuildSettings["DYLIB_COMPATIBILITY_VERSION"] = Value.CreateString("1");
                            BuildSettings["DYLIB_CURRENT_VERSION"] = Value.CreateString("1");
                            BuildSettings["DYLIB_INSTALL_NAME_BASE"] = Value.CreateString("@rpath");
                            BuildSettings["INSTALL_PATH"] = Value.CreateString("$(LOCAL_LIBRARY_DIR)/Frameworks");
                            BuildSettings["LD_RUNPATH_SEARCH_PATHS"] = Value.CreateString("$(inherited) @executable_path/Frameworks @loader_path/Frameworks");
                            BuildSettings["SKIP_INSTALL"] = Value.CreateString("YES");
                        }
                        if (Project.TargetType == TargetType.iOSStaticFramework)
                        {
                            BuildSettings["MACH_O_TYPE"] = Value.CreateString("staticlib");
                            BuildSettings["GENERATE_MASTER_OBJECT_FILE"] = Value.CreateString("YES");
                        }
                        else if (Project.TargetType == TargetType.iOSSharedFramework)
                        {
                            BuildSettings["MACH_O_TYPE"] = Value.CreateString("mh_dylib");
                        }
                        if ((Project.TargetType == TargetType.Executable) || (Project.TargetType == TargetType.DynamicLibrary) || (Project.TargetType == TargetType.iOSStaticFramework) || (Project.TargetType == TargetType.iOSSharedFramework))
                        {
                            BuildSettings["TARGETED_DEVICE_FAMILY"] = Value.CreateString("1,2");
                        }
                    }
                    if ((Project.TargetType == TargetType.Executable) || (Project.TargetType == TargetType.MacBundle) || (Project.TargetType == TargetType.iOSSharedFramework))
                    {
                        var InfoPlistPath = (InputDirectory / "Info.plist").RelativeTo(BaseDirPath);
                        if (System.IO.File.Exists(InfoPlistPath))
                        {
                            BuildSettings["INFOPLIST_FILE"] = Value.CreateString(InfoPlistPath.ToString(PathStringStyle.Unix));
                        }
                    }

                    if (conf.OutputDirectory != null)
                    {
                        BuildSettings["TARGET_BUILD_DIR"] = Value.CreateString(("$(SRCROOT)".AsPath() / conf.OutputDirectory.RelativeTo(BaseDirPath)).ToString(PathStringStyle.Unix));
                    }
                    else
                    {
                        BuildSettings["TARGET_BUILD_DIR"] = Value.CreateString($"$(SRCROOT)/../{ConfigurationType}$(EFFECTIVE_PLATFORM_NAME)");
                    }

                    foreach (var o in conf.Options)
                    {
                        var Prefix = "xcode.target.";
                        if (o.Key.StartsWith(Prefix))
                        {
                            BuildSettings[o.Key.Substring(Prefix.Length)] = Value.CreateString(o.Value);
                        }
                    }
                }

                var confF = Project.Configurations.Merged(Project.TargetType, ToolchainType.Mac_XCode, CompilerType.clang, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem, TargetArchitectureType, null);
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
                                var Hash = GetHashOfPath(TargetName + ":" + RelativePath);
                                Objects.Add(Hash, Value.CreateDict(File));
                                Files.Array.Add(Value.CreateString(Hash));
                            }
                        }
                    }
                    else if (Type == "PBXFrameworksBuildPhase")
                    {
                        if ((Project.TargetType == TargetType.Executable) || (Project.TargetType == TargetType.DynamicLibrary) || (Project.TargetType == TargetType.iOSStaticFramework) || (Project.TargetType == TargetType.iOSSharedFramework) || (Project.TargetType == TargetType.MacBundle))
                        {
                            var Files = Phase["files"];
                            foreach (var Project in ProjectReferences)
                            {
                                var RelativePath = "Frameworks/" + Project.Name;
                                var File = new Dictionary<String, Value>();
                                File.Add("fileRef", Value.CreateString(RelativePathToObjects[RelativePath]));
                                File.Add("isa", Value.CreateString("PBXBuildFile"));
                                var Hash = GetHashOfPath(TargetName + ":" + RelativePath);
                                Objects.Add(Hash, Value.CreateDict(File));
                                Files.Array.Add(Value.CreateString(Hash));
                            }
                        }
                    }
                }
                Target["name"] = Value.CreateString(Project.Name);
                Target["productName"] = Value.CreateString(ProductName);
                var TargetFile = Objects[Target["productReference"].String];

                if (Project.TargetType == TargetType.Executable)
                {
                    if (TargetOperatingSystem == OperatingSystemType.Mac)
                    {
                        Target["productType"] = Value.CreateString("com.apple.product-type.tool");
                        TargetFile.Dict["explicitFileType"] = Value.CreateString("compiled.mach-o.executable");
                        TargetFile.Dict["path"] = Value.CreateString(ProductName);
                    }
                    else if (TargetOperatingSystem == OperatingSystemType.iOS)
                    {
                        Target["productType"] = Value.CreateString("com.apple.product-type.application");
                        TargetFile.Dict["explicitFileType"] = Value.CreateString("wrapper.application");
                        TargetFile.Dict["path"] = Value.CreateString(ProductName + ".app");
                    }
                    else
                    {
                        throw new NotSupportedException("NotSupportedTargetOperatingSystem: " + TargetOperatingSystem.ToString());
                    }
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
                else if (Project.TargetType == TargetType.iOSStaticFramework)
                {
                    Target["productType"] = Value.CreateString("com.apple.product-type.framework");
                    TargetFile.Dict["explicitFileType"] = Value.CreateString("wrapper.framework");
                    TargetFile.Dict["path"] = Value.CreateString(ProductName + ".framework");
                }
                else if (Project.TargetType == TargetType.iOSSharedFramework)
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

                var conf = Project.Configurations.Merged(Project.TargetType, ToolchainType.Mac_XCode, CompilerType.clang, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem, TargetArchitectureType, ConfigurationType);

                var IncludeDirectories = conf.IncludeDirectories.Select(d => d.FullPath.RelativeTo(BaseDirPath).ToString(PathStringStyle.Unix)).ToList();
                if (IncludeDirectories.Count != 0)
                {
                    BuildSettings["HEADER_SEARCH_PATHS"] = Value.CreateArray(IncludeDirectories.Concat(new List<String> { "$(inherited)" }).Select(d => Value.CreateString(d)).ToList());
                }
                var Defines = conf.Defines;
                if (Defines.Count != 0)
                {
                    BuildSettings["GCC_PREPROCESSOR_DEFINITIONS"] = Value.CreateArray(Defines.Select(d => d.Value == null ? d.Key : Regex.IsMatch(d.Value, @"^[0-9]+$") ? d.Key + "=" + d.Value : "'" + d.Key + "=" + "\"" + d.Value.Replace("\"", "") + "\"'").Concat(new List<String> { "$(inherited)" }).Select(d => Value.CreateString(d)).ToList());
                }
                var CFlags = conf.CFlags;
                if (CFlags.Count != 0)
                {
                    BuildSettings["OTHER_CFLAGS"] = Value.CreateArray(CFlags.Concat(new List<String> { "$(inherited)" }).Select(d => Value.CreateString(d)).ToList());
                }
                var CppFlags = conf.CFlags.Concat(conf.CppFlags).ToList();
                if (CppFlags.Count != 0)
                {
                    BuildSettings["OTHER_CPLUSPLUSFLAGS"] = Value.CreateArray(CppFlags.Concat(new List<String> { "$(inherited)" }).Select(d => Value.CreateString(d)).ToList());
                }

                if ((Project.TargetType == TargetType.Executable) || (Project.TargetType == TargetType.DynamicLibrary) || (Project.TargetType == TargetType.iOSStaticFramework) || (Project.TargetType == TargetType.iOSSharedFramework) || (Project.TargetType == TargetType.MacBundle))
                {
                    var LibDirectories = conf.LibDirectories.Select(d => d.FullPath.RelativeTo(BaseDirPath).ToString(PathStringStyle.Unix)).ToList();
                    if (LibDirectories.Count != 0)
                    {
                        BuildSettings["LIBRARY_SEARCH_PATHS"] = Value.CreateArray(LibDirectories.Concat(new List<String> { "$(inherited)" }).Select(d => Value.CreateString(d)).ToList());
                    }
                    var LinkerFlags = conf.Libs.Select(lib => lib.ToString(PathStringStyle.Unix)).Concat(conf.LinkerFlags).ToList();
                    if (LinkerFlags.Count != 0)
                    {
                        BuildSettings["OTHER_LDFLAGS"] = Value.CreateArray(LinkerFlags.Concat(new List<String> { "$(inherited)" }).Select(d => Value.CreateString(d)).ToList());
                    }
                }

                if (TargetOperatingSystem == OperatingSystemType.Mac)
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
                else if ((File.Type == FileType.Unknown) && FileName.EndsWith("Info.plist", StringComparison.OrdinalIgnoreCase))
                {
                    LastKnownFileType = "text.plist.xml";
                }
                if (LastKnownFileType != "")
                {
                    FileObject.Add("lastKnownFileType", Value.CreateString(LastKnownFileType));
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
                FileObject.Add("isa", Value.CreateString("PBXFileReference"));
                FileObject.Add("explicitFileType", Value.CreateString("archive.ar"));
                FileObject.Add("path", Value.CreateString("lib" + Project.Name + ".a"));
                FileObject.Add("sourceTree", Value.CreateString("BUILT_PRODUCTS_DIR"));
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
    }
}
