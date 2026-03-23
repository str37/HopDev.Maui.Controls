using HopDev.Maui.Controls.Platform.Types;

namespace HopDev.Maui.Controls.Platform.Abstractions;

/// <summary>
/// Provides reliable DPI/scale information for a MAUI Window.
/// 
/// On Windows, uses the empirical method (AppWindow.Size.Width / MAUI Window.Width)
/// which is the only approach that works correctly under DPI virtualization
/// (unpackaged apps where GetDpiForWindow lies and returns 96 on scaled monitors).
/// 
/// One instance per Window. Resolved via <see cref="HopDevServices"/> or DI.
/// </summary>
public interface IWindowScaleService
{
    /// <summary>
    /// The real display scale factor, computed empirically.
    /// 1.0 = 100% (96 DPI), 1.5 = 150% (144 DPI), 2.0 = 200% (192 DPI).
    /// Always accurate — does not rely on GetDpiForWindow which lies under virtualization.
    /// </summary>
    double ScaleFactor { get; }

    /// <summary>
    /// Fires when the window moves to a monitor with a different scale factor.
    /// </summary>
    event EventHandler<ScaleChangedEventArgs>? ScaleChanged;

    /// <summary>
    /// Converts physical screen coordinates (actual pixels) to MAUI logical coordinates.
    /// Uses the empirical scale factor for correct results at any DPI.
    /// </summary>
    LogicalPoint ToLogical(PhysicalPoint physical);

    /// <summary>
    /// Converts MAUI logical coordinates to physical screen coordinates.
    /// </summary>
    PhysicalPoint ToPhysical(LogicalPoint logical);

    /// <summary>
    /// The native window handle. HWND on Windows, IntPtr.Zero on other platforms.
    /// </summary>
    nint WindowHandle { get; }

    /// <summary>
    /// Initialize and attach to a MAUI Window. Called once during window creation.
    /// </summary>
    void Attach(Window mauiWindow);

    /// <summary>
    /// True after <see cref="Attach"/> has completed and <see cref="ScaleFactor"/> is available.
    /// </summary>
    bool IsAttached { get; }

    /// <summary>
    /// Detach from the window and release native resources.
    /// </summary>
    void Detach();
}
