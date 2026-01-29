using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using BusinessSuite.BLL.Services;
using BusinessSuite.DAL.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BusinessSuite.UI.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    private readonly AnalyticsService _analyticsService;
    private readonly int _businessId;

    [ObservableProperty] private decimal _totalSales;
    [ObservableProperty] private decimal _totalPurchase;
    [ObservableProperty] private decimal _totalGst;
    [ObservableProperty] private int _newOrdersCount;
    [ObservableProperty] private int _customerCount;
    [ObservableProperty] private decimal _turnover;

    public ObservableCollection<RecentTransaction> RecentTransactions { get; } = new();

    public IAsyncRelayCommand LoadStatsCommand { get; }

    public HomeViewModel(int businessId)
    {
        _businessId = businessId;
        _analyticsService = new AnalyticsService(new SimpleDbContextFactory());
        LoadStatsCommand = new AsyncRelayCommand(LoadStatsAsync);
        _ = LoadStatsAsync();
    }

    public HomeViewModel() // Designer support
    {
        _analyticsService = new AnalyticsService(new SimpleDbContextFactory());
        LoadStatsCommand = new AsyncRelayCommand(LoadStatsAsync);
    }

    private async Task LoadStatsAsync()
    {
        try
        {
            var summary = await _analyticsService.GetDashboardSummaryAsync(_businessId);
            
            TotalSales = summary.TotalSales;
            TotalPurchase = summary.TotalPurchase;
            TotalGst = summary.TotalGst;
            NewOrdersCount = summary.NewOrdersCount;
            CustomerCount = summary.CustomerCount;
            Turnover = summary.Turnover;

            RecentTransactions.Clear();
            foreach (var t in summary.RecentTransactions)
            {
                RecentTransactions.Add(t);
            }
        }
        catch (Exception)
        {
            // Handle error or log
        }
    }
}
