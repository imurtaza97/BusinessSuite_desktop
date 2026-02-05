using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessSuite.DAL.Data;
using BusinessSuite.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessSuite.DAL.Repositories;

public class InvoiceRepository
{
    private readonly AppDbContext _context;

    public InvoiceRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Invoice>> GetAllAsync(int businessId)
    {
        return await _context.Invoices
            .Include(i => i.Customer)
            .Where(i => i.BusinessID == businessId)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();
    }

    public async Task<Invoice?> GetByIdAsync(int id)
    {
        return await _context.Invoices
            .Include(i => i.Business)
            .Include(i => i.Customer)
            .Include(i => i.Items)
            .ThenInclude(ii => ii.Product)
            .FirstOrDefaultAsync(i => i.InvoiceID == id);
    }

    public async Task<bool> AddAsync(Invoice invoice)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            await _context.Invoices.AddAsync(invoice);

            // Update Stock
            foreach (var item in invoice.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductID);
                if (product != null)
                {
                    product.StockQty -= item.Quantity;
                    
                    await _context.StockTransactions.AddAsync(new StockTransaction
                    {
                        ProductID = item.ProductID,
                        BusinessId = invoice.BusinessID,
                        TransactionType = "Sales",
                        Quantity = -item.Quantity,
                        ReferenceID = invoice.InvoiceID,
                        Description = $"Invoice #{invoice.InvoiceNumber}",
                        TransactionDate = invoice.InvoiceDate
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

    public async Task<bool> UpdateAsync(Invoice invoice)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var existing = await _context.Invoices
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.InvoiceID == invoice.InvoiceID);
            
            if (existing == null) return false;

            // 1. Revert previous stock changes
            foreach (var item in existing.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductID);
                if (product != null)
                {
                    product.StockQty += item.Quantity;
                }
            }

            // Remove old stock transactions for this invoice
            var oldTransactions = await _context.StockTransactions
                .Where(t => t.ReferenceID == invoice.InvoiceID && t.TransactionType == "Sales")
                .ToListAsync();
            _context.StockTransactions.RemoveRange(oldTransactions);

            // 2. Map fields
            existing.InvoiceDate = invoice.InvoiceDate;
            existing.DueDate = invoice.DueDate;
            existing.IsAutoRoundOff = invoice.IsAutoRoundOff;
            existing.CustomerID = invoice.CustomerID;
            existing.TotalAmount = invoice.TotalAmount;
            existing.TotalTax = invoice.TotalTax;
            existing.Discount = invoice.Discount;
            existing.GrandTotal = invoice.GrandTotal;
            existing.ShippingCharges = invoice.ShippingCharges;
            existing.PaymentMethod = invoice.PaymentMethod;
            existing.PaymentTerms = invoice.PaymentTerms;
            existing.TermsAndConditions = invoice.TermsAndConditions;
            existing.Notes = invoice.Notes;
            existing.Status = invoice.Status;
            existing.IsItemLevelDiscount = invoice.IsItemLevelDiscount;
            existing.PlaceOfSupply = invoice.PlaceOfSupply;
            existing.ReverseCharge = invoice.ReverseCharge;
            existing.RoundOff = invoice.RoundOff;
            existing.TotalCGST = invoice.TotalCGST;
            existing.TotalSGST = invoice.TotalSGST;
            existing.TotalIGST = invoice.TotalIGST;
            
            // 3. Update items and apply new stock changes
            _context.InvoiceItems.RemoveRange(existing.Items);
            foreach (var item in invoice.Items)
            {
                item.InvoiceID = existing.InvoiceID;
                _context.InvoiceItems.Add(item);

                var product = await _context.Products.FindAsync(item.ProductID);
                if (product != null)
                {
                    product.StockQty -= item.Quantity;

                    await _context.StockTransactions.AddAsync(new StockTransaction
                    {
                        ProductID = item.ProductID,
                        BusinessId = invoice.BusinessID,
                        TransactionType = "Sales",
                        Quantity = -item.Quantity,
                        ReferenceID = invoice.InvoiceID,
                        Description = $"Invoice Updated #{invoice.InvoiceNumber}",
                        TransactionDate = invoice.InvoiceDate
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
            var invoice = await _context.Invoices
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.InvoiceID == id);
                
            if (invoice == null) return false;

            // Revert stock
            foreach (var item in invoice.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductID);
                if (product != null)
                {
                    product.StockQty += item.Quantity;
                }
            }

            // Remove transactions
            var transactions = await _context.StockTransactions
                .Where(t => t.ReferenceID == id && t.TransactionType == "Sales")
                .ToListAsync();
            _context.StockTransactions.RemoveRange(transactions);

            _context.Invoices.Remove(invoice);
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

    public async Task<bool> UpdateStatusAsync(int id, string status)
    {
        var invoice = await _context.Invoices.FindAsync(id);
        if (invoice == null) return false;

        invoice.Status = status;
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<string> GetNextInvoiceNumberAsync(int businessId, string prefix = "INV-")
    {
        var invoices = await _context.Invoices
            .Where(i => i.BusinessID == businessId && i.InvoiceNumber.StartsWith(prefix))
            .ToListAsync();

        int maxNum = 0;
        foreach (var inv in invoices)
        {
            var part = inv.InvoiceNumber.Substring(prefix.Length);
            // Try to extract numeric part even if it has non-digits at the end (though usually it shouldn't)
            var numericPart = new string(part.TakeWhile(char.IsDigit).ToArray());
            if (int.TryParse(numericPart, out int num))
            {
                if (num > maxNum) maxNum = num;
            }
        }

        int nextNum = maxNum + 1;
        // Growth: 4 digits minimum, but expands infinitely
        return $"{prefix}{nextNum:D4}";
    }
}
