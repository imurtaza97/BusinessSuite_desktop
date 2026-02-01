using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BusinessSuite.BLL.DTOs;
using BusinessSuite.BLL.Services;
using BusinessSuite.DAL.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace BusinessSuite.UI.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    private readonly int _businessId;
    private readonly ReportingService _reportingService;
    private readonly ObservableCollection<double> _chartValues = new() { 0, 0, 0, 0, 0, 0 };

    [ObservableProperty] private decimal _totalSales;
    [ObservableProperty] private decimal _totalPurchases;
    [ObservableProperty] private decimal _netProfit;
    [ObservableProperty] private int _activeOrders;
    [ObservableProperty] private decimal _totalReceivable;
    [ObservableProperty] private ObservableCollection<BusinessSuite.DAL.Entities.Invoice> _recentActivity = new();

    // The Command definition that was missing
    public IAsyncRelayCommand LoadDashboardDataCommand { get; }

    // Chart Series
    public ISeries[] SalesTrendSeries { get; set; }


    public Axis[] XAxes { get; set; } = 
    {
        new Axis
        {
            Labels = new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun" },
            MinStep = 1, // Add this to prevent 0.1, 0.2...
            ForceStepToMin = true, // Force it to stay on whole numbers
            LabelsRotation = 0,
            SeparatorsPaint = new SolidColorPaint(SKColors.LightGray) { StrokeThickness = 1 }
        }
    };
    
    public HomeViewModel(int businessId)
    {
        _businessId = businessId;
        _reportingService = new ReportingService(new AppDbContext());
        
        // Initialize Command
        LoadDashboardDataCommand = new AsyncRelayCommand(LoadDataAsync);

        // Initialize Series pointing to the observable collection
        SalesTrendSeries = new ISeries[]
        {
            new LineSeries<double>
            {
                Values = _chartValues,
                Fill = null,
                GeometrySize = 8,
                Stroke = new SolidColorPaint(SKColors.CornflowerBlue) { StrokeThickness = 3 }
            }
        };
    }

    public HomeViewModel() : this(0) // Reuse main constructor for designer
    {
        TotalSales = 150000;
        TotalPurchases = 80000;
        NetProfit = 70000;
    }

    private async Task LoadDataAsync()
    {
        try 
        {
            var summary = await _reportingService.GetDashboardSummaryAsync(_businessId);
            
            TotalSales = summary.TotalSales;
            TotalPurchases = summary.TotalPurchases;
            NetProfit = summary.NetProfit;
            ActiveOrders = summary.ActiveOrdersCount;
            TotalReceivable = summary.TotalReceivable;

            var recent = await _reportingService.GetRecentInvoicesAsync(_businessId, 5);
            RecentActivity.Clear();
            foreach (var invoice in recent)
            {
                RecentActivity.Add(invoice);
            }

            // Load trend data
            var trend = await _reportingService.GetSalesAnalyticsAsync(_businessId, 6);
            
            _chartValues.Clear();
            if (trend == null || !trend.Any())
            {
                for (int i = 0; i < 6; i++) _chartValues.Add(0);
            }
            else
            {
                foreach (var point in trend)
                {
                    _chartValues.Add(point.Value);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Dashboard Load Error: {ex.Message}");
        }
    }
}