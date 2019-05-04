using System;
using Eto.Drawing;
using Eto.Forms;

namespace TypeMakeGui
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            Title = "TypeMakeGui";
            Resizable = false;
            Maximizable = false;

            var t = new TextBox { Text = @"C:\Windows\" };

            Content = new StackLayout
            {
                Padding = 10,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Items =
                {
                    "Hello World!",
                    new TableLayout
                    {
                        Rows =
                        {
                            new TableRow
                            {
                                Cells =
                                {
                                    "Path",
                                    t,
                                    new Button((sender, e) =>
                                    {
                                        var dd = new SelectFolderDialog();
                                        if (dd.ShowDialog(this) == DialogResult.Ok)
                                        {
                                            t.Text = dd.Directory;
                                        }
                                    })
                                }
                            },
                            new TableRow
                            {
                                Cells =
                                {
                                    "Path",
                                    new TextBox(),
                                    new Button()
                                }
                            },
                            new TableRow
                            {
                                Cells =
                                {
                                    "Path",
                                    new TextBox(),
                                    new Button()
                                }
                            }
                        }
                    },
                    new Button { Text = "Generate" }
                }
            };

            if (Platform.IsWpf)
            {
                Location = new Point(-32768, -32768);
            }
        }

        protected override void OnShown(EventArgs e)
        {
            if (Platform.IsWpf)
            {
                Location = new Point((int)(Screen.WorkingArea.Width - Content.Size.Width) / 2, (int)(Screen.WorkingArea.Height - Content.Size.Height) / 2);
            }
            base.OnShown(e);
        }
    }
}
