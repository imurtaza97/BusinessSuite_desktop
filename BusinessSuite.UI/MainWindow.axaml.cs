using Avalonia.Controls;
using BusinessSuite.UI.ViewModels;

namespace BusinessSuite.UI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(BusinessSuite.DAL.Entities.User user)
    {
        InitializeComponent();
        DataContext = new DashboardViewModel(user);
    }
}