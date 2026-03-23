using HopDev.Maui.Controls.Controls;

namespace HopDev.Maui.Controls.Sample.Pages;

public partial class SmartScrollTestPage : ContentPage
{
    public SmartScrollTestPage()
    {
        InitializeComponent();
        GenerateTestItems(SmartContent, 40);
        GenerateTestItems(NativeContent, 40);
    }

    private void GenerateTestItems(Layout container, int count)
    {
        for (int i = 1; i <= count; i++)
        {
            var card = new Border
            {
                Stroke = Color.FromArgb("#30394A"),
                StrokeThickness = 1,
                BackgroundColor = Color.FromArgb("#161B22"),
                Padding = new Thickness(12, 10),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 4 }
            };

            card.Content = new VerticalStackLayout
            {
                Children =
                {
                    new Label { Text = $"Item {i}", FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#E2E8F0") },
                    new Label { Text = $"Row {i} of {count} — compare scrolling behavior between panels", FontSize = 12, TextColor = Color.FromArgb("#94A3B8") }
                }
            };

            container.Children.Add(card);
        }
    }

    private void OnSmartScrolled(object? sender, SmartScrolledEventArgs e)
    {
        LblSmartPos.Text = $"SmartPanel Y: {e.ScrollY:F1}";
    }

    private void OnNativeScrolled(object? sender, ScrolledEventArgs e)
    {
        LblNativePos.Text = $"Native Y: {e.ScrollY:F1}";
    }
}
