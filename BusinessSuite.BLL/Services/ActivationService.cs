using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BusinessSuite.DAL.Data;
using BusinessSuite.DAL.Entities;

namespace BusinessSuite.BLL.Services;

public class ActivationService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public ActivationService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    /// <summary>
    /// Activates the application using a license key.
    /// In a real system, this would call an online API.
    /// </summary>
    public async Task<string> ActivateLicenseAsync(string licenseKey)
    {
        if (string.IsNullOrWhiteSpace(licenseKey))
            return "License key is required.";

        try
        {
            // 1. Get Hardware ID
            string hardwareId = HardwareService.GetHardwareId();

            // 2. Simulated Online Verification
            // In a real system, you would do: 
            // var response = await _httpClient.PostAsync("https://api.businesssuite.com/activate", ...);
            bool isKeyValid = await SimulateOnlineVerification(licenseKey, hardwareId);

            if (!isKeyValid)
                return "Invalid license key or key already used on another machine.";

            // 3. Store Activation Locally
            using var db = _dbFactory.CreateDbContext();
            
            // Generate a secure hash of Key + HardwareID for local verification
            var combinedString = licenseKey.Trim() + hardwareId;
            var secureHash = BCrypt.Net.BCrypt.HashPassword(combinedString);

            var activation = new LicenseActivation
            {
                HardwareIdHash = secureHash,
                LicenseKeyHash = secureHash, // In a more complex scenario, these could be different
                ActivatedOn = DateTime.Now,
                IsValid = true
            };

            db.LicenseActivations.Add(activation);

            await db.SaveChangesAsync();
            return "Success";
        }
        catch (Exception ex)
        {
            return $"Activation Error: {ex.Message}";
        }
    }

    private async Task<bool> SimulateOnlineVerification(string key, string hwId)
    {
        // For demonstration, any key starting with "BS-VALID-" is accepted
        await Task.Delay(1500); // Simulate network latency
        return key.StartsWith("BS-VALID-", StringComparison.OrdinalIgnoreCase);
    }
}
