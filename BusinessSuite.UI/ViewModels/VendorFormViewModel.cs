using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BusinessSuite.BLL.Services;
using BusinessSuite.BLL.StaticData;
using BusinessSuite.DAL.Data;
using BusinessSuite.DAL.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BusinessSuite.UI.ViewModels;

public partial class VendorFormViewModel : ViewModelBase, INotifyDataErrorInfo
{
    private readonly Dictionary<string, List<string>> _errors = new();
    
    public IEnumerable<string> States => LocationData.IndianStates;
    public IEnumerable<string> GstTreatments => new[] { "Regular", "Composition", "Consumer", "Unregistered", "Overseas", "Special Economic Zone" };
    
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
        
        if (string.IsNullOrWhiteSpace(VendorName))
            AddError(nameof(VendorName), "Vendor Name is required");
        else if (VendorName.Length > 100)
            AddError(nameof(VendorName), "Vendor Name cannot exceed 100 characters");

        if (!string.IsNullOrWhiteSpace(GSTIN) && GSTIN.Length != 15)
            AddError(nameof(GSTIN), "GSTIN must be 15 characters long");

        if (IsGstRegistered)
        {
            if (string.IsNullOrWhiteSpace(GSTIN))
                AddError(nameof(GSTIN), "GSTIN is required for registered vendors");
        }

        if (string.IsNullOrWhiteSpace(GstTreatment))
            AddError(nameof(GstTreatment), "GST Treatment is required");

        if (string.IsNullOrWhiteSpace(State))
            AddError(nameof(State), "State is required");

        if (ContactNo?.Length > 15)
            AddError(nameof(ContactNo), "Contact No cannot exceed 15 characters");

        if (Email?.Length > 100)
            AddError(nameof(Email), "Email cannot exceed 100 characters");

        if (Address?.Length > 255)
            AddError(nameof(Address), "Address cannot exceed 255 characters");

        if (State?.Length > 50)
            AddError(nameof(State), "State cannot exceed 50 characters");

        OnPropertyChanged(nameof(HasErrors));
    }

    [ObservableProperty]
    private string _generalErrorMessage = string.Empty;
    
    [ObservableProperty]
    private bool _validationVisible = false;

    [ObservableProperty]
    private int _vendorId;

    [ObservableProperty]
    private string _title = "Add Vendor";

    [ObservableProperty]
    private bool _isServiceProvider;

    public List<string> VendorTypes { get; } = new() { "Vendor", "Service Provider" };

    public string VendorType
    {
        get => IsServiceProvider ? "Service Provider" : "Vendor";
        set => IsServiceProvider = value == "Service Provider";
    }

    public string VendorNameLabel => IsServiceProvider ? "Service Provider Name *" : "Vendor Name *";
    public string VendorNameWatermark => IsServiceProvider ? "Enter service provider or company name" : "Enter vendor or company name";

    partial void OnIsServiceProviderChanged(bool value)
    {
        UpdateTitle();
        OnPropertyChanged(nameof(VendorType));
        OnPropertyChanged(nameof(VendorNameLabel));
        OnPropertyChanged(nameof(VendorNameWatermark));
    }

    private void UpdateTitle()
    {
        Title = IsServiceProvider
            ? (VendorId == 0 ? "Add Service Provider" : "Edit Service Provider")
            : (VendorId == 0 ? "Add Vendor" : "Edit Vendor");
    }

    private string _vendorName = string.Empty;
    public string VendorName
    {
        get => _vendorName;
        set 
        {
            if (SetProperty(ref _vendorName, value))
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

    private string? _address;
    public string? Address
    {
        get => _address;
        set 
        {
            if (SetProperty(ref _address, value))
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

    private string? _gstTreatment = "Unregistered";
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

    private readonly int _businessId;
    private readonly ValidationService _validationService;
    private bool _duplicateWarningShown = false;

    public VendorFormViewModel(int businessId)
    {
        _businessId = businessId;
        _validationService = new ValidationService(new AppDbContextFactory());
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new RelayCommand(Cancel);
    }

    public VendorFormViewModel(int businessId, Vendor vendor) : this(businessId)
    {
        Title = "Edit Vendor";
        VendorId = vendor.VendorID;
        IsServiceProvider = vendor.IsServiceProvider;
        VendorName = vendor.VendorName;
        GSTIN = vendor.GSTIN;
        ContactNo = vendor.ContactNo;
        Email = vendor.Email;
        Address = vendor.Address;
        State = vendor.State;
        GstTreatment = vendor.GstTreatment ?? "Unregistered";
        BankName = vendor.BankName;
        AccountName = vendor.AccountName;
        AccountNumber = vendor.AccountNumber;
        Ifsc = vendor.IFSC;
    }

    public IAsyncRelayCommand SaveCommand { get; }
    public IRelayCommand CancelCommand { get; }

    public event Action<Vendor?>? RequestClose;

    private async Task SaveAsync()
    {
        ValidationVisible = true;
        ValidateAll();
        
        foreach (var propertyName in new[] { nameof(VendorName), nameof(GSTIN), nameof(ContactNo), nameof(Email), nameof(State) })
        {
             ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        if (HasErrors)
        {
            GeneralErrorMessage = "Please correct the errors before saving.";
            return;
        }

        var vendor = new Vendor
        {
            VendorID = VendorId,
            VendorName = VendorName,
            GSTIN = GSTIN,
            ContactNo = ContactNo,
            Email = Email,
            Address = Address,
            State = State,
            GstTreatment = GstTreatment,
            IsServiceProvider = IsServiceProvider,
            BankName = BankName,
            AccountName = AccountName,
            AccountNumber = AccountNumber,
            IFSC = Ifsc,
            BusinessId = _businessId
        };

        // Duplicate check (first press shows warning, second press overrides)
        if (!_duplicateWarningShown)
        {
            var (isDuplicate, dupMessage) = await _validationService.CheckVendorDuplicateAsync(vendor);
            if (isDuplicate)
            {
                _duplicateWarningShown = true;
                GeneralErrorMessage = $"⚠ Duplicate detected: {dupMessage}\nClick 'Save Vendor' again to save anyway, or cancel to go back.";
                return; // Stop — do not close window
            }
        }
        // Either no duplicate, or user confirmed by pressing save again
        _duplicateWarningShown = false;
        RequestClose?.Invoke(vendor);
    }

    private void Cancel()
    {
        RequestClose?.Invoke(null);
    }
}
