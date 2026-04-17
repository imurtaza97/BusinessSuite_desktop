using System;
using System.ComponentModel.DataAnnotations;

namespace BusinessSuite.DAL.Entities;

public enum BusinessGstType
{
    Regular,
    Composition
}

public class Business
{
    public int BusinessID { get; set; }

    public string BusinessName { get; set; } = string.Empty;

    public string OwnerName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? ContactNo { get; set; }

    public string? Address { get; set; }

    public string? State { get; set; }

    public string PasswordHash { get; set; } = string.Empty;

    public string? LogoUrl { get; set; }

    public string? PAN { get; set; }

    public bool IsGSTRegistered { get; set; } = false;

    public string? GSTIN { get; set; }

    public BusinessGstType? GstType { get; set; }

    [MaxLength(100)]
    public string? BankName { get; set; }

    [MaxLength(100)]
    public string? AccountName { get; set; }

    [MaxLength(30)]
    public string? AccountNumber { get; set; }

    [MaxLength(20)]
    public string? IFSC { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
