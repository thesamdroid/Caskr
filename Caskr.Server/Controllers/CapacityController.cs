using System.Security.Claims;
using System.Text;
using Caskr.server.Models;
using Caskr.server.Models.Production;
using Caskr.server.Services;
using Caskr.server.Services.Production;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Caskr.server.Controllers;

#region Request/Response DTOs

/// <summary>
/// Response for capacity overview.
/// </summary>
public class CapacityOverviewResponse
{
    public int TotalEquipmentCount { get; set; }
    public decimal TotalCapacityHours { get; set; }
    public decimal AllocatedHours { get; set; }
    public decimal AvailableHours { get; set; }
    public decimal OverallUtilizationPercent { get; set; }
    public List<EquipmentCapacitySummaryResponse> Equipment { get; set; } = new();
    public List<CapacityAlertResponse> Alerts { get; set; } = new();
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
}

/// <summary>
/// Equipment capacity summary response.
/// </summary>
public class EquipmentCapacitySummaryResponse
{
    public int EquipmentId { get; set; }
    public string EquipmentName { get; set; } = null!;
    public string EquipmentType { get; set; } = null!;
    public decimal TotalCapacityHours { get; set; }
    public decimal AllocatedHours { get; set; }
    public decimal AvailableHours { get; set; }
    public decimal UtilizationPercent { get; set; }
}

/// <summary>
/// Capacity alert response.
/// </summary>
public class CapacityAlertResponse
{
    public string Severity { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public int? EquipmentId { get; set; }
    public string? EquipmentName { get; set; }
}

/// <summary>
/// Bottleneck response.
/// </summary>
public class BottleneckResponse
{
    public int EquipmentId { get; set; }
    public string EquipmentName { get; set; } = null!;
    public string EquipmentType { get; set; } = null!;
    public string Severity { get; set; } = null!;
    public decimal UtilizationPercent { get; set; }
    public int AffectedProductionRuns { get; set; }
    public string AverageWaitTime { get; set; } = null!;
    public string Description { get; set; } = null!;
}

/// <summary>
/// Forecast response.
/// </summary>
public class ForecastResponse
{
    public DateTime GeneratedAt { get; set; }
    public string Method { get; set; } = null!;
    public decimal ConfidenceLevel { get; set; }
    public List<WeeklyForecastResponse> Weeks { get; set; } = new();
    public string Trend { get; set; } = null!;
    public List<string> Assumptions { get; set; } = new();
}

/// <summary>
/// Weekly forecast response.
/// </summary>
public class WeeklyForecastResponse
{
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }
    public decimal PredictedUtilization { get; set; }
    public decimal LowerBound { get; set; }
    public decimal UpperBound { get; set; }
    public decimal PredictedHoursUsed { get; set; }
    public decimal AvailableHours { get; set; }
}

/// <summary>
/// Capacity plan summary response.
/// </summary>
public class CapacityPlanSummaryResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime PlanPeriodStart { get; set; }
    public DateTime PlanPeriodEnd { get; set; }
    public string PlanType { get; set; } = null!;
    public string Status { get; set; } = null!;
    public decimal? TargetProofGallons { get; set; }
    public int? TargetBottles { get; set; }
    public int? TargetBatches { get; set; }
    public int AllocationCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Capacity plan detail response.
/// </summary>
public class CapacityPlanDetailResponse : CapacityPlanSummaryResponse
{
    public string? Notes { get; set; }
    public string? CreatedByUserName { get; set; }
    public List<CapacityAllocationResponse> Allocations { get; set; } = new();
}

