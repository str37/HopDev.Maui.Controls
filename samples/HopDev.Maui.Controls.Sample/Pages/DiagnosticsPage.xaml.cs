using HopDev.Maui.Controls.Platform;
using HopDev.Maui.Controls.Platform.Abstractions;
using HopDev.Maui.Controls.Platform.Types;

namespace HopDev.Maui.Controls.Sample.Pages;

public partial class DiagnosticsPage : ContentPage
{
    private IWindowScaleService? _scaleService;
    private IWindowChromeService? _chromeService;
    private readonly List<string> _dpiLog = new();

    public DiagnosticsPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Resolve per-Window platform services
        if (Window is not null)
        {
            _scaleService = HopDevServices.GetScaleService(Window);
            _chromeService = HopDevServices.GetChromeService(Window);

            if (_scaleService is not null)
            {
                _scaleService.ScaleChanged += OnScaleChanged;
            }
        }

        UpdateDisplayInfo();
        UpdateServiceInfo();

        if (Window != null)
            Window.SizeChanged += OnWindowSizeChanged;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (_scaleService is not null)
            _scaleService.ScaleChanged -= OnScaleChanged;

        if (Window != null)
            Window.SizeChanged -= OnWindowSizeChanged;
    }

    // ═══════════════════════════════════════════════════════════
    // Platform Service Info
    // ═══════════════════════════════════════════════════════════

    private void UpdateServiceInfo()
    {
        // IWindowScaleService
        if (_scaleService is not null)
        {
            var empirical = _scaleService.ScaleFactor;
            LblEmpiricalScale.Text = $"Empirical Scale: {empirical:F4} ({empirical * 100:F1}%)";
            LblWindowHandle.Text = $"HWND: 0x{_scaleService.WindowHandle:X8}";

#if WINDOWS
            UpdateWin32Comparison(empirical);
#else
            LblWin32Dpi.Text = "Win32 DPI: N/A (non-Windows)";
            LblScaleMatch.Text = "";
#endif
        }
        else
        {
            LblEmpiricalScale.Text = "Empirical Scale: (service not attached)";
            LblWin32Dpi.Text = "";
            LblScaleMatch.Text = "";
            LblWindowHandle.Text = "";
        }

        // IWindowChromeService
        if (_chromeService is not null)
        {
            LblChromeAttached.Text = $"Attached: ✅ Yes";
            LblExtended.Text = $"ExtendedIntoTitleBar: {_chromeService.IsContentExtendedIntoTitleBar}";
            var insets = _chromeService.CaptionButtonInsets;
            LblCaptionInsets.Text = $"Caption Insets: Left={insets.Left:F1}  Right={insets.Right:F1}";
        }
        else
        {
            LblChromeAttached.Text = "Attached: ❌ No";
            LblExtended.Text = "";
            LblCaptionInsets.Text = "";
        }
    }

#if WINDOWS
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    private void UpdateWin32Comparison(double empirical)
    {
        try
        {
            var hwnd = _scaleService!.WindowHandle;
            if (hwnd != nint.Zero)
            {
                var win32Dpi = GetDpiForWindow(hwnd);
                var win32Scale = win32Dpi / 96.0;

                LblWin32Dpi.Text = $"Win32 GetDpiForWindow: {win32Dpi} DPI → {win32Scale:F2} scale";

                var delta = Math.Abs(empirical - win32Scale);
                if (delta < 0.01)
                {
                    LblScaleMatch.Text = "✅ Empirical matches Win32 — DPI manifest correct";
                    LblScaleMatch.TextColor = Color.FromArgb("#6EE7B7");
                }
                else if (Math.Abs(win32Scale - 1.0) < 0.01 && empirical > 1.01)
                {
                    LblScaleMatch.Text = $"⚠️ Win32 says 96 DPI but empirical is {empirical:F2} — " +
                                         "PerMonitorV2 manifest may be missing!";
                    LblScaleMatch.TextColor = Color.FromArgb("#FCA5A5");
                }
                else
                {
                    LblScaleMatch.Text = $"⚠️ Mismatch: empirical={empirical:F3} vs win32={win32Scale:F3} " +
                                         $"(delta={delta:F3})";
                    LblScaleMatch.TextColor = Color.FromArgb("#FCD34D");
                }
            }
        }
        catch (Exception ex)
        {
            LblWin32Dpi.Text = $"Win32 DPI query failed: {ex.Message}";
        }
    }
