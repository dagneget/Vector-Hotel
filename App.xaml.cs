using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
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

            // Create unfrozen brushes that can be updated by ThemeManager
            CreateUnfrozenBrushes();

            // Initialize the DataStore just as Program.cs used to do
            DataStore.Load();
            
            // Initialize theme (applies saved theme preference)
            ThemeManager.Initialize();
        }

        private void CreateUnfrozenBrushes()
        {
            var resources = this.Resources;
            
            // Replace frozen brushes with unfrozen ones
            resources["AppBackgroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F9FAFB"));
            resources["SidebarBackgroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
            resources["CardBackgroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
            resources["CardHoverBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F3F4F6"));
            resources["TextPrimaryBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#111827"));
            resources["TextSecondaryBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280"));
            resources["DividerBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5E7EB"));
            resources["BadgeBackgroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F3F4F6"));
            resources["LuminousAccentBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1D4ED8"));
        }
    }
}
