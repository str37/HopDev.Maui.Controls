using System.Runtime.InteropServices;

namespace HopDev.Maui.Controls.Extensions;

/// <summary>
/// Configures a borderless window where the app draws its own title bar.
///
/// The 1px border at the top of the window is the DWM window border.
/// It is intentionally kept — it provides a visual edge indicator that
/// helps users locate the window boundary, especially in dark mode
/// where a borderless window can blend into dark backgrounds.
/// On Win11 this border is theme-colored. On Win10 it respects
/// DWMWA_USE_IMMERSIVE_DARK_MODE for dark mode rendering.
///
/// Requirements for consuming app:
///   1. Window.Title = "" in App.xaml.cs
///   2. Optionally set WindowCaptionBackground = Transparent in Platforms/Windows/App.xaml (belt-and-suspenders)
///   3. Do NOT set WindowCaptionForeground to Transparent — the library handles this automatically
///   4. PerMonitorV2 DPI manifest in Platforms/Windows/app.manifest
///
/// Usage:
///   builder.UseHopDevControls().UseBorderlessWindow();
/// </summary>
public static class BorderlessWindowExtensions
{
#if WINDOWS
    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd, uint dwAttribute, ref int pvAttribute, uint cbAttribute);

    private const uint DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
#endif

    /// <summary>
    /// Configure a borderless window. Call after UseHopDevControls().
    /// Removes standard Windows chrome, pulls content up past MAUI's 32px
    /// AppTitleBarContainer, and enables DWM dark mode for the window border.
    /// </summary>
    public static MauiAppBuilder UseBorderlessWindow(this MauiAppBuilder builder)
    {
#if WINDOWS
        Microsoft.Maui.Handlers.WindowHandler.Mapper.AppendToMapping(
            "BorderlessWindow", (handler, view) =>
            {
                var nativeWindow = handler.PlatformView as Microsoft.UI.Xaml.Window;
                if (nativeWindow is null) return;

                // Tell WinUI we own the title bar
                nativeWindow.ExtendsContentIntoTitleBar = true;
                nativeWindow.SetTitleBar(null);

                // Configure caption buttons
                if (nativeWindow.AppWindow?.TitleBar is { } titleBar)
                {
                    titleBar.ExtendsContentIntoTitleBar = true;
                    titleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
                    titleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
                    titleBar.IconShowOptions = Microsoft.UI.Windowing.IconShowOptions.HideIconAndSystemMenu;
                    titleBar.PreferredHeightOption = Microsoft.UI.Windowing.TitleBarHeightOption.Tall;
                }

                // Pull content up past MAUI's reserved 32px AppTitleBarContainer.
                // MAUI .NET 10 injects a 32px ContentControl that cannot be collapsed.
                // Negative margin is the proven workaround (dotnet/maui#22894).
                if (nativeWindow.Content is Microsoft.UI.Xaml.FrameworkElement root)
                {
                    root.Margin = new Microsoft.UI.Xaml.Thickness(0, -32, 0, 0);

                    // Belt-and-suspenders: set caption background transparent at runtime
                    // in case the consuming app forgot the Platforms/Windows/App.xaml override.
                    root.Resources["WindowCaptionBackground"] =
                        new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
                    root.Resources["WindowCaptionBackgroundDisabled"] =
                        new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);

                    // CRITICAL: Override WindowCaptionForeground with a visible fallback.
                    // If a consuming app sets this to Transparent (a common mistake), WinUI
                    // re-applies it on every focus change and hides the min/max/close glyphs.
                    // A medium gray works for both light and dark themes as a safe fallback;
                    // TitleBar.SetButtonColors() overrides this with the actual theme color.
                    root.Resources["WindowCaptionForeground"] =
                        new Microsoft.UI.Xaml.Media.SolidColorBrush(
                            Microsoft.UI.ColorHelper.FromArgb(0xFF, 0x88, 0x88, 0x88));
                    root.Resources["WindowCaptionForegroundDisabled"] =
                        new Microsoft.UI.Xaml.Media.SolidColorBrush(
                            Microsoft.UI.ColorHelper.FromArgb(0xFF, 0xAA, 0xAA, 0xAA));
                }

                // Enable DWM dark mode rendering for the window border.
                // Without this, Win10 draws a white border even in dark theme apps.
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow);
                int darkMode = 1;
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE,
                    ref darkMode, sizeof(int));

                // Runtime DPI check — warn if manifest is missing
                var dpi = GetDpiForWindow(hwnd);
                if (dpi == 96)
                {
                    System.Diagnostics.Debug.WriteLine(
                        "[UseBorderlessWindow] WARNING: GetDpiForWindow returned 96 — " +
                        "PerMonitorV2 DPI manifest may be missing. " +
                        "Add Platforms/Windows/app.manifest with PerMonitorV2 DPI awareness.");
                }

                // Auto-sync DWM border with app theme changes
                if (Application.Current is { } app)
                {
                    app.RequestedThemeChanged += (_, args) =>
                    {
                        var isDark = app.UserAppTheme == AppTheme.Dark ||
                            (app.UserAppTheme == AppTheme.Unspecified &&
                             args.RequestedTheme == AppTheme.Dark);
                        int mode = isDark ? 1 : 0;
                        DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE,
                            ref mode, sizeof(int));
                    };
                }
            });
#endif
        return builder;
    }

    /// <summary>
    /// Update DWM dark mode for theme changes. Call when toggling light/dark.
    /// </summary>
    public static void UpdateBorderColor(bool dark)
    {
#if WINDOWS
        var window = Application.Current?.Windows.FirstOrDefault();
        var nativeWindow = window?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
        if (nativeWindow is null) return;
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow);
        int darkMode = dark ? 1 : 0;
        DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE,
            ref darkMode, sizeof(int));
#endif
    }

#if WINDOWS
    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);
#endif
}
