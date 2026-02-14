using Avalonia.Controls;
using BusinessSuite.UI.ViewModels;

namespace BusinessSuite.UI.Views;

public partial class WarehouseFormWindow : Window
{
    public WarehouseFormWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(System.EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is WarehouseFormViewModel vm)
        {
            vm.RequestClose += result => Close(result);
        }
    }
}
