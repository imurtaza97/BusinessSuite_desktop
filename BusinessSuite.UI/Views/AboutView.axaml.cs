using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace BusinessSuite.UI.Views;

public partial class AboutView : Window
{
    public AboutView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}
