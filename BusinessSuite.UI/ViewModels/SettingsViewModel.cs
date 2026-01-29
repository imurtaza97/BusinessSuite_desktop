using System;
using System.Linq;
using System.Threading.Tasks;
using BusinessSuite.DAL.Data;
using BusinessSuite.DAL.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BusinessSuite.UI.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly int _businessId;
    
    [ObservableProperty] private string _selectedSection = "Profile";
    
    public System.Collections.Generic.List<string> SettingsSections { get; } = new() 
    { 
        "Profile", 
        "Tax & GST", 
        "Bank Details", 
        "Units of Measure" 
    };

    [ObservableProperty] private string _businessName = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _contactNo = string.Empty;
    [ObservableProperty] private string _address = string.Empty;
    [ObservableProperty] private string _state = string.Empty;
    
    [ObservableProperty] private bool _isGstRegistered;
    [ObservableProperty] private string _gstin = string.Empty;
    [ObservableProperty] private string _gstType = "Regular";
    
    [ObservableProperty] private string _bankName = string.Empty;
    [ObservableProperty] private string _accountNumber = string.Empty;
    [ObservableProperty] private string _ifsc = string.Empty;
    
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isSuccess;
    [ObservableProperty] private System.Collections.ObjectModel.ObservableCollection<UnitOfMeasure> _units = new();
    [ObservableProperty] private string _newUnitName = string.Empty;

    public System.Collections.Generic.IEnumerable<string> States => BLL.StaticData.LocationData.IndianStates;
    public string[] GstTypesList => new[] { "Regular", "Composition" };

    public SettingsViewModel(int businessId)
    {
        _businessId = businessId;
        LoadBusinessDetails();
    }

    private void LoadBusinessDetails()
    {
        using var db = new AppDbContext();
        var business = db.Businesses.FirstOrDefault(b => b.BusinessID == _businessId);
        if (business != null)
        {
            BusinessName = business.BusinessName;
            Email = business.Email ?? "";
            ContactNo = business.ContactNo ?? "";
            Address = business.Address ?? "";
            State = business.State ?? "";
            IsGstRegistered = business.IsGSTRegistered;
            Gstin = business.GSTIN ?? "";
            GstType = business.GstType?.ToString() ?? "Regular";
            BankName = business.BankName ?? "";
            AccountNumber = business.AccountNumber ?? "";
            Ifsc = business.IFSC ?? "";
        }

        var unitList = db.UnitsOfMeasure.Where(u => u.BusinessId == 0 || u.BusinessId == _businessId).ToList();
        foreach (var u in unitList) Units.Add(u);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            using var db = new AppDbContext();
            var business = db.Businesses.FirstOrDefault(b => b.BusinessID == _businessId);
            if (business != null)
            {
                business.BusinessName = BusinessName;
                business.Email = Email;
                business.ContactNo = ContactNo;
                business.Address = Address;
                business.State = State;
                business.IsGSTRegistered = IsGstRegistered;
                business.GSTIN = IsGstRegistered ? Gstin : null;
                business.GstType = IsGstRegistered ? (BusinessGstType)Enum.Parse(typeof(BusinessGstType), GstType) : null;
                business.BankName = BankName;
                business.AccountNumber = AccountNumber;
                business.IFSC = Ifsc;

                await db.SaveChangesAsync();
                
                IsSuccess = true;
                StatusMessage = "Settings saved successfully!";
            }
        }
        catch (Exception ex)
        {
            IsSuccess = false;
            StatusMessage = "Error saving settings: " + ex.Message;
        }

        await Task.Delay(3000);
        StatusMessage = "";
    }

    [RelayCommand]
    private async Task AddUnitAsync()
    {
        if (string.IsNullOrWhiteSpace(NewUnitName)) return;

        using var db = new AppDbContext();
        var unit = new UnitOfMeasure { BusinessId = _businessId, Name = NewUnitName.ToUpper() };
        db.UnitsOfMeasure.Add(unit);
        await db.SaveChangesAsync();

        Units.Add(unit);
        NewUnitName = "";
    }

    [RelayCommand]
    private async Task RemoveUnitAsync(UnitOfMeasure unit)
    {
        if (unit == null || unit.BusinessId == 0) return; // Don't delete system units

        using var db = new AppDbContext();
        db.UnitsOfMeasure.Remove(unit);
        await db.SaveChangesAsync();

        Units.Remove(unit);
    }
}
