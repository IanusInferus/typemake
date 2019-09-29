using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TypeMake
{
    public class NinjaSolutionGenerator
    {
        private String SolutionName;
        private List<ProjectReference> ProjectReferences;
        private PathString ProjectOutputDirectory;
        private String CC;
        private String CXX;
        private String AR;
        private String STRIP;

        public NinjaSolutionGenerator(String SolutionName, List<ProjectReference> ProjectReferences, PathString ProjectOutputDirectory, String CC, String CXX, String AR, String STRIP)
        {
            this.SolutionName = SolutionName;
            this.ProjectReferences = ProjectReferences;
            this.ProjectOutputDirectory = ProjectOutputDirectory.FullPath;
            this.CC = CC;
            this.CXX = CXX;
            this.AR = AR;
            this.STRIP = STRIP;
        }

        public void Generate(bool ForceRegenerate)
        {
            var NinjaScriptPath = ProjectOutputDirectory / "build.ninja";

            var Lines = GenerateLines(NinjaScriptPath).ToList();
            TextFile.WriteToFile(NinjaScriptPath, String.Join("\n", Lines), new UTF8Encoding(false), !ForceRegenerate);
        }

        private IEnumerable<String> GenerateLines(PathString NinjaScriptPath)
        {
            yield return "ninja_required_version = 1.3";
            yield return "";
            yield return "cc = " + CC;
            yield return "cxx = " + CXX;
            yield return "ar = " + AR;
            yield return "strip = " + STRIP;
            yield return "fileflags = ";
            yield return "";
            yield return "rule cc";
            yield return "  command = $cc -MMD -MT $out -MF $out.d $commonflags $cflags $fileflags -c $in -o $out";
            yield return "  description = CC $out";
            yield return "  depfile = $out.d";
            yield return "  deps = gcc";
            yield return "";
            yield return "rule cxx";
            yield return "  command = $cxx -MMD -MT $out -MF $out.d $commonflags $cxxflags $fileflags -c $in -o $out";
            yield return "  description = CXX $out";
            yield return "  depfile = $out.d";
            yield return "  deps = gcc";
            yield return "";
            yield return "rule ar";
            yield return "  command = $ar crs $out $in";
            yield return "  description = AR $out";
            yield return "";
            yield return "rule link";
            yield return "  command = $cxx $ldflags -o $out $in $libs";
            yield return "  description = LINK $out";
            yield return "";
            if (!String.IsNullOrEmpty(STRIP))
            {
                yield return "rule strip";
                yield return "  command = $strip -o $out $in";
                yield return "  description = STRIP $out";
                yield return "";
            }
            foreach (var p in ProjectReferences)
            {
                yield return @"subninja " + p.FilePath.FullPath.RelativeTo(ProjectOutputDirectory).ToString(PathStringStyle.Unix) + ".ninja";
            }
            yield return "";
        }
    }
}
