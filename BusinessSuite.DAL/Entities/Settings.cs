namespace BusinessSuite.DAL.Entities;

public class Settings
{
    public int SettingsID { get; set; }

    public int? BusinessID { get; set; }

    public string Theme { get; set; } = "Light";

    public bool EnableNotifications { get; set; } = true;

    public string Language { get; set; } = "en-US";

    public string DateFormat { get; set; } = "MM/dd/yyyy";

    public string TimeZone { get; set; } = "IST";

    public string Currency { get; set; } = "INR";

    public bool AutoBackup { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}