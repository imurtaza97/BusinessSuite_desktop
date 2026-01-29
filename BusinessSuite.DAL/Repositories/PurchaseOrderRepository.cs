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
        await _context.PurchaseOrders.AddAsync(po);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateAsync(PurchaseOrder po)
    {
        var existing = await _context.PurchaseOrders
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.PurchaseOrderID == po.PurchaseOrderID);
        
        if (existing == null) return false;

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
        
        _context.PurchaseOrderItems.RemoveRange(existing.Items);
        foreach (var item in po.Items)
        {
            item.PurchaseOrderID = existing.PurchaseOrderID;
            _context.PurchaseOrderItems.Add(item);
        }

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var po = await _context.PurchaseOrders
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.PurchaseOrderID == id);
            
        if (po == null) return false;

        _context.PurchaseOrders.Remove(po);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<string> GetNextPONumberAsync(int businessId)
    {
        var lastPO = await _context.PurchaseOrders
            .Where(i => i.BusinessId == businessId)
            .OrderByDescending(i => i.PurchaseOrderID)
            .FirstOrDefaultAsync();

        if (lastPO == null) return "PO-0001";

        if (lastPO.PONumber.StartsWith("PO-") && int.TryParse(lastPO.PONumber.Substring(3), out int lastNum))
        {
            return $"PO-{(lastNum + 1):D4}";
        }

        return $"PO-{(lastPO.PurchaseOrderID + 1):D4}";
    }
}
