#if WINDOWS
using System.Runtime.InteropServices;
using HopDev.Maui.Controls.Platform.Abstractions;
using HopDev.Maui.Controls.Platform.Types;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace HopDev.Maui.Controls.Platform.Windows;

/// <summary>
/// Windows implementation of <see cref="IPointerInterceptService"/>.
/// 
/// Installs a single WH_MOUSE_LL hook per window (instead of per-control) and dispatches
/// wheel events to registered scroll regions with DPI-corrected coordinates.
/// 
/// Based on the proven hook implementation from SmartScrollPanel. Key improvements:
/// - One hook per window, not per panel — avoids duplicate hooks
/// - Uses IWindowScaleService's empirical scale (never lies under DPI virtualization)
/// - PhysicalPoint/LogicalPoint types prevent coordinate system confusion at compile time
/// - Smallest-area targeting handles nested scroll regions correctly
/// 
/// The PerMonitorV2 app.manifest is REQUIRED for correct coordinate spaces.
/// Without it, WH_MOUSE_LL delivers physical coords but ClientToScreen returns
/// virtualized coords — the mismatch breaks all hit-testing on scaled displays.
/// </summary>
public class WindowsPointerInterceptService : IPointerInterceptService
{
    private IWindowScaleService? _scaleService;
    private Window? _mauiWindow;

    // ── Registered scroll regions ──
    private readonly Dictionary<View, Action<PointerWheelEventArgs>> _scrollRegions = new();

    // ── Hook state (static — one hook per process, routes to correct window) ──
    private static bool _hooked;
    private static nint _hookHandle;
    private static nint _hwnd;
    private static HOOKPROC? _hookDelegate; // prevent GC

    // Thread-safe reference to the active service instance
    private static WindowsPointerInterceptService? _activeInstance;

    public bool IsAttached { get; private set; }

    public event EventHandler<PointerWheelEventArgs>? WheelEvent;

    public void RegisterScrollRegion(View view, Action<PointerWheelEventArgs> handler)
    {
        _scrollRegions[view] = handler;
        System.Diagnostics.Debug.WriteLine(
            $"[PointerInterceptService] Registered scroll region: {view.GetType().Name} " +
            $"(total: {_scrollRegions.Count})");
    }

    public void UnregisterScrollRegion(View view)
    {
        _scrollRegions.Remove(view);
        System.Diagnostics.Debug.WriteLine(
            $"[PointerInterceptService] Unregistered scroll region (total: {_scrollRegions.Count})");
    }

    public void Attach(Window mauiWindow, IWindowScaleService scaleService)
    {
        _mauiWindow = mauiWindow;
        _scaleService = scaleService;
        _activeInstance = this;

        // Get the HWND from the scale service (already resolved)
        _hwnd = scaleService.WindowHandle;

        if (_hwnd == nint.Zero)
        {
            System.Diagnostics.Debug.WriteLine(
                "[PointerInterceptService] WARNING: HWND not available yet");
        }

        InstallHook();

        IsAttached = true;
        System.Diagnostics.Debug.WriteLine(
            $"[PointerInterceptService] Attached — hook={_hooked}, HWND=0x{_hwnd:X}");
    }

    public void Detach()
    {
        // Unhook if we're the active instance
        if (_activeInstance == this)
        {
            if (_hooked && _hookHandle != nint.Zero)
            {
                UnhookWindowsHookEx(_hookHandle);
                _hookHandle = nint.Zero;
                _hooked = false;
                _hookDelegate = null;
                System.Diagnostics.Debug.WriteLine("[PointerInterceptService] Hook removed");
            }
            _activeInstance = null;
        }

        _scrollRegions.Clear();
        _mauiWindow = null;
        _scaleService = null;
        IsAttached = false;
    }

    // ═══════════════════════════════════════════════════════════
    // Hook Installation
    // ═══════════════════════════════════════════════════════════

    private static void InstallHook()
    {
        if (_hooked || _hwnd == nint.Zero) return;

        _hookDelegate = LowLevelMouseProc;
        _hookHandle = SetWindowsHookEx(WH_MOUSE_LL, _hookDelegate, nint.Zero, 0);
        _hooked = _hookHandle != nint.Zero;

        System.Diagnostics.Debug.WriteLine(
            $"[PointerInterceptService] Hook install: {(_hooked ? "✓" : "✗")}");
    }

