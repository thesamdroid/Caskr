using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using Caskr.server.Models;
using Caskr.server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Caskr.server.Controllers;

/// <summary>
/// Manages TTB transactions for manual entry, editing, and deletion.
/// All changes are logged to the TTB audit trail for compliance purposes.
/// </summary>
public sealed class TtbTransactionsController(
    CaskrDbContext dbContext,
    IUsersService usersService,
    ITtbAuditLogger auditLogger,
    ILogger<TtbTransactionsController> logger) : AuthorizedApiControllerBase
{
    [HttpGet("/api/ttb/transactions")]
    [ProducesResponseType(typeof(IEnumerable<TtbTransactionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        [FromQuery, Required] int companyId,
        [FromQuery, Required] int month,
        [FromQuery, Required] int year,
        CancellationToken cancellationToken = default)
    {
        if (companyId <= 0)
        {
            return BadRequest(CreateProblem("CompanyId must be provided."));
        }

        if (month < 1 || month > 12)
        {
            return BadRequest(CreateProblem("Month must be between 1 and 12."));
        }

        if (year < 2020)
        {
            return BadRequest(CreateProblem("Year must be 2020 or later."));
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

        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var transactions = await dbContext.TtbTransactions
            .Where(t => t.CompanyId == companyId
                && t.TransactionDate >= startDate
                && t.TransactionDate <= endDate)
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.Id)
            .Select(t => new TtbTransactionResponse
            {
                Id = t.Id,
                CompanyId = t.CompanyId,
                TransactionDate = t.TransactionDate,
                TransactionType = t.TransactionType,
                ProductType = t.ProductType,
                SpiritsType = t.SpiritsType,
                ProofGallons = t.ProofGallons,
                WineGallons = t.WineGallons,
                SourceEntityType = t.SourceEntityType,
                SourceEntityId = t.SourceEntityId,
                Notes = t.Notes
            })
            .ToListAsync(cancellationToken);

        return Ok(transactions);
    }

    [HttpPost("/api/ttb/transactions")]
    [ProducesResponseType(typeof(TtbTransactionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromBody] CreateTtbTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.CompanyId <= 0)
        {
            return BadRequest(CreateProblem("CompanyId must be provided."));
        }

        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var isAdmin = (UserType)user.UserTypeId is UserType.Admin or UserType.SuperAdmin;
        if (!isAdmin && user.CompanyId != request.CompanyId)
        {
            return Forbid();
        }

        // Validate proof gallons and wine gallons
        if (request.ProofGallons < 0)
        {
            return BadRequest(CreateProblem("ProofGallons cannot be negative."));
        }

        if (request.WineGallons < 0)
        {
            return BadRequest(CreateProblem("WineGallons cannot be negative."));
        }

        // Check if the month is locked (report submitted or approved)
        var transactionMonth = request.TransactionDate.Month;
        var transactionYear = request.TransactionDate.Year;
        if (await auditLogger.IsMonthLockedAsync(request.CompanyId, transactionMonth, transactionYear))
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                CreateProblem("Cannot modify data for submitted reports. Contact administrator."));
        }

        var transaction = new TtbTransaction
        {
            CompanyId = request.CompanyId,
            TransactionDate = request.TransactionDate,
            TransactionType = request.TransactionType,
            ProductType = request.ProductType,
            SpiritsType = request.SpiritsType,
            ProofGallons = request.ProofGallons,
            WineGallons = request.WineGallons,
            SourceEntityType = "Manual",
            SourceEntityId = null,
            Notes = request.Notes
        };

        dbContext.TtbTransactions.Add(transaction);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Log the audit trail
        await auditLogger.LogChangeAsync(TtbAuditAction.Create, transaction, null, user.Id, request.CompanyId);

        logger.LogInformation(
            "User {UserId} created manual TTB transaction {TransactionId} for company {CompanyId}",
            user.Id, transaction.Id, request.CompanyId);

        var response = new TtbTransactionResponse
        {
            Id = transaction.Id,
            CompanyId = transaction.CompanyId,
            TransactionDate = transaction.TransactionDate,
            TransactionType = transaction.TransactionType,
            ProductType = transaction.ProductType,
            SpiritsType = transaction.SpiritsType,
            ProofGallons = transaction.ProofGallons,
            WineGallons = transaction.WineGallons,
            SourceEntityType = transaction.SourceEntityType,
            SourceEntityId = transaction.SourceEntityId,
            Notes = transaction.Notes
        };

        return CreatedAtAction(nameof(Get), new { id = transaction.Id }, response);
    }

    [HttpGet("/api/ttb/transactions/{id:int}")]
    [ProducesResponseType(typeof(TtbTransactionResponse), StatusCodes.Status200OK)]
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

        var transaction = await dbContext.TtbTransactions
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (transaction is null)
        {
            return NotFound(CreateProblem("Transaction not found."));
        }

        var isAdmin = (UserType)user.UserTypeId is UserType.Admin or UserType.SuperAdmin;
        if (!isAdmin && user.CompanyId != transaction.CompanyId)
        {
            return Forbid();
        }

        var response = new TtbTransactionResponse
        {
            Id = transaction.Id,
            CompanyId = transaction.CompanyId,
            TransactionDate = transaction.TransactionDate,
            TransactionType = transaction.TransactionType,
            ProductType = transaction.ProductType,
            SpiritsType = transaction.SpiritsType,
            ProofGallons = transaction.ProofGallons,
            WineGallons = transaction.WineGallons,
            SourceEntityType = transaction.SourceEntityType,
            SourceEntityId = transaction.SourceEntityId,
            Notes = transaction.Notes
        };

        return Ok(response);
    }

    [HttpPut("/api/ttb/transactions/{id:int}")]
    [ProducesResponseType(typeof(TtbTransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateTtbTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var transaction = await dbContext.TtbTransactions
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (transaction is null)
        {
            return NotFound(CreateProblem("Transaction not found."));
        }

        var isAdmin = (UserType)user.UserTypeId is UserType.Admin or UserType.SuperAdmin;
        if (!isAdmin && user.CompanyId != transaction.CompanyId)
        {
            return Forbid();
        }

        // Only allow editing manual transactions
        if (transaction.SourceEntityType != "Manual")
        {
            return BadRequest(CreateProblem("Only manual transactions can be edited."));
        }

        // Check if the current month or new month is locked
        var currentMonth = transaction.TransactionDate.Month;
        var currentYear = transaction.TransactionDate.Year;
        var newMonth = request.TransactionDate.Month;
        var newYear = request.TransactionDate.Year;

        if (await auditLogger.IsMonthLockedAsync(transaction.CompanyId, currentMonth, currentYear))
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                CreateProblem("Cannot modify data for submitted reports. Contact administrator."));
        }

        if ((newMonth != currentMonth || newYear != currentYear) &&
            await auditLogger.IsMonthLockedAsync(transaction.CompanyId, newMonth, newYear))
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                CreateProblem("Cannot move transaction to a month with submitted reports. Contact administrator."));
        }

        // Validate proof gallons and wine gallons
        if (request.ProofGallons < 0)
        {
            return BadRequest(CreateProblem("ProofGallons cannot be negative."));
        }

        if (request.WineGallons < 0)
        {
            return BadRequest(CreateProblem("WineGallons cannot be negative."));
        }

        // Capture old values for audit
        var oldTransaction = new TtbTransaction
        {
            Id = transaction.Id,
            CompanyId = transaction.CompanyId,
            TransactionDate = transaction.TransactionDate,
            TransactionType = transaction.TransactionType,
            ProductType = transaction.ProductType,
            SpiritsType = transaction.SpiritsType,
            ProofGallons = transaction.ProofGallons,
            WineGallons = transaction.WineGallons,
            SourceEntityType = transaction.SourceEntityType,
            SourceEntityId = transaction.SourceEntityId,
            Notes = transaction.Notes
        };

        transaction.TransactionDate = request.TransactionDate;
        transaction.TransactionType = request.TransactionType;
        transaction.ProductType = request.ProductType;
        transaction.SpiritsType = request.SpiritsType;
        transaction.ProofGallons = request.ProofGallons;
        transaction.WineGallons = request.WineGallons;
        transaction.Notes = request.Notes;

        await dbContext.SaveChangesAsync(cancellationToken);

        // Log the audit trail
        await auditLogger.LogChangeAsync(TtbAuditAction.Update, transaction, oldTransaction, user.Id, transaction.CompanyId);

        logger.LogInformation(
            "User {UserId} updated manual TTB transaction {TransactionId}",
            user.Id, transaction.Id);

        var response = new TtbTransactionResponse
        {
            Id = transaction.Id,
            CompanyId = transaction.CompanyId,
            TransactionDate = transaction.TransactionDate,
            TransactionType = transaction.TransactionType,
            ProductType = transaction.ProductType,
            SpiritsType = transaction.SpiritsType,
            ProofGallons = transaction.ProofGallons,
            WineGallons = transaction.WineGallons,
            SourceEntityType = transaction.SourceEntityType,
            SourceEntityId = transaction.SourceEntityId,
            Notes = transaction.Notes
        };

        return Ok(response);
    }

    [HttpDelete("/api/ttb/transactions/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var transaction = await dbContext.TtbTransactions
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (transaction is null)
        {
            return NotFound(CreateProblem("Transaction not found."));
        }

        var isAdmin = (UserType)user.UserTypeId is UserType.Admin or UserType.SuperAdmin;
        if (!isAdmin && user.CompanyId != transaction.CompanyId)
        {
            return Forbid();
        }

        // Only allow deleting manual transactions
        if (transaction.SourceEntityType != "Manual")
        {
            return BadRequest(CreateProblem("Only manual transactions can be deleted."));
        }

        // Check if the month is locked
        var transactionMonth = transaction.TransactionDate.Month;
        var transactionYear = transaction.TransactionDate.Year;
        if (await auditLogger.IsMonthLockedAsync(transaction.CompanyId, transactionMonth, transactionYear))
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                CreateProblem("Cannot modify data for submitted reports. Contact administrator."));
        }

        // Capture for audit before deleting
        var companyId = transaction.CompanyId;
        var deletedTransaction = new TtbTransaction
        {
            Id = transaction.Id,
            CompanyId = transaction.CompanyId,
            TransactionDate = transaction.TransactionDate,
            TransactionType = transaction.TransactionType,
            ProductType = transaction.ProductType,
            SpiritsType = transaction.SpiritsType,
            ProofGallons = transaction.ProofGallons,
            WineGallons = transaction.WineGallons,
            SourceEntityType = transaction.SourceEntityType,
            SourceEntityId = transaction.SourceEntityId,
            Notes = transaction.Notes
        };

        dbContext.TtbTransactions.Remove(transaction);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Log the audit trail
        await auditLogger.LogChangeAsync(TtbAuditAction.Delete, null, deletedTransaction, user.Id, companyId);

        logger.LogInformation(
            "User {UserId} deleted manual TTB transaction {TransactionId}",
            user.Id, deletedTransaction.Id);

        return NoContent();
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
}

