using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static TypeMake.Plist;

namespace TypeMake
{
    public class PbxprojSolutionGenerator
    {
        private String SolutionName;
        private List<ProjectReference> ProjectReferences;
        private PathString OutputDirectory;
        private String PbxprojTemplateText;

        public PbxprojSolutionGenerator(String SolutionName, List<ProjectReference> ProjectReferences, PathString OutputDirectory, String PbxprojTemplateText)
        {
            this.SolutionName = SolutionName;
            this.ProjectReferences = ProjectReferences;
            this.OutputDirectory = OutputDirectory.FullPath;
            this.PbxprojTemplateText = PbxprojTemplateText;
        }

        public void Generate(bool ForceRegenerate)
        {
            var PbxprojPath = OutputDirectory / (SolutionName + ".xcodeproj") / "project.pbxproj";
            var BaseDirPath = PbxprojPath.Parent.Parent;

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
                    Objects.Remove(PhaseKey.String);
                }
                var ListKey = Objects[TargetKey.String].Dict["buildConfigurationList"];
                foreach (var BuildConfigurationKey in Objects[ListKey.String].Dict["buildConfigurations"].Array)
                {
                    Objects.Remove(BuildConfigurationKey.String);
                }
                Objects.Remove(ListKey.String);
                Objects.Remove(TargetKey.String);
            }

            RootObject["attributes"].Dict["TargetAttributes"].Dict.Clear();
            RootObject["targets"].Array.Clear();
            RootObject["productRefGroup"] = RootObject["mainGroup"];

            ObjectReferenceValidityTest(Objects, RootObjectKey);

            var RelativePathToObjects = new Dictionary<String, String>();
            foreach (var Project in ProjectReferences)
            {
                var RelativePath = Project.VirtualDir / Project.Name;
                var Added = AddProject(Objects, RootObject["mainGroup"].String, new LinkedList<String>(), new LinkedList<String>(RelativePath.Parts.Where(pp => pp != ".")), Project, BaseDirPath, RelativePathToObjects);
                if (!Added)
                {
                    throw new InvalidOperationException();
                }
            }

            ObjectReferenceValidityTest(Objects, RootObjectKey);
            TextFile.WriteToFile(PbxprojPath, Plist.ToString(p), new UTF8Encoding(false), !ForceRegenerate);
        }

        private static bool AddProject(Dictionary<String, Value> Objects, String GroupOrFileKey, LinkedList<String> Stack, LinkedList<String> RelativePathStack, ProjectReference Project, String BaseDirPath, Dictionary<String, String> RelativePathToFileObjectKey, bool Top = true)
        {
            var GroupOrFile = Objects[GroupOrFileKey].Dict;
            var Type = GroupOrFile["isa"].String;
            if (Type != "PBXGroup") { return false; }
            var PathOrName = GroupOrFile.ContainsKey("path") ? GroupOrFile["path"].String : GroupOrFile.ContainsKey("name") ? GroupOrFile["name"].String : "";
            var Parts = PathOrName.AsPath().Parts.Where(p => p != ".").ToArray();
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
                Added = AddProject(Objects, Child.String, Stack, RelativePathStack, Project, BaseDirPath, RelativePathToFileObjectKey, false);
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
                FileObject.Add("fileEncoding", Value.CreateInteger(4));
                FileObject.Add("isa", Value.CreateString("PBXFileReference"));
                FileObject.Add("lastKnownFileType", Value.CreateString("wrapper.pb-project"));
                FileObject.Add("name", Value.CreateString(FileName));
                FileObject.Add("path", Value.CreateString(Project.FilePath.FullPath.RelativeTo(BaseDirPath).ToString(PathStringStyle.Unix)));
                FileObject.Add("sourceTree", Value.CreateString("<group>"));
                Objects.Add(Hash, Value.CreateDict(FileObject));
                RelativePathToFileObjectKey.Add(RelativePath, Hash);

                ChildHash = Hash;
            }

            for (int k = RelativePathStack.Count - 1; k >= Parts.Length + 1; k -= 1)
            {
                var RelativePath = PathString.Join(Stack.Concat(RelativePathStack.Take(k)).Where(p => p != "")).ToString(PathStringStyle.Unix);
                var DirName = RelativePath.AsPath().FileName;
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
                Objects.Remove(GroupOrFileKey);
            }
        }
        private static String GetHashOfPath(String Path)
        {
            return Hash.GetHashForPath(Path, 24);
        }
    }
}
