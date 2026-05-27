using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace BusinessSuite.UI.Views;

public partial class ProductionOrdersView : UserControl
{
    public ProductionOrdersView() => InitializeComponent();
    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
