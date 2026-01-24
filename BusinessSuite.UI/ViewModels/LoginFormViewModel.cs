using System;
using System.Windows.Input;
using System.Threading.Tasks; 
using Avalonia.Threading;
using BusinessSuite.UI.ViewModels;
using BusinessSuite.DAL.Data;
using BusinessSuite.DAL.Repositories;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Controls.ApplicationLifetimes;

namespace BusinessSuite.UI.ViewModels;

public partial class LoginFormViewModel : ViewModelBase
{
    private readonly AppDbContext _dbFactory;
    private readonly UserRepository _userRepository;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private string _username = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private string _password = string.Empty;

    [ObservableProperty] private bool _isLoggingIn;
    [ObservableProperty] private string _errorMessage = string.Empty;

    public IAsyncRelayCommand LoginCommand { get; }

    public LoginFormViewModel(AppDbContext db)
    {
        _dbFactory = db;
        _userRepository = new UserRepository(db);
        LoginCommand = new AsyncRelayCommand(async () => await LoginAsync(), CanLogin);
    }

    private bool CanLogin()
    {
        return !IsLoggingIn && !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
    }

    private async Task LoginAsync()
    {
        IsLoggingIn = true;
        ErrorMessage = string.Empty;

        try
        {
            var user = await _userRepository.AuthenticateAsync(Username.ToLower().Trim(), Password);
            if (user != null)
            {
                Dispatcher.UIThread.Post(() => NavigateToMain(user));
            }
            else
            {
                ErrorMessage = "Invalid username or password.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"An error occurred: {ex.Message}";
        }
        finally
        {
            IsLoggingIn = false;
        }
    }

    private void NavigateToMain(BusinessSuite.DAL.Entities.User user)
    {
        var mainWindow = new MainWindow(user);
        mainWindow.Show();

        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var oldWindow = desktop.MainWindow;
            desktop.MainWindow = mainWindow;
            oldWindow?.Close();
        }
    }
}