using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace BusinessSuite.UI.Views;

public partial class DebitNotesView : UserControl
{
    public DebitNotesView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
