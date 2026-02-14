using Microsoft.EntityFrameworkCore;

namespace BusinessSuite.DAL.Data;

public class AppDbContextFactory : IDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext()
    {
        return new AppDbContext();
    }
}
