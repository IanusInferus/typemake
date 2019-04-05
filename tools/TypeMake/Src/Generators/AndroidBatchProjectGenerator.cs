using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TypeMake.Cpp
{
    public class AndroidBatchProjectGenerator
    {
        private String SolutionName;
        private String ProjectName;
        private Project Project;
        private List<ProjectReference> ProjectReferences;
        private PathString InputDirectory;
        private PathString OutputDirectory;
        private PathString SolutionOutputDirectory;
        private ToolchainType Toolchain;
        private CompilerType Compiler;
        private OperatingSystemType HostOperatingSystem;
        private ArchitectureType HostArchitecture;
        private OperatingSystemType TargetOperatingSystem;
        private ArchitectureType TargetArchitectureType;
        private ConfigurationType ConfigurationType;
        private PathString Jdk;
        private PathString AndroidSdk;
        private PathString AndroidNdk;
        private String BuildToolVersion;
        private int MinSdkVersion;
        private int TargetSdkVersion;
        private PathString KeyStore;
        private String KeyStorePass;
        private String KeyAlias;
        private String KeyPass;
        private bool IsDebug;

        public AndroidBatchProjectGenerator(String SolutionName, Project Project, List<ProjectReference> ProjectReferences, PathString InputDirectory, PathString OutputDirectory, PathString SolutionOutputDirectory, ToolchainType Toolchain, CompilerType Compiler, OperatingSystemType HostOperatingSystem, ArchitectureType HostArchitecture, OperatingSystemType TargetOperatingSystem, ArchitectureType? TargetArchitectureType, ConfigurationType? ConfigurationType, PathString Jdk, PathString AndroidSdk, PathString AndroidNdk, String BuildToolVersion, int MinSdkVersion, int TargetSdkVersion, PathString KeyStore, String KeyStorePass, String KeyAlias, String KeyPass, bool IsDebug)
        {
            this.SolutionName = SolutionName;
            this.ProjectName = Project.Name.Split(':').First();
            this.Project = Project;
            this.ProjectReferences = ProjectReferences;
            this.InputDirectory = InputDirectory.FullPath;
            this.OutputDirectory = OutputDirectory.FullPath;
            this.SolutionOutputDirectory = SolutionOutputDirectory.FullPath;
            this.Toolchain = Toolchain;
            this.Compiler = Compiler;
            this.HostOperatingSystem = HostOperatingSystem;
            this.HostArchitecture = HostArchitecture;
            this.TargetOperatingSystem = TargetOperatingSystem;
            if (!TargetArchitectureType.HasValue)
            {
                throw new NotSupportedException("ArchitectureTypeIsNull");
            }
            this.TargetArchitectureType = TargetArchitectureType.Value;
            if (!ConfigurationType.HasValue)
            {
                throw new NotSupportedException("ConfigurationTypeIsNull");
            }
            this.ConfigurationType = ConfigurationType.Value;
            this.Jdk = Jdk;
            this.AndroidSdk = AndroidSdk;
            this.AndroidNdk = AndroidNdk;
            this.BuildToolVersion = BuildToolVersion;
            this.MinSdkVersion = MinSdkVersion;
            this.TargetSdkVersion = TargetSdkVersion;
            this.KeyStore = KeyStore;
            this.KeyStorePass = KeyStorePass;
            this.KeyAlias = KeyAlias;
            this.KeyPass = KeyPass;
            this.IsDebug = IsDebug;
        }

        public void Generate(bool ForceRegenerate)
        {
            var BuildBatchPath = OutputDirectory / ProjectName / (HostOperatingSystem == OperatingSystemType.Windows ? "build.cmd" : "build.sh");
            var BaseDirPath = BuildBatchPath.Parent;

            var Lines = GenerateLines(BuildBatchPath, BaseDirPath).ToList();
            TextFile.WriteToFile(BuildBatchPath, String.Join(HostOperatingSystem == OperatingSystemType.Windows ? "\r\n" : "\n", Lines), new UTF8Encoding(false), !ForceRegenerate);
        }

        private IEnumerable<String> GenerateLines(String BuildBatchPath, String BaseDirPath)
        {
            var conf = Project.Configurations.Merged(Project.TargetType, Toolchain, Compiler, HostOperatingSystem, HostArchitecture, TargetOperatingSystem, TargetArchitectureType, ConfigurationType);

            var Abi = GetArchitectureString(TargetArchitectureType);
            var ProjectTargetName = Project.TargetName ?? ProjectName;
            var Suffix = Project.TargetType == TargetType.GradleApplication ? "apk" : Project.TargetType == TargetType.GradleLibrary ? "aar" : throw new InvalidOperationException();
            var JniLibs = Project.TargetType == TargetType.GradleApplication ? "lib" : Project.TargetType == TargetType.GradleLibrary ? "jni" : throw new InvalidOperationException();

            var TargetDirectory = conf.Options.ContainsKey("gradle.targetDirectory") ? conf.Options["gradle.targetDirectory"].AsPath() : (SolutionOutputDirectory / $"{TargetArchitectureType}_Debug");

            var SoLibraryPaths = new List<PathString> { TargetDirectory / $"lib{ProjectTargetName}.so" };
            foreach (var Lib in conf.Libs)
            {
                if (Lib.Extension.ToLowerInvariant() != "so") { continue; }
                var Found = false;
                foreach (var LibDirectory in conf.LibDirectories)
                {
                    if (System.IO.File.Exists(LibDirectory / Lib))
                    {
                        SoLibraryPaths.Add(LibDirectory / Lib);
                        Found = true;
                        break;
                    }
                }
                if (!Found)
                {
                    SoLibraryPaths.Add(SolutionOutputDirectory / $"{TargetArchitectureType}_Debug" / Lib);
                }
            }
            var ApplicationId = conf.Options.ContainsKey("gradle.applicationId") ? conf.Options["gradle.applicationId"] : (SolutionName + "." + ProjectTargetName).ToLower();
            var ManifestSrcFile = conf.Options.ContainsKey("gradle.manifestSrcFile") ? conf.Options["gradle.manifestSrcFile"].AsPath().RelativeTo(BaseDirPath) : (InputDirectory / "AndroidManifest.xml").RelativeTo(BaseDirPath);
            var JavaSrcDirs = conf.Files.Where(f => System.IO.Directory.Exists(f.Path) && System.IO.Directory.EnumerateFiles(f.Path, "*.java", System.IO.SearchOption.AllDirectories).Any()).Select(f => f.Path.RelativeTo(BaseDirPath)).ToList();
            var JarFiles = (conf.Options.ContainsKey("gradle.jarDirs") ? conf.Options["gradle.jarDirs"].Split(';').Select(d => d.AsPath()).ToList() : new List<PathString> { }).SelectMany(d => System.IO.Directory.GetFiles(d, "*.jar", System.IO.SearchOption.AllDirectories).Select(f => f.AsPath().RelativeTo(BaseDirPath))).ToList();
            var ResSrcDirs = conf.Options.ContainsKey("gradle.resSrcDirs") ? conf.Options["gradle.resSrcDirs"].Split(';').Select(d => d.AsPath().RelativeTo(BaseDirPath)).ToList() : System.IO.Directory.Exists(InputDirectory / "res") ? new List<PathString> { (InputDirectory / "res").RelativeTo(BaseDirPath) } : new List<PathString> { };
            var AssetsSrcDirs = conf.Options.ContainsKey("gradle.assetsSrcDirs") ? conf.Options["gradle.assetsSrcDirs"].Split(';').Select(d => d.AsPath().RelativeTo(BaseDirPath)).ToList() : System.IO.Directory.Exists(InputDirectory / "assets") ? new List<PathString> { (InputDirectory / "assets").RelativeTo(BaseDirPath) } : new List<PathString> { };

            if (HostOperatingSystem == Cpp.OperatingSystemType.Windows)
            {
                Func<String, String> e = s => Shell.EscapeArgumentForShell(s, Shell.ShellArgumentStyle.CMD);

                var Javac = Jdk / "bin/javac.exe";
                var Jar = Jdk / "bin/jar.exe";
                var Aapt2 = AndroidSdk / "build-tools" / BuildToolVersion / "aapt2.exe";
                var D8 = AndroidSdk / "build-tools" / BuildToolVersion / "d8.bat";
                var ZipAlign = AndroidSdk / "build-tools" / BuildToolVersion / "zipalign.exe";
                var ApkSigner = AndroidSdk / "build-tools" / BuildToolVersion / "apksigner.bat";
                var AndroidJar = AndroidSdk / $"platforms/android-{TargetSdkVersion}" / "android.jar";
                var Strip = AndroidNdk / "toolchains/llvm/prebuilt/windows-x86_64/bin/llvm-strip.exe";

                yield return @"@echo off";
                yield return @"setlocal";
                yield return @"if ""%SUB_NO_PAUSE_SYMBOL%""==""1"" set NO_PAUSE_SYMBOL=1";
                yield return @"if /I ""%COMSPEC%"" == %CMDCMDLINE% set NO_PAUSE_SYMBOL=1";
                yield return @"set SUB_NO_PAUSE_SYMBOL=1";
                yield return @"call :main";
                yield return @"set EXIT_CODE=%ERRORLEVEL%";
                yield return @"if not ""%NO_PAUSE_SYMBOL%"" == ""1"" pause";
                yield return @"exit /b %EXIT_CODE%";
                yield return @"";
                yield return @":main";
                yield return @"";
                yield return $@"if exist {JniLibs} ( rd /S /Q {JniLibs} ) || exit /b 1";
                yield return @"if exist gen ( rd /S /Q gen ) || exit /b 1";
                yield return @"if exist classes ( rd /S /Q classes ) || exit /b 1";
                yield return @"";
                yield return @":: strip debug symbols";
                yield return $@"md {JniLibs}\{Abi} || exit /b 1";
                foreach (var LibPath in SoLibraryPaths)
                {
                    yield return $@"{e(Strip)} {e(LibPath)} -o {e(JniLibs.AsPath() / Abi / LibPath.FileName)} -discard-all -strip-all || exit /b 1";
                }
                yield return @"";
                if (ResSrcDirs.Count > 0)
                {
                    yield return @":: compile resources";
                    yield return $@"{e(Aapt2)} compile {String.Join(" ", ResSrcDirs.Select(d => "--dir " + e(d)))} -o resources.zip || exit /b 1";
                    yield return @"";
                }
                if (Project.TargetType == TargetType.GradleApplication)
                {
                    yield return @":: link resources and generate apk/aar and R.java";
                    yield return $@"{e(Aapt2)} link -o {e(ProjectTargetName + "." + Suffix)} --rename-manifest-package {e(ApplicationId)} --min-sdk-version {MinSdkVersion} --target-sdk-version {TargetSdkVersion}{(IsDebug ? " --debug-mode" : "")} -I {e(AndroidJar)} --manifest {e(ManifestSrcFile)}{(ResSrcDirs.Count == 0 ? "" : " resources.zip")} --java gen || exit /b 1";
                    yield return @"";
                }
                else
                {
                    yield return @":: link resources and generate R.txt";
                    yield return $@"{e(Aapt2)} link -o dummy.apk -I {e(AndroidJar)} --manifest {e(ManifestSrcFile)}{(ResSrcDirs.Count == 0 ? "" : " resources.zip")} --output-text-symbols R.txt || exit /b 1";
                    yield return $@"{e(Jar)} cvfM {e(ProjectTargetName + "." + Suffix)} R.txt || exit /b 1";
                    yield return $@"{e(Jar)} uvf {e(ProjectTargetName + "." + Suffix)} -C {e(ManifestSrcFile.Parent)} {e(ManifestSrcFile.FileName)} || exit /b 1";
                    foreach (var ResSrcDir in ResSrcDirs)
                    {
                        yield return $@"{e(Jar)} uvf {e(ProjectTargetName + "." + Suffix)} -C {e(ResSrcDir.Parent)} res || exit /b 1";
                    }
                    yield return @"";
                }
                yield return @":: compile java sources";
                yield return @"md classes";
                yield return @"echo >NUL 2>java_source_list.txt";
                if (Project.TargetType == TargetType.GradleApplication)
                {
                    yield return $@"dir /A:-D /S /B gen\*.java >> java_source_list.txt || exit /b 1";
                }
                foreach (var JavaSrcDir in JavaSrcDirs)
                {
                    yield return $@"dir /A:-D /S /B {e(JavaSrcDir / "*.java")} >> java_source_list.txt || exit /b 1";
                }
                yield return $@"{e(Javac)} -g -cp {e(AndroidJar)} -encoding utf-8 -d classes{String.Join("", JavaSrcDirs.Select(d => " -sourcepath " + e(d)))} -sourcepath gen{(JarFiles.Count > 0 ? " -cp " + e(String.Join(";", JarFiles)) : "")} @java_source_list.txt || exit /b 1";
                yield return @"";
                yield return @":: package class files to jar";
                yield return $@"{e(Jar)} cvfM classes.jar -C classes . || exit /b 1";
                yield return @"";
                if (Project.TargetType == TargetType.GradleApplication)
                {
                    yield return @":: convert jar to dex";
                    yield return $@"call {e(D8)} --min-api {MinSdkVersion} --lib {e(AndroidJar)}{(IsDebug ? " --debug" : "")} classes.jar{(JarFiles.Count > 0 ? " " + String.Join(" ", JarFiles.Select(f => e(f))) : "")} || exit /b 1";
                    yield return @"";
                }
                yield return @":: add files to apk/aar";
                yield return $@"{e(Jar)} uvf {e(ProjectTargetName + "." + Suffix)} classes.{(Project.TargetType == TargetType.GradleApplication ? "dex" : "jar")} || exit /b 1";
                if (Project.TargetType == TargetType.GradleApplication)
                {
                    foreach (var AssetsSrcDir in AssetsSrcDirs)
                    {
                        yield return $@"{e(Jar)} uvf {e(ProjectTargetName + "." + Suffix)} -C {e(AssetsSrcDir.Parent)} assets || exit /b 1";
                    }
                }
                yield return $@"{e(Jar)} uvf {e(ProjectTargetName + "." + Suffix)} {JniLibs} || exit /b 1";
                yield return @"";
                yield return @":: align apk/aar";
                yield return $@"{e(ZipAlign)} -f 4 {e(ProjectTargetName + "." + Suffix)} {e(ProjectTargetName + "-aligned." + Suffix)} || exit /b 1";
                yield return $@"move /Y {e(ProjectTargetName + "-aligned." + Suffix)} {e(ProjectTargetName + "." + Suffix)}";
                yield return @"";
                if (Project.TargetType == TargetType.GradleApplication)
                {
                    yield return @":: sign apk";
                    yield return $@"call {e(ApkSigner)} sign --ks {e(KeyStore)} --ks-key-alias {e(KeyAlias)} --ks-pass pass:{e(KeyStorePass)} --key-pass pass:{e(KeyPass)} {e(ProjectTargetName + "." + Suffix)} || exit /b 1";
                    yield return @"";
                }
            }
            else
            {
                Func<String, String> e = s => Shell.EscapeArgumentForShell(s, Shell.ShellArgumentStyle.Bash);

                var Javac = Jdk / "bin/javac";
                var Jar = Jdk / "bin/jar";
                var Aapt2 = AndroidSdk / "build-tools" / BuildToolVersion / "aapt2";
                var D8 = AndroidSdk / "build-tools" / BuildToolVersion / "d8";
                var ZipAlign = AndroidSdk / "build-tools" / BuildToolVersion / "zipalign";
                var ApkSigner = AndroidSdk / "build-tools" / BuildToolVersion / "apksigner";
                var AndroidJar = AndroidSdk / $"platforms/android-{TargetSdkVersion}" / "android.jar";
                var Strip = AndroidNdk / $"toolchains/llvm/prebuilt/{(HostOperatingSystem == OperatingSystemType.Mac ? "darwin" : "linux")}-x86_64/bin/llvm-strip";

                yield return @"#!/bin/bash";
                yield return @"set -e";
                yield return @"";
                yield return $@"[ -d {JniLibs} ] && rm -rf {JniLibs}";
                yield return @"[ -d gen ] && rm -rf gen";
                yield return @"[ -d classes ] && rm -rf classes";
                yield return @"";
                yield return @"# strip debug symbols";
                yield return $@"mkdir -p {JniLibs}/{Abi}";
                foreach (var LibPath in SoLibraryPaths)
                {
                    yield return $@"{e(Strip)} {e(LibPath)} -o {e(JniLibs.AsPath() / Abi / LibPath.FileName)} -discard-all -strip-all";
                }
                yield return @"";
                if (ResSrcDirs.Count > 0)
                {
                    yield return @"# compile resources";
                    yield return $@"{e(Aapt2)} compile {String.Join(" ", ResSrcDirs.Select(d => "--dir " + e(d)))} -o resources.zip";
                    yield return @"";
                }
                if (Project.TargetType == TargetType.GradleApplication)
                {
                    yield return @"# link resources and generate apk/aar and R.java";
                    yield return $@"{e(Aapt2)} link -o {e(ProjectTargetName + "." + Suffix)} --rename-manifest-package {e(ApplicationId)} --min-sdk-version {MinSdkVersion} --target-sdk-version {TargetSdkVersion}{(IsDebug ? " --debug-mode" : "")} -I {e(AndroidJar)} --manifest {e(ManifestSrcFile)}{(ResSrcDirs.Count == 0 ? "" : " resources.zip")} --java gen";
                    yield return @"";
                }
                else
                {
                    yield return @"# link resources and generate R.txt";
                    yield return $@"{e(Aapt2)} link -o dummy.apk -I {e(AndroidJar)} --manifest {e(ManifestSrcFile)}{(ResSrcDirs.Count == 0 ? "" : " resources.zip")} --output-text-symbols R.txt";
                    yield return $@"{e(Jar)} cvfM {e(ProjectTargetName + "." + Suffix)} R.txt";
                    yield return $@"{e(Jar)} uvf {e(ProjectTargetName + "." + Suffix)} -C {e(ManifestSrcFile.Parent)} {e(ManifestSrcFile.FileName)}";
                    foreach (var ResSrcDir in ResSrcDirs)
                    {
                        yield return $@"{e(Jar)} uvf {e(ProjectTargetName + "." + Suffix)} -C {e(ResSrcDir.Parent)} res";
                    }
                    yield return @"";
                }
                yield return @"# compile java sources";
                yield return @"mkdir classes";
                yield return @"> java_source_list.txt";
                if (Project.TargetType == TargetType.GradleApplication)
                {
                    yield return $@"find gen -type f -name *.java >> java_source_list.txt";
                }
                foreach (var JavaSrcDir in JavaSrcDirs)
                {
                    yield return $@"find {e(JavaSrcDir)} -type f -name *.java >> java_source_list.txt";
                }
                yield return $@"{e(Javac)} -g -cp {e(AndroidJar)} -encoding utf-8 -d classes{String.Join("", JavaSrcDirs.Select(d => " -sourcepath " + e(d)))} -sourcepath gen{(JarFiles.Count > 0 ? " -cp " + e(String.Join(";", JarFiles)) : "")} @java_source_list.txt";
                yield return @"";
                yield return @"# package class files to jar";
                yield return $@"{e(Jar)} cvfM classes.jar -C classes .";
                yield return @"";
                if (Project.TargetType == TargetType.GradleApplication)
                {
                    yield return @"# convert jar to dex";
                    yield return $@"{e(D8)} --min-api {MinSdkVersion} --lib {e(AndroidJar)}{(IsDebug ? " --debug" : "")} classes.jar{(JarFiles.Count > 0 ? " " + String.Join(" ", JarFiles.Select(f => e(f))) : "")}";
                    yield return @"";
                }
                yield return @"# add files to apk/aar";
                yield return $@"{e(Jar)} uvf {e(ProjectTargetName + "." + Suffix)} classes.{(Project.TargetType == TargetType.GradleApplication ? "dex" : "jar")}";
                if (Project.TargetType == TargetType.GradleApplication)
                {
                    foreach (var AssetsSrcDir in AssetsSrcDirs)
                    {
                        yield return $@"{e(Jar)} uvf {e(ProjectTargetName + "." + Suffix)} -C {e(AssetsSrcDir.Parent)} assets";
                    }
                }
                yield return $@"{e(Jar)} uvf {e(ProjectTargetName + "." + Suffix)} {JniLibs}";
                yield return @"";
                yield return @"# align apk/aar";
                yield return $@"{e(ZipAlign)} -f 4 {e(ProjectTargetName + "." + Suffix)} {e(ProjectTargetName + "-aligned." + Suffix)}";
                yield return $@"mv -f {e(ProjectTargetName + "-aligned." + Suffix)} {e(ProjectTargetName + "." + Suffix)}";
                yield return @"";
                if (Project.TargetType == TargetType.GradleApplication)
                {
                    yield return @"# sign apk";
                    yield return $@"{e(ApkSigner)} sign --ks {e(KeyStore)} --ks-key-alias {e(KeyAlias)} --ks-pass pass:{e(KeyStorePass)} --key-pass pass:{e(KeyPass)} {e(ProjectTargetName + "." + Suffix)}";
                    yield return @"";
                }
            }
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
    }
}
