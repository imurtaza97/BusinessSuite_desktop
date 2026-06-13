using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using BusinessSuite.BLL.Services;
using BusinessSuite.BLL.StaticData;
using BusinessSuite.DAL.Data;
using BusinessSuite.DAL.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BusinessSuite.UI.ViewModels;

public partial class CustomerFormViewModel : ViewModelBase, INotifyDataErrorInfo
{
    private readonly Dictionary<string, List<string>> _errors = new();
    
    public IEnumerable<string> States => LocationData.IndianStates;
    public IEnumerable<string> GstTreatments => new[] { "Regular", "Composition", "Consumer", "Unregistered", "Overseas", "Special Economic Zone" };
    private readonly int _businessId;
    
    public bool HasErrors => _errors.Any();
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public IEnumerable GetErrors(string? propertyName)
    {
        if (!ValidationVisible) return Enumerable.Empty<string>();
        if (string.IsNullOrEmpty(propertyName) || !_errors.ContainsKey(propertyName))
            return Enumerable.Empty<string>();
        return _errors[propertyName];
    }

    private void AddError(string propertyName, string error)
    {
        if (!_errors.ContainsKey(propertyName))
            _errors[propertyName] = new List<string>();
        _errors[propertyName].Add(error);
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }

    private void ClearAllErrors()
    {
        var propertiesWithErrors = _errors.Keys.ToList();
        _errors.Clear();
        foreach (var propertyName in propertiesWithErrors)
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }

    private void ValidateAll()
    {
        ClearAllErrors();

        if (string.IsNullOrWhiteSpace(CustomerName))
            AddError(nameof(CustomerName), "Customer Name is required");
        else if (CustomerName.Length > 100)
            AddError(nameof(CustomerName), "Customer Name cannot exceed 100 characters");

        if (string.IsNullOrWhiteSpace(GstTreatment))
            AddError(nameof(GstTreatment), "GST Treatment is required");

        if (IsGstRegistered)
        {
            if (string.IsNullOrWhiteSpace(GSTIN))
                AddError(nameof(GSTIN), "GSTIN is required for registered customers");
            else if (GSTIN.Length != 15)
                AddError(nameof(GSTIN), "GSTIN must be 15 characters long");
            else if (GSTIN?.Length > 15)
                AddError(nameof(GSTIN), "GSTIN cannot exceed 15 characters");
        }
        else if (!string.IsNullOrWhiteSpace(GSTIN))
        {
            AddError(nameof(GSTIN), "GSTIN should only be entered for registered customers");
        }

        if (ContactNo?.Length > 15)
            AddError(nameof(ContactNo), "Contact No cannot exceed 15 characters");

        if (Email?.Length > 100)
            AddError(nameof(Email), "Email cannot exceed 100 characters");

        if (string.IsNullOrWhiteSpace(BillingAddress))
            AddError(nameof(BillingAddress), "Billing Address is required");
        else if (BillingAddress?.Length > 255)
            AddError(nameof(BillingAddress), "Billing Address cannot exceed 255 characters");

        if (ShippingAddress?.Length > 255)
            AddError(nameof(ShippingAddress), "Shipping Address cannot exceed 255 characters");

        if (string.IsNullOrWhiteSpace(State))
            AddError(nameof(State), "State is required");
        else if (State?.Length > 50)
            AddError(nameof(State), "State cannot exceed 50 characters");

        OnPropertyChanged(nameof(HasErrors));
    }

    [ObservableProperty]
    private string _generalErrorMessage = string.Empty;
    
    [ObservableProperty]
    private bool _validationVisible = false;

    [ObservableProperty]
    private int _customerId;

    [ObservableProperty]
    private string _title = "Add Customer";

    private string _customerName = string.Empty;
    public string CustomerName
    {
        get => _customerName;
        set 
        {
            if (SetProperty(ref _customerName, value))
            {
                if (ValidationVisible) ValidateAll();
            }
        }
    }

    private string? _gstin;
    public string? GSTIN
    {
        get => _gstin;
        set 
        {
            if (SetProperty(ref _gstin, value))
            {
                if (ValidationVisible) ValidateAll();
            }
        }
    }

    private string? _contactNo;
    public string? ContactNo
    {
        get => _contactNo;
        set 
        {
            if (SetProperty(ref _contactNo, value))
            {
                if (ValidationVisible) ValidateAll();
            }
        }
    }