public record TtbTransactionResponse
{
    public int Id { get; init; }
    public int CompanyId { get; init; }
    public DateTime TransactionDate { get; init; }
    public TtbTransactionType TransactionType { get; init; }
    public string ProductType { get; init; } = string.Empty;
    public TtbSpiritsType SpiritsType { get; init; }
    public decimal ProofGallons { get; init; }
    public decimal WineGallons { get; init; }
    public string? SourceEntityType { get; init; }
    public int? SourceEntityId { get; init; }
    public string? Notes { get; init; }
}

public record CreateTtbTransactionRequest
{
    [Required]
    public int CompanyId { get; init; }

    [Required]
    public DateTime TransactionDate { get; init; }

    [Required]
    public TtbTransactionType TransactionType { get; init; }

    [Required]
    public string ProductType { get; init; } = string.Empty;

    [Required]
    public TtbSpiritsType SpiritsType { get; init; }

    [Required]
    public decimal ProofGallons { get; init; }

    [Required]
    public decimal WineGallons { get; init; }

    public string? Notes { get; init; }
}

public record UpdateTtbTransactionRequest
{
    [Required]
    public DateTime TransactionDate { get; init; }

    [Required]
    public TtbTransactionType TransactionType { get; init; }

    [Required]
    public string ProductType { get; init; } = string.Empty;

    [Required]
    public TtbSpiritsType SpiritsType { get; init; }

    [Required]
    public decimal ProofGallons { get; init; }

    [Required]
    public decimal WineGallons { get; init; }

    public string? Notes { get; init; }
}
