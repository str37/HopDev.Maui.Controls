using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using HopDev.Maui.Controls.Extensions;
using HopDev.Maui.Controls.Platform;
using HopDev.Maui.Controls.Platform.Abstractions;

namespace HopDev.Maui.Controls.Controls;

/// <summary>
/// A thin title bar control that takes ownership of the window's title bar area.
/// 
/// Handles all platform plumbing (ExtendsContentIntoTitleBar, drag regions, caption
/// button measurement, DPI awareness) so the developer only thinks about content.
/// 
/// Three-zone layout: LeadingContent | Content (center) | TrailingContent | [caption buttons]
/// 
/// Usage:
/// <code>
/// &lt;hd:TitleBar TitleBarBackground="{DynamicResource SurfaceBrush}" HeightRequest="48"&gt;
///     &lt;hd:TitleBar.LeadingContent&gt;
///         &lt;Image Source="appicon.png" HeightRequest="20" /&gt;
///     &lt;/hd:TitleBar.LeadingContent&gt;
///     &lt;SearchBar Placeholder="Search..." /&gt;
///     &lt;hd:TitleBar.TrailingContent&gt;
///         &lt;ImageButton Source="settings.png" /&gt;
///     &lt;/hd:TitleBar.TrailingContent&gt;
/// &lt;/hd:TitleBar&gt;
/// </code>
/// 
/// Minimal: <c>&lt;hd:TitleBar Title="My App" /&gt;</c>
/// </summary>
[ContentProperty(nameof(CenterContent))]
public class TitleBar : ContentView
{
    // ═══════════════════════════════════════════════════════════
    // Bindable Properties — Content Zones
    // ═══════════════════════════════════════════════════════════

    public static readonly BindableProperty LeadingContentProperty = BindableProperty.Create(
        nameof(LeadingContent), typeof(View), typeof(TitleBar), null,
        propertyChanged: OnLeadingContentChanged);

    public static readonly BindableProperty CenterContentProperty = BindableProperty.Create(
        nameof(CenterContent), typeof(View), typeof(TitleBar), null,
        propertyChanged: OnCenterContentChanged);

    public static readonly BindableProperty TrailingContentProperty = BindableProperty.Create(
        nameof(TrailingContent), typeof(View), typeof(TitleBar), null,
        propertyChanged: OnTrailingContentChanged);

