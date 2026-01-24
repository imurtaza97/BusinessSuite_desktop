using Avalonia.Controls;

namespace BusinessSuite.UI.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private void Uom_KeyUp(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key == Avalonia.Input.Key.Enter)
        {
            if (DataContext is ViewModels.SettingsViewModel vm)
            {
                vm.AddUomCommand.Execute(null);
            }
        }
    }
}
