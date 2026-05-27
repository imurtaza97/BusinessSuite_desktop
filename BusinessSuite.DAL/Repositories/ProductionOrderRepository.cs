using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessSuite.DAL.Data;
using BusinessSuite.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessSuite.DAL.Repositories;

public class ProductionOrderRepository
{
    private readonly AppDbContext _context;

    public ProductionOrderRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProductionOrder>> GetPaginatedAsync(int businessId, int page, int pageSize, string? searchTerm = null)
    {
        var query = _context.ProductionOrders
            .Include(p => p.Product)
            .Where(p => p.BusinessID == businessId && !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var search = searchTerm.ToLower();
            query = query.Where(p =>
                p.ProductionOrderNumber.ToLower().Contains(search) ||
                (p.Product != null && p.Product.ProductName.ToLower().Contains(search)));
        }

        return await query
            .OrderByDescending(p => p.StartDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetCountAsync(int businessId, string? searchTerm = null)
    {
        var query = _context.ProductionOrders.Where(p => p.BusinessID == businessId && !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var search = searchTerm.ToLower();
            query = query.Where(p =>
                p.ProductionOrderNumber.ToLower().Contains(search) ||
                (p.Product != null && p.Product.ProductName.ToLower().Contains(search)));
        }

        return await query.CountAsync();
    }

    public async Task<ProductionOrder?> GetByIdAsync(int id)
    {
        return await _context.ProductionOrders
            .Include(p => p.Product)
            .FirstOrDefaultAsync(p => p.ProductionOrderID == id && !p.IsDeleted);
    }

    public async Task<bool> AddAsync(ProductionOrder order)
    {
        await _context.ProductionOrders.AddAsync(order);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateAsync(ProductionOrder order)
    {
        var existing = await _context.ProductionOrders.FindAsync(order.ProductionOrderID);
        if (existing == null || existing.IsDeleted)
            return false;

        existing.ProductID = order.ProductID;
        existing.QuantityToMake = order.QuantityToMake;
        existing.UnitOfMeasure = order.UnitOfMeasure;
        existing.StartDate = order.StartDate;
        existing.ExpectedEndDate = order.ExpectedEndDate;
        existing.ActualEndDate = order.ActualEndDate;
        existing.Status = order.Status;
        existing.Notes = order.Notes;
        existing.QuantityCompleted = order.QuantityCompleted;
        existing.QuantityRejected = order.QuantityRejected;
        existing.EstimatedCost = order.EstimatedCost;
        existing.ActualCost = order.ActualCost;
        existing.ModifiedAt = order.ModifiedAt;
        existing.ModifiedByUserID = order.ModifiedByUserID;

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> SoftDeleteAsync(int id, int userId)
    {
        var existing = await _context.ProductionOrders.FindAsync(id);
        if (existing == null || existing.IsDeleted)
            return false;

        if (existing.Status == "Completed")
            return false;

        existing.IsDeleted = true;
        existing.DeletedAt = DateTime.Now;
        existing.DeletedByUserID = userId;
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<string> GetNextProductionOrderNumberAsync(int businessId)
    {
        var count = await _context.ProductionOrders
            .CountAsync(p => p.BusinessID == businessId && !p.IsDeleted);
        return $"PRD-{(count + 1).ToString().PadLeft(4, '0')}";
    }
}
