#if WINDOWS
using HopDev.Maui.Controls.Platform.Abstractions;
using HopDev.Maui.Controls.Platform.Types;

namespace HopDev.Maui.Controls.Platform.Windows;

/// <summary>
/// Windows implementation of <see cref="IWindowScaleService"/>.
/// 
/// Uses the empirical scale detection method discovered during SmartScrollPanel 4K investigation:
/// <c>AppWindow.Size.Width / mauiWindow.Width</c>
/// 
/// This is the ONLY reliable approach for unpackaged MAUI apps (WindowsPackageType=None)
/// where DPI virtualization causes GetDpiForWindow to lie (returns 96 on 150% monitors).
/// 
/// Subscribes to XamlRoot.Changed to detect RasterizationScale changes when the window
/// moves between monitors with different DPI settings.
/// </summary>
public class WindowsScaleService : IWindowScaleService
{
    private Window? _mauiWindow;
    private Microsoft.UI.Xaml.Window? _nativeWindow;
    private Microsoft.UI.Windowing.AppWindow? _appWindow;
    private double _scaleFactor = 1.0;

    public double ScaleFactor => _scaleFactor;
    public nint WindowHandle { get; private set; }
    public bool IsAttached { get; private set; }

    public event EventHandler<ScaleChangedEventArgs>? ScaleChanged;

    public LogicalPoint ToLogical(PhysicalPoint physical) =>
        new(physical.X / _scaleFactor, physical.Y / _scaleFactor);

    public PhysicalPoint ToPhysical(LogicalPoint logical) =>
        new(logical.X * _scaleFactor, logical.Y * _scaleFactor);

    public void Attach(Window mauiWindow)
    {
        _mauiWindow = mauiWindow;
        _nativeWindow = mauiWindow.Handler?.PlatformView as Microsoft.UI.Xaml.Window;

        if (_nativeWindow is null)
        {
            System.Diagnostics.Debug.WriteLine("[WindowsScaleService] ERROR: native window is null");
            return;
        }

        _appWindow = _nativeWindow.AppWindow;
        WindowHandle = WinRT.Interop.WindowNative.GetWindowHandle(_nativeWindow);

        // Compute initial scale empirically — the only reliable method
        ComputeEmpiricalScale();

        // Subscribe to XamlRoot.Changed for DPI change detection
        if (_nativeWindow.Content?.XamlRoot is { } xamlRoot)
        {
            xamlRoot.Changed += OnXamlRootChanged;
        }

        IsAttached = true;
        System.Diagnostics.Debug.WriteLine(
            $"[WindowsScaleService] Attached — scale={_scaleFactor:F2}, HWND=0x{WindowHandle:X}");
    }

    public void Detach()
    {
        if (_nativeWindow?.Content?.XamlRoot is { } xamlRoot)
        {
            xamlRoot.Changed -= OnXamlRootChanged;
        }

        _mauiWindow = null;
        _nativeWindow = null;
        _appWindow = null;
        IsAttached = false;
    }

    private void ComputeEmpiricalScale()
    {
        if (_appWindow is null || _mauiWindow is null) return;

        var physicalWidth = _appWindow.Size.Width;
        var logicalWidth = _mauiWindow.Width;

        if (logicalWidth > 0 && physicalWidth > 0)
        {
            _scaleFactor = physicalWidth / logicalWidth;
        }

        // Sanity check — scale should be 1.0-4.0 for any real display
        if (_scaleFactor < 0.5 || _scaleFactor > 5.0)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[WindowsScaleService] WARNING: empirical scale {_scaleFactor:F2} looks wrong, " +
                $"falling back to XamlRoot.RasterizationScale");

            _scaleFactor = _nativeWindow?.Content?.XamlRoot?.RasterizationScale ?? 1.0;
        }
    }

    private void OnXamlRootChanged(Microsoft.UI.Xaml.XamlRoot sender, Microsoft.UI.Xaml.XamlRootChangedEventArgs args)
    {
        var oldScale = _scaleFactor;
        ComputeEmpiricalScale();

        if (Math.Abs(oldScale - _scaleFactor) > 0.01)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[WindowsScaleService] *** DPI CHANGE: {oldScale:F2} → {_scaleFactor:F2} ***");

            ScaleChanged?.Invoke(this, new ScaleChangedEventArgs(oldScale, _scaleFactor));
        }
    }
}
#endif
