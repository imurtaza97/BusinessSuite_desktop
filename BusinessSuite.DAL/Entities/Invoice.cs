using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessSuite.DAL.Entities;

public class Invoice
{
    [Key]
    public int InvoiceID { get; set; }

    [Required]
    public int BusinessID { get; set; }

    [Required]
    public int CustomerID { get; set; }

    [Required]
    [MaxLength(50)]
    public string InvoiceNumber { get; set; } = string.Empty;

    [Required]
    public DateTime InvoiceDate { get; set; } = DateTime.Now;

    public DateTime? DueDate { get; set; }

    public bool IsAutoRoundOff { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal TotalAmount { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal TotalTax { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Discount { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal GrandTotal { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal TotalPaid { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [MaxLength(20)]
    public string DeliveryStatus { get; set; } = "Pending"; // Pending, Shipped, Returned, Cancelled

    [MaxLength(20)]
    public string PaymentStatus { get; set; } = "Unpaid"; // Unpaid, Paid, Partially Paid

    public bool IsItemLevelDiscount { get; set; }

    [MaxLength(50)]
    public string? PaymentMethod { get; set; }

    [MaxLength(100)]
    public string? PaymentTerms { get; set; }

    public string? TermsAndConditions { get; set; }
 
    [MaxLength(50)]
    public string? PlaceOfSupply { get; set; }
 
    public bool ReverseCharge { get; set; }
 
    [Column(TypeName = "decimal(18, 2)")]
    public decimal RoundOff { get; set; }
 
    [Column(TypeName = "decimal(18, 2)")]
    public decimal TotalCGST { get; set; }
 
    [Column(TypeName = "decimal(18, 2)")]
    public decimal TotalSGST { get; set; }
 
    [Column(TypeName = "decimal(18, 2)")]
    public decimal TotalIGST { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal ShippingCharges { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public bool IsDraft { get; set; } = false;

    // Audit Trail Fields
    public int? CreatedByUserID { get; set; }

    public DateTime? ModifiedAt { get; set; }
    public int? ModifiedByUserID { get; set; }

    public DateTime? PostedAt { get; set; }
    public int? PostedByUserID { get; set; }

    public DateTime? CancelledAt { get; set; }
    public int? CancelledByUserID { get; set; }

    [MaxLength(500)]
    public string? CancellationReason { get; set; }

    // Soft Delete Fields
    public bool IsDeleted { get; set; } = false;

    public DateTime? DeletedAt { get; set; }

    public int? DeletedByUserID { get; set; }

    [MaxLength(500)]
    public string? DeletionReason { get; set; }

    [ForeignKey("BusinessID")]
    public Business? Business { get; set; }

    [ForeignKey("CustomerID")]
    public Customer? Customer { get; set; }

    [ForeignKey("CreatedByUserID")]
    public User? CreatedByUser { get; set; }

    [ForeignKey("ModifiedByUserID")]
    public User? ModifiedByUser { get; set; }

    [ForeignKey("PostedByUserID")]
    public User? PostedByUser { get; set; }

    [ForeignKey("CancelledByUserID")]
    public User? CancelledByUser { get; set; }

    [ForeignKey("DeletedByUserID")]
    public User? DeletedByUser { get; set; }

    public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
}
