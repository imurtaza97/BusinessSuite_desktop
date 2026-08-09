using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BusinessSuite.DAL.Data;
using BusinessSuite.DAL.Entities;
using BusinessSuite.DAL.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BusinessSuite.BLL.Services;

/// <summary>
/// Handles Financial Year lifecycle: close year and carry-forward opening balances.
/// </summary>
public class FinancialYearService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly FinancialYearRepository _fyRepo;

    public FinancialYearService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
        _fyRepo = new FinancialYearRepository(dbFactory);
    }

    // ── PAN Validation ───────────────────────────────────────────────────────

    private static readonly Regex PanRegex =
        new(@"^[A-Z]{5}[0-9]{4}[A-Z]{1}$", RegexOptions.Compiled);

    /// <summary>
    /// Validates and normalises a PAN number.
    /// Returns (isValid, normalisedPan, errorMessage).
    /// </summary>
    public static (bool IsValid, string Normalised, string Error) ValidatePAN(string? pan)
    {
        if (string.IsNullOrWhiteSpace(pan))
            return (true, string.Empty, string.Empty); // Optional — blank is fine

        var upper = pan.Trim().ToUpperInvariant();

        if (upper.Length != 10)
            return (false, upper, "PAN must be exactly 10 characters (e.g. ABCDE1234F).");

        if (!PanRegex.IsMatch(upper))
            return (false, upper,
                "Invalid PAN format. Expected 5 letters, 4 digits, 1 letter (e.g. ABCDE1234F).");

        return (true, upper, string.Empty);
    }

    // ── Year-Close + Carry-Forward ───────────────────────────────────────────

    /// <summary>
    /// Closes <paramref name="fyId"/> and carries the net cash/bank closing balance
    /// forward as an Opening Balance entry in <paramref name="nextFyId"/>.
    /// </summary>
    public async Task<(bool Success, string Message)> CloseAndCarryForwardAsync(
        int fyId,
        int nextFyId,
        int businessId)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var fy = await db.FinancialYears
                .FirstOrDefaultAsync(f => f.FinancialYearID == fyId && f.BusinessId == businessId);
            if (fy == null)
                return (false, "Financial year not found.");
            if (fy.IsClosed)
                return (false, $"{fy.Label} is already closed.");

            var nextFy = await db.FinancialYears
                .FirstOrDefaultAsync(f => f.FinancialYearID == nextFyId && f.BusinessId == businessId);
            if (nextFy == null)
                return (false, "Target (next) financial year not found.");
            if (nextFy.IsClosed)
                return (false, $"Target year {nextFy.Label} is already closed.");

            // 1. Compute net balance for the closing FY from FinanceLedger
            var entries = await db.FinanceLedgers
                .Where(l =>
                    l.BusinessId == businessId &&
                    l.TransactionDate >= fy.StartDate &&
                    l.TransactionDate <= fy.EndDate)
                .ToListAsync();

            decimal totalCredits = entries
                .Where(e => e.Type == "Credit")
                .Sum(e => e.Amount);
            decimal totalDebits = entries
                .Where(e => e.Type == "Debit")
                .Sum(e => e.Amount);
            decimal closingBalance = totalCredits - totalDebits;

            // 2. Create Opening Balance entry in next FY
            var obEntry = new FinanceLedger
            {
                BusinessId = businessId,
                TransactionDate = nextFy.StartDate,
                Amount = Math.Abs(closingBalance),
                Type = closingBalance >= 0 ? "Credit" : "Debit",
                RelatedEntity = "Opening Balance",
                ReferenceType = "FYCarryForward",
                ReferenceID = fy.FinancialYearID,
                Description = $"Opening balance carried forward from {fy.Label}. " +
                              $"Closing balance: ₹{closingBalance:N2}"
            };
            db.FinanceLedgers.Add(obEntry);

            // 3. Close the old FY
            fy.IsClosed = true;
            fy.IsActive = false;
            fy.ClosedAt = DateTime.UtcNow;
            fy.OpeningBalanceCarriedForward = true;

            // 4. Activate the new FY (deactivate all others first)
            var others = await db.FinancialYears
                .Where(f => f.BusinessId == businessId && f.IsActive)
                .ToListAsync();
            foreach (var f in others) f.IsActive = false;
            nextFy.IsActive = true;

            await db.SaveChangesAsync();

            return (true,
                $"{fy.Label} closed successfully. Opening balance of ₹{closingBalance:N2} " +
                $"carried forward to {nextFy.Label}.");
        }
        catch (Exception ex)
        {
            return (false, $"Error closing financial year: {ex.Message}");
        }
    }

    /// <summary>
    /// Closes <paramref name="fyId"/> WITHOUT creating a next FY carry-forward.
    /// Useful for hard-close when the user will create a new FY later.
    /// </summary>
    public async Task<(bool Success, string Message)> CloseYearOnlyAsync(int fyId, int businessId)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var fy = await db.FinancialYears
                .FirstOrDefaultAsync(f => f.FinancialYearID == fyId && f.BusinessId == businessId);

            if (fy == null) return (false, "Financial year not found.");
            if (fy.IsClosed) return (false, $"{fy.Label} is already closed.");

            fy.IsClosed = true;
            fy.IsActive = false;
            fy.ClosedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return (true, $"{fy.Label} has been closed.");
        }
        catch (Exception ex)
        {
            return (false, $"Error closing financial year: {ex.Message}");
        }
    }
}
