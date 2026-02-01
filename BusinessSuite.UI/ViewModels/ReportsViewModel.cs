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

    [ObservableProperty] private GstReportItem _gstReport = new();
    [ObservableProperty] private DateTimeOffset? _startDate = DateTimeOffset.Now.AddMonths(-1);
    [ObservableProperty] private DateTimeOffset? _endDate = DateTimeOffset.Now;
    [ObservableProperty] private bool _isLoading;

    public ReportsViewModel(int businessId)
    {
        _businessId = businessId;
        _reportingService = new ReportingService(new AppDbContext());
        LoadReportsCommand = new AsyncRelayCommand(LoadDataAsync);
    }

    public IAsyncRelayCommand LoadReportsCommand { get; }

    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try 
        {
            var start = StartDate?.DateTime ?? DateTime.Today.AddMonths(-1);
            var end = EndDate?.DateTime ?? DateTime.Today;

            // Fetch Data
            var vendors = await _reportingService.GetVendorPerformanceAsync(_businessId);
            var customers = await _reportingService.GetCustomerInsightsAsync(_businessId);
            var report = await _reportingService.GetGstReportAsync(_businessId, start, end);

            // Update UI
            TopVendors.Clear();
            foreach (var v in vendors) TopVendors.Add(v);

            TopCustomers.Clear();
            foreach (var c in customers) TopCustomers.Add(c);

            GstReport = report;
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