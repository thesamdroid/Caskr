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
/// Manages TTB gauge records for barrels, including manual entry and retrieval.
/// </summary>
public sealed class TtbGaugeRecordsController(
    ITtbGaugeRecordService gaugeRecordService,
    CaskrDbContext dbContext,
    IUsersService usersService,
    ILogger<TtbGaugeRecordsController> logger) : AuthorizedApiControllerBase
{
    [HttpGet("/api/ttb/gauge-records/barrel/{barrelId:int}")]
    [ProducesResponseType(typeof(IEnumerable<TtbGaugeRecordResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForBarrel(int barrelId, CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        // Verify barrel exists and user has access
        var barrel = await dbContext.Barrels
            .FirstOrDefaultAsync(b => b.Id == barrelId, cancellationToken);

        if (barrel is null)
        {
            return NotFound(CreateProblem("Barrel not found."));
        }

        var isAdmin = (UserType)user.UserTypeId is UserType.Admin or UserType.SuperAdmin;
        if (!isAdmin && user.CompanyId != barrel.CompanyId)
        {
            return Forbid();
        }

        var records = await gaugeRecordService.GetGaugeRecordsForBarrelAsync(barrelId);

        var response = records.Select(r => new TtbGaugeRecordResponse
        {
            Id = r.Id,
            BarrelId = r.BarrelId,
            GaugeDate = r.GaugeDate,
            GaugeType = r.GaugeType,
            Proof = r.Proof,
            Temperature = r.Temperature,
            WineGallons = r.WineGallons,
            ProofGallons = r.ProofGallons,
            GaugedByUserId = r.GaugedByUserId,
            GaugedByUserName = r.GaugedByUser?.Name,
            Notes = r.Notes
        });

        return Ok(response);
    }

    [HttpGet("/api/ttb/gauge-records/company/{companyId:int}")]
    [ProducesResponseType(typeof(IEnumerable<TtbGaugeRecordResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetForCompany(
        int companyId,
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

        var records = await gaugeRecordService.GetGaugeRecordsForCompanyAsync(companyId, startDate, endDate);

        var response = records.Select(r => new TtbGaugeRecordResponse
        {
            Id = r.Id,
            BarrelId = r.BarrelId,
            BarrelSku = r.Barrel.Sku,
            GaugeDate = r.GaugeDate,
            GaugeType = r.GaugeType,
            Proof = r.Proof,
            Temperature = r.Temperature,
            WineGallons = r.WineGallons,
            ProofGallons = r.ProofGallons,
            GaugedByUserId = r.GaugedByUserId,
            GaugedByUserName = r.GaugedByUser?.Name,
            Notes = r.Notes
        });

        return Ok(response);
    }

    [HttpGet("/api/ttb/gauge-records/{id:int}")]
    [ProducesResponseType(typeof(TtbGaugeRecordResponse), StatusCodes.Status200OK)]
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

        var record = await gaugeRecordService.GetGaugeRecordByIdAsync(id);

        if (record is null)
        {
            return NotFound(CreateProblem("Gauge record not found."));
        }

        var isAdmin = (UserType)user.UserTypeId is UserType.Admin or UserType.SuperAdmin;
        if (!isAdmin && user.CompanyId != record.Barrel.CompanyId)
        {
            return Forbid();
        }

        var response = new TtbGaugeRecordResponse
        {
            Id = record.Id,
            BarrelId = record.BarrelId,
            BarrelSku = record.Barrel.Sku,
            GaugeDate = record.GaugeDate,
            GaugeType = record.GaugeType,
            Proof = record.Proof,
            Temperature = record.Temperature,
            WineGallons = record.WineGallons,
            ProofGallons = record.ProofGallons,
            GaugedByUserId = record.GaugedByUserId,
            GaugedByUserName = record.GaugedByUser?.Name,
            Notes = record.Notes
        };

        return Ok(response);
    }

    [HttpPost("/api/ttb/gauge-records")]
    [ProducesResponseType(typeof(TtbGaugeRecordResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(
        [FromBody] CreateTtbGaugeRecordRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        // Verify barrel exists and user has access
        var barrel = await dbContext.Barrels
            .FirstOrDefaultAsync(b => b.Id == request.BarrelId, cancellationToken);

        if (barrel is null)
        {
            return NotFound(CreateProblem("Barrel not found."));
        }

        var isAdmin = (UserType)user.UserTypeId is UserType.Admin or UserType.SuperAdmin;
        if (!isAdmin && user.CompanyId != barrel.CompanyId)
        {
            return Forbid();
        }

        // Validate inputs
        if (request.Proof < 0 || request.Proof > 200)
        {
            return BadRequest(CreateProblem("Proof must be between 0 and 200."));
        }

        if (request.Temperature < -40 || request.Temperature > 150)
        {
            return BadRequest(CreateProblem("Temperature must be between -40 and 150 degrees Fahrenheit."));
        }

        if (request.WineGallons <= 0)
        {
            return BadRequest(CreateProblem("WineGallons must be greater than 0."));
        }

        var record = await gaugeRecordService.CreateGaugeRecordAsync(
            request.BarrelId,
            request.GaugeType,
            request.Proof,
            request.Temperature,
            request.WineGallons,
            user.Id,
            request.Notes);

        logger.LogInformation(
            "User {UserId} created gauge record {GaugeRecordId} for barrel {BarrelId}",
            user.Id, record.Id, request.BarrelId);

        var response = new TtbGaugeRecordResponse
        {
            Id = record.Id,
            BarrelId = record.BarrelId,
            BarrelSku = barrel.Sku,
            GaugeDate = record.GaugeDate,
            GaugeType = record.GaugeType,
            Proof = record.Proof,
            Temperature = record.Temperature,
            WineGallons = record.WineGallons,
            ProofGallons = record.ProofGallons,
            GaugedByUserId = record.GaugedByUserId,
            GaugedByUserName = user.Name,
            Notes = record.Notes
        };

        return CreatedAtAction(nameof(Get), new { id = record.Id }, response);
    }

    [HttpPut("/api/ttb/gauge-records/{id:int}")]
    [ProducesResponseType(typeof(TtbGaugeRecordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateTtbGaugeRecordRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var existingRecord = await gaugeRecordService.GetGaugeRecordByIdAsync(id);

        if (existingRecord is null)
        {
            return NotFound(CreateProblem("Gauge record not found."));
        }

        var isAdmin = (UserType)user.UserTypeId is UserType.Admin or UserType.SuperAdmin;
        if (!isAdmin && user.CompanyId != existingRecord.Barrel.CompanyId)
        {
            return Forbid();
        }

        // Validate inputs
        if (request.Proof < 0 || request.Proof > 200)
        {
            return BadRequest(CreateProblem("Proof must be between 0 and 200."));
        }

        if (request.Temperature < -40 || request.Temperature > 150)
        {
            return BadRequest(CreateProblem("Temperature must be between -40 and 150 degrees Fahrenheit."));
        }

        if (request.WineGallons <= 0)
        {
            return BadRequest(CreateProblem("WineGallons must be greater than 0."));
        }

        var record = await gaugeRecordService.UpdateGaugeRecordAsync(
            id,
            request.Proof,
            request.Temperature,
            request.WineGallons,
            request.Notes);

        logger.LogInformation(
            "User {UserId} updated gauge record {GaugeRecordId}",
            user.Id, record.Id);

        var response = new TtbGaugeRecordResponse
        {
            Id = record.Id,
            BarrelId = record.BarrelId,
            BarrelSku = existingRecord.Barrel.Sku,
            GaugeDate = record.GaugeDate,
            GaugeType = record.GaugeType,
            Proof = record.Proof,
            Temperature = record.Temperature,
            WineGallons = record.WineGallons,
            ProofGallons = record.ProofGallons,
            GaugedByUserId = record.GaugedByUserId,
            GaugedByUserName = existingRecord.GaugedByUser?.Name,
            Notes = record.Notes
        };

        return Ok(response);
    }

    [HttpDelete("/api/ttb/gauge-records/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
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

        var record = await gaugeRecordService.GetGaugeRecordByIdAsync(id);

        if (record is null)
        {
            return NotFound(CreateProblem("Gauge record not found."));
        }

        var isAdmin = (UserType)user.UserTypeId is UserType.Admin or UserType.SuperAdmin;
        if (!isAdmin && user.CompanyId != record.Barrel.CompanyId)
        {
            return Forbid();
        }

        await gaugeRecordService.DeleteGaugeRecordAsync(id);

        logger.LogInformation(
            "User {UserId} deleted gauge record {GaugeRecordId}",
            user.Id, record.Id);

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

public record TtbGaugeRecordResponse
{
    public int Id { get; init; }
    public int BarrelId { get; init; }
    public string? BarrelSku { get; init; }
    public DateTime GaugeDate { get; init; }
    public TtbGaugeType GaugeType { get; init; }
    public decimal Proof { get; init; }
    public decimal Temperature { get; init; }
    public decimal WineGallons { get; init; }
    public decimal ProofGallons { get; init; }
    public int? GaugedByUserId { get; init; }
    public string? GaugedByUserName { get; init; }
    public string? Notes { get; init; }
}

public record CreateTtbGaugeRecordRequest
{
    [Required]
    public int BarrelId { get; init; }

    [Required]
    public TtbGaugeType GaugeType { get; init; }

    [Required]
    public decimal Proof { get; init; }

    [Required]
    public decimal Temperature { get; init; }

    [Required]
    public decimal WineGallons { get; init; }

    public string? Notes { get; init; }
}

public record UpdateTtbGaugeRecordRequest
{
    [Required]
    public decimal Proof { get; init; }

    [Required]
    public decimal Temperature { get; init; }

    [Required]
    public decimal WineGallons { get; init; }

    public string? Notes { get; init; }
}
