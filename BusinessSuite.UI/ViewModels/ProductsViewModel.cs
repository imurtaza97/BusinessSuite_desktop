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
using BusinessSuite.BLL.Services;

namespace BusinessSuite.UI.ViewModels;

public partial class ProductsViewModel : ViewModelBase
{
    private readonly ProductRepository _productRepository;
    private readonly LedgerService _ledgerService;
    private readonly int _businessId;

    [ObservableProperty] private ObservableCollection<Product> _products = new();
    [ObservableProperty] private ObservableCollection<Stock> _selectedProductWarehouseStock = new();
    [ObservableProperty] private ObservableCollection<Category> _categories = new();
    [ObservableProperty] private Category? _selectedCategory;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditProductCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteProductCommand))]
    private Product? _selectedProduct;

    partial void OnSelectedProductChanged(Product? value)
    {
        if (value != null)
        {
            _ = LoadWarehouseStockAsync(value.ProductID);
        }
        else
        {
            SelectedProductWarehouseStock.Clear();
        }
    }

    private async Task LoadWarehouseStockAsync(int productId)
    {
        var stocks = await _ledgerService.GetProductStockAsync(productId);
        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => {
            SelectedProductWarehouseStock = new ObservableCollection<Stock>(stocks);
        });
    }

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 25;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _totalPages;

    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;

    partial void OnSearchQueryChanged(string value)
    {
        CurrentPage = 1;
        _ = LoadProductsAsync();
    }

    partial void OnSelectedCategoryChanged(Category? value)
    {
        CurrentPage = 1;
        _ = LoadProductsAsync();
    }

    [ObservableProperty]
    private bool _isBusy;

    public ProductsViewModel(int businessId, LedgerService ledgerService)
    {
        var db = new AppDbContext();
        _productRepository = new ProductRepository(db);
        _ledgerService = ledgerService;
        _businessId = businessId;
        
        LoadProductsCommand = new AsyncRelayCommand(LoadProductsAsync);
        AddProductCommand = new AsyncRelayCommand(AddProductAsync);
        EditProductCommand = new AsyncRelayCommand(EditProductAsync, () => SelectedProduct != null);
        DeleteProductCommand = new AsyncRelayCommand(DeleteProductAsync, () => SelectedProduct != null);
        ClearCategoryFilterCommand = new RelayCommand(() => SelectedCategory = null);
        NextPageCommand = new AsyncRelayCommand(NextPageAsync);
        PreviousPageCommand = new AsyncRelayCommand(PreviousPageAsync);

        _ = LoadCategoriesAsync(db);
    }

    public IAsyncRelayCommand NextPageCommand { get; }
    public IAsyncRelayCommand PreviousPageCommand { get; }

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
            TotalCount = await _productRepository.GetCountAsync(_businessId, SearchQuery, SelectedCategory?.CategoryID);
            TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);

            if (CurrentPage > TotalPages && TotalPages > 0) CurrentPage = TotalPages;
            if (CurrentPage < 1) CurrentPage = 1;

            var products = await _productRepository.GetPaginatedAsync(_businessId, CurrentPage, PageSize, SearchQuery, SelectedCategory?.CategoryID);
            Products = new ObservableCollection<Product>(products);

            OnPropertyChanged(nameof(HasPreviousPage));
            OnPropertyChanged(nameof(HasNextPage));
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
            await LoadProductsAsync();
        }
    }

    private async Task PreviousPageAsync()
    {
        if (HasPreviousPage)
        {
            CurrentPage--;
            await LoadProductsAsync();
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
                    bool success;
                    if (vm.StockQty > 0 && vm.SelectedWarehouse != null)
                    {
                        success = await _ledgerService.AddProductWithStockAsync(result, vm.SelectedWarehouse.WarehouseID, vm.StockQty);
                    }
                    else
                    {
                        success = await _productRepository.AddAsync(result);
                    }

                    if (success)
                    {
                        await LoadProductsAsync();
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
                        await LoadProductsAsync();
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
                await LoadProductsAsync();
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
