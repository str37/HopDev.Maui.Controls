namespace HopDev.Maui.Controls.Controls;

/// <summary>
/// Drop-in ScrollView replacement that fixes WinUI3's broken mouse-wheel scrolling
/// on multi-monitor setups with mixed DPI scaling.
///
/// Why this exists: WinUI3 routes mouse input through internal InputSite threads
/// that MAUI's ScrollView never sees. A standard ScrollView works for touch/trackpad
/// but mouse-wheel scrolling is unreliable or completely broken, especially after
/// moving windows between monitors with different scaling.
///
/// How it works:
///   1. Wraps a native MAUI ScrollView for layout/measurement.
///   2. Installs a WH_MOUSE_LL hook to intercept wheel events before WinUI3 eats them.
///   3. Uses WinUI3 TransformToVisual for DPI-safe hit-testing in physical screen coords.
///   4. Monitors XamlRoot.Changed for DPI changes and forces native re-layout.
///   5. Overlays a custom scrollbar thumb (hides the native one).
///
/// IMPORTANT: The consuming app MUST include a PerMonitorV2 DPI-awareness manifest
/// for unpackaged (WindowsPackageType=None) builds. Without it, Win32 APIs return
/// virtualized coordinates that don't match the low-level hook's physical coords.
/// See README.md for the required app.manifest content.
///
/// Usage:
///   &lt;smart:SmartScrollPanel ScrollBarWidth="8" ThumbColor="{DynamicResource TextMuted}"&gt;
///       &lt;VerticalStackLayout&gt;
///           ... your content ...
///       &lt;/VerticalStackLayout&gt;
///   &lt;/smart:SmartScrollPanel&gt;
/// </summary>
[ContentProperty(nameof(Body))]
public class SmartScrollPanel : ContentView
{
    // ═══════════════════════════════════════════════════════════
    // Bindable Properties
    // ═══════════════════════════════════════════════════════════

    public static readonly BindableProperty BodyProperty = BindableProperty.Create(
        nameof(Body), typeof(View), typeof(SmartScrollPanel), null,
        propertyChanged: OnBodyChanged);

    public static readonly BindableProperty OrientationProperty = BindableProperty.Create(
        nameof(Orientation), typeof(ScrollOrientation), typeof(SmartScrollPanel),
        ScrollOrientation.Vertical, propertyChanged: OnOrientationChanged);

    public static readonly BindableProperty ScrollBarWidthProperty = BindableProperty.Create(
        nameof(ScrollBarWidth), typeof(double), typeof(SmartScrollPanel), 10.0,
        propertyChanged: OnScrollBarWidthChanged);

    public static readonly BindableProperty ThumbColorProperty = BindableProperty.Create(
        nameof(ThumbColor), typeof(Color), typeof(SmartScrollPanel), Colors.Gray,
        propertyChanged: OnThumbColorChanged);

    public static readonly BindableProperty ThumbCornerRadiusProperty = BindableProperty.Create(
        nameof(ThumbCornerRadius), typeof(double), typeof(SmartScrollPanel), 5.0,
        propertyChanged: OnThumbCornerRadiusChanged);

    public static readonly BindableProperty ThumbMinHeightProperty = BindableProperty.Create(
        nameof(ThumbMinHeight), typeof(double), typeof(SmartScrollPanel), 30.0);

    public static readonly BindableProperty ScrollSensitivityProperty = BindableProperty.Create(
        nameof(ScrollSensitivity), typeof(double), typeof(SmartScrollPanel), 1.0);

    // ═══════════════════════════════════════════════════════════
    // CLR Properties
    // ═══════════════════════════════════════════════════════════

