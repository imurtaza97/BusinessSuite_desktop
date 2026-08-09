using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessSuite.DAL.Data;
using BusinessSuite.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessSuite.DAL.Repositories;

public class QuotationRepository
{
    private readonly AppDbContext _context;

    public QuotationRepository(AppDbContext context)
    {
        _context = context;
    }

    /* ============================
       READ
    ============================ */

    public async Task<List<Quotation>> GetAllAsync(int businessId)
    {
        return await _context.Quotations
            .Include(i => i.Customer)
            .Where(i => i.BusinessID == businessId && !i.IsDeleted)
            .OrderByDescending(i => i.QuotationDate)
            .ToListAsync();
    }

    public async Task<List<Quotation>> GetPaginatedAsync(int businessId, int page, int pageSize, string? searchTerm = null)
    {
        var query = _context.Quotations
            .Include(i => i.Customer)
            .Where(i => i.BusinessID == businessId && !i.IsDeleted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var search = searchTerm.ToLower();
            query = query.Where(i => 
                i.QuotationNumber.ToLower().Contains(search) || 
                (i.Customer != null && i.Customer.CustomerName.ToLower().Contains(search)));
        }

        return await query
            .OrderByDescending(i => i.QuotationDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetCountAsync(int businessId, string? searchTerm = null)
    {
        var query = _context.Quotations.Where(i => i.BusinessID == businessId && !i.IsDeleted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var search = searchTerm.ToLower();
            query = query.Where(i => 
                i.QuotationNumber.ToLower().Contains(search) || 
                (i.Customer != null && i.Customer.CustomerName.ToLower().Contains(search)));
        }

        return await query.CountAsync();
    }

    public async Task<Quotation?> GetByIdAsync(int id)
    {
        return await _context.Quotations
            .Include(i => i.Business)
            .Include(i => i.Customer)
            .Include(i => i.Items)
                .ThenInclude(ii => ii.Product)
            .FirstOrDefaultAsync(i => i.QuotationID == id && !i.IsDeleted);
    }

    /* ============================
       CREATE
    ============================ */

    public async Task<bool> AddAsync(Quotation quotation)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            await _context.Quotations.AddAsync(quotation);
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

    public async Task<bool> UpdateAsync(Quotation quotation)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var existing = await _context.Quotations
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.QuotationID == quotation.QuotationID);

            if (existing == null)
                return false;

            // Update all header fields (for both draft and finalized documents)
            existing.QuotationDate = quotation.QuotationDate;
            existing.DueDate = quotation.DueDate;
            existing.CustomerID = quotation.CustomerID;
            existing.DeliveryStatus = quotation.DeliveryStatus;
            existing.PaymentStatus = quotation.PaymentStatus;
            existing.TotalPaid = quotation.TotalPaid;

            existing.TotalAmount = quotation.TotalAmount;
            existing.TotalTax = quotation.TotalTax;
            existing.Discount = quotation.Discount;
            existing.GrandTotal = quotation.GrandTotal;
            existing.RoundOff = quotation.RoundOff;

            existing.TotalCGST = quotation.TotalCGST;
            existing.TotalSGST = quotation.TotalSGST;
            existing.TotalIGST = quotation.TotalIGST;

            existing.ShippingCharges = quotation.ShippingCharges;
            existing.PaymentMethod = quotation.PaymentMethod;
            existing.PaymentTerms = quotation.PaymentTerms;

            existing.PlaceOfSupply = quotation.PlaceOfSupply;
            existing.ReverseCharge = quotation.ReverseCharge;
            existing.IsAutoRoundOff = quotation.IsAutoRoundOff;
            existing.IsItemLevelDiscount = quotation.IsItemLevelDiscount;

            existing.TermsAndConditions = quotation.TermsAndConditions;
            existing.Notes = quotation.Notes;
            existing.IsDraft = quotation.IsDraft;

            // ---- Items ----
            _context.QuotationItems.RemoveRange(existing.Items);

            foreach (var item in quotation.Items)
            {
                item.QuotationID = existing.QuotationID;
                await _context.QuotationItems.AddAsync(item);
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
            var quotation = await _context.Quotations
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.QuotationID == id);

            if (quotation == null)
                return false;

            _context.QuotationItems.RemoveRange(quotation.Items);
            _context.Quotations.Remove(quotation);

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

    public async Task<string> GetNextQuotationNumberAsync(
        int businessId,
        string prefix = "QTN/",
        int padLength = 5)
    {
        var lastNumber = await _context.Quotations
            .Where(i => i.BusinessID == businessId && i.QuotationNumber.StartsWith(prefix))
            .OrderByDescending(i => i.QuotationNumber)
            .Select(i => i.QuotationNumber)
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
       FINALIZE
    ============================ */

    public async Task<bool> FinalizeQuotationAsync(int quotationId, int userId)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var quotation = await _context.Quotations.FindAsync(quotationId);
            if (quotation == null)
                return false;

            if (!quotation.IsDraft)
                return false; // Already finalized

            quotation.IsDraft = false;
            quotation.PostedAt = DateTime.Now;
            quotation.PostedByUserID = userId;

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
