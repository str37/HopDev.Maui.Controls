namespace HopDev.Maui.Controls.Platform.Abstractions;

/// <summary>
/// Controls window chrome — title bar extension, drag regions, caption button metrics.
/// 
/// On Windows, wraps AppWindowTitleBar and InputNonClientPointerSource APIs.
/// On other platforms, provides no-op or platform-appropriate equivalents.
/// 
/// One instance per Window. Requires <see cref="IWindowScaleService"/> to be attached first.
/// </summary>
public interface IWindowChromeService
{
    /// <summary>
    /// Tell the platform to extend app content into the title bar area.
    /// When true, the app draws behind the caption buttons and the standard
    /// title text is hidden.
    /// </summary>
    void ExtendContentIntoTitleBar(bool extend);

    /// <summary>
    /// Whether content is currently extended into the title bar.
    /// </summary>
    bool IsContentExtendedIntoTitleBar { get; }

    /// <summary>
    /// Set which MAUI View acts as the drag region. Pointer events on this
    /// element (that don't hit an interactive region) trigger window move.
    /// </summary>
    void SetDragRegion(View view);

    /// <summary>
    /// Register a View as an interactive (passthrough) region. Pointer input
    /// goes to the control instead of triggering window drag.
    /// </summary>
    void RegisterInteractiveRegion(View view);

    /// <summary>
    /// Remove a previously registered interactive region.
    /// </summary>
    void UnregisterInteractiveRegion(View view);

    /// <summary>
    /// Caption button dimensions in logical pixels.
    /// <see cref="Thickness.Right"/> = total width of min/max/close buttons.
    /// <see cref="Thickness.Left"/> = back button / system reserved left space.
    /// <see cref="Thickness.Top"/> = any top inset (usually 0).
    /// </summary>
    Thickness CaptionButtonInsets { get; }

    /// <summary>
    /// Fires when caption button metrics change (DPI change, window state change).
    /// </summary>
    event EventHandler<CaptionInsetsChangedEventArgs>? CaptionInsetsChanged;

    /// <summary>
    /// Set caption button colors. Pass null for any parameter to use platform defaults.
    /// </summary>
    void SetButtonColors(Color? foreground, Color? hoverBackground, Color? pressedBackground);

    /// <summary>
    /// The native AppWindow (Windows) or equivalent. Typed as object to avoid
    /// leaking platform types into the interface. Cast on the platform side.
    /// </summary>
    object? NativeAppWindow { get; }

    /// <summary>
    /// Initialize and attach. Requires <see cref="IWindowScaleService"/> already attached
    /// on the same Window — the chrome service uses it for DPI-correct measurements.
    /// </summary>
    void Attach(Window mauiWindow, IWindowScaleService scaleService);

    /// <summary>
    /// True after <see cref="Attach"/> has completed.
    /// </summary>
    bool IsAttached { get; }

    /// <summary>
    /// Detach from the window and release native resources.
    /// </summary>
    void Detach();
}
