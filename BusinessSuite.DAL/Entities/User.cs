namespace BusinessSuite.DAL.Entities;

public enum Designation
{
    Owner,
    Admin,
    Manager,
    Staff
}

public class User
{
    public int UserID { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public Designation? Designation { get; set; }

    public string? ContactNo { get; set; }

    public string? Address { get; set; }

    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}