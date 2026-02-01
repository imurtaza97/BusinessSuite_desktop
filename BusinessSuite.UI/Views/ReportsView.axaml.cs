using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BusinessSuite.UI.ViewModels;

namespace BusinessSuite.UI.Views;

public partial class ReportsView : UserControl
{
    public ReportsView()
    {
        InitializeComponent();
    }
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

