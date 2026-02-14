using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessSuite.DAL.Entities;

public class FinanceLedger
{
    [Key]
    public int LedgerID { get; set; }

    [Required]
    public int BusinessId { get; set; }

    [ForeignKey("BusinessId")]
    public Business? Business { get; set; }

    [Required]
    public DateTime TransactionDate { get; set; } = DateTime.Now;

    [Required]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(20)]
    public string Type { get; set; } = "Debit"; // Debit or Credit

    [Required]
    [MaxLength(50)]
    public string RelatedEntity { get; set; } = string.Empty; // Customer, Vendor, Expense, etc.

    public int? RelatedEntityID { get; set; } // ID of Customer/Vendor

    [MaxLength(50)]
    public string? ReferenceType { get; set; } // Invoice, PO, Payment

    public int? ReferenceID { get; set; } // InvoiceID or PurchaseOrderID

    [MaxLength(255)]
    public string? Description { get; set; }
}
