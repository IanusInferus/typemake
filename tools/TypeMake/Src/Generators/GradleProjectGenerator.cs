using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TypeMake.Cpp
{
    public class GradleProjectGenerator
    {
        private String SolutionName;
        private Project Project;
        private List<ProjectReference> ProjectReferences;
        private PathString InputDirectory;
        private PathString OutputDirectory;
        private PathString SolutionOutputDirectory;
        private String BuildGradleTemplateText;
        private ToolchainType Toolchain;
        private CompilerType Compiler;
        private OperatingSystemType BuildingOperatingSystem;
        private ArchitectureType BuildingOperatingSystemArchitecture;
        private OperatingSystemType TargetOperatingSystem;
        private ArchitectureType? TargetArchitectureType;

        public GradleProjectGenerator(String SolutionName, Project Project, List<ProjectReference> ProjectReferences, PathString InputDirectory, PathString OutputDirectory, PathString SolutionOutputDirectory, String BuildGradleTemplateText, ToolchainType Toolchain, CompilerType Compiler, OperatingSystemType BuildingOperatingSystem, ArchitectureType BuildingOperatingSystemArchitecture, OperatingSystemType TargetOperatingSystem, ArchitectureType? TargetArchitectureType)
        {
            this.SolutionName = SolutionName;
            this.Project = Project;
            this.ProjectReferences = ProjectReferences;
            this.InputDirectory = InputDirectory.FullPath;
            this.OutputDirectory = OutputDirectory.FullPath;
            this.SolutionOutputDirectory = SolutionOutputDirectory.FullPath;
            this.BuildGradleTemplateText = BuildGradleTemplateText;
            this.Toolchain = Toolchain;
            this.Compiler = Compiler;
            this.BuildingOperatingSystem = BuildingOperatingSystem;
            this.BuildingOperatingSystemArchitecture = BuildingOperatingSystemArchitecture;
            this.TargetOperatingSystem = TargetOperatingSystem;
            this.TargetArchitectureType = TargetArchitectureType;
        }

        public void Generate(bool ForceRegenerate)
        {
            var BuildGradlePath = OutputDirectory / Project.Name / "build.gradle";
            var BaseDirPath = BuildGradlePath.Parent;

            var Lines = GenerateLines(BuildGradlePath, BaseDirPath).ToList();
            TextFile.WriteToFile(BuildGradlePath, String.Join("\n", Lines), new UTF8Encoding(false), !ForceRegenerate);
        }

        private IEnumerable<String> GenerateLines(String BuildGradlePath, String BaseDirPath)
        {
            var conf = Project.Configurations.Merged(Toolchain, Compiler, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem, null, null);

            var Results = BuildGradleTemplateText.Replace("\r\n", "\n").Split('\n').AsEnumerable();
            Results = Results.Select(Line => Line.Replace("${ApplicationId}", Project.ApplicationIdentifier ?? (SolutionName + "." + Project.TargetName ?? Project.Name).ToLower()));
            Results = Results.Select(Line => Line.Replace("${ProjectSrcDir}", InputDirectory.RelativeTo(BaseDirPath).ToString(PathStringStyle.Unix)));
            Results = Results.Select(Line => Line.Replace("${SolutionOutputDir}", SolutionOutputDirectory.RelativeTo(BaseDirPath).ToString(PathStringStyle.Unix)));
            if (!TargetArchitectureType.HasValue)
            {
                throw new NotSupportedException("ArchitectureTypeIsNull");
            }
            Results = Results.Select(Line => Line.Replace("${ArchitectureType}", TargetArchitectureType.Value.ToString()));
            Results = Results.Select(Line => Line.Replace("${AndroidAbi}", GetArchitectureString(TargetArchitectureType.Value)));
            Results = Results.Select(Line => Line.Replace("${ProjectTargetName}", Project.TargetName ?? Project.Name));

            return Results;
        }

        public static String GetArchitectureString(ArchitectureType Architecture)
        {
            if (Architecture == Cpp.ArchitectureType.x86)
            {
                return "x86";
            }
            else if (Architecture == Cpp.ArchitectureType.x86_64)
            {
                return "x86_64";
            }
            else if (Architecture == Cpp.ArchitectureType.armeabi_v7a)
            {
                return "armeabi-v7a";
            }
            else if (Architecture == Cpp.ArchitectureType.arm64_v8a)
            {
                return "arm64-v8a";
            }
            else
            {
                throw new NotSupportedException("NotSupportedArchitecture: " + Architecture.ToString());
            }
        }
    }
}
