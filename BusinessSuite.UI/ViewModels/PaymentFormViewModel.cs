using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BusinessSuite.BLL.Services;
using BusinessSuite.DAL.Data;
using BusinessSuite.DAL.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BusinessSuite.UI.ViewModels;

public partial class PaymentFormViewModel : ViewModelBase
{
    private readonly LedgerService _ledgerService;
    private readonly int _businessId;
    private readonly string _entityType;
    private readonly int _entityId;

    [ObservableProperty] private string _title;
    [ObservableProperty] private decimal _amount;
    [ObservableProperty] private DateTime _paymentDate = DateTime.Now;
    [ObservableProperty] private string _paymentMethod = "Cash";
    [ObservableProperty] private string _reference = string.Empty;
    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private string _generalErrorMessage = string.Empty;

    public ObservableCollection<string> PaymentMethods { get; } = new()
    {
        "Cash", "Bank Transfer", "Cheque", "UPI", "Credit Card", "Other"
    };

    public PaymentFormViewModel(int businessId, string entityType, int entityId, string entityName, LedgerService ledgerService)
    {
        _businessId = businessId;
        _entityType = entityType;
        _entityId = entityId;
        _ledgerService = ledgerService;
        Title = $"Add Payment - {entityName}";

        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new RelayCommand(Cancel);
    }

    public IAsyncRelayCommand SaveCommand { get; }
    public IRelayCommand CancelCommand { get; }

    public event Action<bool>? RequestClose;

    private async Task SaveAsync()
    {
        if (Amount <= 0)
        {
            GeneralErrorMessage = "Amount must be greater than zero.";
            return;
        }

        try
        {
            await _ledgerService.ProcessPaymentAsync(
                _businessId, 
                _entityType, 
                _entityId, 
                Amount, 
                PaymentMethod, 
                Reference, 
                Notes);
            
            RequestClose?.Invoke(true);
        }
        catch (Exception ex)
        {
            GeneralErrorMessage = "Failed to save payment: " + ex.Message;
        }
    }

    private void Cancel()
    {
        RequestClose?.Invoke(false);
    }
}
