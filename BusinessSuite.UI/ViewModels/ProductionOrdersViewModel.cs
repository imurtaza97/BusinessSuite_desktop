using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using BusinessSuite.DAL.Data;
using BusinessSuite.DAL.Entities;
using BusinessSuite.DAL.Repositories;
using BusinessSuite.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BusinessSuite.UI.ViewModels;

public partial class ProductionOrdersViewModel : ViewModelBase
{
    private readonly ProductionOrderRepository _repository;
    private readonly int _businessId;

    [ObservableProperty] private ObservableCollection<ProductionOrder> productionOrders = new();
    [ObservableProperty] private ProductionOrder? selectedOrder;
    [ObservableProperty] private string searchQuery = string.Empty;
    [ObservableProperty] private int currentPage = 1;
    [ObservableProperty] private int pageSize = 25;
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private int totalPages;
    [ObservableProperty] private bool isBusy;

    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;

    public event Action<ProductionOrder?>? RequestProductionOrderForm;

    public ProductionOrdersViewModel(int businessId)
    {
        _businessId = businessId;
        _repository = new ProductionOrderRepository(new AppDbContext());
    }

    partial void OnSearchQueryChanged(string value)
    {
        CurrentPage = 1;
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            TotalCount = await _repository.GetCountAsync(_businessId, SearchQuery);
            TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalCount / PageSize));
            if (CurrentPage > TotalPages) CurrentPage = TotalPages;
            if (CurrentPage < 1) CurrentPage = 1;

            var list = await _repository.GetPaginatedAsync(_businessId, CurrentPage, PageSize, SearchQuery);
            ProductionOrders = new ObservableCollection<ProductionOrder>(list);
            OnPropertyChanged(nameof(HasPreviousPage));
            OnPropertyChanged(nameof(HasNextPage));
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand] private async Task RefreshAsync() { CurrentPage = 1; await LoadAsync(); }
    [RelayCommand] private async Task NextPageAsync() { if (HasNextPage) { CurrentPage++; await LoadAsync(); } }
    [RelayCommand] private async Task PreviousPageAsync() { if (HasPreviousPage) { CurrentPage--; await LoadAsync(); } }

    [RelayCommand]
    private void AddOrder() => RequestProductionOrderForm?.Invoke(null);

    [RelayCommand]
    private void EditOrder()
    {
        if (SelectedOrder != null)
            RequestProductionOrderForm?.Invoke(SelectedOrder);
    }

    [RelayCommand]
    private async Task DeleteOrderAsync()
    {
        if (SelectedOrder == null) return;

        if (SelectedOrder.Status == "Completed")
        {
            SetStatusMessage("Completed production orders cannot be deleted.", "#B45309");
            return;
        }

        IsBusy = true;
        try
        {
            var success = await _repository.SoftDeleteAsync(
                SelectedOrder.ProductionOrderID, AppState.Instance.GetCurrentUserId());

            if (success)
            {
                SetStatusMessage("Production order deleted.", "#047857");
                await LoadAsync();
            }
            else
            {
                SetStatusMessage("Failed to delete production order.", "#B45309");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}
