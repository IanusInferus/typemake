using System;
using System.Collections.Generic;
using System.Linq;

namespace TypeMake
{
    public partial class Program
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
                    return 1;
                }
            }
        }

        public static int MainInner(String[] args)
        {
            var argv = args.Where(arg => !arg.StartsWith("--") && !arg.Contains("=")).ToArray();
            var options = args.Where(arg => arg.StartsWith("--")).Select(arg => arg.Substring(2).Split(new Char[] { ':' }, 2)).GroupBy(p => p[0]).ToDictionary(g => g.Key, g => g.Last().Skip(1).SingleOrDefault(), StringComparer.OrdinalIgnoreCase);
            var optionLists = args.Where(arg => arg.StartsWith("--")).Select(arg => arg.Substring(2).Split(new Char[] { ':' }, 2)).GroupBy(p => p[0]).ToDictionary(g => g.Key, g => g.Select(Value => Value.Skip(1).SingleOrDefault()).ToList(), StringComparer.OrdinalIgnoreCase);

            var Help = options.ContainsKey("help");
            if (Help)
            {
                DisplayInfo();
                return 0;
            }
            if (argv.Length == 1)
            {
                var RetypemakeScriptPath = argv[0].AsPath();
                RetypemakeScriptReader.Read(RetypemakeScriptPath);
            }
            else if (argv.Length != 0)
            {
                DisplayInfo();
                return 1;
            }

            foreach (var p in args.Where(arg => arg.Contains("=")).Select(arg => arg.Split('=')))
            {
                Environment.SetEnvironmentVariable(p[0], p[1]);
            }

            var Memory = new Shell.EnvironmentVariableMemory();
            var Quiet = options.ContainsKey("quiet");
            var VariablesAndVariableItems = VariableCollection.GetVariableItems();
            var vc = new ConsoleVariableCollector(Memory, Quiet, VariablesAndVariableItems.Value);
            vc.Execute();

            Generation.Run(Memory, Quiet, VariablesAndVariableItems.Key);

            Console.WriteLine("TypeMake successful.");
            return 0;
        }

        public static void DisplayInfo()
        {
            Console.WriteLine(@"TypeMake");
            Console.WriteLine(@"Usage:");
            Console.WriteLine(@"TypeMake [<RetypemakeScript>] <Variable>* [--quiet] [--help]");
            Console.WriteLine(@"RetypemakeScript batch or bash file to get environment variables for diagnostics");
            Console.WriteLine(@"Variable <Key>=<Value> additional environment variables that only take effect in the call");
            Console.WriteLine(@"--quiet no interactive variable input, all variables will be input from environment variables or use default values");
        }
    }
}
