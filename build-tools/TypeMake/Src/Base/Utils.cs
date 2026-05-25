using System;
using System.Collections.Generic;
using System.Linq;

namespace TypeMake
{
    public static class Utils
    {
        public static void CheckedShellExecute(params String[] Arguments)
        {
            if (Shell.Execute(Arguments.First(), Arguments.Skip(1).ToArray()) != 0)
            {
                throw new InvalidOperationException("ErrorInExecution: " + Environment.CurrentDirectory + "$ " + String.Join(" ", Arguments.Select(a => Shell.EscapeArgument(a))));
            }
        }

        public static String CheckedGetEnvironmentVariable(String VariableName)
        {
            var Result = Environment.GetEnvironmentVariable(VariableName);
            if (Result == null)
            {
                throw new InvalidOperationException("MissingEnvironmentVariable: " + VariableName);
            }
            return Result;
        }
    }
}
