using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessSuite.DAL.Data;
using BusinessSuite.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessSuite.BLL.Services;

public class DashboardSummary
{
    public decimal TotalSales { get; set; }
    public decimal TotalPurchase { get; set; }
    public decimal TotalGst { get; set; }
    public int NewOrdersCount { get; set; }
    public int CustomerCount { get; set; }
    public decimal Turnover { get; set; }
    public List<RecentTransaction> RecentTransactions { get; set; } = new();
}

public class RecentTransaction
{
    public string Number { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Sale or Purchase
}

public class AnalyticsService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public AnalyticsService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<DashboardSummary> GetDashboardSummaryAsync(int businessId)
    {
        using var db = _dbFactory.CreateDbContext();
        var summary = new DashboardSummary();

        // Current month sales
        var now = DateTime.Now;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);

        var sales = await db.Invoices
            .Where(i => i.BusinessID == businessId && i.Status != "Cancelled")
            .ToListAsync();

        var purchases = await db.PurchaseOrders
            .Where(p => p.BusinessId == businessId && p.Status != "Cancelled")
            .ToListAsync();

        summary.TotalSales = sales.Where(i => i.InvoiceDate >= startOfMonth).Sum(i => i.GrandTotal);
        summary.TotalPurchase = purchases.Where(i => i.PODate >= startOfMonth).Sum(i => i.GrandTotal);
        summary.TotalGst = sales.Sum(i => i.TotalTax);
        summary.Turnover = sales.Sum(i => i.GrandTotal);
        summary.NewOrdersCount = sales.Count(i => i.InvoiceDate >= startOfMonth);
        summary.CustomerCount = await db.Customers.CountAsync(c => c.BusinessId == businessId);
        // Recent transactions
        var recentSales = sales.OrderByDescending(i => i.InvoiceDate).Take(5).Select(i => new RecentTransaction
        {
            Number = i.InvoiceNumber,
            EntityName = db.Customers.FirstOrDefault(c => c.CustomerID == i.CustomerID)?.CustomerName ?? "Unknown",
            Date = i.InvoiceDate,
            Amount = i.GrandTotal,
            Status = i.Status,
            Type = "Sale"
        });

        var recentPurchases = purchases.OrderByDescending(p => p.PODate).Take(5).Select(p => new RecentTransaction
        {
            Number = p.PONumber,
            EntityName = db.Vendors.FirstOrDefault(v => v.VendorID == p.VendorId)?.VendorName ?? "Unknown",
            Date = p.PODate,
            Amount = p.GrandTotal,
            Status = p.Status,
            Type = "Purchase"
        });

        summary.RecentTransactions = recentSales.Concat(recentPurchases)
            .OrderByDescending(t => t.Date)
            .Take(10)
            .ToList();

        return summary;
    }

    public async Task<List<FinancialReportRow>> GetFinancialBreakdownAsync(int businessId, DateTime start, DateTime end)
    {
        using var db = _dbFactory.CreateDbContext();
        
        var sales = await db.Invoices
            .Where(i => i.BusinessID == businessId && i.InvoiceDate >= start && i.InvoiceDate <= end && i.Status != "Cancelled")
            .OrderBy(i => i.InvoiceDate)
            .ToListAsync();

        var purchases = await db.PurchaseOrders
            .Where(p => p.BusinessId == businessId && p.PODate >= start && p.PODate <= end && p.Status != "Cancelled")
            .OrderBy(p => p.PODate)
            .ToListAsync();

        var report = new List<FinancialReportRow>();

        // Group by month
        var months = sales.Select(s => new DateTime(s.InvoiceDate.Year, s.InvoiceDate.Month, 1))
            .Union(purchases.Select(p => new DateTime(p.PODate.Year, p.PODate.Month, 1)))
            .Distinct()
            .OrderBy(d => d);

        foreach (var month in months)
        {
            var nextMonth = month.AddMonths(1);
            var monthSales = sales.Where(s => s.InvoiceDate >= month && s.InvoiceDate < nextMonth).ToList();
            var monthPurchases = purchases.Where(p => p.PODate >= month && p.PODate < nextMonth).ToList();

            report.Add(new FinancialReportRow
            {
                Month = month.ToString("MMM yyyy"),
                SalesAmount = monthSales.Sum(s => s.GrandTotal),
                PurchaseAmount = monthPurchases.Sum(p => p.GrandTotal),
                GstCollected = monthSales.Sum(s => s.TotalTax),
                GstPaid = monthPurchases.Sum(p => p.TotalTax),
                Profit = monthSales.Sum(s => s.GrandTotal) - monthPurchases.Sum(p => p.GrandTotal)
            });
        }

        return report;
    }
}

public class FinancialReportRow
{
    public string Month { get; set; } = string.Empty;
    public decimal SalesAmount { get; set; }
    public decimal PurchaseAmount { get; set; }
    public decimal GstCollected { get; set; }
    public decimal GstPaid { get; set; }
    public decimal Profit { get; set; }
}