    // ═══════════════════════════════════════════════════════════
    // Hit-Testing — physical screen coords → view bounds
    //
    // Ported from SmartScrollPanel's proven HitTest method.
    // TransformToVisual(null) gives DIPs in window content area,
    // multiplied by scale and offset by ClientToScreen for physical
    // screen coordinates matching the WH_MOUSE_LL hook.
    // ═══════════════════════════════════════════════════════════

    private static bool HitTest(View view, int screenX, int screenY, double scale)
    {
        try
        {
            var native = view.Handler?.PlatformView as Microsoft.UI.Xaml.FrameworkElement;
            if (native == null || native.ActualWidth <= 0 || native.ActualHeight <= 0) return false;

            // TransformToVisual(null) → DIPs in window content area
            var origin = native.TransformToVisual(null)
                .TransformPoint(new global::Windows.Foundation.Point(0, 0));

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

    // ═══════════════════════════════════════════════════════════
    // Hook Callback
    //
    // Ported from SmartScrollPanel's LowLevelMouseProc. Key change:
    // uses _scaleService.ScaleFactor (empirical, never lies) instead
    // of GetDpiForWindow (lies under DPI virtualization).
    // ═══════════════════════════════════════════════════════════

    private static nint LowLevelMouseProc(int nCode, nint wParam, nint lParam)
    {
        if (nCode >= 0 && (wParam == WM_MOUSEWHEEL || wParam == WM_MOUSEHWHEEL))
        {
            var instance = _activeInstance;
            if (instance is null || instance._scrollRegions.Count == 0)
                return CallNextHookEx(_hookHandle, nCode, wParam, lParam);

            var mhs = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);

            // Only handle events over our window
            var hit = WindowFromPoint(mhs.pt);
            var root = hit != nint.Zero ? GetAncestor(hit, GA_ROOT) : nint.Zero;
            if (root != _hwnd)
                return CallNextHookEx(_hookHandle, nCode, wParam, lParam);

            var isHorizontal = wParam == WM_MOUSEHWHEEL;
            var delta = (short)(mhs.mouseData >> 16);

            // Use the empirical scale from IWindowScaleService — never lies
            var scale = instance._scaleService?.ScaleFactor ?? 1.0;

            // Build event args with both coordinate systems
            var physical = new PhysicalPoint(mhs.pt.X, mhs.pt.Y);
            var logical = instance._scaleService?.ToLogical(physical)
                ?? new LogicalPoint(mhs.pt.X, mhs.pt.Y);

            var args = new PointerWheelEventArgs
            {
                PhysicalPosition = physical,
                LogicalPosition = logical,
                Delta = delta,
                IsHorizontal = isHorizontal,
            };

            // Fire global event for controls that want raw access
            instance.WheelEvent?.Invoke(instance, args);
            if (args.Handled)
                return (nint)1;

            // Hit-test registered scroll regions — smallest (most specific) wins
            // This handles nested panels correctly (same logic as SmartScrollPanel's
            // original smallest-area targeting)
            View? targetView = null;
            Action<PointerWheelEventArgs>? targetHandler = null;
            double targetArea = double.MaxValue;

            foreach (var (view, handler) in instance._scrollRegions)
            {
                if (!view.IsVisible || view.Width <= 0) continue;

                if (HitTest(view, mhs.pt.X, mhs.pt.Y, scale))
                {
                    var area = view.Width * view.Height;
                    if (area < targetArea)
                    {
                        targetView = view;
                        targetHandler = handler;
                        targetArea = area;
                    }
                }
            }

            if (targetView is not null && targetHandler is not null)
            {
                var handler = targetHandler;
                var eventArgs = args;
                targetView.Dispatcher.Dispatch(() => handler(eventArgs));
                return (nint)1; // swallow — we handled it
            }
        }

        return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    // ═══════════════════════════════════════════════════════════
    // Win32 Interop — ported from SmartScrollPanel
    // ═══════════════════════════════════════════════════════════

    private const int WH_MOUSE_LL = 14;
    private const nint WM_MOUSEWHEEL = 0x020A;
    private const nint WM_MOUSEHWHEEL = 0x020E;
    private const uint GA_ROOT = 2;

    private delegate nint HOOKPROC(int nCode, nint wParam, nint lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWindowsHookEx(int idHook, HOOKPROC lpfn, nint hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(nint hhk);

    [DllImport("user32.dll")]
    private static extern nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ClientToScreen(nint hWnd, ref POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern nint WindowFromPoint(POINT point);

    [DllImport("user32.dll")]
    private static extern nint GetAncestor(nint hwnd, uint gaFlags);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X, Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData, flags, time;
        public nint dwExtraInfo;
    }
}
#endif
