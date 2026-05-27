using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BusinessSuite.UI.ViewModels;

namespace BusinessSuite.UI.Views;

public partial class BomFormWindow : Window
{
    public BomFormWindow()
    {
        InitializeComponent();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is BomFormViewModel vm)
            vm.RequestClose += ok => Close(ok);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
