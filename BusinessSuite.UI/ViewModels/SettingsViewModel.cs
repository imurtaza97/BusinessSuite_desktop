using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BusinessSuite.BLL.Services;
using BusinessSuite.DAL.Data;
using BusinessSuite.DAL.Entities;
using BusinessSuite.DAL.Repositories;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BusinessSuite.UI.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly int _businessId;
    private readonly AppDbContextFactory _dbFactory;
    private readonly FinancialYearRepository _fyRepo;
    private readonly FinancialYearService _fyService;

    // ── Sidebar sections ─────────────────────────────────────────────────────
    [ObservableProperty] private string _selectedSection = "Profile";

    public System.Collections.Generic.List<string> SettingsSections { get; } = new()
    {
        "Profile",
        "Tax & GST",
        "Bank Details",
        "Financial Year",
        "Units of Measure",
        "Data Management"
    };

    // ── Profile ──────────────────────────────────────────────────────────────
    [ObservableProperty] private string _businessName = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _contactNo = string.Empty;
    [ObservableProperty] private string _address = string.Empty;
    [ObservableProperty] private string _state = string.Empty;

    // PAN
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PanValidationIcon))]
    [NotifyPropertyChangedFor(nameof(PanValidationColor))]
    [NotifyPropertyChangedFor(nameof(PanError))]
    private string _pan = string.Empty;

    public string PanValidationIcon =>
        string.IsNullOrWhiteSpace(Pan) ? "○"
        : ValidationService.ValidatePAN(Pan).IsValid ? "✓" : "✗";

    public string PanValidationColor =>
        string.IsNullOrWhiteSpace(Pan) ? "#9CA3AF"
        : ValidationService.ValidatePAN(Pan).IsValid ? "#16A34A" : "#DC2626";

    public string PanError =>
        string.IsNullOrWhiteSpace(Pan) ? string.Empty
        : ValidationService.ValidatePAN(Pan).Error;

    // ── Tax & GST ────────────────────────────────────────────────────────────
    [ObservableProperty] private bool _isGstRegistered;
    [ObservableProperty] private string _gstin = string.Empty;
    [ObservableProperty] private string _gstType = "Regular";

    // ── Bank Details ─────────────────────────────────────────────────────────
    [ObservableProperty] private string _bankName = string.Empty;
    [ObservableProperty] private string _accountName = string.Empty;
    [ObservableProperty] private string _accountNumber = string.Empty;
    [ObservableProperty] private string _ifsc = string.Empty;

    // ── Status / misc ─────────────────────────────────────────────────────────
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isSuccess;
    [ObservableProperty] private ObservableCollection<UnitOfMeasure> _units = new();
    [ObservableProperty] private string _newUnitName = string.Empty;

    // ── Financial Year ────────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<FinancialYear> _financialYears = new();
    [ObservableProperty] private FinancialYear? _activeFY;

    // New FY form
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NewFYLabel))]
    private DateTimeOffset? _newFYStart = new DateTimeOffset(new DateTime(DateTime.Today.Year, 4, 1));

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NewFYLabel))]
    private DateTimeOffset? _newFYEnd = new DateTimeOffset(new DateTime(DateTime.Today.Year + 1, 3, 31));

    public string NewFYLabel =>
        NewFYStart.HasValue && NewFYEnd.HasValue
            ? $"FY {NewFYStart.Value:yyyy}-{NewFYEnd.Value:yy}"
            : "Select dates above";

    [ObservableProperty] private bool _showNewFYForm = false;
    [ObservableProperty] private string _fyStatusMessage = string.Empty;

    // Close-with-carry-forward dialog
    [ObservableProperty] private bool _showCloseDialog = false;
    [ObservableProperty] private FinancialYear? _fyToClose;
    [ObservableProperty] private FinancialYear? _selectedNextFY;

    public ObservableCollection<FinancialYear> OpenFinancialYears =>
        new(FinancialYears.Where(fy => !fy.IsClosed && fy != FyToClose));

    // ── Static data ───────────────────────────────────────────────────────────
    public System.Collections.Generic.IEnumerable<string> States =>
        BLL.StaticData.LocationData.IndianStates;
    public string[] GstTypesList => new[] { "Regular", "Composition" };

    // ─────────────────────────────────────────────────────────────────────────
    public SettingsViewModel(int businessId)
    {
        _businessId = businessId;
        _dbFactory = new AppDbContextFactory();
        _fyRepo = new FinancialYearRepository(_dbFactory);
        _fyService = new FinancialYearService(_dbFactory);

        LoadBusinessDetails();
        _ = LoadFinancialYearsAsync();
    }

    // ── Load ──────────────────────────────────────────────────────────────────

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
            Pan = business.PAN ?? "";
            IsGstRegistered = business.IsGSTRegistered;
            Gstin = business.GSTIN ?? "";
            GstType = business.GstType?.ToString() ?? "Regular";
            BankName = business.BankName ?? "";
            AccountName = business.AccountName ?? "";
            AccountNumber = business.AccountNumber ?? "";
            Ifsc = business.IFSC ?? "";
        }

        var unitList = db.UnitsOfMeasure
            .Where(u => u.BusinessId == 0 || u.BusinessId == _businessId)
            .ToList();
        foreach (var u in unitList) Units.Add(u);
    }

    private async Task LoadFinancialYearsAsync()
    {
        var list = await _fyRepo.GetAllAsync(_businessId);
        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            FinancialYears.Clear();
            foreach (var fy in list) FinancialYears.Add(fy);
            ActiveFY = FinancialYears.FirstOrDefault(fy => fy.IsActive);
        });
    }

    // ── Save Settings ─────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task SaveAsync()
    {
        // Validate PAN before saving
        var (panOk, normPan, panErr) = ValidationService.ValidatePAN(Pan);
        if (!panOk)
        {
            StatusMessage = $"PAN error: {panErr}";
            IsSuccess = false;
            return;
        }

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
                business.PAN = string.IsNullOrWhiteSpace(normPan) ? null : normPan;
                business.IsGSTRegistered = IsGstRegistered;
                business.GSTIN = IsGstRegistered ? Gstin : null;
                business.GstType = IsGstRegistered
                    ? (BusinessGstType)Enum.Parse(typeof(BusinessGstType), GstType)
                    : null;
                business.BankName = BankName;
                business.AccountName = AccountName;
                business.AccountNumber = AccountNumber;
                business.IFSC = Ifsc;

                await db.SaveChangesAsync();

                // Refresh PAN display after normalisation
                Pan = normPan;

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
        StatusMessage = string.Empty;
    }

    // ── Financial Year commands ───────────────────────────────────────────────

    [RelayCommand]
    private void ShowNewFYFormCommand()
    {
        // Suggest next FY after the latest one
        var latest = FinancialYears.OrderByDescending(f => f.StartDate).FirstOrDefault();
        if (latest != null)
        {
            NewFYStart = new DateTimeOffset(latest.EndDate.AddDays(1));
            var endYear = latest.EndDate.AddDays(1).Year;
            NewFYEnd = new DateTimeOffset(new DateTime(endYear + 1, 3, 31));
        }
        ShowNewFYForm = true;
        FyStatusMessage = string.Empty;
    }

    [RelayCommand]
    private void CancelNewFY() => ShowNewFYForm = false;

    [RelayCommand]
    private async Task CreateFinancialYearAsync()
    {
        try
        {
            if (!NewFYStart.HasValue || !NewFYEnd.HasValue)
            {
                FyStatusMessage = "⚠ Please select both start and end dates.";
                return;
            }

            var start = NewFYStart.Value.DateTime;
            var end = NewFYEnd.Value.DateTime;

            if (end <= start)
            {
                FyStatusMessage = "⚠ End date must be after start date.";
                return;
            }

            var created = await _fyRepo.CreateAsync(_businessId, start, end);
            await LoadFinancialYearsAsync();

            ShowNewFYForm = false;
            FyStatusMessage = $"✓ {created.Label} created{(created.IsActive ? " and set as active." : ".")}";
        }
        catch (Exception ex)
        {
            FyStatusMessage = $"⚠ {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SetActiveFYAsync(FinancialYear fy)
    {
        try
        {
            await _fyRepo.SetActiveAsync(fy.FinancialYearID, _businessId);
            await LoadFinancialYearsAsync();
            FyStatusMessage = $"✓ {fy.Label} is now the active financial year.";
        }
        catch (Exception ex)
        {
            FyStatusMessage = $"⚠ {ex.Message}";
        }
    }

    [RelayCommand]
    private void RequestCloseFY(FinancialYear fy)
    {
        FyToClose = fy;
        SelectedNextFY = FinancialYears.FirstOrDefault(f => !f.IsClosed && f != fy);
        OnPropertyChanged(nameof(OpenFinancialYears));
        ShowCloseDialog = true;
        FyStatusMessage = string.Empty;
    }

    [RelayCommand]
    private void CancelCloseFY()
    {
        ShowCloseDialog = false;
        FyToClose = null;
    }

    [RelayCommand]
    private async Task ConfirmCloseYearAsync()
    {
        if (FyToClose == null) return;

        (bool ok, string msg) result;

        if (SelectedNextFY != null)
        {
            result = await _fyService.CloseAndCarryForwardAsync(
                FyToClose.FinancialYearID,
                SelectedNextFY.FinancialYearID,
                _businessId);
        }
        else
        {
            result = await _fyService.CloseYearOnlyAsync(
                FyToClose.FinancialYearID,
                _businessId);
        }

        ShowCloseDialog = false;
        FyToClose = null;
        await LoadFinancialYearsAsync();
        FyStatusMessage = result.ok ? $"✓ {result.msg}" : $"⚠ {result.msg}";
    }

    [RelayCommand]
    private async Task DeleteFYAsync(FinancialYear fy)
    {
        var ok = await _fyRepo.DeleteAsync(fy.FinancialYearID, _businessId);
        if (ok)
        {
            await LoadFinancialYearsAsync();
            FyStatusMessage = $"✓ {fy.Label} deleted.";
        }
        else
        {
            FyStatusMessage = "⚠ Cannot delete an active or closed financial year.";
        }
    }

    // ── Units of Measure ──────────────────────────────────────────────────────

    [RelayCommand]
    private async Task AddUnitAsync()
    {
        if (string.IsNullOrWhiteSpace(NewUnitName)) return;

        using var db = new AppDbContext();
        var unit = new UnitOfMeasure { BusinessId = _businessId, Name = NewUnitName.ToUpper() };
        db.UnitsOfMeasure.Add(unit);
        await db.SaveChangesAsync();

        Units.Add(unit);
        NewUnitName = string.Empty;
    }

    [RelayCommand]
    private async Task RemoveUnitAsync(UnitOfMeasure unit)
    {
        if (unit == null || unit.BusinessId == 0) return;

        using var db = new AppDbContext();
        db.UnitsOfMeasure.Remove(unit);
        await db.SaveChangesAsync();

        Units.Remove(unit);
    }

    // ── Backup / Restore ──────────────────────────────────────────────────────

    public async Task BackupDataAsync(string path)
    {
        try
        {
            var service = new BLL.Services.BackupService();
            await service.BackupDatabaseAsync(path);
            IsSuccess = true;
            StatusMessage = "Backup created successfully!";
        }
        catch (Exception ex)
        {
            IsSuccess = false;
            StatusMessage = "Backup failed: " + ex.Message;
        }
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
            IsSuccess = true;
            StatusMessage = "Restore completed. Restarting application...";

            await Task.Delay(800);

            var exePath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exePath))
                System.Diagnostics.Process.Start(exePath);

            Environment.Exit(0);
        }
    }
}
