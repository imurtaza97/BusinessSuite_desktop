using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using BusinessSuite.UI.ViewModels;

namespace BusinessSuite.UI.Views;

public partial class InvoiceFormView : UserControl
{
    public InvoiceFormView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
    
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is InvoiceFormViewModel vm)
        {
            UpdateColumnVisibility(vm);
            // We should use WeakEventManager or proper cleanup, but for now simple subscription
            // Note: This might leak if View stays alive longer than VM or vice versa.
            // Ideally use ReactiveUI's WhenAnyValue if available, or PropertyChanged.
            vm.PropertyChanged += ViewModel_PropertyChanged;
        }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is InvoiceFormViewModel vm)
        {
            if (e.PropertyName == nameof(InvoiceFormViewModel.IsGstRegistered) || 
                e.PropertyName == nameof(InvoiceFormViewModel.IsItemLevelDiscount))
            {
                UpdateColumnVisibility(vm);
            }
        }
    }

    private void UpdateColumnVisibility(InvoiceFormViewModel vm)
    {
        var grid = this.FindControl<DataGrid>("InvoiceGrid");
        if (grid != null)
        {
            foreach (var column in grid.Columns)
            {
                if (column.Header as string == "Tax %")
                {
                    // column.IsVisible = vm.IsGstRegistered;
                    column.IsVisible = true;
                }
                else if (column.Header as string == "Tax Amt")
                {
                    // column.IsVisible = vm.IsGstRegistered;
                    column.IsVisible = true;
                }
                else if (column.Header as string == "Discount")
                {
                    column.IsVisible = vm.IsItemLevelDiscount;
                }
            }
        }
    }
}
