using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using System.Linq;
using System.Collections.ObjectModel;
using BusinessSuite.DAL.Data;
using BusinessSuite.DAL.Entities;
using BusinessSuite.UI.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BusinessSuite.BLL.StaticData;
using BusinessSuite.BLL.Services;
using Microsoft.EntityFrameworkCore;

namespace BusinessSuite.UI.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    public System.Collections.Generic.IEnumerable<string> States => LocationData.IndianStates;
    private readonly int _businessId;

    [ObservableProperty]
    private object? _currentView;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentUserInitials))]
    private string _currentUserName = "User";

    public string CurrentUserInitials
    {
        get
        {
            if (string.IsNullOrWhiteSpace(CurrentUserName))
                return "U";

            var parts = CurrentUserName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
                return parts[0][0].ToString().ToUpper();

            return (parts[0][0].ToString() + parts[^1][0].ToString()).ToUpper();
        }
    }

    [ObservableProperty]
    private string _businessName = "Smart Business Suite";

    [ObservableProperty]
    private string _currentUserType = "User";

    [ObservableProperty]
    private bool _isGstRegistered;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDashboardActive))]
    [NotifyPropertyChangedFor(nameof(IsProductsActive))]
    [NotifyPropertyChangedFor(nameof(IsWarehousesActive))]
    [NotifyPropertyChangedFor(nameof(IsCustomersActive))]
    [NotifyPropertyChangedFor(nameof(IsVendorsActive))]
    [NotifyPropertyChangedFor(nameof(IsSalesActive))]
    [NotifyPropertyChangedFor(nameof(IsPurchasesActive))]
    [NotifyPropertyChangedFor(nameof(IsReportsActive))]
    [NotifyPropertyChangedFor(nameof(IsFinanceLedgerActive))]
    [NotifyPropertyChangedFor(nameof(IsStockLedgerActive))]
    [NotifyPropertyChangedFor(nameof(IsSettingsActive))]
    private string _currentViewTitle = "Dashboard";

    public bool IsDashboardActive => CurrentViewTitle == "Dashboard";
    public bool IsProductsActive => CurrentViewTitle == "Products";
    public bool IsWarehousesActive => CurrentViewTitle == "Warehouses";
    public bool IsCustomersActive => CurrentViewTitle == "Customers";
    public bool IsVendorsActive => CurrentViewTitle == "Vendors";
    public bool IsSalesActive => CurrentViewTitle == "Sales";
    public bool IsPurchasesActive => CurrentViewTitle == "Purchases";
    public bool IsReportsActive => CurrentViewTitle == "Reports";
    public bool IsFinanceLedgerActive => CurrentViewTitle == "FinanceLedger";
    public bool IsStockLedgerActive => CurrentViewTitle == "StockLedger";
    public bool IsSettingsActive => CurrentViewTitle == "Settings";

    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public DashboardViewModel(BusinessSuite.DAL.Entities.User user, IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
        CurrentUserName = user.FullName ?? user.UserName;
        CurrentUserType = user.Designation?.ToString() ?? "User";
        
        using var db = _dbFactory.CreateDbContext();
        var business = db.Businesses.FirstOrDefault();
        if (business != null)
        {
            BusinessName = business.BusinessName;
            _businessId = business.BusinessID;
            IsGstRegistered = business.IsGSTRegistered;
        }

        Navigate("Dashboard");
    }

    public DashboardViewModel()
    {
        _dbFactory = new BusinessSuite.DAL.Data.AppDbContextFactory();
        // Parameterless constructor for designer support if needed
        CurrentUserName = "Admin";
        BusinessName = "My Tech Store";
        CurrentUserType = "Admin";
        IsGstRegistered = true;
        NavigateInternal("Dashboard");
    }

    [RelayCommand]
    private void Navigate(object? parameter)
    {
        if (parameter is string target)
        {
            NavigateInternal(target);
        }
    }

    private void NavigateInternal(string target, object? parameter = null)
    {
        CurrentViewTitle = target;
        
        switch (target)
        {
            case "Dashboard":
                var homeVm = new HomeViewModel(_businessId);
                homeVm.RequestNavigation += (target) => NavigateInternal(target);
                CurrentView = homeVm;
                _ = homeVm.LoadDashboardDataCommand.ExecuteAsync(null);
                break;
            case "Products":
                var ledgerServiceP = new LedgerService(_dbFactory);
                var productsVm = new ProductsViewModel(_businessId, ledgerServiceP);
                CurrentView = productsVm;
                _ = productsVm.LoadProductsCommand.ExecuteAsync(null);
                break;
            case "Warehouses":
                var warehousesVm = new WarehousesViewModel(_dbFactory, _businessId);
                CurrentView = warehousesVm;
                _ = warehousesVm.LoadWarehousesCommand.ExecuteAsync(null);
                break;
            case "StockLedger":
                var stockLedgerVm = new StockLedgerViewModel(_dbFactory, _businessId);
                CurrentView = stockLedgerVm;
                _ = stockLedgerVm.LoadDataCommand.ExecuteAsync(null);
                break;
            case "Customers":
                var ledgerService = new LedgerService(_dbFactory);
                var customersVm = new CustomersViewModel(_businessId, ledgerService);
                CurrentView = customersVm;
                _ = customersVm.LoadCustomersCommand.ExecuteAsync(null);
                break;
            case "Vendors":
                var ledgerServiceV = new LedgerService(_dbFactory);
                var vendorsVm = new VendorsViewModel(_businessId, ledgerServiceV);
                CurrentView = vendorsVm;
                _ = vendorsVm.LoadVendorsCommand.ExecuteAsync(null);
                break;
            case "FinanceLedger":
                var financeLedgerVm = new FinanceLedgerViewModel(_dbFactory, _businessId);
                CurrentView = financeLedgerVm;
                _ = financeLedgerVm.LoadDataCommand.ExecuteAsync(null);
                break;
            case "Sales":
                var ledgerServiceS = new LedgerService(_dbFactory);
                var invoicesVm = new InvoicesViewModel(_businessId, ledgerServiceS);
                invoicesVm.RequestInvoiceForm += (invoice) => NavigateInternal("InvoiceForm", invoice);
                CurrentView = invoicesVm;
                _ = invoicesVm.LoadInvoicesCommand.ExecuteAsync(null);
                break;
            case "InvoiceForm":
                var invoiceFormVm = new InvoiceFormViewModel(_businessId, parameter as Invoice);
                invoiceFormVm.RequestClose += (result) => NavigateInternal("Sales");
                CurrentView = invoiceFormVm;
                break;
            case "Purchases":
                var ledgerServiceP2 = new LedgerService(_dbFactory);
                var posVm = new PurchaseOrdersViewModel(_businessId, ledgerServiceP2);
                posVm.RequestPOForm += (po) => NavigateInternal("PurchaseOrderForm", po);
                CurrentView = posVm;
                _ = posVm.LoadPOsCommand.ExecuteAsync(null);
                break;
            case "PurchaseOrderForm":
                var poFormVm = new PurchaseOrderFormViewModel(_businessId, parameter as PurchaseOrder);
                poFormVm.RequestClose += (result) => NavigateInternal("Purchases");
                CurrentView = poFormVm;
                break;
            case "Settings":
                CurrentView = new SettingsViewModel(_businessId);
                break;
            case "Reports":
                var reportsVm = new ReportsViewModel(_businessId);
                CurrentView = reportsVm;
                _ = reportsVm.LoadReportsCommand.ExecuteAsync(null);
                break;
            case "ProductForm":
                OpenProductForm();
                break;
        }
    }

    private async void OpenProductForm()
    {
        var vm = new ProductFormViewModel(_businessId);
        var win = new ProductFormWindow { DataContext = vm };
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            await win.ShowDialog<Product?>(desktop.MainWindow!);
            // We don't necessarily need to navigate away, just refresh if we are on Products view
            if (CurrentView is ProductsViewModel pvm)
            {
                _ = pvm.LoadProductsCommand.ExecuteAsync(null);
            }
        }
    }

    [RelayCommand]
    private void Logout()
    {
        RequestLogout?.Invoke();
    }

    [RelayCommand]
    private void Exit()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    [RelayCommand]
    private async Task About()
    {
        var win = new AboutView();
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            await win.ShowDialog(desktop.MainWindow!);
        }
    }

    public event Action? RequestLogout;
}
