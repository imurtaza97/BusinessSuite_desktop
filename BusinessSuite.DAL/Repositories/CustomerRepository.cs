using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessSuite.DAL.Data;
using BusinessSuite.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessSuite.DAL.Repositories;

public class CustomerRepository
{
    private readonly AppDbContext _context;

    public CustomerRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Customer>> GetAllAsync(int businessId)
    {
        return await _context.Customers
            .Where(c => c.BusinessId == businessId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Customer>> GetPaginatedAsync(int businessId, int page, int pageSize, string? searchTerm = null)
    {
        var query = _context.Customers.Where(c => c.BusinessId == businessId);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var search = searchTerm.ToLower();
            query = query.Where(c => 
                c.CustomerName.ToLower().Contains(search) || 
                (c.ContactNo != null && c.ContactNo.Contains(search)));
        }

        return await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetCountAsync(int businessId, string? searchTerm = null)
    {
        var query = _context.Customers.Where(c => c.BusinessId == businessId);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var search = searchTerm.ToLower();
            query = query.Where(c => 
                c.CustomerName.ToLower().Contains(search) || 
                (c.ContactNo != null && c.ContactNo.Contains(search)));
        }

        return await query.CountAsync();
    }

    public async Task<Customer?> GetByIdAsync(int id)
    {
        return await _context.Customers.FindAsync(id);
    }

    public async Task<bool> AddAsync(Customer customer)
    {
        await _context.Customers.AddAsync(customer);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateAsync(Customer customer)
    {
        var trackedEntity = _context.Customers.Local.FirstOrDefault(c => c.CustomerID == customer.CustomerID);
        if (trackedEntity != null)
        {
            _context.Entry(trackedEntity).CurrentValues.SetValues(customer);
        }
        else
        {
            _context.Customers.Update(customer);
        }
        
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null) return false;

        _context.Customers.Remove(customer);
        return await _context.SaveChangesAsync() > 0;
    }
}
