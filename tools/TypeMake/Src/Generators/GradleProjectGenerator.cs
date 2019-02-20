using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TypeMake.Cpp
{
    public class GradleProjectGenerator
    {
        private String SolutionName;
        private String ProjectName;
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
        private ConfigurationType? ConfigurationType;

        public GradleProjectGenerator(String SolutionName, Project Project, List<ProjectReference> ProjectReferences, PathString InputDirectory, PathString OutputDirectory, PathString SolutionOutputDirectory, String BuildGradleTemplateText, ToolchainType Toolchain, CompilerType Compiler, OperatingSystemType BuildingOperatingSystem, ArchitectureType BuildingOperatingSystemArchitecture, OperatingSystemType TargetOperatingSystem, ArchitectureType? TargetArchitectureType, ConfigurationType? ConfigurationType)
        {
            this.SolutionName = SolutionName;
            this.ProjectName = Project.Name.Split(':').First();
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
            if (!TargetArchitectureType.HasValue)
            {
                throw new NotSupportedException("ArchitectureTypeIsNull");
            }
            this.ConfigurationType = ConfigurationType;
        }

        public void Generate(bool ForceRegenerate)
        {
            var BuildGradlePath = OutputDirectory / ProjectName / "build.gradle";
            var BaseDirPath = BuildGradlePath.Parent;

            var Lines = GenerateLines(BuildGradlePath, BaseDirPath).ToList();
            TextFile.WriteToFile(BuildGradlePath, String.Join("\n", Lines), new UTF8Encoding(false), !ForceRegenerate);
        }

        private IEnumerable<String> GenerateLines(String BuildGradlePath, String BaseDirPath)
        {
            var conf = Project.Configurations.Merged(Project.TargetType, Toolchain, Compiler, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem, TargetArchitectureType, ConfigurationType);
            var confDebug = Project.Configurations.Merged(Project.TargetType, Toolchain, Compiler, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem, TargetArchitectureType, Cpp.ConfigurationType.Debug);
            var confRelease = Project.Configurations.Merged(Project.TargetType, Toolchain, Compiler, BuildingOperatingSystem, BuildingOperatingSystemArchitecture, TargetOperatingSystem, TargetArchitectureType, Cpp.ConfigurationType.Release);

            var Results = BuildGradleTemplateText.Replace("\r\n", "\n").Split('\n').AsEnumerable();
            var SolutionOutputDir = SolutionOutputDirectory.RelativeTo(BaseDirPath).ToString(PathStringStyle.Unix);
            var ArchitectureType = TargetArchitectureType.Value.ToString();
            var ProjectTargetName = Project.TargetName ?? ProjectName;
            var ApplicationId = conf.Options.ContainsKey("gradle.applicationId") ? conf.Options["gradle.applicationId"] : (SolutionName + "." + ProjectTargetName).ToLower();
            var ManifestSrcFile = confDebug.Options.ContainsKey("gradle.manifestSrcFile") ? confDebug.Options["gradle.manifestSrcFile"].AsPath().RelativeTo(BaseDirPath).ToString(PathStringStyle.Unix) : (InputDirectory / "AndroidManifest.xml").RelativeTo(BaseDirPath).ToString(PathStringStyle.Unix);
            var JavaSrcDirs = String.Join(", ", conf.Files.Where(f => System.IO.Directory.Exists(f.Path)).Select(f => "'" + f.Path.RelativeTo(BaseDirPath).ToString(PathStringStyle.Unix) + "'"));
            var ResSrcDirs = conf.Options.ContainsKey("gradle.resSrcDirs") ? String.Join(", ", conf.Options["gradle.resSrcDirs"].Split(';').Select(d => "'" + d.AsPath().RelativeTo(BaseDirPath).ToString(PathStringStyle.Unix) + "'")) : "'" + (InputDirectory / "res").RelativeTo(BaseDirPath).ToString(PathStringStyle.Unix) + "'";
            var AssetsSrcDirs = conf.Options.ContainsKey("gradle.assetsSrcDirs") ? String.Join(", ", conf.Options["gradle.assetsSrcDirs"].Split(';').Select(d => "'" + d.AsPath().RelativeTo(BaseDirPath).ToString(PathStringStyle.Unix) + "'")) : "'" + (InputDirectory / "assets").RelativeTo(BaseDirPath).ToString(PathStringStyle.Unix) + "'";
            var AndroidAbi = GetArchitectureString(TargetArchitectureType.Value);
            var TempJniLibsDirDebug = confDebug.Options.ContainsKey("gradle.tempJniLibsDirectory") ? confDebug.Options["gradle.tempJniLibsDirectory"].AsPath().RelativeTo(BaseDirPath).ToString(PathStringStyle.Unix) : $"{SolutionOutputDir}/{ArchitectureType}_Debug/gradle/{ProjectTargetName}";
            var TempJniLibsDirRelease = confRelease.Options.ContainsKey("gradle.tempJniLibsDirectory") ? confRelease.Options["gradle.tempJniLibsDirectory"].AsPath().RelativeTo(BaseDirPath).ToString(PathStringStyle.Unix) : $"{SolutionOutputDir}/{ArchitectureType}_Release/gradle/{ProjectTargetName}";
            var TargetDirectoryDebug = confDebug.Options.ContainsKey("gradle.targetDirectory") ? confDebug.Options["gradle.targetDirectory"].AsPath().RelativeTo(BaseDirPath).ToString(PathStringStyle.Unix) : $"{SolutionOutputDir}/{ArchitectureType}_Debug";
            var TargetDirectoryRelease = confRelease.Options.ContainsKey("gradle.targetDirectory") ? confRelease.Options["gradle.targetDirectory"].AsPath().RelativeTo(BaseDirPath).ToString(PathStringStyle.Unix) : $"{SolutionOutputDir}/{ArchitectureType}_Release";
            if (ConfigurationType == Cpp.ConfigurationType.Debug)
            {
                TempJniLibsDirRelease = TempJniLibsDirDebug;
                TargetDirectoryRelease = TargetDirectoryDebug;
            }
            else if (ConfigurationType == Cpp.ConfigurationType.Release)
            {
                TempJniLibsDirDebug = TempJniLibsDirRelease;
                TargetDirectoryDebug = TargetDirectoryRelease;
            }
            Results = Results.Select(Line => Line.Replace("${ProjectTargetName}", ProjectTargetName));
            Results = Results.Select(Line => Line.Replace("${ApplicationId}", ApplicationId));
            Results = Results.Select(Line => Line.Replace("${ManifestSrcFile}", ManifestSrcFile));
            Results = Results.Select(Line => Line.Replace("${JavaSrcDirs}", JavaSrcDirs));
            Results = Results.Select(Line => Line.Replace("${ResSrcDirs}", ResSrcDirs));
            Results = Results.Select(Line => Line.Replace("${AssetsSrcDirs}", AssetsSrcDirs));
            Results = Results.Select(Line => Line.Replace("${AndroidAbi}", AndroidAbi));
            Results = Results.Select(Line => Line.Replace("${TempJniLibsDirDebug}", TempJniLibsDirDebug));
            Results = Results.Select(Line => Line.Replace("${TempJniLibsDirRelease}", TempJniLibsDirRelease));
            Results = Results.Select(Line => Line.Replace("${TargetDirectoryDebug}", TargetDirectoryDebug));
            Results = Results.Select(Line => Line.Replace("${TargetDirectoryRelease}", TargetDirectoryRelease));

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
