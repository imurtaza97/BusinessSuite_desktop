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
            .Where(p => p.BusinessId == businessId)
            .OrderByDescending(p => p.PODate)
            .ToListAsync();
    }

    public async Task<PurchaseOrder?> GetByIdAsync(int id)
    {
        return await _context.PurchaseOrders
            .Include(p => p.Business)
            .Include(p => p.Vendor)
            .Include(p => p.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(p => p.PurchaseOrderID == id);
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

        // 🔹 Update header
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
        existing.VendorBillPath = po.VendorBillPath;

        // 🔹 Replace items (safe, no stock impact)
        _context.PurchaseOrderItems.RemoveRange(existing.Items);
        await _context.SaveChangesAsync();

        foreach (var item in po.Items)
        {
            item.PurchaseOrderID = existing.PurchaseOrderID;
            _context.PurchaseOrderItems.Add(item);
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
}
