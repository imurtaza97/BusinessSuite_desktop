using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessSuite.DAL.Data;
using BusinessSuite.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessSuite.DAL.Repositories;

public class WarehouseRepository
{
    private readonly AppDbContext _context;

    public WarehouseRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Warehouse>> GetAllAsync(int businessId)
    {
        return await _context.Warehouses
            .Where(w => w.BusinessId == businessId)
            .OrderBy(w => w.WarehouseName)
            .ToListAsync();
    }

    public async Task<Warehouse?> GetByIdAsync(int warehouseId)
    {
        return await _context.Warehouses.FindAsync(warehouseId);
    }

    public async Task<bool> AddAsync(Warehouse warehouse)
    {
        _context.Warehouses.Add(warehouse);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateAsync(Warehouse warehouse)
    {
        _context.Entry(warehouse).State = EntityState.Modified;
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int warehouseId)
    {
        var warehouse = await _context.Warehouses.FindAsync(warehouseId);
        if (warehouse == null) return false;
        _context.Warehouses.Remove(warehouse);
        return await _context.SaveChangesAsync() > 0;
    }
}
