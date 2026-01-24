using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BusinessSuite.DAL.Entities;
using BusinessSuite.UI.ViewModels;

namespace BusinessSuite.UI.Views;

public partial class ProductFormWindow : Window
{
    public ProductFormWindow()
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
        if (DataContext is ProductFormViewModel vm)
        {
            vm.RequestClose += (product) =>
            {
                Close(product);
            };
        }
    }
}
