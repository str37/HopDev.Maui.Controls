using HopDev.Maui.Controls.Platform.Abstractions;

namespace HopDev.Maui.Controls.Platform;

/// <summary>
/// No-op implementation for platforms without WH_MOUSE_LL hook needs.
/// On mobile and Mac, native scroll handling works correctly without interception.
/// </summary>
public class NoOpPointerInterceptService : IPointerInterceptService
{
    public bool IsAttached { get; private set; }

#pragma warning disable CS0067 // Interface contract — raised when hook is active (Windows only)
    public event EventHandler<PointerWheelEventArgs>? WheelEvent;
#pragma warning restore CS0067

    public void RegisterScrollRegion(View view, Action<PointerWheelEventArgs> handler) { }
    public void UnregisterScrollRegion(View view) { }

    public void Attach(Window mauiWindow, IWindowScaleService scaleService) => IsAttached = true;
    public void Detach() => IsAttached = false;
}
