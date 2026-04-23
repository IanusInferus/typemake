using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace TypeMake.Cpp
{
    public class SlnxGenerator
    {
        private String SolutionName;
        private String SolutionId;
        private List<ProjectReference> ProjectReferences;
        private PathString OutputDirectory;
        private OperatingSystemType TargetOperatingSystem;
        private ArchitectureType TargetArchitecture;

        public SlnxGenerator(String SolutionName, String SolutionId, List<ProjectReference> ProjectReferences, PathString OutputDirectory, OperatingSystemType TargetOperatingSystem, ArchitectureType TargetArchitecture)
        {
            this.SolutionName = SolutionName;
            this.SolutionId = SolutionId;
            this.ProjectReferences = ProjectReferences;
            this.OutputDirectory = OutputDirectory.FullPath;
            this.TargetOperatingSystem = TargetOperatingSystem;
            this.TargetArchitecture = TargetArchitecture;
        }

        public void Generate(bool ForceRegenerate)
        {
            var xSolution = new XElement("Solution");

            var xConfigurations = new XElement("Configurations");
            xConfigurations.Add(new XElement("Platform", new XAttribute("Name", GetPlatformString(TargetArchitecture))));
            xSolution.Add(xConfigurations);

            var xProjects = new List<(String Folder, XElement x)>();
            foreach (var Project in ProjectReferences)
            {
                var Dir = Project.VirtualDir.ToString(PathStringStyle.Unix);
                if (Dir == ".")
                {
                    Dir = "/";
                }
                else
                {
                    Dir = "/" + Dir + "/";
                }
                var FilePath = Project.FilePath.FullPath.RelativeTo(OutputDirectory).ToString(PathStringStyle.Unix);
                xProjects.Add((Dir, new XElement("Project", new XAttribute("Path", FilePath), new XAttribute("Id", Project.Id.ToLowerInvariant()))));
            }

            var ProjectGroups = xProjects.GroupBy(p => p.Folder).ToDictionary(g => g.Key, g => g.Select(p => p.x).ToList());

            foreach (var g in ProjectGroups)
            {
                if (g.Key == "/") { continue; }
                var Dir = g.Key;
                xSolution.Add(new XElement("Folder", new XAttribute("Name", Dir), g.Value));
            }

            if (ProjectGroups.ContainsKey("/"))
            {
                foreach (var x in ProjectGroups["/"])
                {
                    xSolution.Add(x);
                }
            }

            var Text = xSolution.ToString();
            var FullPath = OutputDirectory / (SolutionName + ".slnx");
            TextFile.WriteToFile(FullPath, Text, new UTF8Encoding(false), !ForceRegenerate);
        }

        public static String GetPlatformString(ArchitectureType Architecture)
        {
            if (Architecture == ArchitectureType.x86)
            {
                return "x86";
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
    }
}
