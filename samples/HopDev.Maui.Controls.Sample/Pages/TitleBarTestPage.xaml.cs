using HopDev.Maui.Controls.Platform;
using TitleBar = HopDev.Maui.Controls.Controls.TitleBar;

namespace HopDev.Maui.Controls.Sample.Pages;

public partial class TitleBarTestPage : ContentPage
{
    public TitleBarTestPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(200), UpdateStatus);
    }

    private TitleBar? FindTitleBar()
    {
        var rootPage = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (rootPage is AppLayout layout)
            return layout.FindByName<TitleBar>("AppTitleBar");
        return null;
    }

    private Microsoft.UI.Windowing.AppWindowTitleBar? GetNativeTitleBar()
    {
#if WINDOWS
        var window = Application.Current?.Windows.FirstOrDefault();
        var nativeWindow = window?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
        return nativeWindow?.AppWindow?.TitleBar;
#else
        return null;
#endif
    }

    private void UpdateStatus()
    {
        var titleBar = FindTitleBar();
        if (titleBar is null)
        {
            LblAttached.Text = "IsAttachedToWindow: ❌ TitleBar not found";
            return;
        }

        LblAttached.Text = $"IsAttachedToWindow: {(titleBar.IsAttachedToWindow ? "✅ Yes" : "❌ No")}";
        var insets = titleBar.CaptionButtonInsets;
        LblCaptionInsets.Text = $"Caption Insets: L={insets.Left:F0}  R={insets.Right:F0}";

        var window = Application.Current?.Windows.FirstOrDefault();
        if (window is not null)
        {
            var scaleService = HopDevServices.GetScaleService(window);
            LblScale.Text = scaleService is not null
                ? $"Scale: {scaleService.ScaleFactor:F2} ({scaleService.ScaleFactor * 100:F0}%)"
                : "Scale: unavailable";
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Light / Dark Theme Toggle
    // ═══════════════════════════════════════════════════════════

    private void OnThemeDark(object? sender, EventArgs e)
    {
        SetTheme(dark: true);
        LblTheme.Text = "Current: Dark";
    }

    private void OnThemeLight(object? sender, EventArgs e)
    {
        SetTheme(dark: false);
        LblTheme.Text = "Current: Light";
    }

    private static void SetTheme(bool dark)
    {
        var res = Application.Current?.Resources;
        if (res is null) return;

        if (dark)
        {
            res["PageBackground"]           = Color.FromArgb("#0D1117");
            res["SurfaceColor"]             = Color.FromArgb("#161B22");
            res["CardBackground"]           = Color.FromArgb("#1C2333");
            res["CardBorder"]               = Color.FromArgb("#30394A");
            res["TextPrimary"]              = Color.FromArgb("#E2E8F0");
            res["TextSecondary"]            = Color.FromArgb("#94A3B8");
            res["TextMuted"]                = Color.FromArgb("#64748B");
            res["ButtonPrimaryBackground"]  = Color.FromArgb("#3D8BFD");
            res["ButtonPrimaryText"]        = Color.FromArgb("#FFFFFF");
            res["AccentHover"]              = Color.FromArgb("#2D3D5E");
            res["SuccessText"]              = Color.FromArgb("#6EE7B7");

#if WINDOWS
            MauiProgram.UpdateBorderColor(dark: true); // #0D1117 COLORREF
#endif
        }
        else
        {
            res["PageBackground"]           = Color.FromArgb("#F8FAFC");
            res["SurfaceColor"]             = Color.FromArgb("#FFFFFF");
            res["CardBackground"]           = Color.FromArgb("#FFFFFF");
            res["CardBorder"]               = Color.FromArgb("#E2E8F0");
            res["TextPrimary"]              = Color.FromArgb("#1E293B");
            res["TextSecondary"]            = Color.FromArgb("#64748B");
            res["TextMuted"]                = Color.FromArgb("#94A3B8");
            res["ButtonPrimaryBackground"]  = Color.FromArgb("#2563EB");
            res["ButtonPrimaryText"]        = Color.FromArgb("#FFFFFF");
            res["AccentHover"]              = Color.FromArgb("#DBEAFE");
            res["SuccessText"]              = Color.FromArgb("#16A34A");

#if WINDOWS
            MauiProgram.UpdateBorderColor(dark: false); // #F8FAFC COLORREF
#endif
        }

        // Update TitleBar colors to match
        var rootPage = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (rootPage is AppLayout layout)
        {
            var tb = layout.FindByName<TitleBar>("AppTitleBar");
            if (tb is not null)
            {
                tb.TitleBarBackground = new SolidColorBrush(
                    dark ? Color.FromArgb("#161B22") : Color.FromArgb("#FFFFFF"));
                tb.ButtonForegroundColor = dark
                    ? Color.FromArgb("#94A3B8")
                    : Color.FromArgb("#64748B");
                tb.ButtonHoverColor = dark
                    ? Color.FromArgb("#2D3D5E")
                    : Color.FromArgb("#DBEAFE");
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Caption Button Themes
    // ═══════════════════════════════════════════════════════════

    private void OnThemeDefault(object? sender, EventArgs e)
    {
        var tb = FindTitleBar();
        if (tb is null) return;
        tb.ButtonForegroundColor = Color.FromArgb("#94A3B8");
        tb.ButtonHoverColor = Color.FromArgb("#2D3D5E");
        tb.ButtonPressedColor = Color.FromArgb("#1E293B");
#if WINDOWS
        var native = GetNativeTitleBar();
        if (native is not null)
            native.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
#endif
        LblLastAction.Text = "Theme → Default";
    }

    private void OnThemeBlue(object? sender, EventArgs e)
    {
        var tb = FindTitleBar();
        if (tb is null) return;
        tb.ButtonForegroundColor = Colors.White;
        tb.ButtonHoverColor = Color.FromArgb("#2563EB");
        tb.ButtonPressedColor = Color.FromArgb("#1D4ED8");
#if WINDOWS
        var native = GetNativeTitleBar();
        if (native is not null)
            native.ButtonBackgroundColor = global::Windows.UI.Color.FromArgb(80, 37, 99, 235);
#endif
        LblLastAction.Text = "Theme → Blue Accent";
    }

    private void OnThemeGreen(object? sender, EventArgs e)
    {
        var tb = FindTitleBar();
        if (tb is null) return;
        tb.ButtonForegroundColor = Colors.White;
        tb.ButtonHoverColor = Color.FromArgb("#16A34A");
        tb.ButtonPressedColor = Color.FromArgb("#15803D");
#if WINDOWS
        var native = GetNativeTitleBar();
        if (native is not null)
            native.ButtonBackgroundColor = global::Windows.UI.Color.FromArgb(80, 22, 163, 74);
#endif
        LblLastAction.Text = "Theme → Green Accent";
    }
}
