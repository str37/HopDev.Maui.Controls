using HopDev.Maui.Controls.Platform.Types;

namespace HopDev.Maui.Controls.Platform.Abstractions;

/// <summary>
/// Pointer wheel event data with DPI-corrected coordinates.
/// Both physical and logical positions are provided so consumers
/// never need to do their own coordinate conversion.
/// </summary>
public class PointerWheelEventArgs : EventArgs
{
    /// <summary>Cursor position in physical screen pixels (from WH_MOUSE_LL).</summary>
    public PhysicalPoint PhysicalPosition { get; init; }

    /// <summary>Cursor position in MAUI logical coordinates (DPI-corrected).</summary>
    public LogicalPoint LogicalPosition { get; init; }

    /// <summary>Wheel delta. Positive = scroll up, negative = scroll down.
    /// Normalized to multiples of 120 (standard Windows wheel tick).</summary>
    public int Delta { get; init; }

    /// <summary>True if this is a horizontal wheel event.</summary>
    public bool IsHorizontal { get; init; }

    /// <summary>Set to true to prevent further dispatch to other regions.</summary>
    public bool Handled { get; set; }
}
