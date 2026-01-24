using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessSuite.DAL.Data;
using BusinessSuite.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessSuite.DAL.Repositories;

public class ProductRepository
{
    private readonly AppDbContext _context;

    public ProductRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Product>> GetAllAsync(int businessId)
    {
        return await _context.Products
            .Where(p => p.BusinessID == businessId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        return await _context.Products.FindAsync(id);
    }

    public async Task<bool> AddAsync(Product product)
    {
        await _context.Products.AddAsync(product);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateAsync(Product product)
    {
        var trackedEntity = _context.Products.Local.FirstOrDefault(p => p.ProductID == product.ProductID);
        if (trackedEntity != null)
        {
            // If already tracked, update its values
            _context.Entry(trackedEntity).CurrentValues.SetValues(product);
        }
        else
        {
            // If not tracked, attach and mark as modified
            _context.Products.Update(product);
        }
        
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return false;

        _context.Products.Remove(product);
        return await _context.SaveChangesAsync() > 0;
    }
}
