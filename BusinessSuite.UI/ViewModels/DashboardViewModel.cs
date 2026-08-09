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
using BusinessSuite.UI.Services;
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
    [NotifyPropertyChangedFor(nameof(IsQuotationsActive))]
    [NotifyPropertyChangedFor(nameof(IsCreditNotesActive))]
    [NotifyPropertyChangedFor(nameof(IsDebitNotesActive))]
    [NotifyPropertyChangedFor(nameof(IsBillOfMaterialsActive))]
    [NotifyPropertyChangedFor(nameof(IsProductionOrdersActive))]
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
    public bool IsQuotationsActive => CurrentViewTitle == "Quotations";
    public bool IsCreditNotesActive => CurrentViewTitle == "CreditNotes";
    public bool IsDebitNotesActive => CurrentViewTitle == "DebitNotes";
    public bool IsBillOfMaterialsActive => CurrentViewTitle == "BillOfMaterials";
    public bool IsProductionOrdersActive => CurrentViewTitle == "ProductionOrders";
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
            
            // Initialize AppState with current user and business
            AppState.Instance.Initialize(user, business.BusinessID);
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
                invoicesVm.RequestCreditNoteForm += (args) => NavigateInternal("CreditNoteForm", args);
                invoicesVm.RequestDebitNoteForm += (args) => NavigateInternal("DebitNoteForm", args);
                CurrentView = invoicesVm;
                _ = invoicesVm.LoadInvoicesCommand.ExecuteAsync(null);
                break;
            case "Quotations":
                var quotationsVm = new QuotationsViewModel(_businessId);
                quotationsVm.RequestQuotationForm += (quotation) => NavigateInternal("QuotationForm", quotation);
                quotationsVm.RequestInvoiceForm += (invoice) => NavigateInternal("InvoiceForm", invoice);
                CurrentView = quotationsVm;
                _ = quotationsVm.LoadQuotationsCommand.ExecuteAsync(null);
                break;
            case "CreditNotes":
                var creditNotesVm = new CreditNotesViewModel(_businessId);
                creditNotesVm.RequestCreditNoteForm += (args) => NavigateInternal("CreditNoteForm", args);
                CurrentView = creditNotesVm;
                _ = creditNotesVm.LoadCommand.ExecuteAsync(null);
                break;
            case "DebitNotes":
                var debitNotesVm = new DebitNotesViewModel(_businessId);
                debitNotesVm.RequestDebitNoteForm += (args) => NavigateInternal("DebitNoteForm", args);
                CurrentView = debitNotesVm;
                _ = debitNotesVm.LoadCommand.ExecuteAsync(null);
                break;
            case "CreditNoteForm":
            {
                var cnArgs = parameter as AmendmentFormArgs;
                var cnFormVm = new CreditNoteFormViewModel(_businessId, cnArgs?.NoteId, cnArgs?.InvoiceId);
                cnFormVm.RequestClose += () => NavigateInternal(cnArgs?.ReturnTo ?? "CreditNotes");
                CurrentView = cnFormVm;
                CurrentViewTitle = cnArgs?.NoteId.HasValue == true ? "Edit Credit Note" : "Credit Note";
                break;
            }
            case "DebitNoteForm":
            {
                var dnArgs = parameter as AmendmentFormArgs;
                var dnFormVm = new DebitNoteFormViewModel(_businessId, dnArgs?.NoteId, dnArgs?.InvoiceId);
                dnFormVm.RequestClose += () => NavigateInternal(dnArgs?.ReturnTo ?? "DebitNotes");
                CurrentView = dnFormVm;
                CurrentViewTitle = dnArgs?.NoteId.HasValue == true ? "Edit Debit Note" : "Debit Note";
                break;
            }
            case "InvoiceForm":
                var invoiceFormVm = new InvoiceFormViewModel(_businessId, parameter as Invoice);
                invoiceFormVm.RequestClose += (result) => NavigateInternal("Sales");
                CurrentView = invoiceFormVm;
                break;
            case "QuotationForm":
                var quotationFormVm = new QuotationFormViewModel(_businessId, parameter as Quotation);
                quotationFormVm.RequestClose += (result) => NavigateInternal("Quotations");
                CurrentView = quotationFormVm;
                break;
            case "BillOfMaterials":
                var bomVm = new BillOfMaterialsViewModel(_businessId);
                CurrentView = bomVm;
                _ = bomVm.LoadCommand.ExecuteAsync(null);
                break;
            case "ProductionOrders":
                var prodOrdersVm = new ProductionOrdersViewModel(_businessId);
                prodOrdersVm.RequestProductionOrderForm += (order) => NavigateInternal("ProductionOrderForm", order);
                CurrentView = prodOrdersVm;
                _ = prodOrdersVm.LoadCommand.ExecuteAsync(null);
                break;
            case "ProductionOrderForm":
            {
                var mfgService = new ManufacturingService(_dbFactory);
                var existingOrder = parameter as ProductionOrder;
                var prodFormVm = new ProductionOrderFormViewModel(
                    _businessId, mfgService, existingOrder?.ProductionOrderID);
                prodFormVm.RequestClose += () => NavigateInternal("ProductionOrders");
                CurrentView = prodFormVm;
                CurrentViewTitle = existingOrder != null
                    ? $"Production - {existingOrder.ProductionOrderNumber}"
                    : "New Production Order";
                break;
            }
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
