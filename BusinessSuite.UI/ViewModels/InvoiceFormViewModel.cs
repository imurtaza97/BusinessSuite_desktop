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

public partial class InvoiceFormViewModel : ViewModelBase, INotifyDataErrorInfo
{
    private readonly InvoiceRepository _invoiceRepository;
    private readonly CustomerRepository _customerRepository;
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
        if (SelectedCustomer == null) AddError(nameof(SelectedCustomer), "Customer is required");
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
                    // If multiple rows and one is empty, maybe allow it but warn? 
                    // No, usually best to require it if added.
                    AddError(nameof(Items), "Product is missing in one of the rows");
                }
            }
        }

        OnPropertyChanged(nameof(HasErrors));
    }

    [ObservableProperty] private bool _validationVisible = false;
    [ObservableProperty] private string _generalErrorMessage = string.Empty;
    [ObservableProperty] private string _title = "Create Invoice";
    [ObservableProperty] private string _invoiceNumber = "";
    [ObservableProperty] private DateTime? _invoiceDate = DateTime.Now;
    [ObservableProperty] private DateTime? _dueDate;
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

    private readonly int? _existingInvoiceId;

    [ObservableProperty] private ObservableCollection<Customer> _customers = new();
    [ObservableProperty] private ObservableCollection<Customer> _filteredCustomers = new();
    [ObservableProperty] private string _customerSearchQuery = string.Empty;
    [ObservableProperty] private bool _isCustomerDropDownOpen;
    [ObservableProperty] private Customer? _selectedCustomer;

    [ObservableProperty] private ObservableCollection<Product> _products = new();
    [ObservableProperty] private ObservableCollection<UnitOfMeasure> _units = new();
    [ObservableProperty] private ObservableCollection<string> _unitNames = new();

    [ObservableProperty] private ObservableCollection<Warehouse> _warehouses = new();
    [ObservableProperty] private Warehouse? _selectedWarehouse;

    [ObservableProperty] private ObservableCollection<InvoiceItemViewModel> _items = new();

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
    [ObservableProperty] private bool _isDiscountPercentage;
    [ObservableProperty] private decimal _discountPercentage;

    private readonly bool _isFinalized;
    private readonly string? _originalDeliveryStatus;
    private readonly string? _originalPaymentStatus;

    public bool IsGstRegistered => _business?.IsGSTRegistered ?? false;
    public bool IsComposition => _business?.GstType == BusinessGstType.Composition;
    public bool IsRegularScheme => IsGstRegistered && !IsComposition;
    public bool IsDiscountAmountMode => !IsDiscountPercentage;
    public bool IsFinalized => _isFinalized;
    public bool CanEditCoreFields => !_isFinalized;
    public bool CanEditItems => !_isFinalized;
    public bool CanEditItemsAndBasics => !_isFinalized; // Phase 2: Restrict core business fields
    public bool CanEditPaymentStatus => true; // Always editable for tracking
    public bool CanEditPaymentFields => true; // Amount paid is always editable
    public bool CanEditDeliveryStatus => true; // Always editable for tracking
    public bool CanEditDeliveryFields => true; // Status is always editable
    public bool CanEditNotes => true; // Notes always editable
    public bool CanEditDiscountAmount => CanEditCoreFields && !IsItemLevelDiscount && !IsDiscountPercentage;
    public bool CanEditDiscountPercentage => CanEditCoreFields && !IsItemLevelDiscount;
    public string TotalInWords => BLL.Services.NumberToWordsConverter.ConvertToWords(GrandTotal).ToUpper();

    private static string GetFinancialYear(DateTime date)
    {
        if (date.Month >= 4)
            return $"{date.Year % 100}-{(date.Year + 1) % 100}";
        else
            return $"{(date.Year - 1) % 100}-{date.Year % 100}";
    }


    public InvoiceFormViewModel(int businessId, Invoice? existingInvoice = null)
    {
        _businessId = businessId;
        var db = new AppDbContext();
        _invoiceRepository = new InvoiceRepository(db);
        _customerRepository = new CustomerRepository(db);
        _productRepository = new ProductRepository(db);
        _warehouseRepository = new WarehouseRepository(db);
        var dbFactory = new AppDbContextFactory();
        _ledgerService = new LedgerService(dbFactory);
        _taxCalculator = new TaxCalculator();

        _business = db.Businesses.FirstOrDefault(b => b.BusinessID == businessId) ?? new Business();

        SaveCommand = new AsyncRelayCommand(SaveAsync);
        SaveDraftCommand = new AsyncRelayCommand(SaveDraftAsync);
        CancelCommand = new RelayCommand(Cancel);
        AddItemCommand = new RelayCommand(AddItem);
        RemoveItemCommand = new RelayCommand<InvoiceItemViewModel>(RemoveItem);
        AddProductCommand = new AsyncRelayCommand(AddProductAsync);
        AddCustomerCommand = new AsyncRelayCommand(AddCustomerAsync);

        if (existingInvoice != null)
        {
            _existingInvoiceId = existingInvoice.InvoiceID;
            _isFinalized = !existingInvoice.IsDraft;
            _originalDeliveryStatus = existingInvoice.DeliveryStatus;
            _originalPaymentStatus = existingInvoice.PaymentStatus;

            Title = $"Edit Invoice - {existingInvoice.InvoiceNumber}";
            InvoiceNumber = existingInvoice.InvoiceNumber;
            InvoiceDate = existingInvoice.InvoiceDate;
            PaymentMethod = existingInvoice.PaymentMethod ?? "Cash";
            PaymentTerms = existingInvoice.PaymentTerms ?? "Due on Receipt";
            TermsAndConditions = existingInvoice.TermsAndConditions;
            Notes = existingInvoice.Notes;
            ShippingCharges = existingInvoice.ShippingCharges;
            Discount = existingInvoice.Discount;
            DeliveryStatus = existingInvoice.DeliveryStatus ?? "Pending";
            PaymentStatus = existingInvoice.PaymentStatus ?? "Unpaid";
            TotalPaid = existingInvoice.TotalPaid;
            PlaceOfSupply = existingInvoice.PlaceOfSupply ?? _business.State;
            RoundOff = existingInvoice.RoundOff;
            DueDate = existingInvoice.DueDate;
            IsAutoRoundOff = existingInvoice.IsAutoRoundOff;
        }
        else
        {
            PlaceOfSupply = _business.State;
            DueDate = DateTime.Now.AddDays(30);
        }

        // Don't use fire-and-forget in constructor if possible, 
        // but since we are in a VM, we trigger it.
        // We'll make sure it's robust.
        Task.Run(async () => await InitializeAsync(existingInvoice));
    }

    public IAsyncRelayCommand AddProductCommand { get; }

    public List<string> PaymentMethodsList { get; } = new() { "Cash", "Bank Transfer", "UPI/QR", "Card", "Cheque" };
    public List<string> DeliveryStatusOptions { get; } = new() { "Pending", "Shipped", "Returned", "Cancelled" };
    public List<string> PaymentStatusOptions { get; } = new() { "Unpaid", "Paid", "Partially Paid" };
    public List<decimal> TaxRatesList { get; } = new() { 0, 5, 12, 18, 28 };
    
    // Item Type Options
    public List<string> ItemTypes { get; } = new() { "Product", "Service" };
    public List<string> ProductUnits { get; } = new() { "Pieces", "KG", "Meters", "Liters", "Box", "Pack", "Kg", "nos", "nos." };
    public List<string> ServiceUnits { get; } = new() { "Hours", "Days", "Week", "Month", "Project", "Call", "Consultation" };

    public IAsyncRelayCommand AddCustomerCommand { get; }

    private async Task AddCustomerAsync()
    {
        var vm = new CustomerFormViewModel(_businessId);
        var win = new CustomerFormWindow { DataContext = vm };

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var result = await win.ShowDialog<Customer?>(desktop.MainWindow!);
            if (result != null)
            {
                var success = await _customerRepository.AddAsync(result);
                if (success)
                {
                    Customers.Add(result);
                    FilteredCustomers.Add(result);
                    SelectedCustomer = result;
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

    private async Task InitializeAsync(Invoice? existing = null)
    {
        var nextNumber = "";
        if (existing == null)
        {
            var fy = GetFinancialYear(InvoiceDate ?? DateTime.Now);

            string prefix = IsComposition
                ? $"BOS/{fy}/"
                : $"INV/{fy}/";

            nextNumber = await _invoiceRepository.GetNextInvoiceNumberAsync(_businessId, prefix);
        }

        var customersList = await _customerRepository.GetAllAsync(_businessId);
        var productsList = await _productRepository.GetAllAsync(_businessId);
        var warehousesList = await _warehouseRepository.GetAllAsync(_businessId);

        using var db = new AppDbContext();
        var unitList = await db.UnitsOfMeasure.Where(u => u.BusinessId == 0 || u.BusinessId == _businessId).ToListAsync();

        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (existing == null)
                InvoiceNumber = nextNumber;

            Customers.Clear();
            FilteredCustomers.Clear();
            foreach (var c in customersList)
            {
                Customers.Add(c);
                FilteredCustomers.Add(c);
            }

            if (existing != null)
                SelectedCustomer = Customers.FirstOrDefault(c => c.CustomerID == existing.CustomerID);

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
                IsItemLevelDiscount = existing.IsItemLevelDiscount;
                Items.Clear();
                foreach (var item in existing.Items)
                {
                    var itemVm = new InvoiceItemViewModel(Products, Units.ToList(), TaxRatesList);
                    itemVm.PropertyChanged += Item_PropertyChanged;
                    itemVm.InvoiceItemId = item.InvoiceItemID;
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
        var item = new InvoiceItemViewModel(Products, Units.ToList(), TaxRatesList);
        item.PropertyChanged += Item_PropertyChanged;
        // Ensure no invalid selection
        if (item.FilteredProducts.Count > 0)
            item.SelectedProduct = null;
        Items.Add(item);
    }

    private void RemoveItem(InvoiceItemViewModel? item)
    {
        if (item != null)
        {
            item.PropertyChanged -= Item_PropertyChanged;
            // Clear selection to avoid invalid index
            item.SelectedProduct = null;
            Items.Remove(item);
            CalculateTotals();
        }
    }

    private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(InvoiceItemViewModel.TotalAmount) ||
            e.PropertyName == nameof(InvoiceItemViewModel.TaxAmount) ||
            e.PropertyName == nameof(InvoiceItemViewModel.SelectedProduct) ||
            e.PropertyName == nameof(InvoiceItemViewModel.Quantity) ||
            e.PropertyName == nameof(InvoiceItemViewModel.UnitPrice) ||
            e.PropertyName == nameof(InvoiceItemViewModel.TaxRate) ||
            e.PropertyName == nameof(InvoiceItemViewModel.Discount))
        {
            CalculateTotals();
        }
    }

    partial void OnCustomerSearchQueryChanged(string value)
    {
        if (_ignoreSearchUpdate) return;
        if (SelectedCustomer != null && string.Equals(value, SelectedCustomer.CustomerName, StringComparison.OrdinalIgnoreCase)) return;

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (_ignoreSearchUpdate) return;
            if (SelectedCustomer != null && string.Equals(value, SelectedCustomer.CustomerName, StringComparison.OrdinalIgnoreCase)) return;

            var query = value?.ToLower() ?? "";
            var filteredList = string.IsNullOrWhiteSpace(query)
                ? Customers.ToList()
                : Customers.Where(c =>
                    c.CustomerName.ToLower().Contains(query) ||
                    (c.ContactNo?.Contains(query) ?? false)).ToList();

            if (SelectedCustomer != null && !filteredList.Contains(SelectedCustomer))
            {
                filteredList.Insert(0, SelectedCustomer);
            }

            if (!filteredList.SequenceEqual(FilteredCustomers))
            {
                FilteredCustomers.Clear();
                foreach (var c in filteredList) FilteredCustomers.Add(c);
            }

            if (!string.IsNullOrWhiteSpace(query) && SelectedCustomer == null)
                IsCustomerDropDownOpen = true;
        });
    }

    partial void OnSelectedCustomerChanged(Customer? value)
    {
        if (value != null)
        {
            _ignoreSearchUpdate = true;
            try
            {
                if (CustomerSearchQuery != value.CustomerName)
                {
                    CustomerSearchQuery = value.CustomerName;
                }
            }
            finally
            {
                _ignoreSearchUpdate = false;
            }
            IsCustomerDropDownOpen = false;
        }
        CalculateTotals(); // Re-calculate tax based on customer state
    }

    partial void OnPlaceOfSupplyChanged(string? value)
    {
        CalculateTotals();
    }

    partial void OnRoundOffChanged(decimal value)
    {
        CalculateTotals();
    }

    partial void OnDiscountChanged(decimal value)
    {
        if (!IsItemLevelDiscount && !IsDiscountPercentage)
            CalculateTotals();
    }

    partial void OnIsDiscountPercentageChanged(bool value)
    {
        OnPropertyChanged(nameof(IsDiscountAmountMode));
        OnPropertyChanged(nameof(CanEditDiscountAmount));
        OnPropertyChanged(nameof(CanEditDiscountPercentage));
        CalculateTotals();
    }

    partial void OnDiscountPercentageChanged(decimal value)
    {
        if (!IsItemLevelDiscount)
            CalculateTotals();
    }

    partial void OnIsItemLevelDiscountChanged(bool value)
    {
        OnPropertyChanged(nameof(CanEditDiscountAmount));
        OnPropertyChanged(nameof(CanEditDiscountPercentage));
        CalculateTotals();
    }

    partial void OnDeliveryStatusChanged(string value)
    {
        OnPropertyChanged(nameof(CanEditDeliveryStatus));
    }

    partial void OnShippingChargesChanged(decimal value)
    {
        CalculateTotals();
    }

    partial void OnIsAutoRoundOffChanged(bool value)
    {
        CalculateTotals();
    }

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
                    PlaceOfSupply ?? SelectedCustomer?.State);

                // Non-GST or Composition: No tax
                if (!IsGstRegistered || IsComposition)
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

        decimal discountAmount = Discount;
        if (IsItemLevelDiscount)
        {
            discountAmount = itemDiscountTotal;
        }
        else if (IsDiscountPercentage)
        {
            discountAmount = Math.Round(subTotal * DiscountPercentage / 100m, 2);
            Discount = discountAmount;
        }

        decimal tempGrandTotal;
        if (IsItemLevelDiscount)
        {
            Discount = itemDiscountTotal;
            tempGrandTotal = (subTotal - itemDiscountTotal) + taxTotal + ShippingCharges;
        }
        else
        {
            tempGrandTotal = subTotal + taxTotal - discountAmount + ShippingCharges;
        }

        if (IsAutoRoundOff)
        {
            decimal totalBeforeRound = tempGrandTotal;
            // Round DOWN to the nearest rupee (minimum side) with 00 paise
            GrandTotal = Math.Floor(tempGrandTotal);
            RoundOff = GrandTotal - totalBeforeRound;
        }
        else
        {
            GrandTotal = tempGrandTotal;
            RoundOff = 0;
        }
        OnPropertyChanged(nameof(TotalInWords));
    }

    public IAsyncRelayCommand SaveDraftCommand { get; }
    public IAsyncRelayCommand SaveCommand { get; }
    public IRelayCommand CancelCommand { get; }
    public IRelayCommand AddItemCommand { get; }
    public IRelayCommand<InvoiceItemViewModel> RemoveItemCommand { get; }

    public event Action<Invoice?>? RequestClose;

    private async Task SaveDraftAsync()
    {
        // Phase 1: Prevent reverting finalized invoices to draft
        if (_isFinalized)
        {
            GeneralErrorMessage = "Finalized invoices cannot be reverted to draft. Edit permitted fields only or create a new invoice.";
            return;
        }

        ValidationVisible = true;
        ClearAllErrors();

        if (SelectedCustomer == null)
            AddError(nameof(SelectedCustomer), "Customer is required");
        if (string.IsNullOrWhiteSpace(InvoiceNumber))
            AddError(nameof(InvoiceNumber), "Invoice number is required");

        if (HasErrors)
        {
            GeneralErrorMessage = _errors.Values.SelectMany(e => e).Distinct().FirstOrDefault() ?? string.Empty;
            return;
        }

        var emptyItems = Items.Where(i => i.SelectedProduct == null).ToList();
        foreach (var empty in emptyItems) Items.Remove(empty);

        // Phase 3: Enforce status transitions even in draft mode
        if (_existingInvoiceId.HasValue)
        {
            if (!IsValidFinalDeliveryTransition(_originalDeliveryStatus ?? string.Empty, DeliveryStatus))
            {
                GeneralErrorMessage = "Delivery status cannot be moved backward in draft.";
                return;
            }
            if (!IsValidFinalPaymentTransition(_originalPaymentStatus ?? string.Empty, PaymentStatus))
            {
                GeneralErrorMessage = "Payment status cannot be moved backward in draft.";
                return;
            }
        }

        var invoice = new Invoice
        {
            InvoiceID = _existingInvoiceId ?? 0,
            BusinessID = _businessId,
            CustomerID = SelectedCustomer!.CustomerID,
            InvoiceNumber = InvoiceNumber,
            InvoiceDate = InvoiceDate ?? DateTime.Now,
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
            DeliveryStatus = _existingInvoiceId.HasValue ? DeliveryStatus : "Pending",
            PaymentStatus = _existingInvoiceId.HasValue ? PaymentStatus : "Unpaid",
            TotalPaid = _existingInvoiceId.HasValue ? TotalPaid : 0,
            IsItemLevelDiscount = IsItemLevelDiscount,
            DueDate = DueDate,
            IsAutoRoundOff = IsAutoRoundOff,
            IsDraft = true,
            Items = Items.Select(i => new InvoiceItem
            {
                InvoiceItemID = i.InvoiceItemId,
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
                TotalAmount = i.TotalAmount,
                ItemType = i.ItemType
            }).ToList()
        };

        try
        {
            if (_existingInvoiceId.HasValue)
            {
                var updated = await _invoiceRepository.UpdateAsync(invoice);
                if (!updated)
                    throw new InvalidOperationException("Unable to update draft invoice.");
            }
            else
            {
                var added = await _invoiceRepository.AddAsync(invoice);
                if (!added)
                    throw new InvalidOperationException("Unable to save draft invoice.");
            }

            RequestClose?.Invoke(invoice);
        }
        catch (Exception ex)
        {
            GeneralErrorMessage = "Failed to save draft invoice: " + ex.Message;
        }
    }

    private bool IsValidFinalDeliveryTransition(string original, string current)
    {
        if (original == current) return true;
        if (original == "Pending")
            return current == "Shipped" || current == "Returned" || current == "Cancelled";
        if (original == "Shipped")
            return current == "Returned" || current == "Cancelled";
        return false;
    }

    private bool IsValidFinalPaymentTransition(string original, string current)
    {
        if (original == current) return true;
        if (original == "Unpaid")
            return current == "Partially Paid" || current == "Paid";
        if (original == "Partially Paid")
            return current == "Paid";
        return false;
    }

    private async Task SaveAsync()
    {
        // Filter out empty rows before validation if possible, or just validate them
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

        if (_existingInvoiceId.HasValue)
        {
            // Phase 3: Enforce status transitions - prevent reversals for BOTH draft and finalized
            if (!IsValidFinalDeliveryTransition(_originalDeliveryStatus ?? string.Empty, DeliveryStatus))
            {
                GeneralErrorMessage = "Delivery status cannot be moved backward. Current: " + _originalDeliveryStatus + " → Attempted: " + DeliveryStatus;
                return;
            }

            if (!IsValidFinalPaymentTransition(_originalPaymentStatus ?? string.Empty, PaymentStatus))
            {
                GeneralErrorMessage = "Payment status cannot be moved backward. Current: " + _originalPaymentStatus + " → Attempted: " + PaymentStatus;
                return;
            }
        }

        var invoice = new Invoice
        {
            InvoiceID = _existingInvoiceId ?? 0,
            BusinessID = _businessId,
            CustomerID = SelectedCustomer!.CustomerID,
            InvoiceNumber = InvoiceNumber,
            InvoiceDate = InvoiceDate ?? DateTime.Now,
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
            DueDate = DueDate,
            IsAutoRoundOff = IsAutoRoundOff,
            IsDraft = false,
            Items = Items.Select(i => new InvoiceItem
            {
                InvoiceItemID = i.InvoiceItemId,
                ProductID = i.SelectedProduct!.ProductID,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                TaxRate = i.TaxRate,
                TaxAmount = i.TaxAmount,
                CGST_Rate = i.TaxRate / 2, // Assuming split if not IGST
                CGST_Amount = i.CgstAmount,
                SGST_Rate = i.TaxRate / 2,
                SGST_Amount = i.SgstAmount,
                IGST_Rate = i.TaxRate,
                IGST_Amount = i.IgstAmount,
                HSNCode = i.HsnCode,
                Unit = i.Unit,
                Discount = i.Discount,
                TotalAmount = i.TotalAmount,
                ItemType = i.ItemType
            }).ToList()
        };

        bool success = false;
        if (_existingInvoiceId.HasValue)
        {
             try 
             {
                // We use process even for updates to handle status changes (e.g. Draft -> Shipped)
                await _ledgerService.ProcessInvoiceAsync(invoice, SelectedWarehouse!.WarehouseID);
                success = true;
             }
             catch(Exception ex)
             {
                 GeneralErrorMessage = "Failed to update invoice: " + ex.Message;
                 success = false;
             }
        }
        else
        {
            try
            {
                await _ledgerService.ProcessInvoiceAsync(invoice, SelectedWarehouse!.WarehouseID);
                success = true;
            }
            catch (Exception ex)
            {
                success = false;
                GeneralErrorMessage = "Failed to process invoice: " + ex.Message;
            }
        }

        if (success)
        {
            RequestClose?.Invoke(invoice);
        }
        else if (string.IsNullOrEmpty(GeneralErrorMessage))
        {
            GeneralErrorMessage = "Failed to save invoice to database.";
        }
    }

    private void Cancel()
    {
        RequestClose?.Invoke(null);
    }
}

