using System;
using System.Collections.Generic;
using System.Linq;

namespace TypeMake
{
    public class ConsoleVariableCollector
    {
        private Shell.EnvironmentVariableMemory Memory;
        private bool Quiet;
        private List<VariableItem> SortedItems;
        public ConsoleVariableCollector(Shell.EnvironmentVariableMemory Memory, bool Quiet, List<VariableItem> Items)
        {
            this.Memory = Memory;
            this.Quiet = Quiet;
            var DuplicateVariableNames = Items.Select(i => i.VariableName).GroupBy(n => n).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (DuplicateVariableNames.Count > 0)
            {
                throw new ArgumentException("DuplicateVariableNames: " + String.Join(" ", DuplicateVariableNames));
            }
            var Dict = Items.ToDictionary(i => i.VariableName);
            foreach (var i in Items)
            {
                foreach (var d in i.DependentVariableNames)
                {
                    if (!Dict.ContainsKey(d))
                    {
                        throw new ArgumentException("NonexistDependency: " + i + " -> " + d);
                    }
                }
            }
            this.SortedItems = Items.PartialOrderBy(i => i.DependentVariableNames.Select(n => Dict[n])).ToList();
        }
        public void Execute()
        {
            var PreviousFetchWithUserInteraction = new Stack<Tuple<int, int, Shell.ConsolePositionState>>();
            var Index = 0;
            while (Index < SortedItems.Count)
            {
                var IsLast = Index == SortedItems.Count - 1;
                try
                {
                    var Current = Quiet ? null : Tuple.Create(Index, Console.CursorTop, Shell.GetConsolePositionState());
                    var i = SortedItems[Index];
                    bool Interactive = false;
                    var s = i.GetVariableSpec();
                    if (s.OnNotApply)
                    {
                    }
                    else if (s.OnFixed)
                    {
                        if (!i.IsHidden)
                        {
                            String str;
                            if (s.Fixed.OnBoolean)
                            {
                                str = s.Fixed.Boolean ? "True" : "False";
                            }
                            else if (s.Fixed.OnInteger)
                            {
                                str = s.Fixed.Integer.ToString();
                            }
                            else if (s.Fixed.OnString)
                            {
                                str = s.Fixed.String;
                            }
                            else if (s.Fixed.OnPath)
                            {
                                str = s.Fixed.Path.ToString();
                            }
                            else if (s.Fixed.OnStringSet)
                            {
                                str = String.Join(" ", s.Fixed.StringSet);
                            }
                            else
                            {
                                throw new InvalidOperationException();
                            }
                            Console.WriteLine(i.VariableName + "=" + str);
                        }
                        i.SetVariableValue(s.Fixed);
                    }
                    else if (s.OnBoolean)
                    {
                        var v = Shell.RequireEnvironmentVariableBoolean(Memory, i.VariableName, Quiet, s.Boolean.DefaultValue, Options =>
                        {
                            Options.InputDisplay = s.Boolean.InputDisplay ?? Options.InputDisplay;
                            Options.ForegroundColor = IsLast ? ConsoleColor.Cyan : (ConsoleColor?)null;
                            Options.OnInteraction = () => Interactive = true;
                        });
                        i.SetVariableValue(VariableValue.CreateBoolean(v));
                    }
                    else if (s.OnInteger)
                    {
                        var v = Shell.RequireEnvironmentVariable(Memory, i.VariableName, new Shell.EnvironmentVariableReadOptions
                        {
                             Quiet = Quiet,
                             DefaultValue = s.Integer.DefaultValue.ToString(),
                             InputDisplay = s.Integer.InputDisplay,
                             Validator = str =>
                             {
                                 if (int.TryParse(str, out var intValue))
                                 {
                                     if (s.Integer.Validator != null)
                                     {
                                         return s.Integer.Validator(intValue);
                                     }
                                     else
                                     {
                                         return new KeyValuePair<bool, String>(true, "");
                                     }
                                 }
                                 else
                                 {
                                     return new KeyValuePair<bool, String>(false, "Parse error.");
                                 }
                             },
                            ForegroundColor = IsLast ? ConsoleColor.Cyan : (ConsoleColor?)null,
                            OnInteraction = () => Interactive = true
                        });
                        i.SetVariableValue(VariableValue.CreateInteger(int.Parse(v)));
                    }
                    else if (s.OnString)
                    {
                        var v = Shell.RequireEnvironmentVariable(Memory, i.VariableName, new Shell.EnvironmentVariableReadOptions
                        {
                            Quiet = Quiet,
                            DefaultValue = s.String.DefaultValue,
                            InputDisplay = s.String.InputDisplay,
                            IsPassword = s.String.IsPassword,
                            Validator = s.String.Validator,
                            ForegroundColor = IsLast ? ConsoleColor.Cyan : (ConsoleColor?)null,
                            OnInteraction = () => Interactive = true
                        });
                        i.SetVariableValue(VariableValue.CreateString(v));
                    }
                    else if (s.OnSelection)
                    {
                        var v = Shell.RequireEnvironmentVariableSelection(Memory, i.VariableName, Quiet, s.Selection.Selections, s.Selection.DefaultValue, Options =>
                        {
                            Options.InputDisplay = s.Selection.InputDisplay ?? Options.InputDisplay;
                            var OriginalValidator = Options.Validator;
                            var OriginalPostMapper = Options.PostMapper;
                            Options.Validator = str =>
                            {
                                if (OriginalValidator != null)
                                {
                                    var result = OriginalValidator(str);
                                    if (!result.Key) { return result; }
                                }
                                if (OriginalPostMapper != null)
                                {
                                    str = OriginalPostMapper(str);
                                }
                                if (s.Selection.Validator != null)
                                {
                                    var result = s.Selection.Validator(str);
                                    if (!result.Key) { return result; }
                                }
                                return new KeyValuePair<bool, String>(true, "");
                            };
                            Options.PostMapper = str =>
                            {
                                if (OriginalPostMapper != null)
                                {
                                    str = OriginalPostMapper(str);
                                }
                                if (s.Selection.PostMapper != null)
                                {
                                    str = s.Selection.PostMapper(str);
                                }
                                return str;
                            };
                            Options.ForegroundColor = IsLast ? ConsoleColor.Cyan : (ConsoleColor?)null;
                            Options.OnInteraction = () => Interactive = true;
                        });
                        i.SetVariableValue(VariableValue.CreateString(v));
                    }
                    else if (s.OnPath)
                    {
                        var v = s.Path.IsDirectory ? Shell.RequireEnvironmentVariableDirectoryPath(Memory, i.VariableName, Quiet, s.Path.DefaultValue, s.Path.Validator, Options =>
                        {
                            Options.InputDisplay = s.Path.InputDisplay ?? Options.InputDisplay;
                            Options.ForegroundColor = IsLast ? ConsoleColor.Cyan : (ConsoleColor?)null;
                            Options.OnInteraction = () => Interactive = true;
                        }) : Shell.RequireEnvironmentVariableFilePath(Memory, i.VariableName, Quiet, s.Path.DefaultValue, s.Path.Validator, Options =>
                        {
                            Options.InputDisplay = s.Path.InputDisplay ?? Options.InputDisplay;
                            Options.ForegroundColor = IsLast ? ConsoleColor.Cyan : (ConsoleColor?)null;
                            Options.OnInteraction = () => Interactive = true;
                        });
                        i.SetVariableValue(VariableValue.CreatePath(v));
                    }
                    else if (s.OnMultiSelection)
                    {
                        var v = Shell.RequireEnvironmentVariableMultipleSelection(Memory, i.VariableName, Quiet, s.MultiSelection.Selections, s.MultiSelection.DefaultValues, s.MultiSelection.Validator, Options =>
                        {
                            Options.InputDisplay = s.MultiSelection.InputDisplay ?? Options.InputDisplay;
                            Options.ForegroundColor = IsLast ? ConsoleColor.Cyan : (ConsoleColor?)null;
                            Options.OnInteraction = () => Interactive = true;
                        });
                        i.SetVariableValue(VariableValue.CreateStringSet(new HashSet<String>(v)));
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                    if (Interactive && (Current != null))
                    {
                        PreviousFetchWithUserInteraction.Push(Current);
                    }
                }
                catch (Shell.UserCancelledException)
                {
                    if (PreviousFetchWithUserInteraction.Count > 0)
                    {
                        var p = PreviousFetchWithUserInteraction.Pop();
                        Index = p.Item1;
                        if (Shell.OperatingSystem == Shell.OperatingSystemType.Windows)
                        {
                            var cps = p.Item3;
                            var Top = p.Item2;

                            var cpsNew = Shell.GetConsolePositionState();
                            Shell.SetConsolePositionState(cps);
                            Shell.BackspaceCursorToLine(Top);
                            Shell.SetConsolePositionState(cpsNew);
                        }
                        Memory.Variables.Remove(SortedItems[Index].VariableName);
                    }
                    continue;
                }
                Index += 1;
            }
        }
    }
}
