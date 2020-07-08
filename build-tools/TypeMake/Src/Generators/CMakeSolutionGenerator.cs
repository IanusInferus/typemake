using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TypeMake.Cpp
{
    public class CMakeSolutionGenerator
    {
        private String SolutionName;
        private List<ProjectReference> ProjectReferences;
        private PathString OutputDirectory;
        private String CC;
        private String CXX;
        private String AR;
        private String STRIP;

        public CMakeSolutionGenerator(String SolutionName, List<ProjectReference> ProjectReferences, PathString OutputDirectory, String CC, String CXX, String AR, String STRIP)
        {
            this.SolutionName = SolutionName;
            this.ProjectReferences = ProjectReferences;
            this.OutputDirectory = OutputDirectory.FullPath;
            this.CC = CC;
            this.CXX = CXX;
            this.AR = AR;
            this.STRIP = STRIP;
        }

        public void Generate(bool ForceRegenerate)
        {
            var CMakeListsPath = OutputDirectory / "CMakeLists.txt";

            var Lines = GenerateLines(CMakeListsPath).ToList();
            TextFile.WriteToFile(CMakeListsPath, String.Join("\n", Lines), new UTF8Encoding(false), !ForceRegenerate);
        }

        private IEnumerable<String> GenerateLines(PathString CMakeListsPath)
        {
            yield return @"cmake_minimum_required(VERSION 3.3.2)";
            yield return $@"set(CMAKE_C_COMPILER {GetStringLiteral(CC)})";
            yield return $@"set(CMAKE_CXX_COMPILER {GetStringLiteral(CXX)})";
            yield return $@"set(CMAKE_AR {GetStringLiteral(AR)} CACHE FILEPATH ""Archiver"" FORCE)";
            yield return $@"set(CMAKE_STRIP {GetStringLiteral(STRIP)} CACHE FILEPATH ""Strip"" FORCE)";
            yield return $@"project({SolutionName})";
            foreach (var p in ProjectReferences)
            {
                yield return @"add_subdirectory(" + p.FilePath.FullPath.RelativeTo(OutputDirectory).ToString(PathStringStyle.Unix) + ")";
            }
            yield return "";
        }

        private static String GetStringLiteral(String s)
        {
            var d = new Dictionary<Char, String>
            {
                ['\\'] = @"\\",
                ['\t'] = @"\t",
                ['\r'] = @"\r",
                ['\n'] = @"\n",
                ['"'] = @"\""",
                ['$'] = @"\$"
            };
            return @"""" + String.Join("", s.SelectMany(c => d.ContainsKey(c) ? d[c] : new String(c, 1))) + @"""";
        }
    }
}
