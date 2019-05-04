using System;
using System.Linq;
using Eto.Forms;

namespace TypeMakeGui
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            foreach (var p in args.Where(arg => arg.Contains("=")).Select(arg => arg.Split('=')))
            {
                Environment.SetEnvironmentVariable(p[0], p[1]);
            }
            var a = new Application(Eto.Platform.Detect);
            a.UnhandledException += (sender, e) =>
            {
                MessageBox.Show(e.ToString(), "UnhandledException", MessageBoxType.Error);
            };
            a.Run(new MainForm());
        }
    }
}