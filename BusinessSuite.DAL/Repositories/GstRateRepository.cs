using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessSuite.DAL.Data;
using BusinessSuite.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessSuite.DAL.Repositories;

public class GstRateRepository
{
    private readonly AppDbContext _context;

    public GstRateRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<decimal>> GetAllPercentagesAsync()
    {
        var rates = await _context.GstRates.ToListAsync();
        return rates
            .AsEnumerable()
            .OrderBy(r => r.Percentage)
            .Select(r => r.Percentage)
            .ToList();
    }
}
