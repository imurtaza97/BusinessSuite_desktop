using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessSuite.DAL.Entities;

public class PurchaseOrder
{
    [Key]
    public int PurchaseOrderID { get; set; }

    [Required]
    public int BusinessId { get; set; }

    [Required]
    public int VendorId { get; set; }

    [Required]
    [MaxLength(50)]
    public string PONumber { get; set; } = string.Empty;

    [Required]
    public DateTime PODate { get; set; } = DateTime.Now;

    public DateTime? ExpectedDeliveryDate { get; set; }

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
    public string DeliveryStatus { get; set; } = "Pending"; // Pending, Received, Returned-to-Vendor, Cancelled

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

    [MaxLength(500)]
    public string? VendorBillPath { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public bool IsDraft { get; set; } = false;

    [ForeignKey("BusinessId")]
    public Business? Business { get; set; }

    [ForeignKey("VendorId")]
    public Vendor? Vendor { get; set; }

    public ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
}
