using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BusinessSuite.UI.ViewModels;

namespace BusinessSuite.UI.Views;

public partial class LoginForm : Window
{
    public LoginForm()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void LoginButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is LoginFormViewModel vm)
        {
            vm.LoginCommand.Execute(null);
        }
    }
}
