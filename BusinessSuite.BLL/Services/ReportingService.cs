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
            .Where(i => i.BusinessID == businessId && i.DeliveryStatus != "Cancelled")
            .ToListAsync();

        var purchaseOrders = await _context.PurchaseOrders
            .Where(p => p.BusinessId == businessId && p.DeliveryStatus != "Cancelled")
            .ToListAsync();

        summary.TotalSales = invoices.Sum(i => i.GrandTotal);
        summary.TotalPurchases = purchaseOrders.Sum(p => p.GrandTotal);
        summary.NetProfit = summary.TotalSales - summary.TotalPurchases;
        summary.ActiveOrdersCount = invoices.Count(i => i.PaymentStatus == "Unpaid");
        summary.TotalReceivable = invoices.Where(i => i.PaymentStatus == "Unpaid").Sum(i => i.GrandTotal);

        return summary;
    }

    // Inside ReportingService.cs
    public async Task<List<ChartDataPoint>> GetSalesAnalyticsAsync(int businessId, int year = 0)
    {
        if (year == 0) year = DateTime.Now.Year;
        
        var startDate = new DateTime(year, 1, 1);
        var endDate = new DateTime(year, 12, 31, 23, 59, 59);
        
        var salesData = await _context.Invoices
            .Where(i => i.BusinessID == businessId && i.DeliveryStatus != "Cancelled" && 
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
            .Where(p => p.BusinessId == businessId && p.DeliveryStatus != "Cancelled")
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
            .Where(i => i.BusinessID == businessId && i.DeliveryStatus != "Cancelled")
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
            .Where(i => i.BusinessID == businessId && i.DeliveryStatus != "Cancelled" && 
                        i.InvoiceDate >= startDate && i.InvoiceDate <= endDate)
            .ToListAsync();

        var purchases = await _context.PurchaseOrders
            .Where(p => p.BusinessId == businessId && p.DeliveryStatus != "Cancelled" && 
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
            .Where(i => i.BusinessID == businessId && i.DeliveryStatus != "Cancelled")
            .OrderByDescending(i => i.InvoiceDate)
            .Take(count)
            .ToListAsync();
    }

    // GSTR-1: HSN-wise Summary of Outward Supplies (Sales)
    public async Task<List<Gstr1Item>> GetGstr1ReportAsync(int businessId, DateTime startDate, DateTime endDate)
    {
        var items = await _context.InvoiceItems
            .AsNoTracking()
            .Include(ii => ii.Invoice!)
            .ThenInclude(i => i.Customer)
            .Where(ii => ii.Invoice!.BusinessID == businessId && 
                         ii.Invoice.DeliveryStatus != "Cancelled" &&
                         ii.Invoice.InvoiceDate >= startDate && 
                         ii.Invoice.InvoiceDate <= endDate)
            .Select(ii => new Gstr1Item
            {
                InvoiceNumber = ii.Invoice!.InvoiceNumber,
                InvoiceDate = ii.Invoice.InvoiceDate,
                CustomerName = ii.Invoice.Customer != null ? ii.Invoice.Customer.CustomerName : "Unknown",
                CustomerGstin = ii.Invoice.Customer!.GSTIN,
                CustomerState = ii.Invoice.Customer.State,
                GstTreatment = ii.Invoice.Customer.GstTreatment,
                HsnCode = ii.HSNCode,
                ItemType = ii.ItemType,
                Quantity = ii.Quantity,
                UnitPrice = ii.UnitPrice,
                TaxableAmount = ii.TotalAmount,
                TaxRate = ii.TaxRate,
                CgstAmount = ii.CGST_Amount,
                SgstAmount = ii.SGST_Amount,
                IgstAmount = ii.IGST_Amount
            })
            .ToListAsync();

        return items.OrderBy(x => x.HsnCode).ThenBy(x => x.InvoiceNumber).ToList();
    }

    // GSTR-3B: Tax Summary by Customer Type and Rate
    public async Task<List<Gstr3BItem>> GetGstr3BReportAsync(int businessId, DateTime startDate, DateTime endDate)
    {
        var invoiceItems = await _context.InvoiceItems
            .AsNoTracking()
            .Include(ii => ii.Invoice!)
            .ThenInclude(i => i.Customer)
            .Where(ii => ii.Invoice!.BusinessID == businessId && 
                         ii.Invoice.DeliveryStatus != "Cancelled" &&
                         ii.Invoice.InvoiceDate >= startDate && 
                         ii.Invoice.InvoiceDate <= endDate)
            .ToListAsync();

        // Group by customer type and tax rate
        var groupedData = invoiceItems
            .GroupBy(ii => new 
            { 
                CustomerType = string.IsNullOrEmpty(ii.Invoice!.Customer!.GSTIN) ? "B2C" : "B2B",
                TaxRate = ii.TaxRate 
            })
            .Select(g => new Gstr3BItem
            {
                CustomerType = g.Key.CustomerType,
                TaxRate = g.Key.TaxRate,
                TransactionCount = g.Count(),
                TaxableAmount = g.Sum(x => x.TotalAmount),
                CgstAmount = g.Sum(x => x.CGST_Amount),
                SgstAmount = g.Sum(x => x.SGST_Amount),
                IgstAmount = g.Sum(x => x.IGST_Amount)
            })
            .OrderBy(x => x.CustomerType)
            .ThenBy(x => x.TaxRate)
            .ToList();

        return groupedData;
    }

    // Tax Liability Report: Monthly/Quarterly breakdown
    public async Task<List<TaxLiabilityReportItem>> GetTaxLiabilityReportAsync(int businessId, DateTime startDate, DateTime endDate, string period = "month")
    {
        var invoices = await _context.Invoices
            .AsNoTracking()
            .Where(i => i.BusinessID == businessId && 
                        i.DeliveryStatus != "Cancelled" &&
                        i.InvoiceDate >= startDate && 
                        i.InvoiceDate <= endDate)
            .ToListAsync();

        var purchaseOrders = await _context.PurchaseOrders
            .AsNoTracking()
            .Where(p => p.BusinessId == businessId && 
                        p.DeliveryStatus != "Cancelled" &&
                        p.PODate >= startDate && 
                        p.PODate <= endDate)
            .ToListAsync();

        var result = new List<TaxLiabilityReportItem>();

        if (period == "month")
        {
            // Group by Month
            var months = new Dictionary<string, TaxLiabilityReportItem>();
            
            foreach (var inv in invoices)
            {
                var key = inv.InvoiceDate.ToString("yyyy-MM");
                if (!months.ContainsKey(key))
                {
                    months[key] = new TaxLiabilityReportItem { Period = key };
                }
                months[key].SalesCount++;
                months[key].SalesTaxableAmount += inv.TotalAmount;
                months[key].OutgoingCgst += inv.TotalCGST;
                months[key].OutgoingSgst += inv.TotalSGST;
                months[key].OutgoingIgst += inv.TotalIGST;
            }

            foreach (var po in purchaseOrders)
            {
                var key = po.PODate.ToString("yyyy-MM");
                if (!months.ContainsKey(key))
                {
                    months[key] = new TaxLiabilityReportItem { Period = key };
                }
                months[key].PurchaseCount++;
                months[key].PurchasesTaxableAmount += po.TotalAmount;
                months[key].IncomingCgst += po.TotalCGST;
                months[key].IncomingSgst += po.TotalSGST;
                months[key].IncomingIgst += po.TotalIGST;
            }

            result = months.Values.OrderBy(x => x.Period).ToList();
        }
        else if (period == "quarter")
        {
            // Group by Quarter
            var quarters = new Dictionary<string, TaxLiabilityReportItem>();
            
            foreach (var inv in invoices)
            {
                var quarter = (inv.InvoiceDate.Month - 1) / 3 + 1;
                var key = $"{inv.InvoiceDate.Year}-Q{quarter}";
                if (!quarters.ContainsKey(key))
                {
                    quarters[key] = new TaxLiabilityReportItem { Period = key };
                }
                quarters[key].SalesCount++;
                quarters[key].SalesTaxableAmount += inv.TotalAmount;
                quarters[key].OutgoingCgst += inv.TotalCGST;
                quarters[key].OutgoingSgst += inv.TotalSGST;
                quarters[key].OutgoingIgst += inv.TotalIGST;
            }

            foreach (var po in purchaseOrders)
            {
                var quarter = (po.PODate.Month - 1) / 3 + 1;
                var key = $"{po.PODate.Year}-Q{quarter}";
                if (!quarters.ContainsKey(key))
                {
                    quarters[key] = new TaxLiabilityReportItem { Period = key };
                }
                quarters[key].PurchaseCount++;
                quarters[key].PurchasesTaxableAmount += po.TotalAmount;
                quarters[key].IncomingCgst += po.TotalCGST;
                quarters[key].IncomingSgst += po.TotalSGST;
                quarters[key].IncomingIgst += po.TotalIGST;
            }

            result = quarters.Values.OrderBy(x => x.Period).ToList();
        }

        return result;
    }

    // HSN-wise Summary: Product Report
    public async Task<List<HsnSummaryItem>> GetHsnSummaryReportAsync(int businessId, DateTime startDate, DateTime endDate)
    {
        var items = await _context.InvoiceItems
            .AsNoTracking()
            .Include(ii => ii.Invoice)
            .Where(ii => ii.Invoice!.BusinessID == businessId && 
                         ii.Invoice.DeliveryStatus != "Cancelled" &&
                         ii.Invoice.InvoiceDate >= startDate && 
                         ii.Invoice.InvoiceDate <= endDate)
            .ToListAsync();

        var grouped = items
            .GroupBy(ii => new { ii.HSNCode, ii.ItemType, ii.TaxRate, ii.CGST_Rate, ii.SGST_Rate, ii.IGST_Rate })
            .Select(g => new HsnSummaryItem
            {
                HsnCode = g.Key.HSNCode,
                ItemType = g.Key.ItemType,
                TaxRate = g.Key.TaxRate,
                TotalQuantity = g.Sum(x => x.Quantity),
                TaxableAmount = g.Sum(x => x.TotalAmount),
                CgstRate = g.Key.CGST_Rate,
                CgstAmount = g.Sum(x => x.CGST_Amount),
                SgstRate = g.Key.SGST_Rate,
                SgstAmount = g.Sum(x => x.SGST_Amount),
                IgstRate = g.Key.IGST_Rate,
                IgstAmount = g.Sum(x => x.IGST_Amount)
            })
            .OrderBy(x => x.HsnCode ?? "")
            .ThenBy(x => x.ItemType)
            .ToList();

        return grouped;
    }
}
