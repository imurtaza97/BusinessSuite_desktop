using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessSuite.DAL.Entities;

/// <summary>
/// Represents an Indian fiscal financial year (typically Apr 1 – Mar 31).
/// Only one FinancialYear can be active (IsActive=true) per business at a time.
/// Once closed (IsClosed=true) the year cannot be re-opened.
/// </summary>
public class FinancialYear
{
    [Key]
    public int FinancialYearID { get; set; }

    /// <summary>FK to the owning business.</summary>
    [Required]
    public int BusinessId { get; set; }

    [ForeignKey(nameof(BusinessId))]
    public Business? Business { get; set; }

    /// <summary>Display label, e.g. "FY 2024-25".</summary>
    [Required]
    [MaxLength(20)]
    public string Label { get; set; } = string.Empty;

    /// <summary>First day of the financial year, e.g. 2024-04-01.</summary>
    [Required]
    public DateTime StartDate { get; set; }

    /// <summary>Last day of the financial year, e.g. 2025-03-31.</summary>
    [Required]
    public DateTime EndDate { get; set; }

    /// <summary>True if this is the currently active / working FY.
    /// Only one FY per business may have IsActive=true.</summary>
    public bool IsActive { get; set; } = false;

    /// <summary>True once the year has been formally closed. Cannot be reversed.</summary>
    public bool IsClosed { get; set; } = false;

    /// <summary>UTC timestamp when this FY was closed (null if still open).</summary>
    public DateTime? ClosedAt { get; set; }

    /// <summary>Whether the closing balance was carried forward to the next FY's opening balance.</summary>
    public bool OpeningBalanceCarriedForward { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ── Computed helpers (not stored) ───────────────────────────────────────
    [NotMapped]
    public string StatusLabel =>
        IsClosed ? $"Closed ({ClosedAt:dd MMM yyyy})"
        : IsActive ? "Active"
        : "Inactive";

    [NotMapped]
    public string PeriodLabel =>
        $"{StartDate:dd MMM yyyy}  –  {EndDate:dd MMM yyyy}";
}
