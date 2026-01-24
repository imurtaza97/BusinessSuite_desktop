using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore; // Required for IDbContextFactory
using BusinessSuite.DAL.Data;
using BusinessSuite.DAL.Entities;
using BusinessSuite.BLL.Services;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using BusinessSuite.UI.Views;
using Avalonia.Controls.ApplicationLifetimes;

namespace BusinessSuite.UI.ViewModels;

public partial class ActivationFormViewModel : ViewModelBase
{
    // Inject the Factory and the Service
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ActivationService _activationService;

    [ObservableProperty] private string? _hardwareId;
    [ObservableProperty] private string? _licenseKey;
    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private bool _isBusy;

    public string ActivationButtonText => IsBusy ? "Activating..." : "Activate";

    public IAsyncRelayCommand ActivateCommand { get; }

    public ActivationFormViewModel(IDbContextFactory<AppDbContext> dbFactory, ActivationService activationService)
    {
        _dbFactory = dbFactory;
        _activationService = activationService;
        HardwareId = HardwareService.GetHardwareId();
        
        ActivateCommand = new AsyncRelayCommand(ActivateLicenseAsync);
    }

    private async Task ActivateLicenseAsync()
    {
        if (string.IsNullOrWhiteSpace(LicenseKey))
        {
            StatusMessage = "License key required.";
            return;
        }

        IsBusy = true;
        OnPropertyChanged(nameof(ActivationButtonText));
        StatusMessage = "Verifying license online...";

        try 
        {
            var result = await _activationService.ActivateLicenseAsync(LicenseKey);

            if (result == "Success")
            {
                StatusMessage = "Activation Successful!";
                NavigateToRegister();
            }
            else
            {
                StatusMessage = result;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = "Error: " + ex.Message;
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(ActivationButtonText));
        }
    }

    private void NavigateToRegister()
    {
        // 1. Create dependencies for the new form
        var registerService = new RegisterService(_dbFactory);
        var registerVm = new RegisterFormViewModel(registerService);

        // 2. Create and setup the form
        var registerForm = new RegisterForm
        {
            DataContext = registerVm
        };
        
        registerForm.Show();
        
        // 3. Update the main window reference
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var oldWindow = desktop.MainWindow;
            desktop.MainWindow = registerForm;
            oldWindow?.Close();
        }
    }
}