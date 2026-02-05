using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessSuite.DAL.Data;
using BusinessSuite.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessSuite.DAL.Repositories;

public class PurchaseOrderRepository
{
    private readonly AppDbContext _context;

    public PurchaseOrderRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<PurchaseOrder>> GetAllAsync(int businessId)
    {
        return await _context.PurchaseOrders
            .Include(i => i.Vendor)
            .Where(i => i.BusinessId == businessId)
            .OrderByDescending(i => i.PODate)
            .ToListAsync();
    }

    public async Task<PurchaseOrder?> GetByIdAsync(int id)
    {
        return await _context.PurchaseOrders
            .Include(i => i.Business)
            .Include(i => i.Vendor)
            .Include(i => i.Items)
            .ThenInclude(ii => ii.Product)
            .FirstOrDefaultAsync(i => i.PurchaseOrderID == id);
    }

    public async Task<bool> AddAsync(PurchaseOrder po)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            await _context.PurchaseOrders.AddAsync(po);

            // Update Stock
            foreach (var item in po.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductID);
                if (product != null)
                {
                    product.StockQty += item.Quantity;

                    await _context.StockTransactions.AddAsync(new StockTransaction
                    {
                        ProductID = item.ProductID,
                        BusinessId = po.BusinessId,
                        TransactionType = "Purchase",
                        Quantity = item.Quantity,
                        ReferenceID = po.PurchaseOrderID,
                        Description = $"Purchase Order #{po.PONumber}",
                        TransactionDate = po.PODate
                    });
                }
            }

            var result = await _context.SaveChangesAsync() > 0;
            await transaction.CommitAsync();
            return result;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> UpdateAsync(PurchaseOrder po)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var existing = await _context.PurchaseOrders
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.PurchaseOrderID == po.PurchaseOrderID);
            
            if (existing == null) return false;

            // 1. Revert previous stock changes
            foreach (var item in existing.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductID);
                if (product != null)
                {
                    product.StockQty -= item.Quantity;
                }
            }

            // Remove old stock transactions for this PO
            var oldTransactions = await _context.StockTransactions
                .Where(t => t.ReferenceID == po.PurchaseOrderID && t.TransactionType == "Purchase")
                .ToListAsync();
            _context.StockTransactions.RemoveRange(oldTransactions);

            // 2. Map fields
            existing.PODate = po.PODate;
            existing.ExpectedDeliveryDate = po.ExpectedDeliveryDate;
            existing.IsAutoRoundOff = po.IsAutoRoundOff;
            existing.VendorId = po.VendorId;
            existing.TotalAmount = po.TotalAmount;
            existing.TotalTax = po.TotalTax;
            existing.Discount = po.Discount;
            existing.GrandTotal = po.GrandTotal;
            existing.ShippingCharges = po.ShippingCharges;
            existing.PaymentMethod = po.PaymentMethod;
            existing.PaymentTerms = po.PaymentTerms;
            existing.TermsAndConditions = po.TermsAndConditions;
            existing.Notes = po.Notes;
            existing.Status = po.Status;
            existing.IsItemLevelDiscount = po.IsItemLevelDiscount;
            existing.PlaceOfSupply = po.PlaceOfSupply;
            existing.ReverseCharge = po.ReverseCharge;
            existing.RoundOff = po.RoundOff;
            existing.TotalCGST = po.TotalCGST;
            existing.TotalSGST = po.TotalSGST;
            existing.TotalIGST = po.TotalIGST;
            existing.VendorBillPath = po.VendorBillPath; // Also sync the attachment path
            
            // 3. Update items and apply new stock changes
            _context.PurchaseOrderItems.RemoveRange(existing.Items);
            foreach (var item in po.Items)
            {
                item.PurchaseOrderID = existing.PurchaseOrderID;
                _context.PurchaseOrderItems.Add(item);

                var product = await _context.Products.FindAsync(item.ProductID);
                if (product != null)
                {
                    product.StockQty += item.Quantity;

                    await _context.StockTransactions.AddAsync(new StockTransaction
                    {
                        ProductID = item.ProductID,
                        BusinessId = po.BusinessId,
                        TransactionType = "Purchase",
                        Quantity = item.Quantity,
                        ReferenceID = po.PurchaseOrderID,
                        Description = $"Purchase Order Updated #{po.PONumber}",
                        TransactionDate = po.PODate
                    });
                }
            }

            var result = await _context.SaveChangesAsync() > 0;
            await transaction.CommitAsync();
            return result;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var po = await _context.PurchaseOrders
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.PurchaseOrderID == id);
                
            if (po == null) return false;

            // Revert stock
            foreach (var item in po.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductID);
                if (product != null)
                {
                    product.StockQty -= item.Quantity;
                }
            }

            // Remove transactions
            var transactions = await _context.StockTransactions
                .Where(t => t.ReferenceID == id && t.TransactionType == "Purchase")
                .ToListAsync();
            _context.StockTransactions.RemoveRange(transactions);

            _context.PurchaseOrders.Remove(po);
            var result = await _context.SaveChangesAsync() > 0;
            await transaction.CommitAsync();
            return result;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<string> GetNextPONumberAsync(int businessId)
    {
        string prefix = "PO-";
        var pos = await _context.PurchaseOrders
            .Where(i => i.BusinessId == businessId && i.PONumber.StartsWith(prefix))
            .ToListAsync();

        int maxNum = 0;
        foreach (var po in pos)
        {
            var part = po.PONumber.Substring(prefix.Length);
            var numericPart = new string(part.TakeWhile(char.IsDigit).ToArray());
            if (int.TryParse(numericPart, out int num))
            {
                if (num > maxNum) maxNum = num;
            }
        }

        int nextNum = maxNum + 1;
        return $"{prefix}{nextNum:D4}";
    }
}
