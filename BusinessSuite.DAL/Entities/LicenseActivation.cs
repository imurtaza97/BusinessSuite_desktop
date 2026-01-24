namespace BusinessSuite.DAL.Entities;

public class LicenseActivation
{
    public int LicenseActivationID { get; set; }

    public int? BusinessID { get; set; }

    public string LicenseKeyHash { get; set; } = string.Empty;

    public string HardwareIdHash { get; set; } = string.Empty;

    public DateTime ActivatedOn { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresOn { get; set; }

    public bool IsValid { get; set; } = true;
}