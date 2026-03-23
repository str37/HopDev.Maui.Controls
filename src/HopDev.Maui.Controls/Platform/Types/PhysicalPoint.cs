namespace HopDev.Maui.Controls.Platform.Types;

/// <summary>
/// A point in physical screen coordinates (actual pixels on the display).
/// Distinct from <see cref="LogicalPoint"/> to prevent the category of bug
/// where physical and logical coordinates are accidentally mixed — the exact
/// root cause of the SmartScrollPanel 4K scaling issue.
/// </summary>
public readonly record struct PhysicalPoint(double X, double Y)
{
    public override string ToString() => $"Physical({X:F1}, {Y:F1})";
}
