using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using BusinessSuite.BLL.DTOs;
using BusinessSuite.BLL.Services;
using BusinessSuite.DAL.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BusinessSuite.UI.ViewModels;

public partial class ReportsViewModel : ViewModelBase
{
    private readonly int _businessId;
    private readonly ReportingService _reportingService;

    public ObservableCollection<VendorPerformanceStats> TopVendors { get; } = new();
    public ObservableCollection<CustomerInsightStats> TopCustomers { get; } = new();
    
    // GST Reports
    [ObservableProperty] private GstReportItem _gstReport = new();
    public ObservableCollection<Gstr1Item> Gstr1Items { get; } = new();
    public ObservableCollection<Gstr3BItem> Gstr3BItems { get; } = new();
    public ObservableCollection<TaxLiabilityReportItem> TaxLiabilityItems { get; } = new();
    public ObservableCollection<HsnSummaryItem> HsnSummaryItems { get; } = new();
    public ObservableCollection<string> PeriodOptions { get; } = new() { "month", "quarter" };
    
    [ObservableProperty] private DateTimeOffset? _startDate = DateTimeOffset.Now.AddMonths(-1);
    [ObservableProperty] private DateTimeOffset? _endDate = DateTimeOffset.Now;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isGstRegistered;
    [ObservableProperty] private string _selectedPeriod = "month"; // month or quarter

    public ReportsViewModel(int businessId)
    {
        _businessId = businessId;
        var db = new AppDbContext();
        _reportingService = new ReportingService(db);
        LoadReportsCommand = new AsyncRelayCommand(LoadDataAsync);

        // Load business GST status
        var business = db.Businesses.Find(businessId);
        IsGstRegistered = business?.IsGSTRegistered ?? false;
    }

    public IAsyncRelayCommand LoadReportsCommand { get; }

    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try 
        {
            var start = StartDate?.DateTime ?? DateTime.Today.AddMonths(-1);
            var end = EndDate?.DateTime ?? DateTime.Today;

            // Fetch basic reports (vendors, customers, basic GST)
            var vendors = await _reportingService.GetVendorPerformanceAsync(_businessId);
            var customers = await _reportingService.GetCustomerInsightsAsync(_businessId);
            var report = await _reportingService.GetGstReportAsync(_businessId, start, end);

            // Update UI
            TopVendors.Clear();
            foreach (var v in vendors) TopVendors.Add(v);

            TopCustomers.Clear();
            foreach (var c in customers) TopCustomers.Add(c);

            GstReport = report;

            // Fetch GST detailed reports
            if (IsGstRegistered)
            {
                var gstr1 = await _reportingService.GetGstr1ReportAsync(_businessId, start, end);
                Gstr1Items.Clear();
                foreach (var item in gstr1) Gstr1Items.Add(item);

                var gstr3b = await _reportingService.GetGstr3BReportAsync(_businessId, start, end);
                Gstr3BItems.Clear();
                foreach (var item in gstr3b) Gstr3BItems.Add(item);

                var taxLiability = await _reportingService.GetTaxLiabilityReportAsync(_businessId, start, end, SelectedPeriod);
                TaxLiabilityItems.Clear();
                foreach (var item in taxLiability) TaxLiabilityItems.Add(item);

                var hsnSummary = await _reportingService.GetHsnSummaryReportAsync(_businessId, start, end);
                HsnSummaryItems.Clear();
                foreach (var item in hsnSummary) HsnSummaryItems.Add(item);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");

            Console.WriteLine($"FATAL ERROR: {ex.Message}");
    Console.WriteLine($"STACK TRACE: {ex.StackTrace}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}