public partial class InvoiceItemViewModel : ObservableObject
{
    private readonly ObservableCollection<Product> _allProducts;
    [ObservableProperty] private ObservableCollection<Product> _filteredProducts;
    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private bool _isDropDownOpen;
    private bool _ignoreSearchUpdate;

    [ObservableProperty] private int _invoiceItemId;
    [ObservableProperty] private Product? _selectedProduct;
    public string ProductType => SelectedProduct?.Type ?? string.Empty;
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
    [ObservableProperty] private string _itemType = "Product"; // "Product" or "Service"
    
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

    public InvoiceItemViewModel(ObservableCollection<Product> products, List<UnitOfMeasure> units, List<decimal> taxRates)
    {
        _allProducts = products;
        _filteredProducts = new ObservableCollection<Product>(products);
        Units = new ObservableCollection<UnitOfMeasure>(units);
        TaxRates = new ObservableCollection<decimal>(taxRates);

        // Update filtered list when main products collection changes
        _allProducts.CollectionChanged += (s, e) => Avalonia.Threading.Dispatcher.UIThread.Post(() => UpdateFilteredProducts(SearchQuery));
    }

    private void UpdateFilteredProducts(string? value)
    {
        var query = value?.ToLower() ?? "";
        var filteredList = string.IsNullOrWhiteSpace(query)
            ? _allProducts.ToList()
            : _allProducts.Where(p =>
                p.ProductName.ToLower().Contains(query) ||
                (p.SKU?.ToLower().Contains(query) ?? false) ||
                p.Type.ToLower().Contains(query)).ToList();

        // CRITICAL: Always keep SelectedProduct in the list to prevent de-selection
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
        // Only null out if we are NOT in the middle of a search/text update
        if (SelectedProduct != null && !FilteredProducts.Contains(SelectedProduct))
        {
            // Instead of nulling, consider adding it back to the list 
            // to prevent the DataGrid from losing the binding context.
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
                    (p.SKU?.ToLower().Contains(query) ?? false) ||
                    p.Type.ToLower().Contains(query)).ToList();

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
            // Explicitly set values to ensure they are fetched from the product entity
            UnitPrice = value.SalePrice;
            TaxRate = value.TaxRate;
            HsnCode = value.HSNCode;
            Unit = value.Unit;
            Discount = 0; // Reset discount when product changes
            ItemType = value.IsService ? "Service" : "Product"; // Auto-set type based on product

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
            OnPropertyChanged(nameof(ProductType));
        }
    }
}
