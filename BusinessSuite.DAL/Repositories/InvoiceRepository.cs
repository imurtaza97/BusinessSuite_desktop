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
        await _context.Invoices.AddAsync(invoice);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateAsync(Invoice invoice)
    {
        var existing = await _context.Invoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.InvoiceID == invoice.InvoiceID);
        
        if (existing == null) return false;

        // Explicitly map all fields to ensure they are updated correctly
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
        
        // Simple strategy for items: Remove all and re-add
        _context.InvoiceItems.RemoveRange(existing.Items);
        foreach (var item in invoice.Items)
        {
            item.InvoiceID = existing.InvoiceID;
            _context.InvoiceItems.Add(item);
        }

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.InvoiceID == id);
            
        if (invoice == null) return false;

        _context.Invoices.Remove(invoice);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateStatusAsync(int id, string status)
    {
        var invoice = await _context.Invoices.FindAsync(id);
        if (invoice == null) return false;

        invoice.Status = status;
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<string> GetNextInvoiceNumberAsync(int businessId)
    {
        var lastInvoice = await _context.Invoices
            .Where(i => i.BusinessID == businessId)
            .OrderByDescending(i => i.InvoiceID)
            .FirstOrDefaultAsync();

        if (lastInvoice == null) return "INV-0001";

        if (lastInvoice.InvoiceNumber.StartsWith("INV-") && int.TryParse(lastInvoice.InvoiceNumber.Substring(4), out int lastNum))
        {
            return $"INV-{(lastNum + 1):D4}";
        }

        return $"INV-{(lastInvoice.InvoiceID + 1):D4}";
    }
}
