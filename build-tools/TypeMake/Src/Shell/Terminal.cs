using System;
using System.Collections.Generic;
using System.Linq;

namespace TypeMake
{
    public interface ITerminal
    {
        String ReadLinePassword(ConsoleColor? ForegroundColor, String PromptText, bool EnableCancellation);
        String ReadLineWithSuggestion(ConsoleColor? ForegroundColor, String PromptText, Func<String, int, bool, bool, String> Suggester, bool EnableCancellation);
        void WriteLine(ConsoleColor? ForegroundColor, String Text);
        void WriteLineError(ConsoleColor? ForegroundColor, String Text);
        ITerminalCursorAndText GetCursorAndText();
        void LoadCursorAndText(ITerminalCursorAndText ct);
    }
    public interface ITerminalCursorAndText
    {
    }

    public class WindowsTerminal : ITerminal
    {
        public String ReadLinePassword(ConsoleColor? ForegroundColor, String PromptText, bool EnableCancellation)
        {
            var ct = GetCursorAndText();
            SaveColor();
            try
            {
                SetColor(null, ForegroundColor);
                Console.Write(PromptText);
                var l = new LinkedList<Char>();
                while (true)
                {
                    var ki = Console.ReadKey(true);
                    if (EnableCancellation && (ki.Key == ConsoleKey.Escape))
                    {
                        throw new Shell.UserCancelledException();
                    }
                    if (ki.Key == ConsoleKey.Enter)
                    {
                        Console.WriteLine();
                        break;
                    }
                    if (ki.Key == ConsoleKey.Backspace)
                    {
                        l.RemoveLast();
                    }
                    else
                    {
                        var c = ki.KeyChar;
                        if (Char.IsControl(c)) { continue; }
                        l.AddLast(ki.KeyChar);
                    }
                }
                return new String(l.ToArray());
            }
            finally
            {
                LoadColor();
                LoadCursorAndText(ct);
            }
        }
        public String ReadLineWithSuggestion(ConsoleColor? ForegroundColor, String PromptText, Func<String, int, bool, bool, String> Suggester, bool EnableCancellation)
        {
            var ct = GetCursorAndText();
            SaveColor();
            try
            {
                SetColor(null, ForegroundColor);
                Console.Write(PromptText);
                var Confirmed = new LinkedList<KeyValuePair<Char, KeyValuePair<int, int>>>();
                var Suggested = new LinkedList<KeyValuePair<Char, KeyValuePair<int, int>>>();
                LinkedListNode<KeyValuePair<Char, KeyValuePair<int, int>>> CurrentCharNode = null;
                int ConfirmedLastTop = Console.CursorTop;
                int ConfirmedLastLeft = Console.CursorLeft;
                int SuggestedLastTop = Console.CursorTop;
                int SuggestedLastLeft = Console.CursorLeft;
                void ClearSuggestion()
                {
                    Suggested = new LinkedList<KeyValuePair<Char, KeyValuePair<int, int>>>();
                }
                void RefreshSuggestion()
                {
                    if (Suggester == null) { return; }
                    var v = new String(Confirmed.Select(p => p.Key).Concat(Suggested.Select(p => p.Key)).ToArray());
                    var vSuggested = Suggester(v, Confirmed.Count, false, false).Substring(Confirmed.Count);
                    Suggested = new LinkedList<KeyValuePair<Char, KeyValuePair<int, int>>>(vSuggested.Select(c => new KeyValuePair<Char, KeyValuePair<int, int>>(c, new KeyValuePair<int, int>(SuggestedLastTop, SuggestedLastLeft))));
                }
                void CycleSuggestion(bool CyclePrevious)
                {
                    if (Suggester == null) { return; }
                    var v = new String(Confirmed.Select(p => p.Key).Concat(Suggested.Select(p => p.Key)).ToArray());
                    var vSuggested = Suggester(v, Confirmed.Count, true, CyclePrevious).Substring(Confirmed.Count);
                    Suggested = new LinkedList<KeyValuePair<Char, KeyValuePair<int, int>>>(vSuggested.Select(c => new KeyValuePair<Char, KeyValuePair<int, int>>(c, new KeyValuePair<int, int>(SuggestedLastTop, SuggestedLastLeft))));
                }
                void MoveSuggestionToConfirmed()
                {
                    foreach (var n in Suggested.ToList())
                    {
                        Console.Write(n.Key);
                        Confirmed.AddLast(n);
                    }
                    Suggested.Clear();
                    ConfirmedLastTop = SuggestedLastTop;
                    ConfirmedLastLeft = SuggestedLastLeft;
                    RefreshCharsAfterCursor();
                }
                void RefreshCharsAfterCursor()
                {
                    var Top = Console.CursorTop;
                    var Left = Console.CursorLeft;
                    MoveCursorToPosition(SuggestedLastTop, SuggestedLastLeft);
                    BackspaceCursorToPosition(Top, Left);
                    var Next = CurrentCharNode;
                    while (Next != null)
                    {
                        Next.Value = new KeyValuePair<Char, KeyValuePair<int, int>>(Next.Value.Key, new KeyValuePair<int, int>(Console.CursorTop, Console.CursorLeft));
                        Console.Write(Next.Value.Key);
                        Next = Next.Next;
                    }
                    ConfirmedLastTop = Console.CursorTop;
                    ConfirmedLastLeft = Console.CursorLeft;
                    Next = Suggested.First;
                    SetColor(ConsoleColor.Blue, ConsoleColor.Yellow);
                    while (Next != null)
                    {
                        Next.Value = new KeyValuePair<Char, KeyValuePair<int, int>>(Next.Value.Key, new KeyValuePair<int, int>(Console.CursorTop, Console.CursorLeft));
                        Console.Write(Next.Value.Key);
                        Next = Next.Next;
                    }
                    SetColor(null, null);
                    SuggestedLastTop = Console.CursorTop;
                    SuggestedLastLeft = Console.CursorLeft;
                    MoveCursorToPosition(Top, Left);
                }
                while (true)
                {
                    var Top = Console.CursorTop;
                    var Left = Console.CursorLeft;
                    var cps = GetConsolePositionState();
                    var ki = Console.ReadKey(true);
                    if (Console.BufferWidth != cps.BufferWidth)
                    {
                        var BackupTop = Console.CursorTop;
                        var BackupLeft = Console.CursorLeft;
                        var cpsBackup = GetConsolePositionState();
                        var BackupCharNode = CurrentCharNode;
                        SetConsolePositionState(cps);
                        if ((SuggestedLastTop > Top) || ((SuggestedLastTop == Top) && (SuggestedLastLeft > Left)))
                        {
                            MoveCursorToPosition(SuggestedLastTop, SuggestedLastLeft);
                        }
                        CurrentCharNode = Confirmed.First;
                        if (CurrentCharNode != null)
                        {
                            BackspaceCursorToPosition(CurrentCharNode.Value.Value.Key, CurrentCharNode.Value.Value.Value);
                        }
                        else
                        {
                            BackspaceCursorToPosition(SuggestedLastTop, SuggestedLastLeft);
                        }
                        SetConsolePositionState(cpsBackup);
                        SuggestedLastTop = Console.CursorTop;
                        SuggestedLastLeft = Console.CursorLeft;
                        RefreshCharsAfterCursor();
                        CurrentCharNode = BackupCharNode;
                        MoveCursorToPosition(BackupTop, BackupLeft);
                        Top = Console.CursorTop;
                        Left = Console.CursorLeft;
                    }
                    if (EnableCancellation && (ki.Key == ConsoleKey.Escape))
                    {
                        ClearSuggestion();
                        RefreshCharsAfterCursor();
                        throw new Shell.UserCancelledException();
                    }
                    if (ki.Key == ConsoleKey.Enter)
                    {
                        Console.WriteLine();
                        break;
                    }
                    if (ki.Key == ConsoleKey.LeftArrow)
                    {
                        if (CurrentCharNode == null)
                        {
                            CurrentCharNode = Confirmed.Last;
                        }
                        else
                        {
                            if (CurrentCharNode.Previous == null) { continue; }
                            CurrentCharNode = CurrentCharNode.Previous;
                        }
                        if (CurrentCharNode == null)
                        {
                            MoveCursorToPosition(ConfirmedLastTop, ConfirmedLastLeft);
                        }
                        else
                        {
                            MoveCursorToPosition(CurrentCharNode.Value.Value);
                        }
                    }
                    else if (ki.Key == ConsoleKey.RightArrow)
                    {
                        if (CurrentCharNode == null)
                        {
                            MoveSuggestionToConfirmed();
                            MoveCursorToPosition(ConfirmedLastTop, ConfirmedLastLeft);
                            continue;
                        }
                        else
                        {
                            CurrentCharNode = CurrentCharNode.Next;
                        }
                        if (CurrentCharNode == null)
                        {
                            MoveCursorToPosition(ConfirmedLastTop, ConfirmedLastLeft);
                        }
                        else
                        {
                            MoveCursorToPosition(CurrentCharNode.Value.Value);
                        }
                    }
                    else if (ki.Key == ConsoleKey.Home)
                    {
                        CurrentCharNode = Confirmed.First;
                        if (CurrentCharNode == null)
                        {
                            MoveCursorToPosition(ConfirmedLastTop, ConfirmedLastLeft);
                        }
                        else
                        {
                            MoveCursorToPosition(CurrentCharNode.Value.Value);
                        }
                    }
                    else if (ki.Key == ConsoleKey.End)
                    {
                        CurrentCharNode = null;
                        MoveCursorToPosition(ConfirmedLastTop, ConfirmedLastLeft);
                        MoveSuggestionToConfirmed();
                        MoveCursorToPosition(ConfirmedLastTop, ConfirmedLastLeft);
                    }
                    else if (ki.Key == ConsoleKey.Backspace)
                    {
                        if (CurrentCharNode == null)
                        {
                            if (Suggested.Count > 0)
                            {
                                ClearSuggestion();
                                RefreshCharsAfterCursor();
                            }
                            else if (Confirmed.Last != null)
                            {
                                MoveCursorToPosition(Confirmed.Last.Value.Value);
                                Confirmed.RemoveLast();
                                RefreshCharsAfterCursor();
                            }
                        }
                        else
                        {
                            if (CurrentCharNode.Previous != null)
                            {
                                MoveCursorToPosition(CurrentCharNode.Previous.Value.Value);
                                Confirmed.Remove(CurrentCharNode.Previous);
                                RefreshSuggestion();
                                RefreshCharsAfterCursor();
                            }
                        }
                    }
                    else if (ki.Key == ConsoleKey.Delete)
                    {
                        if (CurrentCharNode == null)
                        {
                            ClearSuggestion();
                            RefreshCharsAfterCursor();
                        }
                        else
                        {
                            var Next = CurrentCharNode.Next;
                            Confirmed.Remove(CurrentCharNode);
                            CurrentCharNode = Next;
                            RefreshSuggestion();
                            RefreshCharsAfterCursor();
                        }
                    }
                    else if (ki.Key == ConsoleKey.Tab)
                    {
                        if (ki.Modifiers == ConsoleModifiers.Shift)
                        {
                            CycleSuggestion(true);
                            RefreshCharsAfterCursor();
                        }
                        else
                        {
                            CycleSuggestion(false);
                            RefreshCharsAfterCursor();
                        }
                    }
                    else
                    {
                        var c = ki.KeyChar;
                        if (Char.IsControl(c)) { continue; }
                        if (CurrentCharNode != null)
                        {
                            Confirmed.AddBefore(CurrentCharNode, new KeyValuePair<Char, KeyValuePair<int, int>>(ki.KeyChar, new KeyValuePair<int, int>(Top, Left)));
                        }
                        else
                        {
                            Confirmed.AddLast(new KeyValuePair<Char, KeyValuePair<int, int>>(ki.KeyChar, new KeyValuePair<int, int>(Top, Left)));
                            CurrentCharNode = null;
                        }
                        Console.Write(c);
                        RefreshSuggestion();
                        RefreshCharsAfterCursor();
                    }
                }
                return new String(Confirmed.Select(p => p.Key).Concat(Suggested.Select(p => p.Key)).ToArray());
            }
            finally
            {
                LoadColor();
                LoadCursorAndText(ct);
            }
        }
        public void WriteLine(ConsoleColor? ForegroundColor, String Text)
        {
            SaveColor();
            try
            {
                SetColor(null, ForegroundColor);
                Console.WriteLine(Text);
            }
            finally
            {
                LoadColor();
            }
        }
        public void WriteLineError(ConsoleColor? ForegroundColor, String Text)
        {
            SaveColor();
            try
            {
                SetColor(null, ForegroundColor);
                Console.Error.WriteLine(Text);
            }
            finally
            {
                LoadColor();
            }
        }

