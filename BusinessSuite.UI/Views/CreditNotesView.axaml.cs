using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace BusinessSuite.UI.Views;

public partial class CreditNotesView : UserControl
{
    public CreditNotesView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
