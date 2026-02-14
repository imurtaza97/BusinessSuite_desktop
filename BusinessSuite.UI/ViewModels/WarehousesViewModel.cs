using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BusinessSuite.DAL.Data;
using BusinessSuite.DAL.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using BusinessSuite.BLL.Services;

namespace BusinessSuite.UI.ViewModels;

public partial class WarehousesViewModel : ViewModelBase
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly LedgerService _ledgerService;
    private readonly int _businessId;

    [ObservableProperty] private ObservableCollection<Warehouse> _warehouses = new();
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditWarehouseCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteWarehouseCommand))]
    private Warehouse? _selectedWarehouse;

    [ObservableProperty] private ObservableCollection<Stock> _warehouseStock = new();
    [ObservableProperty] private bool _isBusy;
    
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 15; // User specifically asked to limit or 15
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _totalPages;

    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;

    public WarehousesViewModel(IDbContextFactory<AppDbContext> dbFactory, int businessId)
    {
        _dbFactory = dbFactory;
        _ledgerService = new LedgerService(dbFactory);
        _businessId = businessId;

        LoadWarehousesCommand = new AsyncRelayCommand(LoadWarehousesAsync);
        AddWarehouseCommand = new AsyncRelayCommand(AddWarehouseAsync);
        EditWarehouseCommand = new AsyncRelayCommand(EditWarehouseAsync, () => SelectedWarehouse != null);
        DeleteWarehouseCommand = new AsyncRelayCommand(DeleteWarehouseAsync, () => SelectedWarehouse != null);
        AdjustStockCommand = new AsyncRelayCommand<Stock>(AdjustStockAsync);
        NextPageCommand = new AsyncRelayCommand(NextPageAsync);
        PreviousPageCommand = new AsyncRelayCommand(PreviousPageAsync);
    }

    public IAsyncRelayCommand NextPageCommand { get; }
    public IAsyncRelayCommand PreviousPageCommand { get; }

    public IAsyncRelayCommand LoadWarehousesCommand { get; }
    public IAsyncRelayCommand AddWarehouseCommand { get; }
    public IAsyncRelayCommand EditWarehouseCommand { get; }
    public IAsyncRelayCommand DeleteWarehouseCommand { get; }
    public IAsyncRelayCommand<Stock> AdjustStockCommand { get; }

    partial void OnSelectedWarehouseChanged(Warehouse? value)
    {
        if (value != null)
            _ = LoadWarehouseStockAsync(value.WarehouseID);
        else
            WarehouseStock.Clear();
    }

    private async Task LoadWarehousesAsync()
    {
        IsBusy = true;
        try
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var query = db.Warehouses.Where(w => w.BusinessId == _businessId);
            
            TotalCount = await query.CountAsync();
            TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);

            if (CurrentPage > TotalPages && TotalPages > 0) CurrentPage = TotalPages;
            if (CurrentPage < 1) CurrentPage = 1;

            var warehouses = await query
                .OrderBy(w => w.WarehouseName)
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
            
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => {
                Warehouses = new ObservableCollection<Warehouse>(warehouses);
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
            await LoadWarehousesAsync();
        }
    }

    private async Task PreviousPageAsync()
    {
        if (HasPreviousPage)
        {
            CurrentPage--;
            await LoadWarehousesAsync();
        }
    }

    private async Task LoadWarehouseStockAsync(int warehouseId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var stock = await db.Stocks
            .Include(s => s.Product)
            .Where(s => s.WarehouseID == warehouseId)
            .ToListAsync();
        
        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => {
            WarehouseStock = new ObservableCollection<Stock>(stock);
        });
    }

    private async Task AddWarehouseAsync()
    {
        var vm = new WarehouseFormViewModel(_businessId);
        var dialog = new Views.WarehouseFormWindow { DataContext = vm };
        
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var result = await dialog.ShowDialog<Warehouse?>(desktop.MainWindow!);
            if (result != null)
            {
                using var db = await _dbFactory.CreateDbContextAsync();
                db.Warehouses.Add(result);
                await db.SaveChangesAsync();
                Warehouses.Add(result);
                SelectedWarehouse = result;
            }
        }
    }

    private async Task EditWarehouseAsync()
    {
        if (SelectedWarehouse == null) return;

        var vm = new WarehouseFormViewModel(_businessId, SelectedWarehouse);
        var dialog = new Views.WarehouseFormWindow { DataContext = vm };
        
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var result = await dialog.ShowDialog<Warehouse?>(desktop.MainWindow!);
            if (result != null)
            {
                using var db = await _dbFactory.CreateDbContextAsync();
                db.Warehouses.Update(result);
                await db.SaveChangesAsync();
                
                var index = Warehouses.IndexOf(SelectedWarehouse);
                if (index >= 0) Warehouses[index] = result;
                SelectedWarehouse = result;
            }
        }
    }

    private async Task DeleteWarehouseAsync()
    {
        if (SelectedWarehouse == null) return;

        // Prevent deleting the system-created Main Warehouse
        if (SelectedWarehouse.IsMainWarehouse)
        {
            return;
        }

        var dialog = new Views.ConfirmDeleteWindow();
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var confirmed = await dialog.ShowDialog<bool>(desktop.MainWindow!);
            if (confirmed)
            {
                IsBusy = true;
                try
                {
                    using var db = await _dbFactory.CreateDbContextAsync();
                    
                    // Check if ANY related data exists in Stocks or StockTransactions (even with 0 quantity)
                    // because SQLite foreign keys will prevent deletion if records exist.
                    var hasStockRecords = await db.Stocks.AnyAsync(s => s.WarehouseID == SelectedWarehouse.WarehouseID);
                    var hasTxRecords = await db.StockTransactions.AnyAsync(t => t.WarehouseID == SelectedWarehouse.WarehouseID || t.ToWarehouseID == SelectedWarehouse.WarehouseID);

                    if (hasStockRecords || hasTxRecords)
                    {
                        // Prevent crash by stopping here if dependencies exist
                        return;
                    }

                    db.Warehouses.Remove(SelectedWarehouse);
                    await db.SaveChangesAsync();
                    
                    // Refresh the entire list to update pagination counts correctly
                    await LoadWarehousesAsync();
                    SelectedWarehouse = null;
                }
                catch (Exception ex)
                {
                    // Log the error but don't crash the whole application
                    System.Diagnostics.Debug.WriteLine($"Warehouse Deletion Error: {ex.Message}");
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }
    }

    private async Task AdjustStockAsync(Stock? stock)
    {
        if (stock == null || SelectedWarehouse == null || stock.Product == null) return;

        var vm = new StockAdjustmentViewModel(_ledgerService, _businessId, stock.Product, SelectedWarehouse, stock.Quantity);
        var dialog = new Views.StockAdjustmentWindow { DataContext = vm };
        
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var result = await dialog.ShowDialog<bool>(desktop.MainWindow!);
            if (result)
            {
                await LoadWarehouseStockAsync(SelectedWarehouse.WarehouseID);
            }
        }
    }
}
