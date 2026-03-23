using HopDev.Maui.Controls.Platform.Abstractions;

namespace HopDev.Maui.Controls.Platform;

/// <summary>
/// Central accessor for per-Window platform services.
/// Services are stored as attached properties on the MAUI Window,
/// scoping them correctly for multi-window apps (e.g., HopDev.Cloud sidebar windows).
/// 
/// Usage from any control:
/// <code>
/// var scaleService = HopDevServices.GetScaleService(this.Window);
/// </code>
/// 
/// Services are auto-created and attached by <see cref="Extensions.ServiceCollectionExtensions.UseHopDevControls"/>.
/// </summary>
public static class HopDevServices
{
    // ═══════════════════════════════════════════════════════════
    // Attached Properties — one service instance per Window
    // ═══════════════════════════════════════════════════════════

    public static readonly BindableProperty ScaleServiceProperty =
        BindableProperty.CreateAttached(
            "ScaleService",
            typeof(IWindowScaleService),
            typeof(HopDevServices),
            null);

    public static readonly BindableProperty ChromeServiceProperty =
        BindableProperty.CreateAttached(
            "ChromeService",
            typeof(IWindowChromeService),
            typeof(HopDevServices),
            null);

    public static readonly BindableProperty PointerServiceProperty =
        BindableProperty.CreateAttached(
            "PointerService",
            typeof(IPointerInterceptService),
            typeof(HopDevServices),
            null);

    // ═══════════════════════════════════════════════════════════
    // Public Accessors
    // ═══════════════════════════════════════════════════════════

    /// <summary>Get the <see cref="IWindowScaleService"/> for a Window. Returns null if not attached.</summary>
    public static IWindowScaleService? GetScaleService(Window window) =>
        (IWindowScaleService?)window.GetValue(ScaleServiceProperty);

    /// <summary>Get the <see cref="IWindowChromeService"/> for a Window. Returns null if not attached.</summary>
    public static IWindowChromeService? GetChromeService(Window window) =>
        (IWindowChromeService?)window.GetValue(ChromeServiceProperty);

    /// <summary>Get the <see cref="IPointerInterceptService"/> for a Window. Returns null if not attached.</summary>
    public static IPointerInterceptService? GetPointerService(Window window) =>
        (IPointerInterceptService?)window.GetValue(PointerServiceProperty);

    // ═══════════════════════════════════════════════════════════
    // Internal Setters (used by lifecycle hooks)
    // ═══════════════════════════════════════════════════════════

    internal static void SetScaleService(Window window, IWindowScaleService service) =>
        window.SetValue(ScaleServiceProperty, service);

    internal static void SetChromeService(Window window, IWindowChromeService service) =>
        window.SetValue(ChromeServiceProperty, service);

    internal static void SetPointerService(Window window, IPointerInterceptService service) =>
        window.SetValue(PointerServiceProperty, service);

    // ═══════════════════════════════════════════════════════════
    // Convenience — resolve from a View (walks up to Window)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Get the <see cref="IWindowScaleService"/> for the Window containing this View.
    /// Convenient for use inside controls.
    /// </summary>
    public static IWindowScaleService? GetScaleService(View view) =>
        view.Window is { } w ? GetScaleService(w) : null;

    /// <summary>
    /// Get the <see cref="IWindowChromeService"/> for the Window containing this View.
    /// </summary>
    public static IWindowChromeService? GetChromeService(View view) =>
        view.Window is { } w ? GetChromeService(w) : null;

    /// <summary>
    /// Get the <see cref="IPointerInterceptService"/> for the Window containing this View.
    /// </summary>
    public static IPointerInterceptService? GetPointerService(View view) =>
        view.Window is { } w ? GetPointerService(w) : null;
}
