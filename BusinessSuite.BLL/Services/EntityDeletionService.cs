using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BusinessSuite.DAL.Data;
using BusinessSuite.DAL.Entities;

namespace BusinessSuite.BLL.Services;

public class EntityDeletionService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public EntityDeletionService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<(bool Success, string Message)> DeleteProductAsync(int productId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var product = await db.Products.FindAsync(productId);
        if (product == null)
            return (false, "Product not found.");

        if (await db.InvoiceItems.AnyAsync(i => i.ProductID == productId))
            return (false, "Cannot delete this product because it is used in one or more invoices.");

        if (await db.PurchaseOrderItems.AnyAsync(i => i.ProductID == productId))
            return (false, "Cannot delete this product because it is used in one or more purchase orders.");

        var stockEntries = await db.Stocks.Where(s => s.ProductID == productId).ToListAsync();
        if (stockEntries.Any() && stockEntries.Any(s => s.Quantity != 0))
            return (false, "Cannot delete this product while it has non-zero stock.");

        if (await db.StockTransactions.AnyAsync(t => t.ProductID == productId))
            return (false, "Cannot delete this product because it has stock transaction history.");

        if (stockEntries.Any())
            db.Stocks.RemoveRange(stockEntries);

        db.Products.Remove(product);
        await db.SaveChangesAsync();
        return (true, "Product deleted successfully.");
    }

    public async Task<(bool Success, string Message)> DeleteCustomerAsync(int customerId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var customer = await db.Customers.FindAsync(customerId);
        if (customer == null)
            return (false, "Customer not found.");

        if (await db.Invoices.AnyAsync(i => i.CustomerID == customerId))
            return (false, "Cannot delete this customer while invoices exist for them.");

        if (await db.FinanceLedgers.AnyAsync(l => l.RelatedEntity == "Customer" && l.RelatedEntityID == customerId))
            return (false, "Cannot delete this customer while finance ledger entries exist for them.");

        if (await db.Payments.AnyAsync(p => p.CustomerID == customerId))
            return (false, "Cannot delete this customer while payment records exist for them.");

        db.Customers.Remove(customer);
        await db.SaveChangesAsync();
        return (true, "Customer deleted successfully.");
    }

    public async Task<(bool Success, string Message)> DeleteVendorAsync(int vendorId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var vendor = await db.Vendors.FindAsync(vendorId);
        if (vendor == null)
            return (false, "Vendor not found.");

        if (await db.Products.AnyAsync(p => p.PreferredVendorID == vendorId))
            return (false, "Cannot delete this vendor because one or more products reference it as a preferred vendor.");

        if (await db.PurchaseOrders.AnyAsync(po => po.VendorId == vendorId))
            return (false, "Cannot delete this vendor while purchase orders exist for them.");

        if (await db.FinanceLedgers.AnyAsync(l => l.RelatedEntity == "Vendor" && l.RelatedEntityID == vendorId))
            return (false, "Cannot delete this vendor while finance ledger entries exist for them.");

        if (await db.Payments.AnyAsync(p => p.VendorID == vendorId))
            return (false, "Cannot delete this vendor while payment records exist for them.");

        db.Vendors.Remove(vendor);
        await db.SaveChangesAsync();
        return (true, "Vendor deleted successfully.");
    }

    public async Task<(bool Success, string Message)> DeleteWarehouseAsync(int warehouseId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var warehouse = await db.Warehouses.FindAsync(warehouseId);
        if (warehouse == null)
            return (false, "Warehouse not found.");

        if (warehouse.IsMainWarehouse)
            return (false, "The main warehouse cannot be deleted.");

        if (await db.Stocks.AnyAsync(s => s.WarehouseID == warehouseId))
            return (false, "Cannot delete this warehouse while stock records exist for it.");

        if (await db.StockTransactions.AnyAsync(t => t.WarehouseID == warehouseId || t.ToWarehouseID == warehouseId))
            return (false, "Cannot delete this warehouse while stock transaction history exists for it.");

        db.Warehouses.Remove(warehouse);
        await db.SaveChangesAsync();
        return (true, "Warehouse deleted successfully.");
    }
}
