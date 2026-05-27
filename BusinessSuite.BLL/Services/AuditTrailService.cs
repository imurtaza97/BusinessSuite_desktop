using System;
using System.Threading.Tasks;
using BusinessSuite.DAL.Data;
using BusinessSuite.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessSuite.BLL.Services;

public class AuditTrailService
{
    private readonly AppDbContext _context;

    public AuditTrailService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Logs a change to an audit trail for compliance tracking
    /// </summary>
    /// <param name="businessId">Business that owns the document</param>
    /// <param name="documentType">Type of document: Invoice, PurchaseOrder, CreditNote, DebitNote</param>
    /// <param name="documentId">ID of the document</param>
    /// <param name="action">Action performed: Created, Modified, Finalized, Cancelled, Unposted, Deleted</param>
    /// <param name="fieldName">Field that was changed (or "All" for bulk operations)</param>
    /// <param name="oldValue">Previous value (max 500 chars, null if creation)</param>
    /// <param name="newValue">New value (max 500 chars)</param>
    /// <param name="changedByUserId">User who made the change</param>
    /// <param name="reason">Why the change was made (amendments, corrections, etc)</param>
    /// <param name="ipAddress">Optional IP address for security audit</param>
    /// <returns>True if logged successfully</returns>
    public async Task<bool> LogChangeAsync(
        int businessId,
        string documentType,
        int documentId,
        string action,
        string fieldName,
        string? oldValue,
        string? newValue,
        int changedByUserId,
        string? reason = null,
        string? ipAddress = null)
    {
        try
        {
            // Truncate values if too long (500 char limit)
            oldValue = TruncateValue(oldValue);
            newValue = TruncateValue(newValue);

            var auditLog = new AuditLog
            {
                BusinessID = businessId,
                DocumentType = documentType,
                DocumentID = documentId,
                Action = action,
                FieldName = fieldName,
                OldValue = oldValue,
                NewValue = newValue,
                ChangedByUserID = changedByUserId,
                ChangedAt = DateTime.Now,
                Reason = reason,
                IPAddress = ipAddress
            };

            await _context.AuditLogs.AddAsync(auditLog);
            return await _context.SaveChangesAsync() > 0;
        }
        catch (Exception ex)
        {
            // Log service failure but don't crash the main operation
            System.Diagnostics.Debug.WriteLine($"Audit logging failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Logs document creation
    /// </summary>
    public async Task<bool> LogCreatedAsync(
        int businessId,
        string documentType,
        int documentId,
        int createdByUserId,
        string? notes = null)
    {
        return await LogChangeAsync(
            businessId,
            documentType,
            documentId,
            "Created",
            "All",
            null,
            "New document created",
            createdByUserId,
            notes);
    }

    /// <summary>
    /// Logs document finalization
    /// </summary>
    public async Task<bool> LogFinalizedAsync(
        int businessId,
        string documentType,
        int documentId,
        int finalizedByUserId)
    {
        return await LogChangeAsync(
            businessId,
            documentType,
            documentId,
            "Finalized",
            "IsDraft",
            "true",
            "false",
            finalizedByUserId,
            "Document posted/finalized");
    }

    /// <summary>
    /// Logs document cancellation
    /// </summary>
    public async Task<bool> LogCancelledAsync(
        int businessId,
        string documentType,
        int documentId,
        int cancelledByUserId,
        string reason)
    {
        return await LogChangeAsync(
            businessId,
            documentType,
            documentId,
            "Cancelled",
            "Status",
            "Active",
            "Cancelled",
            cancelledByUserId,
            reason);
    }

    /// <summary>
    /// Logs document unposting (admin reverting a finalized document)
    /// </summary>
    public async Task<bool> LogUnpostedAsync(
        int businessId,
        string documentType,
        int documentId,
        int unpostedByUserId,
        string reason)
    {
        return await LogChangeAsync(
            businessId,
            documentType,
            documentId,
            "Unposted",
            "IsDraft",
            "false",
            "true",
            unpostedByUserId,
            reason);
    }

    /// <summary>
    /// Logs document deletion (soft delete)
    /// </summary>
    public async Task<bool> LogDeletedAsync(
        int businessId,
        string documentType,
        int documentId,
        int deletedByUserId,
        string reason)
    {
        return await LogChangeAsync(
            businessId,
            documentType,
            documentId,
            "Deleted",
            "IsDeleted",
            "false",
            "true",
            deletedByUserId,
            reason);
    }

    /// <summary>
    /// Logs a field modification
    /// </summary>
    public async Task<bool> LogFieldModifiedAsync(
        int businessId,
        string documentType,
        int documentId,
        string fieldName,
        string? oldValue,
        string? newValue,
        int modifiedByUserId,
        string? reason = null)
    {
        return await LogChangeAsync(
            businessId,
            documentType,
            documentId,
            "Modified",
            fieldName,
            oldValue,
            newValue,
            modifiedByUserId,
            reason);
    }

    /// <summary>
    /// Retrieves audit logs for a specific document
    /// </summary>
    public async Task<System.Collections.Generic.List<AuditLog>> GetAuditTrailAsync(
        int businessId,
        string documentType,
        int documentId)
    {
        return await _context.AuditLogs
            .Where(a => a.BusinessID == businessId && a.DocumentType == documentType && a.DocumentID == documentId)
            .OrderByDescending(a => a.ChangedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Retrieves audit logs for a date range
    /// </summary>
    public async Task<System.Collections.Generic.List<AuditLog>> GetAuditTrailByDateRangeAsync(
        int businessId,
        DateTime startDate,
        DateTime endDate)
    {
        return await _context.AuditLogs
            .Where(a => a.BusinessID == businessId && a.ChangedAt >= startDate && a.ChangedAt <= endDate)
            .OrderByDescending(a => a.ChangedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Retrieves audit logs by user (who made changes)
    /// </summary>
    public async Task<System.Collections.Generic.List<AuditLog>> GetAuditTrailByUserAsync(
        int businessId,
        int userId)
    {
        return await _context.AuditLogs
            .Where(a => a.BusinessID == businessId && a.ChangedByUserID == userId)
            .OrderByDescending(a => a.ChangedAt)
            .ToListAsync();
    }

    /* ============================
       HELPER METHODS
    ============================ */

    private static string? TruncateValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        const int maxLength = 500;
        return value.Length > maxLength ? value.Substring(0, maxLength) : value;
    }
}
