using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessSuite.BLL.DTOs;
using BusinessSuite.DAL.Data;
using Microsoft.EntityFrameworkCore;

namespace BusinessSuite.BLL.Services;

public class ReportingService
{
    private readonly AppDbContext _context;

    public ReportingService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardSummary> GetDashboardSummaryAsync(int businessId)
    {
        var summary = new DashboardSummary();

        var invoices = await _context.Invoices
            .Where(i => i.BusinessID == businessId && i.Status != "Cancelled")
            .ToListAsync();

        var purchaseOrders = await _context.PurchaseOrders
            .Where(p => p.BusinessId == businessId && p.Status != "Cancelled")
            .ToListAsync();

        summary.TotalSales = invoices.Sum(i => i.GrandTotal);
        summary.TotalPurchases = purchaseOrders.Sum(p => p.GrandTotal);
        summary.NetProfit = summary.TotalSales - summary.TotalPurchases;
        summary.ActiveOrdersCount = invoices.Count(i => i.Status == "Unpaid");
        summary.TotalReceivable = invoices.Where(i => i.Status == "Unpaid").Sum(i => i.GrandTotal);

        return summary;
    }

    // Inside ReportingService.cs
    public async Task<List<ChartDataPoint>> GetSalesAnalyticsAsync(int businessId, int year = 0)
    {
        if (year == 0) year = DateTime.Now.Year;
        
        var startDate = new DateTime(year, 1, 1);
        var endDate = new DateTime(year, 12, 31, 23, 59, 59);
        
        var salesData = await _context.Invoices
            .Where(i => i.BusinessID == businessId && i.Status != "Cancelled" && 
                        i.InvoiceDate >= startDate && i.InvoiceDate <= endDate)
            .GroupBy(i => new { i.InvoiceDate.Year, i.InvoiceDate.Month })
            .Select(g => new 
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Total = g.Sum(i => (double)i.GrandTotal) 
            })
            .ToListAsync();

        var result = new List<ChartDataPoint>();
        for (int m = 1; m <= 12; m++)
        {
            var monthData = salesData.FirstOrDefault(d => d.Month == m);
            var date = new DateTime(year, m, 1);
            result.Add(new ChartDataPoint
            {
                Date = date,
                Label = date.ToString("MMM"),
                Value = monthData?.Total ?? 0
            });
        }
                
        return result.OrderBy(x => x.Date).ToList();
    }

    public async Task<List<VendorPerformanceStats>> GetVendorPerformanceAsync(int businessId)
    {
        var data = await _context.PurchaseOrders
            .Where(p => p.BusinessId == businessId && p.Status != "Cancelled")
            .GroupBy(p => p.VendorId)
            .Select(g => new VendorPerformanceStats
            {
                VendorId = g.Key,
                VendorName = g.Select(x => x.Vendor != null ? x.Vendor.VendorName : "Unknown Vendor").FirstOrDefault() ?? "Unknown Vendor",
                // Keep the sum as double for the DB calculation
                TotalPurchaseVolume = (decimal)g.Sum(p => (double)p.GrandTotal),
                OrderCount = g.Count(),
                LastPurchaseDate = g.Max(p => p.PODate)
            })
            .ToListAsync(); // Fetch to memory first

        // Now sort in C# where decimals work perfectly
        return data.OrderByDescending(v => v.TotalPurchaseVolume)
                .Take(10)
                .ToList();
    }

    public async Task<List<CustomerInsightStats>> GetCustomerInsightsAsync(int businessId)
    {
        var data = await _context.Invoices
            .Where(i => i.BusinessID == businessId && i.Status != "Cancelled")
            .GroupBy(i => i.CustomerID)
            .Select(g => new CustomerInsightStats
            {
                CustomerId = g.Key,
                CustomerName = g.Select(x => x.Customer != null ? x.Customer.CustomerName : "Unknown Customer").FirstOrDefault() ?? "Unknown Customer",
                TotalSpent = (decimal)g.Sum(i => (double)i.GrandTotal),
                OrderCount = g.Count(),
                LastOrderDate = g.Max(i => i.InvoiceDate)
            })
            .ToListAsync(); // Fetch to memory first

        return data.OrderByDescending(c => c.TotalSpent)
                .Take(10)
                .ToList();
    }

    public async Task<GstReportItem> GetGstReportAsync(int businessId, DateTime startDate, DateTime endDate)
    {
        // This is a simplified GST report aggregating all rates
        // In a real scenario, you'd group by rate (5%, 12%, 18%)
        
        var invoices = await _context.Invoices
            .Where(i => i.BusinessID == businessId && i.Status != "Cancelled" && 
                        i.InvoiceDate >= startDate && i.InvoiceDate <= endDate)
            .ToListAsync();

        var purchases = await _context.PurchaseOrders
            .Where(p => p.BusinessId == businessId && p.Status != "Cancelled" && 
                        p.PODate >= startDate && p.PODate <= endDate)
            .ToListAsync();

        var report = new GstReportItem
        {
            TaxableValue = invoices.Sum(i => i.TotalAmount),
            OutputTaxAmount = invoices.Sum(i => i.TotalTax), // Collected
            InputTaxAmount = purchases.Sum(p => p.TotalTax), // Paid
            // TaxRate is averaged or handled per-line item usually, leaving 0 for summary
        };

        return report;
    }

    public async Task<List<BusinessSuite.DAL.Entities.Invoice>> GetRecentInvoicesAsync(int businessId, int count = 5)
    {
        return await _context.Invoices
            .Include(i => i.Customer)
            .Where(i => i.BusinessID == businessId && i.Status != "Cancelled")
            .OrderByDescending(i => i.InvoiceDate)
            .Take(count)
            .ToListAsync();
    }
}
