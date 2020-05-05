﻿using System;
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
        private OperatingSystemType HostOperatingSystem;
        private ArchitectureType HostArchitecture;
        private OperatingSystemType TargetOperatingSystem;
        private ArchitectureType? TargetArchitectureType;
        private ToolchainType Toolchain;
        private CompilerType Compiler;
        private CLibraryType CLibrary;
        private CLibraryForm CLibraryForm;
        private CppLibraryType CppLibrary;
        private CppLibraryForm CppLibraryForm;
        private ConfigurationType? ConfigurationType;
        private PathString AndroidNdk;

        public GradleProjectGenerator(String SolutionName, Project Project, List<ProjectReference> ProjectReferences, PathString InputDirectory, PathString OutputDirectory, PathString SolutionOutputDirectory, String BuildGradleTemplateText, OperatingSystemType HostOperatingSystem, ArchitectureType HostArchitecture, OperatingSystemType TargetOperatingSystem, ArchitectureType? TargetArchitectureType, ToolchainType Toolchain, CompilerType Compiler, CLibraryType CLibrary, CLibraryForm CLibraryForm, CppLibraryType CppLibrary, CppLibraryForm CppLibraryForm, ConfigurationType? ConfigurationType, PathString AndroidNdk)
        {
            this.SolutionName = SolutionName;
            this.ProjectName = Project.Name.Split(':').First();
            this.Project = Project;
            this.ProjectReferences = ProjectReferences;
            this.InputDirectory = InputDirectory.FullPath;
            this.OutputDirectory = OutputDirectory.FullPath;
            this.SolutionOutputDirectory = SolutionOutputDirectory.FullPath;
            this.BuildGradleTemplateText = BuildGradleTemplateText;
            this.HostOperatingSystem = HostOperatingSystem;
            this.HostArchitecture = HostArchitecture;
            this.TargetOperatingSystem = TargetOperatingSystem;
            this.TargetArchitectureType = TargetArchitectureType;
            if (!TargetArchitectureType.HasValue)
            {
                throw new NotSupportedException("ArchitectureTypeIsNull");
            }
            this.Toolchain = Toolchain;
            this.Compiler = Compiler;
            this.CLibrary = CLibrary;
            this.CLibraryForm = CLibraryForm;
            this.CppLibrary = CppLibrary;
            this.CppLibraryForm = CppLibraryForm;
            this.ConfigurationType = ConfigurationType;
            this.AndroidNdk = AndroidNdk;
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
            var conf = Project.Configurations.Merged(Project.TargetType, HostOperatingSystem, HostArchitecture, TargetOperatingSystem, TargetArchitectureType, Toolchain, Compiler, CLibrary, CLibraryForm, CppLibrary, CppLibraryForm, ConfigurationType);
            var confDebug = Project.Configurations.Merged(Project.TargetType, HostOperatingSystem, HostArchitecture, TargetOperatingSystem, TargetArchitectureType, Toolchain, Compiler, CLibrary, CLibraryForm, CppLibrary, CppLibraryForm, Cpp.ConfigurationType.Debug);
            var confRelease = Project.Configurations.Merged(Project.TargetType, HostOperatingSystem, HostArchitecture, TargetOperatingSystem, TargetArchitectureType, Toolchain, Compiler, CLibrary, CLibraryForm, CppLibrary, CppLibraryForm, Cpp.ConfigurationType.Release);

            var Results = BuildGradleTemplateText.Replace("\r\n", "\n").Split('\n').AsEnumerable();
            var SolutionOutputDir = SolutionOutputDirectory.RelativeTo(BaseDirPath).ToString(PathStringStyle.Unix);
            var ArchitectureType = TargetArchitectureType.Value.ToString();
            var ProjectTargetName = Project.TargetName ?? ProjectName;
            var ApplicationId = conf.Options.ContainsKey("gradle.applicationId") ? conf.Options["gradle.applicationId"] : (SolutionName + "." + ProjectTargetName).ToLower();
            var ManifestSrcFile = confDebug.Options.ContainsKey("gradle.manifestSrcFile") ? confDebug.Options["gradle.manifestSrcFile"].AsPath().RelativeTo(BaseDirPath).ToString(PathStringStyle.Unix) : (InputDirectory / "AndroidManifest.xml").RelativeTo(BaseDirPath).ToString(PathStringStyle.Unix);
            var JavaSrcDirs = String.Join(", ", conf.Files.Where(f => System.IO.Directory.Exists(f.Path)).Select(f => "'" + f.Path.RelativeTo(BaseDirPath).ToString(PathStringStyle.Unix) + "'"));
            var ResSrcDirs = conf.Options.ContainsKey("gradle.resSrcDirs") ? String.Join(", ", conf.Options["gradle.resSrcDirs"].Split(';').Select(d => "'" + d.AsPath().RelativeTo(BaseDirPath).ToString(PathStringStyle.Unix) + "'")) : "'" + (InputDirectory / "res").RelativeTo(BaseDirPath).ToString(PathStringStyle.Unix) + "'";
            var AssetsSrcDirs = conf.Options.ContainsKey("gradle.assetsSrcDirs") ? String.Join(", ", conf.Options["gradle.assetsSrcDirs"].Split(';').Select(d => "'" + d.AsPath().RelativeTo(BaseDirPath).ToString(PathStringStyle.Unix) + "'")) : "'" + (InputDirectory / "assets").RelativeTo(BaseDirPath).ToString(PathStringStyle.Unix) + "'";
            var JarDirs = conf.Options.ContainsKey("gradle.jarDirs") ? conf.Options["gradle.jarDirs"].Split(';').Select(d => d.AsPath().RelativeTo(BaseDirPath).ToString(PathStringStyle.Unix)).ToList() : new List<String> { };
            var AndroidAbi = GetArchitectureString(TargetArchitectureType.Value);
            var TempJniLibsDirDebug = confDebug.Options.ContainsKey("gradle.tempJniLibsDirectory") ? confDebug.Options["gradle.tempJniLibsDirectory"].AsPath().RelativeTo(BaseDirPath).ToString(PathStringStyle.Unix) : $"{SolutionOutputDir}/{ArchitectureType}_Debug/gradle/{ProjectName}";
            var TempJniLibsDirRelease = confRelease.Options.ContainsKey("gradle.tempJniLibsDirectory") ? confRelease.Options["gradle.tempJniLibsDirectory"].AsPath().RelativeTo(BaseDirPath).ToString(PathStringStyle.Unix) : $"{SolutionOutputDir}/{ArchitectureType}_Release/gradle/{ProjectName}";
            var TargetDirectoryDebug = confDebug.Options.ContainsKey("gradle.targetDirectory") ? confDebug.Options["gradle.targetDirectory"].AsPath() : (SolutionOutputDirectory / $"{ArchitectureType}_Debug");
            var TargetDirectoryRelease = confRelease.Options.ContainsKey("gradle.targetDirectory") ? confRelease.Options["gradle.targetDirectory"].AsPath() : (SolutionOutputDirectory / $"{ArchitectureType}_Release");
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
            var SoLibraryPathsDebug = new List<PathString> { TargetDirectoryDebug / $"lib{ProjectTargetName}.so" };
            var SoLibraryPathsRelease = new List<PathString> { TargetDirectoryRelease / $"lib{ProjectTargetName}.so" };
            foreach (var Lib in confDebug.Libs)
            {
                if (Lib.Extension.ToLowerInvariant() != "so") { continue; }
                var Found = false;
                foreach (var LibDirectory in confDebug.LibDirectories)
                {
                    if (System.IO.File.Exists(LibDirectory / Lib))
                    {
                        SoLibraryPathsDebug.Add(LibDirectory / Lib);
                        Found = true;
                        break;
                    }
                }
                if (!Found)
                {
                    SoLibraryPathsDebug.Add(SolutionOutputDirectory / $"{ArchitectureType}_Debug" / Lib);
                }
            }
            foreach (var Lib in confRelease.Libs)
            {
                if (Lib.Extension.ToLowerInvariant() != "so") { continue; }
                var Found = false;
                foreach (var LibDirectory in confRelease.LibDirectories)
                {
                    if (System.IO.File.Exists(LibDirectory / Lib))
                    {
                        SoLibraryPathsRelease.Add(LibDirectory / Lib);
                        Found = true;
                        break;
                    }
                }
                if (!Found)
                {
                    SoLibraryPathsRelease.Add(SolutionOutputDirectory / $"{ArchitectureType}_Release" / Lib);
                }
            }
            if ((CppLibrary == CppLibraryType.libcxx) && (CppLibraryForm == CppLibraryForm.Dynamic))
            {
                var LibcxxSo = AndroidNdk / $"toolchains/llvm/prebuilt/{GetHostArchitectureString(HostOperatingSystem, HostArchitecture)}-x86_64/sysroot/usr/lib/{GetTargetTripletString(TargetArchitectureType.Value)}/libc++_shared.so";
                SoLibraryPathsDebug.Add(LibcxxSo);
                SoLibraryPathsRelease.Add(LibcxxSo);
            }
            var LibsDebug = String.Join(", ", SoLibraryPathsDebug.Select(p => "'" + p.RelativeTo(BaseDirPath).ToString(PathStringStyle.Unix) + "'"));
            var LibsRelease = String.Join(", ", SoLibraryPathsRelease.Select(p => "'" + p.RelativeTo(BaseDirPath).ToString(PathStringStyle.Unix) + "'"));
            Results = Results.Select(Line => Line.Replace("${ApplicationId}", ApplicationId));
            Results = Results.Select(Line => Line.Replace("${ManifestSrcFile}", ManifestSrcFile));
            Results = Results.Select(Line => Line.Replace("${JavaSrcDirs}", JavaSrcDirs));
            Results = Results.Select(Line => Line.Replace("${ResSrcDirs}", ResSrcDirs));
            Results = Results.Select(Line => Line.Replace("${AssetsSrcDirs}", AssetsSrcDirs));
            Results = Results.Select(Line => Line.Replace("${AndroidAbi}", AndroidAbi));
            Results = Results.Select(Line => Line.Replace("${TempJniLibsDirDebug}", TempJniLibsDirDebug));
            Results = Results.Select(Line => Line.Replace("${TempJniLibsDirRelease}", TempJniLibsDirRelease));
            Results = Results.Select(Line => Line.Replace("${LibsDebug}", LibsDebug));
            Results = Results.Select(Line => Line.Replace("${LibsRelease}", LibsRelease));
            Results = Results.SelectMany(Line => Line.Contains("${JarDirs}") ? JarDirs.Select(JarDir => Line.Replace("${JarDirs}", JarDir)) : new List<String> { Line });

            return Results;
        }

        public static String GetArchitectureString(ArchitectureType Architecture)
        {
            if (Architecture == Cpp.ArchitectureType.x86)
            {
                return "x86";
            }
            else if (Architecture == Cpp.ArchitectureType.x64)
            {
                return "x86_64";
            }
            else if (Architecture == Cpp.ArchitectureType.armv7a)
            {
                return "armeabi-v7a";
            }
            else if (Architecture == Cpp.ArchitectureType.arm64)
            {
                return "arm64-v8a";
            }
            else
            {
                throw new NotSupportedException("NotSupportedArchitecture: " + Architecture.ToString());
            }
        }
        public static String GetHostArchitectureString(OperatingSystemType OperatingSystem, ArchitectureType Architecture)
        {
            if ((OperatingSystem == OperatingSystemType.Windows) && (Architecture == ArchitectureType.x64))
            {
                return "windows-x86_64";
            }
            if ((OperatingSystem == OperatingSystemType.Linux) && (Architecture == ArchitectureType.x64))
            {
                return "linux-x86_64";
            }
            if ((OperatingSystem == OperatingSystemType.MacOS) && (Architecture == ArchitectureType.x64))
            {
                return "darwin-x86_64";
            }
            else
            {
                throw new NotSupportedException("NotSupportedHost: " + OperatingSystem.ToString() + " " + Architecture.ToString());
            }
        }
        public static String GetNdkArchitectureString(ArchitectureType Architecture)
        {
            if (Architecture == Cpp.ArchitectureType.x86)
            {
                return "i686";
            }
            else if (Architecture == Cpp.ArchitectureType.x64)
            {
                return "x86_64";
            }
            else if (Architecture == Cpp.ArchitectureType.armv7a)
            {
                return "arm";
            }
            else if (Architecture == Cpp.ArchitectureType.arm64)
            {
                return "aarch64";
            }
            else
            {
                throw new NotSupportedException("NotSupportedArchitecture: " + Architecture.ToString());
            }
        }
        public static String GetTargetTripletString(ArchitectureType Architecture)
        {
            return GetNdkArchitectureString(Architecture) + "-linux-android";
        }
    }
}