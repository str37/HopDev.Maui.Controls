#if WINDOWS
using HopDev.Maui.Controls.Platform.Abstractions;
using Microsoft.UI.Input;
using Windows.Graphics;

namespace HopDev.Maui.Controls.Platform.Windows;

/// <summary>
/// Windows implementation of <see cref="IWindowChromeService"/>.
/// 
/// Wraps WinUI3's AppWindowTitleBar API for:
/// - ExtendsContentIntoTitleBar
/// - Caption button metrics (RightInset, LeftInset)
/// - Button color customization
/// - InputNonClientPointerSource for drag/interactive region management
/// 
/// Caption button insets are read from AppWindowTitleBar and converted from physical
/// pixels to logical using <see cref="IWindowScaleService.ScaleFactor"/>.
/// 
/// Interactive regions use InputNonClientPointerSource.SetRegionRects with
/// NonClientRegionKind.Passthrough so pointer events go to app controls
/// instead of triggering window drag.
/// </summary>
public class WindowsChromeService : IWindowChromeService
{
    private Window? _mauiWindow;
    private IWindowScaleService? _scaleService;
    private Microsoft.UI.Xaml.Window? _nativeWindow;
    private Microsoft.UI.Windowing.AppWindow? _appWindow;
    private InputNonClientPointerSource? _inputSource;

    private readonly List<View> _interactiveRegions = new();
    private View? _dragRegion;
    private bool _updateScheduled;

    public bool IsContentExtendedIntoTitleBar { get; private set; }
    public Thickness CaptionButtonInsets { get; private set; } = Thickness.Zero;
    public object? NativeAppWindow => _appWindow;
    public bool IsAttached { get; private set; }

    public event EventHandler<CaptionInsetsChangedEventArgs>? CaptionInsetsChanged;

    public void ExtendContentIntoTitleBar(bool extend)
    {
        if (_appWindow?.TitleBar is { } titleBar)
        {
            titleBar.ExtendsContentIntoTitleBar = extend;

            // Make caption button backgrounds transparent so MAUI content shows through
            if (extend)
            {
                titleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
                titleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
            }

            IsContentExtendedIntoTitleBar = extend;
            UpdateCaptionInsets();
        }
    }

    public void SetDragRegion(View view)
    {
        // Unsubscribe from old drag region layout changes
        if (_dragRegion is not null)
            _dragRegion.SizeChanged -= OnRegionLayoutChanged;

        _dragRegion = view;

        // Track layout changes so we can recalculate passthrough rects
        if (view is not null)
            view.SizeChanged += OnRegionLayoutChanged;

        ScheduleRegionUpdate();
    }

    public void RegisterInteractiveRegion(View view)
    {
        if (_interactiveRegions.Contains(view)) return;

        _interactiveRegions.Add(view);
        view.SizeChanged += OnRegionLayoutChanged;

        ScheduleRegionUpdate();
    }

    public void UnregisterInteractiveRegion(View view)
    {
        if (_interactiveRegions.Remove(view))
        {
            view.SizeChanged -= OnRegionLayoutChanged;
            ScheduleRegionUpdate();
        }
    }

    public void SetButtonColors(Color? foreground, Color? hoverBackground, Color? pressedBackground)
    {
        if (_appWindow?.TitleBar is not { } titleBar) return;

        if (foreground is not null)
            titleBar.ButtonForegroundColor = foreground.ToWindowsColor();
        if (hoverBackground is not null)
            titleBar.ButtonHoverBackgroundColor = hoverBackground.ToWindowsColor();
        if (pressedBackground is not null)
            titleBar.ButtonPressedBackgroundColor = pressedBackground.ToWindowsColor();
    }

    public void Attach(Window mauiWindow, IWindowScaleService scaleService)
    {
        _mauiWindow = mauiWindow;
        _scaleService = scaleService;
        _nativeWindow = mauiWindow.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
        _appWindow = _nativeWindow?.AppWindow;

        if (_appWindow is null)
        {
            System.Diagnostics.Debug.WriteLine("[WindowsChromeService] ERROR: AppWindow is null");
            return;
        }

        // Acquire InputNonClientPointerSource for passthrough region management
        try
        {
            _inputSource = InputNonClientPointerSource.GetForWindowId(_appWindow.Id);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[WindowsChromeService] InputNonClientPointerSource unavailable: {ex.Message}");
        }

        UpdateCaptionInsets();

        // Re-measure caption insets when DPI changes
        scaleService.ScaleChanged += (_, _) =>
        {
            UpdateCaptionInsets();
            ScheduleRegionUpdate();
        };

        IsAttached = true;
        System.Diagnostics.Debug.WriteLine(
            $"[WindowsChromeService] Attached — caption insets: {CaptionButtonInsets}, " +
            $"InputSource: {(_inputSource is not null ? "✓" : "✗")}");
    }

