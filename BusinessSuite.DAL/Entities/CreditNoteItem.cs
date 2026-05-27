using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessSuite.DAL.Entities;

/// <summary>
/// Line items for credit notes with full GST tax breakdown
/// </summary>
public class CreditNoteItem
{
    [Key]
    public int CreditNoteItemID { get; set; }

    [Required]
    public int CreditNoteID { get; set; }

    [Required]
    public int OriginalInvoiceItemID { get; set; }

    [Required]
    public int ProductID { get; set; }

    [Required]
    [MaxLength(20)]
    public string ItemType { get; set; } = "Product"; // "Product" or "Service"

    [Required]
    [MaxLength(12)]
    public string HSNCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public decimal Quantity { get; set; }

    [Required]
    [MaxLength(20)]
    public string UnitOfMeasure { get; set; } = string.Empty;

    [Required]
    public decimal UnitPrice { get; set; }

    [Required]
    public decimal LineTotal { get; set; } // Quantity * UnitPrice

    // GST Tax Breakdown
    public decimal CGST_Rate { get; set; }
    public decimal CGST_Amount { get; set; }

    public decimal SGST_Rate { get; set; }
    public decimal SGST_Amount { get; set; }

    public decimal IGST_Rate { get; set; }
    public decimal IGST_Amount { get; set; }

    public decimal TotalTax { get; set; } // CGST_Amount + SGST_Amount + IGST_Amount
    public decimal GrandTotal { get; set; } // LineTotal + TotalTax

    // Navigation properties
    [ForeignKey("CreditNoteID")]
    public CreditNote? CreditNote { get; set; }

    [ForeignKey("ProductID")]
    public Product? Product { get; set; }
}
