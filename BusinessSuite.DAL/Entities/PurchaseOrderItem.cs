using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessSuite.DAL.Entities;

public class PurchaseOrderItem
{
    [Key]
    public int PurchaseOrderItemID { get; set; }

    [Required]
    public int PurchaseOrderID { get; set; }

    [Required]
    public int ProductID { get; set; }

    [Required]
    public int Quantity { get; set; }

    [Required]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal UnitPrice { get; set; }

    [Required]
    [Column(TypeName = "decimal(5, 2)")]
    public decimal TaxRate { get; set; }

    [Required]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal TaxAmount { get; set; }

    [MaxLength(20)]
    public string? HSNCode { get; set; }

    [MaxLength(10)]
    public string? Unit { get; set; }

    [Required]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal TotalAmount { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Discount { get; set; }

    // Tax Breakdown
    [Column(TypeName = "decimal(5, 2)")]
    public decimal CGST_Rate { get; set; }
    [Column(TypeName = "decimal(18, 2)")]
    public decimal CGST_Amount { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal SGST_Rate { get; set; }
    [Column(TypeName = "decimal(18, 2)")]
    public decimal SGST_Amount { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal IGST_Rate { get; set; }
    [Column(TypeName = "decimal(18, 2)")]
    public decimal IGST_Amount { get; set; }

    [ForeignKey("PurchaseOrderID")]
    public PurchaseOrder? PurchaseOrder { get; set; }

    [ForeignKey("ProductID")]
    public Product? Product { get; set; }
}
