using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using BusinessSuite.BLL.Services;
using BusinessSuite.DAL.Data;
using BusinessSuite.DAL.Entities;
using BusinessSuite.DAL.Repositories;
using BusinessSuite.UI.Services;
using BusinessSuite.UI.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;

namespace BusinessSuite.UI.ViewModels;

public partial class CreditNoteFormViewModel : ObservableObject
{
    private readonly AppDbContext _context;
    private readonly CreditNoteRepository _creditNoteRepository;
    private readonly InvoiceRepository _invoiceRepository;
    private readonly ProductRepository _productRepository;
    private readonly AuditTrailService _auditTrailService;
    private readonly TaxCalculator _taxCalculator;
    private readonly Business _business;
    private readonly int? _creditNoteId;
    private readonly int? _invoiceId;
    private bool _suppressInvoiceSelectionHandler;
    private Invoice? _loadedInvoice;

    private static readonly List<decimal> TaxRatesList = new() { 0, 5, 12, 18, 28 };

    [ObservableProperty] private CreditNote creditNote = new();
    [ObservableProperty] private ObservableCollection<CreditNoteItemViewModel> items = new();
    [ObservableProperty] private ObservableCollection<Invoice> invoices = new();
    [ObservableProperty] private Invoice? selectedInvoice;
    [ObservableProperty] private int? selectedInvoiceId;
    [ObservableProperty] private bool hasSelectedInvoiceDetails;
    [ObservableProperty] private string sourceInvoiceNumber = string.Empty;
    [ObservableProperty] private string sourceInvoiceDate = string.Empty;
    [ObservableProperty] private string sourceCustomerName = string.Empty;
    [ObservableProperty] private string sourceCustomerGstin = string.Empty;
    [ObservableProperty] private string sourcePlaceOfSupply = string.Empty;
    [ObservableProperty] private string sourceInvoiceTotal = string.Empty;
    [ObservableProperty] private string sourcePaymentStatus = string.Empty;
    [ObservableProperty] private int sourceLineItemCount;
    [ObservableProperty] private ObservableCollection<Product> products = new();
    [ObservableProperty] private ObservableCollection<string> unitNames = new();
    [ObservableProperty] private decimal subtotal;
    [ObservableProperty] private decimal totalCGST;
    [ObservableProperty] private decimal totalSGST;
    [ObservableProperty] private decimal totalIGST;
    [ObservableProperty] private decimal grandTotal;
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private string? errorMessage;
    [ObservableProperty] private bool canEdit = true;

    public int BusinessId { get; }

    public string CreditNoteNumber
    {
        get => CreditNote.CreditNoteNumber;
        set { CreditNote.CreditNoteNumber = value; OnPropertyChanged(); }
    }

    public DateTime? CreditNoteDate
    {
        get => CreditNote.CreditNoteDate;
        set
        {
            if (value.HasValue)
            {
                CreditNote.CreditNoteDate = value.Value;
                OnPropertyChanged();
            }
        }
    }

    public string Status
    {
        get => CreditNote.Status;
        set { CreditNote.Status = value; OnPropertyChanged(); }
    }

    public string Reason
    {
        get => CreditNote.Reason;
        set { CreditNote.Reason = value; OnPropertyChanged(); }
    }

    public string? Notes
    {
        get => CreditNote.Notes;
        set { CreditNote.Notes = value; OnPropertyChanged(); }
    }

    public bool IsGstRegistered => _business.IsGSTRegistered;

    public event Action? RequestClose;

    public CreditNoteFormViewModel(int businessId, int? creditNoteId = null, int? invoiceId = null)
    {
        BusinessId = businessId;
        _context = new AppDbContext();
        _creditNoteId = creditNoteId;
        _invoiceId = invoiceId;
        _creditNoteRepository = new CreditNoteRepository(_context);
        _invoiceRepository = new InvoiceRepository(_context);
        _productRepository = new ProductRepository(_context);
        _auditTrailService = new AuditTrailService(_context);
        _taxCalculator = new TaxCalculator();
        _business = _context.Businesses.FirstOrDefault(b => b.BusinessID == businessId) ?? new Business { BusinessID = businessId };

        _ = InitializeAsync();
    }

    partial void OnSelectedInvoiceIdChanged(int? value)
    {
        if (_suppressInvoiceSelectionHandler || !value.HasValue || _creditNoteId.HasValue)
            return;

        _ = ApplySelectedInvoiceAsync(value.Value);
    }

