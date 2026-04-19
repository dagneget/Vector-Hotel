using System.Drawing;

namespace HRS.UI
{
    public static class Theme
    {
        // Colors from The Nocturnal Concierge design
        public static readonly Color Surface = ColorTranslator.FromHtml("#131315");
        public static readonly Color SurfaceContainerLowest = ColorTranslator.FromHtml("#0e0e10");
        public static readonly Color SurfaceContainerLow = ColorTranslator.FromHtml("#1c1b1d");
        public static readonly Color SurfaceContainerHigh = ColorTranslator.FromHtml("#2a2a2c");
        public static readonly Color SurfaceContainerHighest = ColorTranslator.FromHtml("#353437");
        public static readonly Color SurfaceBright = ColorTranslator.FromHtml("#39393b");
        
        public static readonly Color Primary = ColorTranslator.FromHtml("#b0c6ff");
        public static readonly Color PrimaryContainer = ColorTranslator.FromHtml("#568cff");
        public static readonly Color OnPrimary = ColorTranslator.FromHtml("#002c6f");
        public static readonly Color OnSurface = ColorTranslator.FromHtml("#e5e1e4");
        public static readonly Color OnSurfaceVariant = ColorTranslator.FromHtml("#c0c6d6");
        public static readonly Color OnTertiaryContainer = ColorTranslator.FromHtml("#00311f");
        
        public static readonly Color SecondaryContainer = ColorTranslator.FromHtml("#cedded");
        public static readonly Color OnSecondaryContainer = ColorTranslator.FromHtml("#021a2c");
        
        public static readonly Color OutlineVariant = Color.FromArgb(38, ColorTranslator.FromHtml("#414754")); // 15% opacity

        // Status Colors
        public static readonly Color Tertiary = ColorTranslator.FromHtml("#4edea3"); // Checked-in
        public static readonly Color OrangeAccent = Color.Orange; // Pending 
        public static readonly Color Secondary = ColorTranslator.FromHtml("#b9c7df"); // Reserved / Confirmed
        
        public static Font DisplayFont = new Font("Segoe UI", 24F, FontStyle.Bold);
        public static Font HeadlineFont = new Font("Segoe UI", 16F, FontStyle.Bold);
        public static Font BodyFont = new Font("Segoe UI", 10F, FontStyle.Regular);
        public static Font LabelFont = new Font("Segoe UI", 8F, FontStyle.Bold);
    }
}
