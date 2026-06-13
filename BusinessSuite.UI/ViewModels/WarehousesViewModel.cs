using System;
using System.Collections.Generic;
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
    private readonly EntityDeletionService _deletionService;
    private readonly int _businessId;

    [ObservableProperty] private ObservableCollection<Warehouse> _warehouses = new();
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditWarehouseCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteWarehouseCommand))]
    private Warehouse? _selectedWarehouse;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteWarehouseCommand))]
    private ObservableCollection<Warehouse> _selectedWarehouses = new();

    [ObservableProperty] private ObservableCollection<Stock> _warehouseStock = new();
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private decimal _totalStockValue;
    
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
        _deletionService = new EntityDeletionService(dbFactory);
        _businessId = businessId;

        LoadWarehousesCommand = new AsyncRelayCommand(LoadWarehousesAsync);
        AddWarehouseCommand = new AsyncRelayCommand(AddWarehouseAsync);
        EditWarehouseCommand = new AsyncRelayCommand(EditWarehouseAsync, () => SelectedWarehouse != null);
        DeleteWarehouseCommand = new AsyncRelayCommand(DeleteWarehouseAsync);
        AdjustStockCommand = new AsyncRelayCommand<Stock>(AdjustStockAsync);
        NextPageCommand = new AsyncRelayCommand(NextPageAsync);
        PreviousPageCommand = new AsyncRelayCommand(PreviousPageAsync);
    }

    private bool CanDeleteWarehouses() => SelectedWarehouses.Count > 0;

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
            .Where(s => s.WarehouseID == warehouseId && (s.Product == null || !s.Product.IsService))
            .ToListAsync();
        
        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => {
            WarehouseStock = new ObservableCollection<Stock>(stock);
            // Compute total value: Quantity × SalePrice
            TotalStockValue = stock
                .Where(s => s.Product != null)
                .Sum(s => s.Quantity * (s.Product!.SalePrice));
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
        var selectedWarehouses = SelectedWarehouses.ToList();
        if (selectedWarehouses.Count == 0 && SelectedWarehouse != null)
            selectedWarehouses.Add(SelectedWarehouse);

        if (selectedWarehouses.Count == 0)
        {
            SetStatusMessage("Select one or more warehouses to delete.", "#B45309");
            return;
        }

        ClearStatusMessage();
        int count = selectedWarehouses.Count;
        string confirmMsg = count == 1 ? "Are you sure you want to delete this warehouse?" : $"Are you sure you want to delete {count} warehouses?";
        bool confirmed = await ShowConfirmDeleteDialog(confirmMsg);
        if (!confirmed) return;
        
        IsBusy = true;
        try
        {
            int successCount = 0;
            int failCount = 0;
            string lastError = string.Empty;
            
            foreach (var warehouse in selectedWarehouses)
            {
                var (success, message) = await _deletionService.DeleteWarehouseAsync(warehouse.WarehouseID);
                if (success)
                {
                    successCount++;
                    Warehouses.Remove(warehouse);
                }
                else
                {
                    failCount++;
                    lastError = message;
                }
            }
            
            SelectedWarehouse = null;
            
            if (successCount > 0 && failCount == 0)
            {
                SetStatusMessage($"{successCount} warehouse(s) deleted successfully.", "#047857");
            }
            else if (successCount > 0 && failCount > 0)
            {
                SetStatusMessage($"{successCount} deleted, {failCount} failed: {lastError}", "#FB923C");
            }
            else
            {
                SetStatusMessage($"Failed to delete: {lastError}", "#B45309");
            }
        }
        finally
        {
            IsBusy = false;
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

    private async Task<bool> ShowConfirmDeleteDialog(string message = "Are you sure you want to delete the selected item?")
    {
        var dialog = new Views.ConfirmDeleteWindow(message);
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return await dialog.ShowDialog<bool>(desktop.MainWindow!);
        }
        return false;
    }
}
