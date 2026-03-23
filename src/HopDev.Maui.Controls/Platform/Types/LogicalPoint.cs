namespace HopDev.Maui.Controls.Platform.Types;

/// <summary>
/// A point in MAUI logical coordinates (device-independent pixels).
/// Distinct from <see cref="PhysicalPoint"/> to prevent coordinate system confusion.
/// </summary>
public readonly record struct LogicalPoint(double X, double Y)
{
    /// <summary>Convert to MAUI's built-in Point type for interop with MAUI APIs.</summary>
    public Point ToMauiPoint() => new(X, Y);

    /// <summary>Create from MAUI's built-in Point type.</summary>
    public static LogicalPoint FromMaui(Point p) => new(p.X, p.Y);

    public override string ToString() => $"Logical({X:F1}, {Y:F1})";
}
