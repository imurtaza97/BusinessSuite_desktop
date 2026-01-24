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

    [MaxLength(10)]
    public string? UOM { get; set; } = "PCS";

    [MaxLength(50)]
    public string? Category { get; set; }

    [Required]
    public decimal PurchasePrice { get; set; }

    [Required]
    public decimal SalePrice { get; set; }

    public int StockQty { get; set; } = 0;

    [Required]
    public decimal TaxRate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public override string ToString() => ProductName;
}
