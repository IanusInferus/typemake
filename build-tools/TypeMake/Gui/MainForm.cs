using System;
using System.Collections.Generic;
using System.Linq;
using Eto.Drawing;
using Eto.Forms;
using TypeMake;

namespace TypeMakeGui
{
    public partial class MainForm : Form
    {
        private Shell.EnvironmentVariableMemory ManualMemory;
        private Shell.EnvironmentVariableMemory FullMemory;
        private Variables Variables;
        private List<VariableItem> SortedVariableItems;
        private HashSet<String> ValidatedVariableNames;

        public MainForm()
        {
            Title = "TypeMake";

            var VariablesAndVariableItems = VariableCollection.GetVariableItems();
            Variables = VariablesAndVariableItems.Key;
            SortedVariableItems = GetSortedVariableItems(VariablesAndVariableItems.Value);

            ManualMemory = new Shell.EnvironmentVariableMemory();
            FullMemory = new Shell.EnvironmentVariableMemory();
            ValidatedVariableNames = new HashSet<String>();

            RebuildView();

            if (Platform.IsWpf)
            {
                Location = new Point(-32768, -32768);
            }
        }

        private List<VariableItem> GetSortedVariableItems(List<VariableItem> Items)
        {
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
            return Items.PartialOrderBy(i => i.DependentVariableNames.Select(n => Dict[n])).ToList();
        }

        protected override void OnShown(EventArgs e)
        {
            if (Platform.IsWpf)
            {
                Location = new Point((int)(Screen.WorkingArea.Width - Content.Size.Width) / 2, (int)(Screen.WorkingArea.Height - Content.Size.Height) / 2);
            }
            if ((Width < 64) || (Height < 64) || (Location.X < 0) || (Location.Y < 0))
            {
                Maximize();
            }
            base.OnShown(e);
        }

        private void RebuildView()
        {
            Point? ScrollPosition = null;
            if (Content != null)
            {
                ScrollPosition = ((Scrollable)Content).ScrollPosition;
            }

            var Button_Load = new Button { Text = "&Load" };
            Button_Load.Click += (sender, e) =>
            {
                var ofd = new OpenFileDialog();
                ofd.CheckFileExists = true;
                ofd.Filters.Add(new FileFilter("Retypemake Script", "retypemake.cmd", "retypemake.sh"));
                if (ofd.ShowDialog(this) == DialogResult.Ok)
                {
                    RetypemakeScriptReader.Read(ofd.FileName);

                    ManualMemory = new Shell.EnvironmentVariableMemory();
                    FullMemory = new Shell.EnvironmentVariableMemory();
                    ValidatedVariableNames = new HashSet<String>();

                    RebuildView();
                }
                ofd.Dispose();
            };

            var TableLayout_VariableGrid = new TableLayout();
            TableLayout_VariableGrid.Spacing = new Size(5, 5);
            foreach (var i in SortedVariableItems)
            {
                var r = new TableRow();
                if (RebuildVariableRow(i, TableLayout_VariableGrid, r))
                {
                    TableLayout_VariableGrid.Rows.Add(r);
                }
            }

            var Button_Generate = new Button { Text = "&Generate", Enabled = SortedVariableItems.Select(i => i.VariableName).Except(ValidatedVariableNames).Count() == 0 };
            Button_Generate.Click += (sender, e) => Generate();

            Content = new Scrollable
            {
                ExpandContentWidth = true,
                ExpandContentHeight = true,
                Content = new StackLayout
                {
                    Padding = 10,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    Items =
                    {
                        Button_Load,
                        TableLayout_VariableGrid,
                        Button_Generate
                    }
                }
            };

            if (ScrollPosition != null)
            {
                ((Scrollable)Content).ScrollPosition = ScrollPosition.Value;
            }
        }

