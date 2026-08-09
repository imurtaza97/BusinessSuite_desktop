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

    private static string GetFinancialYear(DateTime date)
    {
        if (date.Month >= 4)
            return $"{date.Year % 100}-{(date.Year + 1) % 100}";
        else
            return $"{(date.Year - 1) % 100}-{date.Year % 100}";
    }

    public async Task<List<PurchaseOrder>> GetAllAsync(int businessId)
    {
        return await _context.PurchaseOrders
            .Include(p => p.Vendor)
            .Where(p => p.BusinessId == businessId && !p.IsDeleted)
            .OrderByDescending(p => p.PODate)
            .ToListAsync();
    }

    public async Task<List<PurchaseOrder>> GetPaginatedAsync(int businessId, int page, int pageSize, string? searchTerm = null)
    {
        var query = _context.PurchaseOrders
            .Include(p => p.Vendor)
            .Where(p => p.BusinessId == businessId && !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var search = searchTerm.ToLower();
            query = query.Where(p => 
                p.PONumber.ToLower().Contains(search) || 
                (p.Vendor != null && p.Vendor.VendorName.ToLower().Contains(search)));
        }

        return await query
            .OrderByDescending(p => p.PODate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetCountAsync(int businessId, string? searchTerm = null)
    {
        var query = _context.PurchaseOrders.Where(p => p.BusinessId == businessId && !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var search = searchTerm.ToLower();
            query = query.Where(p => 
                p.PONumber.ToLower().Contains(search) || 
                (p.Vendor != null && p.Vendor.VendorName.ToLower().Contains(search)));
        }

        return await query.CountAsync();
    }

    public async Task<PurchaseOrder?> GetByIdAsync(int id)
    {
        return await _context.PurchaseOrders
            .Include(p => p.Business)
            .Include(p => p.Vendor)
            .Include(p => p.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(p => p.PurchaseOrderID == id && !p.IsDeleted);
    }

    // ✅ CREATE PO (NO STOCK)
    public async Task<bool> AddAsync(PurchaseOrder po)
    {
        await _context.PurchaseOrders.AddAsync(po);
        return await _context.SaveChangesAsync() > 0;
    }

    // ✅ UPDATE PO (NO STOCK)
    public async Task<bool> UpdateAsync(PurchaseOrder po)
    {
        var existing = await _context.PurchaseOrders
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.PurchaseOrderID == po.PurchaseOrderID);

        if (existing == null)
            return false;

        // Update all header fields (for both draft and finalized documents)
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
        existing.DeliveryStatus = po.DeliveryStatus;
        existing.PaymentStatus = po.PaymentStatus;
        existing.TotalPaid = po.TotalPaid;
        existing.IsItemLevelDiscount = po.IsItemLevelDiscount;
        existing.PlaceOfSupply = po.PlaceOfSupply;
        existing.ReverseCharge = po.ReverseCharge;
        existing.RoundOff = po.RoundOff;
        existing.TotalCGST = po.TotalCGST;
        existing.TotalSGST = po.TotalSGST;
        existing.TotalIGST = po.TotalIGST;
        existing.VendorBillPath = po.VendorBillPath;
        existing.IsDraft = po.IsDraft;

        // Replace items (safe, no stock impact)
        _context.PurchaseOrderItems.RemoveRange(existing.Items);
        await _context.SaveChangesAsync();

        foreach (var item in po.Items)
        {
            item.PurchaseOrderID = existing.PurchaseOrderID;
            await _context.PurchaseOrderItems.AddAsync(item);
        }

        return await _context.SaveChangesAsync() > 0;
    }

    // ✅ DELETE PO (NO STOCK)
    public async Task<bool> DeleteAsync(int id)
    {
        var po = await _context.PurchaseOrders
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.PurchaseOrderID == id);

        if (po == null)
            return false;

        _context.PurchaseOrderItems.RemoveRange(po.Items);
        _context.PurchaseOrders.Remove(po);

        return await _context.SaveChangesAsync() > 0;
    }

    // ✅ PO NUMBER GENERATION
    public async Task<string> GetNextPONumberAsync(int businessId, DateTime poDate, int padLength = 5)
    {
        var fy = GetFinancialYear(poDate);
        string prefix = $"PO/{fy}/";

        var numbers = await _context.PurchaseOrders
            .Where(p => p.BusinessId == businessId && p.PONumber.StartsWith(prefix))
            .Select(p => p.PONumber.Substring(prefix.Length))
            .ToListAsync();

        int maxNum = 0;
        foreach (var n in numbers)
        {
            if (int.TryParse(n, out int num) && num > maxNum)
                maxNum = num;
        }

        return $"{prefix}{(maxNum + 1).ToString().PadLeft(padLength, '0')}";
    }
    public async Task<bool> UpdateDeliveryStatusAsync(int id, string status)
    {
        var po = await _context.PurchaseOrders.FindAsync(id);
        if (po == null)
            return false;

        po.DeliveryStatus = status;
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdatePaymentStatusAsync(int id, string status)
    {
        var po = await _context.PurchaseOrders.FindAsync(id);
        if (po == null)
            return false;

        po.PaymentStatus = status;
        return await _context.SaveChangesAsync() > 0;
    }

    /* ============================
       FINALIZE & UNPOST
    ============================ */

    public async Task<bool> FinalizePurchaseOrderAsync(int purchaseOrderId, int userId)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var po = await _context.PurchaseOrders.FindAsync(purchaseOrderId);
            if (po == null)
                return false;

            if (!po.IsDraft)
                return false; // Already finalized

            po.IsDraft = false;
            po.PostedAt = DateTime.Now;
            po.PostedByUserID = userId;

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

    public async Task<bool> UnpostPurchaseOrderAsync(int purchaseOrderId, string reason, int adminUserId)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var po = await _context.PurchaseOrders.FindAsync(purchaseOrderId);
            if (po == null)
                return false;

            if (po.IsDraft)
                return false; // Already in draft

            // Unpost: Revert to draft for editing
            po.IsDraft = true;
            po.PostedAt = null;
            po.PostedByUserID = null;

            // Log the unpost action in AuditLog
            var auditLog = new AuditLog
            {
                BusinessID = po.BusinessId,
                DocumentType = "PurchaseOrder",
                DocumentID = purchaseOrderId,
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
