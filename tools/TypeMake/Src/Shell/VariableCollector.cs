using System;
using System.Collections.Generic;

namespace TypeMake
{
    public class VariableCollector
    {
        private List<Action<Action>> VariableFetchs = new List<Action<Action>> { };

        //parameter of Fetch: OnInteraction
        public void AddVariableFetch(Action<Action> Fetch)
        {
            VariableFetchs.Add(Fetch);
        }
        public void Execute()
        {
            var PreviousFetchWithUserInteraction = new Stack<Tuple<int, int, int, Shell.ConsolePositionState>>();
            var Index = 0;
            while (Index < VariableFetchs.Count)
            {
                try
                {
                    var Current = Tuple.Create(Index, Console.CursorTop, Console.CursorLeft, Shell.GetConsolePositionState());
                    var Fetch = VariableFetchs[Index];
                    bool Interactive = false;
                    Fetch(() => Interactive = true);
                    if (Interactive)
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
                            var cps = p.Item4;
                            var Top = p.Item2;
                            var Left = p.Item3;

                            var cpsNew = Shell.GetConsolePositionState();
                            Shell.SetConsolePositionState(cps);
                            Shell.BackspaceCursorToPosition(Top, Left);
                            Shell.SetConsolePositionState(cpsNew);
                        }
                    }
                    continue;
                }
                Index += 1;
            }
        }
    }
}
