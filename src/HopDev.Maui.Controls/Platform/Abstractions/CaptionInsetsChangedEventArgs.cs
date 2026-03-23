namespace HopDev.Maui.Controls.Platform.Abstractions;

/// <summary>
/// Event data for caption button metric changes. Occurs when DPI changes
/// or when window state changes (maximized windows may have different insets).
/// </summary>
public class CaptionInsetsChangedEventArgs : EventArgs
{
    /// <summary>Caption button insets in logical pixels.</summary>
    public Thickness Insets { get; init; }

    public CaptionInsetsChangedEventArgs(Thickness insets)
    {
        Insets = insets;
    }
}
