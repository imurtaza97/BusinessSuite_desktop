using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessSuite.DAL.Data;
using BusinessSuite.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessSuite.DAL.Repositories;

public class CreditNoteRepository
{
    private readonly AppDbContext _context;

    public CreditNoteRepository(AppDbContext context)
    {
        _context = context;
    }

    /* ============================
       READ
    ============================ */

    public async Task<List<CreditNote>> GetAllAsync(int businessId)
    {
        return await _context.CreditNotes
            .Include(cn => cn.Business)
            .Include(cn => cn.OriginalInvoice)
            .Include(cn => cn.CreatedByUser)
            .Where(cn => cn.BusinessID == businessId && !cn.IsDeleted)
            .OrderByDescending(cn => cn.CreditNoteDate)
            .ToListAsync();
    }

    public async Task<List<CreditNote>> GetPaginatedAsync(int businessId, int page, int pageSize, string? searchTerm = null)
    {
        var query = _context.CreditNotes
            .Include(cn => cn.OriginalInvoice)
            .Where(cn => cn.BusinessID == businessId && !cn.IsDeleted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var search = searchTerm.ToLower();
            query = query.Where(cn => 
                cn.CreditNoteNumber.ToLower().Contains(search) || 
                (cn.OriginalInvoice != null && cn.OriginalInvoice.InvoiceNumber.ToLower().Contains(search)));
        }

        return await query
            .OrderByDescending(cn => cn.CreditNoteDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetCountAsync(int businessId, string? searchTerm = null)
    {
        var query = _context.CreditNotes.Where(cn => cn.BusinessID == businessId && !cn.IsDeleted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var search = searchTerm.ToLower();
            query = query.Where(cn => 
                cn.CreditNoteNumber.ToLower().Contains(search) || 
                (cn.OriginalInvoice != null && cn.OriginalInvoice.InvoiceNumber.ToLower().Contains(search)));
        }

        return await query.CountAsync();
    }

    public async Task<CreditNote?> GetByIdAsync(int id)
    {
        return await _context.CreditNotes
            .Include(cn => cn.Business)
            .Include(cn => cn.OriginalInvoice)
            .Include(cn => cn.CreditNoteItems)
                .ThenInclude(cni => cni.Product)
            .Include(cn => cn.CreatedByUser)
            .Include(cn => cn.FinalizedByUser)
            .Include(cn => cn.CancelledByUser)
            .Include(cn => cn.DeletedByUser)
            .FirstOrDefaultAsync(cn => cn.CreditNoteID == id && !cn.IsDeleted);
    }

    public async Task<List<CreditNote>> GetByInvoiceIdAsync(int invoiceId)
    {
        return await _context.CreditNotes
            .Include(cn => cn.CreditNoteItems)
            .Where(cn => cn.OriginalInvoiceID == invoiceId && !cn.IsDeleted)
            .OrderByDescending(cn => cn.CreditNoteDate)
            .ToListAsync();
    }

    /* ============================
       CREATE
    ============================ */

    public async Task<bool> AddAsync(CreditNote creditNote)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            await _context.CreditNotes.AddAsync(creditNote);
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

    public async Task<bool> UpdateAsync(CreditNote creditNote)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var existing = await _context.CreditNotes
                .Include(cn => cn.CreditNoteItems)
                .FirstOrDefaultAsync(cn => cn.CreditNoteID == creditNote.CreditNoteID && !cn.IsDeleted);

            if (existing == null)
                return false;

            // Can only edit if in Draft status
            if (existing.Status != "Draft")
                return false;

            // Update basic fields
            existing.CreditNoteDate = creditNote.CreditNoteDate;
            existing.Reason = creditNote.Reason;
            existing.SubTotal = creditNote.SubTotal;
            existing.TotalCGST = creditNote.TotalCGST;
            existing.TotalSGST = creditNote.TotalSGST;
            existing.TotalIGST = creditNote.TotalIGST;
            existing.GrandTotal = creditNote.GrandTotal;
            existing.Notes = creditNote.Notes;

            // Update items
            _context.CreditNoteItems.RemoveRange(existing.CreditNoteItems);

            foreach (var item in creditNote.CreditNoteItems)
            {
                item.CreditNoteID = existing.CreditNoteID;
                await _context.CreditNoteItems.AddAsync(item);
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
       FINALIZE & CANCEL
    ============================ */

    public async Task<bool> FinalizeAsync(int creditNoteId, int userId)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var creditNote = await _context.CreditNotes.FindAsync(creditNoteId);
            if (creditNote == null || creditNote.IsDeleted)
                return false;

            if (creditNote.Status != "Draft")
                return false;

            creditNote.Status = "Finalized";
            creditNote.IsDraft = false;
            creditNote.FinalizedAt = DateTime.Now;
            creditNote.FinalizedByUserID = userId;

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

    public async Task<bool> CancelAsync(int creditNoteId, string reason, int userId)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var creditNote = await _context.CreditNotes.FindAsync(creditNoteId);
            if (creditNote == null || creditNote.IsDeleted)
                return false;

            creditNote.Status = "Cancelled";
            creditNote.CancelledAt = DateTime.Now;
            creditNote.CancelledByUserID = userId;
            creditNote.CancellationReason = reason;

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
       SOFT DELETE
    ============================ */

    public async Task<bool> SoftDeleteAsync(int creditNoteId, string reason, int userId)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var creditNote = await _context.CreditNotes.FindAsync(creditNoteId);
            if (creditNote == null || creditNote.IsDeleted)
                return false;

            creditNote.IsDeleted = true;
            creditNote.DeletedAt = DateTime.Now;
            creditNote.DeletedByUserID = userId;
            creditNote.DeletionReason = reason;

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
       NUMBER GENERATION
    ============================ */

    public async Task<string> GetNextCreditNoteNumberAsync(
        int businessId,
        int invoiceId,
        string prefix = "CN-")
    {
        var invoice = await _context.Invoices.FindAsync(invoiceId);
        if (invoice == null)
            return $"{prefix}ERROR";

        var invoiceNumber = invoice.InvoiceNumber;
        var count = await _context.CreditNotes
            .Where(cn => cn.BusinessID == businessId && cn.OriginalInvoiceID == invoiceId && !cn.IsDeleted)
            .CountAsync();

        return $"{invoiceNumber}-{prefix}{(count + 1).ToString().PadLeft(2, '0')}";
    }
}