    // ═══════════════════════════════════════════════════════════
    // Bindable Properties — Fallback Title
    // ═══════════════════════════════════════════════════════════

    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title), typeof(string), typeof(TitleBar), null,
        propertyChanged: OnTitleChanged);

    public static readonly BindableProperty TitleFontSizeProperty = BindableProperty.Create(
        nameof(TitleFontSize), typeof(double), typeof(TitleBar), 13.0,
        propertyChanged: OnTitleAppearanceChanged);

    public static readonly BindableProperty TitleColorProperty = BindableProperty.Create(
        nameof(TitleColor), typeof(Color), typeof(TitleBar), null,
        propertyChanged: OnTitleAppearanceChanged);

    // ═══════════════════════════════════════════════════════════
    // Bindable Properties — Appearance
    // ═══════════════════════════════════════════════════════════

    public static readonly BindableProperty TitleBarBackgroundProperty = BindableProperty.Create(
        nameof(TitleBarBackground), typeof(Brush), typeof(TitleBar), null,
        propertyChanged: OnAppearanceChanged);

    public static readonly BindableProperty ButtonHoverColorProperty = BindableProperty.Create(
        nameof(ButtonHoverColor), typeof(Color), typeof(TitleBar), null,
        propertyChanged: OnButtonColorsChanged);

    public static readonly BindableProperty ButtonForegroundColorProperty = BindableProperty.Create(
        nameof(ButtonForegroundColor), typeof(Color), typeof(TitleBar), null,
        propertyChanged: OnButtonColorsChanged);

    public static readonly BindableProperty ButtonPressedColorProperty = BindableProperty.Create(
        nameof(ButtonPressedColor), typeof(Color), typeof(TitleBar), null,
        propertyChanged: OnButtonColorsChanged);

    // ═══════════════════════════════════════════════════════════
    // Bindable Properties — Behavior
    // ═══════════════════════════════════════════════════════════

    public static readonly BindableProperty AutoExtendProperty = BindableProperty.Create(
        nameof(AutoExtend), typeof(bool), typeof(TitleBar), true);

    // ═══════════════════════════════════════════════════════════
    // Attached Property — Interactive Region Declaration (Open/Closed Principle)
    // ═══════════════════════════════════════════════════════════

    public static readonly BindableProperty IsInteractiveProperty =
        BindableProperty.CreateAttached(
            "IsInteractive",
            typeof(bool),
            typeof(TitleBar),
            false);

    public static void SetIsInteractive(BindableObject view, bool value) =>
        view.SetValue(IsInteractiveProperty, value);

    public static bool GetIsInteractive(BindableObject view) =>
        (bool)view.GetValue(IsInteractiveProperty);

    // ═══════════════════════════════════════════════════════════
    // Read-Only Bindable Properties
    // ═══════════════════════════════════════════════════════════

    private static readonly BindablePropertyKey CaptionButtonInsetsPropertyKey =
        BindableProperty.CreateReadOnly(
            nameof(CaptionButtonInsets), typeof(Thickness), typeof(TitleBar), Thickness.Zero);
    public static readonly BindableProperty CaptionButtonInsetsProperty =
        CaptionButtonInsetsPropertyKey.BindableProperty;

    private static readonly BindablePropertyKey IsAttachedToWindowPropertyKey =
        BindableProperty.CreateReadOnly(
            nameof(IsAttachedToWindow), typeof(bool), typeof(TitleBar), false);
    public static readonly BindableProperty IsAttachedToWindowProperty =
        IsAttachedToWindowPropertyKey.BindableProperty;

    // ═══════════════════════════════════════════════════════════
    // CLR Properties
    // ═══════════════════════════════════════════════════════════

    public View? LeadingContent { get => (View?)GetValue(LeadingContentProperty); set => SetValue(LeadingContentProperty, value); }
    public View? CenterContent { get => (View?)GetValue(CenterContentProperty); set => SetValue(CenterContentProperty, value); }
    public View? TrailingContent { get => (View?)GetValue(TrailingContentProperty); set => SetValue(TrailingContentProperty, value); }

    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public double TitleFontSize { get => (double)GetValue(TitleFontSizeProperty); set => SetValue(TitleFontSizeProperty, value); }
    public Color? TitleColor { get => (Color?)GetValue(TitleColorProperty); set => SetValue(TitleColorProperty, value); }

    public Brush? TitleBarBackground { get => (Brush?)GetValue(TitleBarBackgroundProperty); set => SetValue(TitleBarBackgroundProperty, value); }
    public Color? ButtonHoverColor { get => (Color?)GetValue(ButtonHoverColorProperty); set => SetValue(ButtonHoverColorProperty, value); }
    public Color? ButtonForegroundColor { get => (Color?)GetValue(ButtonForegroundColorProperty); set => SetValue(ButtonForegroundColorProperty, value); }
    public Color? ButtonPressedColor { get => (Color?)GetValue(ButtonPressedColorProperty); set => SetValue(ButtonPressedColorProperty, value); }

    /// <summary>When true (default), the control automatically calls ExtendContentIntoTitleBar
    /// when it attaches to the platform. Set false to manage this yourself.</summary>
    public bool AutoExtend { get => (bool)GetValue(AutoExtendProperty); set => SetValue(AutoExtendProperty, value); }

    public Thickness CaptionButtonInsets => (Thickness)GetValue(CaptionButtonInsetsProperty);
    public bool IsAttachedToWindow => (bool)GetValue(IsAttachedToWindowProperty);

    // ═══════════════════════════════════════════════════════════
    // Events
    // ═══════════════════════════════════════════════════════════

    /// <summary>Fires when the control has attached to the platform window
    /// and all metrics are available.</summary>
    public event EventHandler? AttachedToWindow;

    /// <summary>Fires when the back button is pressed (if visible). Phase 4.</summary>
#pragma warning disable CS0067 // Public API — raised when ShowBackButton is implemented (Phase 4)
    public event EventHandler? BackButtonClicked;
