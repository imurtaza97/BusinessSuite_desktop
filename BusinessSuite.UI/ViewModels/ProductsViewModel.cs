using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BusinessSuite.DAL.Data;
using BusinessSuite.DAL.Entities;
using BusinessSuite.DAL.Repositories;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BusinessSuite.UI.ViewModels;

public partial class ProductsViewModel : ViewModelBase
{
    private readonly ProductRepository _productRepository;
    private readonly int _businessId;

    [ObservableProperty]
    private ObservableCollection<Product> _products = new();

    [ObservableProperty]
    private ObservableCollection<Category> _categories = new();

    [ObservableProperty]
    private Category? _selectedCategory;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditProductCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteProductCommand))]
    private Product? _selectedProduct;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    private List<Product> _allProducts = new();

    partial void OnSearchQueryChanged(string value) => ApplyFilter();
    partial void OnSelectedCategoryChanged(Category? value) => ApplyFilter();

    private void ApplyFilter()
    {
        var filtered = _allProducts.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            var query = SearchQuery.ToLower();
            filtered = filtered.Where(p => 
                (p.ProductName?.ToLower().Contains(query) ?? false) || 
                (p.SKU?.ToLower().Contains(query) ?? false));
        }

        if (SelectedCategory != null)
        {
            filtered = filtered.Where(p => p.CategoryID == SelectedCategory.CategoryID);
        }

        Products = new ObservableCollection<Product>(filtered);
    }

    [ObservableProperty]
    private bool _isBusy;

    public ProductsViewModel(int businessId)
    {
        var db = new AppDbContext();
        _productRepository = new ProductRepository(db);
        _businessId = businessId;
        
        LoadProductsCommand = new AsyncRelayCommand(LoadProductsAsync);
        AddProductCommand = new AsyncRelayCommand(AddProductAsync);
        EditProductCommand = new AsyncRelayCommand(EditProductAsync, () => SelectedProduct != null);
        DeleteProductCommand = new AsyncRelayCommand(DeleteProductAsync, () => SelectedProduct != null);
        ClearCategoryFilterCommand = new RelayCommand(() => SelectedCategory = null);

        _ = LoadCategoriesAsync(db);
    }

    public IRelayCommand ClearCategoryFilterCommand { get; }

    private async Task LoadCategoriesAsync(AppDbContext db)
    {
        var repo = new CategoryRepository(db);
        var categories = await repo.GetAllAsync(_businessId);
        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            Categories.Clear();
            foreach (var c in categories) Categories.Add(c);
        });
    }

    public IAsyncRelayCommand LoadProductsCommand { get; }
    public IAsyncRelayCommand AddProductCommand { get; }
    public IAsyncRelayCommand EditProductCommand { get; }
    public IAsyncRelayCommand DeleteProductCommand { get; }

    private async Task LoadProductsAsync()
    {
        IsBusy = true;
        try
        {
            var products = await _productRepository.GetAllAsync(_businessId);
            _allProducts = products.ToList();
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(ApplyFilter);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task AddProductAsync()
    {
        var vm = new ProductFormViewModel(_businessId);
        var dialog = new Views.ProductFormWindow { DataContext = vm };
        
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var result = await dialog.ShowDialog<Product?>(desktop.MainWindow!);
            if (result != null)
            {
                result.BusinessID = _businessId;
                IsBusy = true;
                try
                {
                    var success = await _productRepository.AddAsync(result);
                    if (success)
                    {
                        _allProducts.Insert(0, result);
                        ApplyFilter();
                    }
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }
    }

    private async Task EditProductAsync()
    {
        if (SelectedProduct == null) return;
        
        var vm = new ProductFormViewModel(_businessId, SelectedProduct);
        var dialog = new Views.ProductFormWindow { DataContext = vm };
        
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var result = await dialog.ShowDialog<Product?>(desktop.MainWindow!);
            if (result != null)
            {
                result.BusinessID = _businessId;
                result.ProductID = SelectedProduct.ProductID; // Ensure ID is preserved
                
                IsBusy = true;
                try
                {
                    var success = await _productRepository.UpdateAsync(result);
                    if (success)
                    {
                        var masterIndex = _allProducts.FindIndex(p => p.ProductID == result.ProductID);
                        if (masterIndex >= 0) _allProducts[masterIndex] = result;
                        
                        ApplyFilter();
                        SelectedProduct = result;
                    }
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }
    }

    private async Task DeleteProductAsync()
    {
        if (SelectedProduct == null) return;

        // We will implement the actual dialog call once the Window is created
        // For now, I'll prepare the logic flow
        bool confirmed = await ShowConfirmDeleteDialog();
        if (!confirmed) return;
        
        IsBusy = true;
        try
        {
            var success = await _productRepository.DeleteAsync(SelectedProduct.ProductID);
            if (success)
            {
                var productToRemove = _allProducts.FirstOrDefault(p => p.ProductID == SelectedProduct.ProductID);
                if (productToRemove != null) _allProducts.Remove(productToRemove);
                
                ApplyFilter();
                SelectedProduct = null;
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task<bool> ShowConfirmDeleteDialog()
    {
        var dialog = new Views.ConfirmDeleteWindow();
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return await dialog.ShowDialog<bool>(desktop.MainWindow!);
        }
        return false;
    }
}
