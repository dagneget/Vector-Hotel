using MaterialDesignThemes.Wpf;
using System;
using System.Configuration;
using System.Windows;
using System.Windows.Media;

namespace HRS.Services
{
    public static class ThemeManager
    {
        private const string ThemeKey = "AppTheme";
        private static bool _isDarkMode = false;

        public static bool IsDarkMode => _isDarkMode;

        public static event Action<bool> ThemeChanged;

        static ThemeManager()
        {
            // Load saved theme preference
            var savedTheme = ConfigurationManager.AppSettings[ThemeKey];
            _isDarkMode = savedTheme?.ToLower() == "dark";
        }

        public static void Initialize()
        {
            ApplyTheme(_isDarkMode);
        }

        public static void ToggleTheme()
        {
            _isDarkMode = !_isDarkMode;
            ApplyTheme(_isDarkMode);
            SaveThemePreference();
            ThemeChanged?.Invoke(_isDarkMode);
        }

        public static void SetTheme(bool isDark)
        {
            if (_isDarkMode != isDark)
            {
                _isDarkMode = isDark;
                ApplyTheme(_isDarkMode);
                SaveThemePreference();
                ThemeChanged?.Invoke(_isDarkMode);
            }
        }

        private static void ApplyTheme(bool isDark)
        {
            var app = Application.Current;
            if (app == null) return;

            // Get the MaterialDesign theme
            var theme = app.Resources.MergedDictionaries[0] as BundledTheme;
            if (theme != null)
            {
                theme.BaseTheme = isDark ? BaseTheme.Dark : BaseTheme.Light;
            }

            // Update custom brushes
            UpdateCustomBrushes(isDark);
        }

        private static void UpdateCustomBrushes(bool isDark)
        {
            var resources = Application.Current.Resources;

            if (isDark)
            {
                // Dark Mode Colors - update existing brush colors
                UpdateBrushColor(resources, "AppBackgroundBrush", "#0F172A");
                UpdateBrushColor(resources, "SidebarBackgroundBrush", "#1E293B");
                UpdateBrushColor(resources, "CardBackgroundBrush", "#1E293B");
                UpdateBrushColor(resources, "CardHoverBrush", "#334155");
                UpdateBrushColor(resources, "TextPrimaryBrush", "#F1F5F9");
                UpdateBrushColor(resources, "TextSecondaryBrush", "#94A3B8");
                UpdateBrushColor(resources, "DividerBrush", "#334155");
                UpdateBrushColor(resources, "BadgeBackgroundBrush", "#1E293B");
                UpdateBrushColor(resources, "LuminousAccentBrush", "#3B82F6"); // Brighter Blue for Dark Mode
            }
            else
            {
                // Light Mode Colors - update existing brush colors
                UpdateBrushColor(resources, "AppBackgroundBrush", "#F9FAFB");
                UpdateBrushColor(resources, "SidebarBackgroundBrush", "#FFFFFF");
                UpdateBrushColor(resources, "CardBackgroundBrush", "#FFFFFF");
                UpdateBrushColor(resources, "CardHoverBrush", "#F3F4F6");
                UpdateBrushColor(resources, "TextPrimaryBrush", "#111827");
                UpdateBrushColor(resources, "TextSecondaryBrush", "#6B7280");
                UpdateBrushColor(resources, "DividerBrush", "#E5E7EB");
                UpdateBrushColor(resources, "BadgeBackgroundBrush", "#F3F4F6");
                UpdateBrushColor(resources, "LuminousAccentBrush", "#1D4ED8"); // Deep Blue for Light Mode
            }
        }

        private static void UpdateBrushColor(ResourceDictionary resources, string key, string colorHex)
        {
            var newColor = (Color)ColorConverter.ConvertFromString(colorHex);
            resources[key] = new SolidColorBrush(newColor);
        }

        private static void SaveThemePreference()
        {
            try
            {
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                config.AppSettings.Settings.Remove(ThemeKey);
                config.AppSettings.Settings.Add(ThemeKey, _isDarkMode ? "dark" : "light");
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
            catch { /* Silently fail if can't save */ }
        }
    }
}
