using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using static TypeMake.Plist;

namespace TypeMake.Cpp
{
    public class PbxprojGenerator
    {
        private Project Project;
        private List<ProjectReference> ProjectReferences;
        private String InputDirectory;
        private String OutputDirectory;
        private String PbxprojTemplateText;
        private OperatingSystemType BuildingOperatingSystem;
        private OperatingSystemType TargetOperatingSystem;

        public PbxprojGenerator(Project Project, List<ProjectReference> ProjectReferences, String InputDirectory, String OutputDirectory, String PbxprojTemplateText, OperatingSystemType BuildingOperatingSystem, OperatingSystemType TargetOperatingSystem)
        {
            this.Project = Project;
            this.ProjectReferences = ProjectReferences;
            this.InputDirectory = Path.GetFullPath(InputDirectory);
            this.OutputDirectory = Path.GetFullPath(OutputDirectory);
            this.PbxprojTemplateText = PbxprojTemplateText;
            this.BuildingOperatingSystem = BuildingOperatingSystem;
            this.TargetOperatingSystem = TargetOperatingSystem;
        }

        public void Generate(bool ForceRegenerate)
        {
            var PbxprojPath = Path.Combine(OutputDirectory, Path.Combine(Project.Name + ".xcodeproj", "project.pbxproj"));
            var BaseDirPath = Path.GetDirectoryName(Path.GetDirectoryName(PbxprojPath));

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
            foreach (var conf in Project.Configurations)
            {
                foreach (var f in conf.Files)
                {
                    var PathParts = GetPathParts(FileNameHandling.GetRelativePath(Path.GetFullPath(f.Path), InputDirectory).Replace('\\', '/'));
                    PathParts[0] = Path.Combine(FileNameHandling.GetRelativePath(InputDirectory, OutputDirectory), PathParts[0]).Replace('\\', '/');
                    var RelativePath = String.Join("/", PathParts);
                    if (RelativePathToObjects.ContainsKey(RelativePath)) { continue; }
                    var Added = AddFile(Objects, RootObject["mainGroup"].String, new LinkedList<String>(), new LinkedList<String>(PathParts), f, BaseDirPath, RelativePathToObjects);
                    if (!Added)
                    {
                        throw new InvalidOperationException();
                    }
                }
            }

            foreach (var Project in ProjectReferences)
            {
                var RelativePath = "Frameworks/" + Project.Name;
                AddProjectReference(Objects, RootObject["mainGroup"].String, new LinkedList<String>(), new LinkedList<String>(GetPathParts(RelativePath)), Project, BaseDirPath, RelativePathToObjects);
            }

            foreach (var TargetKey in Targets)
            {
                var conf = ConfigurationUtils.GetMergedConfiguration(ToolchainType.Mac_XCode, CompilerType.clang, BuildingOperatingSystem, TargetOperatingSystem, null, null, Project.Configurations);

                var Target = Objects[TargetKey.String].Dict;
                var TargetName = Target["name"].String;
                foreach (var PhaseKey in Target["buildPhases"].Array)
                {
                    var Phase = Objects[PhaseKey.String].Dict;
                    var Type = Phase["isa"].String;
                    if (Type == "PBXSourcesBuildPhase")
                    {
                        var Files = Phase["files"];
                        foreach (var f in conf.Files)
                        {
                            if ((f.Type == FileType.CSource) || (f.Type == FileType.CppSource) || (f.Type == FileType.ObjectiveCSource) || (f.Type == FileType.ObjectiveCppSource))
                            {
                                var PathParts = GetPathParts(FileNameHandling.GetRelativePath(Path.GetFullPath(f.Path), InputDirectory).Replace('\\', '/'));
                                PathParts[0] = Path.Combine(FileNameHandling.GetRelativePath(InputDirectory, OutputDirectory), PathParts[0]).Replace('\\', '/');
                                var RelativePath = String.Join("/", PathParts);
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
                Target["name"] = Value.CreateString(Project.Name);
                var ProductName = !String.IsNullOrEmpty(Project.TargetName) ? Project.TargetName : Project.Name;
                Target["productName"] = Value.CreateString(ProductName);
                var TargetFile = Objects[Target["productReference"].String];

                if (conf.TargetType == TargetType.Executable)
                {
                    if (TargetOperatingSystem == OperatingSystemType.Mac)
                    {
                        Target["productType"] = Value.CreateString("com.apple.product-type.tool");
                    }
                    else if (TargetOperatingSystem == OperatingSystemType.iOS)
                    {
                        Target["productType"] = Value.CreateString("com.apple.product-type.application");
                    }
                    else
                    {
                        throw new NotSupportedException("NotSupportedTargetOperatingSystem: " + TargetOperatingSystem.ToString());
                    }
                    TargetFile.Dict["explicitFileType"] = Value.CreateString("compiled.mach-o.executable");
                    TargetFile.Dict["path"] = Value.CreateString(ProductName);
                }
                else if (conf.TargetType == TargetType.StaticLibrary)
                {
                    Target["productType"] = Value.CreateString("com.apple.product-type.library.static");
                    TargetFile.Dict["explicitFileType"] = Value.CreateString("archive.ar");
                    TargetFile.Dict["path"] = Value.CreateString("lib" + ProductName + ".a");
                }
                else if (conf.TargetType == TargetType.DynamicLibrary)
                {
                    Target["productType"] = Value.CreateString("com.apple.product-type.library.dynamic");
                    TargetFile.Dict["explicitFileType"] = Value.CreateString("compiled.mach-o.dylib");
                    TargetFile.Dict["path"] = Value.CreateString("lib" + ProductName + ".dylib");
                }
                else
                {
                    throw new NotSupportedException("NotSupportedTargetType: " + conf.TargetType.ToString());
                }
            }

            foreach (var BuildConfigurationKey in Objects[RootObject["buildConfigurationList"].String].Dict["buildConfigurations"].Array)
            {
                var BuildConfiguration = Objects[BuildConfigurationKey.String].Dict;
                var ConfigurationType = (ConfigurationType)(Enum.Parse(typeof(ConfigurationType), BuildConfiguration["name"].String));
                var BuildSettings = BuildConfiguration["buildSettings"].Dict;

                var conf = ConfigurationUtils.GetMergedConfiguration(ToolchainType.Mac_XCode, CompilerType.clang, BuildingOperatingSystem, TargetOperatingSystem, ConfigurationType, null, Project.Configurations);

                var IncludeDirectories = conf.IncludeDirectories.Select(d => FileNameHandling.GetRelativePath(Path.GetFullPath(d), BaseDirPath).Replace('\\', '/')).ToList();
                if (IncludeDirectories.Count != 0)
                {
                    BuildSettings.SetItem("HEADER_SEARCH_PATHS", Value.CreateArray(IncludeDirectories.Concat(new List<String> { "$(inherited)" }).Select(d => Value.CreateString(d)).ToList()));
                }
                var Defines = conf.Defines;
                if (Defines.Count != 0)
                {
                    BuildSettings.SetItem("GCC_PREPROCESSOR_DEFINITIONS", Value.CreateArray(Defines.Select(d => d.Key + (d.Value == null ? "" : "=" + d.Value)).Concat(new List<String> { "$(inherited)" }).Select(d => Value.CreateString(d)).ToList()));
                }
                var CFlags = conf.CFlags;
                if (CFlags.Count != 0)
                {
                    BuildSettings.SetItem("OTHER_CFLAGS", Value.CreateArray(CFlags.Concat(new List<String> { "$(inherited)" }).Select(d => Value.CreateString(d)).ToList()));
                }
                var CppFlags = conf.CFlags.Concat(conf.CppFlags).ToList();
                if (CppFlags.Count != 0)
                {
                    BuildSettings.SetItem("OTHER_CPLUSPLUSFLAGS", Value.CreateArray(CppFlags.Concat(new List<String> { "$(inherited)" }).Select(d => Value.CreateString(d)).ToList()));
                }

                if ((conf.TargetType == TargetType.Executable) || (conf.TargetType == TargetType.DynamicLibrary))
                {
                    var LibDirectories = conf.LibDirectories.Select(d => FileNameHandling.GetRelativePath(Path.GetFullPath(d), BaseDirPath).Replace('\\', '/')).ToList();
                    if (LibDirectories.Count != 0)
                    {
                        BuildSettings.SetItem("LIBRARY_SEARCH_PATHS", Value.CreateArray(LibDirectories.Concat(new List<String> { "$(inherited)" }).Select(d => Value.CreateString(d)).ToList()));
                    }
                    var LinkerFlags = conf.Libs.Concat(conf.LinkerFlags).ToList();
                    if (LinkerFlags.Count != 0)
                    {
                        BuildSettings.SetItem("OTHER_LDFLAGS", Value.CreateArray(CppFlags.Concat(new List<String> { "$(inherited)" }).Select(d => Value.CreateString(d)).ToList()));
                    }
                }

                if (TargetOperatingSystem == OperatingSystemType.Mac)
                {
                    BuildSettings.SetItem("SDKROOT", Value.CreateString("macosx"));
                }
                else if (TargetOperatingSystem == OperatingSystemType.iOS)
                {
                    BuildSettings.SetItem("SDKROOT", Value.CreateString("iphoneos"));
                    if (conf.TargetType == TargetType.Executable)
                    {
                        BuildSettings.SetItem("INFOPLIST_FILE", Value.CreateString(FileNameHandling.GetRelativePath(Path.Combine(InputDirectory, "Info.plist"), BaseDirPath).Replace('\\', '/')));
                        BuildSettings.SetItem("PRODUCT_BUNDLE_IDENTIFIER", Value.CreateString(conf.BundleIdentifier));
                    }
                }
                else
                {
                    throw new NotSupportedException("NotSupportedTargetOperatingSystem: " + TargetOperatingSystem.ToString());
                }
            }

            ObjectReferenceValidityTest(Objects, RootObjectKey);
            TextFile.WriteToFile(PbxprojPath, Plist.ToString(p), new UTF8Encoding(false), !ForceRegenerate);
        }

        private static bool AddFile(Dictionary<String, Value> Objects, String GroupOrFileKey, LinkedList<String> Stack, LinkedList<String> RelativePathStack, File File, String BaseDirPath, Dictionary<String, String> RelativePathToFileObjectKey, bool Top = true)
        {
            var GroupOrFile = Objects[GroupOrFileKey].Dict;
            var Type = GroupOrFile["isa"].String;
            if (Type != "PBXGroup") { return false; }
            var Path = GroupOrFile.ContainsKey("path") ? GroupOrFile["path"].String : "";
            var Parts = GetPathParts(Path).Where(p => p != "").ToArray();
            if (Parts.Length > RelativePathStack.Count) { return false; }
            if (!Parts.SequenceEqual(RelativePathStack.Take(Parts.Length))) { return false; }
            foreach (var Part in Parts)
            {
                Stack.AddLast(Part);
            }
            for (int k = 0; k < Parts.Length; k += 1)
            {
                RelativePathStack.RemoveFirst();
            }
            var Children = GroupOrFile["children"];
            var Added = false;
            foreach (var Child in Children.Array)
            {
                Added = AddFile(Objects, Child.String, Stack, RelativePathStack, File, BaseDirPath, RelativePathToFileObjectKey, false);
                if (Added)
                {
                    break;
                }
            }
            foreach (var Part in Parts.Reverse())
            {
                RelativePathStack.AddFirst(Part);
            }
            for (int k = 0; k < Parts.Length; k += 1)
            {
                Stack.RemoveLast();
            }
            if (Added) { return true; }

            if (!Top && (Parts.Length == 0)) { return false; }

            String ChildHash;
            {
                var RelativePath = String.Join("/", Stack.Concat(RelativePathStack).Where(p => p != ""));
                var FileName = System.IO.Path.GetFileName(RelativePathStack.Last.Value);
                var Hash = GetHashOfPath(RelativePath);

                var FileObject = new Dictionary<string, Value>();
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
                if (RelativePathStack.Count - 1 < Parts.Length + 1)
                {
                    FileObject.Add("name", Value.CreateString(FileName));
                    FileObject.Add("path", Value.CreateString(RelativePath));
                }
                else
                {
                    FileObject.Add("path", Value.CreateString(FileName));
                }
                FileObject.Add("sourceTree", Value.CreateString("<group>"));
                Objects.Add(Hash, Value.CreateDict(FileObject));
                RelativePathToFileObjectKey.Add(RelativePath, Hash);

                ChildHash = Hash;
            }

            for (int k = RelativePathStack.Count - 1; k >= Parts.Length + 1; k -= 1)
            {
                var RelativePath = String.Join("/", Stack.Concat(RelativePathStack.Take(k)).Where(p => p != ""));
                var DirName = System.IO.Path.GetFileName(RelativePath);
                var Hash = GetHashOfPath(RelativePath);

                var FileObject = new Dictionary<string, Value>();
                FileObject.Add("children", Value.CreateArray(new List<Value> { Value.CreateString(ChildHash) }));
                FileObject.Add("isa", Value.CreateString("PBXGroup"));
                if (k == Parts.Length + 1)
                {
                    FileObject.Add("name", Value.CreateString(DirName));
                    FileObject.Add("path", Value.CreateString(RelativePath));
                }
                else
                {
                    FileObject.Add("path", Value.CreateString(DirName));
                }
                FileObject.Add("sourceTree", Value.CreateString("<group>"));
                Objects.Add(Hash, Value.CreateDict(FileObject));

                ChildHash = Hash;
            }

            Children.Array.Add(Value.CreateString(ChildHash));

            return true;
        }

        private static bool AddProjectReference(Dictionary<String, Value> Objects, String GroupOrFileKey, LinkedList<String> Stack, LinkedList<String> RelativePathStack, ProjectReference Project, String BaseDirPath, Dictionary<String, String> RelativePathToFileObjectKey, bool Top = true)
        {
            var GroupOrFile = Objects[GroupOrFileKey].Dict;
            var Type = GroupOrFile["isa"].String;
            if (Type != "PBXGroup") { return false; }
            var Path = GroupOrFile.ContainsKey("path") ? GroupOrFile["path"].String : GroupOrFile.ContainsKey("name") ? GroupOrFile["name"].String : "";
            var Parts = GetPathParts(Path).Where(p => p != "").ToArray();
            if (Parts.Length > RelativePathStack.Count) { return false; }
            if (!Parts.SequenceEqual(RelativePathStack.Take(Parts.Length))) { return false; }
            foreach (var Part in Parts)
            {
                Stack.AddLast(Part);
            }
            for (int k = 0; k < Parts.Length; k += 1)
            {
                RelativePathStack.RemoveFirst();
            }
            var Children = GroupOrFile["children"];
            var Added = false;
            foreach (var Child in Children.Array)
            {
                Added = AddProjectReference(Objects, Child.String, Stack, RelativePathStack, Project, BaseDirPath, RelativePathToFileObjectKey, false);
                if (Added)
                {
                    break;
                }
            }
            foreach (var Part in Parts.Reverse())
            {
                RelativePathStack.AddFirst(Part);
            }
            for (int k = 0; k < Parts.Length; k += 1)
            {
                Stack.RemoveLast();
            }
            if (Added) { return true; }

            if (!Top && (Parts.Length == 0)) { return false; }

            String ChildHash;
            {
                var RelativePath = String.Join("/", Stack.Concat(RelativePathStack).Where(p => p != ""));
                var FileName = RelativePathStack.Last.Value;
                var Hash = GetHashOfPath(RelativePath);

                var FileObject = new Dictionary<string, Value>();
                FileObject.Add("isa", Value.CreateString("PBXFileReference"));
                FileObject.Add("explicitFileType", Value.CreateString("archive.ar"));
                FileObject.Add("path", Value.CreateString("lib" + Project.Name + ".a"));
                FileObject.Add("sourceTree", Value.CreateString("BUILT_PRODUCTS_DIR"));
                Objects.Add(Hash, Value.CreateDict(FileObject));
                RelativePathToFileObjectKey.Add(RelativePath, Hash);

                ChildHash = Hash;
            }

            for (int k = RelativePathStack.Count - 1; k >= Parts.Length + 1; k -= 1)
            {
                var RelativePath = String.Join("/", Stack.Concat(RelativePathStack.Take(k)).Where(p => p != ""));
                var DirName = System.IO.Path.GetFileName(RelativePath);
                var Hash = GetHashOfPath(RelativePath);

                var FileObject = new Dictionary<string, Value>();
                FileObject.Add("children", Value.CreateArray(new List<Value> { Value.CreateString(ChildHash) }));
                FileObject.Add("isa", Value.CreateString("PBXGroup"));
                FileObject.Add("name", Value.CreateString(DirName));
                FileObject.Add("sourceTree", Value.CreateString("<group>"));
                Objects.Add(Hash, Value.CreateDict(FileObject));

                ChildHash = Hash;
            }

            Children.Array.Add(Value.CreateString(ChildHash));

            return true;
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
        private static String[] GetPathParts(String Path)
        {
            var Parts = Path.TrimEnd('/', '\\').Split('/', '\\');
            return Parts;
        }
        private static String GetHashOfPath(String Path)
        {
            return Hash.GetHashForPath(Path, 24);
        }
    }
}
