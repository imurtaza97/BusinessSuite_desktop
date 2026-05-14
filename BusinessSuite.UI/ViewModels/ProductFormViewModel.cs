using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using BusinessSuite.DAL.Data;
using BusinessSuite.DAL.Entities;
using BusinessSuite.DAL.Repositories;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading;

namespace BusinessSuite.UI.ViewModels;

public partial class ProductFormViewModel : ViewModelBase, INotifyDataErrorInfo
{
    private readonly GstRateRepository _gstRateRepository;
    private readonly CategoryRepository _categoryRepository;
    private readonly VendorRepository _vendorRepository;
    private readonly WarehouseRepository _warehouseRepository;
    private readonly Dictionary<string, List<string>> _errors = new();
    private readonly int _businessId;
    
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
        
        if (string.IsNullOrWhiteSpace(ProductName))
            AddError(nameof(ProductName), "Product Name is required");
        else if (ProductName.Length > 100)
            AddError(nameof(ProductName), "Product Name cannot exceed 100 characters");

        if (ProductSku?.Length > 30)
            AddError(nameof(ProductSku), "SKU cannot exceed 30 characters");

        if (ProductHsnCode?.Length > 20)
            AddError(nameof(ProductHsnCode), "HSN Code cannot exceed 20 characters");

        if (SelectedCategory == null)
            AddError(nameof(SelectedCategory), "Category is required");

        // Purchase price required only for products or external services
        if (!IsService || !IsInternalService)
        {
            if (PurchasePrice < 0)
                AddError(nameof(PurchasePrice), "Purchase Price cannot be negative");
            else if (!IsService && PurchasePrice <= 0)
                AddError(nameof(PurchasePrice), "Purchase Price must be greater than zero");
        }

        if (SalePrice <= 0)
            AddError(nameof(SalePrice), "Sale Price must be greater than zero");

        if (!IsService)
        {
            if (StockQty < 0)
                AddError(nameof(StockQty), "Stock Quantity cannot be negative");

            if (StockQty > 0 && SelectedWarehouse == null)
                AddError(nameof(SelectedWarehouse), "Warehouse is required for initial stock");
        }

        if (TaxRate < 0)
            AddError(nameof(TaxRate), "Tax Rate is required");

        if (string.IsNullOrWhiteSpace(Unit))
            AddError(nameof(Unit), "Unit (UOM) is required");

        if (StockQty > 0 && SelectedWarehouse == null)
            AddError(nameof(SelectedWarehouse), "Warehouse is required for initial stock");

