using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BusinessSuite.UI.ViewModels;

namespace BusinessSuite.UI.Views;

public partial class CustomersView : UserControl
{
    public CustomersView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void DataGrid_DoubleTapped(object? sender, RoutedEventArgs e)
    {
        if (DataContext is CustomersViewModel vm && vm.EditCustomerCommand.CanExecute(null))
        {
            vm.EditCustomerCommand.Execute(null);
        }
    }
}
