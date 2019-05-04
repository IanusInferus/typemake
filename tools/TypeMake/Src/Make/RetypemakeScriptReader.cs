using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

namespace TypeMake
{
    public static class RetypemakeScriptReader
    {
        public static void Read(PathString RetypemakeScriptPath)
        {
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
    }
}
