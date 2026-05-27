using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BusinessSuite.DAL.Data;
using BusinessSuite.DAL.Entities;
using BusinessSuite.DAL.Repositories;
using BusinessSuite.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;

namespace BusinessSuite.UI.ViewModels;

public partial class BomFormViewModel : ObservableObject
{
    private readonly BillOfMaterialsRepository _bomRepository;
    private readonly int _businessId;
    private readonly int? _bomId;

    [ObservableProperty] private string title = "Add BOM Line";
    [ObservableProperty] private ObservableCollection<Product> finishedProducts = new();
    [ObservableProperty] private ObservableCollection<Product> rawMaterials = new();
    [ObservableProperty] private ObservableCollection<string> unitNames = new();
    [ObservableProperty] private Product? selectedFinishedProduct;
    [ObservableProperty] private Product? selectedRawMaterial;
    [ObservableProperty] private decimal quantity = 1;
    [ObservableProperty] private string unitOfMeasure = "nos";
    [ObservableProperty] private decimal wastagePercentage;
    [ObservableProperty] private string? notes;
    [ObservableProperty] private string? errorMessage;

    public BomFormViewModel(int businessId, int? bomId = null)
    {
        _businessId = businessId;
        _bomId = bomId;
        var db = new AppDbContext();
        _bomRepository = new BillOfMaterialsRepository(db);
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        var db = new AppDbContext();
        var products = await db.Products
            .Where(p => p.BusinessID == _businessId && !p.IsDraft && !p.IsService)
            .OrderBy(p => p.ProductName)
            .ToListAsync();

        FinishedProducts = new ObservableCollection<Product>(products);
        RawMaterials = new ObservableCollection<Product>(products);

        var units = await db.UnitsOfMeasure.OrderBy(u => u.Name).ToListAsync();
        UnitNames = new ObservableCollection<string>(units.Select(u => u.Name));

        if (_bomId.HasValue)
        {
            Title = "Edit BOM Line";
            var bom = await _bomRepository.GetByIdAsync(_bomId.Value);
            if (bom == null)
            {
                ErrorMessage = "BOM line not found";
                return;
            }

            SelectedFinishedProduct = FinishedProducts.FirstOrDefault(p => p.ProductID == bom.FinishedProductID);
            SelectedRawMaterial = RawMaterials.FirstOrDefault(p => p.ProductID == bom.RawMaterialProductID);
            Quantity = bom.Quantity;
            UnitOfMeasure = bom.UnitOfMeasure;
            WastagePercentage = bom.WastagePercentage;
            Notes = bom.Notes;
        }
    }

    partial void OnSelectedFinishedProductChanged(Product? value)
    {
        if (value != null && string.IsNullOrWhiteSpace(UnitOfMeasure))
            UnitOfMeasure = value.Unit ?? "nos";
    }

    partial void OnSelectedRawMaterialChanged(Product? value)
    {
        if (value != null && _bomId == null)
            UnitOfMeasure = value.Unit ?? UnitOfMeasure;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;

        if (SelectedFinishedProduct == null)
        {
            ErrorMessage = "Select a finished product";
            return;
        }

        if (SelectedRawMaterial == null)
        {
            ErrorMessage = "Select a raw material";
            return;
        }

        if (SelectedFinishedProduct.ProductID == SelectedRawMaterial.ProductID)
        {
            ErrorMessage = "Finished product and raw material cannot be the same";
            return;
        }

        if (Quantity <= 0)
        {
            ErrorMessage = "Quantity must be greater than zero";
            return;
        }

        if (WastagePercentage < 0 || WastagePercentage > 100)
        {
            ErrorMessage = "Wastage must be between 0 and 100";
            return;
        }

        if (await _bomRepository.ExistsAsync(
                _businessId,
                SelectedFinishedProduct.ProductID,
                SelectedRawMaterial.ProductID,
                _bomId))
        {
            ErrorMessage = "This raw material is already in the BOM for this product";
            return;
        }

        var userId = AppState.Instance.GetCurrentUserId();
        bool success;

        if (_bomId.HasValue)
        {
            var bom = await _bomRepository.GetByIdAsync(_bomId.Value);
            if (bom == null)
            {
                ErrorMessage = "BOM line not found";
                return;
            }

            bom.FinishedProductID = SelectedFinishedProduct.ProductID;
            bom.RawMaterialProductID = SelectedRawMaterial.ProductID;
            bom.Quantity = Quantity;
            bom.UnitOfMeasure = UnitOfMeasure;
            bom.WastagePercentage = WastagePercentage;
            bom.Notes = Notes;
            bom.ModifiedAt = DateTime.Now;
            bom.ModifiedByUserID = userId;
            success = await _bomRepository.UpdateAsync(bom);
        }
        else
        {
            var bom = new BillOfMaterials
            {
                BusinessID = _businessId,
                FinishedProductID = SelectedFinishedProduct.ProductID,
                RawMaterialProductID = SelectedRawMaterial.ProductID,
                Quantity = Quantity,
                UnitOfMeasure = UnitOfMeasure,
                WastagePercentage = WastagePercentage,
                Notes = Notes,
                CreatedByUserID = userId
            };
            success = await _bomRepository.AddAsync(bom);
        }

        if (!success)
            ErrorMessage = "Failed to save BOM line";
        else
            RequestClose?.Invoke(true);
    }

    public event Action<bool>? RequestClose;
}
