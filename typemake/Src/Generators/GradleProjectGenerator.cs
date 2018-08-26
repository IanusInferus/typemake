using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace TypeMake.Cpp
{
    public class GradleProjectGenerator
    {
        private String SolutionName;
        private Project Project;
        private List<ProjectReference> ProjectReferences;
        private String InputDirectory;
        private String OutputDirectory;
        private String SolutionOutputDirectory;
        private String BuildGradleTemplateText;
        private ToolchainType Toolchain;
        private CompilerType Compiler;
        private OperatingSystemType BuildingOperatingSystem;
        private OperatingSystemType TargetOperatingSystem;
        private ArchitectureType? ArchitectureType;

        public GradleProjectGenerator(String SolutionName, Project Project, List<ProjectReference> ProjectReferences, String InputDirectory, String OutputDirectory, String SolutionOutputDirectory, String BuildGradleTemplateText, ToolchainType Toolchain, CompilerType Compiler, OperatingSystemType BuildingOperatingSystem, OperatingSystemType TargetOperatingSystem, ArchitectureType? ArchitectureType)
        {
            this.SolutionName = SolutionName;
            this.Project = Project;
            this.ProjectReferences = ProjectReferences;
            this.InputDirectory = Path.GetFullPath(InputDirectory);
            this.OutputDirectory = Path.GetFullPath(OutputDirectory);
            this.SolutionOutputDirectory = Path.GetFullPath(SolutionOutputDirectory);
            this.BuildGradleTemplateText = BuildGradleTemplateText;
            this.Toolchain = Toolchain;
            this.Compiler = Compiler;
            this.BuildingOperatingSystem = BuildingOperatingSystem;
            this.TargetOperatingSystem = TargetOperatingSystem;
            this.ArchitectureType = ArchitectureType;
        }

        public void Generate(bool EnableRebuild)
        {
            var BuildGradlePath = Path.Combine(OutputDirectory, Path.Combine(Project.Name, "build.gradle"));
            var BaseDirPath = Path.GetDirectoryName(BuildGradlePath);

            var Lines = GenerateLines(BuildGradlePath, BaseDirPath).ToList();
            TextFile.WriteToFile(BuildGradlePath, String.Join("\n", Lines), new UTF8Encoding(false), !EnableRebuild);
        }

        private IEnumerable<String> GenerateLines(String BuildGradlePath, String BaseDirPath)
        {
            var conf = ConfigurationUtils.GetMergedConfiguration(Toolchain, Compiler, BuildingOperatingSystem, TargetOperatingSystem, null, null, Project.Configurations);

            var Results = BuildGradleTemplateText.Replace("\r\n", "\n").Split('\n').AsEnumerable();
            Results = Results.Select(Line => Line.Replace("${ApplicationId}", Project.ApplicationIdentifier ?? (SolutionName + "." + Project.TargetName ?? Project.Name).ToLower()));
            Results = Results.Select(Line => Line.Replace("${ProjectSrcDir}", FileNameHandling.GetRelativePath(InputDirectory, BaseDirPath).Replace('\\', '/')));
            Results = Results.Select(Line => Line.Replace("${SolutionOutputDir}", FileNameHandling.GetRelativePath(SolutionOutputDirectory, BaseDirPath).Replace('\\', '/').TrimEnd('/')));
            if (!ArchitectureType.HasValue)
            {
                throw new NotSupportedException("ArchitectureTypeIsNull");
            }
            Results = Results.Select(Line => Line.Replace("${ArchitectureType}", ArchitectureType.Value.ToString()));
            Results = Results.Select(Line => Line.Replace("${AndroidAbi}", GetArchitectureString(ArchitectureType.Value)));
            Results = Results.Select(Line => Line.Replace("${ProjectTargetName}", Project.TargetName ?? Project.Name));

            return Results;
        }

        private static String GetArchitectureString(ArchitectureType Architecture)
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