#endif

    // ═══════════════════════════════════════════════════════════
    // Display Info
    // ═══════════════════════════════════════════════════════════

    private void UpdateDisplayInfo()
    {
        var display = DeviceDisplay.Current.MainDisplayInfo;

        LblScreenResolution.Text = $"Screen: {display.Width:F0} × {display.Height:F0} px (physical)";
        LblDensity.Text = $"Display Density: {display.Density:F2} ({display.Density * 96:F0} DPI)";
        LblIs4K.Text = $"4K+: {(display.Width >= 3840 ? "✅ YES" : "❌ No")} ({display.Width:F0}px wide)";

        UpdateLiveMetrics();
    }

    private void OnWindowSizeChanged(object? sender, EventArgs e)
    {
        UpdateLiveMetrics();

        // Re-read scale — it may change during resize if window moves between monitors
        if (_scaleService is not null)
            LblEmpiricalScale.Text = $"Empirical Scale: {_scaleService.ScaleFactor:F4} " +
                                     $"({_scaleService.ScaleFactor * 100:F1}%)";
    }

    private void UpdateLiveMetrics()
    {
        var w = Window?.Width ?? 0;
        var h = Window?.Height ?? 0;

        LblLiveWidth.Text = $"Window Width: {w:F0} px (MAUI logical)";
        LblLiveHeight.Text = $"Window Height: {h:F0} px (MAUI logical)";
        LblWindowSize.Text = $"Window: {w:F0} × {h:F0} (logical)";

        var breakpoint = w switch
        {
            >= 1600 => "Extra Wide (≥1600)",
            >= 1200 => "Wide (≥1200)",
            _ => "Standard (<1200)"
        };
        LblLiveBreakpoint.Text = $"Layout Breakpoint: {breakpoint}";
    }

    // ═══════════════════════════════════════════════════════════
    // DPI Change Events
    // ═══════════════════════════════════════════════════════════

    private void OnScaleChanged(object? sender, ScaleChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var entry = $"[{DateTime.Now:HH:mm:ss}] {e.OldScale:F2} → {e.NewScale:F2} " +
                        $"({e.NewScale * 100:F0}%)";
            _dpiLog.Insert(0, entry);
            if (_dpiLog.Count > 10) _dpiLog.RemoveAt(10);

            LblDpiLog.Text = string.Join("\n", _dpiLog);

            // Refresh all metrics
            UpdateServiceInfo();
        });
    }

    // ═══════════════════════════════════════════════════════════
    // Coordinate Conversion Test
    // ═══════════════════════════════════════════════════════════

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        var pos = e.GetPosition(sender as VisualElement);
        if (!pos.HasValue) return;

        var logicalPos = $"Logical: ({pos.Value.X:F1}, {pos.Value.Y:F1})";
        LblPointerLogical.Text = logicalPos;

        if (_scaleService is not null)
        {
            // Demonstrate the PhysicalPoint ↔ LogicalPoint round-trip
            var logical = new LogicalPoint(pos.Value.X, pos.Value.Y);
            var physical = _scaleService.ToPhysical(logical);
            var backToLogical = _scaleService.ToLogical(physical);

            LblPointerRoundTrip.Text =
                $"→ Physical({physical.X:F1}, {physical.Y:F1}) " +
                $"→ Logical({backToLogical.X:F1}, {backToLogical.Y:F1})  " +
                $"[scale={_scaleService.ScaleFactor:F2}]";
        }
        else
        {
            LblPointerRoundTrip.Text = "(scale service unavailable)";
        }
    }
}
