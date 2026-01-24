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

        if (Category?.Length > 50)
            AddError(nameof(Category), "Category cannot exceed 50 characters");

        if (PurchasePrice <= 0)
            AddError(nameof(PurchasePrice), "Purchase Price must be greater than zero");

        if (SalePrice <= 0)
            AddError(nameof(SalePrice), "Sale Price must be greater than zero");

        if (StockQty < 0)
            AddError(nameof(StockQty), "Stock Quantity cannot be negative");

        if (TaxRate < 0)
            AddError(nameof(TaxRate), "Tax Rate is required");

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

    private string? _productHsnCode;
    public string? ProductHsnCode
    {
        get => _productHsnCode;
        set 
        {
            if (SetProperty(ref _productHsnCode, value))
            {
                if (ValidationVisible) ValidateAll();
            }
        }
    }

    private string? _category;
    public string? Category
    {
        get => _category;
        set 
        {
            if (SetProperty(ref _category, value))
            {
                if (ValidationVisible) ValidateAll();
            }
        }
    }

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

    private int _stockQty;
    public int StockQty
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

    private string? _uom;
    public string? Uom
    {
        get => _uom;
        set 
        {
            if (SetProperty(ref _uom, value))
            {
                if (ValidationVisible) ValidateAll();
            }
        }
    }

    public ObservableCollection<decimal> AvailableTaxRates { get; } = new();

    public ProductFormViewModel(int businessId)
    {
        _businessId = businessId;
        var db = new AppDbContext();
        _gstRateRepository = new GstRateRepository(db);
        
        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(Cancel);
        
        _ = LoadRatesAsync();
    }

    public ProductFormViewModel(int businessId, Product product) : this(businessId)
    {
        Title = "Edit Product";
        ProductId = product.ProductID;
        ProductName = product.ProductName;
        ProductSku = product.SKU;
        ProductHsnCode = product.HSNCode;
        Category = product.Category;
        PurchasePrice = product.PurchasePrice;
        SalePrice = product.SalePrice;
        StockQty = product.StockQty;
        TaxRate = product.TaxRate;
        Uom = product.UOM;
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

    public IRelayCommand SaveCommand { get; }
    public IRelayCommand CancelCommand { get; }

    public event Action<Product?>? RequestClose;

    private void Save()
    {
        ValidationVisible = true;
        ValidateAll();
        
        foreach (var propertyName in new[] { nameof(ProductName), nameof(PurchasePrice), nameof(SalePrice), nameof(TaxRate), nameof(StockQty) })
        {
             ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        if (HasErrors)
        {
            GeneralErrorMessage = "Please correct the errors before saving.";
            return;
        }

        var product = new Product
        {
            ProductID = ProductId,
            BusinessID = _businessId,
            ProductName = ProductName,
            SKU = ProductSku,
            HSNCode = ProductHsnCode,
            Category = Category,
            PurchasePrice = PurchasePrice,
            SalePrice = SalePrice,
            StockQty = StockQty,
            TaxRate = TaxRate,
            UOM = Uom
        };

        RequestClose?.Invoke(product);
    }

    private void Cancel()
    {
        RequestClose?.Invoke(null);
    }
}