        private static readonly int TextBoxWidth = 1400;
        private bool RebuildVariableRow(VariableItem i, TableLayout tl, TableRow r)
        {
            r.Cells.Clear();
            r.Cells.Add(new Label { Text = i.VariableName, VerticalAlignment = VerticalAlignment.Center, Height = 24 });

            if (!i.DependentVariableNames.All(di => ValidatedVariableNames.Contains(di)))
            {
                r.Cells.Add(new TextBox { Text = "?", ReadOnly = true, BackgroundColor = Colors.LightGrey, Width = TextBoxWidth });
                r.Cells.Add("");
                r.Cells.Add(new Label { Text = "✗", TextColor = Colors.Red, VerticalAlignment = VerticalAlignment.Center });
                return !i.IsHidden;
            }

            var s = i.GetVariableSpec();
            if (s.OnNotApply)
            {
                r.Cells.Add(new TextBox { Text = "-", ReadOnly = true, BackgroundColor = Colors.LightGrey, Width = TextBoxWidth });
                r.Cells.Add("");
                r.Cells.Add("");
                ValidatedVariableNames.Add(i.VariableName);
                return !i.IsHidden;
            }
            else if (s.OnFixed)
            {
                r.Cells.Add(new TextBox { Text = GetVariableValueString(s.Fixed), ReadOnly = true, BackgroundColor = Colors.LightGrey, Width = TextBoxWidth });
                r.Cells.Add("");
                r.Cells.Add("");
                i.SetVariableValue(s.Fixed);
                ValidatedVariableNames.Add(i.VariableName);
                return !i.IsHidden;
            }

            if (s.OnBoolean)
            {
                var cb = new CheckBox { Checked = s.Boolean.DefaultValue, Width = TextBoxWidth };
                cb.LostFocus += (sender, e) => RebuildDepedeningVariableItems(i, s, tl, r);
                cb.MouseLeave += (sender, e) => RebuildDepedeningVariableItems(i, s, tl, r);
                r.Cells.Add(cb);
                r.Cells.Add("");
                r.Cells.Add("");
            }
            else if (s.OnInteger)
            {
                var ns = new NumericStepper { Value = s.Integer.DefaultValue, Width = TextBoxWidth };
                ns.LostFocus += (sender, e) => RebuildDepedeningVariableItems(i, s, tl, r);
                ns.MouseLeave += (sender, e) => RebuildDepedeningVariableItems(i, s, tl, r);
                r.Cells.Add(ns);
                r.Cells.Add("");
                r.Cells.Add("");
            }
            else if (s.OnString)
            {
                var tb = new TextBox { Text = s.String.DefaultValue, Width = TextBoxWidth };
                tb.LostFocus += (sender, e) => RebuildDepedeningVariableItems(i, s, tl, r);
                tb.MouseLeave += (sender, e) => RebuildDepedeningVariableItems(i, s, tl, r);
                r.Cells.Add(tb);
                r.Cells.Add("");
                r.Cells.Add("");
            }
            else if (s.OnSelection)
            {
                var d = new DropDown { Width = TextBoxWidth };
                foreach (var Selection in s.Selection.Selections)
                {
                    d.Items.Add(Selection);
                }
                d.SelectedKey = s.Selection.DefaultValue;
                d.LostFocus += (sender, e) => RebuildDepedeningVariableItems(i, s, tl, r);
                d.MouseLeave += (sender, e) => RebuildDepedeningVariableItems(i, s, tl, r);
                r.Cells.Add(d);
                r.Cells.Add("");
                r.Cells.Add("");
            }
            else if (s.OnPath)
            {
                var tb = new TextBox { Text = s.Path.DefaultValue?.FullPath?.ToString() };
                tb.LostFocus += (sender, e) => RebuildDepedeningVariableItems(i, s, tl, r);
                tb.MouseLeave += (sender, e) => RebuildDepedeningVariableItems(i, s, tl, r);
                r.Cells.Add(tb);
                r.Cells.Add(new Button((sender, e) =>
                {
                    if (s.Path.IsDirectory)
                    {
                        var sfd = new SelectFolderDialog();
                        sfd.Directory = tb.Text;
                        if (sfd.ShowDialog(this) == DialogResult.Ok)
                        {
                            tb.Text = sfd.Directory;
                            RebuildDepedeningVariableItems(i, s, tl, r);
                        }
                    }
                    else
                    {
                        var ofd = new OpenFileDialog();
                        ofd.FileName = tb.Text;
                        if (ofd.ShowDialog(this) == DialogResult.Ok)
                        {
                            tb.Text = ofd.FileName;
                            RebuildDepedeningVariableItems(i, s, tl, r);
                        }
                    }
                })
                { Text = "...", Width = 40 });
                r.Cells.Add("");
            }
            else if (s.OnMultiSelection)
            {
                var l = new StackLayout();
                l.Orientation = Orientation.Horizontal;
                l.Width = TextBoxWidth;
                foreach (var Selection in s.MultiSelection.Selections)
                {
                    var cb = new CheckBox { Text = Selection, Checked = s.MultiSelection.DefaultValues.Contains(Selection) };
                    cb.LostFocus += (sender, e) => RebuildDepedeningVariableItems(i, s, tl, r);
                    cb.MouseLeave += (sender, e) => RebuildDepedeningVariableItems(i, s, tl, r);
                    l.Items.Add(cb);
                }
                r.Cells.Add(l);
                r.Cells.Add("");
                r.Cells.Add("");
            }
            else
            {
                throw new InvalidOperationException();
            }

            ReadVariables(i, s, r);
            SyncVariableValue(i, s, r, false);

            return !i.IsHidden;
        }