#pragma warning restore CS0067

    // ═══════════════════════════════════════════════════════════
    // Internal Layout Elements
    // ═══════════════════════════════════════════════════════════

    private readonly Grid _root;
    private readonly ContentView _leadingHost;
    private readonly ContentView _centerHost;
    private readonly ContentView _trailingHost;
    private readonly BoxView _captionSpacer;
    private readonly Label _fallbackTitle;

    private IWindowScaleService? _scaleService;
    private IWindowChromeService? _chromeService;

    // ═══════════════════════════════════════════════════════════
    // Constructor
    // ═══════════════════════════════════════════════════════════

    public TitleBar()
    {
        // Build internal layout in code — matches SmartScrollPanel pattern
        _leadingHost = new ContentView { VerticalOptions = LayoutOptions.Center };
        _centerHost = new ContentView { VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Fill };
        _trailingHost = new ContentView { VerticalOptions = LayoutOptions.Center };
        _captionSpacer = new BoxView { Color = Colors.Transparent, WidthRequest = 0 };

        _fallbackTitle = new Label
        {
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Start,
            FontSize = 13,
            IsVisible = false
        };

        _root = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),   // 0: leading
                new ColumnDefinition(GridLength.Star),   // 1: center
                new ColumnDefinition(GridLength.Auto),   // 2: trailing
                new ColumnDefinition(GridLength.Auto),   // 3: caption spacer
            },
            ColumnSpacing = 0,
            IsClippedToBounds = true,
        };

        Grid.SetColumn(_leadingHost, 0);
        Grid.SetColumn(_centerHost, 1);
        Grid.SetColumn(_fallbackTitle, 1);
        Grid.SetColumn(_trailingHost, 2);
        Grid.SetColumn(_captionSpacer, 3);

        _root.Children.Add(_leadingHost);
        _root.Children.Add(_centerHost);
        _root.Children.Add(_fallbackTitle);
        _root.Children.Add(_trailingHost);
        _root.Children.Add(_captionSpacer);

        // Set the internal layout as this ContentView's content
        base.Content = _root;

        // Wire handler lifecycle
        HandlerChanged += OnHandlerChanged;
    }

    // ═══════════════════════════════════════════════════════════
    // Platform Hookup
    // ═══════════════════════════════════════════════════════════

    private void OnHandlerChanged(object? sender, EventArgs e)
    {
        if (Handler is null || Window is null) return;

        // Ensure services are created and attached for this Window
        ServiceCollectionExtensions.EnsureHopDevServicesAttached(Window);

        _scaleService = HopDevServices.GetScaleService(Window);
        _chromeService = HopDevServices.GetChromeService(Window);

        if (_chromeService is null)
        {
            System.Diagnostics.Debug.WriteLine("[TitleBar] WARNING: chrome service unavailable");
            return;
        }

        // Extend content into title bar
        if (AutoExtend)
        {
            _chromeService.ExtendContentIntoTitleBar(true);
        }

        // Set the root grid as the drag region
        _chromeService.SetDragRegion(_root);

        // Read caption button insets and size the spacer
        UpdateCaptionSpacer();

        // Subscribe to inset changes (DPI change, window state change)
        _chromeService.CaptionInsetsChanged += OnCaptionInsetsChanged;

        // Apply button colors
        ApplyButtonColors();

        // Register interactive regions for current content
        RegisterAllInteractiveRegions();

        // Mark as attached
        SetValue(IsAttachedToWindowPropertyKey, true);
        AttachedToWindow?.Invoke(this, EventArgs.Empty);

        System.Diagnostics.Debug.WriteLine(
            $"[TitleBar] Attached — caption insets: {CaptionButtonInsets}, " +
            $"scale: {_scaleService?.ScaleFactor:F2}");
    }

    // ═══════════════════════════════════════════════════════════
    // Caption Button Spacing
    // ═══════════════════════════════════════════════════════════

    private int _captionRetryCount;

    private void UpdateCaptionSpacer()
    {
        if (_chromeService is null) return;

        var insets = _chromeService.CaptionButtonInsets;
        SetValue(CaptionButtonInsetsPropertyKey, insets);

        _captionSpacer.WidthRequest = insets.Right;
        _leadingHost.Padding = new Thickness(insets.Left, 0, 0, 0);

        // If insets are zero, the AppWindowTitleBar may not be fully initialized yet.
        // Retry up to 5 times with increasing delay (100ms, 200ms, 400ms, 800ms, 1600ms).
        // This handles the timing gap between WindowHandler mapping and TitleBar
        // initialization — common on first render, especially at non-100% DPI.
        if (insets.Right <= 0 && _captionRetryCount < 5)
        {
            _captionRetryCount++;
            var delay = TimeSpan.FromMilliseconds(100 * Math.Pow(2, _captionRetryCount - 1));
            Dispatcher.DispatchDelayed(delay, UpdateCaptionSpacer);

            System.Diagnostics.Debug.WriteLine(
                $"[TitleBar] Caption insets zero — retry {_captionRetryCount}/5 in {delay.TotalMilliseconds}ms");
        }
        else if (insets.Right > 0)
        {
            _captionRetryCount = 0; // reset for future DPI changes
        }
    }

    private void OnCaptionInsetsChanged(object? sender, CaptionInsetsChangedEventArgs e)
    {
        // Dispatch to UI thread — DPI changes may fire from background
        MainThread.BeginInvokeOnMainThread(UpdateCaptionSpacer);
    }

    // ═══════════════════════════════════════════════════════════
    // Interactive Region Management
    // ═══════════════════════════════════════════════════════════

    private void RegisterAllInteractiveRegions()
    {
        if (_chromeService is null) return;

        // Register each content zone's subtree
        if (LeadingContent is not null)
            RegisterInteractiveSubtree(LeadingContent);
        if (CenterContent is not null)
            RegisterInteractiveSubtree(CenterContent);
        if (TrailingContent is not null)
            RegisterInteractiveSubtree(TrailingContent);
    }

    private void RegisterInteractiveSubtree(View root)
    {
        if (_chromeService is null) return;

        // Check the explicit attached property first (Open/Closed principle)
        if (GetIsInteractive(root))
        {
            _chromeService.RegisterInteractiveRegion(root);
        }

        // Auto-detect well-known interactive types (convenience, not exhaustive)
        if (IsKnownInteractiveType(root))
        {
            _chromeService.RegisterInteractiveRegion(root);
        }

        // Recurse into layout children
        if (root is Layout layout)
        {
            foreach (var child in layout.Children.OfType<View>())
            {
                RegisterInteractiveSubtree(child);
            }
        }
        else if (root is ContentView contentView && contentView.Content is View content)
        {
            RegisterInteractiveSubtree(content);
        }
    }

    private static bool IsKnownInteractiveType(View view) =>
        view is Button or ImageButton or
               Entry or SearchBar or Editor or
               Picker or DatePicker or TimePicker or
               CheckBox or Switch or RadioButton or
               Slider or Stepper;

    // ═══════════════════════════════════════════════════════════
    // Button Color Application
    // ═══════════════════════════════════════════════════════════

    private void ApplyButtonColors()
    {
        _chromeService?.SetButtonColors(
            ButtonForegroundColor,
            ButtonHoverColor,
            ButtonPressedColor);
    }

    // ═══════════════════════════════════════════════════════════
    // Property Changed Handlers
    // ═══════════════════════════════════════════════════════════

    private static void OnLeadingContentChanged(BindableObject b, object? oldVal, object? newVal)
    {
        var tb = (TitleBar)b;
        tb._leadingHost.Content = newVal as View;
        if (tb.IsAttachedToWindow) tb.RegisterAllInteractiveRegions();
    }

    private static void OnCenterContentChanged(BindableObject b, object? oldVal, object? newVal)
    {
        var tb = (TitleBar)b;
        var hasContent = newVal is not null;
        tb._centerHost.Content = newVal as View;
        tb._centerHost.IsVisible = hasContent;
        tb._fallbackTitle.IsVisible = !hasContent && !string.IsNullOrEmpty(tb.Title);
        if (tb.IsAttachedToWindow) tb.RegisterAllInteractiveRegions();
    }

    private static void OnTrailingContentChanged(BindableObject b, object? oldVal, object? newVal)
    {
        var tb = (TitleBar)b;
        tb._trailingHost.Content = newVal as View;
        if (tb.IsAttachedToWindow) tb.RegisterAllInteractiveRegions();
    }

    private static void OnTitleChanged(BindableObject b, object? oldVal, object? newVal)
    {
        var tb = (TitleBar)b;
        var title = newVal as string;
        tb._fallbackTitle.Text = title;
        tb._fallbackTitle.IsVisible = !string.IsNullOrEmpty(title) && tb.CenterContent is null;
    }

    private static void OnTitleAppearanceChanged(BindableObject b, object? oldVal, object? newVal)
    {
        var tb = (TitleBar)b;
        tb._fallbackTitle.FontSize = tb.TitleFontSize;
        if (tb.TitleColor is not null) tb._fallbackTitle.TextColor = tb.TitleColor;
    }

    private static void OnAppearanceChanged(BindableObject b, object? oldVal, object? newVal)
    {
        var tb = (TitleBar)b;
        if (tb.TitleBarBackground is not null)
            tb._root.Background = tb.TitleBarBackground;
    }

    private static void OnButtonColorsChanged(BindableObject b, object? oldVal, object? newVal)
    {
        var tb = (TitleBar)b;
        if (tb.IsAttachedToWindow) tb.ApplyButtonColors();
    }
}
