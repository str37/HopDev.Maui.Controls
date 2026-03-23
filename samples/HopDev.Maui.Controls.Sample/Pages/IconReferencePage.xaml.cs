namespace HopDev.Maui.Controls.Sample.Pages;

public partial class IconReferencePage : ContentPage
{
    private const string FluentFont = "FluentSystemIcons-Filled";

    // ── Verified codepoints from Cloud.Maui MainLayout + Patterns.md ──
    private static readonly (string Glyph, string Name, string Code)[] NavIconList =
    [
        ("\uF585", "Notes",       "F585"),
        ("\uF5CF", "Contacts",    "F5CF"),
        ("\uF46E", "Secrets",     "F46E"),
        ("\uF33B", "Documents",   "F33B"),
        ("\uF1A8", "Bookmarks",   "F1A8"),
        ("\uF494", "Photos",      "F494"),
        ("\uF304", "Dashboard",   "F304"),
        ("\uF4E1", "Email",       "F4E1"),
        ("\uF6B2", "Settings",    "F6B2"),
        ("\uF49B", "About",       "F49B"),
        ("\uF335", "Crop",        "F335"),
        ("\uF310", "Logs",        "F310"),
    ];

    private static readonly (string Glyph, string Name, string Code)[] ActionIconList =
    [
        ("\uF699", "Search",      "F699"),
        ("\uF109", "Add/Create",  "F109"),
        ("\uF3DC", "Edit",        "F3DC"),
        ("\uF34C", "Delete",      "F34C"),
        ("\uF689", "Save",        "F689"),
        ("\uF369", "Dismiss/X",   "F369"),
        ("\uF32B", "Copy",        "F32B"),
        ("\uF634", "Print",       "F634"),
        ("\uF236", "Backup/Cloud","F236"),
    ];

    private static readonly (string Glyph, string Name, string Code)[] SecurityIconList =
    [
        ("\uF46E", "Lock Closed", "F46E"),
        ("\uE79D", "Lock Closed (alt)", "E79D"),
        ("\uE7A3", "Lock Open",   "E7A3"),
        ("\uF4C0", "Key",         "F4C0"),
        ("\uE754", "Key Reset",   "E754"),
        ("\uF6CE", "Shield",      "F6CE"),
        ("\uE8DB", "Password",    "E8DB"),
    ];

    private static readonly (string Glyph, string Name, string Code)[] AltIconList =
    [
        ("\uF4A3", "Image",        "F4A3"),
        ("\uF4A5", "Image Edit",   "F4A5"),
        ("\uF4A9", "Image Multiple","F4A9"),
        ("\uF47E", "Library",      "F47E"),
        ("\uF47C", "Layer",        "F47C"),
        ("\uF396", "Folder",       "F396"),
        ("\uF398", "Folder Open",  "F398"),
        ("\uF5F3", "Person",       "F5F3"),
        ("\uF5F5", "Person Add",   "F5F5"),
        ("\uF57E", "Notebook",     "F57E"),
        ("\uF586", "Note Add",     "F586"),
        ("\uF5B1", "Organization", "F5B1"),
        ("\uF213", "Calendar",     "F213"),
        ("\uF482", "Link",         "F482"),
        ("\uF4F1", "Mail Read",    "F4F1"),
        ("\uF42B", "Home",         "F42B"),
        ("\uF4DA", "Grid",         "F4DA"),
        ("\uF2E7", "Clipboard",    "F2E7"),
        ("\uF711", "Star",         "F711"),
        ("\uF713", "Star Emphasis","F713"),
        ("\uE8D7", "Panel/Window", "E8D7"),
        ("\uF1CE", "Camera",       "F1CE"),
        ("\uF219", "Camera Add",   "F219"),
        ("\uF775", "Tag",          "F775"),
        ("\uF3A0", "Filter",       "F3A0"),
        ("\uF6F8", "Sort",         "F6F8"),
        ("\uF186", "Arrow Down",   "F186"),
        ("\uF18E", "Arrow Up",     "F18E"),
        ("\uF296", "Checkmark",    "F296"),
        ("\uF297", "Checkmark Circle", "F297"),
        ("\uF83D", "Warning",      "F83D"),
        ("\uF349", "Error Circle", "F349"),
        ("\uF449", "Info",         "F449"),
        ("\uF1F2", "Bug",          "F1F2"),
        ("\uF6C6", "Share",        "F6C6"),
        ("\uF313", "Database",     "F313"),
        ("\uF84C", "Wrench",       "F84C"),
    ];

    public IconReferencePage()
    {
        InitializeComponent();
        BuildSection(NavIcons, NavIconList);
        BuildSection(ActionIcons, ActionIconList);
        BuildSection(SecurityIcons, SecurityIconList);
        BuildSection(AltIcons, AltIconList);
        BuildSizeComparison();
    }

    private static void BuildSection(VerticalStackLayout container,
        (string Glyph, string Name, string Code)[] icons)
    {
        // Grid rows: each icon row shows icon at 20px, 24px, name, and code
        foreach (var (glyph, name, code) in icons)
        {
            var row = new HorizontalStackLayout { Spacing = 16 };

            // Icon at 20pt (standard)
            row.Children.Add(new Label
            {
                Text = glyph, FontFamily = FluentFont, FontSize = 20,
                TextColor = Color.FromArgb("#E2E8F0"),
                WidthRequest = 28, HorizontalTextAlignment = TextAlignment.Center,
                VerticalOptions = LayoutOptions.Center
            });

            // Icon at 24pt (sidebar size)
            row.Children.Add(new Label
            {
                Text = glyph, FontFamily = FluentFont, FontSize = 24,
                TextColor = Color.FromArgb("#3D8BFD"),
                WidthRequest = 32, HorizontalTextAlignment = TextAlignment.Center,
                VerticalOptions = LayoutOptions.Center
            });

            // Name
            row.Children.Add(new Label
            {
                Text = name, FontSize = 14,
                TextColor = Color.FromArgb("#E2E8F0"),
                VerticalOptions = LayoutOptions.Center,
                WidthRequest = 160
            });

            // Code
            row.Children.Add(new Label
            {
                Text = $"\\u{code}  (&#x{code};)", FontSize = 12,
                TextColor = Color.FromArgb("#64748B"),
                VerticalOptions = LayoutOptions.Center
            });

            container.Children.Add(row);
        }
    }

    private void BuildSizeComparison()
    {
        int[] sizes = [14, 16, 18, 20, 22, 24, 28, 32, 36, 48];
        foreach (var size in sizes)
        {
            var row = new HorizontalStackLayout { Spacing = 12 };

            row.Children.Add(new Label
            {
                Text = "\uF585", FontFamily = FluentFont, FontSize = size,
                TextColor = Color.FromArgb("#E2E8F0"),
                VerticalOptions = LayoutOptions.Center
            });

            row.Children.Add(new Label
            {
                Text = $"{size}pt", FontSize = 13,
                TextColor = Color.FromArgb("#94A3B8"),
                VerticalOptions = LayoutOptions.Center
            });

            // Also show with a label next to it
            row.Children.Add(new Label
            {
                Text = "Notes", FontSize = size * 0.6,
                TextColor = Color.FromArgb("#94A3B8"),
                VerticalOptions = LayoutOptions.Center
            });

            SizeComparison.Children.Add(row);
        }
    }
}
