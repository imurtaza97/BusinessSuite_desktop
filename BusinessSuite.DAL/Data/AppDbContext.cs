using Microsoft.EntityFrameworkCore;
using BusinessSuite.DAL.Entities;

namespace BusinessSuite.DAL.Data;

public class AppDbContext : DbContext
{
    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<GstRate> GstRates => Set<GstRate>();
    public DbSet<LicenseActivation> LicenseActivations => Set<LicenseActivation>();
    public DbSet<Settings> Settings => Set<Settings>();
    public DbSet<UnitOfMeasure> UnitsOfMeasure => Set<UnitOfMeasure>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        var dbPath = DatabasePathProvider.GetDatabasePath();
        options.UseSqlite($"Data Source={dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Seed standard GST rates
        modelBuilder.Entity<GstRate>().HasData(
            new GstRate { RateID = 1, Percentage = 0, Description = "Exempt/Nil Rated" },
            new GstRate { RateID = 2, Percentage = 5, Description = "Essential Items" },
            new GstRate { RateID = 3, Percentage = 12, Description = "Standard Rate (Lower)" },
            new GstRate { RateID = 4, Percentage = 18, Description = "Standard Rate" },
            new GstRate { RateID = 5, Percentage = 28, Description = "Luxury/Demerit Items" }
        );
        
        modelBuilder.Entity<UnitOfMeasure>().HasData(
            new UnitOfMeasure { UomID = 1, BusinessId = 0, Name = "PCS", Description = "Pieces" },
            new UnitOfMeasure { UomID = 2, BusinessId = 0, Name = "BOX", Description = "Box" },
            new UnitOfMeasure { UomID = 3, BusinessId = 0, Name = "KG", Description = "Kilograms" },
            new UnitOfMeasure { UomID = 4, BusinessId = 0, Name = "MTR", Description = "Meters" },
            new UnitOfMeasure { UomID = 5, BusinessId = 0, Name = "NOS", Description = "Numbers" }
        );
    }
}
