using Avalonia.Controls;
using BusinessSuite.UI.ViewModels;

namespace BusinessSuite.UI.Views;

public partial class InvoiceExportOptionsWindow : Window
{
    public InvoiceExportOptionsWindow()
    {
        InitializeComponent();
        if (DataContext is InvoiceExportOptionsViewModel vm)
        {
            vm.RequestClose += action => Close(action);
        }
    }

    protected override void OnOpened(System.EventArgs e)
    {
        base.OnOpened(e);
        if (DataContext is InvoiceExportOptionsViewModel vm)
        {
            vm.RequestClose += action => Close(action);
        }
    }
}
