using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessSuite.DAL.Entities;

/// <summary>
/// Bill of Materials (BOM) for product manufacturing
/// Defines recipes/formulas showing which raw materials go into finished products
/// </summary>
public class BillOfMaterials
{
    [Key]
    public int BOM_ID { get; set; }

    [Required]
    public int BusinessID { get; set; }

    [Required]
    public int FinishedProductID { get; set; } // The final product being manufactured

    [Required]
    public int RawMaterialProductID { get; set; } // The component/raw material

    [Required]
    public decimal Quantity { get; set; } // Amount of raw material needed per unit of finished product

    [Required]
    [MaxLength(20)]
    public string UnitOfMeasure { get; set; } = string.Empty;

    public decimal WastagePercentage { get; set; } = 0; // Optional wastage allowance (0-100)

    [MaxLength(500)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int CreatedByUserID { get; set; }

    public DateTime? ModifiedAt { get; set; }
    public int? ModifiedByUserID { get; set; }

    // Navigation properties
    [ForeignKey("BusinessID")]
    public Business? Business { get; set; }

    [ForeignKey("FinishedProductID")]
    public Product? FinishedProduct { get; set; }

    [ForeignKey("RawMaterialProductID")]
    public Product? RawMaterialProduct { get; set; }

    [ForeignKey("CreatedByUserID")]
    public User? CreatedByUser { get; set; }

    [ForeignKey("ModifiedByUserID")]
    public User? ModifiedByUser { get; set; }
}
