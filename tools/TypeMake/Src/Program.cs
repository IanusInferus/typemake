using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

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
                String[] Lines;
                Regex rVariable;
                if (RetypemakeScriptPath.Extension.ToLowerInvariant() == "cmd")
                {
                    Lines = File.ReadAllLines(RetypemakeScriptPath, System.Text.Encoding.Default);
                    rVariable = new Regex(@"^set\s+(""(?<Key>[^=]+)=(?<Value>.*)""|(?<Key>[^=]+)=(?<Value>.*))\s*$");
                }
                else if (RetypemakeScriptPath.Extension.ToLowerInvariant() == "sh")
                {
                    Lines = File.ReadAllLines(RetypemakeScriptPath, new System.Text.UTF8Encoding(false));
                    rVariable = new Regex(@"^export\s+(?<Key>[^=]+)=('(?<Value>.*)'|(?<Value>.*))\s*$");
                }
                else
                {
                    throw new InvalidOperationException("InvalidRetypemakeScript");
                }
                foreach (var Line in Lines)
                {
                    var Match = rVariable.Match(Line);
                    if (Match.Success)
                    {
                        var Key = Match.Result("${Key}");
                        var Value = Match.Result("${Value}");
                        if (Key == "BuildDirectory")
                        {
                            Environment.SetEnvironmentVariable(Key, RetypemakeScriptPath.FullPath.Parent);
                        }
                        else
                        {
                            Environment.SetEnvironmentVariable(Key, Value);
                        }
                    }
                }
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

            var Quiet = options.ContainsKey("quiet");
            Generation.Run(Quiet);
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
            Console.WriteLine(@"--quiet no interactive variable input, all variables must be input from environment variables");
        }
    }
}
