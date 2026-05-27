using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BusinessSuite.BLL.Services;
using BusinessSuite.DAL.Data;
using BusinessSuite.DAL.Entities;
using BusinessSuite.DAL.Repositories;
using BusinessSuite.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;

namespace BusinessSuite.UI.ViewModels;

public partial class ProductionOrderFormViewModel : ObservableObject
{
    private readonly ProductionOrderRepository _orderRepository;
    private readonly ManufacturingService _manufacturingService;
    private readonly int _businessId;
    private int? _orderId;

    public static readonly string[] StatusOptions = { "Pending", "In-Progress", "On-Hold", "Completed", "Cancelled" };

    [ObservableProperty] private string title = "New Production Order";
    [ObservableProperty] private string productionOrderNumber = string.Empty;
    [ObservableProperty] private ObservableCollection<Product> products = new();
    [ObservableProperty] private Product? selectedProduct;
    [ObservableProperty] private ObservableCollection<Warehouse> warehouses = new();
    [ObservableProperty] private Warehouse? selectedWarehouse;
    [ObservableProperty] private decimal quantityToMake = 1;
    [ObservableProperty] private string unitOfMeasure = "nos";
    [ObservableProperty] private DateTime? startDate = DateTime.Today;
    [ObservableProperty] private DateTime? expectedEndDate = DateTime.Today.AddDays(7);
    [ObservableProperty] private string status = "Pending";
    [ObservableProperty] private string? notes;
    [ObservableProperty] private decimal estimatedCost;
    [ObservableProperty] private decimal actualCost;
    [ObservableProperty] private decimal quantityCompleted;
    [ObservableProperty] private ObservableCollection<BomRequirementLine> materialRequirements = new();
    [ObservableProperty] private bool hasMaterialRequirements;
    [ObservableProperty] private bool canEdit = true;
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private string? errorMessage;

    public bool CanStart => CanEdit && Status == "Pending";
    public bool CanComplete => CanEdit && (Status == "Pending" || Status == "In-Progress");
    public bool CanCancel => CanEdit && Status != "Completed" && Status != "Cancelled";
    public bool IsClosed => Status is "Completed" or "Cancelled";

    public ProductionOrderFormViewModel(int businessId, ManufacturingService manufacturingService, int? orderId = null)
    {
        _businessId = businessId;
        _orderId = orderId;
        _manufacturingService = manufacturingService;
        _orderRepository = new ProductionOrderRepository(new AppDbContext());
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            await using var db = new AppDbContext();
            var productList = await db.Products
                .Where(p => p.BusinessID == _businessId && !p.IsDraft && !p.IsService)
                .OrderBy(p => p.ProductName)
                .ToListAsync();
            Products = new ObservableCollection<Product>(productList);

            var warehouseList = await db.Warehouses
                .Where(w => w.BusinessId == _businessId)
                .OrderBy(w => w.WarehouseName)
                .ToListAsync();
            Warehouses = new ObservableCollection<Warehouse>(warehouseList);
            SelectedWarehouse = Warehouses.FirstOrDefault(w => w.IsMainWarehouse) ?? Warehouses.FirstOrDefault();

            if (_orderId.HasValue)
                await LoadExistingAsync(_orderId.Value);
            else
                await StartNewAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task StartNewAsync()
    {
        ProductionOrderNumber = await _orderRepository.GetNextProductionOrderNumberAsync(_businessId);
        Status = "Pending";
        CanEdit = true;
        Title = "New Production Order";
    }

    private async Task LoadExistingAsync(int orderId)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null)
        {
            ErrorMessage = "Production order not found";
            return;
        }

        Title = $"Production Order - {order.ProductionOrderNumber}";
        ProductionOrderNumber = order.ProductionOrderNumber;
        SelectedProduct = Products.FirstOrDefault(p => p.ProductID == order.ProductID);
        QuantityToMake = order.QuantityToMake;
        UnitOfMeasure = order.UnitOfMeasure;
        StartDate = order.StartDate;
        ExpectedEndDate = order.ExpectedEndDate;
        Status = order.Status;
        Notes = order.Notes;
        EstimatedCost = order.EstimatedCost;
        ActualCost = order.ActualCost;
        QuantityCompleted = order.QuantityCompleted;
        CanEdit = order.Status is not "Completed" and not "Cancelled";

