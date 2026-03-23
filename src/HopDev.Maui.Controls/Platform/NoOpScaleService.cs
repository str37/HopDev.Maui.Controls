using HopDev.Maui.Controls.Platform.Types;

namespace HopDev.Maui.Controls.Platform;

/// <summary>
/// No-op implementation for platforms without native DPI interop needs (iOS, Android).
/// Returns scale factor 1.0 and passes coordinates through unchanged.
/// </summary>
public class NoOpScaleService : Abstractions.IWindowScaleService
{
    public double ScaleFactor => 1.0;
    public nint WindowHandle => nint.Zero;
    public bool IsAttached { get; private set; }

#pragma warning disable CS0067 // Interface contract — raised on Windows when DPI changes
    public event EventHandler<Abstractions.ScaleChangedEventArgs>? ScaleChanged;
#pragma warning restore CS0067

    public LogicalPoint ToLogical(PhysicalPoint physical) => new(physical.X, physical.Y);
    public PhysicalPoint ToPhysical(LogicalPoint logical) => new(logical.X, logical.Y);

    public void Attach(Window mauiWindow) => IsAttached = true;
    public void Detach() => IsAttached = false;
}
