using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessSuite.DAL.Data;
using BusinessSuite.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessSuite.DAL.Repositories;

public class BillOfMaterialsRepository
{
    private readonly AppDbContext _context;

    public BillOfMaterialsRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<BillOfMaterials>> GetAllAsync(int businessId, string? searchTerm = null)
    {
        var query = _context.BillOfMaterials
            .Include(b => b.FinishedProduct)
            .Include(b => b.RawMaterialProduct)
            .Where(b => b.BusinessID == businessId && b.IsActive);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var search = searchTerm.ToLower();
            query = query.Where(b =>
                (b.FinishedProduct != null && b.FinishedProduct.ProductName.ToLower().Contains(search)) ||
                (b.RawMaterialProduct != null && b.RawMaterialProduct.ProductName.ToLower().Contains(search)));
        }

        return await query
            .OrderBy(b => b.FinishedProduct!.ProductName)
            .ThenBy(b => b.RawMaterialProduct!.ProductName)
            .ToListAsync();
    }

    public async Task<List<BillOfMaterials>> GetByFinishedProductAsync(int finishedProductId)
    {
        return await _context.BillOfMaterials
            .Include(b => b.RawMaterialProduct)
            .Where(b => b.FinishedProductID == finishedProductId && b.IsActive)
            .ToListAsync();
    }

    public async Task<BillOfMaterials?> GetByIdAsync(int bomId)
    {
        return await _context.BillOfMaterials
            .Include(b => b.FinishedProduct)
            .Include(b => b.RawMaterialProduct)
            .FirstOrDefaultAsync(b => b.BOM_ID == bomId && b.IsActive);
    }

    public async Task<bool> AddAsync(BillOfMaterials bom)
    {
        await _context.BillOfMaterials.AddAsync(bom);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateAsync(BillOfMaterials bom)
    {
        var existing = await _context.BillOfMaterials.FindAsync(bom.BOM_ID);
        if (existing == null || !existing.IsActive)
            return false;

        existing.FinishedProductID = bom.FinishedProductID;
        existing.RawMaterialProductID = bom.RawMaterialProductID;
        existing.Quantity = bom.Quantity;
        existing.UnitOfMeasure = bom.UnitOfMeasure;
        existing.WastagePercentage = bom.WastagePercentage;
        existing.Notes = bom.Notes;
        existing.ModifiedAt = bom.ModifiedAt;
        existing.ModifiedByUserID = bom.ModifiedByUserID;

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeactivateAsync(int bomId, int userId)
    {
        var existing = await _context.BillOfMaterials.FindAsync(bomId);
        if (existing == null)
            return false;

        existing.IsActive = false;
        existing.ModifiedAt = System.DateTime.Now;
        existing.ModifiedByUserID = userId;
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> ExistsAsync(int businessId, int finishedProductId, int rawMaterialId, int? excludeBomId = null)
    {
        var query = _context.BillOfMaterials.Where(b =>
            b.BusinessID == businessId &&
            b.FinishedProductID == finishedProductId &&
            b.RawMaterialProductID == rawMaterialId &&
            b.IsActive);

        if (excludeBomId.HasValue)
            query = query.Where(b => b.BOM_ID != excludeBomId.Value);

        return await query.AnyAsync();
    }
}