        private String GetVariableSpecDefaultValueString(VariableSpec s)
        {
            if (s.OnBoolean)
            {
                return s.Boolean.DefaultValue ? "True" : "False";
            }
            else if (s.OnInteger)
            {
                return s.Integer.DefaultValue.ToString();
            }
            else if (s.OnString)
            {
                return s.String.DefaultValue;
            }
            else if (s.OnSelection)
            {
                return s.Selection.DefaultValue;
            }
            else if (s.OnPath)
            {
                return s.Path.DefaultValue?.ToString() ?? "";
            }
            else if (s.OnMultiSelection)
            {
                return String.Join(" ", s.MultiSelection.DefaultValues);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        private String GetVariableValueString(VariableValue v)
        {
            if (v.OnBoolean)
            {
                return v.Boolean ? "True" : "False";
            }
            else if (v.OnInteger)
            {
                return v.Integer.ToString();
            }
            else if (v.OnString)
            {
                return v.String;
            }
            else if (v.OnPath)
            {
                return v.Path.ToString();
            }
            else if (v.OnStringSet)
            {
                return String.Join(" ", v.StringSet);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        private void ReadVariables(VariableItem i, VariableSpec s, TableRow r)
        {
            String v = null;
            if (ManualMemory.Variables.ContainsKey(i.VariableName))
            {
                v = ManualMemory.Variables[i.VariableName];
            }
            else
            {
                v = Environment.GetEnvironmentVariable(i.VariableName);
                if (v == "_EMPTY_")
                {
                    v = "";
                }
            }
            if (v == null) { return; }
            if (s.OnBoolean)
            {
                bool Value;
                if (bool.TryParse(v, out Value))
                {
                    ((CheckBox)(r.Cells[1].Control)).Checked = Value;
                }
            }
            else if (s.OnInteger)
            {
                int Value;
                if (int.TryParse(v, out Value))
                {
                    ((NumericStepper)(r.Cells[1].Control)).Value = Value;
                }
            }
            else if (s.OnString)
            {
                ((TextBox)(r.Cells[1].Control)).Text = v;
            }
            else if (s.OnSelection)
            {
                if (s.Selection.Selections.Contains(v))
                {
                    ((DropDown)(r.Cells[1].Control)).SelectedKey = v;
                }
            }
            else if (s.OnPath)
            {
                ((TextBox)(r.Cells[1].Control)).Text = v == "" ? v : v.AsPath().FullPath.ToString();
            }
            else if (s.OnMultiSelection)
            {
                var Selected = new HashSet<String>(v.Split(' ').Distinct());
                var l = (StackLayout)(r.Cells[1].Control);
                foreach (var Item in l.Items)
                {
                    var cb = (CheckBox)(Item.Control);
                    cb.Checked = Selected.Contains(cb.Text);
                }
            }
        }
        private bool SyncVariableValue(VariableItem i, VariableSpec s, TableRow r, bool ManualSet)
        {
            KeyValuePair<bool, String> ValidateResult = new KeyValuePair<bool, String>(true, "");
            VariableValue v = null;
            if (s.OnBoolean)
            {
                v = VariableValue.CreateBoolean(((CheckBox)(r.Cells[1].Control)).Checked.Value);
            }
            else if (s.OnInteger)
            {
                var Value = Convert.ToInt32(((NumericStepper)(r.Cells[1].Control)).Value);
                if (s.Integer.Validator != null)
                {
                    ValidateResult = s.Integer.Validator(Value);
                }
                v = VariableValue.CreateInteger(Value);
            }
            else if (s.OnString)
            {
                var Value = ((TextBox)(r.Cells[1].Control)).Text;
                if (s.String.Validator != null)
                {
                    ValidateResult = s.String.Validator(Value);
                }
                v = VariableValue.CreateString(Value);
            }
            else if (s.OnSelection)
            {
                var Value = ((DropDown)(r.Cells[1].Control)).SelectedKey;
                if (s.Selection.Validator != null)
                {
                    ValidateResult = s.Selection.Validator(Value);
                }
                v = VariableValue.CreateString(Value);
            }
            else if (s.OnPath)
            {
                var Text = ((TextBox)(r.Cells[1].Control)).Text;
                if (String.IsNullOrEmpty(Text))
                {
                    ValidateResult = new KeyValuePair<bool, String>(false, "Path is empty.");
                }
                else
                {
                    var Value = Text.AsPath().FullPath;
                    if (s.Path.Validator != null)
                    {
                        ValidateResult = s.Path.Validator(Value);
                    }
                    else
                    {
                        if (s.Path.IsDirectory)
                        {
                            if (System.IO.Directory.Exists(Value))
                            {
                                ValidateResult = new KeyValuePair<bool, String>(true, "");
                            }
                            else
                            {
                                ValidateResult = new KeyValuePair<bool, String>(false, "Directory not exist.");
                            }
                        }
                        else
                        {
                            if (System.IO.File.Exists(Value))
                            {
                                ValidateResult = new KeyValuePair<bool, String>(true, "");
                            }
                            else
                            {
                                ValidateResult = new KeyValuePair<bool, String>(false, "File not exist.");
                            }
                        }
                    }
                    v = VariableValue.CreatePath(Value);
                }
            }
            else if (s.OnMultiSelection)
            {
                var l = (StackLayout)(r.Cells[1].Control);
                var Value = l.Items.Select(Item => (CheckBox)Item.Control).Where(Item => Item.Checked.Value).Select(Item => Item.Text).ToList();
                if (s.MultiSelection.Validator != null)
                {
                    ValidateResult = s.MultiSelection.Validator(Value);
                }
                v = VariableValue.CreateStringSet(new HashSet<String>(Value));
            }
            else
            {
                return false;
            }

            var Updated = false;
            if (v != null)
            {
                var DefaultValue = GetVariableSpecDefaultValueString(s);
                var EnvironmentValue = Environment.GetEnvironmentVariable(i.VariableName);
                if (EnvironmentValue == "_EMPTY_")
                {
                    EnvironmentValue = "";
                }
                var Value = GetVariableValueString(v);
                if (FullMemory.Variables.ContainsKey(i.VariableName))
                {
                    Updated = FullMemory.Variables[i.VariableName] != Value;
                }
                else
                {
                    Updated = true;
                }
                if (ManualSet)
                {
                    if (EnvironmentValue != null ? (Value == EnvironmentValue) : (Value == DefaultValue))
                    {
                        if (ManualMemory.Variables.ContainsKey(i.VariableName))
                        {
                            ManualMemory.Variables.Remove(i.VariableName);
                        }
                    }
                    else
                    {
                        ManualMemory.Variables[i.VariableName] = Value;
                    }
                }
                if (s.OnSelection)
                {
                    ManualMemory.VariableSelections[i.VariableName] = s.Selection.Selections.ToList();
                }
                else if (s.OnMultiSelection)
                {
                    ManualMemory.VariableMultipleSelections[i.VariableName] = s.MultiSelection.Selections.ToList();
                }
                FullMemory.Variables[i.VariableName] = Value;
                if (s.OnSelection)
                {
                    FullMemory.VariableSelections[i.VariableName] = s.Selection.Selections.ToList();
                }
                else if (s.OnMultiSelection)
                {
                    FullMemory.VariableMultipleSelections[i.VariableName] = s.MultiSelection.Selections.ToList();
                }
            }

            var Label = (Label)(r.Cells[3].Control);
            if (ValidateResult.Key)
            {
                Label.Text = "✓";
                Label.TextColor = Colors.Green;
                Label.VerticalAlignment = VerticalAlignment.Center;
                i.SetVariableValue(v);
                ValidatedVariableNames.Add(i.VariableName);
                return Updated;
            }
            else
            {
                Label.Text = "✗";
                Label.TextColor = Colors.Red;
                Label.VerticalAlignment = VerticalAlignment.Center;
                ValidatedVariableNames.Remove(i.VariableName);
                return false;
            }
        }
        private void RebuildDepedeningVariableItems(VariableItem i, VariableSpec s, TableLayout tl, TableRow r)
        {
            if (SyncVariableValue(i, s, r, true))
            {
                //var Dict = SortedVariableItems.ToDictionary(ii => ii.VariableName);
                //foreach (var Row in tl.Rows.SkipWhile(rr => rr != r).Skip(1))
                //{
                //    var VariableName = ((Label)(Row.Cells[0].Control)).Text;
                //    if (Dict[VariableName].DependentVariableNames.Contains(i.VariableName))
                //    {
                //        RebuildVariableRow(Dict[VariableName], tl, Row);
                //    }
                //}
                RebuildView(); //workaround Eto.Forms bug that controls disappear in partial update
            }
        }

        private void Generate()
        {
            if (SortedVariableItems.Select(i => i.VariableName).Except(ValidatedVariableNames).Count() > 0) { return; }
            Generation.Run(FullMemory, false, Variables);
            if ((Variables.HostOperatingSystem == TypeMake.Cpp.OperatingSystemType.Windows) && (Variables.TargetOperatingSystem == TypeMake.Cpp.OperatingSystemType.Windows))
            {
                if (MessageBox.Show("Generation finished. Open project now?", "Generate", MessageBoxButtons.YesNo, MessageBoxType.Question) == DialogResult.Yes)
                {
                    System.Diagnostics.Process.Start(Variables.BuildDirectory / (Build.SolutionName + ".sln"));
                }
            }
            else if ((Variables.HostOperatingSystem == TypeMake.Cpp.OperatingSystemType.MacOS) && ((Variables.TargetOperatingSystem == TypeMake.Cpp.OperatingSystemType.MacOS) || (Variables.TargetOperatingSystem == TypeMake.Cpp.OperatingSystemType.iOS)))
            {
                if (MessageBox.Show("Generation finished. Open project now?", "Generate", MessageBoxButtons.YesNo, MessageBoxType.Question) == DialogResult.Yes)
                {
                    System.Diagnostics.Process.Start(Variables.BuildDirectory / (Build.SolutionName + ".xcodeproj"));
                }
            }
            else
            {
                MessageBox.Show("Generation finished.", "Generate", MessageBoxType.Information);
            }
        }
    }
}
