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

// GSTR-1 Report (Outward Supplies - Sales)
public class Gstr1Item
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerGstin { get; set; }
    public string? CustomerState { get; set; }
    public string? GstTreatment { get; set; }
    public string? HsnCode { get; set; }
    public string ItemType { get; set; } = "Product"; // Product or Service
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal IgstAmount { get; set; }
    public decimal TotalTaxAmount => CgstAmount + SgstAmount + IgstAmount;
}

// GSTR-3B Summary (Tax Liability by Type and Rate)
public class Gstr3BItem
{
    public string CustomerType { get; set; } = string.Empty; // "B2B" (Registered), "B2C" (Unregistered), "Exempt"
    public decimal TaxRate { get; set; }
    public int TransactionCount { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal IgstAmount { get; set; }
    public decimal TotalTaxAmount => CgstAmount + SgstAmount + IgstAmount;
}

// Tax Liability Report (Period-wise)
public class TaxLiabilityReportItem
{
    public string Period { get; set; } = string.Empty; // "2026-01" for month, "2026-Q1" for quarter
    public int SalesCount { get; set; }
    public int PurchaseCount { get; set; }
    public decimal SalesTaxableAmount { get; set; }
    public decimal PurchasesTaxableAmount { get; set; }
    public decimal OutgoingCgst { get; set; }
    public decimal OutgoingSgst { get; set; }
    public decimal OutgoingIgst { get; set; }
    public decimal IncomingCgst { get; set; }
    public decimal IncomingSgst { get; set; }
    public decimal IncomingIgst { get; set; }
    public decimal NetCgstPayable => OutgoingCgst - IncomingCgst;
    public decimal NetSgstPayable => OutgoingSgst - IncomingSgst;
    public decimal NetIgstPayable => OutgoingIgst - IncomingIgst;
    public decimal TotalTaxPayable => NetCgstPayable + NetSgstPayable + NetIgstPayable;
}

// HSN-wise Summary (Product Report)
public class HsnSummaryItem
{
    public string? HsnCode { get; set; }
    public string ItemType { get; set; } = "Product";
    public decimal TaxRate { get; set; }
    public int TotalQuantity { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal CgstRate { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstRate { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal IgstRate { get; set; }
    public decimal IgstAmount { get; set; }
    public decimal TotalTaxAmount => CgstAmount + SgstAmount + IgstAmount;
}
