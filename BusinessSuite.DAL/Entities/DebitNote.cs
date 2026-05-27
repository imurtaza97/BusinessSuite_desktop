using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessSuite.DAL.Entities;

/// <summary>
/// Debit Notes for GST-compliant document amendments
/// Used when increasing invoice amount (additional items, late delivery charges)
/// </summary>
public class DebitNote
{
    [Key]
    public int DebitNoteID { get; set; }

    [Required]
    public int BusinessID { get; set; }

    [Required]
    public int OriginalInvoiceID { get; set; }

    [Required]
    [MaxLength(50)]
    public string DebitNoteNumber { get; set; } = string.Empty; // Format: INV-001-DN-01

    [Required]
    public DateTime DebitNoteDate { get; set; }

    [Required]
    [MaxLength(1000)]
    public string Reason { get; set; } = string.Empty; // "Additional items", "Surcharge", etc.

    [MaxLength(500)]
    public string? Notes { get; set; }

    public decimal SubTotal { get; set; }
    public decimal TotalCGST { get; set; }
    public decimal TotalSGST { get; set; }
    public decimal TotalIGST { get; set; }
    public decimal GrandTotal { get; set; }

    [Required]
    [MaxLength(10)]
    public string Status { get; set; } = "Draft"; // "Draft", "Finalized", "Cancelled"

    public bool IsDraft { get; set; } = true;

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public int? DeletedByUserID { get; set; }
    public string? DeletionReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int CreatedByUserID { get; set; }

    public DateTime? ModifiedAt { get; set; }
    public int? ModifiedByUserID { get; set; }

    public DateTime? FinalizedAt { get; set; }
    public int? FinalizedByUserID { get; set; }

    public DateTime? CancelledAt { get; set; }
    public int? CancelledByUserID { get; set; }
    public string? CancellationReason { get; set; }

    // Navigation properties
    [ForeignKey("BusinessID")]
    public Business? Business { get; set; }

    [ForeignKey("OriginalInvoiceID")]
    public Invoice? OriginalInvoice { get; set; }

    [ForeignKey("CreatedByUserID")]
    public User? CreatedByUser { get; set; }

    [ForeignKey("ModifiedByUserID")]
    public User? ModifiedByUser { get; set; }

    [ForeignKey("FinalizedByUserID")]
    public User? FinalizedByUser { get; set; }

    [ForeignKey("CancelledByUserID")]
    public User? CancelledByUser { get; set; }

    [ForeignKey("DeletedByUserID")]
    public User? DeletedByUser { get; set; }

    public virtual ICollection<DebitNoteItem> DebitNoteItems { get; set; } = new List<DebitNoteItem>();
}
