using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BusinessSuite.UI.ViewModels;

namespace BusinessSuite.UI.Views;

public partial class VendorsView : UserControl
{
    public VendorsView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void DataGrid_DoubleTapped(object? sender, RoutedEventArgs e)
    {
        if (DataContext is VendorsViewModel vm && vm.EditVendorCommand.CanExecute(null))
        {
            vm.EditVendorCommand.Execute(null);
        }
    }
}
