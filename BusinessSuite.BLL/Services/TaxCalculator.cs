using System;

namespace BusinessSuite.BLL.Services;

public class TaxCalculator
{
    public class TaxBreakdown
    {
        public decimal TotalTaxAmount { get; set; }
        public decimal CGST { get; set; }
        public decimal SGST { get; set; }
        public decimal IGST { get; set; }
        public bool IsInterState { get; set; }
        public bool IsITCBlocked { get; set; } = false;
    }

    /// <summary>
    /// Calculates the GST breakdown based on business and customer locations.
    /// </summary>
    /// <param name="baseAmount">The taxable amount.</param>
    /// <param name="gstPercentage">The GST rate percentage (e.g., 18).</param>
    /// <param name="businessState">State of the business.</param>
    /// <param name="customerState">State of the customer.</param>
    /// <returns>TaxBreakdown containing the split components.</returns>
    public TaxBreakdown CalculateTax(decimal baseAmount, decimal gstPercentage, string? businessState, string? customerState)
    {
        var result = new TaxBreakdown();
        
        // Calculate total tax first
        result.TotalTaxAmount = Math.Round(baseAmount * (gstPercentage / 100), 2);

        // Determine if Inter-state (IGST) or Intra-state (CGST + SGST)
        // Note: Comparison is case-insensitive and trims whitespace.
        bool isInterState = !string.Equals(businessState?.Trim(), customerState?.Trim(), StringComparison.OrdinalIgnoreCase);
        result.IsInterState = isInterState;

        if (isInterState)
        {
            result.IGST = result.TotalTaxAmount;
            result.CGST = 0;
            result.SGST = 0;
        }
        else
        {
            result.IGST = 0;
            // Split total tax into CGST and SGST (50/50)
            result.CGST = Math.Round(result.TotalTaxAmount / 2, 2);
            result.SGST = result.TotalTaxAmount - result.CGST; // Handle rounding difference
        }

        return result;
    }

    /// <summary>
    /// Calculates tax with Composition scheme check. 
    /// Composition businesses cannot claim ITC (Input Tax Credit).
    /// </summary>
    /// <param name="baseAmount">The taxable amount.</param>
    /// <param name="gstPercentage">The GST rate percentage (e.g., 18).</param>
    /// <param name="businessState">State of the business.</param>
    /// <param name="customerState">State of the customer.</param>
    /// <param name="businessGstType">Business GST scheme type (Regular or Composition).</param>
    /// <param name="isForPurchase">True if calculating for purchase (inbound), false for sales (outbound).</param>
    /// <returns>TaxBreakdown with ITC blocked if applicable.</returns>
    public TaxBreakdown CalculateTaxWithCompositionCheck(
        decimal baseAmount, 
        decimal gstPercentage, 
        string? businessState, 
        string? customerState,
        string? businessGstType,
        bool isForPurchase = false)
    {
        var result = CalculateTax(baseAmount, gstPercentage, businessState, customerState);

        // Check if Composition business claiming ITC on purchase
        if (isForPurchase && !string.IsNullOrWhiteSpace(businessGstType) && businessGstType == "Composition")
        {
            // Composition businesses CANNOT claim ITC on purchases
            result.IsITCBlocked = true;
            result.CGST = 0;
            result.SGST = 0;
            result.IGST = 0;
            result.TotalTaxAmount = 0;
        }

        return result;
    }
}
