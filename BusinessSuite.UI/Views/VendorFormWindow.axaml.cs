using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BusinessSuite.UI.ViewModels;

namespace BusinessSuite.UI.Views;

public partial class VendorFormWindow : Window
{
    public VendorFormWindow()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is VendorFormViewModel vm)
        {
            vm.RequestClose += result => Close(result);
        }
    }
}