    private string? _email;
    public string? Email
    {
        get => _email;
        set 
        {
            if (SetProperty(ref _email, value))
            {
                if (ValidationVisible) ValidateAll();
            }
        }
    }

    private string? _billingAddress;
    public string? BillingAddress
    {
        get => _billingAddress;
        set 
        {
            if (SetProperty(ref _billingAddress, value))
            {
                if (ValidationVisible) ValidateAll();
            }
        }
    }

    private string? _shippingAddress;
    public string? ShippingAddress
    {
        get => _shippingAddress;
        set 
        {
            if (SetProperty(ref _shippingAddress, value))
            {
                if (ValidationVisible) ValidateAll();
            }
        }
    }

    private string? _state;
    public string? State
    {
        get => _state;
        set 
        {
            if (SetProperty(ref _state, value))
            {
                if (ValidationVisible) ValidateAll();
            }
        }
    }

    private string? _gstTreatment = "Regular";
    public string? GstTreatment
    {
        get => _gstTreatment;
        set
        {
            if (SetProperty(ref _gstTreatment, value))
            {
                OnPropertyChanged(nameof(IsGstRegistered));
                if (!IsGstRegistered) GSTIN = null;
            }
        }
    }

    public bool IsGstRegistered => GstTreatment == "Regular" || GstTreatment == "Composition";
    public bool IsNotGstRegistered => !IsGstRegistered;
    [ObservableProperty] private string? _bankName;
    [ObservableProperty] private string? _accountName;
    [ObservableProperty] private string? _accountNumber;
    [ObservableProperty] private string? _ifsc;
    private readonly ValidationService _validationService;
    private bool _duplicateWarningShown = false;

    public CustomerFormViewModel(int businessId)
    {
        _businessId = businessId;
        _validationService = new ValidationService(new AppDbContextFactory());
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new RelayCommand(Cancel);
    }

    public CustomerFormViewModel(int businessId, Customer customer) : this(businessId)
    {
        Title = "Edit Customer";
        CustomerId = customer.CustomerID;
        CustomerName = customer.CustomerName;
        GSTIN = customer.GSTIN;
        ContactNo = customer.ContactNo;
        Email = customer.Email;
        BillingAddress = customer.BillingAddress;
        ShippingAddress = customer.ShippingAddress;
        State = customer.State;
        GstTreatment = customer.GstTreatment ?? "Regular";
        BankName = customer.BankName;
        AccountName = customer.AccountName;
        AccountNumber = customer.AccountNumber;
        Ifsc = customer.IFSC;
    }

    public IAsyncRelayCommand SaveCommand { get; }
    public IRelayCommand CancelCommand { get; }

    public event Action<Customer?>? RequestClose;

    private async Task SaveAsync()
    {
        ValidationVisible = true;
        ValidateAll();
        
        // Trigger ErrorsChanged for UI update
        foreach (var propertyName in new[] { nameof(CustomerName), nameof(GSTIN), nameof(ContactNo), nameof(Email), nameof(State) })
        {
             ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        if (HasErrors)
        {
            GeneralErrorMessage = "Please correct the errors before saving.";
            return;
        }

        var customer = new Customer
        {
            CustomerID = CustomerId,
            BusinessId = _businessId,
            CustomerName = CustomerName,
            GSTIN = GSTIN,
            ContactNo = ContactNo,
            Email = Email,
            BillingAddress = BillingAddress,
            ShippingAddress = ShippingAddress,
            State = State,
            GstTreatment = GstTreatment,
            BankName = BankName,
            AccountName = AccountName,
            AccountNumber = AccountNumber,
            IFSC = Ifsc
        };

        // Duplicate check (first press shows warning, second press overrides)
        if (!_duplicateWarningShown)
        {
            var (isDuplicate, dupMessage) = await _validationService.CheckCustomerDuplicateAsync(customer);
            if (isDuplicate)
            {
                _duplicateWarningShown = true;
                GeneralErrorMessage = $"⚠ Duplicate detected: {dupMessage}\nClick 'Save Customer' again to save anyway, or cancel to go back.";
                return; // Stop — do not close window
            }
        }
        _duplicateWarningShown = false;
        RequestClose?.Invoke(customer);
    }

    private void Cancel()
    {
        RequestClose?.Invoke(null);
    }
}
