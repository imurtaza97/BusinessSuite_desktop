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
    [ObservableProperty] private System.Collections.ObjectModel.ObservableCollection<UnitOfMeasure> _uoms = new();
    [ObservableProperty] private string _newUomName = string.Empty;

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
            Address = business.Address ?? "";
            State = business.State ?? "";
            IsGstRegistered = business.IsGSTRegistered;
            Gstin = business.GSTIN ?? "";
            GstType = business.GstType?.ToString() ?? "Regular";
            BankName = business.BankName ?? "";
            AccountNumber = business.AccountNumber ?? "";
            Ifsc = business.IFSC ?? "";
        }

        var uomList = db.UnitsOfMeasure.Where(u => u.BusinessId == 0 || u.BusinessId == _businessId).ToList();
        foreach (var u in uomList) Uoms.Add(u);
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
    private async Task AddUomAsync()
    {
        if (string.IsNullOrWhiteSpace(NewUomName)) return;

        using var db = new AppDbContext();
        var uom = new UnitOfMeasure { BusinessId = _businessId, Name = NewUomName.ToUpper() };
        db.UnitsOfMeasure.Add(uom);
        await db.SaveChangesAsync();

        Uoms.Add(uom);
        NewUomName = "";
    }

    [RelayCommand]
    private async Task RemoveUomAsync(UnitOfMeasure uom)
    {
        if (uom == null || uom.BusinessId == 0) return; // Don't delete system UOMs

        using var db = new AppDbContext();
        db.UnitsOfMeasure.Remove(uom);
        await db.SaveChangesAsync();

        Uoms.Remove(uom);
    }
}
