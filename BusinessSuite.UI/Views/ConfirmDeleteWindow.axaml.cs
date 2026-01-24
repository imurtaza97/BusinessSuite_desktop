using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace BusinessSuite.UI.Views;

public partial class ConfirmDeleteWindow : Window
{
    public ConfirmDeleteWindow()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public void Delete_Click(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    public void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