        private void BackspaceCursorToLine(int Top)
        {
            BackspaceCursorToPosition(Top, 0);
        }
        private void BackspaceCursorToPosition(int Top, int Left)
        {
            while (true)
            {
                if (Console.CursorTop < Top) { break; }
                if (Console.CursorTop == Top)
                {
                    if (Console.CursorLeft <= Left) { break; }
                }
                if ((Console.CursorLeft == 0) && (Console.CursorTop > 0))
                {
                    var PrevLeft = Console.BufferWidth - 1;
                    var PrevTop = Console.CursorTop - 1;
                    MoveCursorToPosition(PrevTop, PrevLeft);
                    Console.Write(" ");
                    MoveCursorToPosition(PrevTop, PrevLeft);
                }
                else
                {
                    var Count = Console.CursorLeft - (Console.CursorTop == Top ? Left : 0);
                    Console.Write(new String('\b', Count) + new String(' ', Count) + new String('\b', Count));
                }
            }
            MoveCursorToPosition(Top, Left);
        }
        private void MoveCursorToPosition(int Top, int Left)
        {
            Console.SetCursorPosition(Math.Max(0, Math.Min(Left, Console.BufferWidth - 1)), Math.Max(0, Math.Min(Top, Console.BufferHeight - 1)));
        }
        private void MoveCursorToPosition(KeyValuePair<int, int> Pair)
        {
            MoveCursorToPosition(Pair.Key, Pair.Value);
        }
        private class ConsolePositionState
        {
            public int BufferWidth;
            public int BufferHeight;
            public int WindowWidth;
            public int WindowHeight;
            public int WindowTop;
            public int WindowLeft;
        }
        private ConsolePositionState GetConsolePositionState()
        {
            return new ConsolePositionState
            {
                BufferWidth = Console.BufferWidth,
                BufferHeight = Console.BufferHeight,
                WindowWidth = Console.WindowWidth,
                WindowHeight = Console.WindowHeight,
                WindowTop = Console.WindowTop,
                WindowLeft = Console.WindowLeft
            };
        }
        private void SetConsolePositionState(ConsolePositionState s)
        {
            Console.SetWindowSize(s.WindowWidth, s.WindowHeight);
            Console.SetWindowPosition(s.WindowLeft, s.WindowTop);
            Console.SetBufferSize(s.BufferWidth, s.BufferHeight);
        }

