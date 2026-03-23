namespace HopDev.Maui.Controls.Platform.Abstractions;

/// <summary>
/// Centralized pointer event interception for a Window.
/// 
/// On Windows, installs a single WH_MOUSE_LL hook per window (instead of per-control)
/// and dispatches wheel events to registered scroll regions with DPI-corrected coordinates.
/// 
/// This solves two bugs from SmartScrollPanel's original per-control hook approach:
/// 1. "Wrong panel gets scroll" — the service has a global view of all registered regions
/// 2. "Dead zone at >100% scaling" — coordinates go through IWindowScaleService.ToLogical()
/// 
/// One instance per Window. Requires <see cref="IWindowScaleService"/> to be attached first.
/// </summary>
public interface IPointerInterceptService
{
    /// <summary>
    /// Register a MAUI View as a scroll region. When a wheel event's DPI-corrected
    /// position falls within this view's bounds, the handler is invoked.
    /// </summary>
    /// <param name="view">The MAUI View whose bounds define the scroll region.</param>
    /// <param name="handler">Callback invoked with DPI-corrected wheel event data.</param>
    void RegisterScrollRegion(View view, Action<PointerWheelEventArgs> handler);

    /// <summary>
    /// Remove a previously registered scroll region.
    /// </summary>
    void UnregisterScrollRegion(View view);

    /// <summary>
    /// Fires for any wheel event on the window, with corrected coordinates.
    /// For controls that want raw access without region-based dispatch.
    /// </summary>
    event EventHandler<PointerWheelEventArgs>? WheelEvent;

    /// <summary>
    /// Initialize and attach. Requires <see cref="IWindowScaleService"/> already attached
    /// on the same Window — the pointer service uses it for coordinate conversion.
    /// </summary>
    void Attach(Window mauiWindow, IWindowScaleService scaleService);

    /// <summary>
    /// True after <see cref="Attach"/> has completed and the hook is active.
    /// </summary>
    bool IsAttached { get; }

    /// <summary>
    /// Detach the hook and release native resources.
    /// </summary>
    void Detach();
}
