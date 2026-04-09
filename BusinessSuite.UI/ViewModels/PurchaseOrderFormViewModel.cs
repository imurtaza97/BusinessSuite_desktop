using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using BusinessSuite.BLL.Services;
using BusinessSuite.DAL.Data;
using BusinessSuite.DAL.Entities;
using BusinessSuite.DAL.Repositories;
using BusinessSuite.UI.Views;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.EntityFrameworkCore;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BusinessSuite.UI.ViewModels;

public partial class PurchaseOrderFormViewModel : ViewModelBase, INotifyDataErrorInfo
{
    private readonly PurchaseOrderRepository _poRepository;
    private readonly VendorRepository _vendorRepository;
    private readonly ProductRepository _productRepository;
    private readonly WarehouseRepository _warehouseRepository;
    private readonly LedgerService _ledgerService;
    private readonly TaxCalculator _taxCalculator;
    private readonly Business _business;
    private readonly int _businessId;

    private readonly Dictionary<string, List<string>> _errors = new();
    private bool _ignoreSearchUpdate;

    public bool HasErrors => _errors.Any();
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public IEnumerable GetErrors(string? propertyName)
    {
        if (!ValidationVisible) return Enumerable.Empty<string>();
        if (string.IsNullOrEmpty(propertyName) || !_errors.ContainsKey(propertyName))
            return Enumerable.Empty<string>();
        return _errors[propertyName];
    }

    private void AddError(string propertyName, string error)
    {
        if (!_errors.ContainsKey(propertyName))
            _errors[propertyName] = new List<string>();
        _errors[propertyName].Add(error);
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }

    private void ClearAllErrors()
    {
        var propertiesWithErrors = _errors.Keys.ToList();
        _errors.Clear();
        foreach (var propertyName in propertiesWithErrors)
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }

    private void ValidateAll()
    {
        ClearAllErrors();
        if (SelectedVendor == null) AddError(nameof(SelectedVendor), "Vendor is required");
        if (SelectedWarehouse == null) AddError(nameof(SelectedWarehouse), "Warehouse is required");
        if (!Items.Any()) 
            AddError(nameof(Items), "At least one item is required");
        else
        {
            var validItems = Items.Where(i => i.SelectedProduct != null).ToList();
            if (!validItems.Any())
                AddError(nameof(Items), "At least one product must be selected");

            foreach (var item in Items)
            {
                if (item.SelectedProduct != null)
                {
                    if (item.Quantity <= 0) 
                        AddError(nameof(Items), $"Quantity for '{item.SelectedProduct.ProductName}' must be greater than zero");
                }
                else if (Items.Count > 1)
                {
                    AddError(nameof(Items), "Product is missing in one of the rows");
                }
            }
        }

        OnPropertyChanged(nameof(HasErrors));
    }

    [ObservableProperty] private bool _validationVisible = false;
    [ObservableProperty] private string _generalErrorMessage = string.Empty;
    [ObservableProperty] private string _title = "Create Purchase Order";
    [ObservableProperty] private string _poNumber = "";
    [ObservableProperty] private DateTime? _poDate = DateTime.Now;
    [ObservableProperty] private DateTime? _expectedDeliveryDate;
    [ObservableProperty] private bool _isAutoRoundOff = true;

    [ObservableProperty] private string? _paymentMethod = "Cash";
    [ObservableProperty] private string? _paymentTerms = "Due on Receipt";
    [ObservableProperty] private string? _termsAndConditions;
    [ObservableProperty] private string? _notes;
    [ObservableProperty] private decimal _shippingCharges;
    [ObservableProperty] private bool _reverseCharge;
    [ObservableProperty] private string _deliveryStatus = "Pending";
    [ObservableProperty] private string _paymentStatus = "Unpaid";
    [ObservableProperty] private decimal _totalPaid;

    partial void OnTotalPaidChanged(decimal value)
    {
        if (value <= 0) PaymentStatus = "Unpaid";
        else if (value < GrandTotal) PaymentStatus = "Partially Paid";
        else PaymentStatus = "Paid";
    }

