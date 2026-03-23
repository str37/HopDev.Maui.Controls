namespace HopDev.Maui.Controls.Sample.Pages;

public partial class ScrollViewTestPage : ContentPage
{
    public ScrollViewTestPage()
    {
        InitializeComponent();
        GenerateTestItems();
    }

    private void GenerateTestItems()
    {
        for (int i = 1; i <= 50; i++)
        {
            var card = new Border
            {
                Stroke = Color.FromArgb("#30394A"),
                StrokeThickness = 1,
                BackgroundColor = Color.FromArgb("#1C2333"),
                Padding = new Thickness(16, 12),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 6 }
            };

            var stack = new HorizontalStackLayout { Spacing = 12 };
            stack.Children.Add(new Label
            {
                Text = $"#{i:D2}",
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#3D8BFD"),
                VerticalOptions = LayoutOptions.Center,
                WidthRequest = 40
            });
            stack.Children.Add(new VerticalStackLayout
            {
                Children =
                {
                    new Label { Text = $"Test Item {i}", FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#E2E8F0") },
                    new Label { Text = $"Scroll test row — verify smooth scrolling at current DPI/scale. Height varies intentionally.", FontSize = 13, TextColor = Color.FromArgb("#94A3B8") }
                }
            });

            card.Content = stack;
            ScrollContent.Children.Add(card);
        }
    }

    private void OnScrolled(object? sender, ScrolledEventArgs e)
    {
        LblScrollPos.Text = $"ScrollY: {e.ScrollY:F1}";
        LblContentSize.Text = $"Content: {TestScrollView.ContentSize.Height:F0}px";
        LblViewportSize.Text = $"Viewport: {TestScrollView.Height:F0}px";
    }
}
