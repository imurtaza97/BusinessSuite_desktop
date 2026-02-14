using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BusinessSuite.UI.ViewModels;
using System;

namespace BusinessSuite.UI.Views;

public partial class PaymentFormWindow : Window
{
    public PaymentFormWindow()
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
        if (DataContext is PaymentFormViewModel vm)
        {
            vm.RequestClose += (result) => Close(result);
        }
    }
}
