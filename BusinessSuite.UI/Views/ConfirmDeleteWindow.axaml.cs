using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace BusinessSuite.UI.Views;

public partial class ConfirmDeleteWindow : Window
{
    private TextBlock? _messageTextBlock;

    public string Message
    {
        get => _messageTextBlock?.Text ?? "Are you sure?";
        set
        {
            if (_messageTextBlock != null)
                _messageTextBlock.Text = value;
        }
    }

    public ConfirmDeleteWindow()
    {
        InitializeComponent();
        _messageTextBlock = this.FindControl<TextBlock>("MessageTextBlock");
    }

    public ConfirmDeleteWindow(string message) : this()
    {
        Message = message;
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
