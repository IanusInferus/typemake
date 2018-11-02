﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace TypeMake
{
    public class Program
    {
        public static int Main(String[] args)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                return MainInner(args);
            }
            else
            {
                try
                {
                    return MainInner(args);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    return -1;
                }
            }
        }
        public static int MainInner(String[] args)
        {
            var BuildingOperatingSystem = Cpp.OperatingSystemType.Windows; //TODO

            var argv = args.Where(arg => !arg.StartsWith("--")).ToArray();
            var options = args.Where(arg => arg.StartsWith("--")).Select(arg => arg.Substring(2).Split(new Char[] { ':' }, 2)).GroupBy(p => p[0]).ToDictionary(g => g.Key, g => g.Last().Skip(1).SingleOrDefault(), StringComparer.OrdinalIgnoreCase);
            var ForceRegenerate = options.ContainsKey("regen");
            var EnableNonTargetingOperatingSystemDummy = options.ContainsKey("dummy");
            if (argv.Length >= 1)
            {
                var Target = args[0];
                if ((Target == "win") || (Target == "windows"))
                {
                    if (argv.Length == 3)
                    {
                        var SourceDirectory = argv[1];
                        var BuildDirectory = argv[2];

                        var m = new Make(Cpp.ToolchainType.Windows_VisualC, Cpp.CompilerType.VisualC, BuildingOperatingSystem, Cpp.OperatingSystemType.Windows, null, SourceDirectory, BuildDirectory, ForceRegenerate, EnableNonTargetingOperatingSystemDummy);
                        m.Execute();
                        return 0;
                    }
                }
                else if (Target == "linux")
                {
                    if (argv.Length == 3)
                    {
                        var SourceDirectory = argv[1];
                        var BuildDirectory = argv[2];

                        var m = new Make(Cpp.ToolchainType.CMake, Cpp.CompilerType.gcc, BuildingOperatingSystem, Cpp.OperatingSystemType.Linux, null, SourceDirectory, BuildDirectory, ForceRegenerate, EnableNonTargetingOperatingSystemDummy);
                        m.Execute();
                        return 0;
                    }
                }
                else if (Target == "mac")
                {
                    if (argv.Length == 3)
                    {
                        var SourceDirectory = argv[1];
                        var BuildDirectory = argv[2];

                        var m = new Make(Cpp.ToolchainType.Mac_XCode, Cpp.CompilerType.clang, BuildingOperatingSystem, Cpp.OperatingSystemType.Mac, null, SourceDirectory, BuildDirectory, ForceRegenerate, EnableNonTargetingOperatingSystemDummy);
                        m.Execute();
                        return 0;
                    }
                }
                else if (Target == "ios")
                {
                    if (argv.Length == 3)
                    {
                        var SourceDirectory = argv[1];
                        var BuildDirectory = argv[2];

                        var m = new Make(Cpp.ToolchainType.Mac_XCode, Cpp.CompilerType.clang, BuildingOperatingSystem, Cpp.OperatingSystemType.iOS, null, SourceDirectory, BuildDirectory, ForceRegenerate, EnableNonTargetingOperatingSystemDummy);
                        m.Execute();
                        return 0;
                    }
                }
                else if (Target == "android")
                {
                    if (argv.Length == 3)
                    {
                        var SourceDirectory = argv[1];
                        var BuildDirectory = argv[2];

                        //TODO: command line call

                        //TODO: option input
                        //TODO: sdk/ndk path
                        //TODO: ArchitectureType
                        //TODO: automatic build
                        //TODO: quiet mode/non-interactive mode

                        //TODO: create remake script for all targets

                        var m = new Make(Cpp.ToolchainType.Gradle_CMake, Cpp.CompilerType.clang, BuildingOperatingSystem, Cpp.OperatingSystemType.Android, Cpp.ArchitectureType.armeabi_v7a, SourceDirectory, BuildDirectory, ForceRegenerate, EnableNonTargetingOperatingSystemDummy);
                        m.Execute();
                        return 0;
                    }
                }
            }
            else
            {
                DisplayInfo();
                return 0;
            }

            DisplayInfo();
            return 1;
        }

        public static void DisplayInfo()
        {
            Console.WriteLine(@"TypeMake");
            Console.WriteLine(@"Usage:");
            Console.WriteLine(@"TypeMake <Target> <SourceDirectory> <BuildDirectory> [/rebuild]");
            Console.WriteLine(@"Example:");
            Console.WriteLine(@"TypeMake win C:\Project\TypeMake\Repo C:\Project\TypeMake\Repo\build\vc15");
        }
    }
}
