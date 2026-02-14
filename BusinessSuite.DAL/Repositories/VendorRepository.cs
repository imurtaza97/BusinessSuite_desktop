using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessSuite.DAL.Data;
using BusinessSuite.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessSuite.DAL.Repositories;

public class VendorRepository
{
    private readonly AppDbContext _context;

    public VendorRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Vendor>> GetAllAsync(int businessId)
    {
        return await _context.Vendors
            .Where(v => v.BusinessId == businessId)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Vendor>> GetPaginatedAsync(int businessId, int page, int pageSize, string? searchTerm = null)
    {
        var query = _context.Vendors.Where(v => v.BusinessId == businessId);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var search = searchTerm.ToLower();
            query = query.Where(v => 
                v.VendorName.ToLower().Contains(search) || 
                (v.ContactNo != null && v.ContactNo.Contains(search)));
        }

        return await query
            .OrderByDescending(v => v.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetCountAsync(int businessId, string? searchTerm = null)
    {
        var query = _context.Vendors.Where(v => v.BusinessId == businessId);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var search = searchTerm.ToLower();
            query = query.Where(v => 
                v.VendorName.ToLower().Contains(search) || 
                (v.ContactNo != null && v.ContactNo.Contains(search)));
        }

        return await query.CountAsync();
    }

    public async Task<Vendor?> GetByIdAsync(int id)
    {
        return await _context.Vendors.FindAsync(id);
    }

    public async Task<bool> AddAsync(Vendor vendor)
    {
        await _context.Vendors.AddAsync(vendor);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateAsync(Vendor vendor)
    {
        var trackedEntity = _context.Vendors.Local.FirstOrDefault(v => v.VendorID == vendor.VendorID);
        if (trackedEntity != null)
        {
            _context.Entry(trackedEntity).CurrentValues.SetValues(vendor);
        }
        else
        {
            _context.Vendors.Update(vendor);
        }
        
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var vendor = await _context.Vendors.FindAsync(id);
        if (vendor == null) return false;

        _context.Vendors.Remove(vendor);
        return await _context.SaveChangesAsync() > 0;
    }
}
