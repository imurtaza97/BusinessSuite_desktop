using BusinessSuite.DAL.Entities;

namespace BusinessSuite.BLL.DTOs;

public class RegistrationRequest
{
    // Business
    public string BusinessName { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string? ContactNo { get; set; } = string.Empty;
    public bool IsGSTRegistered { get; set; } = false;
    public string? GSTIN { get; set; }   
    public BusinessGstType? GstType { get; set; }

    // Admin User
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    // Settings (System)
    public string DefaultCurrency { get; set; } = string.Empty;
    public string TimeZone { get; set; } = string.Empty;
    public string DateFormat { get; set; }  = string.Empty;
}
