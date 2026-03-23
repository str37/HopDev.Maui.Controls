namespace HopDev.Maui.Controls.Platform.Abstractions;

/// <summary>
/// Event data for DPI/scale factor changes, typically when a window
/// moves between monitors with different display scaling.
/// </summary>
public class ScaleChangedEventArgs : EventArgs
{
    public double OldScale { get; init; }
    public double NewScale { get; init; }

    public ScaleChangedEventArgs(double oldScale, double newScale)
    {
        OldScale = oldScale;
        NewScale = newScale;
    }
}
