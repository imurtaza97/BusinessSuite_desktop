using System.Threading.Tasks;
using BusinessSuite.BLL.Services;
using BusinessSuite.DAL.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BusinessSuite.UI.ViewModels;

public partial class StockAdjustmentViewModel : ViewModelBase
{
    private readonly LedgerService _ledgerService;
    private readonly int _businessId;
    private readonly int _productId;
    private readonly int _warehouseId;

    [ObservableProperty] private string _productName;
    [ObservableProperty] private string _warehouseName;
    [ObservableProperty] private decimal _currentQuantity;
    [ObservableProperty] private decimal _newQuantity;
    [ObservableProperty] private string _reason = "Manual Adjustment";
    [ObservableProperty] private bool _isBusy;

    public StockAdjustmentViewModel(LedgerService ledgerService, int businessId, Product product, Warehouse warehouse, decimal currentQty)
    {
        _ledgerService = ledgerService;
        _businessId = businessId;
        _productId = product.ProductID;
        _warehouseId = warehouse.WarehouseID;
        
        ProductName = product.ProductName;
        WarehouseName = warehouse.WarehouseName;
        CurrentQuantity = currentQty;
        NewQuantity = currentQty;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        IsBusy = true;
        try
        {
            await _ledgerService.UpdateStockAdjustAsync(_businessId, _productId, _warehouseId, NewQuantity, Reason);
            RequestClose?.Invoke(true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke(false);
    }

    public event System.Action<bool>? RequestClose;
}