        OnPropertyChanged(nameof(HasErrors));
    }

    [ObservableProperty]
    private string _generalErrorMessage = string.Empty;
    
    [ObservableProperty]
    private bool _validationVisible = false;
    [ObservableProperty]
    private int _productId;

    [ObservableProperty]
    private string _title = "Add Product";

    [ObservableProperty]
    private bool _isService;

    [ObservableProperty]
    private bool _isInternalService = true;

    public bool IsProduct => !IsService;

    public bool ShowVendorAndCost => !IsService || !IsInternalService;

    public string SaveButtonText => IsService ? "Save Service" : "Save Product";

    public string CodeLabel => IsService ? "SAC Code" : "HSN Code";
    public string CodeWatermark => IsService ? "Enter SAC code" : "Enter HSN code";
    public string SkuLabel => IsService ? "Service Code" : "SKU";
    public string SkuWatermark => IsService ? "Enter service code" : "SKU code";
    public string CategoryLabel => IsService ? "Service Category" : "Category";
    public string PreferredVendorLabel => IsService ? "Service Provider" : "Preferred Vendor";
    public string PurchasePriceLabel => IsService ? "Service Cost *" : "Purchase Price *";
    public string SalePriceLabel => IsService ? "Service Fee *" : "Sale Price *";

    partial void OnIsServiceChanged(bool value)
    {
        Title = value ? (ProductId == 0 ? "Add Service" : "Edit Service") : (ProductId == 0 ? "Add Product" : "Edit Product");
        OnPropertyChanged(nameof(IsProduct));
        OnPropertyChanged(nameof(SaveButtonText));
        OnPropertyChanged(nameof(CodeLabel));
        OnPropertyChanged(nameof(CodeWatermark));
        OnPropertyChanged(nameof(SkuLabel));
        OnPropertyChanged(nameof(SkuWatermark));
        OnPropertyChanged(nameof(CategoryLabel));
        OnPropertyChanged(nameof(PreferredVendorLabel));
        OnPropertyChanged(nameof(PurchasePriceLabel));
        OnPropertyChanged(nameof(SalePriceLabel));
        OnPropertyChanged(nameof(ShowVendorAndCost));
        if (value)
        {
            StockQty = 0;
            SelectedWarehouse = null;
            if (IsInternalService)
            {
                SelectedPreferredVendor = null;
                PurchasePrice = 0;
            }
        }
        if (ValidationVisible) ValidateAll();
    }

    partial void OnIsInternalServiceChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowVendorAndCost));
        if (value && IsService)
        {
            SelectedPreferredVendor = null;
            PurchasePrice = 0;
        }
        if (ValidationVisible) ValidateAll();
    }

    [ObservableProperty]
    private bool _isGstRegistered;

    private string _productName = string.Empty;
    public string ProductName
    {
        get => _productName;
        set 
        {
            if (SetProperty(ref _productName, value))
            {
                if (ValidationVisible) ValidateAll();
            }
        }
    }

    private string? _productSku;
    public string? ProductSku
    {
        get => _productSku;
        set 
        {
            if (SetProperty(ref _productSku, value))
            {
                if (ValidationVisible) ValidateAll();
            }
        }
    }

    [ObservableProperty] private ObservableCollection<Category> _categories = new();
    [ObservableProperty] private Category? _selectedCategory;
    [ObservableProperty] private ObservableCollection<Vendor> _vendors = new();
    [ObservableProperty] private Vendor? _selectedPreferredVendor;

    [ObservableProperty] private ObservableCollection<Warehouse> _warehouses = new();
    [ObservableProperty] private Warehouse? _selectedWarehouse;

    partial void OnSelectedCategoryChanged(Category? value)
    {
        if (ValidationVisible) ValidateAll();
    }

    [ObservableProperty] private string? _productHsnCode;

    private decimal _purchasePrice;
    public decimal PurchasePrice
    {
        get => _purchasePrice;
        set 
        {
            if (SetProperty(ref _purchasePrice, value))
            {
                if (ValidationVisible) ValidateAll();
            }
        }
    }

    private decimal _salePrice;
    public decimal SalePrice
    {
        get => _salePrice;
        set 
        {
            if (SetProperty(ref _salePrice, value))
            {
                if (ValidationVisible) ValidateAll();
            }
        }
    }

    private decimal _stockQty;
    public decimal StockQty
    {
        get => _stockQty;
        set 
        {
            if (SetProperty(ref _stockQty, value))
            {
                if (ValidationVisible) ValidateAll();
            }
        }
    }

    private decimal _taxRate = 18;
    public decimal TaxRate
    {
        get => _taxRate;
        set 
        {
            if (SetProperty(ref _taxRate, value))
            {
                if (ValidationVisible) ValidateAll();
            }
        }
    }


    private string? _unit;
    public string? Unit
    {
        get => _unit;
        set 
        {
            var sanitized = value;
            if (sanitized != null && sanitized.Contains("BusinessSuite.DAL.Entities")) sanitized = "nos";

            if (SetProperty(ref _unit, sanitized))
            {
                if (ValidationVisible) ValidateAll();
            }
        }
    }

    public ObservableCollection<UnitOfMeasure> Units { get; } = new();
    public ObservableCollection<string> UnitNames { get; } = new();

    public ObservableCollection<decimal> AvailableTaxRates { get; } = new();


    public ProductFormViewModel(int businessId)
    {
        _businessId = businessId;
        var db = new AppDbContext();
        _gstRateRepository = new GstRateRepository(db);
        _categoryRepository = new CategoryRepository(db);
        _vendorRepository = new VendorRepository(db);
        _warehouseRepository = new WarehouseRepository(db);

        SaveCommand = new RelayCommand(Save);
        SaveDraftCommand = new AsyncRelayCommand(SaveDraftAsync);
        CancelCommand = new RelayCommand(Cancel);
        AddCategoryCommand = new RelayCommand(AddCategory);
        LoadUnits(db);
        
        // Load business GST status
        var business = db.Businesses.Find(businessId);
        IsGstRegistered = business?.IsGSTRegistered ?? false;

        _ = LoadRatesAsync();
        _ = LoadCategoriesVendorsAndWarehousesAsync();
        Unit = "nos";
    }

    public IRelayCommand AddCategoryCommand { get; }
    [ObservableProperty] private string _newCategoryName = string.Empty;

    private async void AddCategory()
    {
        if (string.IsNullOrWhiteSpace(NewCategoryName)) return;
        
        var category = new Category 
        { 
            BusinessID = _businessId, 
            Name = NewCategoryName 
        };
        
        if (await _categoryRepository.AddAsync(category))
        {
            Categories.Add(category);
            SelectedCategory = category;
            NewCategoryName = string.Empty;
        }
    }

    private async Task LoadCategoriesVendorsAndWarehousesAsync()
    {
        var categories = await _categoryRepository.GetAllAsync(_businessId);
        var vendors = await _vendorRepository.GetAllAsync(_businessId);
        var warehouses = await _warehouseRepository.GetAllAsync(_businessId);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Categories.Clear();
            foreach (var c in categories) Categories.Add(c);
            
            Vendors.Clear();
            foreach (var v in vendors) Vendors.Add(v);

            Warehouses.Clear();
            foreach (var w in warehouses) Warehouses.Add(w);
            SelectedWarehouse = Warehouses.FirstOrDefault(w => w.IsMainWarehouse) ?? Warehouses.FirstOrDefault();

            // Re-select if editing
            if (_pendingCategoryId.HasValue)
                SelectedCategory = Categories.FirstOrDefault(c => c.CategoryID == _pendingCategoryId);
            if (_pendingVendorId.HasValue)
                SelectedPreferredVendor = Vendors.FirstOrDefault(v => v.VendorID == _pendingVendorId);
        });
    }

    private int? _pendingCategoryId;
    private int? _pendingVendorId;

    private void LoadUnits(AppDbContext db)
    {
        Units.Clear();
        UnitNames.Clear();
        var units = db.UnitsOfMeasure.Where(u => u.BusinessId == 0 || u.BusinessId == _businessId).ToList();
        foreach (var u in units) 
        {
            Units.Add(u);
            UnitNames.Add(u.Name);
        }
    }

    public ProductFormViewModel(int businessId, Product product) : this(businessId)
    {
        Title = "Edit Product";
        ProductId = product.ProductID;
        ProductName = product.ProductName;
        ProductSku = product.SKU;
        ProductHsnCode = product.HSNCode;
        _pendingCategoryId = product.CategoryID;
        _pendingVendorId = product.PreferredVendorID;
        PurchasePrice = product.PurchasePrice;
        SalePrice = product.SalePrice;
        StockQty = product.StockQty;
        TaxRate = product.TaxRate;
        Unit = product.Unit;
        IsService = product.IsService;
        IsInternalService = product.IsInternalService;
    }

    private async Task LoadRatesAsync()
    {
        try
        {
            var rates = await _gstRateRepository.GetAllPercentagesAsync();
            
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var capturedTaxRate = TaxRate;
                AvailableTaxRates.Clear();
                if (rates != null && rates.Any())
                {
                    foreach (var rate in rates)
                    {
                        AvailableTaxRates.Add(rate);
                    }
                }
                else
                {
                    // Fallback to standard rates
                    var standardRates = new decimal[] { 0, 5, 12, 18, 28 };
                    foreach (var rate in standardRates)
                    {
                        AvailableTaxRates.Add(rate);
                    }
                }

                if (!AvailableTaxRates.Contains(capturedTaxRate))
                {
                    AvailableTaxRates.Add(capturedTaxRate);
                    var sorted = AvailableTaxRates.OrderBy(r => r).ToList();
                    AvailableTaxRates.Clear();
                    foreach(var r in sorted) AvailableTaxRates.Add(r);
                }
                
                TaxRate = capturedTaxRate == 0 ? 18 : capturedTaxRate;
                OnPropertyChanged(nameof(TaxRate));
            });
        }
        catch 
        {
            // Fallback
            foreach (var rate in new decimal[] { 0, 5, 12, 18, 28 })
                AvailableTaxRates.Add(rate);
        }
    }

    public IAsyncRelayCommand SaveDraftCommand { get; }
    public IRelayCommand SaveCommand { get; }
    public IRelayCommand CancelCommand { get; }

    public event Action<Product?>? RequestClose;

    private Task SaveDraftAsync()
    {
        ValidationVisible = true;
        ClearAllErrors();

        if (string.IsNullOrWhiteSpace(ProductName))
            AddError(nameof(ProductName), "Product Name is required");

        if (HasErrors)
        {
            GeneralErrorMessage = _errors.Values.SelectMany(e => e).Distinct().FirstOrDefault() ?? string.Empty;
            OnPropertyChanged(nameof(HasErrors));
            return Task.CompletedTask;
        }

        var product = new Product
        {
            ProductID = ProductId,
            BusinessID = _businessId,
            ProductName = ProductName,
            SKU = ProductSku,
            HSNCode = ProductHsnCode,
            CategoryID = SelectedCategory?.CategoryID,
            PreferredVendorID = SelectedPreferredVendor?.VendorID,
            PurchasePrice = PurchasePrice,
            SalePrice = SalePrice,
            StockQty = 0,
            TaxRate = TaxRate,
            Unit = Unit,
            IsDraft = true,
            IsService = IsService
        };

        RequestClose?.Invoke(product);
        return Task.CompletedTask;
    }

    private void Save()
    {
        ValidationVisible = true;
        ValidateAll();
        
        foreach (var propertyName in new[] { nameof(ProductName), nameof(PurchasePrice), nameof(SalePrice), nameof(TaxRate), nameof(StockQty), nameof(Unit) })
        {
             ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        if (HasErrors)
        {
            var allErrors = _errors.Values.SelectMany(e => e).Distinct().ToList();
            GeneralErrorMessage = string.Join(", ", allErrors);
            return;
        }

        var product = new Product
        {
            ProductID = ProductId,
            BusinessID = _businessId,
            ProductName = ProductName,
            SKU = ProductSku,
            HSNCode = ProductHsnCode,
            CategoryID = SelectedCategory?.CategoryID,
            PreferredVendorID = SelectedPreferredVendor?.VendorID,
            PurchasePrice = PurchasePrice,
            SalePrice = SalePrice,
            StockQty = IsService ? 0 : StockQty,
            TaxRate = TaxRate,
            Unit = Unit,
            IsDraft = false,
            IsService = IsService,
            IsInternalService = IsInternalService
        };

        RequestClose?.Invoke(product);
    }

    private void Cancel()
    {
        RequestClose?.Invoke(null);
    }
}
