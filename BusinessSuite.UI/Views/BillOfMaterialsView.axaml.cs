using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace BusinessSuite.UI.Views;

public partial class BillOfMaterialsView : UserControl
{
    public BillOfMaterialsView() => InitializeComponent();
    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
