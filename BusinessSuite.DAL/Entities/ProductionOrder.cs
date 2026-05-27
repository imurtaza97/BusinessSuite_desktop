using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessSuite.DAL.Entities;

/// <summary>
/// Production Orders for manufacturing workflow
/// Tracks manufacturing orders from creation through completion
/// </summary>
public class ProductionOrder
{
    [Key]
    public int ProductionOrderID { get; set; }

    [Required]
    public int BusinessID { get; set; }

    [Required]
    public int ProductID { get; set; } // The product being manufactured

    [Required]
    [MaxLength(50)]
    public string ProductionOrderNumber { get; set; } = string.Empty; // Format: PO-001

    [Required]
    public decimal QuantityToMake { get; set; }

    [Required]
    [MaxLength(20)]
    public string UnitOfMeasure { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime ExpectedEndDate { get; set; }

    public DateTime? ActualEndDate { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Pending"; // "Pending", "In-Progress", "Completed", "Cancelled", "On-Hold"

    [MaxLength(500)]
    public string? Notes { get; set; }

    // Quantities
    public decimal QuantityCompleted { get; set; } = 0;
    public decimal QuantityRejected { get; set; } = 0;

    // Costs
    public decimal EstimatedCost { get; set; }
    public decimal ActualCost { get; set; } = 0;

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public int? DeletedByUserID { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int CreatedByUserID { get; set; }

    public DateTime? ModifiedAt { get; set; }
    public int? ModifiedByUserID { get; set; }

    // Navigation properties
    [ForeignKey("BusinessID")]
    public Business? Business { get; set; }

    [ForeignKey("ProductID")]
    public Product? Product { get; set; }

    [ForeignKey("CreatedByUserID")]
    public User? CreatedByUser { get; set; }

    [ForeignKey("ModifiedByUserID")]
    public User? ModifiedByUser { get; set; }

    [ForeignKey("DeletedByUserID")]
    public User? DeletedByUser { get; set; }
}
