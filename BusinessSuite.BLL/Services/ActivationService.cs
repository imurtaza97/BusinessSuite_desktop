using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BusinessSuite.DAL.Data;
using BusinessSuite.DAL.Entities;

namespace BusinessSuite.BLL.Services;

public class ActivationService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private static readonly HttpClient _httpClient = new HttpClient();

    // Secure API URL from environment variables, defaulting to localhost for dev
    private string ApiUrl => Environment.GetEnvironmentVariable("BUSINESS_SUITE_API_URL") 
                            ?? "http://localhost:3000/api/business-suite/verify-license";

    public ActivationService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    /// <summary>
    /// Activates the application using a license key and email.
    /// Verifies against a secure Next.js API endpoint.
    /// </summary>
    public async Task<string> ActivateLicenseAsync(string licenseKey, string email)
    {
        if (string.IsNullOrWhiteSpace(licenseKey))
            return "License key is required.";
        
        if (string.IsNullOrWhiteSpace(email))
            return "Email is required.";

        try
        {
            // 1. Get Hardware ID
            string hardwareId = HardwareService.GetHardwareId();

            // 2. Remote Verification via Next.js API
            var verificationResponse = await VerifyKeyViaApiAsync(licenseKey, email, hardwareId);

            if (!verificationResponse.Success)
                return verificationResponse.Message ?? "Verification failed.";

            // 3. Store Activation Locally
            using var db = _dbFactory.CreateDbContext();
            
            // Generate a secure hash of Key + HardwareID for local verification
            var combinedString = licenseKey.Trim() + hardwareId;
            var secureHash = BCrypt.Net.BCrypt.HashPassword(combinedString);

            var activation = new LicenseActivation
            {
                HardwareIdHash = secureHash,
                LicenseKeyHash = secureHash, 
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

    private async Task<ActivationResponse> VerifyKeyViaApiAsync(string key, string email, string hwId)
    {
        try 
        {
            var requestBody = new 
            { 
                email = email, 
                licenseKey = key, 
                hardwareId = hwId 
            };

            var response = await _httpClient.PostAsJsonAsync(ApiUrl, requestBody);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ActivationResponse>() 
                       ?? new ActivationResponse { Success = false, Message = "Empty response from server" };
            }
            
            var errorResponse = await response.Content.ReadFromJsonAsync<ActivationResponse>();
            return errorResponse ?? new ActivationResponse { Success = false, Message = $"Server returned {response.StatusCode}" };
        }
        catch (Exception ex)
        {
            return new ActivationResponse { Success = false, Message = $"Network Error: {ex.Message}" };
        }
    }
}

public class ActivationResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public UserInfo? User { get; set; }
}

public class UserInfo
{
    public string? FullName { get; set; }
    public string? BusinessName { get; set; }
    public bool IsPaid { get; set; }
}
