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

    /* ============================
       READ
    ============================ */

    public async Task<List<Invoice>> GetAllAsync(int businessId)
    {
        return await _context.Invoices
            .Include(i => i.Customer)
            .Where(i => i.BusinessID == businessId && !i.IsDeleted)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();
    }

    public async Task<List<Invoice>> GetPaginatedAsync(int businessId, int page, int pageSize, string? searchTerm = null)
    {
        var query = _context.Invoices
            .Include(i => i.Customer)
            .Where(i => i.BusinessID == businessId && !i.IsDeleted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var search = searchTerm.ToLower();
            query = query.Where(i => 
                i.InvoiceNumber.ToLower().Contains(search) || 
                (i.Customer != null && i.Customer.CustomerName.ToLower().Contains(search)));
        }

        return await query
            .OrderByDescending(i => i.InvoiceDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetCountAsync(int businessId, string? searchTerm = null)
    {
        var query = _context.Invoices.Where(i => i.BusinessID == businessId && !i.IsDeleted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var search = searchTerm.ToLower();
            query = query.Where(i => 
                i.InvoiceNumber.ToLower().Contains(search) || 
                (i.Customer != null && i.Customer.CustomerName.ToLower().Contains(search)));
        }

        return await query.CountAsync();
    }

    public async Task<Invoice?> GetByIdAsync(int id)
    {
        return await _context.Invoices
            .Include(i => i.Business)
            .Include(i => i.Customer)
            .Include(i => i.Items)
                .ThenInclude(ii => ii.Product)
            .FirstOrDefaultAsync(i => i.InvoiceID == id && !i.IsDeleted);
    }

    /* ============================
       CREATE
    ============================ */

    public async Task<bool> AddAsync(Invoice invoice)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            await _context.Invoices.AddAsync(invoice);
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

    /* ============================
       UPDATE
    ============================ */

    public async Task<bool> UpdateAsync(Invoice invoice)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var existing = await _context.Invoices
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.InvoiceID == invoice.InvoiceID);

            if (existing == null)
                return false;

            // Phase 5: Database-level protection for finalized documents
            if (!existing.IsDraft)
            {
                // Finalized invoice: Only allow updates to these fields
                existing.DeliveryStatus = invoice.DeliveryStatus;
                existing.PaymentStatus = invoice.PaymentStatus;
                existing.TotalPaid = invoice.TotalPaid;
                existing.Notes = invoice.Notes;
            }
            else
            {
                // Draft invoice: Allow all field updates
                // ---- Header fields ----
                existing.InvoiceDate = invoice.InvoiceDate;
                existing.DueDate = invoice.DueDate;
                existing.CustomerID = invoice.CustomerID;
                existing.DeliveryStatus = invoice.DeliveryStatus;
                existing.PaymentStatus = invoice.PaymentStatus;

                existing.TotalAmount = invoice.TotalAmount;
                existing.TotalTax = invoice.TotalTax;
                existing.Discount = invoice.Discount;
                existing.GrandTotal = invoice.GrandTotal;
                existing.RoundOff = invoice.RoundOff;

                existing.TotalCGST = invoice.TotalCGST;
                existing.TotalSGST = invoice.TotalSGST;
                existing.TotalIGST = invoice.TotalIGST;

                existing.ShippingCharges = invoice.ShippingCharges;
                existing.PaymentMethod = invoice.PaymentMethod;
                existing.PaymentTerms = invoice.PaymentTerms;

                existing.PlaceOfSupply = invoice.PlaceOfSupply;
                existing.ReverseCharge = invoice.ReverseCharge;
                existing.IsAutoRoundOff = invoice.IsAutoRoundOff;
                existing.IsItemLevelDiscount = invoice.IsItemLevelDiscount;

                existing.TermsAndConditions = invoice.TermsAndConditions;
                existing.Notes = invoice.Notes;

                // ---- Items ----
                _context.InvoiceItems.RemoveRange(existing.Items);

                foreach (var item in invoice.Items)
                {
                    item.InvoiceID = existing.InvoiceID;
                    await _context.InvoiceItems.AddAsync(item);
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

    /* ============================
       DELETE
    ============================ */

    public async Task<bool> DeleteAsync(int id)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var invoice = await _context.Invoices
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.InvoiceID == id);

            if (invoice == null)
                return false;

            _context.InvoiceItems.RemoveRange(invoice.Items);
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

    /* ============================
       STATUS
    ============================ */

    public async Task<bool> UpdateDeliveryStatusAsync(int id, string status)
    {
        var invoice = await _context.Invoices.FindAsync(id);
        if (invoice == null)
            return false;

        invoice.DeliveryStatus = status;
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdatePaymentStatusAsync(int id, string status)
    {
        var invoice = await _context.Invoices.FindAsync(id);
        if (invoice == null)
            return false;

        invoice.PaymentStatus = status;
        return await _context.SaveChangesAsync() > 0;
    }

    /* ============================
       NUMBER GENERATION
    ============================ */

    public async Task<string> GetNextInvoiceNumberAsync(
        int businessId,
        string prefix = "INV/",
        int padLength = 5)
    {
        var lastNumber = await _context.Invoices
            .Where(i => i.BusinessID == businessId && i.InvoiceNumber.StartsWith(prefix))
            .OrderByDescending(i => i.InvoiceNumber)
            .Select(i => i.InvoiceNumber)
            .FirstOrDefaultAsync();

        int next = 1;

        if (!string.IsNullOrEmpty(lastNumber))
        {
            var numericPart = lastNumber.Substring(prefix.Length);
            if (int.TryParse(numericPart, out int num))
                next = num + 1;
        }

        return $"{prefix}{next.ToString().PadLeft(padLength, '0')}";
    }

    /* ============================
       FINALIZE & UNPOST
    ============================ */

    public async Task<bool> FinalizeInvoiceAsync(int invoiceId, int userId)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var invoice = await _context.Invoices.FindAsync(invoiceId);
            if (invoice == null)
                return false;

            if (!invoice.IsDraft)
                return false; // Already finalized

            invoice.IsDraft = false;
            invoice.PostedAt = DateTime.Now;
            invoice.PostedByUserID = userId;

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

    public async Task<bool> UnpostInvoiceAsync(int invoiceId, string reason, int adminUserId)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var invoice = await _context.Invoices.FindAsync(invoiceId);
            if (invoice == null)
                return false;

            if (invoice.IsDraft)
                return false; // Already in draft

            // Unpost: Revert to draft for editing
            invoice.IsDraft = true;
            invoice.PostedAt = null;
            invoice.PostedByUserID = null;

            // Log the unpost action in AuditLog
            var auditLog = new AuditLog
            {
                BusinessID = invoice.BusinessID,
                DocumentType = "Invoice",
                DocumentID = invoiceId,
                Action = "Unposted",
                FieldName = "IsDraft",
                OldValue = "false",
                NewValue = "true",
                ChangedByUserID = adminUserId,
                ChangedAt = DateTime.Now,
                Reason = reason
            };

            await _context.AuditLogs.AddAsync(auditLog);

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
}
