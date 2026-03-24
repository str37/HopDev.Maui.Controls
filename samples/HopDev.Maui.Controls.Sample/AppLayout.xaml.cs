using HopDev.Maui.Controls.Sample.Pages;

namespace HopDev.Maui.Controls.Sample;

public partial class AppLayout : ContentPage
{
    // ── Sizing constants ─────────────────────────────────────
    // Sidebar uses stacked vertical layout → icons appear larger.
    // Title bar uses horizontal layout → icon + text side-by-side.
    // Both use Segoe MDL2 Assets. Sidebar inactive=20, active=24.
    // Title bar icons=18 keeps visual weight balanced.
    private const string IconFont = "Segoe MDL2 Assets";
    private const double IconSizeActive = 24;
    private const double IconSizeInactive = 20;
    private const double TitleBarHeight = 58;

    // ── Sidebar — Top items ─────────────────────────────────
    private readonly (string Icon, string Label, string Key, Func<ContentPage> Factory)[] _sidebarItems =
    [
        ("\uE70B", "Notes",     "notes",     () => new PlaceholderPage("Notes", "Your encrypted notes with markdown preview, categories, and subnotes.")),
        ("\uE77B", "Contacts",  "contacts",  () => new PlaceholderPage("Contacts", "Contact management with addresses, favorites, and search.")),
        ("\uE8B7", "Documents", "documents", () => new PlaceholderPage("Documents", "DocVault document browser with versioning and search.")),
        ("\uE735", "Bookmarks", "bookmarks", () => new PlaceholderPage("Bookmarks", "Web bookmark organizer with folders and drag-and-drop.")),
        ("\uE722", "Media",     "media",     () => new PlaceholderPage("Media", "Photo gallery, curator, and image tools.")),
    ];

    // ── Sidebar — Bottom-docked ──────────────────────────────
    private readonly (string Icon, string Label, string Key, Func<ContentPage> Factory)[] _sidebarBottomItems =
    [
        ("\uE72E", "Secrets",   "secrets",   () => new PlaceholderPage("Secrets", "Encrypted password vault with folder organization.")),
    ];

    // ── Dropdown Definitions ────────────────────────────────
    private readonly (string Label, string Key, Func<ContentPage> Factory)[] _testsMenu =
    [
        ("TitleBar",       "titlebar",     () => new TitleBarTestPage()),
        ("Diagnostics",    "diagnostics",  () => new DiagnosticsPage()),
        ("Font Debug",     "fontdebug",    () => new FontDebugPage()),
        ("Icon Reference", "iconref",      () => new IconReferencePage()),
        ("SmartScroll",    "smartscroll",  () => new SmartScrollTestPage()),
        ("ScrollView",     "scrollview",   () => new ScrollViewTestPage()),
        ("Drag Drop",      "dragdrop",     () => new DragDropTestPage()),
    ];

    private readonly (string Label, string Key, Func<ContentPage> Factory)[] _utilitiesMenu =
    [
        ("Import/Export",  "importexport",() => new PlaceholderPage("Import/Export", "Import data from CSV, vCard, KeePass, and other formats.")),
    ];

    private readonly Dictionary<string, ContentPage> _pageCache = new();
    private readonly Dictionary<string, Border> _sidebarBorders = new();
    private string _currentPage = "";
    private bool _sidebarExpanded = true;

    // ── Dropdown hover state ─────────────────────────────────
    private View? _activeMenuAnchor;
    private bool _isOverDropdown;
    private CancellationTokenSource? _dismissCts;

