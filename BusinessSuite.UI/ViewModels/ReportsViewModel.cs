using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using BusinessSuite.BLL.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BusinessSuite.UI.ViewModels;

public partial class ReportsViewModel : ViewModelBase
{
    private readonly AnalyticsService _analyticsService;
    private readonly int _businessId;

    [ObservableProperty] private DateTimeOffset _startDate = DateTimeOffset.Now.AddMonths(-6);
    [ObservableProperty] private DateTimeOffset _endDate = DateTimeOffset.Now;
    
    [ObservableProperty] private decimal _totalSales;
    [ObservableProperty] private decimal _totalPurchases;
    [ObservableProperty] private decimal _totalGstCollected;
    [ObservableProperty] private decimal _totalGstPaid;
    [ObservableProperty] private decimal _netProfit;

    public ObservableCollection<FinancialReportRow> ReportRows { get; } = new();

    public IAsyncRelayCommand LoadReportCommand { get; }

    public ReportsViewModel(int businessId)
    {
        _businessId = businessId;
        _analyticsService = new AnalyticsService(new SimpleDbContextFactory());
        LoadReportCommand = new AsyncRelayCommand(LoadReportAsync);
        _ = LoadReportAsync();
    }

    private async Task LoadReportAsync()
    {
        var rows = await _analyticsService.GetFinancialBreakdownAsync(_businessId, StartDate.DateTime, EndDate.DateTime);
        
        ReportRows.Clear();
        decimal sales = 0, purchases = 0, gstColl = 0, gstPaid = 0;

        foreach (var row in rows)
        {
            ReportRows.Add(row);
            sales += row.SalesAmount;
            purchases += row.PurchaseAmount;
            gstColl += row.GstCollected;
            gstPaid += row.GstPaid;
        }

        TotalSales = sales;
        TotalPurchases = purchases;
        TotalGstCollected = gstColl;
        TotalGstPaid = gstPaid;
        NetProfit = sales - purchases;
    }

    partial void OnStartDateChanged(DateTimeOffset value) => _ = LoadReportAsync();
    partial void OnEndDateChanged(DateTimeOffset value) => _ = LoadReportAsync();
}