    private async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            var productList = await _productRepository.GetAllAsync(BusinessId);
            Products = new ObservableCollection<Product>(productList);

            var units = await _context.UnitsOfMeasure.OrderBy(u => u.Name).ToListAsync();
            UnitNames = new ObservableCollection<string>(units.Select(u => u.Name));

            var finalized = await _invoiceRepository.GetAllAsync(BusinessId);
            Invoices = new ObservableCollection<Invoice>(
                finalized.Where(i => !i.IsDraft).OrderByDescending(i => i.InvoiceDate));

            if (_creditNoteId.HasValue)
                await LoadExistingAsync(_creditNoteId.Value);
            else if (_invoiceId.HasValue)
                await StartNewForInvoiceAsync(_invoiceId.Value);
            else
                await StartBlankDraftAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading form: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task StartBlankDraftAsync()
    {
        CreditNote = new CreditNote
        {
            BusinessID = BusinessId,
            CreditNoteDate = DateTime.Now,
            Status = "Draft",
            IsDraft = true,
            CreatedByUserID = AppState.Instance.GetCurrentUserId()
        };
        Items.Clear();
        CanEdit = true;
        OnPropertyChanged(nameof(CreditNoteNumber));
        OnPropertyChanged(nameof(CreditNoteDate));
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(Reason));
        OnPropertyChanged(nameof(Notes));
        await Task.CompletedTask;
    }

    private Task StartNewForInvoiceAsync(int invoiceId) => ApplySelectedInvoiceAsync(invoiceId);

    private async Task ApplySelectedInvoiceAsync(int invoiceId)
    {
        IsLoading = true;
        try
        {
            var invoice = await _invoiceRepository.GetByIdAsync(invoiceId);
            if (invoice == null)
            {
                ErrorMessage = "Invoice not found";
                return;
            }

            if (invoice.IsDraft)
            {
                ErrorMessage = "Credit notes can only be created against finalized invoices";
                return;
            }

            _loadedInvoice = invoice;

            _suppressInvoiceSelectionHandler = true;
            try
            {
                SelectedInvoiceId = invoiceId;
                SelectedInvoice = invoice;
            }
            finally
            {
                _suppressInvoiceSelectionHandler = false;
            }

            CreditNote = new CreditNote
            {
                BusinessID = BusinessId,
                OriginalInvoiceID = invoiceId,
                CreditNoteDate = DateTime.Now,
                Status = "Draft",
                IsDraft = true,
                CreatedByUserID = AppState.Instance.GetCurrentUserId()
            };

            CreditNote.CreditNoteNumber = await _creditNoteRepository.GetNextCreditNoteNumberAsync(BusinessId, invoiceId);

            AmendmentInvoiceAutoFill.PopulateCreditItems(
                invoice, Items, Products, UnitNames.ToList(), TaxRatesList,
                _business, IsGstRegistered, vm => vm.PropertyChanged += Item_PropertyChanged);

            UpdateInvoiceSummary(invoice);
            CanEdit = true;
            ErrorMessage = null;
            NotifyHeaderProperties();
            RecalculateTotals();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading invoice: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void UpdateInvoiceSummary(Invoice? invoice)
    {
        var target = new AmendmentInvoiceSummaryTarget();
        AmendmentInvoiceAutoFill.ApplySummary(invoice, target);
        HasSelectedInvoiceDetails = target.HasDetails;
        SourceInvoiceNumber = target.InvoiceNumber;
        SourceInvoiceDate = target.InvoiceDate;
        SourceCustomerName = target.CustomerName;
        SourceCustomerGstin = target.CustomerGstin;
        SourcePlaceOfSupply = target.PlaceOfSupply;
        SourceInvoiceTotal = target.InvoiceTotal;
        SourcePaymentStatus = target.PaymentStatus;
        SourceLineItemCount = target.LineItemCount;
    }

    private async Task LoadExistingAsync(int creditNoteId)
    {
        var note = await _creditNoteRepository.GetByIdAsync(creditNoteId);
        if (note == null)
        {
            ErrorMessage = "Credit note not found";
            return;
        }

        CreditNote = note;

        var invoice = await _invoiceRepository.GetByIdAsync(note.OriginalInvoiceID);
        _loadedInvoice = invoice;
        _suppressInvoiceSelectionHandler = true;
        try
        {
            SelectedInvoiceId = note.OriginalInvoiceID;
            SelectedInvoice = invoice ?? note.OriginalInvoice;
        }
        finally
        {
            _suppressInvoiceSelectionHandler = false;
        }

        UpdateInvoiceSummary(invoice);

        Items.Clear();
        foreach (var item in note.CreditNoteItems)
        {
            var vm = CreateItemViewModel(item);
            vm.PropertyChanged += Item_PropertyChanged;
            Items.Add(vm);
        }

        CanEdit = note.IsDraft;
        NotifyHeaderProperties();
        RecalculateTotals();
    }

    private CreditNoteItemViewModel CreateItemViewModel(CreditNoteItem item)
    {
        var vm = new CreditNoteItemViewModel(Products, UnitNames.ToList(), TaxRatesList)
        {
            CreditNoteItemId = item.CreditNoteItemID,
            OriginalInvoiceItemId = item.OriginalInvoiceItemID,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            HsnCode = item.HSNCode,
            Unit = item.UnitOfMeasure,
            ItemType = item.ItemType,
            TaxRate = item.IGST_Rate > 0 ? item.IGST_Rate : item.CGST_Rate + item.SGST_Rate,
            TaxAmount = item.TotalTax,
            CgstAmount = item.CGST_Amount,
            SgstAmount = item.SGST_Amount,
            IgstAmount = item.IGST_Amount,
            TotalAmount = item.GrandTotal
        };

        var product = Products.FirstOrDefault(p => p.ProductID == item.ProductID);
        if (product != null)
            vm.SelectedProduct = product;

        return vm;
    }

    private string? GetCustomerStateForTax()
    {
        var invoice = _loadedInvoice ?? SelectedInvoice;
        return invoice?.Customer?.State ?? invoice?.PlaceOfSupply;
    }

    private void NotifyHeaderProperties()
    {
        OnPropertyChanged(nameof(CreditNoteNumber));
        OnPropertyChanged(nameof(CreditNoteDate));
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(Reason));
        OnPropertyChanged(nameof(Notes));
    }

    [RelayCommand]
    private void AddItem()
    {
        if (!CanEdit) return;
        var item = new CreditNoteItemViewModel(Products, UnitNames.ToList(), TaxRatesList);
        item.PropertyChanged += Item_PropertyChanged;
        Items.Add(item);
    }

    [RelayCommand]
    private void RemoveItem(CreditNoteItemViewModel? item)
    {
        if (!CanEdit || item == null) return;
        item.PropertyChanged -= Item_PropertyChanged;
        Items.Remove(item);
        RecalculateTotals();
    }

    [RelayCommand]
    private async Task AddProductAsync()
    {
        var vm = new ProductFormViewModel(BusinessId);
        var win = new ProductFormWindow { DataContext = vm };
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var result = await win.ShowDialog<Product?>(desktop.MainWindow!);
            if (result != null && await _productRepository.AddAsync(result))
                Products.Add(result);
        }
    }

    private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is CreditNoteItemViewModel item)
            item.RecalculateLine(_business, GetCustomerStateForTax(), IsGstRegistered);

        if (e.PropertyName is nameof(CreditNoteItemViewModel.TotalAmount)
            or nameof(CreditNoteItemViewModel.TaxAmount)
            or nameof(CreditNoteItemViewModel.SelectedProduct)
            or nameof(CreditNoteItemViewModel.Quantity)
            or nameof(CreditNoteItemViewModel.UnitPrice)
            or nameof(CreditNoteItemViewModel.TaxRate))
        {
            RecalculateTotals();
        }
    }

    public void RecalculateTotals()
    {
        foreach (var item in Items)
            item.RecalculateLine(_business, GetCustomerStateForTax(), IsGstRegistered);

        Subtotal = Items.Sum(i => i.LineTotal);
        TotalCGST = Items.Sum(i => i.CgstAmount);
        TotalSGST = Items.Sum(i => i.SgstAmount);
        TotalIGST = Items.Sum(i => i.IgstAmount);
        GrandTotal = Items.Sum(i => i.TotalAmount);

        CreditNote.SubTotal = Subtotal;
        CreditNote.TotalCGST = TotalCGST;
        CreditNote.TotalSGST = TotalSGST;
        CreditNote.TotalIGST = TotalIGST;
        CreditNote.GrandTotal = GrandTotal;
    }

    private List<CreditNoteItem> BuildEntityItems()
    {
        return Items
            .Where(i => i.SelectedProduct != null)
            .Select(i => new CreditNoteItem
            {
                CreditNoteItemID = i.CreditNoteItemId,
                CreditNoteID = CreditNote.CreditNoteID,
                OriginalInvoiceItemID = ResolveOriginalInvoiceItemId(i),
                ProductID = i.SelectedProduct!.ProductID,
                ItemType = i.ItemType,
                HSNCode = i.HsnCode ?? string.Empty,
                Description = i.SelectedProduct.ProductName,
                Quantity = i.Quantity,
                UnitOfMeasure = i.Unit ?? "nos",
                UnitPrice = i.UnitPrice,
                LineTotal = i.LineTotal,
                CGST_Rate = i.IgstAmount > 0 ? 0 : i.TaxRate / 2,
                CGST_Amount = i.CgstAmount,
                SGST_Rate = i.IgstAmount > 0 ? 0 : i.TaxRate / 2,
                SGST_Amount = i.SgstAmount,
                IGST_Rate = i.IgstAmount > 0 ? i.TaxRate : 0,
                IGST_Amount = i.IgstAmount,
                TotalTax = i.TaxAmount,
                GrandTotal = i.TotalAmount
            })
            .ToList();
    }

    private int ResolveOriginalInvoiceItemId(CreditNoteItemViewModel item)
    {
        if (item.OriginalInvoiceItemId > 0)
            return item.OriginalInvoiceItemId;

        var invoice = _loadedInvoice ?? SelectedInvoice;
        if (invoice?.Items == null || item.SelectedProduct == null)
            return 0;

        return invoice.Items
            .FirstOrDefault(ii => ii.ProductID == item.SelectedProduct.ProductID)?
            .InvoiceItemID ?? 0;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        IsLoading = true;
        try
        {
            var invoice = _loadedInvoice ?? SelectedInvoice;
            if (invoice == null)
            {
                ErrorMessage = "Please select an original invoice";
                return;
            }

            if (string.IsNullOrWhiteSpace(Reason))
            {
                ErrorMessage = "Please enter a reason for the credit note";
                return;
            }

            if (!Items.Any(i => i.SelectedProduct != null))
            {
                ErrorMessage = "Please add at least one item with a product";
                return;
            }

            RecalculateTotals();

            if (invoice.Items == null || !invoice.Items.Any())
            {
                var fullInvoice = await _invoiceRepository.GetByIdAsync(invoice.InvoiceID);
                if (fullInvoice != null)
                {
                    invoice = fullInvoice;
                    _loadedInvoice = fullInvoice;
                }
            }

            CreditNote.OriginalInvoiceID = invoice.InvoiceID;
            CreditNote.CreditNoteItems = BuildEntityItems();

            bool result;
            if (CreditNote.CreditNoteID == 0)
            {
                result = await _creditNoteRepository.AddAsync(CreditNote);
                if (result)
                {
                    await _auditTrailService.LogCreatedAsync(
                        BusinessId, "CreditNote", CreditNote.CreditNoteID,
                        CreditNote.CreatedByUserID, $"Credit note created: {CreditNote.Reason}");
                    NotifyHeaderProperties();
                }
            }
            else
            {
                result = await _creditNoteRepository.UpdateAsync(CreditNote);
                if (result)
                {
                    await _auditTrailService.LogFieldModifiedAsync(
                        BusinessId, "CreditNote", CreditNote.CreditNoteID, "All",
                        null, null, AppState.Instance.GetCurrentUserId(), "Credit note updated");
                }
            }

            ErrorMessage = result ? null : "Failed to save credit note";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error saving credit note: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task FinalizeAsync()
    {
        if (CreditNote.CreditNoteID == 0)
        {
            ErrorMessage = "Please save the credit note first";
            return;
        }

        IsLoading = true;
        try
        {
            var result = await _creditNoteRepository.FinalizeAsync(
                CreditNote.CreditNoteID, AppState.Instance.GetCurrentUserId());

            if (result)
            {
                CreditNote.Status = "Finalized";
                CreditNote.IsDraft = false;
                CanEdit = false;
                NotifyHeaderProperties();
                await _auditTrailService.LogFinalizedAsync(
                    BusinessId, "CreditNote", CreditNote.CreditNoteID, AppState.Instance.GetCurrentUserId());
                ErrorMessage = null;
            }
            else
            {
                ErrorMessage = "Failed to finalize credit note";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error finalizing credit note: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task VoidNoteAsync()
    {
        if (CreditNote.CreditNoteID == 0)
        {
            ErrorMessage = "Please save the credit note first";
            return;
        }

        IsLoading = true;
        try
        {
            var result = await _creditNoteRepository.CancelAsync(
                CreditNote.CreditNoteID, "Cancelled by user", AppState.Instance.GetCurrentUserId());

            if (result)
            {
                CreditNote.Status = "Cancelled";
                CanEdit = false;
                NotifyHeaderProperties();
                await _auditTrailService.LogCancelledAsync(
                    BusinessId, "CreditNote", CreditNote.CreditNoteID,
                    AppState.Instance.GetCurrentUserId(), "Credit note cancelled");
                ErrorMessage = null;
            }
            else
            {
                ErrorMessage = "Failed to cancel credit note";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error cancelling credit note: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void Close()
    {
        RequestClose?.Invoke();
    }
}

public partial class CreditNoteItemViewModel : ObservableObject
{
    private readonly ObservableCollection<Product> _allProducts;

    [ObservableProperty] private ObservableCollection<Product> filteredProducts;
    [ObservableProperty] private string searchQuery = string.Empty;
    [ObservableProperty] private bool isDropDownOpen;
    [ObservableProperty] private int creditNoteItemId;
    [ObservableProperty] private int originalInvoiceItemId;
    [ObservableProperty] private Product? selectedProduct;
    [ObservableProperty] private decimal quantity = 1;
    [ObservableProperty] private decimal unitPrice;
    [ObservableProperty] private decimal taxRate;
    [ObservableProperty] private decimal taxAmount;
    [ObservableProperty] private decimal cgstAmount;
    [ObservableProperty] private decimal sgstAmount;
    [ObservableProperty] private decimal igstAmount;
    [ObservableProperty] private decimal totalAmount;
    [ObservableProperty] private decimal lineTotal;
    [ObservableProperty] private string? hsnCode;
    [ObservableProperty] private string itemType = "Product";
    [ObservableProperty] private ObservableCollection<decimal> taxRates;

    private string? _unit;
    public string? Unit
    {
        get => _unit;
        set => SetProperty(ref _unit, value);
    }

    public CreditNoteItemViewModel(
        ObservableCollection<Product> products,
        List<string> unitNames,
        List<decimal> taxRatesList)
    {
        _allProducts = products;
        FilteredProducts = new ObservableCollection<Product>(products);
        TaxRates = new ObservableCollection<decimal>(taxRatesList);
        _allProducts.CollectionChanged += (_, _) =>
            FilteredProducts = new ObservableCollection<Product>(_allProducts);
    }

    public void RecalculateLine(Business business, string? customerState, bool isGstRegistered)
    {
        if (SelectedProduct == null)
        {
            LineTotal = 0;
            TaxAmount = 0;
            CgstAmount = 0;
            SgstAmount = 0;
            IgstAmount = 0;
            TotalAmount = 0;
            return;
        }

        LineTotal = Math.Round(Quantity * UnitPrice, 2);
        if (!isGstRegistered)
        {
            TaxAmount = 0;
            CgstAmount = 0;
            SgstAmount = 0;
            IgstAmount = 0;
            TotalAmount = LineTotal;
            return;
        }

        var tax = new TaxCalculator().CalculateTax(
            LineTotal, TaxRate, business.State, customerState);

        TaxAmount = tax.TotalTaxAmount;
        CgstAmount = tax.CGST;
        SgstAmount = tax.SGST;
        IgstAmount = tax.IGST;
        TotalAmount = LineTotal + TaxAmount;
    }

    partial void OnSelectedProductChanged(Product? value)
    {
        if (value == null) return;
        UnitPrice = value.SalePrice;
        TaxRate = value.TaxRate;
        HsnCode = value.HSNCode;
        Unit = value.Unit;
        ItemType = value.IsService ? "Service" : "Product";
        SearchQuery = value.ProductName;
        IsDropDownOpen = false;
    }

    partial void OnSearchQueryChanged(string value)
    {
        var query = value?.ToLower() ?? string.Empty;
        var list = string.IsNullOrWhiteSpace(query)
            ? _allProducts.ToList()
            : _allProducts.Where(p =>
                p.ProductName.ToLower().Contains(query) ||
                (p.SKU?.ToLower().Contains(query) ?? false)).ToList();

        if (SelectedProduct != null && !list.Contains(SelectedProduct))
            list.Insert(0, SelectedProduct);

        FilteredProducts = new ObservableCollection<Product>(list);
        if (!string.IsNullOrWhiteSpace(query) && SelectedProduct == null)
            IsDropDownOpen = true;
    }
}
