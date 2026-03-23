#if WINDOWS
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml.Input;
#endif

namespace HopDev.Maui.Controls.Controls;

/// <summary>
/// A bottom-right corner resize grip for HD/4K screens.
/// On pointer down, immediately hands off to Windows native resize —
/// feels identical to grabbing the actual window corner.
/// </summary>
public class ResizeGrip : ContentView
{
    private const int GripSize = 20;

#if WINDOWS
    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);

    [DllImport("user32.dll")]
    private static extern IntPtr SetCursor(IntPtr hCursor);

    private const uint WM_NCLBUTTONDOWN = 0x00A1;
    private const int HTBOTTOMRIGHT = 17;
    private const int IDC_SIZENWSE = 32642;
#endif

    public ResizeGrip()
    {
        WidthRequest = GripSize;
        HeightRequest = GripSize;
        HorizontalOptions = LayoutOptions.End;
        VerticalOptions = LayoutOptions.End;
        Margin = new Thickness(0, 0, 2, 2);

        Content = BuildGripVisual();

        // Cursor change on hover (MAUI level — works cross-platform)
        var pointerEnter = new PointerGestureRecognizer();
        pointerEnter.PointerEntered += (_, _) =>
        {
#if WINDOWS
            SetCursor(LoadCursor(IntPtr.Zero, IDC_SIZENWSE));
#endif
        };
        GestureRecognizers.Add(pointerEnter);

        // Hook native PointerPressed after handler is attached
        HandlerChanged += OnHandlerChanged;
    }

    private void OnHandlerChanged(object? sender, EventArgs e)
    {
#if WINDOWS
        // Get the native WinUI element and hook PointerPressed directly.
        // This fires BEFORE MAUI's gesture system captures the pointer,
        // so our ReleaseCapture + SendMessage takes over instantly.
        if (Handler?.PlatformView is Microsoft.UI.Xaml.UIElement nativeView)
        {
            nativeView.PointerPressed += OnNativePointerPressed;
            nativeView.PointerEntered += (_, _) =>
                SetCursor(LoadCursor(IntPtr.Zero, IDC_SIZENWSE));
        }
#endif
    }

#if WINDOWS
    private void OnNativePointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (e.Pointer.PointerDeviceType is not
            (Microsoft.UI.Input.PointerDeviceType.Mouse or
             Microsoft.UI.Input.PointerDeviceType.Pen))
            return;

        var window = Application.Current?.Windows.FirstOrDefault();
        var nativeWindow = window?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
        if (nativeWindow is null) return;

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow);

        // Release MAUI/WinUI mouse capture and hand off to Windows.
        // From this point Windows handles the entire resize operation —
        // cursor, rubber-band, snapping — identical to grabbing the corner.
        ReleaseCapture();
        SendMessage(hwnd, WM_NCLBUTTONDOWN, (IntPtr)HTBOTTOMRIGHT, IntPtr.Zero);

        e.Handled = true;
    }
#endif

    private static View BuildGripVisual()
    {
        var canvas = new AbsoluteLayout
        {
            WidthRequest = GripSize,
            HeightRequest = GripSize
        };

        var color = Color.FromArgb("#64748B");
        var dotSize = 2.5;

        var positions = new (double x, double y)[]
        {
            (14, 6),
            (10, 10), (14, 10),
            (6, 14), (10, 14), (14, 14),
        };

        foreach (var (x, y) in positions)
        {
            var dot = new BoxView
            {
                Color = color,
                WidthRequest = dotSize,
                HeightRequest = dotSize,
                CornerRadius = dotSize / 2
            };
            AbsoluteLayout.SetLayoutBounds(dot, new Rect(x, y, dotSize, dotSize));
            canvas.Children.Add(dot);
        }

        return canvas;
    }
}
