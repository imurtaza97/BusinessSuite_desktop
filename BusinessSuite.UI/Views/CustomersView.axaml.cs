using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BusinessSuite.DAL.Entities;
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

    private void DataGrid_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is CustomersViewModel vm && sender is DataGrid grid)
        {
            vm.SelectedCustomers.Clear();
            foreach (Customer? customer in grid.SelectedItems)
            {
                if (customer != null)
                {
                    vm.SelectedCustomers.Add(customer);
                }
            }
        }
    }

    private void DataGrid_DoubleTapped(object? sender, RoutedEventArgs e)
    {
        if (DataContext is CustomersViewModel vm && vm.EditCustomerCommand.CanExecute(null))
        {
            vm.EditCustomerCommand.Execute(null);
        }
    }
}
