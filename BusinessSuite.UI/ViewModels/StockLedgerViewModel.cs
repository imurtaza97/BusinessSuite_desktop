using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BusinessSuite.BLL.Services;
using BusinessSuite.DAL.Data;
using BusinessSuite.DAL.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;

namespace BusinessSuite.UI.ViewModels;

public partial class StockLedgerViewModel : ViewModelBase
{
    private readonly LedgerService _ledgerService;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly int _businessId;

    [ObservableProperty] private ObservableCollection<StockTransaction> _transactions = new();
    [ObservableProperty] private ObservableCollection<Product> _products = new();
    [ObservableProperty] private ObservableCollection<Warehouse> _warehouses = new();
    
    [ObservableProperty] private Product? _selectedProduct;
    [ObservableProperty] private Warehouse? _selectedWarehouse;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 25;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _totalPages;
    [ObservableProperty] private bool _isBusy;

    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;

    public StockLedgerViewModel(IDbContextFactory<AppDbContext> dbFactory, int businessId)
    {
        _dbFactory = dbFactory;
        _ledgerService = new LedgerService(dbFactory);
        _businessId = businessId;

        LoadDataCommand = new AsyncRelayCommand(LoadDataAsync);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        NextPageCommand = new AsyncRelayCommand(NextPageAsync);
        PreviousPageCommand = new AsyncRelayCommand(PreviousPageAsync);
    }

    public IAsyncRelayCommand LoadDataCommand { get; }
    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand NextPageCommand { get; }
    public IAsyncRelayCommand PreviousPageCommand { get; }

    private async Task LoadDataAsync()
    {
        IsBusy = true;
        try
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var products = await db.Products.Where(p => p.BusinessID == _businessId).ToListAsync();
            var warehouses = await db.Warehouses.Where(w => w.BusinessId == _businessId).ToListAsync();

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => {
                Products = new ObservableCollection<Product>(products);
                Warehouses = new ObservableCollection<Warehouse>(warehouses);
            });

            await RefreshAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RefreshAsync()
    {
        IsBusy = true;
        try
        {
            TotalCount = await _ledgerService.GetStockLedgerCountAsync(_businessId, SelectedProduct?.ProductID, SelectedWarehouse?.WarehouseID);
            TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);

            if (CurrentPage > TotalPages && TotalPages > 0) CurrentPage = TotalPages;
            if (CurrentPage < 1) CurrentPage = 1;

            var txs = await _ledgerService.GetStockLedgerPaginatedAsync(_businessId, CurrentPage, PageSize, SelectedProduct?.ProductID, SelectedWarehouse?.WarehouseID);
            
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => {
                Transactions = new ObservableCollection<StockTransaction>(txs);
                OnPropertyChanged(nameof(HasPreviousPage));
                OnPropertyChanged(nameof(HasNextPage));
            });
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task NextPageAsync()
    {
        if (HasNextPage)
        {
            CurrentPage++;
            await RefreshAsync();
        }
    }

    private async Task PreviousPageAsync()
    {
        if (HasPreviousPage)
        {
            CurrentPage--;
            await RefreshAsync();
        }
    }

    partial void OnSelectedProductChanged(Product? value)
    {
        CurrentPage = 1;
        _ = RefreshAsync();
    }

    partial void OnSelectedWarehouseChanged(Warehouse? value)
    {
        CurrentPage = 1;
        _ = RefreshAsync();
    }
}
