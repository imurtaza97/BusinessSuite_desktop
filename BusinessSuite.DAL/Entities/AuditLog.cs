using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessSuite.DAL.Entities;

/// <summary>
/// Audit trail for tracking all modifications to critical documents
/// Required for GST compliance and financial audits
/// </summary>
public class AuditLog
{
    [Key]
    public int AuditLogID { get; set; }

    [Required]
    public int BusinessID { get; set; }

    [Required]
    [MaxLength(50)]
    public string DocumentType { get; set; } = string.Empty; // "Invoice", "PurchaseOrder", "CreditNote", "DebitNote"

    [Required]
    public int DocumentID { get; set; }

    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty; // "Created", "Modified", "Finalized", "Cancelled", "Unposted"

    [Required]
    [MaxLength(100)]
    public string FieldName { get; set; } = string.Empty; // Field that changed, or "All" for bulk actions

    [MaxLength(500)]
    public string? OldValue { get; set; }

    [MaxLength(500)]
    public string? NewValue { get; set; }

    [Required]
    public int ChangedByUserID { get; set; }

    [Required]
    public DateTime ChangedAt { get; set; } = DateTime.Now;

    [MaxLength(200)]
    public string? IPAddress { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; } // Reason for change (e.g., "Corrected tax calculation", "Customer requested amendment")

    [ForeignKey("BusinessID")]
    public Business? Business { get; set; }

    [ForeignKey("ChangedByUserID")]
    public User? ChangedByUser { get; set; }
}