        private class TerminalCursorAndText : ITerminalCursorAndText
        {
            public int CursorTop;
            public ConsolePositionState cps;
        }
        public ITerminalCursorAndText GetCursorAndText()
        {
            return new TerminalCursorAndText { CursorTop = Console.CursorTop, cps = GetConsolePositionState() };
        }
        public void LoadCursorAndText(ITerminalCursorAndText ct)
        {
            var t = (TerminalCursorAndText)(ct);
            var cpsNew = GetConsolePositionState();
            SetConsolePositionState(t.cps);
            BackspaceCursorToLine(t.CursorTop);
            SetConsolePositionState(cpsNew);
        }

        private ConsoleColor? CurrentBackgroundColor = null;
        private ConsoleColor? CurrentForegroundColor = null;
        public void SetColor(ConsoleColor? BackgroundColor, ConsoleColor? ForegroundColor)
        {
            if ((CurrentBackgroundColor != BackgroundColor) || (CurrentForegroundColor != ForegroundColor))
            {
                Console.ResetColor();
                CurrentBackgroundColor = BackgroundColor;
                CurrentForegroundColor = ForegroundColor;
                if (BackgroundColor != null)
                {
                    Console.BackgroundColor = BackgroundColor.Value;
                }
                if (ForegroundColor != null)
                {
                    Console.ForegroundColor = ForegroundColor.Value;
                }
            }
        }
        private ConsoleColor? SavedBackgroundColor = null;
        private ConsoleColor? SavedForegroundColor = null;
        public void SaveColor()
        {
            SavedBackgroundColor = CurrentBackgroundColor;
            SavedForegroundColor = CurrentForegroundColor;
        }
        public void LoadColor()
        {
            SetColor(SavedBackgroundColor, SavedForegroundColor);
        }
    }

