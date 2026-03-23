using HopDev.Maui.Controls.Extensions;

namespace HopDev.Maui.Controls.Sample;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseHopDevControls()
            .UseBorderlessWindow()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("DMSans-Regular.ttf", "DM Sans");
                fonts.AddFont("DMSans-Bold.ttf", "DM Sans Bold");
                fonts.AddFont("JetBrainsMono-Regular.ttf", "JetBrainsMono");
                fonts.AddFont("JetBrainsMono-Bold.ttf", "JetBrainsMono-Bold");
                fonts.AddFont("FluentSystemIcons-Filled.ttf", "FluentFilled");
                fonts.AddFont("FluentSystemIcons-Regular.ttf", "FluentRegular");
            });

        return builder.Build();
    }

#if WINDOWS
    /// <summary>
    /// Update DWM dark mode for theme changes. Call when toggling light/dark.
    /// Delegates to the library method.
    /// </summary>
    public static void UpdateBorderColor(bool dark) =>
        BorderlessWindowExtensions.UpdateBorderColor(dark);
#endif
}
