using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessSuite.DAL.Data;
using BusinessSuite.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessSuite.DAL.Repositories;

public class DebitNoteRepository
{
    private readonly AppDbContext _context;

    public DebitNoteRepository(AppDbContext context)
    {
        _context = context;
    }

    /* ============================
       READ
    ============================ */

    public async Task<List<DebitNote>> GetAllAsync(int businessId)
    {
        return await _context.DebitNotes
            .Include(dn => dn.Business)
            .Include(dn => dn.OriginalInvoice)
            .Include(dn => dn.CreatedByUser)
            .Where(dn => dn.BusinessID == businessId && !dn.IsDeleted)
            .OrderByDescending(dn => dn.DebitNoteDate)
            .ToListAsync();
    }

    public async Task<List<DebitNote>> GetPaginatedAsync(int businessId, int page, int pageSize, string? searchTerm = null)
    {
        var query = _context.DebitNotes
            .Include(dn => dn.OriginalInvoice)
            .Where(dn => dn.BusinessID == businessId && !dn.IsDeleted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var search = searchTerm.ToLower();
            query = query.Where(dn => 
                dn.DebitNoteNumber.ToLower().Contains(search) || 
                (dn.OriginalInvoice != null && dn.OriginalInvoice.InvoiceNumber.ToLower().Contains(search)));
        }

        return await query
            .OrderByDescending(dn => dn.DebitNoteDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetCountAsync(int businessId, string? searchTerm = null)
    {
        var query = _context.DebitNotes.Where(dn => dn.BusinessID == businessId && !dn.IsDeleted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var search = searchTerm.ToLower();
            query = query.Where(dn => 
                dn.DebitNoteNumber.ToLower().Contains(search) || 
                (dn.OriginalInvoice != null && dn.OriginalInvoice.InvoiceNumber.ToLower().Contains(search)));
        }

        return await query.CountAsync();
    }

    public async Task<DebitNote?> GetByIdAsync(int id)
    {
        return await _context.DebitNotes
            .Include(dn => dn.Business)
            .Include(dn => dn.OriginalInvoice)
            .Include(dn => dn.DebitNoteItems)
                .ThenInclude(dni => dni.Product)
            .Include(dn => dn.CreatedByUser)
            .Include(dn => dn.FinalizedByUser)
            .Include(dn => dn.CancelledByUser)
            .Include(dn => dn.DeletedByUser)
            .FirstOrDefaultAsync(dn => dn.DebitNoteID == id && !dn.IsDeleted);
    }

    public async Task<List<DebitNote>> GetByInvoiceIdAsync(int invoiceId)
    {
        return await _context.DebitNotes
            .Include(dn => dn.DebitNoteItems)
            .Where(dn => dn.OriginalInvoiceID == invoiceId && !dn.IsDeleted)
            .OrderByDescending(dn => dn.DebitNoteDate)
            .ToListAsync();
    }

    /* ============================
       CREATE
    ============================ */

    public async Task<bool> AddAsync(DebitNote debitNote)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            await _context.DebitNotes.AddAsync(debitNote);
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

    public async Task<bool> UpdateAsync(DebitNote debitNote)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var existing = await _context.DebitNotes
                .Include(dn => dn.DebitNoteItems)
                .FirstOrDefaultAsync(dn => dn.DebitNoteID == debitNote.DebitNoteID && !dn.IsDeleted);

            if (existing == null)
                return false;

            // Can only edit if in Draft status
            if (existing.Status != "Draft")
                return false;

            // Update basic fields
            existing.DebitNoteDate = debitNote.DebitNoteDate;
            existing.Reason = debitNote.Reason;
            existing.SubTotal = debitNote.SubTotal;
            existing.TotalCGST = debitNote.TotalCGST;
            existing.TotalSGST = debitNote.TotalSGST;
            existing.TotalIGST = debitNote.TotalIGST;
            existing.GrandTotal = debitNote.GrandTotal;
            existing.Notes = debitNote.Notes;

            // Update items
            _context.DebitNoteItems.RemoveRange(existing.DebitNoteItems);

            foreach (var item in debitNote.DebitNoteItems)
            {
                item.DebitNoteID = existing.DebitNoteID;
                await _context.DebitNoteItems.AddAsync(item);
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

    public async Task<bool> FinalizeAsync(int debitNoteId, int userId)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var debitNote = await _context.DebitNotes.FindAsync(debitNoteId);
            if (debitNote == null || debitNote.IsDeleted)
                return false;

            if (debitNote.Status != "Draft")
                return false;

            debitNote.Status = "Finalized";
            debitNote.IsDraft = false;
            debitNote.FinalizedAt = DateTime.Now;
            debitNote.FinalizedByUserID = userId;

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

    public async Task<bool> CancelAsync(int debitNoteId, string reason, int userId)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var debitNote = await _context.DebitNotes.FindAsync(debitNoteId);
            if (debitNote == null || debitNote.IsDeleted)
                return false;

            debitNote.Status = "Cancelled";
            debitNote.CancelledAt = DateTime.Now;
            debitNote.CancelledByUserID = userId;
            debitNote.CancellationReason = reason;

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

    public async Task<bool> SoftDeleteAsync(int debitNoteId, string reason, int userId)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var debitNote = await _context.DebitNotes.FindAsync(debitNoteId);
            if (debitNote == null || debitNote.IsDeleted)
                return false;

            debitNote.IsDeleted = true;
            debitNote.DeletedAt = DateTime.Now;
            debitNote.DeletedByUserID = userId;
            debitNote.DeletionReason = reason;

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

    public async Task<string> GetNextDebitNoteNumberAsync(
        int businessId,
        int invoiceId,
        string prefix = "DN-")
    {
        var invoice = await _context.Invoices.FindAsync(invoiceId);
        if (invoice == null)
            return $"{prefix}ERROR";

        var invoiceNumber = invoice.InvoiceNumber;
        var count = await _context.DebitNotes
            .Where(dn => dn.BusinessID == businessId && dn.OriginalInvoiceID == invoiceId && !dn.IsDeleted)
            .CountAsync();

        return $"{invoiceNumber}-{prefix}{(count + 1).ToString().PadLeft(2, '0')}";
    }
}
