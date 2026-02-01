using System;

namespace BusinessSuite.BLL.DTOs;

public class DashboardSummary
{
    public decimal TotalSales { get; set; }
    public decimal TotalPurchases { get; set; }
    public decimal NetProfit { get; set; }
    public int ActiveOrdersCount { get; set; }
    public int PendingDeliveriesCount { get; set; }
    public decimal TotalReceivable { get; set; }
}

public class ChartDataPoint
{
    public DateTime Date { get; set; }
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
}

public class VendorPerformanceStats
{
    public int VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public decimal TotalPurchaseVolume { get; set; }
    public int OrderCount { get; set; }
    public DateTime LastPurchaseDate { get; set; }
}

public class CustomerInsightStats
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalSpent { get; set; }
    public int OrderCount { get; set; }
    public DateTime LastOrderDate { get; set; }
}

public class GstReportItem
{
    public decimal TaxRate { get; set; }
    public decimal TaxableValue { get; set; }
    public decimal InputTaxAmount { get; set; } // Paid on Purchase
    public decimal OutputTaxAmount { get; set; } // Collected on Sales
    public decimal NetTaxLiability => OutputTaxAmount - InputTaxAmount;
}
