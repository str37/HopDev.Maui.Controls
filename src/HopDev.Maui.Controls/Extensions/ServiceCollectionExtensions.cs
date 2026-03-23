using HopDev.Maui.Controls.Platform;
using HopDev.Maui.Controls.Platform.Abstractions;

namespace HopDev.Maui.Controls.Extensions;

/// <summary>
/// Extension methods for registering HopDev.Maui.Controls platform services.
/// 
/// Usage in MauiProgram.cs:
/// <code>
/// var builder = MauiApp.CreateBuilder();
/// builder.UseHopDevControls();
/// </code>
/// 
/// This wires lifecycle events to auto-create and attach per-Window services
/// (IWindowScaleService, IWindowChromeService, IPointerInterceptService).
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register HopDev.Maui.Controls platform services and wire lifecycle hooks
    /// for automatic per-Window service attachment and cleanup.
    /// </summary>
    public static MauiAppBuilder UseHopDevControls(this MauiAppBuilder builder)
    {
        // Wire MAUI Window lifecycle for cleanup.
        // Attachment is lazy — triggered by controls on HandlerChanged via
        // EnsureHopDevServicesAttached(). Detachment must be explicit to release
        // native hooks (WH_MOUSE_LL), XamlRoot subscriptions, and interactive
        // region lists. This fires once per Window when its handler is created.
        Microsoft.Maui.Handlers.WindowHandler.Mapper.AppendToMapping("HopDevServicesCleanup",
            (handler, view) =>
            {
                if (view is Window mauiWindow)
                {
                    mauiWindow.Destroying += (_, _) => DetachHopDevServices(mauiWindow);
                }
            });

        return builder;
    }

    /// <summary>
    /// Called by controls (or app code) to ensure services are created and attached
    /// for a given Window. Safe to call multiple times — idempotent.
    /// </summary>
    public static void EnsureHopDevServicesAttached(Window mauiWindow)
    {
        // Scale service — always first, others depend on it
        var scaleService = HopDevServices.GetScaleService(mauiWindow);
        if (scaleService is null)
        {
            scaleService = CreateScaleService();
            scaleService.Attach(mauiWindow);
            HopDevServices.SetScaleService(mauiWindow, scaleService);
        }

        // Chrome service
        var chromeService = HopDevServices.GetChromeService(mauiWindow);
        if (chromeService is null)
        {
            chromeService = CreateChromeService();
            chromeService.Attach(mauiWindow, scaleService);
            HopDevServices.SetChromeService(mauiWindow, chromeService);
        }

        // Pointer intercept service
        var pointerService = HopDevServices.GetPointerService(mauiWindow);
        if (pointerService is null)
        {
            pointerService = CreatePointerService();
            pointerService.Attach(mauiWindow, scaleService);
            HopDevServices.SetPointerService(mauiWindow, pointerService);
        }
    }

    /// <summary>
    /// Detach and clean up all services for a Window. Called automatically on
    /// window destroy when <see cref="UseHopDevControls"/> is registered.
    /// Releases native hooks (WH_MOUSE_LL), XamlRoot subscriptions, and
    /// interactive region lists. Safe to call multiple times.
    /// </summary>
    public static void DetachHopDevServices(Window mauiWindow)
    {
        var pointer = HopDevServices.GetPointerService(mauiWindow);
        var chrome = HopDevServices.GetChromeService(mauiWindow);
        var scale = HopDevServices.GetScaleService(mauiWindow);

        pointer?.Detach();
        chrome?.Detach();
        scale?.Detach();

        // Clear attached properties so IsAttached checks are accurate
        mauiWindow.ClearValue(HopDevServices.PointerServiceProperty);
        mauiWindow.ClearValue(HopDevServices.ChromeServiceProperty);
        mauiWindow.ClearValue(HopDevServices.ScaleServiceProperty);

        System.Diagnostics.Debug.WriteLine(
            $"[HopDevServices] Detached all services for window " +
            $"(scale={scale is not null}, chrome={chrome is not null}, pointer={pointer is not null})");
    }

    // ═══════════════════════════════════════════════════════════
    // Platform-specific factory methods
    // ═══════════════════════════════════════════════════════════

    private static IWindowScaleService CreateScaleService()
    {
#if WINDOWS
        return new Platform.Windows.WindowsScaleService();
#else
        return new NoOpScaleService();
#endif
    }

    private static IWindowChromeService CreateChromeService()
    {
#if WINDOWS
        return new Platform.Windows.WindowsChromeService();
#else
        return new NoOpChromeService();
#endif
    }

    private static IPointerInterceptService CreatePointerService()
    {
#if WINDOWS
        return new Platform.Windows.WindowsPointerInterceptService();
#else
        return new NoOpPointerInterceptService();
#endif
    }
}
