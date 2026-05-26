using System;
using System.Collections.Generic;
using System.Linq;

namespace TypeMake
{
    public static class Utils
    {
        public static void CheckedShellExecute(String ProgramPath, params String[] Arguments)
        {
            if (Shell.Execute(ProgramPath, Arguments) != 0)
            {
                throw new InvalidOperationException("ErrorInExecution: " + Environment.CurrentDirectory + "$ " + Shell.EscapeArgument(ProgramPath) + (Arguments.Length > 0 ? " " : "") + String.Join(" ", Arguments.Select(a => Shell.EscapeArgument(a))));
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
        public static String TryGetEnvironmentVariable(String VariableName, String DefaultValue)
        {
            var Result = Environment.GetEnvironmentVariable(VariableName);
            if (Result == null)
            {
                return DefaultValue;
            }
            return Result;
        }

        public static List<String> GetCmdBatchHeader()
        {
            var Lines = new List<String>();
            Lines.Add(@"@echo off");
            Lines.Add(@"");
            Lines.Add(@"setlocal");
            Lines.Add(@"REM encode with https://pscodec.puter.site/");
            Lines.Add(@"REM $ppid=(Get-CimInstance Win32_Process -Filter ""ProcessId=$PID"").ParentProcessId; $pppid=(Get-CimInstance Win32_Process -Filter ""ProcessId=$ppid"").ParentProcessId; $ppppid=(Get-CimInstance Win32_Process -Filter ""ProcessId=$pppid"").ParentProcessId; (Get-CimInstance Win32_Process -Filter ""ProcessId=$PID"").Name; (Get-CimInstance Win32_Process -Filter ""ProcessId=$ppid"").Name; (Get-CimInstance Win32_Process -Filter ""ProcessId=$pppid"").Name; (Get-CimInstance Win32_Process -Filter ""ProcessId=$ppppid"").Name");
            Lines.Add(@"for /f %%A in ('powershell -NoProfile -EncodedCommand ""JABwAHAAaQBkAD0AKABHAGUAdAAtAEMAaQBtAEkAbgBzAHQAYQBuAGMAZQAgAFcAaQBuADMAMgBfAFAAcgBvAGMAZQBzAHMAIAAtAEYAaQBsAHQAZQByACAAIgBQAHIAbwBjAGUAcwBzAEkAZAA9ACQAUABJAEQAIgApAC4AUABhAHIAZQBuAHQAUAByAG8AYwBlAHMAcwBJAGQAOwAgACQAcABwAHAAaQBkAD0AKABHAGUAdAAtAEMAaQBtAEkAbgBzAHQAYQBuAGMAZQAgAFcAaQBuADMAMgBfAFAAcgBvAGMAZQBzAHMAIAAtAEYAaQBsAHQAZQByACAAIgBQAHIAbwBjAGUAcwBzAEkAZAA9ACQAcABwAGkAZAAiACkALgBQAGEAcgBlAG4AdABQAHIAbwBjAGUAcwBzAEkAZAA7ACAAJABwAHAAcABwAGkAZAA9ACgARwBlAHQALQBDAGkAbQBJAG4AcwB0AGEAbgBjAGUAIABXAGkAbgAzADIAXwBQAHIAbwBjAGUAcwBzACAALQBGAGkAbAB0AGUAcgAgACIAUAByAG8AYwBlAHMAcwBJAGQAPQAkAHAAcABwAGkAZAAiACkALgBQAGEAcgBlAG4AdABQAHIAbwBjAGUAcwBzAEkAZAA7ACAAKABHAGUAdAAtAEMAaQBtAEkAbgBzAHQAYQBuAGMAZQAgAFcAaQBuADMAMgBfAFAAcgBvAGMAZQBzAHMAIAAtAEYAaQBsAHQAZQByACAAIgBQAHIAbwBjAGUAcwBzAEkAZAA9ACQAUABJAEQAIgApAC4ATgBhAG0AZQA7ACAAKABHAGUAdAAtAEMAaQBtAEkAbgBzAHQAYQBuAGMAZQAgAFcAaQBuADMAMgBfAFAAcgBvAGMAZQBzAHMAIAAtAEYAaQBsAHQAZQByACAAIgBQAHIAbwBjAGUAcwBzAEkAZAA9ACQAcABwAGkAZAAiACkALgBOAGEAbQBlADsAIAAoAEcAZQB0AC0AQwBpAG0ASQBuAHMAdABhAG4AYwBlACAAVwBpAG4AMwAyAF8AUAByAG8AYwBlAHMAcwAgAC0ARgBpAGwAdABlAHIAIAAiAFAAcgBvAGMAZQBzAHMASQBkAD0AJABwAHAAcABpAGQAIgApAC4ATgBhAG0AZQA7ACAAKABHAGUAdAAtAEMAaQBtAEkAbgBzAHQAYQBuAGMAZQAgAFcAaQBuADMAMgBfAFAAcgBvAGMAZQBzAHMAIAAtAEYAaQBsAHQAZQByACAAIgBQAHIAbwBjAGUAcwBzAEkAZAA9ACQAcABwAHAAcABpAGQAIgApAC4ATgBhAG0AZQA=""') do (");
            Lines.Add(@"REM echo %%A");
            Lines.Add(@"set LAUNCHER=%%A");
            Lines.Add(@")");
            Lines.Add(@"");
            Lines.Add(@"if ""%SUB_NO_PAUSE_SYMBOL%""==""1"" set NO_PAUSE_SYMBOL=1");
            Lines.Add(@"if /I NOT ""%LAUNCHER%""==""explorer.exe"" set NO_PAUSE_SYMBOL=1");
            Lines.Add(@"set SUB_NO_PAUSE_SYMBOL=1");
            Lines.Add(@"call :main %*");
            Lines.Add(@"set EXIT_CODE=%ERRORLEVEL%");
            Lines.Add(@"if not ""%NO_PAUSE_SYMBOL%""==""1"" pause");
            Lines.Add(@"exit /b %EXIT_CODE%");
            Lines.Add(@"");
            Lines.Add(@":main");
            return Lines;
        }
    }
}