    /// <summary>
    /// Mainly based on ECMA-48 CSI sequences (https://man7.org/linux/man-pages/man4/console_codes.4.html)
    /// </summary>
    public class EscapeTerminal : ITerminal
    {
        public String ReadLinePassword(ConsoleColor? ForegroundColor, String PromptText, bool EnableCancellation)
        {
            SaveCursor();
            SaveColor();
            try
            {
                SetColor(null, ForegroundColor);
                Console.Write(PromptText);
                var l = new LinkedList<Char>();
                while (true)
                {
                    var ki = Console.ReadKey(true);
                    if (EnableCancellation && (ki.Key == ConsoleKey.Escape))
                    {
                        throw new Shell.UserCancelledException();
                    }
                    if (ki.Key == ConsoleKey.Enter)
                    {
                        Console.WriteLine();
                        break;
                    }
                    if (ki.Key == ConsoleKey.Backspace)
                    {
                        l.RemoveLast();
                    }
                    else
                    {
                        var c = ki.KeyChar;
                        if (Char.IsControl(c)) { continue; }
                        l.AddLast(ki.KeyChar);
                    }
                }
                return new String(l.ToArray());
            }
            finally
            {
                LoadColor();
                LoadCursor();
                ErasePosterior();
            }
        }
        public String ReadLineWithSuggestion(ConsoleColor? ForegroundColor, String PromptText, Func<String, int, bool, bool, String> Suggester, bool EnableCancellation)
        {
            // workaround cursor position change on input line overflow at the terminal bottom
            // https://unix.stackexchange.com/questions/565597/restore-cursor-position-after-having-saved-it
            Console.Write("\n\n\n\n\n\n\x1B[6A");

            SaveCursor();
            SaveColor();

            var Confirmed = "";
            var Index = 0;
            var Suggested = "";
            void RefreshSuggestion()
            {
                Suggested = Suggester(Confirmed + Suggested, Confirmed.Length, false, false).Substring(Confirmed.Length);
            }
            void CycleSuggestion(bool CyclePrevious)
            {
                Suggested = Suggester(Confirmed + Suggested, Confirmed.Length, true, CyclePrevious).Substring(Confirmed.Length);
            }
            void Refresh()
            {
                LoadCursor();
                ErasePosterior();

                SetColor(null, ForegroundColor);
                Console.Write(PromptText);
                Console.Write(Confirmed);
                SetColor(ConsoleColor.Blue, ConsoleColor.Yellow);
                Console.Write(Suggested);

                LoadCursor();
                SetColor(null, ForegroundColor);
                Console.Write(PromptText);
                Console.Write(Confirmed.Substring(0, Index));

                SetColor(null, null);
            }

            try
            {
                Refresh();

                while (true)
                {
                    var ki = Console.ReadKey(true);
                    if (EnableCancellation && (ki.Key == ConsoleKey.Escape))
                    {
                        Suggested = "";
                        Refresh();
                        throw new Shell.UserCancelledException();
                    }
                    if (ki.Key == ConsoleKey.Enter)
                    {
                        Confirmed = Confirmed + Suggested;
                        Suggested = "";
                        Index = Confirmed.Length;
                        Refresh();
                        Console.WriteLine();
                        break;
                    }
                    if (ki.Key == ConsoleKey.LeftArrow)
                    {
                        if (Index > 0)
                        {
                            Index -= 1;
                            Refresh();
                        }
                    }
                    else if (ki.Key == ConsoleKey.RightArrow)
                    {
                        if ((Index == Confirmed.Length) && (Suggested != ""))
                        {
                            Confirmed = Confirmed + Suggested;
                            Suggested = "";
                            Index = Confirmed.Length;
                            Refresh();
                        }
                        else if (Index < Confirmed.Length)
                        {
                            Index += 1;
                            Refresh();
                        }
                    }
                    else if (ki.Key == ConsoleKey.Home)
                    {
                        if (Index > 0)
                        {
                            Index = 0;
                            Refresh();
                        }
                    }
                    else if (ki.Key == ConsoleKey.End)
                    {
                        if ((Index == Confirmed.Length) && (Suggested != ""))
                        {
                            Confirmed = Confirmed + Suggested;
                            Suggested = "";
                            Index = Confirmed.Length;
                            Refresh();
                        }
                        else if (Index < Confirmed.Length)
                        {
                            Index = Confirmed.Length;
                            Refresh();
                        }
                    }
                    else if (ki.Key == ConsoleKey.Backspace)
                    {
                        if ((Index == Confirmed.Length) && (Suggested != ""))
                        {
                            Suggested = "";
                            Refresh();
                        }
                        else if (Index > 0)
                        {
                            Confirmed = Confirmed.Substring(0, Index - 1) + Confirmed.Substring(Index);
                            Index -= 1;
                            Refresh();
                        }
                    }
                    else if (ki.Key == ConsoleKey.Delete)
                    {
                        if (Index < Confirmed.Length)
                        {
                            Confirmed = Confirmed.Substring(0, Index) + Confirmed.Substring(Index + 1);
                            RefreshSuggestion();
                            Refresh();
                        }
                        else if ((Index == Confirmed.Length) && (Suggested != ""))
                        {
                            Suggested = "";
                            Refresh();
                        }
                    }
                    else if (ki.Key == ConsoleKey.Tab)
                    {
                        if (ki.Modifiers == ConsoleModifiers.Shift)
                        {
                            CycleSuggestion(true);
                            Refresh();
                        }
                        else
                        {
                            CycleSuggestion(false);
                            Refresh();
                        }
                    }
                    else
                    {
                        var c = ki.KeyChar;
                        if (Char.IsControl(c)) { continue; }
                        Confirmed = Confirmed.Substring(0, Index) + c + Confirmed.Substring(Index);
                        Index += 1;
                        RefreshSuggestion();
                        Refresh();
                    }
                }
                return Confirmed + Suggested;
            }
            finally
            {
                LoadColor();
                LoadCursor();
                ErasePosterior();
            }
        }
        public void WriteLine(ConsoleColor? ForegroundColor, String Text)
        {
            SaveColor();
            try
            {
                SetColor(null, ForegroundColor);
                Console.WriteLine(Text);
            }
            finally
            {
                LoadColor();
            }
        }
        public void WriteLineError(ConsoleColor? ForegroundColor, String Text)
        {
            SaveColor();
            try
            {
                SetColor(null, ForegroundColor);
                Console.Error.WriteLine(Text);
            }
            finally
            {
                LoadColor();
            }
        }

