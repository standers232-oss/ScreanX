using System;
using System.Windows;
using System.Windows.Media;

namespace ScreanX.Services
{
    public static class ThemeService
    {
        public static void ApplyTheme(string themeMode, string palette)
        {
            var app = Application.Current;
            if (app == null) return;

            bool isDark = themeMode.Equals("Dark", StringComparison.OrdinalIgnoreCase);

            Color primary, onPrimary, primaryContainer, onPrimaryContainer;
            Color secondaryContainer, onSecondaryContainer;
            Color surface, surfaceContainerLowest, surfaceContainerLow, surfaceContainer, surfaceContainerHigh, surfaceContainerHighest;
            Color onSurface, onSurfaceVariant, outline, outlineVariant;
            Color inverseSurface, inverseOnSurface;

            if (isDark)
            {
                onSurface = (Color)ColorConverter.ConvertFromString("#E6E0E9");
                onSurfaceVariant = (Color)ColorConverter.ConvertFromString("#CAC4D0");
                outline = (Color)ColorConverter.ConvertFromString("#938F99");
                outlineVariant = (Color)ColorConverter.ConvertFromString("#49454F");
                inverseSurface = (Color)ColorConverter.ConvertFromString("#E6E0E9");
                inverseOnSurface = (Color)ColorConverter.ConvertFromString("#313033");

                switch (palette.ToLowerInvariant())
                {
                    case "teal":
                        primary = (Color)ColorConverter.ConvertFromString("#4CDADA");
                        onPrimary = (Color)ColorConverter.ConvertFromString("#003737");
                        primaryContainer = (Color)ColorConverter.ConvertFromString("#005050");
                        onPrimaryContainer = (Color)ColorConverter.ConvertFromString("#6FF7F6");
                        secondaryContainer = (Color)ColorConverter.ConvertFromString("#334B4B");
                        onSecondaryContainer = (Color)ColorConverter.ConvertFromString("#CCE8E6");
                        surface = (Color)ColorConverter.ConvertFromString("#0E1514");
                        surfaceContainerLowest = (Color)ColorConverter.ConvertFromString("#090F0E");
                        surfaceContainerLow = (Color)ColorConverter.ConvertFromString("#161D1C");
                        surfaceContainer = (Color)ColorConverter.ConvertFromString("#1B2221");
                        surfaceContainerHigh = (Color)ColorConverter.ConvertFromString("#252D2B");
                        surfaceContainerHighest = (Color)ColorConverter.ConvertFromString("#303836");
                        break;

                    case "blue":
                        primary = (Color)ColorConverter.ConvertFromString("#91CCFF");
                        onPrimary = (Color)ColorConverter.ConvertFromString("#003355");
                        primaryContainer = (Color)ColorConverter.ConvertFromString("#004B76");
                        onPrimaryContainer = (Color)ColorConverter.ConvertFromString("#CCE5FF");
                        secondaryContainer = (Color)ColorConverter.ConvertFromString("#374857");
                        onSecondaryContainer = (Color)ColorConverter.ConvertFromString("#D5E4F7");
                        surface = (Color)ColorConverter.ConvertFromString("#111418");
                        surfaceContainerLowest = (Color)ColorConverter.ConvertFromString("#0B0E12");
                        surfaceContainerLow = (Color)ColorConverter.ConvertFromString("#191C20");
                        surfaceContainer = (Color)ColorConverter.ConvertFromString("#1E2024");
                        surfaceContainerHigh = (Color)ColorConverter.ConvertFromString("#282A2F");
                        surfaceContainerHighest = (Color)ColorConverter.ConvertFromString("#33353A");
                        break;

                    case "orange":
                        primary = (Color)ColorConverter.ConvertFromString("#FFB5A0");
                        onPrimary = (Color)ColorConverter.ConvertFromString("#5C1D0C");
                        primaryContainer = (Color)ColorConverter.ConvertFromString("#7D2D15");
                        onPrimaryContainer = (Color)ColorConverter.ConvertFromString("#FFDBD1");
                        secondaryContainer = (Color)ColorConverter.ConvertFromString("#55413D");
                        onSecondaryContainer = (Color)ColorConverter.ConvertFromString("#F5DDD7");
                        surface = (Color)ColorConverter.ConvertFromString("#1A1211");
                        surfaceContainerLowest = (Color)ColorConverter.ConvertFromString("#140C0B");
                        surfaceContainerLow = (Color)ColorConverter.ConvertFromString("#221A19");
                        surfaceContainer = (Color)ColorConverter.ConvertFromString("#271F1E");
                        surfaceContainerHigh = (Color)ColorConverter.ConvertFromString("#322928");
                        surfaceContainerHighest = (Color)ColorConverter.ConvertFromString("#3D3433");
                        break;

                    default: // Purple
                        primary = (Color)ColorConverter.ConvertFromString("#D0BCFF");
                        onPrimary = (Color)ColorConverter.ConvertFromString("#381E72");
                        primaryContainer = (Color)ColorConverter.ConvertFromString("#4F378B");
                        onPrimaryContainer = (Color)ColorConverter.ConvertFromString("#EADDFF");
                        secondaryContainer = (Color)ColorConverter.ConvertFromString("#4A4458");
                        onSecondaryContainer = (Color)ColorConverter.ConvertFromString("#E8DEF8");
                        surface = (Color)ColorConverter.ConvertFromString("#141218");
                        surfaceContainerLowest = (Color)ColorConverter.ConvertFromString("#0F0D13");
                        surfaceContainerLow = (Color)ColorConverter.ConvertFromString("#1D1B20");
                        surfaceContainer = (Color)ColorConverter.ConvertFromString("#211F26");
                        surfaceContainerHigh = (Color)ColorConverter.ConvertFromString("#2B2930");
                        surfaceContainerHighest = (Color)ColorConverter.ConvertFromString("#36343B");
                        break;
                }
            }
            else // Light mode
            {
                onSurface = (Color)ColorConverter.ConvertFromString("#1D1B20");
                onSurfaceVariant = (Color)ColorConverter.ConvertFromString("#49454F");
                outline = (Color)ColorConverter.ConvertFromString("#79747E");
                outlineVariant = (Color)ColorConverter.ConvertFromString("#CAC4D0");
                inverseSurface = (Color)ColorConverter.ConvertFromString("#313033");
                inverseOnSurface = (Color)ColorConverter.ConvertFromString("#F4EFF4");

                switch (palette.ToLowerInvariant())
                {
                    case "teal":
                        primary = (Color)ColorConverter.ConvertFromString("#006A6A");
                        onPrimary = Colors.White;
                        primaryContainer = (Color)ColorConverter.ConvertFromString("#6FF7F6");
                        onPrimaryContainer = (Color)ColorConverter.ConvertFromString("#002020");
                        secondaryContainer = (Color)ColorConverter.ConvertFromString("#CCE8E6");
                        onSecondaryContainer = (Color)ColorConverter.ConvertFromString("#052020");
                        surface = (Color)ColorConverter.ConvertFromString("#F4FBF9");
                        surfaceContainerLowest = Colors.White;
                        surfaceContainerLow = (Color)ColorConverter.ConvertFromString("#EEF5F3");
                        surfaceContainer = (Color)ColorConverter.ConvertFromString("#E8EFED");
                        surfaceContainerHigh = (Color)ColorConverter.ConvertFromString("#E2EAE7");
                        surfaceContainerHighest = (Color)ColorConverter.ConvertFromString("#DCE4E1");
                        break;

                    case "blue":
                        primary = (Color)ColorConverter.ConvertFromString("#00639B");
                        onPrimary = Colors.White;
                        primaryContainer = (Color)ColorConverter.ConvertFromString("#CCE5FF");
                        onPrimaryContainer = (Color)ColorConverter.ConvertFromString("#001D32");
                        secondaryContainer = (Color)ColorConverter.ConvertFromString("#D5E4F7");
                        onSecondaryContainer = (Color)ColorConverter.ConvertFromString("#0E1D2A");
                        surface = (Color)ColorConverter.ConvertFromString("#F8F9FF");
                        surfaceContainerLowest = Colors.White;
                        surfaceContainerLow = (Color)ColorConverter.ConvertFromString("#F2F4FB");
                        surfaceContainer = (Color)ColorConverter.ConvertFromString("#ECEEF5");
                        surfaceContainerHigh = (Color)ColorConverter.ConvertFromString("#E6E8EF");
                        surfaceContainerHighest = (Color)ColorConverter.ConvertFromString("#E0E2E9");
                        break;

                    case "orange":
                        primary = (Color)ColorConverter.ConvertFromString("#9C4329");
                        onPrimary = Colors.White;
                        primaryContainer = (Color)ColorConverter.ConvertFromString("#FFDBD1");
                        onPrimaryContainer = (Color)ColorConverter.ConvertFromString("#3A0B01");
                        secondaryContainer = (Color)ColorConverter.ConvertFromString("#F5DDD7");
                        onSecondaryContainer = (Color)ColorConverter.ConvertFromString("#2C1510");
                        surface = (Color)ColorConverter.ConvertFromString("#FFF8F6");
                        surfaceContainerLowest = Colors.White;
                        surfaceContainerLow = (Color)ColorConverter.ConvertFromString("#FAF1EE");
                        surfaceContainer = (Color)ColorConverter.ConvertFromString("#F4EBE8");
                        surfaceContainerHigh = (Color)ColorConverter.ConvertFromString("#EEE5E2");
                        surfaceContainerHighest = (Color)ColorConverter.ConvertFromString("#E8DFDC");
                        break;

                    default: // Purple
                        primary = (Color)ColorConverter.ConvertFromString("#6750A4");
                        onPrimary = Colors.White;
                        primaryContainer = (Color)ColorConverter.ConvertFromString("#EADDFF");
                        onPrimaryContainer = (Color)ColorConverter.ConvertFromString("#21005D");
                        secondaryContainer = (Color)ColorConverter.ConvertFromString("#E8DEF8");
                        onSecondaryContainer = (Color)ColorConverter.ConvertFromString("#1D192B");
                        surface = (Color)ColorConverter.ConvertFromString("#FEF7FF");
                        surfaceContainerLowest = Colors.White;
                        surfaceContainerLow = (Color)ColorConverter.ConvertFromString("#F7F2FA");
                        surfaceContainer = (Color)ColorConverter.ConvertFromString("#F3EDF7");
                        surfaceContainerHigh = (Color)ColorConverter.ConvertFromString("#ECE6F0");
                        surfaceContainerHighest = (Color)ColorConverter.ConvertFromString("#E6E0E9");
                        break;
                }
            }

            // Update app resources
            SetBrush(app, "M3.PrimaryBrush", primary);
            SetBrush(app, "M3.OnPrimaryBrush", onPrimary);
            SetBrush(app, "M3.PrimaryContainerBrush", primaryContainer);
            SetBrush(app, "M3.OnPrimaryContainerBrush", onPrimaryContainer);

            SetBrush(app, "M3.SecondaryContainerBrush", secondaryContainer);
            SetBrush(app, "M3.OnSecondaryContainerBrush", onSecondaryContainer);

            SetBrush(app, "M3.SurfaceBrush", surface);
            SetBrush(app, "M3.SurfaceContainerLowestBrush", surfaceContainerLowest);
            SetBrush(app, "M3.SurfaceContainerLowBrush", surfaceContainerLow);
            SetBrush(app, "M3.SurfaceContainerBrush", surfaceContainer);
            SetBrush(app, "M3.SurfaceContainerHighBrush", surfaceContainerHigh);
            SetBrush(app, "M3.SurfaceContainerHighestBrush", surfaceContainerHighest);

            SetBrush(app, "M3.OnSurfaceBrush", onSurface);
            SetBrush(app, "M3.OnSurfaceVariantBrush", onSurfaceVariant);
            SetBrush(app, "M3.OutlineBrush", outline);
            SetBrush(app, "M3.OutlineVariantBrush", outlineVariant);
            SetBrush(app, "M3.InverseSurfaceBrush", inverseSurface);
            SetBrush(app, "M3.InverseOnSurfaceBrush", inverseOnSurface);
        }

        private static void SetBrush(System.Windows.Application app, string key, System.Windows.Media.Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            app.Resources[key] = brush;
        }
    }
}
