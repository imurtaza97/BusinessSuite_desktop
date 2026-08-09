using System;
using BusinessSuite.DAL.Entities;
using BusinessSuite.BLL.DTOs;
using BusinessSuite.DAL.Data;
using Microsoft.EntityFrameworkCore;

namespace BusinessSuite.BLL.Services;

public class RegisterService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public RegisterService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<bool> IsSetupRequired()
    {
        using var context = await _dbFactory.CreateDbContextAsync();
        return !await context.Businesses.AnyAsync();
    }

    public async Task<string> RegisterBusiness(RegistrationRequest request)
    {
        using var context = await _dbFactory.CreateDbContextAsync();

        // 1. Enforce Single Business Restriction
        if (await context.Businesses.AnyAsync())
            return "Registration failed: A business profile already exists.";

        // 2. Validation
        if (string.IsNullOrWhiteSpace(request.BusinessName)) return "Business Name is required.";
        if (string.IsNullOrWhiteSpace(request.OwnerName)) return "Owner Name is required.";
        if (string.IsNullOrWhiteSpace(request.Email)) return "Email is required.";
        if (string.IsNullOrWhiteSpace(request.Address)) return "Address is required.";
        if (string.IsNullOrWhiteSpace(request.State)) return "State is required.";
        
        if (request.IsGSTRegistered)
        {
            if (string.IsNullOrWhiteSpace(request.GSTIN)) return "GSTIN is required for GST registered businesses.";
            if (request.GSTIN.Length != 15) return "GSTIN must be exactly 15 characters long.";
            if (request.GstType == null) return "GST Type is required for GST registered businesses.";
        }

        if (string.IsNullOrWhiteSpace(request.UserName)) return "Admin Username is required.";
        if (string.IsNullOrWhiteSpace(request.Password)) return "Admin Password is required.";
        if (request.Password.Length < 6) return "Password must be at least 6 characters long.";

        using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // 4. Create Business Entity
            var business = new Business
            {
                BusinessName = request.BusinessName,
                OwnerName = request.OwnerName,
                Email = request.Email,
                GSTIN = request.GSTIN,
                PAN = request.PAN,
                Address = request.Address,
                ContactNo = request.ContactNo,
                State = request.State,
                GstType = request.GstType,
                IsGSTRegistered = request.IsGSTRegistered,
                PasswordHash = hashedPassword
            };
            context.Businesses.Add(business);
            await context.SaveChangesAsync();

            // 4a. Create Default Warehouse
            var warehouse = new Warehouse
            {
                BusinessId = business.BusinessID,
                WarehouseName = "Main Warehouse",
                Address = business.Address,
                State = business.State,
                IsMainWarehouse = true
            };
            context.Warehouses.Add(warehouse);

            // 5. Create Admin User Entity
            var adminUser = new User
            {
                UserName = request.UserName,
                FullName = business.OwnerName,
                Email = business.Email,
                PasswordHash = hashedPassword,
                Designation = Designation.Owner,
                ContactNo = business.ContactNo
            };
            context.Users.Add(adminUser);

            // 6. Create Default Settings
            var settings = new Settings
            {
                BusinessID = business.BusinessID,
                Currency = request.DefaultCurrency,
                TimeZone = request.TimeZone,
                DateFormat = request.DateFormat
            };
            context.Settings.Add(settings);

            await context.SaveChangesAsync();

            // 7. Write the very first audit log entry — business registration
            var registrationAudit = new AuditLog
            {
                BusinessID = business.BusinessID,
                DocumentType = "Business",
                DocumentID = business.BusinessID,
                Action = "Created",
                FieldName = "All",
                OldValue = null,
                NewValue = $"{business.BusinessName} registered",
                ChangedByUserID = adminUser.UserID,
                ChangedAt = DateTime.Now,
                Reason = "Initial business registration"
            };
            context.AuditLogs.Add(registrationAudit);

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return "Success";
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return $"Error: {ex.Message}";
        }
    }
}