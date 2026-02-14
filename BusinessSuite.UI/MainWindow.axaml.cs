using Avalonia.Controls;
using BusinessSuite.UI.ViewModels;
using BusinessSuite.UI.Views;

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
        var dbFactory = new BusinessSuite.DAL.Data.AppDbContextFactory();
        var vm = new DashboardViewModel(user, dbFactory);
        DataContext = vm;
        vm.RequestLogout += OnLogout;
    }

    private void OnLogout()
    {
        var db = new BusinessSuite.DAL.Data.AppDbContext();
        var loginForm = new LoginForm 
        { 
            DataContext = new LoginFormViewModel(db) 
        };
        loginForm.Show();
        Close();
    }
}