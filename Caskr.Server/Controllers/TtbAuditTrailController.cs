using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Caskr.server.Models;
using Caskr.server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Caskr.server.Controllers;

/// <summary>
/// Provides access to TTB audit trail for compliance reporting and inspections.
/// The audit trail is immutable once created and provides a complete record of
/// all changes to TTB compliance data.
/// </summary>
public sealed class TtbAuditTrailController(
    ITtbAuditLogger auditLogger,
    CaskrDbContext dbContext,
    IUsersService usersService,
    ILogger<TtbAuditTrailController> logger) : AuthorizedApiControllerBase
{
    /// <summary>
    /// Gets audit logs for a company within a date range.
    /// </summary>
    [HttpGet("/api/ttb/audit-trail")]
    [ProducesResponseType(typeof(IEnumerable<TtbAuditLogResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        [FromQuery, Required] int companyId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? entityType = null,
        [FromQuery] TtbAuditAction? action = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (companyId <= 0)
        {
            return BadRequest(CreateProblem("CompanyId must be provided."));
        }

        if (pageSize > 500)
        {
            pageSize = 500;
        }

        if (page < 1)
        {
            page = 1;
        }

        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var isAdmin = (UserType)user.UserTypeId is UserType.Admin or UserType.SuperAdmin;
        if (!isAdmin && user.CompanyId != companyId)
        {
            return Forbid();
        }

        var query = dbContext.TtbAuditLogs
            .Include(a => a.ChangedByUser)
            .Where(a => a.CompanyId == companyId);

        if (startDate.HasValue)
        {
            query = query.Where(a => a.ChangeTimestamp >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(a => a.ChangeTimestamp <= endDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            query = query.Where(a => a.EntityType == entityType);
        }

        if (action.HasValue)
        {
            query = query.Where(a => a.Action == action.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var logs = await query
            .OrderByDescending(a => a.ChangeTimestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new TtbAuditLogResponse
            {
                Id = a.Id,
                CompanyId = a.CompanyId,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                Action = a.Action,
                ChangedByUserId = a.ChangedByUserId,
                ChangedByUserName = a.ChangedByUser != null ? a.ChangedByUser.Name : null,
                ChangeTimestamp = a.ChangeTimestamp,
                OldValues = a.OldValues,
                NewValues = a.NewValues,
                IpAddress = a.IpAddress,
                ChangeDescription = a.ChangeDescription
            })
            .ToListAsync(cancellationToken);

        logger.LogInformation(
            "User {UserId} retrieved {Count} audit logs for company {CompanyId}",
            user.Id, logs.Count, companyId);

        return Ok(new TtbAuditLogListResponse
        {
            Items = logs,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    /// <summary>
    /// Exports audit logs to CSV format for TTB inspections.
    /// </summary>
    [HttpGet("/api/ttb/audit-trail/export")]
    [Produces("text/csv")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportCsv(
        [FromQuery, Required] int companyId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        if (companyId <= 0)
        {
            return BadRequest(CreateProblem("CompanyId must be provided."));
        }

        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var isAdmin = (UserType)user.UserTypeId is UserType.Admin or UserType.SuperAdmin;
        if (!isAdmin && user.CompanyId != companyId)
        {
            return Forbid();
        }

        var csvContent = await auditLogger.ExportAuditLogsToCsvAsync(companyId, startDate, endDate);

        var fileName = $"TTB_Audit_Trail_{companyId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        var bytes = Encoding.UTF8.GetBytes(csvContent);

        logger.LogInformation(
            "User {UserId} exported audit trail CSV for company {CompanyId}",
            user.Id, companyId);

        return File(bytes, "text/csv", fileName);
    }

    /// <summary>
    /// Gets a single audit log entry by ID.
    /// </summary>
    [HttpGet("/api/ttb/audit-trail/{id:int}")]
    [ProducesResponseType(typeof(TtbAuditLogResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var auditLog = await dbContext.TtbAuditLogs
            .Include(a => a.ChangedByUser)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (auditLog is null)
        {
            return NotFound(CreateProblem("Audit log entry not found."));
        }

        var isAdmin = (UserType)user.UserTypeId is UserType.Admin or UserType.SuperAdmin;
        if (!isAdmin && user.CompanyId != auditLog.CompanyId)
        {
            return Forbid();
        }

        var response = new TtbAuditLogResponse
        {
            Id = auditLog.Id,
            CompanyId = auditLog.CompanyId,
            EntityType = auditLog.EntityType,
            EntityId = auditLog.EntityId,
            Action = auditLog.Action,
            ChangedByUserId = auditLog.ChangedByUserId,
            ChangedByUserName = auditLog.ChangedByUser?.Name,
            ChangeTimestamp = auditLog.ChangeTimestamp,
            OldValues = auditLog.OldValues,
            NewValues = auditLog.NewValues,
            IpAddress = auditLog.IpAddress,
            UserAgent = auditLog.UserAgent,
            ChangeDescription = auditLog.ChangeDescription
        };

        return Ok(response);
    }

    /// <summary>
    /// Gets recent audit activity summary for dashboard display.
    /// </summary>
    [HttpGet("/api/ttb/audit-trail/recent")]
    [ProducesResponseType(typeof(IEnumerable<TtbAuditLogResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRecent(
        [FromQuery, Required] int companyId,
        [FromQuery] int count = 10,
        CancellationToken cancellationToken = default)
    {
        if (companyId <= 0)
        {
            return BadRequest(CreateProblem("CompanyId must be provided."));
        }

        if (count > 100)
        {
            count = 100;
        }

        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var isAdmin = (UserType)user.UserTypeId is UserType.Admin or UserType.SuperAdmin;
        if (!isAdmin && user.CompanyId != companyId)
        {
            return Forbid();
        }

        var logs = await dbContext.TtbAuditLogs
            .Include(a => a.ChangedByUser)
            .Where(a => a.CompanyId == companyId)
            .OrderByDescending(a => a.ChangeTimestamp)
            .Take(count)
            .Select(a => new TtbAuditLogResponse
            {
                Id = a.Id,
                CompanyId = a.CompanyId,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                Action = a.Action,
                ChangedByUserId = a.ChangedByUserId,
                ChangedByUserName = a.ChangedByUser != null ? a.ChangedByUser.Name : null,
                ChangeTimestamp = a.ChangeTimestamp,
                ChangeDescription = a.ChangeDescription
            })
            .ToListAsync(cancellationToken);

        return Ok(logs);
    }

    private async Task<User?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId))
        {
            return null;
        }

        return await usersService.GetUserByIdAsync(userId);
    }

    private static ProblemDetails CreateProblem(string detail) => new()
    {
        Detail = detail,
        Title = "TTB Audit Trail Request Failed"
    };
}

public record TtbAuditLogResponse
{
    public int Id { get; init; }
    public int CompanyId { get; init; }
    public string EntityType { get; init; } = string.Empty;
    public int EntityId { get; init; }
    public TtbAuditAction Action { get; init; }
    public int ChangedByUserId { get; init; }
    public string? ChangedByUserName { get; init; }
    public DateTime ChangeTimestamp { get; init; }
    public string? OldValues { get; init; }
    public string? NewValues { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public string? ChangeDescription { get; init; }
}

public record TtbAuditLogListResponse
{
    public IEnumerable<TtbAuditLogResponse> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}
