using Avalonia.Controls;
using BusinessSuite.UI.ViewModels;

namespace BusinessSuite.UI.Views;

public partial class StockAdjustmentWindow : Window
{
    public StockAdjustmentWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(System.EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is StockAdjustmentViewModel vm)
        {
            vm.RequestClose += result => Close(result);
        }
    }
}
