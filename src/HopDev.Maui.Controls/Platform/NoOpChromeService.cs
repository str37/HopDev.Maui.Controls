using HopDev.Maui.Controls.Platform.Abstractions;

namespace HopDev.Maui.Controls.Platform;

/// <summary>
/// No-op implementation for platforms without window chrome customization (iOS, Android).
/// All operations are safe to call but have no effect. CaptionButtonInsets is zero.
/// On these platforms, TitleBar renders as a simple header bar with no OS chrome interaction.
/// </summary>
public class NoOpChromeService : IWindowChromeService
{
    public bool IsContentExtendedIntoTitleBar => false;
    public Thickness CaptionButtonInsets => Thickness.Zero;
    public object? NativeAppWindow => null;
    public bool IsAttached { get; private set; }

#pragma warning disable CS0067 // Interface contract — raised on Windows when DPI changes
    public event EventHandler<CaptionInsetsChangedEventArgs>? CaptionInsetsChanged;
#pragma warning restore CS0067

    public void ExtendContentIntoTitleBar(bool extend) { }
    public void SetDragRegion(View view) { }
    public void RegisterInteractiveRegion(View view) { }
    public void UnregisterInteractiveRegion(View view) { }
    public void SetButtonColors(Color? foreground, Color? hoverBackground, Color? pressedBackground) { }

    public void Attach(Window mauiWindow, IWindowScaleService scaleService) => IsAttached = true;
    public void Detach() => IsAttached = false;
}