    public AppLayout()
    {
        InitializeComponent();
        BuildSidebar();
        WireMenuInteractions();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_currentPage == "" && _sidebarItems.Length > 0)
            NavigateTo(_sidebarItems[0].Key);
    }

    // ═══════════════════════════════════════════════════════════
    //  Title Bar Menu Interactions (hover + click)
    // ═══════════════════════════════════════════════════════════

    private void WireMenuInteractions()
    {
        // Tests — hover to open, click as fallback
        WireDropdownTrigger(MenuTests, _testsMenu);
        WireDropdownTrigger(MenuUtilities, _utilitiesMenu);

        // Settings / About — hover highlight + tap navigates
        WireNavButton(MenuSettings, "settings");
        WireNavButton(MenuAbout, "about");

        // Dropdown menu itself — keep open while hovering
        var ddEnter = new PointerGestureRecognizer();
        ddEnter.PointerEntered += (_, _) =>
        {
            _isOverDropdown = true;
            CancelDismiss();
        };
        DropdownMenu.GestureRecognizers.Add(ddEnter);

        var ddExit = new PointerGestureRecognizer();
        ddExit.PointerExited += (_, _) =>
        {
            _isOverDropdown = false;
            ScheduleDismiss();
        };
        DropdownMenu.GestureRecognizers.Add(ddExit);
    }

    /// <summary>
    /// Wires a menu button to show its dropdown on hover and toggle on click.
    /// Hovering from one dropdown trigger to another switches instantly.
    /// </summary>
    private void WireDropdownTrigger(Border menuButton,
        (string Label, string Key, Func<ContentPage> Factory)[] items)
    {
        // Hover → show
        var enter = new PointerGestureRecognizer();
        enter.PointerEntered += (_, _) =>
        {
            CancelDismiss();
            menuButton.BackgroundColor = Color.FromArgb("#403D8BFD");
            ShowDropdown(items, menuButton);
        };
        menuButton.GestureRecognizers.Add(enter);

        // Hover exit → schedule dismiss
        var exit = new PointerGestureRecognizer();
        exit.PointerExited += (_, _) =>
        {
            if (_activeMenuAnchor != menuButton) return;
            menuButton.BackgroundColor = Colors.Transparent;
            ScheduleDismiss();
        };
        menuButton.GestureRecognizers.Add(exit);

        // Click — toggle (fallback for touch / accessibility)
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) =>
        {
            if (DropdownOverlay.IsVisible && _activeMenuAnchor == menuButton)
                DismissDropdown();
            else
                ShowDropdown(items, menuButton);
        };
        menuButton.GestureRecognizers.Add(tap);
    }

    /// <summary>Hover highlight + tap navigation for non-dropdown buttons.</summary>
    private void WireNavButton(Border button, string route)
    {
        var enter = new PointerGestureRecognizer();
        enter.PointerEntered += (_, _) =>
        {
            button.BackgroundColor = Color.FromArgb("#403D8BFD");
            // If a dropdown is open, dismiss it as the user moved to a direct nav button
            if (DropdownOverlay.IsVisible)
                DismissDropdown();
        };
        button.GestureRecognizers.Add(enter);

        var exit = new PointerGestureRecognizer();
        exit.PointerExited += (_, _) =>
            button.BackgroundColor = Colors.Transparent;
        button.GestureRecognizers.Add(exit);

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) =>
        {
            DismissDropdown();
            NavigateTo(route);
        };
        button.GestureRecognizers.Add(tap);
    }

    /// <summary>
    /// Schedules dropdown dismiss after 200ms. Cancelled if the user
    /// hovers back over the trigger or the dropdown menu.
    /// </summary>
    private void ScheduleDismiss()
    {
        _dismissCts?.Cancel();
        _dismissCts = new CancellationTokenSource();
        var token = _dismissCts.Token;

        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(200), () =>
        {
            if (token.IsCancellationRequested) return;
            if (_isOverDropdown) return;
            DismissDropdown();
        });
    }

    private void CancelDismiss()
    {
        _dismissCts?.Cancel();
        _dismissCts = null;
    }

    // ═══════════════════════════════════════════════════════════
    //  Sidebar
    // ═══════════════════════════════════════════════════════════

    private void BuildSidebar()
    {
        SidebarItems.Children.Clear();
        SidebarBottomItems.Children.Clear();
        _sidebarBorders.Clear();

        foreach (var (icon, label, key, _) in _sidebarItems)
            SidebarItems.Children.Add(BuildSidebarButton(icon, label, key));

        SidebarBottomItems.Children.Add(new BoxView
        {
            HeightRequest = 1,
            Color = Color.FromArgb("#30394A"),
            Margin = new Thickness(8, 4)
        });
        foreach (var (icon, label, key, _) in _sidebarBottomItems)
            SidebarBottomItems.Children.Add(BuildSidebarButton(icon, label, key));
    }

    private Border BuildSidebarButton(string icon, string label, string key)
    {
        var iconLabel = new Label
        {
            Text = icon, FontSize = IconSizeInactive,
            FontFamily = IconFont,
            TextColor = Color.FromArgb("#64748B"),
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };

        var textLabel = new Label
        {
            Text = label, FontSize = 15,
            TextColor = Color.FromArgb("#64748B"),
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };

        var stack = new VerticalStackLayout
        {
            Spacing = 2,
            HorizontalOptions = LayoutOptions.Center,
            Children = { iconLabel }
        };
        if (_sidebarExpanded)
            stack.Children.Add(textLabel);

        var border = new Border
        {
            Content = stack,
            Padding = new Thickness(4, 8),
            WidthRequest = _sidebarExpanded ? 160 : 52,
            StrokeThickness = 0,
            BackgroundColor = Colors.Transparent,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 6 }
        };

        var itemKey = key;
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => NavigateTo(itemKey);
        border.GestureRecognizers.Add(tap);

        var pointerEnter = new PointerGestureRecognizer();
        pointerEnter.PointerEntered += (_, _) =>
        {
            if (itemKey != _currentPage)
                border.BackgroundColor = Color.FromArgb("#403D8BFD");
        };
        var pointerExit = new PointerGestureRecognizer();
        pointerExit.PointerExited += (_, _) =>
        {
            if (itemKey != _currentPage)
                border.BackgroundColor = Colors.Transparent;
        };
        border.GestureRecognizers.Add(pointerEnter);
        border.GestureRecognizers.Add(pointerExit);

        _sidebarBorders[key] = border;
        return border;
    }

    private void OnToggleSidebar(object? sender, TappedEventArgs e)
    {
        _sidebarExpanded = !_sidebarExpanded;
        CollapseIcon.Text = _sidebarExpanded ? "◀" : "▶";
        BuildSidebar();
        UpdateSidebarVisualState(_currentPage);
    }

    // ═══════════════════════════════════════════════════════════
    //  Navigation
    // ═══════════════════════════════════════════════════════════

    private void NavigateTo(string key)
    {
        if (key == _currentPage) return;

        if (!_pageCache.TryGetValue(key, out var page))
        {
            Func<ContentPage>? factory = null;

            foreach (var item in _sidebarItems)
                if (item.Key == key) { factory = item.Factory; break; }

            if (factory is null)
                foreach (var item in _sidebarBottomItems)
                    if (item.Key == key) { factory = item.Factory; break; }

            if (factory is null)
                foreach (var item in _testsMenu)
                    if (item.Key == key) { factory = item.Factory; break; }

            if (factory is null)
                foreach (var item in _utilitiesMenu)
                    if (item.Key == key) { factory = item.Factory; break; }

            if (factory is null && key == "settings")
                factory = () => new PlaceholderPage("Settings", "Connection, appearance, and preference configuration.");
            if (factory is null && key == "about")
                factory = () => new PlaceholderPage("About", "App info, security details, and open source credits.");

            if (factory is null) return;
            page = factory();
            _pageCache[key] = page;
        }

        PageHost.Content = page.Content;
        _currentPage = key;

        UpdateSidebarVisualState(key);
        DismissDropdown();
    }

    private void UpdateSidebarVisualState(string activeKey)
    {
        foreach (var (key, border) in _sidebarBorders)
        {
            var isActive = key == activeKey;
            border.BackgroundColor = isActive
                ? Color.FromArgb("#403D8BFD")
                : Colors.Transparent;

            if (border.Content is VerticalStackLayout stack)
            {
                foreach (var child in stack.Children)
                {
                    if (child is Label lbl)
                    {
                        if (lbl.FontFamily == IconFont)
                        {
                            lbl.FontSize = isActive ? IconSizeActive : IconSizeInactive;
                            lbl.TextColor = isActive
                                ? Color.FromArgb("#E2E8F0")
                                : Color.FromArgb("#64748B");
                        }
                        else
                        {
                            lbl.TextColor = isActive
                                ? Color.FromArgb("#E2E8F0")
                                : Color.FromArgb("#64748B");
                            lbl.FontAttributes = isActive
                                ? FontAttributes.Bold : FontAttributes.None;
                        }
                    }
                }
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  Dropdown Menus
    // ═══════════════════════════════════════════════════════════

    private void ShowDropdown((string Label, string Key, Func<ContentPage> Factory)[] items, View anchor)
    {
        CancelDismiss();
        DropdownItems.Children.Clear();

        foreach (var (label, key, _) in items)
        {
            var menuLabel = new Label
            {
                Text = label,
                FontSize = 15,
                Padding = new Thickness(12, 8)
            };
            menuLabel.SetDynamicResource(Label.TextColorProperty, "TextPrimary");

            var menuBorder = new Border
            {
                Content = menuLabel,
                StrokeThickness = 0,
                BackgroundColor = Colors.Transparent,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 4 }
            };

            var menuKey = key;
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) => NavigateTo(menuKey);
            menuBorder.GestureRecognizers.Add(tap);

            var hover = new PointerGestureRecognizer();
            hover.PointerEntered += (_, _) =>
                menuBorder.BackgroundColor = Color.FromArgb("#403D8BFD");
            var hoverExit = new PointerGestureRecognizer();
            hoverExit.PointerExited += (_, _) =>
                menuBorder.BackgroundColor = Colors.Transparent;
            menuBorder.GestureRecognizers.Add(hover);
            menuBorder.GestureRecognizers.Add(hoverExit);

            DropdownItems.Children.Add(menuBorder);
        }

        // Position below the anchor
        double x = 0;
        VisualElement? current = anchor;
        while (current != null && current != this)
        {
            x += current.Bounds.X;
            current = current.Parent as VisualElement;
        }

        AbsoluteLayout.SetLayoutBounds(DropdownMenu,
            new Rect(x, TitleBarHeight, AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize));
        AbsoluteLayout.SetLayoutFlags(DropdownMenu,
            Microsoft.Maui.Layouts.AbsoluteLayoutFlags.None);

        DropdownMenu.WidthRequest = 220;
        DropdownOverlay.IsVisible = true;
        _activeMenuAnchor = anchor;
    }

    private void DismissDropdown()
    {
        CancelDismiss();
        DropdownOverlay.IsVisible = false;
        _activeMenuAnchor = null;
        _isOverDropdown = false;
    }

    private void OnDropdownDismiss(object? sender, TappedEventArgs e)
    {
        DismissDropdown();
    }

    // ═══════════════════════════════════════════════════════════
    //  TitleBar Lifecycle
    // ═══════════════════════════════════════════════════════════

    private void OnTitleBarAttached(object? sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine(
            $"[AppLayout] TitleBar attached — insets: {AppTitleBar.CaptionButtonInsets}");
    }
}
