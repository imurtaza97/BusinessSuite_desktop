using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BusinessSuite.DAL.Entities;
using BusinessSuite.UI.ViewModels;

namespace BusinessSuite.UI.Views;

public partial class ProductsView : UserControl
{
    public ProductsView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void DataGrid_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is ProductsViewModel vm && sender is DataGrid grid)
        {
            vm.SelectedProducts.Clear();
            foreach (Product? product in grid.SelectedItems)
            {
                if (product != null)
                {
                    vm.SelectedProducts.Add(product);
                }
            }
        }
    }

    private void DataGrid_DoubleTapped(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ProductsViewModel vm && vm.EditProductCommand.CanExecute(null))
        {
            vm.EditProductCommand.Execute(null);
        }
    }
}
