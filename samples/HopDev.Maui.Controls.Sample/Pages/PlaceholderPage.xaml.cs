namespace HopDev.Maui.Controls.Sample.Pages;

public partial class PlaceholderPage : ContentPage
{
    public PlaceholderPage(string title, string description)
    {
        InitializeComponent();
        TitleLabel.Text = title;
        DescriptionLabel.Text = description;
    }
}