        await RefreshRequirementsAsync();
        NotifyActionStates();
    }

    partial void OnSelectedProductChanged(Product? value)
    {
        if (value != null)
            UnitOfMeasure = value.Unit ?? "nos";
        _ = RefreshRequirementsAsync();
    }

    partial void OnSelectedWarehouseChanged(Warehouse? value) => _ = RefreshRequirementsAsync();

    partial void OnQuantityToMakeChanged(decimal value) => _ = RefreshRequirementsAsync();

    private async Task RefreshRequirementsAsync()
    {
        if (SelectedProduct == null || SelectedWarehouse == null || QuantityToMake <= 0)
        {
            MaterialRequirements.Clear();
            HasMaterialRequirements = false;
            EstimatedCost = 0;
            return;
        }

        MaterialRequirements = new ObservableCollection<BomRequirementLine>(
            await _manufacturingService.GetMaterialRequirementsAsync(
                SelectedProduct.ProductID, QuantityToMake, SelectedWarehouse.WarehouseID));

        HasMaterialRequirements = MaterialRequirements.Any();
        EstimatedCost = await _manufacturingService.EstimateProductionCostAsync(
            SelectedProduct.ProductID, QuantityToMake);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;

        if (SelectedProduct == null)
        {
            ErrorMessage = "Select a product to manufacture";
            return;
        }

        if (SelectedWarehouse == null)
        {
            ErrorMessage = "Select a warehouse";
            return;
        }

        if (QuantityToMake <= 0)
        {
            ErrorMessage = "Quantity must be greater than zero";
            return;
        }

        if (!MaterialRequirements.Any())
        {
            ErrorMessage = "No BOM defined for this product. Add bill of materials first.";
            return;
        }

        if (MaterialRequirements.Any(m => !m.HasSufficientStock) && Status != "Completed")
        {
            ErrorMessage = "Insufficient raw material stock for one or more BOM items";
            return;
        }

        IsLoading = true;
        try
        {
            await RefreshRequirementsAsync();
            var userId = AppState.Instance.GetCurrentUserId();
            bool success;

            if (_orderId.HasValue)
            {
                var order = await _orderRepository.GetByIdAsync(_orderId.Value);
                if (order == null)
                {
                    ErrorMessage = "Production order not found";
                    return;
                }

                order.ProductID = SelectedProduct.ProductID;
                order.QuantityToMake = QuantityToMake;
                order.UnitOfMeasure = UnitOfMeasure;
                order.StartDate = StartDate ?? DateTime.Today;
                order.ExpectedEndDate = ExpectedEndDate ?? DateTime.Today.AddDays(7);
                order.Notes = Notes;
                order.EstimatedCost = EstimatedCost;
                order.ModifiedAt = DateTime.Now;
                order.ModifiedByUserID = userId;
                success = await _orderRepository.UpdateAsync(order);
            }
            else
            {
                var order = new ProductionOrder
                {
                    BusinessID = _businessId,
                    ProductID = SelectedProduct.ProductID,
                    ProductionOrderNumber = ProductionOrderNumber,
                    QuantityToMake = QuantityToMake,
                    UnitOfMeasure = UnitOfMeasure,
                    StartDate = StartDate ?? DateTime.Today,
                    ExpectedEndDate = ExpectedEndDate ?? DateTime.Today.AddDays(7),
                    Status = "Pending",
                    Notes = Notes,
                    EstimatedCost = EstimatedCost,
                    CreatedByUserID = userId
                };
                success = await _orderRepository.AddAsync(order);
                if (success)
                {
                    _orderId = order.ProductionOrderID;
                    Title = $"Production Order - {order.ProductionOrderNumber}";
                    NotifyActionStates();
                }
            }

            if (success)
                ErrorMessage = null;
            else
                ErrorMessage = "Failed to save production order";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task StartProductionAsync()
    {
        if (!_orderId.HasValue)
        {
            ErrorMessage = "Save the production order first";
            return;
        }

        var order = await _orderRepository.GetByIdAsync(_orderId.Value);
        if (order == null) return;

        order.Status = "In-Progress";
        order.ModifiedAt = DateTime.Now;
        order.ModifiedByUserID = AppState.Instance.GetCurrentUserId();
        await _orderRepository.UpdateAsync(order);
        Status = "In-Progress";
        NotifyActionStates();
    }

    [RelayCommand]
    private async Task CompleteProductionAsync()
    {
        if (!_orderId.HasValue || SelectedWarehouse == null)
        {
            ErrorMessage = "Save the order and select a warehouse first";
            return;
        }

        IsLoading = true;
        try
        {
            var order = await _orderRepository.GetByIdAsync(_orderId.Value);
            if (order == null)
            {
                ErrorMessage = "Production order not found";
                return;
            }

            order.QuantityToMake = QuantityToMake;
            await _manufacturingService.ProcessProductionCompletionAsync(
                order, SelectedWarehouse.WarehouseID, AppState.Instance.GetCurrentUserId());

            Status = order.Status;
            ActualCost = order.ActualCost;
            QuantityCompleted = order.QuantityCompleted;
            CanEdit = false;
            NotifyActionStates();
            ErrorMessage = null;
            await RefreshRequirementsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CancelOrderAsync()
    {
        if (!_orderId.HasValue) return;

        var order = await _orderRepository.GetByIdAsync(_orderId.Value);
        if (order == null) return;

        order.Status = "Cancelled";
        order.ModifiedAt = DateTime.Now;
        order.ModifiedByUserID = AppState.Instance.GetCurrentUserId();
        await _orderRepository.UpdateAsync(order);
        Status = "Cancelled";
        CanEdit = false;
        NotifyActionStates();
    }

    [RelayCommand]
    private void Close() => RequestClose?.Invoke();

    private void NotifyActionStates()
    {
        OnPropertyChanged(nameof(CanStart));
        OnPropertyChanged(nameof(CanComplete));
        OnPropertyChanged(nameof(CanCancel));
        OnPropertyChanged(nameof(IsClosed));
    }

    public event Action? RequestClose;
}