    public View? Body { get => (View?)GetValue(BodyProperty); set => SetValue(BodyProperty, value); }
    public ScrollOrientation Orientation { get => (ScrollOrientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }
    public double ScrollBarWidth { get => (double)GetValue(ScrollBarWidthProperty); set => SetValue(ScrollBarWidthProperty, value); }
    public Color ThumbColor { get => (Color)GetValue(ThumbColorProperty); set => SetValue(ThumbColorProperty, value); }
    public double ThumbCornerRadius { get => (double)GetValue(ThumbCornerRadiusProperty); set => SetValue(ThumbCornerRadiusProperty, value); }
    public double ThumbMinHeight { get => (double)GetValue(ThumbMinHeightProperty); set => SetValue(ThumbMinHeightProperty, value); }
    public double ScrollSensitivity { get => (double)GetValue(ScrollSensitivityProperty); set => SetValue(ScrollSensitivityProperty, value); }

    // ═══════════════════════════════════════════════════════════
    // Events & Public State
    // ═══════════════════════════════════════════════════════════

    public event EventHandler<SmartScrolledEventArgs>? Scrolled;

    public double ScrollY => _scrollView.ScrollY;
    public double ScrollX => _scrollView.ScrollX;

    // ═══════════════════════════════════════════════════════════
    // Internal Views
    // ═══════════════════════════════════════════════════════════

    private readonly ScrollView _scrollView;
    private readonly Grid _root;

    private readonly Border _vThumb;
    private double _vThumbHeight;
    private double _vThumbDragStartTranslation;

    private readonly Border _hThumb;
    private double _hThumbWidth;
    private double _hThumbDragStartTranslation;

    // ═══════════════════════════════════════════════════════════
    // Constructor
    // ═══════════════════════════════════════════════════════════

    public SmartScrollPanel()
    {
        _scrollView = new ScrollView
        {
            Orientation = Orientation,
            VerticalScrollBarVisibility = ScrollBarVisibility.Never,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Never,
        };
        _scrollView.Scrolled += OnInternalScrolled;
        _scrollView.SizeChanged += (_, _) => RefreshThumbs();

        _vThumb = CreateThumb(vertical: true);
        _hThumb = CreateThumb(vertical: false);

        _root = new Grid { IsClippedToBounds = true };
        _root.Children.Add(_scrollView);
        _root.Children.Add(_vThumb);
        _root.Children.Add(_hThumb);

        Content = _root;

        HandlerChanged += OnSelfHandlerChanged;
        Loaded += OnLoaded;
    }

    private Border CreateThumb(bool vertical)
    {
        var thumb = new Border
        {
            BackgroundColor = ThumbColor,
            StrokeThickness = 0,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
            {
                CornerRadius = new CornerRadius(ThumbCornerRadius)
            },
            Opacity = 0.6,
            IsVisible = false,
            InputTransparent = false,
        };

        if (vertical)
        {
            thumb.WidthRequest = ScrollBarWidth;
            thumb.HeightRequest = 50;
            thumb.VerticalOptions = LayoutOptions.Start;
            thumb.HorizontalOptions = LayoutOptions.End;
            thumb.Margin = new Thickness(0, 2, 2, 2);

            var pan = new PanGestureRecognizer();
            pan.PanUpdated += OnVThumbPan;
            thumb.GestureRecognizers.Add(pan);
        }
        else
        {
            thumb.HeightRequest = ScrollBarWidth;
            thumb.WidthRequest = 50;
            thumb.VerticalOptions = LayoutOptions.End;
            thumb.HorizontalOptions = LayoutOptions.Start;
            thumb.Margin = new Thickness(2, 0, 2, 2);

            var pan = new PanGestureRecognizer();
            pan.PanUpdated += OnHThumbPan;
            thumb.GestureRecognizers.Add(pan);
        }

        var hover = new PointerGestureRecognizer();
        hover.PointerEntered += (_, _) => thumb.Opacity = 1.0;
        hover.PointerExited += (_, _) => thumb.Opacity = 0.6;
        thumb.GestureRecognizers.Add(hover);

        return thumb;
    }

    // ═══════════════════════════════════════════════════════════
    // Lifecycle
    // ═══════════════════════════════════════════════════════════

    private void OnLoaded(object? sender, EventArgs e)
    {
#if WINDOWS
        WireHook(0);
        WireXamlRootChanged();
#endif
    }

    private void OnSelfHandlerChanged(object? sender, EventArgs e)
    {
#if WINDOWS
        WireHook(0);
        WireXamlRootChanged();
#endif
    }

    // ═══════════════════════════════════════════════════════════
    // Content Management
    // ═══════════════════════════════════════════════════════════

    private static void OnBodyChanged(BindableObject b, object? oldVal, object? newVal)
    {
        var panel = (SmartScrollPanel)b;
        if (panel._scrollView.Content is View old) old.SizeChanged -= panel.OnContentSizeChanged;
        panel._scrollView.Content = newVal as View;
        if (newVal is View v) v.SizeChanged += panel.OnContentSizeChanged;
    }

    private void OnContentSizeChanged(object? sender, EventArgs e) => RefreshThumbs();

    // ═══════════════════════════════════════════════════════════
    // Scroll Tracking & Thumb Updates
    // ═══════════════════════════════════════════════════════════

    private void OnInternalScrolled(object? sender, ScrolledEventArgs e)
    {
        RefreshThumbs();
        Scrolled?.Invoke(this, new SmartScrolledEventArgs(e.ScrollX, e.ScrollY));
    }

    private void RefreshThumbs()
    {
        UpdateVThumb();
        UpdateHThumb();
    }

    private void UpdateVThumb()
    {
        var contentH = _scrollView.ContentSize.Height;
        var viewportH = _scrollView.Height;

        if (contentH <= 0 || viewportH <= 0 || contentH <= viewportH)
        {
            _vThumb.IsVisible = false;
            return;
        }

        _vThumb.IsVisible = true;
        _vThumbHeight = Math.Max(ThumbMinHeight, (viewportH / contentH) * viewportH);
        _vThumb.HeightRequest = _vThumbHeight;

        var maxScroll = contentH - viewportH;
        var scrollRatio = maxScroll > 0 ? _scrollView.ScrollY / maxScroll : 0;
        _vThumb.TranslationY = scrollRatio * (viewportH - _vThumbHeight - 4);
    }

    private void UpdateHThumb()
    {
        var contentW = _scrollView.ContentSize.Width;
        var viewportW = _scrollView.Width;

        if (contentW <= 0 || viewportW <= 0 || contentW <= viewportW)
        {
            _hThumb.IsVisible = false;
            return;
        }

        _hThumb.IsVisible = true;
        _hThumbWidth = Math.Max(ThumbMinHeight, (viewportW / contentW) * viewportW);
        _hThumb.WidthRequest = _hThumbWidth;

        var maxScroll = contentW - viewportW;
        var scrollRatio = maxScroll > 0 ? _scrollView.ScrollX / maxScroll : 0;
        _hThumb.TranslationX = scrollRatio * (viewportW - _hThumbWidth - 4);
    }

    // ═══════════════════════════════════════════════════════════
    // Thumb Drag
    // ═══════════════════════════════════════════════════════════

    private void OnVThumbPan(object? sender, PanUpdatedEventArgs e)
    {
        var contentH = _scrollView.ContentSize.Height;
        var viewportH = _scrollView.Height;
        if (contentH <= viewportH) return;

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _vThumbDragStartTranslation = _vThumb.TranslationY;
                _vThumb.Opacity = 1.0;
                break;
            case GestureStatus.Running:
                var thumbTravel = Math.Max(1, viewportH - _vThumbHeight - 4);
                var newThumbY = Math.Clamp(_vThumbDragStartTranslation + e.TotalY, 0, thumbTravel);
                var targetY = (newThumbY / thumbTravel) * (contentH - viewportH);
                ScrollTo(null, targetY);
                break;
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                _vThumb.Opacity = 0.6;
                break;
        }
    }

