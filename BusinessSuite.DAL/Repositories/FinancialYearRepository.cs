using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessSuite.DAL.Data;
using BusinessSuite.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessSuite.DAL.Repositories;

public class FinancialYearRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public FinancialYearRepository(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    // ── Queries ─────────────────────────────────────────────────────────────

    public async Task<List<FinancialYear>> GetAllAsync(int businessId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.FinancialYears
            .Where(fy => fy.BusinessId == businessId)
            .OrderByDescending(fy => fy.StartDate)
            .ToListAsync();
    }

    public async Task<FinancialYear?> GetActiveAsync(int businessId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.FinancialYears
            .FirstOrDefaultAsync(fy => fy.BusinessId == businessId && fy.IsActive);
    }

    public async Task<FinancialYear?> GetByIdAsync(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.FinancialYears.FindAsync(id);
    }

    // ── Mutations ────────────────────────────────────────────────────────────

    /// <summary>Creates a new financial year. Does NOT auto-activate it.</summary>
    public async Task<FinancialYear> CreateAsync(int businessId, DateTime startDate, DateTime endDate)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        // Validate no overlap with existing FYs
        bool overlap = await db.FinancialYears.AnyAsync(fy =>
            fy.BusinessId == businessId &&
            fy.StartDate < endDate &&
            fy.EndDate > startDate);

        if (overlap)
            throw new InvalidOperationException(
                "The date range overlaps with an existing financial year for this business.");

        // Auto-build label if none
        string label = $"FY {startDate:yyyy}-{endDate:yy}";

        // Check if this is the first FY — if so, auto-activate
        bool isFirst = !await db.FinancialYears.AnyAsync(fy => fy.BusinessId == businessId);

        var fy = new FinancialYear
        {
            BusinessId = businessId,
            Label = label,
            StartDate = startDate,
            EndDate = endDate,
            IsActive = isFirst,
            IsClosed = false
        };

        db.FinancialYears.Add(fy);
        await db.SaveChangesAsync();
        return fy;
    }

    /// <summary>
    /// Sets the given FY as the active one for this business.
    /// Deactivates all others. Cannot activate a closed FY.
    /// </summary>
    public async Task SetActiveAsync(int fyId, int businessId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var target = await db.FinancialYears
            .FirstOrDefaultAsync(fy => fy.FinancialYearID == fyId && fy.BusinessId == businessId);

        if (target == null) throw new InvalidOperationException("Financial year not found.");
        if (target.IsClosed) throw new InvalidOperationException("Cannot activate a closed financial year.");

        // Deactivate all
        var all = await db.FinancialYears
            .Where(fy => fy.BusinessId == businessId && fy.IsActive)
            .ToListAsync();
        foreach (var f in all) f.IsActive = false;

        target.IsActive = true;
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Closes the given FY: marks IsClosed=true, IsActive=false, records ClosedAt.
    /// Does not carry forward balances — that is handled by FinancialYearService.
    /// </summary>
    public async Task<FinancialYear> CloseYearAsync(int fyId, int businessId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var fy = await db.FinancialYears
            .FirstOrDefaultAsync(f => f.FinancialYearID == fyId && f.BusinessId == businessId);

        if (fy == null) throw new InvalidOperationException("Financial year not found.");
        if (fy.IsClosed) throw new InvalidOperationException($"{fy.Label} is already closed.");

        fy.IsClosed = true;
        fy.IsActive = false;
        fy.ClosedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return fy;
    }

    /// <summary>Marks that the opening balance carry-forward has been completed.</summary>
    public async Task MarkCarryForwardDoneAsync(int fyId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var fy = await db.FinancialYears.FindAsync(fyId);
        if (fy != null)
        {
            fy.OpeningBalanceCarriedForward = true;
            await db.SaveChangesAsync();
        }
    }

    /// <summary>Deletes a FY — only allowed if it has never been activated or is a new draft.</summary>
    public async Task<bool> DeleteAsync(int fyId, int businessId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var fy = await db.FinancialYears
            .FirstOrDefaultAsync(f => f.FinancialYearID == fyId && f.BusinessId == businessId);

        if (fy == null) return false;
        if (fy.IsClosed || fy.IsActive) return false; // Cannot delete active or closed

        db.FinancialYears.Remove(fy);
        await db.SaveChangesAsync();
        return true;
    }
}
