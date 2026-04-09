using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BusinessSuite.DAL.Entities;
using BusinessSuite.UI.ViewModels;

namespace BusinessSuite.UI.Views;

public partial class WarehousesView : UserControl
{
    public WarehousesView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void DataGrid_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is WarehousesViewModel vm && sender is DataGrid grid)
        {
            vm.SelectedWarehouses.Clear();
            foreach (Warehouse? warehouse in grid.SelectedItems)
            {
                if (warehouse != null)
                {
                    vm.SelectedWarehouses.Add(warehouse);
                }
            }
        }
    }
}
