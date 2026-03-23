namespace HopDev.Maui.Controls.Controls;

/// <summary>
/// Event args for SmartScrollPanel scroll events.
/// Placeholder — will be populated when SmartScrollPanel migrates to this project (Phase 2).
/// </summary>
public class SmartScrolledEventArgs : EventArgs
{
    public double ScrollX { get; }
    public double ScrollY { get; }

    public SmartScrolledEventArgs(double scrollX, double scrollY)
    {
        ScrollX = scrollX;
        ScrollY = scrollY;
    }
}
