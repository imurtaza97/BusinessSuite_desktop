using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessSuite.DAL.Entities;

public class Product
{
    [Key]
    public int ProductID { get; set; }

    [Required]
    public int BusinessID { get; set; }

    [ForeignKey("BusinessID")]
    public Business? Business { get; set; }

    [MaxLength(100)]
    public string ProductName { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? SKU { get; set; }

    [MaxLength(20)]
    public string? HSNCode { get; set; }

    [Required]
    [MaxLength(10)]
    public string? Unit { get; set; } = "nos";

    public int? CategoryID { get; set; }

    [ForeignKey("CategoryID")]
    public Category? Category { get; set; }

    public int? PreferredVendorID { get; set; }

    [ForeignKey("PreferredVendorID")]
    public Vendor? PreferredVendor { get; set; }

    [Required]
    public decimal PurchasePrice { get; set; }

    [Required]
    public decimal SalePrice { get; set; }

    public decimal StockQty { get; set; } = 0;

    public bool IsDraft { get; set; } = false;

    public bool IsService { get; set; } = false;

    [NotMapped]
    public string Type => IsService ? "Service" : "Product";

    [Required]
    public decimal TaxRate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public override string ToString() => ProductName;
}
