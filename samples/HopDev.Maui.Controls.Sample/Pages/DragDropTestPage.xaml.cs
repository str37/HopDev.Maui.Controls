namespace HopDev.Maui.Controls.Sample.Pages;

public partial class DragDropTestPage : ContentPage
{
    private readonly List<string> _items = new();
    private string? _draggedItem;

    public DragDropTestPage()
    {
        InitializeComponent();

        for (int i = 1; i <= 20; i++)
            _items.Add($"Task {i}: Sample draggable item");

        RebuildList();
    }

    private void RebuildList()
    {
        DragContainer.Children.Clear();

        for (int i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            var index = i;

            var card = new Border
            {
                Stroke = Color.FromArgb("#30394A"),
                StrokeThickness = 1,
                BackgroundColor = Color.FromArgb("#1C2333"),
                Padding = new Thickness(16, 12),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 6 }
            };

            var stack = new HorizontalStackLayout { Spacing = 12 };

            // Drag handle
            var handle = new Label
            {
                Text = "\u2630",
                FontSize = 20,
                TextColor = Color.FromArgb("#64748B"),
                VerticalOptions = LayoutOptions.Center
            };

            // Index badge
            var badge = new Label
            {
                Text = $"{i + 1}",
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#3D8BFD"),
                VerticalOptions = LayoutOptions.Center,
                WidthRequest = 30
            };

            // Item text
            var text = new Label
            {
                Text = item,
                FontSize = 15,
                TextColor = Color.FromArgb("#E2E8F0"),
                VerticalOptions = LayoutOptions.Center
            };

            stack.Children.Add(handle);
            stack.Children.Add(badge);
            stack.Children.Add(text);
            card.Content = stack;

            // Drag gesture
            var drag = new DragGestureRecognizer();
            drag.DragStarting += (s, e) =>
            {
                _draggedItem = item;
                e.Data.Text = item;
                card.Opacity = 0.5;
                LblDragStatus.Text = $"Dragging: {item}";
                LblDragStatus.TextColor = Color.FromArgb("#FCD34D");
            };
            drag.DropCompleted += (s, e) =>
            {
                card.Opacity = 1.0;
            };
            card.GestureRecognizers.Add(drag);

            // Drop gesture
            var drop = new DropGestureRecognizer { AllowDrop = true };
            drop.DragOver += (s, e) =>
            {
                e.AcceptedOperation = DataPackageOperation.Copy;
                card.BackgroundColor = Color.FromArgb("#252D3D");
            };
            drop.DragLeave += (s, e) =>
            {
                card.BackgroundColor = Color.FromArgb("#1C2333");
            };
            drop.Drop += (s, e) =>
            {
                card.BackgroundColor = Color.FromArgb("#1C2333");
                if (_draggedItem != null && _draggedItem != item)
                {
                    var fromIdx = _items.IndexOf(_draggedItem);
                    var toIdx = _items.IndexOf(item);
                    if (fromIdx >= 0 && toIdx >= 0)
                    {
                        _items.RemoveAt(fromIdx);
                        _items.Insert(toIdx, _draggedItem);
                        RebuildList();
                        LblDragStatus.Text = $"Moved \"{_draggedItem}\" from #{fromIdx + 1} to #{toIdx + 1}";
                        LblDragStatus.TextColor = Color.FromArgb("#6EE7B7");
                    }
                }
                _draggedItem = null;
            };
            card.GestureRecognizers.Add(drop);

            DragContainer.Children.Add(card);
        }
    }
}
