using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using static TypeMake.Plist;

namespace TypeMake
{
    public class XcworkspaceGenerator
    {
        private String SolutionName;
        private List<ProjectReference> ProjectReferences;
        private PathString OutputDirectory;

        public XcworkspaceGenerator(String SolutionName, List<ProjectReference> ProjectReferences, PathString OutputDirectory)
        {
            this.SolutionName = SolutionName;
            this.ProjectReferences = ProjectReferences;
            this.OutputDirectory = OutputDirectory.FullPath;
        }

        public void Generate(bool ForceRegenerate)
        {
            var XcworkspacedataPath = OutputDirectory / (SolutionName + ".xcworkspace") / "contents.xcworkspacedata";
            var BaseDirPath = XcworkspacedataPath.Parent.Parent;

            var x = new XElement("Workspace", new XAttribute("version", "1.0"));

            var RelativePathToGroups = new Dictionary<PathString, XElement>();
            foreach (var Project in ProjectReferences)
            {
                var VirtualDirParts = Project.VirtualDir.Parts;
                var xCurrentGroup = x;
                var CurrentPath = "".AsPath();
                foreach (var Part in VirtualDirParts)
                {
                    var ParentPath = CurrentPath;
                    CurrentPath = ParentPath / Part;
                    if (RelativePathToGroups.ContainsKey(CurrentPath))
                    {
                        xCurrentGroup = RelativePathToGroups[CurrentPath];
                    }
                    else
                    {
                        xCurrentGroup = new XElement("Group", new XAttribute("location", "group:" + CurrentPath == "." ? "" : CurrentPath.ToString(PathStringStyle.Unix)), new XAttribute("name", Part));
                        RelativePathToGroups.Add(CurrentPath, xCurrentGroup);
                        x.Add(xCurrentGroup);
                    }
                }
                xCurrentGroup.Add(new XElement("FileRef", new XAttribute("location", "group:" + Project.FilePath.RelativeTo(BaseDirPath).ToString(PathStringStyle.Unix))));
            }

            var xFile = XmlFile.ToString(x);
            TextFile.WriteToFile(XcworkspacedataPath, xFile, Encoding.UTF8, !ForceRegenerate);
        }
    }
}