    private void OnHThumbPan(object? sender, PanUpdatedEventArgs e)
    {
        var contentW = _scrollView.ContentSize.Width;
        var viewportW = _scrollView.Width;
        if (contentW <= viewportW) return;

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _hThumbDragStartTranslation = _hThumb.TranslationX;
                _hThumb.Opacity = 1.0;
                break;
            case GestureStatus.Running:
                var thumbTravel = Math.Max(1, viewportW - _hThumbWidth - 4);
                var newThumbX = Math.Clamp(_hThumbDragStartTranslation + e.TotalX, 0, thumbTravel);
                var targetX = (newThumbX / thumbTravel) * (contentW - viewportW);
                ScrollTo(targetX, null);
                break;
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                _hThumb.Opacity = 0.6;
                break;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Scroll Helper
    // ═══════════════════════════════════════════════════════════

    private void ScrollTo(double? x, double? y)
    {
#if WINDOWS
        try
        {
            if (_scrollView.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.ScrollViewer sv)
            {
                sv.ChangeView(x ?? sv.HorizontalOffset, y ?? sv.VerticalOffset, null, disableAnimation: true);
                return;
            }
        }
        catch { }
#endif
        _ = _scrollView.ScrollToAsync(x ?? _scrollView.ScrollX, y ?? _scrollView.ScrollY, false);
    }

    // ═══════════════════════════════════════════════════════════
    // Public Methods
    // ═══════════════════════════════════════════════════════════

    public void ScrollToTop() => _ = _scrollView.ScrollToAsync(0, 0, true);
    public void ScrollToBottom() => _ = _scrollView.ScrollToAsync(0, double.MaxValue, true);

    // ═══════════════════════════════════════════════════════════
    // Property Change Handlers
    // ═══════════════════════════════════════════════════════════

    private static void OnOrientationChanged(BindableObject b, object? o, object? n)
        => ((SmartScrollPanel)b)._scrollView.Orientation = (ScrollOrientation)n!;

    private static void OnScrollBarWidthChanged(BindableObject b, object? o, object? n)
    {
        var p = (SmartScrollPanel)b; var w = (double)n!;
        p._vThumb.WidthRequest = w; p._hThumb.HeightRequest = w;
    }

    private static void OnThumbColorChanged(BindableObject b, object? o, object? n)
    {
        var p = (SmartScrollPanel)b; var c = (Color)n!;
        p._vThumb.BackgroundColor = c; p._hThumb.BackgroundColor = c;
    }

    private static void OnThumbCornerRadiusChanged(BindableObject b, object? o, object? n)
    {
        var p = (SmartScrollPanel)b; var r = new CornerRadius((double)n!);
        p._vThumb.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = r };
        p._hThumb.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = r };
    }

    // ═══════════════════════════════════════════════════════════
    // WINDOWS — Low-Level Mouse Hook + DPI Handling
    // ═══════════════════════════════════════════════════════════