/// <summary>
/// Capacity allocation response.
/// </summary>
public class CapacityAllocationResponse
{
    public int Id { get; set; }
    public int EquipmentId { get; set; }
    public string EquipmentName { get; set; } = null!;
    public string AllocationType { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal HoursAllocated { get; set; }
    public string? ProductionType { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Constraint response.
/// </summary>
public class ConstraintResponse
{
    public int Id { get; set; }
    public int? EquipmentId { get; set; }
    public string? EquipmentName { get; set; }
    public string ConstraintType { get; set; } = null!;
    public decimal ConstraintValue { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? Reason { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Scenario comparison request.
/// </summary>
public class ScenarioCompareRequest
{
    public List<int> ScenarioIds { get; set; } = new();
}

/// <summary>
/// Paged result wrapper.
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}

#endregion

/// <summary>
/// Controller for capacity planning and analysis operations.
/// Provides endpoints for capacity overview, utilization analysis, bottleneck detection,
/// forecasting, what-if scenarios, and constraint management.
/// </summary>
public class CapacityController(
    CaskrDbContext dbContext,
    ICapacityAnalysisService capacityService,
    IUsersService usersService,
    ILogger<CapacityController> logger)
    : AuthorizedApiControllerBase
{
    private readonly CaskrDbContext _dbContext = dbContext;
    private readonly ICapacityAnalysisService _capacityService = capacityService;
    private readonly IUsersService _usersService = usersService;
    private readonly ILogger<CapacityController> _logger = logger;

    #region Capacity Overview Endpoints

    /// <summary>
    /// Get capacity overview for a date range.
    /// </summary>
    /// <param name="companyId">The company ID</param>
    /// <param name="startDate">Start of analysis period</param>
    /// <param name="endDate">End of analysis period</param>
    /// <returns>Capacity overview with equipment summaries and alerts</returns>
    [HttpGet("overview/company/{companyId}")]
    [ProducesResponseType(typeof(CapacityOverviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CapacityOverviewResponse>> GetOverview(
        int companyId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        var user = await GetCurrentUserAsync();
        if (!IsAuthorizedForCompany(user, companyId))
            return Forbid();

        if (endDate <= startDate)
            return BadRequest(new { error = "End date must be after start date" });

        _logger.LogInformation("Getting capacity overview for company {CompanyId} from {Start} to {End}",
            companyId, startDate, endDate);

        var overview = await _capacityService.GetCapacityOverviewAsync(companyId, startDate, endDate);

        return Ok(new CapacityOverviewResponse
        {
            TotalEquipmentCount = overview.TotalEquipmentCount,
            TotalCapacityHours = overview.TotalCapacityHours,
            AllocatedHours = overview.AllocatedHours,
            AvailableHours = overview.AvailableHours,
            OverallUtilizationPercent = overview.OverallUtilizationPercent,
            Equipment = overview.EquipmentSummaries.Select(e => new EquipmentCapacitySummaryResponse
            {
                EquipmentId = e.EquipmentId,
                EquipmentName = e.EquipmentName,
                EquipmentType = e.EquipmentType,
                TotalCapacityHours = e.TotalCapacityHours,
                AllocatedHours = e.AllocatedHours,
                AvailableHours = e.AvailableHours,
                UtilizationPercent = e.UtilizationPercent
            }).ToList(),
            Alerts = overview.Alerts.Select(a => new CapacityAlertResponse
            {
                Severity = a.Severity.ToString(),
                Title = a.Title,
                Description = a.Description,
                EquipmentId = a.EquipmentId,
                EquipmentName = a.EquipmentName
            }).ToList(),
            PeriodStart = startDate,
            PeriodEnd = endDate
        });
    }

    /// <summary>
    /// Get detailed capacity for specific equipment.
    /// </summary>
    [HttpGet("equipment/{equipmentId}")]
    [ProducesResponseType(typeof(EquipmentCapacityDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EquipmentCapacityDetail>> GetEquipmentCapacity(
        int equipmentId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        var equipment = await _dbContext.Equipment.FindAsync(equipmentId);
        if (equipment == null)
            return NotFound();

        var user = await GetCurrentUserAsync();
        if (!IsAuthorizedForCompany(user, equipment.CompanyId))
            return Forbid();

        var detail = await _capacityService.GetEquipmentCapacityAsync(equipmentId, startDate, endDate);
        return Ok(detail);
    }

    /// <summary>
    /// Get capacity breakdown by production type.
    /// </summary>
    [HttpGet("by-type/company/{companyId}")]
    [ProducesResponseType(typeof(IEnumerable<CapacityByProductionType>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CapacityByProductionType>>> GetCapacityByType(
        int companyId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        var user = await GetCurrentUserAsync();
        if (!IsAuthorizedForCompany(user, companyId))
            return Forbid();

        var result = await _capacityService.GetCapacityByTypeAsync(companyId, startDate, endDate);
        return Ok(result);
    }

    #endregion

    #region Utilization Endpoints

    /// <summary>
    /// Calculate utilization report.
    /// </summary>
    [HttpGet("utilization/company/{companyId}")]
    [ProducesResponseType(typeof(UtilizationReport), StatusCodes.Status200OK)]
    public async Task<ActionResult<UtilizationReport>> GetUtilization(
        int companyId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] string groupBy = "week")
    {
        var user = await GetCurrentUserAsync();
        if (!IsAuthorizedForCompany(user, companyId))
            return Forbid();

        var result = await _capacityService.CalculateUtilizationAsync(companyId, startDate, endDate, groupBy);
        return Ok(result);
    }

    /// <summary>
    /// Get historical utilization trend.
    /// </summary>
    [HttpGet("utilization/trend/company/{companyId}")]
    [ProducesResponseType(typeof(UtilizationTrend), StatusCodes.Status200OK)]
    public async Task<ActionResult<UtilizationTrend>> GetUtilizationTrend(
        int companyId,
        [FromQuery] int months = 6)
    {
        var user = await GetCurrentUserAsync();
        if (!IsAuthorizedForCompany(user, companyId))
            return Forbid();

        var result = await _capacityService.GetUtilizationTrendAsync(companyId, months);
        return Ok(result);
    }

    /// <summary>
    /// Get per-equipment utilization.
    /// </summary>
    [HttpGet("utilization/equipment/company/{companyId}")]
    [ProducesResponseType(typeof(IEnumerable<EquipmentUtilization>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EquipmentUtilization>>> GetEquipmentUtilization(
        int companyId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        var user = await GetCurrentUserAsync();
        if (!IsAuthorizedForCompany(user, companyId))
            return Forbid();

        var result = await _capacityService.GetEquipmentUtilizationAsync(companyId, startDate, endDate);
        return Ok(result);
    }

    #endregion

    #region Bottleneck Analysis Endpoints

    /// <summary>
    /// Identify current bottlenecks.
    /// </summary>
    [HttpGet("bottlenecks/company/{companyId}")]
    [ProducesResponseType(typeof(IEnumerable<BottleneckResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<BottleneckResponse>>> GetBottlenecks(
        int companyId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] string? minSeverity = null)
    {
        var user = await GetCurrentUserAsync();
        if (!IsAuthorizedForCompany(user, companyId))
            return Forbid();

        BottleneckSeverity? severityFilter = null;
        if (!string.IsNullOrEmpty(minSeverity) && Enum.TryParse<BottleneckSeverity>(minSeverity, true, out var severity))
        {
            severityFilter = severity;
        }

        var bottlenecks = await _capacityService.IdentifyBottlenecksAsync(companyId, startDate, endDate, severityFilter);

        return Ok(bottlenecks.Select(b => new BottleneckResponse
        {
            EquipmentId = b.EquipmentId,
            EquipmentName = b.EquipmentName,
            EquipmentType = b.EquipmentType,
            Severity = b.Severity.ToString(),
            UtilizationPercent = b.UtilizationPercent,
            AffectedProductionRuns = b.AffectedProductionRuns,
            AverageWaitTime = b.AverageWaitTime.ToString(@"hh\:mm"),
            Description = b.Description
        }));
    }

    /// <summary>
    /// Detailed bottleneck analysis for specific equipment.
    /// </summary>
    [HttpGet("bottlenecks/{equipmentId}")]
    [ProducesResponseType(typeof(BottleneckAnalysis), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BottleneckAnalysis>> AnalyzeBottleneck(
        int equipmentId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        var equipment = await _dbContext.Equipment.FindAsync(equipmentId);
        if (equipment == null)
            return NotFound();

        var user = await GetCurrentUserAsync();
        if (!IsAuthorizedForCompany(user, equipment.CompanyId))
            return Forbid();

        var analysis = await _capacityService.AnalyzeBottleneckAsync(equipmentId, startDate, endDate);
        return Ok(analysis);
    }

    /// <summary>
    /// Get resolution suggestions for a bottleneck.
    /// </summary>
    [HttpGet("bottlenecks/{equipmentId}/resolutions")]
    [ProducesResponseType(typeof(IEnumerable<BottleneckResolution>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<BottleneckResolution>>> GetResolutions(
        int equipmentId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        var equipment = await _dbContext.Equipment.FindAsync(equipmentId);
        if (equipment == null)
            return NotFound();

        var user = await GetCurrentUserAsync();
        if (!IsAuthorizedForCompany(user, equipment.CompanyId))
            return Forbid();

        var analysis = await _capacityService.AnalyzeBottleneckAsync(equipmentId, startDate, endDate);
        var bottleneck = new Bottleneck(
            equipmentId,
            equipment.Name,
            equipment.EquipmentType.ToString(),
            analysis.Severity,
            analysis.UtilizationPercent,
            analysis.AffectedRuns.Count,
            analysis.AverageWaitTime,
            analysis.EstimatedLostCapacity,
            ""
        );

        var resolutions = await _capacityService.SuggestResolutionsAsync(bottleneck);
        return Ok(resolutions);
    }

    #endregion

    #region Capacity Plan Management Endpoints

    /// <summary>
    /// Create new capacity plan.
    /// </summary>
    [HttpPost("plans/company/{companyId}")]
    [ProducesResponseType(typeof(CapacityPlanDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CapacityPlanDetailResponse>> CreatePlan(
        int companyId,
        [FromBody] CreateCapacityPlanDto dto)
    {
        var user = await GetCurrentUserAsync();
        if (!IsAuthorizedForCompany(user, companyId))
            return Forbid();

        try
        {
            var plan = await _capacityService.CreateCapacityPlanAsync(dto, companyId, user!.Id);
            var response = MapToDetailResponse(plan);
            return CreatedAtAction(nameof(GetPlan), new { companyId, planId = plan.Id }, response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// List capacity plans.
    /// </summary>
    [HttpGet("plans/company/{companyId}")]
    [ProducesResponseType(typeof(PagedResult<CapacityPlanSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CapacityPlanSummaryResponse>>> GetPlans(
        int companyId,
        [FromQuery] string? status = null,
        [FromQuery] string? planType = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var user = await GetCurrentUserAsync();
        if (!IsAuthorizedForCompany(user, companyId))
            return Forbid();

        CapacityPlanStatus? statusFilter = null;
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<CapacityPlanStatus>(status, true, out var s))
            statusFilter = s;

        CapacityPlanType? typeFilter = null;
        if (!string.IsNullOrEmpty(planType) && Enum.TryParse<CapacityPlanType>(planType, true, out var t))
            typeFilter = t;

        var plans = await _capacityService.GetCapacityPlansAsync(companyId, statusFilter, typeFilter);
        var planList = plans.ToList();

        var paged = planList
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(MapToSummaryResponse)
            .ToList();

        return Ok(new PagedResult<CapacityPlanSummaryResponse>
        {
            Items = paged,
            TotalCount = planList.Count,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }

    /// <summary>
    /// Get capacity plan details.
    /// </summary>
    [HttpGet("plans/{planId}/company/{companyId}")]
    [ProducesResponseType(typeof(CapacityPlanDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CapacityPlanDetailResponse>> GetPlan(int planId, int companyId)
    {
        var user = await GetCurrentUserAsync();
        if (!IsAuthorizedForCompany(user, companyId))
            return Forbid();

        var plan = await _capacityService.GetCapacityPlanAsync(planId, companyId);
        if (plan == null)
            return NotFound();

        return Ok(MapToDetailResponse(plan));
    }

    /// <summary>
    /// Update capacity plan.
    /// </summary>
    [HttpPut("plans/{planId}/company/{companyId}")]
    [ProducesResponseType(typeof(CapacityPlanDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CapacityPlanDetailResponse>> UpdatePlan(
        int planId,
        int companyId,
        [FromBody] UpdateCapacityPlanDto dto)
    {
        var user = await GetCurrentUserAsync();
        if (!IsAuthorizedForCompany(user, companyId))
            return Forbid();

        try
        {
            var plan = await _capacityService.UpdateCapacityPlanAsync(planId, dto, companyId);
            return Ok(MapToDetailResponse(plan));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Delete/archive capacity plan.
    /// </summary>
    [HttpDelete("plans/{planId}/company/{companyId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePlan(int planId, int companyId)
    {
        var user = await GetCurrentUserAsync();
        if (!IsAuthorizedForCompany(user, companyId))
            return Forbid();

        try
        {
            await _capacityService.DeleteCapacityPlanAsync(planId, companyId);
            return NoContent();
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Validate capacity plan against constraints.
    /// </summary>
    [HttpPost("plans/{planId}/validate")]
    [ProducesResponseType(typeof(CapacityValidation), StatusCodes.Status200OK)]
    public async Task<ActionResult<CapacityValidation>> ValidatePlan(int planId)
    {
        var plan = await _dbContext.CapacityPlans.FindAsync(planId);
        if (plan == null)
            return NotFound();

        var user = await GetCurrentUserAsync();
        if (!IsAuthorizedForCompany(user, plan.CompanyId))
            return Forbid();

        var validation = await _capacityService.ValidateCapacityPlanAsync(planId);
        return Ok(validation);
    }

    /// <summary>
    /// Activate a draft plan.
    /// </summary>
    [HttpPost("plans/{planId}/activate/company/{companyId}")]
    [ProducesResponseType(typeof(CapacityPlanDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CapacityPlanDetailResponse>> ActivatePlan(int planId, int companyId)
    {
        var user = await GetCurrentUserAsync();
        if (!IsAuthorizedForCompany(user, companyId))
            return Forbid();

        try
        {
            var plan = await _capacityService.ActivateCapacityPlanAsync(planId, companyId);
            return Ok(MapToDetailResponse(plan));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    #endregion

    #region Forecasting Endpoints

    /// <summary>
    /// Get capacity forecast.
    /// </summary>
    [HttpGet("forecast/company/{companyId}")]
    [ProducesResponseType(typeof(ForecastResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ForecastResponse>> GetForecast(
        int companyId,
        [FromQuery] int weeksAhead = 12,
        [FromQuery] string method = "MovingAverage")
    {
        var user = await GetCurrentUserAsync();
        if (!IsAuthorizedForCompany(user, companyId))
            return Forbid();

        if (!Enum.TryParse<ForecastMethod>(method, true, out var forecastMethod))
            return BadRequest(new { error = "Invalid forecast method. Use: MovingAverage, ExponentialSmoothing, LinearRegression, or SeasonalAdjusted" });

        try
        {
            var forecast = await _capacityService.ForecastCapacityAsync(companyId, weeksAhead, forecastMethod);

            return Ok(new ForecastResponse
            {
                GeneratedAt = forecast.GeneratedAt,
                Method = forecast.Method.ToString(),
                ConfidenceLevel = forecast.ConfidenceLevel,
                Weeks = forecast.WeeklyForecasts.Select(w => new WeeklyForecastResponse
                {
                    WeekStart = w.WeekStart,
                    WeekEnd = w.WeekEnd,
                    PredictedUtilization = w.PredictedUtilization,
                    LowerBound = w.LowerBound,
                    UpperBound = w.UpperBound,
                    PredictedHoursUsed = w.PredictedHoursUsed,
                    AvailableHours = w.AvailableHours
                }).ToList(),
                Trend = forecast.WeeklyForecasts.Count >= 2
                    ? (forecast.WeeklyForecasts.Last().PredictedUtilization > forecast.WeeklyForecasts.First().PredictedUtilization ? "Increasing" : "Decreasing")
                    : "Stable",
                Assumptions = forecast.Assumptions
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get demand forecast.
    /// </summary>
    [HttpGet("forecast/demand/company/{companyId}")]
    [ProducesResponseType(typeof(DemandForecast), StatusCodes.Status200OK)]
    public async Task<ActionResult<DemandForecast>> GetDemandForecast(
        int companyId,
        [FromQuery] int weeksAhead = 12)
    {
        var user = await GetCurrentUserAsync();
        if (!IsAuthorizedForCompany(user, companyId))
            return Forbid();

        var forecast = await _capacityService.ForecastDemandAsync(companyId, weeksAhead);
        return Ok(forecast);
    }

    /// <summary>
    /// Analyze capacity vs demand gap.
    /// </summary>
    [HttpGet("gap-analysis/company/{companyId}")]
    [ProducesResponseType(typeof(GapAnalysis), StatusCodes.Status200OK)]
    public async Task<ActionResult<GapAnalysis>> GetGapAnalysis(
        int companyId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        var user = await GetCurrentUserAsync();
        if (!IsAuthorizedForCompany(user, companyId))
            return Forbid();

        var analysis = await _capacityService.AnalyzeCapacityGapAsync(companyId, startDate, endDate);
        return Ok(analysis);
    }

    #endregion

    #region What-If Scenario Endpoints

    /// <summary>
    /// Create and run what-if scenario.
    /// </summary>
    [HttpPost("scenarios/company/{companyId}")]
    [ProducesResponseType(typeof(ScenarioResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<ScenarioResult>> RunScenario(
        int companyId,
        [FromBody] WhatIfScenarioDto scenario)
    {
        var user = await GetCurrentUserAsync();
        if (!IsAuthorizedForCompany(user, companyId))
            return Forbid();

        var result = await _capacityService.RunScenarioAsync(scenario, companyId);
        return Ok(result);
    }

    /// <summary>
    /// Compare multiple scenarios.
    /// </summary>
    [HttpPost("scenarios/compare")]
    [ProducesResponseType(typeof(IEnumerable<ScenarioComparison>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ScenarioComparison>>> CompareScenarios(
        [FromBody] ScenarioCompareRequest request)
    {
        var comparisons = await _capacityService.CompareScenarioResultsAsync(request.ScenarioIds);
        return Ok(comparisons);
    }

    #endregion

    #region Constraint Management Endpoints

    /// <summary>
    /// List capacity constraints.
    /// </summary>
    [HttpGet("constraints/company/{companyId}")]
    [ProducesResponseType(typeof(IEnumerable<ConstraintResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ConstraintResponse>>> GetConstraints(
        int companyId,
        [FromQuery] int? equipmentId = null,
        [FromQuery] bool activeOnly = true)
    {
        var user = await GetCurrentUserAsync();
        if (!IsAuthorizedForCompany(user, companyId))
            return Forbid();

        var constraints = await _capacityService.GetConstraintsAsync(companyId, equipmentId, activeOnly);

        return Ok(constraints.Select(c => new ConstraintResponse
        {
            Id = c.Id,
            EquipmentId = c.EquipmentId,
            EquipmentName = c.Equipment?.Name,
            ConstraintType = c.ConstraintType.ToString(),
            ConstraintValue = c.ConstraintValue,
            EffectiveFrom = c.EffectiveFrom,
            EffectiveTo = c.EffectiveTo,
            Reason = c.Reason,
            IsActive = c.IsActive
        }));
    }

    /// <summary>
    /// Create constraint.
    /// </summary>
    [HttpPost("constraints/company/{companyId}")]
    [ProducesResponseType(typeof(ConstraintResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<ConstraintResponse>> CreateConstraint(
        int companyId,
        [FromBody] CreateConstraintDto dto)
    {
        var user = await GetCurrentUserAsync();
        if (!IsAuthorizedForCompany(user, companyId))
            return Forbid();

        var constraint = await _capacityService.CreateConstraintAsync(dto, companyId, user!.Id);

        return CreatedAtAction(nameof(GetConstraints), new { companyId }, new ConstraintResponse
        {
            Id = constraint.Id,
            EquipmentId = constraint.EquipmentId,
            ConstraintType = constraint.ConstraintType.ToString(),
            ConstraintValue = constraint.ConstraintValue,
            EffectiveFrom = constraint.EffectiveFrom,
            EffectiveTo = constraint.EffectiveTo,
            Reason = constraint.Reason,
            IsActive = constraint.IsActive
        });
    }

    /// <summary>
    /// Update constraint.
    /// </summary>
    [HttpPut("constraints/{constraintId}/company/{companyId}")]
    [ProducesResponseType(typeof(ConstraintResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ConstraintResponse>> UpdateConstraint(
        int constraintId,
        int companyId,
        [FromBody] UpdateConstraintDto dto)
    {
        var user = await GetCurrentUserAsync();
        if (!IsAuthorizedForCompany(user, companyId))
            return Forbid();

        try
        {
            var constraint = await _capacityService.UpdateConstraintAsync(constraintId, dto, companyId);
            return Ok(new ConstraintResponse
            {
                Id = constraint.Id,
                EquipmentId = constraint.EquipmentId,
                ConstraintType = constraint.ConstraintType.ToString(),
                ConstraintValue = constraint.ConstraintValue,
                EffectiveFrom = constraint.EffectiveFrom,
                EffectiveTo = constraint.EffectiveTo,
                Reason = constraint.Reason,
                IsActive = constraint.IsActive
            });
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Deactivate constraint.
    /// </summary>
    [HttpDelete("constraints/{constraintId}/company/{companyId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteConstraint(int constraintId, int companyId)
    {
        var user = await GetCurrentUserAsync();
        if (!IsAuthorizedForCompany(user, companyId))
            return Forbid();

        try
        {
            await _capacityService.DeleteConstraintAsync(constraintId, companyId);
            return NoContent();
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    #endregion

    #region Export Endpoints

    /// <summary>
    /// Export utilization report.
    /// </summary>
    [HttpGet("export/utilization/company/{companyId}")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportUtilization(
        int companyId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] string format = "csv")
    {
        var user = await GetCurrentUserAsync();
        if (!IsAuthorizedForCompany(user, companyId))
            return Forbid();

        var utilization = await _capacityService.GetEquipmentUtilizationAsync(companyId, startDate, endDate);

        if (format.ToLower() == "csv")
        {
            var csv = new StringBuilder();
            csv.AppendLine("Equipment ID,Equipment Name,Equipment Type,Utilization %,Hours Available,Hours Allocated,Hours in Maintenance");

            foreach (var item in utilization)
            {
                csv.AppendLine($"{item.EquipmentId},{EscapeCsv(item.EquipmentName)},{item.EquipmentType},{item.UtilizationPercent},{item.HoursAvailable},{item.HoursAllocated},{item.HoursInMaintenance}");
            }

            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"utilization-report-{startDate:yyyy-MM-dd}-{endDate:yyyy-MM-dd}.csv");
        }

        return BadRequest(new { error = "Only CSV format is currently supported" });
    }

    /// <summary>
    /// Export forecast report.
    /// </summary>
    [HttpGet("export/forecast/company/{companyId}")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportForecast(
        int companyId,
        [FromQuery] int weeksAhead = 12,
        [FromQuery] string format = "csv")
    {
        var user = await GetCurrentUserAsync();
        if (!IsAuthorizedForCompany(user, companyId))
            return Forbid();

        var forecast = await _capacityService.ForecastCapacityAsync(companyId, weeksAhead);

        if (format.ToLower() == "csv")
        {
            var csv = new StringBuilder();
            csv.AppendLine("Week Start,Week End,Predicted Utilization %,Lower Bound %,Upper Bound %,Predicted Hours,Available Hours");

            foreach (var week in forecast.WeeklyForecasts)
            {
                csv.AppendLine($"{week.WeekStart:yyyy-MM-dd},{week.WeekEnd:yyyy-MM-dd},{week.PredictedUtilization},{week.LowerBound},{week.UpperBound},{week.PredictedHoursUsed},{week.AvailableHours}");
            }

            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"forecast-report-{weeksAhead}-weeks.csv");
        }

        return BadRequest(new { error = "Only CSV format is currently supported" });
    }

    /// <summary>
    /// Export capacity plan.
    /// </summary>
    [HttpGet("plans/{planId}/export/company/{companyId}")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportPlan(
        int planId,
        int companyId,
        [FromQuery] string format = "csv")
    {
        var user = await GetCurrentUserAsync();
        if (!IsAuthorizedForCompany(user, companyId))
            return Forbid();

        var plan = await _capacityService.GetCapacityPlanAsync(planId, companyId);
        if (plan == null)
            return NotFound();

        if (format.ToLower() == "csv")
        {
            var csv = new StringBuilder();
            csv.AppendLine($"Capacity Plan: {plan.Name}");
            csv.AppendLine($"Period: {plan.PlanPeriodStart:yyyy-MM-dd} to {plan.PlanPeriodEnd:yyyy-MM-dd}");
            csv.AppendLine($"Status: {plan.Status}");
            csv.AppendLine();
            csv.AppendLine("Allocations:");
            csv.AppendLine("Equipment ID,Equipment Name,Type,Start Date,End Date,Hours Allocated,Production Type,Notes");

            foreach (var allocation in plan.Allocations)
            {
                csv.AppendLine($"{allocation.EquipmentId},{EscapeCsv(allocation.Equipment.Name)},{allocation.AllocationType},{allocation.StartDate:yyyy-MM-dd},{allocation.EndDate:yyyy-MM-dd},{allocation.HoursAllocated},{allocation.ProductionType},{EscapeCsv(allocation.Notes ?? "")}");
            }

            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"capacity-plan-{plan.Id}.csv");
        }

        return BadRequest(new { error = "Only CSV format is currently supported" });
    }

    #endregion

    #region Private Helper Methods

    private async Task<User?> GetCurrentUserAsync()
    {
        return await GetCurrentUserAsync(_usersService);
    }

    private bool IsAuthorizedForCompany(User? user, int companyId)
    {
        if (user is null)
            return false;

        return (UserType)user.UserTypeId == UserType.SuperAdmin || user.CompanyId == companyId;
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }

    private static CapacityPlanSummaryResponse MapToSummaryResponse(CapacityPlan plan)
    {
        return new CapacityPlanSummaryResponse
        {
            Id = plan.Id,
            Name = plan.Name,
            Description = plan.Description,
            PlanPeriodStart = plan.PlanPeriodStart,
            PlanPeriodEnd = plan.PlanPeriodEnd,
            PlanType = plan.PlanType.ToString(),
            Status = plan.Status.ToString(),
            TargetProofGallons = plan.TargetProofGallons,
            TargetBottles = plan.TargetBottles,
            TargetBatches = plan.TargetBatches,
            AllocationCount = plan.Allocations?.Count ?? 0,
            CreatedAt = plan.CreatedAt
        };
    }

    private static CapacityPlanDetailResponse MapToDetailResponse(CapacityPlan plan)
    {
        return new CapacityPlanDetailResponse
        {
            Id = plan.Id,
            Name = plan.Name,
            Description = plan.Description,
            PlanPeriodStart = plan.PlanPeriodStart,
            PlanPeriodEnd = plan.PlanPeriodEnd,
            PlanType = plan.PlanType.ToString(),
            Status = plan.Status.ToString(),
            TargetProofGallons = plan.TargetProofGallons,
            TargetBottles = plan.TargetBottles,
            TargetBatches = plan.TargetBatches,
            Notes = plan.Notes,
            CreatedByUserName = plan.CreatedByUser?.Name,
            AllocationCount = plan.Allocations?.Count ?? 0,
            CreatedAt = plan.CreatedAt,
            Allocations = plan.Allocations?.Select(a => new CapacityAllocationResponse
            {
                Id = a.Id,
                EquipmentId = a.EquipmentId,
                EquipmentName = a.Equipment?.Name ?? "Unknown",
                AllocationType = a.AllocationType.ToString(),
                StartDate = a.StartDate,
                EndDate = a.EndDate,
                HoursAllocated = a.HoursAllocated,
                ProductionType = a.ProductionType?.ToString(),
                Notes = a.Notes
            }).ToList() ?? new List<CapacityAllocationResponse>()
        };
    }

    #endregion
}
