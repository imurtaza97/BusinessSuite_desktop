using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using BusinessSuite.DAL.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BusinessSuite.UI.ViewModels;

public partial class WarehouseFormViewModel : ObservableValidator
{
    private readonly int _businessId;
    
    [ObservableProperty] private int _warehouseId;
    [ObservableProperty] private string _title = "Add Warehouse";
    
    [Required(ErrorMessage = "Warehouse Name is required")]
    [MinLength(3, ErrorMessage = "Warehouse Name must be at least 3 characters")]
    [ObservableProperty] private string _warehouseName = string.Empty;
    
    [ObservableProperty] private string? _address;
    [ObservableProperty] private string? _city;
    [ObservableProperty] private string? _state;
    [ObservableProperty] private string? _zipCode;
    [ObservableProperty] private bool _isMainWarehouse;

    public WarehouseFormViewModel(int businessId)
    {
        _businessId = businessId;
    }

    public WarehouseFormViewModel(int businessId, Warehouse warehouse) : this(businessId)
    {
        Title = "Edit Warehouse";
        WarehouseId = warehouse.WarehouseID;
        WarehouseName = warehouse.WarehouseName;
        Address = warehouse.Address;
        City = warehouse.City;
        State = warehouse.State;
        ZipCode = warehouse.ZipCode;
        IsMainWarehouse = warehouse.IsMainWarehouse;
    }

    [RelayCommand]
    private void Save()
    {
        ValidateAllProperties();
        if (HasErrors) return;

        var warehouse = new Warehouse
        {
            WarehouseID = WarehouseId,
            BusinessId = _businessId,
            WarehouseName = WarehouseName,
            Address = Address,
            City = City,
            State = State,
            ZipCode = ZipCode,
            IsMainWarehouse = IsMainWarehouse
        };
        
        RequestClose?.Invoke(warehouse);
    }

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke(null);
    }

    public event System.Action<Warehouse?>? RequestClose;
}
