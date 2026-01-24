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
    private readonly TaxCalculator _taxCalculator;
    private readonly Business _business;
    private readonly int _businessId;

    private readonly Dictionary<string, List<string>> _errors = new();

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
        if (!Items.Any()) AddError(nameof(Items), "At least one item is required");
        
        foreach (var item in Items)
        {
            if (item.Quantity <= 0) AddError(nameof(Items), "Quantity must be greater than zero");
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
    [ObservableProperty] private string _status = "Unpaid";

    private readonly int? _existingInvoiceId;

    [ObservableProperty] private ObservableCollection<Customer> _customers = new();
    [ObservableProperty] private ObservableCollection<Customer> _filteredCustomers = new();
    [ObservableProperty] private string _customerSearchQuery = string.Empty;
    [ObservableProperty] private bool _isCustomerDropDownOpen;
    [ObservableProperty] private Customer? _selectedCustomer;

    [ObservableProperty] private ObservableCollection<Product> _products = new();
    [ObservableProperty] private ObservableCollection<UnitOfMeasure> _uoms = new();
    [ObservableProperty] private ObservableCollection<string> _uomNames = new();
    
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

    public bool IsGstRegistered => _business?.IsGSTRegistered ?? false;

    public InvoiceFormViewModel(int businessId, Invoice? existingInvoice = null)
    {
        _businessId = businessId;
        var db = new AppDbContext();
        _invoiceRepository = new InvoiceRepository(db);
        _customerRepository = new CustomerRepository(db);
        _productRepository = new ProductRepository(db);
        _taxCalculator = new TaxCalculator();

        _business = db.Businesses.FirstOrDefault(b => b.BusinessID == businessId) ?? new Business();

        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new RelayCommand(Cancel);
        AddItemCommand = new RelayCommand(AddItem);
        RemoveItemCommand = new RelayCommand<InvoiceItemViewModel>(RemoveItem);
        AddProductCommand = new AsyncRelayCommand(AddProductAsync);
        AddCustomerCommand = new AsyncRelayCommand(AddCustomerAsync);

        if (existingInvoice != null)
        {
            _existingInvoiceId = existingInvoice.InvoiceID;
            Title = $"Edit Invoice - {existingInvoice.InvoiceNumber}";
            InvoiceNumber = existingInvoice.InvoiceNumber;
            InvoiceDate = existingInvoice.InvoiceDate;
            PaymentMethod = existingInvoice.PaymentMethod ?? "Cash";
            PaymentTerms = existingInvoice.PaymentTerms ?? "Due on Receipt";
            TermsAndConditions = existingInvoice.TermsAndConditions;
            Notes = existingInvoice.Notes;
            ShippingCharges = existingInvoice.ShippingCharges;
            Discount = existingInvoice.Discount;
            Status = existingInvoice.Status ?? "Unpaid";
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
    public List<decimal> TaxRatesList { get; } = new() { 0, 5, 12, 18, 28 };

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
                Customers.Add(result);
                FilteredCustomers.Add(result);
                SelectedCustomer = result;
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
                Products.Add(result);
                // Can't easily select it here as we don't know which row clicked, 
                // but it's now in the list.
            }
        }
    }

    private async Task InitializeAsync(Invoice? existing = null)
    {
        var nextNumber = "";
        if (existing == null)
            nextNumber = await _invoiceRepository.GetNextInvoiceNumberAsync(_businessId);

        var customersList = await _customerRepository.GetAllAsync(_businessId);
        var productsList = await _productRepository.GetAllAsync(_businessId);
        
        using var db = new AppDbContext();
        var uomList = await db.UnitsOfMeasure.Where(u => u.BusinessId == 0 || u.BusinessId == _businessId).ToListAsync();

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

            Uoms.Clear();
            UomNames.Clear();
            foreach (var u in uomList) 
            {
                Uoms.Add(u);
                UomNames.Add(u.Name);
            }

            if (existing != null)
            {
                IsItemLevelDiscount = existing.IsItemLevelDiscount;
                Items.Clear();
                foreach (var item in existing.Items)
                {
                    var itemVm = new InvoiceItemViewModel(Products.ToList(), Uoms.ToList(), TaxRatesList);
                    itemVm.PropertyChanged += Item_PropertyChanged;
                    itemVm.SelectedProduct = Products.FirstOrDefault(p => p.ProductID == item.ProductID);
                    itemVm.Quantity = item.Quantity;
                    itemVm.UnitPrice = item.UnitPrice;
                    itemVm.TaxRate = item.TaxRate;
                    itemVm.Discount = item.Discount;
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
        var item = new InvoiceItemViewModel(Products.ToList(), Uoms.ToList(), TaxRatesList);
        item.PropertyChanged += Item_PropertyChanged;
        Items.Add(item);
    }

    private void RemoveItem(InvoiceItemViewModel? item)
    {
        if (item != null)
        {
            item.PropertyChanged -= Item_PropertyChanged;
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
        // Avoid re-filtering if the search query was set by selecting an item
        if (SelectedCustomer != null && value == SelectedCustomer.CustomerName)
            return;

        var query = value?.ToLower() ?? "";
        var filteredList = string.IsNullOrWhiteSpace(query)
            ? Customers.ToList()
            : Customers.Where(c => 
                c.CustomerName.ToLower().Contains(query) || 
                (c.ContactNo?.Contains(query) ?? false)).ToList();

        // Ensure SelectedCustomer is kept in the list
        if (SelectedCustomer != null && !filteredList.Contains(SelectedCustomer))
        {
            filteredList.Insert(0, SelectedCustomer);
        }

        if (!filteredList.SequenceEqual(FilteredCustomers))
        {
            FilteredCustomers.Clear();
            foreach (var c in filteredList) FilteredCustomers.Add(c);
        }

        if (!string.IsNullOrWhiteSpace(query) && query != SelectedCustomer?.CustomerName?.ToLower())
            IsCustomerDropDownOpen = true;
    }

    partial void OnSelectedCustomerChanged(Customer? value)
    {
        if (value != null)
        {
            if (CustomerSearchQuery != value.CustomerName)
            {
                CustomerSearchQuery = value.CustomerName;
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
        if (!IsItemLevelDiscount)
            CalculateTotals();
    }

    partial void OnIsItemLevelDiscountChanged(bool value)
    {
        CalculateTotals();
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

                item.TaxAmount = tax.TotalTaxAmount;
                item.CgstAmount = tax.CGST;
                item.SgstAmount = tax.SGST;
                item.IgstAmount = tax.IGST;
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
            GrandTotal = Math.Round(tempGrandTotal, 0);
            RoundOff = GrandTotal - totalBeforeRound;
        }
        else
        {
            GrandTotal = tempGrandTotal + RoundOff;
        }
    }

    public IAsyncRelayCommand SaveCommand { get; }
    public IRelayCommand CancelCommand { get; }
    public IRelayCommand AddItemCommand { get; }
    public IRelayCommand<InvoiceItemViewModel> RemoveItemCommand { get; }

    public event Action<Invoice?>? RequestClose;

    private async Task SaveAsync()
    {
        ValidationVisible = true;
        ValidateAll();

        if (HasErrors)
        {
            GeneralErrorMessage = "Please correct the errors before saving.";
            return;
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
            Status = Status,
            IsItemLevelDiscount = IsItemLevelDiscount,
            DueDate = DueDate,
            IsAutoRoundOff = IsAutoRoundOff,
            Items = Items.Select(i => new InvoiceItem
            {
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
                UOM = i.Uom,
                Discount = i.Discount,
                TotalAmount = i.TotalAmount
            }).ToList()
        };

        bool success;
        if (_existingInvoiceId.HasValue)
            success = await _invoiceRepository.UpdateAsync(invoice);
        else
            success = await _invoiceRepository.AddAsync(invoice);

        if (success)
        {
            RequestClose?.Invoke(invoice);
        }
        else
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
    private readonly List<Product> _allProducts;
    [ObservableProperty] private ObservableCollection<Product> _filteredProducts;
    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private bool _isDropDownOpen;
 
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
    [ObservableProperty] private string? _uom;

    public InvoiceItemViewModel(List<Product> products, List<UnitOfMeasure> uoms, List<decimal> taxRates)
    {
        _allProducts = products;
        _filteredProducts = new ObservableCollection<Product>(products);
        Uoms = new ObservableCollection<UnitOfMeasure>(uoms);
        TaxRates = new ObservableCollection<decimal>(taxRates);
    }

    [ObservableProperty] private ObservableCollection<UnitOfMeasure> _uoms;
    [ObservableProperty] private ObservableCollection<decimal> _taxRates;

    partial void OnSearchQueryChanged(string value)
    {
        // Avoid re-filtering if the search query was set by selecting an item
        // or if it matches the current selection (prevents feedback loops)
        if (SelectedProduct != null && value == SelectedProduct.ProductName)
            return;

        var query = value?.ToLower() ?? "";
        var filteredList = string.IsNullOrWhiteSpace(query) 
            ? _allProducts.ToList() 
            : _allProducts.Where(p => 
                p.ProductName.ToLower().Contains(query) || 
                (p.SKU?.ToLower().Contains(query) ?? false)).ToList();

        // If we have a selection and it's not in the filtered list, add it back
        // to prevent Avalonia from resetting SelectedProduct to null
        if (SelectedProduct != null && !filteredList.Contains(SelectedProduct))
        {
            filteredList.Insert(0, SelectedProduct);
        }

        // Only update collection if it actually changed to reduce UI flickering
        if (!filteredList.SequenceEqual(FilteredProducts))
        {
            FilteredProducts.Clear();
            foreach (var p in filteredList)
            {
                FilteredProducts.Add(p);
            }
        }

        if (!string.IsNullOrWhiteSpace(query) && query != SelectedProduct?.ProductName?.ToLower())
            IsDropDownOpen = true;
    }

    partial void OnSelectedProductChanged(Product? value)
    {
        if (value != null)
        {
            // Explicitly set values to ensure they are fetched from the product entity
            UnitPrice = value.SalePrice;
            TaxRate = value.TaxRate;
            HsnCode = value.HSNCode;
            Uom = value.UOM;
            Discount = 0; // Reset discount when product changes
            
            // Sync search query with selection name without triggering re-filter
            if (SearchQuery != value.ProductName)
            {
                SearchQuery = value.ProductName;
            }
            
            // Close dropdown on selection
            IsDropDownOpen = false;
        }
    }
}
