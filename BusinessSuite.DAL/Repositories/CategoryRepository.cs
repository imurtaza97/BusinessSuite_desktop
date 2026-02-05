using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessSuite.DAL.Data;
using BusinessSuite.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessSuite.DAL.Repositories;

public class CategoryRepository
{
    private readonly AppDbContext _context;

    public CategoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Category>> GetAllAsync(int businessId)
    {
        return await _context.Categories
            .Where(c => c.BusinessID == businessId)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<bool> AddAsync(Category category)
    {
        await _context.Categories.AddAsync(category);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateAsync(Category category)
    {
        _context.Categories.Update(category);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null) return false;
        _context.Categories.Remove(category);
        return await _context.SaveChangesAsync() > 0;
    }
}
