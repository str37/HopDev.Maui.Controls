namespace HopDev.Maui.Controls.Sample;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // AppLayout is the root — no Shell. TitleBar replaces Windows chrome.
        // Title must be empty — any text here renders in the native title strip.
        return new Window(new AppLayout())
        {
            Title = "",
            Width = 1280,
            Height = 900
        };
    }
}