        // Mac don't support \x1B[s and \x1B[u https://stackoverflow.com/questions/25879183/can-terminal-app-be-made-to-respect-ansi-escape-codes
        private void SaveCursor()
        {
            Console.Write("\x1B" + "7");
        }
        private void LoadCursor()
        {
            Console.Write("\x1B" + "8");
        }
        private void ErasePosterior()
        {
            Console.Write("\x1B[J");
        }

        public ITerminalCursorAndText GetCursorAndText()
        {
            return null;
        }
        public void LoadCursorAndText(ITerminalCursorAndText ct)
        {
        }

        private int GetColorCode(ConsoleColor Color, bool Back)
        {
            if (!Back)
            {
                if (Color == ConsoleColor.Black) { return 30; }
                if (Color == ConsoleColor.Red) { return 31; }
                if (Color == ConsoleColor.Green) { return 32; }
                if (Color == ConsoleColor.Yellow) { return 33; }
                if (Color == ConsoleColor.Blue) { return 34; }
                if (Color == ConsoleColor.Magenta) { return 35; }
                if (Color == ConsoleColor.Cyan) { return 36; }
                if (Color == ConsoleColor.White) { return 37; }
                if (Color == ConsoleColor.DarkGray) { return 30; }
                if (Color == ConsoleColor.DarkRed) { return 31; }
                if (Color == ConsoleColor.DarkGreen) { return 32; }
                if (Color == ConsoleColor.DarkYellow) { return 33; }
                if (Color == ConsoleColor.DarkBlue) { return 34; }
                if (Color == ConsoleColor.DarkMagenta) { return 35; }
                if (Color == ConsoleColor.DarkCyan) { return 36; }
                if (Color == ConsoleColor.Gray) { return 37; }
                throw new InvalidOperationException();
            }
            else
            {
                if (Color == ConsoleColor.Black) { return 40; }
                if (Color == ConsoleColor.Red) { return 41; }
                if (Color == ConsoleColor.Green) { return 42; }
                if (Color == ConsoleColor.Yellow) { return 43; }
                if (Color == ConsoleColor.Blue) { return 44; }
                if (Color == ConsoleColor.Magenta) { return 45; }
                if (Color == ConsoleColor.Cyan) { return 46; }
                if (Color == ConsoleColor.White) { return 47; }
                if (Color == ConsoleColor.DarkGray) { return 40; }
                if (Color == ConsoleColor.DarkRed) { return 41; }
                if (Color == ConsoleColor.DarkGreen) { return 42; }
                if (Color == ConsoleColor.DarkYellow) { return 43; }
                if (Color == ConsoleColor.DarkBlue) { return 44; }
                if (Color == ConsoleColor.DarkMagenta) { return 45; }
                if (Color == ConsoleColor.DarkCyan) { return 46; }
                if (Color == ConsoleColor.Gray) { return 47; }
                throw new InvalidOperationException();
            }
        }
        private ConsoleColor? CurrentBackgroundColor = null;
        private ConsoleColor? CurrentForegroundColor = null;
        public void SetColor(ConsoleColor? BackgroundColor, ConsoleColor? ForegroundColor)
        {
            if ((CurrentBackgroundColor != BackgroundColor) || (CurrentForegroundColor != ForegroundColor))
            {
                Console.Write($"\x1B[0m");
                CurrentBackgroundColor = BackgroundColor;
                CurrentForegroundColor = ForegroundColor;
                if (BackgroundColor != null)
                {
                    Console.Write($"\x1B[{GetColorCode(BackgroundColor.Value, true)}m");
                }
                if (ForegroundColor != null)
                {
                    Console.Write($"\x1B[{GetColorCode(ForegroundColor.Value, false)}m");
                }
            }
        }
        private ConsoleColor? SavedBackgroundColor = null;
        private ConsoleColor? SavedForegroundColor = null;
        public void SaveColor()
        {
            SavedBackgroundColor = CurrentBackgroundColor;
            SavedForegroundColor = CurrentForegroundColor;
        }
        public void LoadColor()
        {
            SetColor(SavedBackgroundColor, SavedForegroundColor);
        }
    }

    public class SimpleTerminal : ITerminal
    {
        public String ReadLinePassword(ConsoleColor? ForegroundColor, String PromptText, bool EnableCancellation)
        {
            Console.Write(PromptText);
            return Console.ReadLine();
        }
        public String ReadLineWithSuggestion(ConsoleColor? ForegroundColor, String PromptText, Func<String, int, bool, bool, String> Suggester, bool EnableCancellation)
        {
            Console.Write(PromptText);
            return Console.ReadLine();
        }
        public void WriteLine(ConsoleColor? ForegroundColor, String Text)
        {
            Console.WriteLine(Text);
        }
        public void WriteLineError(ConsoleColor? ForegroundColor, String Text)
        {
            Console.Error.WriteLine(Text);
        }
        public ITerminalCursorAndText GetCursorAndText()
        {
            return null;
        }
        public void LoadCursorAndText(ITerminalCursorAndText ct)
        {
        }
    }
}
