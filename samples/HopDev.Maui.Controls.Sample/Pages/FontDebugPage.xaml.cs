namespace HopDev.Maui.Controls.Sample.Pages;

public partial class FontDebugPage : ContentPage
{
    private static readonly (string Glyph, string Hex, string Name)[] TestCodepoints =
    [
        ("\uF699", "F699", "Search"),
        ("\uF585", "F585", "Notes"),
        ("\uF5CF", "F5CF", "Contacts/People"),
        ("\uF46E", "F46E", "Lock/Secrets"),
        ("\uF33B", "F33B", "Documents"),
        ("\uF1A8", "F1A8", "Bookmarks"),
        ("\uF494", "F494", "Photos"),
        ("\uF109", "F109", "Add"),
        ("\uF3DC", "F3DC", "Edit"),
        ("\uF34C", "F34C", "Delete"),
        ("\uF4C0", "F4C0", "Key"),
        ("\uF6B2", "F6B2", "Settings"),
    ];

    public FontDebugPage()
    {
        InitializeComponent();
        BuildCodepointTests("FluentFilled", CodepointTestAlias);
        BuildCodepointTests("FluentSystemIcons-Filled", CodepointTestInternal);
        RunDiagnostics();
    }

    private static void BuildCodepointTests(string fontFamily, VerticalStackLayout container)
    {
        foreach (var (glyph, hex, name) in TestCodepoints)
        {
            var row = new HorizontalStackLayout { Spacing = 12 };

            row.Children.Add(new Label
            {
                Text = glyph,
                FontFamily = fontFamily,
                FontSize = 22,
                TextColor = Colors.White,
                WidthRequest = 32,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalOptions = LayoutOptions.Center
            });

            row.Children.Add(new Label
            {
                Text = $"{hex} — {name}",
                FontSize = 13,
                TextColor = Color.FromArgb("#94A3B8"),
                VerticalOptions = LayoutOptions.Center
            });

            container.Children.Add(row);
        }
    }

    private void RunDiagnostics()
    {
        var lines = new List<string>();

#if WINDOWS
        try
        {
            // Check if font files exist in the app package
            var appDir = AppContext.BaseDirectory;
            lines.Add($"AppDir: {appDir}");

            // Search for Fluent font files
            var fontPaths = new[]
            {
                System.IO.Path.Combine(appDir, "FluentSystemIcons-Filled.ttf"),
                System.IO.Path.Combine(appDir, "Resources", "Fonts", "FluentSystemIcons-Filled.ttf"),
                System.IO.Path.Combine(appDir, "Fonts", "FluentSystemIcons-Filled.ttf"),
            };

            foreach (var path in fontPaths)
            {
                var exists = System.IO.File.Exists(path);
                lines.Add($"  {(exists ? "✅" : "❌")} {path}");
            }

            // Also search recursively
            lines.Add("");
            lines.Add("Searching for *Fluent* files:");
            try
            {
                var found = System.IO.Directory.GetFiles(appDir, "*Fluent*", System.IO.SearchOption.AllDirectories);
                if (found.Length == 0)
                    lines.Add("  ❌ No files matching *Fluent* found anywhere");
                else
                    foreach (var f in found)
                        lines.Add($"  ✅ {f}");
            }
            catch (Exception ex)
            {
                lines.Add($"  Search error: {ex.Message}");
            }

            // List all .ttf files
            lines.Add("");
            lines.Add("All .ttf files in app directory:");
            try
            {
                var ttfs = System.IO.Directory.GetFiles(appDir, "*.ttf", System.IO.SearchOption.AllDirectories);
                if (ttfs.Length == 0)
                    lines.Add("  ❌ No .ttf files found");
                else
                    foreach (var f in ttfs)
                        lines.Add($"  📄 {f}");
            }
            catch (Exception ex)
            {
                lines.Add($"  Search error: {ex.Message}");
            }

            // Try to list fonts registered with MAUI
            lines.Add("");
            lines.Add("Font registration check:");
            lines.Add("  (Font manager API not accessible — check .ttf file presence above)");
            lines.Add("  If .ttf files are present but icons show as boxes,");
            lines.Add("  the issue is the FontFamily name reference in XAML/code.");
        }
        catch (Exception ex)
        {
            lines.Add($"Diagnostic error: {ex.Message}");
        }
#else
        lines.Add("Font diagnostics only available on Windows");
#endif

        LblDebug.Text = string.Join("\n", lines);
    }
}