#if WINDOWS

    private static bool _hooked;
    private static IntPtr _hookHandle, _hwnd;
    private static readonly List<WeakReference<SmartScrollPanel>> _panels = new();

    // Thread-safe cached XamlRoot scale (volatile not allowed on double)
    private static long _scaleBits = BitConverter.DoubleToInt64Bits(1.0);
    private static double CachedScale
    {
        get => BitConverter.Int64BitsToDouble(System.Threading.Interlocked.Read(ref _scaleBits));
        set => System.Threading.Interlocked.Exchange(ref _scaleBits, BitConverter.DoubleToInt64Bits(value));
    }

    // ── DPI change detection ──

    private bool _xamlRootWired;

    private void WireXamlRootChanged()
    {
        if (_xamlRootWired) return;
        try
        {
            var win = Application.Current?.Windows.FirstOrDefault();
            if (win?.Handler?.PlatformView is not Microsoft.UI.Xaml.Window nw) { Retry(WireXamlRootChanged); return; }
            var xr = nw.Content?.XamlRoot;
            if (xr == null) { Retry(WireXamlRootChanged); return; }

            CachedScale = xr.RasterizationScale;
            xr.Changed += OnXamlRootChanged;
            _xamlRootWired = true;
            System.Diagnostics.Trace.WriteLine($"[SmartScrollPanel] XamlRoot wired — scale={CachedScale:F2}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"[SmartScrollPanel] XamlRoot wiring failed: {ex.Message}");
        }
    }

    private void OnXamlRootChanged(Microsoft.UI.Xaml.XamlRoot sender, Microsoft.UI.Xaml.XamlRootChangedEventArgs _)
    {
        var newScale = sender.RasterizationScale;
        var oldScale = CachedScale;
        CachedScale = newScale;

        if (Math.Abs(newScale - oldScale) < 0.01) return;
        System.Diagnostics.Trace.WriteLine($"[SmartScrollPanel] DPI change: {oldScale:F2} → {newScale:F2}");

        // Save scroll ratio, force re-layout, restore position
        var contentH = _scrollView.ContentSize.Height;
        var viewportH = _scrollView.Height;
        var ratio = contentH > viewportH ? _scrollView.ScrollY / (contentH - viewportH) : 0.0;
        ratio = Math.Clamp(ratio, 0, 1);

        ForceNativeRelayout();

        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(200), () =>
        {
            ForceNativeRelayout();
            var newContentH = _scrollView.ContentSize.Height;
            var newViewportH = _scrollView.Height;
            if (newContentH > newViewportH)
                ScrollTo(null, ratio * (newContentH - newViewportH));
            RefreshThumbs();
        });
    }

    private bool _inRelayout;

    private void ForceNativeRelayout()
    {
        if (_inRelayout) return;
        _inRelayout = true;
        try
        {
            if (_scrollView.Content is View content)
            {
                content.InvalidateMeasure();
                _scrollView.InvalidateMeasure();
            }
            try
            {
                if (_scrollView.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.ScrollViewer sv)
                {
                    sv.InvalidateMeasure();
                    sv.InvalidateArrange();
                    if (sv.Content is Microsoft.UI.Xaml.UIElement c) { c.InvalidateMeasure(); c.InvalidateArrange(); }
                    sv.UpdateLayout();
                }
            }
            catch { }
        }
        finally { _inRelayout = false; }
    }

    private void Retry(Action action)
        => Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(200), action);

    // ── Win32 imports ──

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(POINT point);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

    private delegate IntPtr HOOKPROC(int nCode, IntPtr wParam, IntPtr lParam);
    private static HOOKPROC? _hookDelegate;

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, HOOKPROC lpfn, IntPtr hMod, uint dwThreadId);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct POINT { public int X, Y; }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public POINT pt; public uint mouseData, flags, time; public IntPtr dwExtraInfo;
    }

    private const int WH_MOUSE_LL = 14;
    private const uint WM_MOUSEWHEEL = 0x020A, WM_MOUSEHWHEEL = 0x020E, GA_ROOT = 2;

    // ── Hook setup ──

    private void WireHook(int attempt)
    {
        lock (_panels)
        {
            _panels.RemoveAll(wr => !wr.TryGetTarget(out _));
            if (!_panels.Any(wr => wr.TryGetTarget(out var p) && p == this))
                _panels.Add(new WeakReference<SmartScrollPanel>(this));
        }

        if (_hooked) return;

        var win = Application.Current?.Windows.FirstOrDefault();
        if (win?.Handler?.PlatformView is Microsoft.UI.Xaml.Window nw)
            _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(nw);

        if (_hwnd == IntPtr.Zero)
        {
            if (attempt < 10)
                _ = Task.Delay(Math.Min(100 * (attempt + 1), 1000))
                    .ContinueWith(_ => Dispatcher.Dispatch(() => WireHook(attempt + 1)));
            return;
        }

        _hookDelegate = LowLevelMouseProc;
        _hookHandle = SetWindowsHookEx(WH_MOUSE_LL, _hookDelegate, IntPtr.Zero, 0);
        _hooked = _hookHandle != IntPtr.Zero;

        var dpi = GetDpiForWindow(_hwnd);
        System.Diagnostics.Trace.WriteLine(
            $"[SmartScrollPanel] Hook {(_hooked ? "✓" : "✗")} — HWND={_hwnd}, DPI={dpi}, XamlScale={CachedScale:F2}");
    }

    // ── DPI scale resolution ──

    private static double GetEffectiveScale()
    {
        var win32Scale = GetDpiForWindow(_hwnd) / 96.0;
        // If Win32 reports 96 DPI but XamlRoot knows better, manifest may be missing
        if (Math.Abs(win32Scale - 1.0) < 0.01 && CachedScale > 1.01)
        {
            System.Diagnostics.Trace.WriteLine(
                $"[SmartScrollPanel] WARNING: Win32 DPI=96 but XamlRoot={CachedScale:F2}. " +
                $"Is app.manifest with PerMonitorV2 missing?");
            return CachedScale;
        }
        return win32Scale;
    }

    // ── Hit-testing in physical screen coordinates ──

    private static bool HitTest(SmartScrollPanel panel, int screenX, int screenY, double scale)
    {
        try
        {
            var native = panel._root.Handler?.PlatformView as Microsoft.UI.Xaml.FrameworkElement
                      ?? panel._scrollView.Handler?.PlatformView as Microsoft.UI.Xaml.FrameworkElement;
            if (native == null || native.ActualWidth <= 0 || native.ActualHeight <= 0) return false;

            // TransformToVisual(null) → DIPs in window content
            var origin = native.TransformToVisual(null).TransformPoint(new Windows.Foundation.Point(0, 0));

            // DIP → physical client → physical screen
            var clientOrigin = new POINT { X = 0, Y = 0 };
            ClientToScreen(_hwnd, ref clientOrigin);

            var left = clientOrigin.X + origin.X * scale;
            var top = clientOrigin.Y + origin.Y * scale;
            var right = left + native.ActualWidth * scale;
            var bottom = top + native.ActualHeight * scale;

            return screenX >= left && screenX <= right && screenY >= top && screenY <= bottom;
        }
        catch { return false; }
    }

    // ── Hook callback ──

    private static IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (wParam == (IntPtr)WM_MOUSEWHEEL || wParam == (IntPtr)WM_MOUSEHWHEEL))
        {
            var mhs = System.Runtime.InteropServices.Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);

            // Only handle events over our window
            var hit = WindowFromPoint(mhs.pt);
            if ((hit != IntPtr.Zero ? GetAncestor(hit, GA_ROOT) : IntPtr.Zero) != _hwnd)
                return CallNextHookEx(_hookHandle, nCode, wParam, lParam);

            var isHorizontal = wParam == (IntPtr)WM_MOUSEHWHEEL;
            var delta = (short)(mhs.mouseData >> 16);
            var scale = GetEffectiveScale();

            SmartScrollPanel? target = null;
            double targetArea = double.MaxValue;
            lock (_panels)
            {
                foreach (var wr in _panels)
                {
                    if (!wr.TryGetTarget(out var p) || !p.IsVisible || p.Width <= 0) continue;

                    // Skip panels that can't scroll in the relevant direction
                    if (isHorizontal)
                    { if (p._scrollView.ContentSize.Width <= p._scrollView.Width + 1) continue; }
                    else
                    { if (p._scrollView.ContentSize.Height <= p._scrollView.Height + 1) continue; }

                    if (HitTest(p, mhs.pt.X, mhs.pt.Y, scale))
                    {
                        // Prefer the smallest (most specific) panel — handles nesting
                        var area = p.Width * p.Height;
                        if (area < targetArea) { target = p; targetArea = area; }
                    }
                }
            }

            if (target != null)
            {
                target.Dispatcher.Dispatch(() => HandleWheel(target, delta, isHorizontal));
                return (IntPtr)1; // swallow
            }
        }

        return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    private static void HandleWheel(SmartScrollPanel target, int delta, bool isHorizontal)
    {
        var px = delta * 0.8 * target.ScrollSensitivity;

        if (isHorizontal)
        {
            var maxX = Math.Max(0, target._scrollView.ContentSize.Width - target._scrollView.Width);
            if (maxX > 0) target.ScrollTo(Math.Clamp(target._scrollView.ScrollX - px, 0, maxX), null);
        }
        else
        {
            var maxY = Math.Max(0, target._scrollView.ContentSize.Height - target._scrollView.Height);
            if (maxY > 0) target.ScrollTo(null, Math.Clamp(target._scrollView.ScrollY - px, 0, maxY));
        }
    }

#endif
}
