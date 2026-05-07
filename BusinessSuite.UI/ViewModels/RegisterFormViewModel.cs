using System;
using System.Threading.Tasks;
using BusinessSuite.BLL.DTOs;
using BusinessSuite.BLL.Services;
using BusinessSuite.BLL.StaticData;
using Avalonia.Threading;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BusinessSuite.DAL.Entities;

namespace BusinessSuite.UI.ViewModels;

public partial class RegisterFormViewModel : ViewModelBase
{
    private readonly RegisterService _registerService;

    // The DTO that holds our form data
    public RegistrationRequest Request { get; set; } = new();

    // List of Indian States and UTs (Fetched from shared location)
    public IEnumerable<string> States => LocationData.IndianStates;

    // List of GST Types for ComboBox
    public BusinessGstType[] GstTypes { get; } = Enum.GetValues<BusinessGstType>();

    // UI Status Message
    [ObservableProperty] private string _statusMessage = "";

    // Status Color
    [ObservableProperty] private string _statusColor = "#DC2626"; // Default red

    // IsBusy state for UI feedback
    [ObservableProperty] private bool _isBusy;

    // Reactive property for GST Registered checkbox
    [ObservableProperty] private bool _isGstRegistered;

    // Command for the Register Button
    public IAsyncRelayCommand RegisterCommand { get; }

    public RegisterFormViewModel(RegisterService registerService)
    {
        _registerService = registerService;

        // Create the command
        RegisterCommand = new AsyncRelayCommand(ExecuteRegister);
    }

    private async Task ExecuteRegister()
    {
        if (IsBusy) return;

        try
        {
            // Sync reactive property to DTO
            Request.IsGSTRegistered = IsGstRegistered;

            // 1. Client-side Validation logic
            if (string.IsNullOrWhiteSpace(Request.BusinessName)) { SetStatus("Business Name is required.", "#DC2626"); return; }
            if (string.IsNullOrWhiteSpace(Request.OwnerName)) { SetStatus("Owner Name is required.", "#DC2626"); return; }
            if (string.IsNullOrWhiteSpace(Request.Email)) { SetStatus("Email is required.", "#DC2626"); return; }
            if (string.IsNullOrWhiteSpace(Request.Address)) { SetStatus("Address is required.", "#DC2626"); return; }
            if (string.IsNullOrWhiteSpace(Request.State)) { SetStatus("State is required.", "#DC2626"); return; }
            
            if (Request.IsGSTRegistered)
            {
                if (string.IsNullOrWhiteSpace(Request.GSTIN)) { SetStatus("GSTIN is required.", "#DC2626"); return; }
                if (Request.GSTIN.Length != 15) { SetStatus("GSTIN must be 15 chars.", "#DC2626"); return; }
                if (Request.GstType == null) { SetStatus("Select GST Type.", "#DC2626"); return; }
            }

            if (string.IsNullOrWhiteSpace(Request.UserName)) { SetStatus("Username is required.", "#DC2626"); return; }
            Request.UserName = Request.UserName.ToLower().Trim();

            if (string.IsNullOrWhiteSpace(Request.Password)) { SetStatus("Password is required.", "#DC2626"); return; }
            if (Request.Password.Length < 6) { SetStatus("Password must be at least 6 chars.", "#DC2626"); return; }

            Dispatcher.UIThread.Post(() => IsBusy = true);

            SetStatus("Processing registration...", "#2563EB");
            
            var result = await _registerService.RegisterBusiness(Request);
            
            if (result == "Success")
            {
                SetStatus("Registration successful! Please close the window and run the application to login.", "#16A34A");
            }
            else
            {
                SetStatus(result, "#DC2626");
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Error: {ex.Message}", "#DC2626");
        }
        finally
        {
            Dispatcher.UIThread.Post(() => IsBusy = false);
        }
    }

    private void SetStatus(string message, string color)
    {
        Dispatcher.UIThread.Post(() =>
        {
            StatusMessage = message;
            StatusColor = color;
        });
    }

    public async Task RestoreDataAsync(string path)
    {
        try
        {
            var service = new BLL.Services.BackupService();
            await service.RestoreDatabaseAsync(path);
        }
        catch (ApplicationException ex)
            when (ex.Message == "RESTORE_SUCCESS_RESTART_REQUIRED")
        {
            SetStatus("Restore completed. Restarting application...", "#16A34A");

            await Task.Delay(800); // allow UI to update

            var exePath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exePath))
            {
                System.Diagnostics.Process.Start(exePath);
            }

            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            SetStatus("Restore failed: " + ex.Message, "#DC2626");
        }
    }
}