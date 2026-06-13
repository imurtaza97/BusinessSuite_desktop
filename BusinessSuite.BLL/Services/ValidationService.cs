using System;
using System.Linq;
using System.Threading.Tasks;
using BusinessSuite.DAL.Data;
using BusinessSuite.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessSuite.BLL.Services;

public class ValidationService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public ValidationService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<(bool IsDuplicate, string Message)> CheckVendorDuplicateAsync(Vendor vendor)
    {
        await using var ctx = await _dbFactory.CreateDbContextAsync();
        var exists = await ctx.Vendors.AnyAsync(v =>
            v.VendorID != vendor.VendorID &&
            (EF.Functions.Like(v.VendorName, vendor.VendorName) ||
             (!string.IsNullOrWhiteSpace(v.GSTIN) && v.GSTIN == vendor.GSTIN) ||
             (!string.IsNullOrWhiteSpace(v.ContactNo) && v.ContactNo == vendor.ContactNo) ||
             (!string.IsNullOrWhiteSpace(v.Email) && v.Email == vendor.Email)));
        return exists ? (true, "A vendor with the same name, GSTIN, phone or email already exists.") : (false, string.Empty);
    }

    public async Task<(bool IsDuplicate, string Message)> CheckCustomerDuplicateAsync(Customer customer)
    {
        await using var ctx = await _dbFactory.CreateDbContextAsync();
        var exists = await ctx.Customers.AnyAsync(c =>
            c.CustomerID != customer.CustomerID &&
            (EF.Functions.Like(c.CustomerName, customer.CustomerName) ||
             (!string.IsNullOrWhiteSpace(c.GSTIN) && c.GSTIN == customer.GSTIN) ||
             (!string.IsNullOrWhiteSpace(c.ContactNo) && c.ContactNo == customer.ContactNo) ||
             (!string.IsNullOrWhiteSpace(c.Email) && c.Email == customer.Email)));
        return exists ? (true, "A customer with the same name, GSTIN, phone or email already exists.") : (false, string.Empty);
    }

    public async Task<(bool IsDuplicate, string Message)> CheckProductDuplicateAsync(Product product)
    {
        await using var ctx = await _dbFactory.CreateDbContextAsync();
        var exists = await ctx.Products.AnyAsync(p =>
            p.ProductID != product.ProductID &&
            (EF.Functions.Like(p.ProductName, product.ProductName) ||
             (!string.IsNullOrWhiteSpace(p.SKU) && p.SKU == product.SKU)));
        return exists ? (true, "A product/service with the same name or SKU already exists.") : (false, string.Empty);
    }
}