    partial void OnPaymentStatusChanged(string value)
    {
        if (value == "Paid")
            TotalPaid = GrandTotal;
        else if (value == "Unpaid")
            TotalPaid = 0;
    }

    partial void OnGrandTotalChanged(decimal value)
    {
        if (PaymentStatus == "Paid")
            TotalPaid = value;
    }
    
    [ObservableProperty] private string? _vendorBillPath;
    [ObservableProperty] private string? _vendorBillFileName;

    private readonly int? _existingPOId;

    [ObservableProperty] private ObservableCollection<Vendor> _vendors = new();
    [ObservableProperty] private ObservableCollection<Vendor> _filteredVendors = new();
    [ObservableProperty] private string _vendorSearchQuery = string.Empty;
    [ObservableProperty] private bool _isVendorDropDownOpen;
    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(ShowGstFields))]
    private Vendor? _selectedVendor;

    [ObservableProperty] private ObservableCollection<Product> _products = new();
    [ObservableProperty] private ObservableCollection<UnitOfMeasure> _units = new();
    [ObservableProperty] private ObservableCollection<string> _unitNames = new();

    [ObservableProperty] private ObservableCollection<Warehouse> _warehouses = new();
    [ObservableProperty] private Warehouse? _selectedWarehouse;

    [ObservableProperty] private ObservableCollection<PurchaseOrderItemViewModel> _items = new();

    [ObservableProperty] private decimal _totalAmount;
    [ObservableProperty] private decimal _totalTax;
    [ObservableProperty] private decimal _totalCGST;
    [ObservableProperty] private decimal _totalSGST;
    [ObservableProperty] private decimal _totalIGST;
    [ObservableProperty] private decimal _discount;
    [ObservableProperty] private decimal _roundOff;
    [ObservableProperty] private decimal _grandTotal;
    [ObservableProperty] private string? _placeOfSupply;
    [ObservableProperty] private bool _isItemLevelDiscount;

    public bool IsGstRegistered => _business?.IsGSTRegistered ?? false;

    public bool ShowGstFields => SelectedVendor?.GstTreatment?.Equals("Regular", StringComparison.OrdinalIgnoreCase) ?? false;

    public PurchaseOrderFormViewModel(int businessId, PurchaseOrder? existingPO = null)
    {
        _businessId = businessId;
        var db = new AppDbContext();
        _poRepository = new PurchaseOrderRepository(db);
        _vendorRepository = new VendorRepository(db);
        _productRepository = new ProductRepository(db);
        _warehouseRepository = new WarehouseRepository(db);
        var dbFactory = new AppDbContextFactory();
        _ledgerService = new LedgerService(dbFactory);
        _taxCalculator = new TaxCalculator();

        _business = db.Businesses.FirstOrDefault(b => b.BusinessID == businessId) ?? new Business();

        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new RelayCommand(Cancel);
        AddItemCommand = new RelayCommand(AddItem);
        RemoveItemCommand = new RelayCommand<PurchaseOrderItemViewModel>(RemoveItem);
        AddProductCommand = new AsyncRelayCommand(AddProductAsync);
        AddVendorCommand = new AsyncRelayCommand(AddVendorAsync);

        if (existingPO != null)
        {
            _existingPOId = existingPO.PurchaseOrderID;
            Title = $"Edit Purchase Order - {existingPO.PONumber}";
            PoNumber = existingPO.PONumber;
            PoDate = existingPO.PODate;
            PaymentMethod = existingPO.PaymentMethod ?? "Cash";
            PaymentTerms = existingPO.PaymentTerms ?? "Due on Receipt";
            TermsAndConditions = existingPO.TermsAndConditions;
            Notes = existingPO.Notes;
            ShippingCharges = existingPO.ShippingCharges;
            Discount = existingPO.Discount;
            DeliveryStatus = existingPO.DeliveryStatus ?? "Pending";
            // Use default initialization for PaymentStatus and TotalPaid
            // They will be set in InitializeAsync after Wait for CalculateTotals

            PlaceOfSupply = existingPO.PlaceOfSupply ?? _business.State;
            RoundOff = existingPO.RoundOff;
            ExpectedDeliveryDate = existingPO.ExpectedDeliveryDate;
            IsAutoRoundOff = existingPO.IsAutoRoundOff;
            IsItemLevelDiscount = existingPO.IsItemLevelDiscount;
            VendorBillPath = existingPO.VendorBillPath;
            if (!string.IsNullOrEmpty(VendorBillPath))
                VendorBillFileName = System.IO.Path.GetFileName(VendorBillPath);
        }
        else
        {
            PlaceOfSupply = _business.State;
            ExpectedDeliveryDate = DateTime.Now.AddDays(7);
        }

        Task.Run(async () => await InitializeAsync(existingPO));
    }

    public IAsyncRelayCommand AddProductCommand { get; }

    public List<string> PaymentMethodsList { get; } = new() { "Cash", "Bank Transfer", "UPI/QR", "Card", "Cheque" };
    public List<string> DeliveryStatusOptions { get; } = new() { "Pending", "Received", "Returned-to-Vendor", "Cancelled" };
    public List<string> PaymentStatusOptions { get; } = new() { "Unpaid", "Paid", "Partially Paid" };
    public List<decimal> TaxRatesList { get; } = new() { 0, 5, 12, 18, 28 };

    public IAsyncRelayCommand AddVendorCommand { get; }

    private async Task AddVendorAsync()
    {
        var vm = new VendorFormViewModel(_businessId);
        var win = new VendorFormWindow { DataContext = vm };

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var result = await win.ShowDialog<Vendor?>(desktop.MainWindow!);
            if (result != null)
            {
                var success = await _vendorRepository.AddAsync(result);
                if (success)
                {
                    Vendors.Add(result);
                    FilteredVendors.Add(result);
                    SelectedVendor = result;
                }
            }
        }
    }

    private async Task AddProductAsync()
    {
        var vm = new ProductFormViewModel(_businessId);
        var win = new ProductFormWindow { DataContext = vm };

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var result = await win.ShowDialog<Product?>(desktop.MainWindow!);
            if (result != null)
            {
                var success = await _productRepository.AddAsync(result);
                if (success)
                {
                    Products.Add(result);
                }
            }
        }
    }

    private async Task InitializeAsync(PurchaseOrder? existing = null)
    {
        var nextNumber = "";
        if (existing == null)
        {
            var date = PoDate ?? DateTime.Now;
            nextNumber = await _poRepository.GetNextPONumberAsync(_businessId, date);
        }
        var vendorsList = await _vendorRepository.GetAllAsync(_businessId);
        var productsList = await _productRepository.GetAllAsync(_businessId);
        var warehousesList = await _warehouseRepository.GetAllAsync(_businessId);

        using var db = new AppDbContext();
        var unitList = await db.UnitsOfMeasure.Where(u => u.BusinessId == 0 || u.BusinessId == _businessId).ToListAsync();

        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (existing == null)
                PoNumber = nextNumber;

            Vendors.Clear();
            FilteredVendors.Clear();
            foreach (var v in vendorsList)
            {
                Vendors.Add(v);
                FilteredVendors.Add(v);
            }

            if (existing != null)
                SelectedVendor = Vendors.FirstOrDefault(v => v.VendorID == existing.VendorId);

            Products.Clear();
            foreach (var p in productsList) Products.Add(p);

            Units.Clear();
            UnitNames.Clear();
            foreach (var u in unitList)
            {
                Units.Add(u);
                UnitNames.Add(u.Name);
            }

            Warehouses.Clear();
            foreach (var w in warehousesList) Warehouses.Add(w);
            SelectedWarehouse = Warehouses.FirstOrDefault(w => w.IsMainWarehouse) ?? Warehouses.FirstOrDefault();

            if (existing != null)
            {
                Items.Clear();
                foreach (var item in existing.Items)
                {
                    var itemVm = new PurchaseOrderItemViewModel(Products, Units.ToList(), TaxRatesList);
                    itemVm.PropertyChanged += Item_PropertyChanged;
                    itemVm.PurchaseOrderItemId = item.PurchaseOrderItemID;
                    itemVm.SelectedProduct = Products.FirstOrDefault(p => p.ProductID == item.ProductID);
                    itemVm.Quantity = item.Quantity;
                    itemVm.UnitPrice = item.UnitPrice;
                    itemVm.TaxRate = item.TaxRate;
                    itemVm.Discount = item.Discount;
                    itemVm.Unit = item.Unit;
                    itemVm.HsnCode = item.HSNCode;
                    Items.Add(itemVm);
                }
                CalculateTotals();
                
                // Moved from constructor to ensure GrandTotal is calculated first
                PaymentStatus = existing.PaymentStatus ?? "Unpaid";
                TotalPaid = existing.TotalPaid;
            }
            else
            {
                if (Items.Count == 0)
                    AddItem();
            }
        });
    }

    private void AddItem()
    {
        var item = new PurchaseOrderItemViewModel(Products, Units.ToList(), TaxRatesList);
        
        item.IsTaxEditable = ShowGstFields;
        if (!ShowGstFields)
        {
            item.TaxRate = 0;
        }

        item.PropertyChanged += Item_PropertyChanged;
        if (item.FilteredProducts.Count > 0)
            item.SelectedProduct = null;
        Items.Add(item);
    }

    private void RemoveItem(PurchaseOrderItemViewModel? item)
    {
        if (item != null)
        {
            item.PropertyChanged -= Item_PropertyChanged;
            item.SelectedProduct = null;
            Items.Remove(item);
            CalculateTotals();
        }
    }

    private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PurchaseOrderItemViewModel.TotalAmount) ||
            e.PropertyName == nameof(PurchaseOrderItemViewModel.TaxAmount) ||
            e.PropertyName == nameof(PurchaseOrderItemViewModel.SelectedProduct) ||
            e.PropertyName == nameof(PurchaseOrderItemViewModel.Quantity) ||
            e.PropertyName == nameof(PurchaseOrderItemViewModel.UnitPrice) ||
            e.PropertyName == nameof(PurchaseOrderItemViewModel.TaxRate) ||
            e.PropertyName == nameof(PurchaseOrderItemViewModel.Discount))
        {
            CalculateTotals();
        }
    }
    partial void OnVendorSearchQueryChanged(string value)
    {
        if (_ignoreSearchUpdate) return;
        if (SelectedVendor != null && string.Equals(value, SelectedVendor.VendorName, StringComparison.OrdinalIgnoreCase)) return;

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (_ignoreSearchUpdate) return;
            if (SelectedVendor != null && string.Equals(value, SelectedVendor.VendorName, StringComparison.OrdinalIgnoreCase)) return;

            var query = value?.ToLower() ?? "";
            var filteredList = string.IsNullOrWhiteSpace(query)
                ? Vendors.ToList()
                : Vendors.Where(v =>
                    v.VendorName.ToLower().Contains(query) ||
                    (v.ContactNo?.Contains(query) ?? false)).ToList();

            if (SelectedVendor != null && !filteredList.Contains(SelectedVendor))
            {
                filteredList.Insert(0, SelectedVendor);
            }

            if (!filteredList.SequenceEqual(FilteredVendors))
            {
                FilteredVendors.Clear();
                foreach (var v in filteredList) FilteredVendors.Add(v);
            }

            if (!string.IsNullOrWhiteSpace(query) && SelectedVendor == null)
                IsVendorDropDownOpen = true;
        });
    }

    partial void OnSelectedVendorChanged(Vendor? value)
    {
        if (value != null)
        {
            _ignoreSearchUpdate = true;
            try
            {
                if (VendorSearchQuery != value.VendorName)
                {
                    VendorSearchQuery = value.VendorName;
                }
            }
            finally
            {
                _ignoreSearchUpdate = false;
            }
            IsVendorDropDownOpen = false;

            // Lock tax to 0 if vendor is unregistered
            foreach (var item in Items)
            {
                item.IsTaxEditable = ShowGstFields;
                if (!ShowGstFields)
                {
                    item.TaxRate = 0;
                }
            }
        }
        CalculateTotals();
    }

    partial void OnPlaceOfSupplyChanged(string? value) => CalculateTotals();
    partial void OnRoundOffChanged(decimal value) => CalculateTotals();
    partial void OnDiscountChanged(decimal value) { if (!IsItemLevelDiscount) CalculateTotals(); }
    partial void OnIsItemLevelDiscountChanged(bool value) => CalculateTotals();
    partial void OnShippingChargesChanged(decimal value) => CalculateTotals();
    partial void OnIsAutoRoundOffChanged(bool value) => CalculateTotals();

    private void CalculateTotals()
    {
        decimal subTotal = 0;
        decimal taxTotal = 0;
        decimal cgstTotal = 0;
        decimal sgstTotal = 0;
        decimal igstTotal = 0;
        decimal itemDiscountTotal = 0;

        foreach (var item in Items)
        {
            if (item.SelectedProduct != null)
            {
                decimal itemBaseAmount = item.Quantity * item.UnitPrice;
                decimal discountedAmount = IsItemLevelDiscount ? (itemBaseAmount - item.Discount) : itemBaseAmount;

                var tax = _taxCalculator.CalculateTax(
                    discountedAmount,
                    item.TaxRate,
                    _business.State,
                    PlaceOfSupply ?? SelectedVendor?.State);

                // Non-GST Business & Non-GST Vendor: No tax
                if (!ShowGstFields)
                {
                    item.TaxAmount = 0;
                    item.CgstAmount = 0;
                    item.SgstAmount = 0;
                    item.IgstAmount = 0;
                }
                else
                {
                    item.TaxAmount = tax.TotalTaxAmount;
                    item.CgstAmount = tax.CGST;
                    item.SgstAmount = tax.SGST;
                    item.IgstAmount = tax.IGST;
                }
                item.TotalAmount = discountedAmount + item.TaxAmount;

                subTotal += itemBaseAmount;
                taxTotal += item.TaxAmount;
                cgstTotal += tax.CGST;
                sgstTotal += tax.SGST;
                igstTotal += tax.IGST;
                itemDiscountTotal += item.Discount;
            }
        }

        TotalAmount = subTotal;
        TotalTax = taxTotal;
        TotalCGST = cgstTotal;
        TotalSGST = sgstTotal;
        TotalIGST = igstTotal;

        decimal tempGrandTotal;
        if (IsItemLevelDiscount)
        {
            Discount = itemDiscountTotal;
            tempGrandTotal = (subTotal - itemDiscountTotal) + taxTotal + ShippingCharges;
        }
        else
        {
            tempGrandTotal = subTotal + taxTotal - Discount + ShippingCharges;
        }

        if (IsAutoRoundOff)
        {
            decimal totalBeforeRound = tempGrandTotal;
            GrandTotal = Math.Floor(tempGrandTotal);
            RoundOff = GrandTotal - totalBeforeRound;
        }
        else
        {
            GrandTotal = tempGrandTotal;
            RoundOff = 0;
        }
    }

    public IAsyncRelayCommand SaveCommand { get; }
    public IRelayCommand CancelCommand { get; }
    public IRelayCommand AddItemCommand { get; }
    public IRelayCommand<PurchaseOrderItemViewModel> RemoveItemCommand { get; }

    public event Action<PurchaseOrder?>? RequestClose;

    private async Task SaveAsync()
    {
        var emptyItems = Items.Where(i => i.SelectedProduct == null).ToList();
        foreach (var empty in emptyItems) Items.Remove(empty);

        ValidationVisible = true;
        ValidateAll();

        if (HasErrors)
        {
            var allErrors = _errors.Values.SelectMany(e => e).Distinct().ToList();
            GeneralErrorMessage = string.Join(", ", allErrors);
            return;
        }

        var po = new PurchaseOrder
        {
            PurchaseOrderID = _existingPOId ?? 0,
            BusinessId = _businessId,
            VendorId = SelectedVendor!.VendorID,
            PONumber = PoNumber,
            PODate = PoDate ?? DateTime.Now,
            TotalAmount = TotalAmount,
            TotalTax = TotalTax,
            Discount = Discount,
            GrandTotal = GrandTotal,
            ShippingCharges = ShippingCharges,
            PlaceOfSupply = PlaceOfSupply,
            RoundOff = RoundOff,
            TotalCGST = TotalCGST,
            TotalSGST = TotalSGST,
            TotalIGST = TotalIGST,
            PaymentMethod = PaymentMethod,
            PaymentTerms = PaymentTerms,
            TermsAndConditions = TermsAndConditions,
            Notes = Notes,
            DeliveryStatus = DeliveryStatus,
            PaymentStatus = PaymentStatus,
            TotalPaid = TotalPaid,
            IsItemLevelDiscount = IsItemLevelDiscount,
            ExpectedDeliveryDate = ExpectedDeliveryDate,
            IsAutoRoundOff = IsAutoRoundOff,
            VendorBillPath = VendorBillPath,
            Items = Items.Select(i => new PurchaseOrderItem
            {
                PurchaseOrderItemID = i.PurchaseOrderItemId,
                ProductID = i.SelectedProduct!.ProductID,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                TaxRate = i.TaxRate,
                TaxAmount = i.TaxAmount,
                CGST_Rate = i.TaxRate / 2,
                CGST_Amount = i.CgstAmount,
                SGST_Rate = i.TaxRate / 2,
                SGST_Amount = i.SgstAmount,
                IGST_Rate = i.TaxRate,
                IGST_Amount = i.IgstAmount,
                HSNCode = i.HsnCode,
                Unit = i.Unit,
                Discount = i.Discount,
                TotalAmount = i.TotalAmount
            }).ToList()
        };

        bool success = false;
        if (_existingPOId.HasValue)
        {
            try 
            {
                await _ledgerService.ProcessPurchaseOrderAsync(po, SelectedWarehouse!.WarehouseID);
                success = true;
            }
            catch(Exception ex)
            {
                GeneralErrorMessage = "Failed to update PO: " + ex.Message;
                success = false;
            }
        }
        else
        {
            try
            {
                await _ledgerService.ProcessPurchaseOrderAsync(po, SelectedWarehouse!.WarehouseID);
                success = true;
            }
            catch (Exception ex)
            {
                success = false;
                GeneralErrorMessage = "Failed to process purchase order: " + ex.Message;
            }
        }

        if (success)
        {
            RequestClose?.Invoke(po);
        }
        else if (string.IsNullOrEmpty(GeneralErrorMessage))
        {
            GeneralErrorMessage = "Failed to save purchase order to database.";
        }
    }

    private void Cancel() => RequestClose?.Invoke(null);

    public async Task AttachBillAsync(string sourcePath)
    {
        try
        {
            var storage = new FileStorageService();
            var savedPath = await storage.StoreBillAsync(sourcePath);
            VendorBillPath = savedPath;
            VendorBillFileName = System.IO.Path.GetFileName(sourcePath);
        }
        catch (Exception ex)
        {
            GeneralErrorMessage = "Failed to attach bill: " + ex.Message;
        }
    }
}