    public void Detach()
    {
        // Unsubscribe layout change handlers
        if (_dragRegion is not null)
            _dragRegion.SizeChanged -= OnRegionLayoutChanged;
        foreach (var view in _interactiveRegions)
            view.SizeChanged -= OnRegionLayoutChanged;

        // Clear passthrough regions
        try
        {
            _inputSource?.SetRegionRects(NonClientRegionKind.Passthrough, Array.Empty<RectInt32>());
        }
        catch { /* Window may already be destroyed */ }

        _interactiveRegions.Clear();
        _dragRegion = null;
        _inputSource = null;
        _mauiWindow = null;
        _nativeWindow = null;
        _appWindow = null;
        IsAttached = false;
    }

    // ═══════════════════════════════════════════════════════════
    // Caption Button Metrics
    // ═══════════════════════════════════════════════════════════

    private void UpdateCaptionInsets()
    {
        if (_appWindow?.TitleBar is not { } titleBar || _scaleService is null) return;

        var scale = _scaleService.ScaleFactor;
        if (scale <= 0) scale = 1.0;

        // TitleBar.RightInset and LeftInset are in physical pixels
        var rightInset = titleBar.RightInset / scale;
        var leftInset = titleBar.LeftInset / scale;

        var newInsets = new Thickness(leftInset, 0, rightInset, 0);

        if (newInsets != CaptionButtonInsets)
        {
            CaptionButtonInsets = newInsets;
            CaptionInsetsChanged?.Invoke(this, new CaptionInsetsChangedEventArgs(newInsets));
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Interactive Region Management via InputNonClientPointerSource
    // ═══════════════════════════════════════════════════════════

    private void OnRegionLayoutChanged(object? sender, EventArgs e)
    {
        ScheduleRegionUpdate();
    }

    /// <summary>
    /// Coalesce rapid layout changes into a single passthrough rect update.
    /// Multiple views may resize simultaneously (e.g., on DPI change) — we
    /// batch them into one SetRegionRects call to avoid flicker and overhead.
    /// </summary>
    private void ScheduleRegionUpdate()
    {
        if (_updateScheduled || _inputSource is null) return;
        _updateScheduled = true;

        // Post to UI thread — layout may still be in progress
        _mauiWindow?.Dispatcher.Dispatch(() =>
        {
            _updateScheduled = false;
            UpdatePassthroughRegions();
        });
    }

    /// <summary>
    /// Compute bounds of all interactive views in physical pixels and set them
    /// as passthrough regions on InputNonClientPointerSource. The OS will route
    /// pointer events in these regions to the app instead of treating them as
    /// title bar drag operations.
    /// </summary>
    private void UpdatePassthroughRegions()
    {
        if (_inputSource is null || _scaleService is null || _nativeWindow is null) return;

        var scale = _scaleService.ScaleFactor;
        if (scale <= 0) scale = 1.0;

        var rects = new List<RectInt32>();

        foreach (var view in _interactiveRegions)
        {
            var rect = GetViewRectInPhysicalPixels(view, scale);
            if (rect.HasValue)
                rects.Add(rect.Value);
        }

        try
        {
            _inputSource.SetRegionRects(NonClientRegionKind.Passthrough, rects.ToArray());

            System.Diagnostics.Debug.WriteLine(
                $"[WindowsChromeService] Updated passthrough regions: {rects.Count} rects " +
                $"(scale={scale:F2})");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[WindowsChromeService] SetRegionRects failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets a MAUI View's bounding rectangle in physical pixel coordinates
    /// relative to the window's client area. Returns null if the view isn't
    /// laid out or isn't visible.
    /// </summary>
    private RectInt32? GetViewRectInPhysicalPixels(View view, double scale)
    {
        if (view.Width <= 0 || view.Height <= 0 || !view.IsVisible) return null;

        try
        {
            // Get the view's native WinUI element
            var nativeView = view.Handler?.PlatformView as Microsoft.UI.Xaml.FrameworkElement;
            if (nativeView is null || nativeView.ActualWidth <= 0) return null;

            // TransformToVisual(null) gives position in DIP relative to window content
            var transform = nativeView.TransformToVisual(null);
            var origin = transform.TransformPoint(new global::Windows.Foundation.Point(0, 0));

            // Convert DIPs to physical pixels (InputNonClientPointerSource expects physical)
            return new RectInt32(
                (int)(origin.X * scale),
                (int)(origin.Y * scale),
                (int)(nativeView.ActualWidth * scale),
                (int)(nativeView.ActualHeight * scale));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[WindowsChromeService] GetViewRect failed for {view.GetType().Name}: {ex.Message}");
            return null;
        }
    }
}

/// <summary>
/// Extension to convert MAUI Color to Windows.UI.Color.
/// </summary>
internal static class ColorExtensions
{
    public static global::Windows.UI.Color ToWindowsColor(this Color color)
    {
        return global::Windows.UI.Color.FromArgb(
            (byte)(color.Alpha * 255),
            (byte)(color.Red * 255),
            (byte)(color.Green * 255),
            (byte)(color.Blue * 255));
    }
}
#endif
