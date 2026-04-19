using System;
using System.IO;
using System.Windows;
using HRS.Services;

namespace HRS
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            this.DispatcherUnhandledException += (s, args) =>
            {
                System.IO.File.WriteAllText("crash.log", args.Exception.ToString() + "\n" + (args.Exception.InnerException?.ToString() ?? ""));
                System.Windows.MessageBox.Show("Crash: " + args.Exception.Message);
                args.Handled = true;
            };

            // Initialize the DataStore just as Program.cs used to do
            DataStore.Load();
        }
    }
}