public partial class PurchaseOrderItemViewModel : ObservableObject
{
    private readonly ObservableCollection<Product> _allProducts;
    [ObservableProperty] private ObservableCollection<Product> _filteredProducts;
    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private bool _isDropDownOpen;
    private bool _ignoreSearchUpdate;

    [ObservableProperty] private int _purchaseOrderItemId;
    [ObservableProperty] private Product? _selectedProduct;
    [ObservableProperty] private int _quantity = 1;
    [ObservableProperty] private decimal _unitPrice;
    [ObservableProperty] private decimal _taxRate;
    [ObservableProperty] private decimal _discount;
    [ObservableProperty] private decimal _taxAmount;
    [ObservableProperty] private decimal _cgstAmount;
    [ObservableProperty] private decimal _sgstAmount;
    [ObservableProperty] private decimal _igstAmount;
    [ObservableProperty] private decimal _totalAmount;
    [ObservableProperty] private string? _hsnCode;
    [ObservableProperty] private bool _isTaxEditable = true;
    private string? _unit;
    public string? Unit
    {
        get => _unit;
        set 
        {
            var sanitized = value;
            if (sanitized != null && sanitized.Contains("BusinessSuite.DAL.Entities")) sanitized = "nos";
            SetProperty(ref _unit, sanitized);
        }
    }

