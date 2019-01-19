using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TypeMake
{
    public class CMakeSolutionGenerator
    {
        private String SolutionName;
        private List<ProjectReference> ProjectReferences;
        private PathString OutputDirectory;

        public CMakeSolutionGenerator(String SolutionName, List<ProjectReference> ProjectReferences, PathString OutputDirectory)
        {
            this.SolutionName = SolutionName;
            this.ProjectReferences = ProjectReferences;
            this.OutputDirectory = OutputDirectory.FullPath;
        }

        public void Generate(bool ForceRegenerate)
        {
            var CMakeListsPath = OutputDirectory / "CMakeLists.txt";

            var Lines = GenerateLines(CMakeListsPath).ToList();
            TextFile.WriteToFile(CMakeListsPath, String.Join("\n", Lines), new UTF8Encoding(false), !ForceRegenerate);
        }

        private IEnumerable<String> GenerateLines(String CMakeListsPath)
        {
            yield return @"cmake_minimum_required(VERSION 3.0.2)";
            yield return $@"project({SolutionName})";
            foreach (var p in ProjectReferences)
            {
                yield return @"add_subdirectory(" + p.FilePath.FullPath.RelativeTo(OutputDirectory).ToString(PathStringStyle.Unix) + ")";
            }
            yield return "";
        }
    }
}
