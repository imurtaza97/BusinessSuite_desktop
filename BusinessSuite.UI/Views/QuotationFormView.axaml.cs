using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using BusinessSuite.UI.ViewModels;

namespace BusinessSuite.UI.Views;

public partial class QuotationFormView : UserControl
{
    public QuotationFormView()
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
        if (DataContext is QuotationFormViewModel vm)
        {
            UpdateColumnVisibility(vm);
            vm.PropertyChanged += ViewModel_PropertyChanged;
        }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is QuotationFormViewModel vm)
        {
            if (e.PropertyName == nameof(QuotationFormViewModel.IsGstRegistered) || 
                e.PropertyName == nameof(QuotationFormViewModel.IsItemLevelDiscount))
            {
                UpdateColumnVisibility(vm);
            }
        }
    }

    private void UpdateColumnVisibility(QuotationFormViewModel vm)
    {
        var grid = this.FindControl<DataGrid>("QuotationGrid");
        if (grid != null)
        {
            foreach (var column in grid.Columns)
            {
                if (column.Header as string == "Tax %")
                {
                    column.IsVisible = true;
                }
                else if (column.Header as string == "Tax Amt")
                {
                    column.IsVisible = true;
                }
                else if (column.Header as string == "Discount (₹)")
                {
                    column.IsVisible = vm.IsItemLevelDiscount;
                }
            }
        }
    }
}