    public PurchaseOrderItemViewModel(ObservableCollection<Product> products, List<UnitOfMeasure> units, List<decimal> taxRates)
    {
        _allProducts = products;
        _filteredProducts = new ObservableCollection<Product>(products);
        Units = new ObservableCollection<UnitOfMeasure>(units);
        TaxRates = new ObservableCollection<decimal>(taxRates);
        TaxRates = new ObservableCollection<decimal>(taxRates);
        _allProducts.CollectionChanged += (s, e) => Avalonia.Threading.Dispatcher.UIThread.Post(() => UpdateFilteredProducts(SearchQuery));
    }

    private void UpdateFilteredProducts(string? value)
    {
        var query = value?.ToLower() ?? "";
        var filteredList = string.IsNullOrWhiteSpace(query)
            ? _allProducts.ToList()
            : _allProducts.Where(p =>
                p.ProductName.ToLower().Contains(query) ||
                (p.SKU?.ToLower().Contains(query) ?? false)).ToList();

        // CRITICAL: Always keep SelectedProduct in the list to prevent Avalonia from nullifying the selection
        if (SelectedProduct != null && !filteredList.Contains(SelectedProduct))
        {
            filteredList.Insert(0, SelectedProduct);
        }

        if (filteredList.SequenceEqual(FilteredProducts)) return;

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (filteredList.SequenceEqual(FilteredProducts)) return;
            FilteredProducts.Clear();
            foreach (var p in filteredList) FilteredProducts.Add(p);
        });
    }

    [ObservableProperty] private ObservableCollection<UnitOfMeasure> _units;
    [ObservableProperty] private ObservableCollection<decimal> _taxRates;

    private void EnsureSelectedProductInFiltered()
    {
        if (SelectedProduct != null && !FilteredProducts.Contains(SelectedProduct))
        {
            FilteredProducts.Insert(0, SelectedProduct);
        }
    }

    partial void OnSearchQueryChanged(string value)
    {
        if (_ignoreSearchUpdate) return;
        if (SelectedProduct != null && string.Equals(value, SelectedProduct.ProductName, StringComparison.OrdinalIgnoreCase)) return;

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (_ignoreSearchUpdate) return;
            if (SelectedProduct != null && string.Equals(value, SelectedProduct.ProductName, StringComparison.OrdinalIgnoreCase)) return;

            var query = value?.ToLower() ?? "";
            var filteredList = string.IsNullOrWhiteSpace(query)
                ? _allProducts.ToList()
                : _allProducts.Where(p =>
                    p.ProductName.ToLower().Contains(query) ||
                    (p.SKU?.ToLower().Contains(query) ?? false)).ToList();

            if (SelectedProduct != null && !filteredList.Contains(SelectedProduct))
                filteredList.Insert(0, SelectedProduct);

            if (!filteredList.SequenceEqual(FilteredProducts))
            {
                FilteredProducts.Clear();
                foreach (var p in filteredList) FilteredProducts.Add(p);
            }

            if (!string.IsNullOrWhiteSpace(value) && SelectedProduct == null)
                IsDropDownOpen = true;
        });
    }

    partial void OnSelectedProductChanged(Product? value)
    {
        if (value != null)
        {
            UnitPrice = value.PurchasePrice; // DIFFERENT: Use PurchasePrice for PO
            TaxRate = value.TaxRate;
            HsnCode = value.HSNCode;
            Unit = value.Unit;
            Discount = 0;
            
            _ignoreSearchUpdate = true;
            try
            {
                if (SearchQuery != value.ProductName) SearchQuery = value.ProductName;
            }
            finally
            {
                _ignoreSearchUpdate = false;
            }

            EnsureSelectedProductInFiltered();
            IsDropDownOpen = false;

            // Check parent VM's selected vendor status
            // Note: This requires accessing the parent VM or a flag. 
            // For now, we'll rely on the parent VM setting it when adding items or changing vendors.
        }
    }
}
