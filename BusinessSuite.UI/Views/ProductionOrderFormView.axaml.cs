using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace BusinessSuite.UI.Views;

public partial class ProductionOrderFormView : UserControl
{
    public ProductionOrderFormView() => InitializeComponent();
    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
