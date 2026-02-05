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
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<GstRate> GstRates => Set<GstRate>();
    public DbSet<LicenseActivation> LicenseActivations => Set<LicenseActivation>();
    public DbSet<Settings> Settings => Set<Settings>();
    public DbSet<UnitOfMeasure> UnitsOfMeasure => Set<UnitOfMeasure>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();

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
            new UnitOfMeasure { UnitID = 1, BusinessId = 0, Name = "PCS", Description = "Pieces" },
            new UnitOfMeasure { UnitID = 2, BusinessId = 0, Name = "BOX", Description = "Box" },
            new UnitOfMeasure { UnitID = 3, BusinessId = 0, Name = "KG", Description = "Kilograms" },
            new UnitOfMeasure { UnitID = 4, BusinessId = 0, Name = "MTR", Description = "Meters" },
            new UnitOfMeasure { UnitID = 5, BusinessId = 0, Name = "NOS", Description = "Numbers" },
            new UnitOfMeasure { UnitID = 6, BusinessId = 0, Name = "LTR", Description = "Litres" },
            new UnitOfMeasure { UnitID = 7, BusinessId = 0, Name = "GMS", Description = "Grams" },
            new UnitOfMeasure { UnitID = 8, BusinessId = 0, Name = "ML", Description = "Millilitres" },
            new UnitOfMeasure { UnitID = 9, BusinessId = 0, Name = "DOZ", Description = "Dozen" },
            new UnitOfMeasure { UnitID = 10, BusinessId = 0, Name = "PAIR", Description = "Pair" },
            new UnitOfMeasure { UnitID = 11, BusinessId = 0, Name = "SET", Description = "Set" },
            new UnitOfMeasure { UnitID = 12, BusinessId = 0, Name = "PKT", Description = "Packet" },
            new UnitOfMeasure { UnitID = 13, BusinessId = 0, Name = "TIN", Description = "Tin" },
            new UnitOfMeasure { UnitID = 14, BusinessId = 0, Name = "BAG", Description = "Bag" },
            new UnitOfMeasure { UnitID = 15, BusinessId = 0, Name = "BTL", Description = "Bottle" },
            new UnitOfMeasure { UnitID = 16, BusinessId = 0, Name = "JAR", Description = "Jar" },
            new UnitOfMeasure { UnitID = 17, BusinessId = 0, Name = "CAN", Description = "Can" },
            new UnitOfMeasure { UnitID = 18, BusinessId = 0, Name = "TUBE", Description = "Tube" },
            new UnitOfMeasure { UnitID = 19, BusinessId = 0, Name = "ROLL", Description = "Roll" },
            new UnitOfMeasure { UnitID = 20, BusinessId = 0, Name = "SHEET", Description = "Sheet" },
            new UnitOfMeasure { UnitID = 21, BusinessId = 0, Name = "SQFT", Description = "Square Feet" },
            new UnitOfMeasure { UnitID = 22, BusinessId = 0, Name = "SQM", Description = "Square Meter" },
            new UnitOfMeasure { UnitID = 23, BusinessId = 0, Name = "CFT", Description = "Cubic Feet" },
            new UnitOfMeasure { UnitID = 24, BusinessId = 0, Name = "CUM", Description = "Cubic Meter" }
        );
    }
}
