using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessSuite.DAL.Data;
using BusinessSuite.DAL.Entities;
using BusinessSuite.DAL.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BusinessSuite.BLL.Services;

public class BomRequirementLine
{
    public int RawMaterialProductId { get; set; }
    public string MaterialName { get; set; } = string.Empty;
    public decimal RequiredQty { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal AvailableStock { get; set; }
    public bool HasSufficientStock => AvailableStock >= RequiredQty;
}

public class ManufacturingService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public ManufacturingService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public ManufacturingService()
    {
        _dbFactory = new AppDbContextFactory();
    }

    public async Task<List<BomRequirementLine>> GetMaterialRequirementsAsync(
        int finishedProductId,
        decimal quantityToMake,
        int warehouseId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var bomRepo = new BillOfMaterialsRepository(db);

        var bomLines = await bomRepo.GetByFinishedProductAsync(finishedProductId);
        var result = new List<BomRequirementLine>();

        foreach (var line in bomLines)
        {
            var required = CalculateRequiredQuantity(line.Quantity, line.WastagePercentage, quantityToMake);
            var stock = await db.Stocks
                .FirstOrDefaultAsync(s => s.ProductID == line.RawMaterialProductID && s.WarehouseID == warehouseId);

            result.Add(new BomRequirementLine
            {
                RawMaterialProductId = line.RawMaterialProductID,
                MaterialName = line.RawMaterialProduct?.ProductName ?? "Unknown",
                RequiredQty = required,
                Unit = line.UnitOfMeasure,
                AvailableStock = stock?.Quantity ?? 0
            });
        }

        return result;
    }

    public async Task<decimal> EstimateProductionCostAsync(int finishedProductId, decimal quantityToMake)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var bomLines = await db.BillOfMaterials
            .Include(b => b.RawMaterialProduct)
            .Where(b => b.FinishedProductID == finishedProductId && b.IsActive)
            .ToListAsync();

        decimal total = 0;
        foreach (var line in bomLines)
        {
            var required = CalculateRequiredQuantity(line.Quantity, line.WastagePercentage, quantityToMake);
            var unitCost = line.RawMaterialProduct?.PurchasePrice ?? 0;
            total += required * unitCost;
        }

        return Math.Round(total, 2);
    }

    public async Task ProcessProductionCompletionAsync(ProductionOrder order, int warehouseId, int userId)
    {
        if (order.Status == "Completed" || order.Status == "Cancelled")
            throw new InvalidOperationException("Production order is already closed.");

        await using var db = await _dbFactory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();

        try
        {
            bool alreadyProcessed = await db.StockTransactions.AnyAsync(t =>
                t.ReferenceType == "ProductionOrder" &&
                t.ReferenceID == order.ProductionOrderID &&
                t.TransactionType == "In");

            if (alreadyProcessed)
                throw new InvalidOperationException("Stock has already been posted for this production order.");

            var bomLines = await db.BillOfMaterials
                .Include(b => b.RawMaterialProduct)
                .Where(b => b.FinishedProductID == order.ProductID && b.IsActive)
                .ToListAsync();

            if (!bomLines.Any())
                throw new InvalidOperationException("No bill of materials defined for this product. Add BOM lines first.");

            var qtyToProduce = order.QuantityToMake;
            decimal actualCost = 0;

            foreach (var line in bomLines)
            {
                if (line.RawMaterialProduct?.IsService == true)
                    continue;

                var required = CalculateRequiredQuantity(line.Quantity, line.WastagePercentage, qtyToProduce);
                var stock = await db.Stocks.FirstOrDefaultAsync(s =>
                    s.ProductID == line.RawMaterialProductID && s.WarehouseID == warehouseId);

                var available = stock?.Quantity ?? 0;
                if (available < required)
                {
                    throw new InvalidOperationException(
                        $"Insufficient stock for '{line.RawMaterialProduct?.ProductName}'. Required: {required:N2}, Available: {available:N2}");
                }

                var oldQty = stock!.Quantity;
                stock.Quantity -= required;

                db.StockTransactions.Add(new StockTransaction
                {
                    BusinessId = order.BusinessID,
                    ProductID = line.RawMaterialProductID,
                    WarehouseID = warehouseId,
                    Quantity = -required,
                    TransactionType = "Out",
                    ReferenceType = "ProductionOrder",
                    ReferenceID = order.ProductionOrderID,
                    TransactionNumber = order.ProductionOrderNumber,
                    TransactionDate = DateTime.Now,
                    PreviousQuantity = oldQty,
                    NewQuantity = oldQty - required,
                    Description = $"Consumed for production {order.ProductionOrderNumber}"
                });

                actualCost += required * (line.RawMaterialProduct?.PurchasePrice ?? 0);
            }

            var finishedProduct = await db.Products.FindAsync(order.ProductID);
            if (finishedProduct != null && !finishedProduct.IsService)
            {
                var finishedStock = await db.Stocks.FirstOrDefaultAsync(s =>
                    s.ProductID == order.ProductID && s.WarehouseID == warehouseId);

                decimal oldFinishedQty = 0;
                if (finishedStock == null)
                {
                    finishedStock = new Stock
                    {
                        ProductID = order.ProductID,
                        WarehouseID = warehouseId,
                        Quantity = qtyToProduce
                    };
                    db.Stocks.Add(finishedStock);
                }
                else
                {
                    oldFinishedQty = finishedStock.Quantity;
                    finishedStock.Quantity += qtyToProduce;
                }

                db.StockTransactions.Add(new StockTransaction
                {
                    BusinessId = order.BusinessID,
                    ProductID = order.ProductID,
                    WarehouseID = warehouseId,
                    Quantity = qtyToProduce,
                    TransactionType = "In",
                    ReferenceType = "ProductionOrder",
                    ReferenceID = order.ProductionOrderID,
                    TransactionNumber = order.ProductionOrderNumber,
                    TransactionDate = DateTime.Now,
                    PreviousQuantity = oldFinishedQty,
                    NewQuantity = oldFinishedQty + qtyToProduce,
                    Description = $"Produced via {order.ProductionOrderNumber}"
                });
            }

            var dbOrder = await db.ProductionOrders.FindAsync(order.ProductionOrderID);
            if (dbOrder != null)
            {
                dbOrder.Status = "Completed";
                dbOrder.QuantityCompleted = qtyToProduce;
                dbOrder.ActualEndDate = DateTime.Now;
                dbOrder.ActualCost = Math.Round(actualCost, 2);
                dbOrder.ModifiedAt = DateTime.Now;
                dbOrder.ModifiedByUserID = userId;
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            order.Status = "Completed";
            order.QuantityCompleted = qtyToProduce;
            order.ActualEndDate = DateTime.Now;
            order.ActualCost = Math.Round(actualCost, 2);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public static decimal CalculateRequiredQuantity(decimal bomQtyPerUnit, decimal wastagePercent, decimal unitsToMake)
    {
        var baseQty = bomQtyPerUnit * unitsToMake;
        if (wastagePercent <= 0)
            return Math.Round(baseQty, 2);
        return Math.Round(baseQty * (1 + wastagePercent / 100m), 2);
    }
}
