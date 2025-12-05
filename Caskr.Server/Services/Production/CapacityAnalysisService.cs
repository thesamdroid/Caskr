using Caskr.server.Models;
using Caskr.server.Models.Production;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Caskr.server.Services.Production;

/// <summary>
/// Service for analyzing production capacity, identifying bottlenecks,
/// forecasting utilization, and supporting what-if scenario planning.
/// </summary>
public class CapacityAnalysisService : ICapacityAnalysisService
{
    private readonly CaskrDbContext _context;
    private readonly ILogger<CapacityAnalysisService> _logger;

    // Constants for capacity calculation
    private const decimal DefaultDailyOperatingHours = 16m;
    private const decimal HighUtilizationThreshold = 85m;
    private const decimal CriticalUtilizationThreshold = 95m;
    private const int MinDataPointsForForecast = 4;

    public CapacityAnalysisService(CaskrDbContext context, ILogger<CapacityAnalysisService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Current Capacity Analysis

    public async Task<CapacityOverview> GetCapacityOverviewAsync(int companyId, DateTime start, DateTime end)
    {
        _logger.LogInformation("Getting capacity overview for company {CompanyId} from {Start} to {End}", companyId, start, end);

        var equipment = await _context.Equipment
            .Where(e => e.CompanyId == companyId && e.IsActive)
            .ToListAsync();

        var allocations = await _context.CapacityAllocations
            .Include(a => a.Equipment)
            .Include(a => a.CapacityPlan)
            .Where(a => a.CapacityPlan.CompanyId == companyId &&
                        a.StartDate <= end && a.EndDate >= start)
            .ToListAsync();

        var constraints = await GetActiveConstraintsAsync(companyId, start, end);

        var totalDays = (end - start).Days + 1;
        var summaries = new List<EquipmentCapacitySummary>();
        var alerts = new List<CapacityAlert>();

        decimal totalCapacityHours = 0;
        decimal totalAllocatedHours = 0;

        foreach (var eq in equipment)
        {
            var dailyHours = GetDailyHoursForEquipment(eq.Id, constraints);
            var capacityHours = dailyHours * totalDays;
            var eqAllocations = allocations.Where(a => a.EquipmentId == eq.Id).ToList();
            var allocatedHours = CalculateAllocatedHours(eqAllocations, start, end);
            var availableHours = Math.Max(0, capacityHours - allocatedHours);
            var utilizationPercent = capacityHours > 0 ? (allocatedHours / capacityHours) * 100 : 0;

            summaries.Add(new EquipmentCapacitySummary(
                eq.Id,
                eq.Name,
                eq.EquipmentType.ToString(),
                capacityHours,
                allocatedHours,
                availableHours,
                Math.Round(utilizationPercent, 2)
            ));

            totalCapacityHours += capacityHours;
            totalAllocatedHours += allocatedHours;

            // Generate alerts for high utilization
            if (utilizationPercent >= CriticalUtilizationThreshold)
            {
                alerts.Add(new CapacityAlert(
                    CapacityAlertSeverity.Critical,
                    "Critical Utilization",
                    $"{eq.Name} is at {utilizationPercent:F1}% utilization",
                    eq.Id,
                    eq.Name
                ));
            }
            else if (utilizationPercent >= HighUtilizationThreshold)
            {
                alerts.Add(new CapacityAlert(
                    CapacityAlertSeverity.Warning,
                    "High Utilization",
                    $"{eq.Name} is at {utilizationPercent:F1}% utilization",
                    eq.Id,
                    eq.Name
                ));
            }
        }

        var overallUtilization = totalCapacityHours > 0
            ? (totalAllocatedHours / totalCapacityHours) * 100
            : 0;

        return new CapacityOverview(
            equipment.Count,
            totalCapacityHours,
            totalAllocatedHours,
            Math.Max(0, totalCapacityHours - totalAllocatedHours),
            Math.Round(overallUtilization, 2),
            summaries,
            alerts
        );
    }

    public async Task<EquipmentCapacityDetail> GetEquipmentCapacityAsync(int equipmentId, DateTime start, DateTime end)
    {
        var equipment = await _context.Equipment
            .FirstOrDefaultAsync(e => e.Id == equipmentId)
            ?? throw new InvalidOperationException($"Equipment {equipmentId} not found");

        var allocations = await _context.CapacityAllocations
            .Where(a => a.EquipmentId == equipmentId &&
                        a.StartDate <= end && a.EndDate >= start)
            .ToListAsync();

        var constraints = await _context.CapacityConstraints
            .Where(c => (c.EquipmentId == equipmentId || c.EquipmentId == null) &&
                        c.CompanyId == equipment.CompanyId &&
                        c.IsActive &&
                        c.EffectiveFrom <= end &&
                        (c.EffectiveTo == null || c.EffectiveTo >= start))
            .ToListAsync();

        var totalDays = (end - start).Days + 1;
        var dailyHours = GetDailyHoursForEquipment(equipmentId, constraints);
        var totalCapacity = dailyHours * totalDays;

        var productionHours = allocations
            .Where(a => a.AllocationType == CapacityAllocationType.Production)
            .Sum(a => CalculateHoursInRange(a, start, end));
        var maintenanceHours = allocations
            .Where(a => a.AllocationType == CapacityAllocationType.Maintenance)
            .Sum(a => CalculateHoursInRange(a, start, end));
        var bufferHours = allocations
            .Where(a => a.AllocationType == CapacityAllocationType.Buffer)
            .Sum(a => CalculateHoursInRange(a, start, end));

        var totalAllocated = productionHours + maintenanceHours + bufferHours;
        var utilizationPercent = totalCapacity > 0 ? (totalAllocated / totalCapacity) * 100 : 0;

        var allocationSummaries = allocations.Select(a => new AllocationSummary(
            a.Id,
            a.AllocationType.ToString(),
            a.StartDate,
            a.EndDate,
            a.HoursAllocated,
            a.ProductionType?.ToString(),
            a.Notes
        )).ToList();

        var constraintSummaries = constraints.Select(c => new ConstraintSummary(
            c.Id,
            c.ConstraintType.ToString(),
            c.ConstraintValue,
            c.EffectiveFrom,
            c.EffectiveTo,
            c.Reason
        )).ToList();

        return new EquipmentCapacityDetail(
            equipment.Id,
            equipment.Name,
            equipment.EquipmentType.ToString(),
            totalCapacity,
            productionHours,
            maintenanceHours,
            bufferHours,
            Math.Max(0, totalCapacity - totalAllocated),
            Math.Round(utilizationPercent, 2),
            allocationSummaries,
            constraintSummaries
        );
    }

    public async Task<IEnumerable<CapacityByProductionType>> GetCapacityByTypeAsync(int companyId, DateTime start, DateTime end)
    {
        var allocations = await _context.CapacityAllocations
            .Include(a => a.CapacityPlan)
            .Where(a => a.CapacityPlan.CompanyId == companyId &&
                        a.AllocationType == CapacityAllocationType.Production &&
                        a.StartDate <= end && a.EndDate >= start)
            .ToListAsync();

        var totalHours = allocations.Sum(a => CalculateHoursInRange(a, start, end));

        var grouped = allocations
            .GroupBy(a => a.ProductionType ?? ProductionType.Other)
            .Select(g =>
            {
                var hours = g.Sum(a => CalculateHoursInRange(a, start, end));
                return new CapacityByProductionType(
                    g.Key.ToString(),
                    hours,
                    totalHours > 0 ? Math.Round((hours / totalHours) * 100, 2) : 0,
                    g.Count()
                );
            })
            .OrderByDescending(c => c.AllocatedHours)
            .ToList();

        return grouped;
    }

    #endregion

    #region Utilization Analysis

    public async Task<UtilizationReport> CalculateUtilizationAsync(int companyId, DateTime start, DateTime end, string groupBy = "week")
    {
        var overview = await GetCapacityOverviewAsync(companyId, start, end);

        var breakdowns = new List<UtilizationBreakdown>();
        var currentStart = start;

        while (currentStart < end)
        {
            var periodEnd = groupBy.ToLower() switch
            {
                "day" => currentStart.AddDays(1),
                "week" => currentStart.AddDays(7),
                "month" => currentStart.AddMonths(1),
                _ => currentStart.AddDays(7)
            };
            periodEnd = periodEnd > end ? end : periodEnd;

            var periodOverview = await GetCapacityOverviewAsync(companyId, currentStart, periodEnd);
            breakdowns.Add(new UtilizationBreakdown(
                currentStart,
                periodEnd,
                periodOverview.OverallUtilizationPercent,
                periodOverview.TotalCapacityHours,
                periodOverview.AllocatedHours
            ));

            currentStart = periodEnd;
        }

        return new UtilizationReport(
            start,
            end,
            overview.OverallUtilizationPercent,
            overview.TotalCapacityHours,
            overview.AllocatedHours,
            breakdowns
        );
    }

    public async Task<IEnumerable<EquipmentUtilization>> GetEquipmentUtilizationAsync(int companyId, DateTime start, DateTime end)
    {
        var equipment = await _context.Equipment
            .Where(e => e.CompanyId == companyId && e.IsActive)
            .ToListAsync();

        var results = new List<EquipmentUtilization>();

        foreach (var eq in equipment)
        {
            var detail = await GetEquipmentCapacityAsync(eq.Id, start, end);
            results.Add(new EquipmentUtilization(
                eq.Id,
                eq.Name,
                eq.EquipmentType.ToString(),
                detail.UtilizationPercent,
                detail.TotalCapacityHours,
                detail.AllocatedHours,
                detail.MaintenanceHours
            ));
        }

        return results.OrderByDescending(u => u.UtilizationPercent);
    }

    public async Task<UtilizationTrend> GetUtilizationTrendAsync(int companyId, int periodMonths = 6)
    {
        var endDate = DateTime.UtcNow.Date;
        var startDate = endDate.AddMonths(-periodMonths);

        var monthlyData = new List<MonthlyUtilization>();
        var currentMonth = new DateTime(startDate.Year, startDate.Month, 1);

        while (currentMonth < endDate)
        {
            var monthEnd = currentMonth.AddMonths(1).AddDays(-1);
            if (monthEnd > endDate) monthEnd = endDate;

            var overview = await GetCapacityOverviewAsync(companyId, currentMonth, monthEnd);
            monthlyData.Add(new MonthlyUtilization(
                currentMonth.Year,
                currentMonth.Month,
                overview.OverallUtilizationPercent,
                overview.TotalCapacityHours,
                overview.AllocatedHours
            ));

            currentMonth = currentMonth.AddMonths(1);
        }

        // Calculate trend direction
        decimal trendDirection = 0;
        if (monthlyData.Count >= 2)
        {
            var firstHalf = monthlyData.Take(monthlyData.Count / 2).Average(m => m.UtilizationPercent);
            var secondHalf = monthlyData.Skip(monthlyData.Count / 2).Average(m => m.UtilizationPercent);
            trendDirection = secondHalf - firstHalf;
        }

        var trendDescription = trendDirection switch
        {
            > 5 => "Increasing",
            < -5 => "Decreasing",
            _ => "Stable"
        };

        return new UtilizationTrend(
            startDate,
            endDate,
            monthlyData,
            Math.Round(trendDirection, 2),
            trendDescription
        );
    }

    #endregion

    #region Bottleneck Detection

    public async Task<IEnumerable<Bottleneck>> IdentifyBottlenecksAsync(int companyId, DateTime start, DateTime end, BottleneckSeverity? minSeverity = null)
    {
        var utilizations = await GetEquipmentUtilizationAsync(companyId, start, end);
        var bottlenecks = new List<Bottleneck>();

        foreach (var util in utilizations)
        {
            var severity = GetBottleneckSeverity(util.UtilizationPercent);
            if (minSeverity.HasValue && severity < minSeverity.Value)
                continue;

            if (util.UtilizationPercent >= HighUtilizationThreshold)
            {
                // Get affected production runs
                var affectedRuns = await GetAffectedProductionRunsAsync(util.EquipmentId, start, end);

                bottlenecks.Add(new Bottleneck(
                    util.EquipmentId,
                    util.EquipmentName,
                    util.EquipmentType,
                    severity,
                    util.UtilizationPercent,
                    affectedRuns.Count,
                    TimeSpan.FromHours((double)(util.HoursAllocated - util.HoursAvailable) / Math.Max(1, affectedRuns.Count)),
                    util.HoursAllocated - util.HoursAvailable,
                    $"{util.EquipmentName} is operating at {util.UtilizationPercent:F1}% capacity, potentially causing delays for {affectedRuns.Count} production runs"
                ));
            }
        }

        return bottlenecks.OrderByDescending(b => b.Severity).ThenByDescending(b => b.UtilizationPercent);
    }

    public async Task<BottleneckAnalysis> AnalyzeBottleneckAsync(int equipmentId, DateTime start, DateTime end)
    {
        var equipment = await _context.Equipment.FindAsync(equipmentId)
            ?? throw new InvalidOperationException($"Equipment {equipmentId} not found");

        var detail = await GetEquipmentCapacityAsync(equipmentId, start, end);
        var severity = GetBottleneckSeverity(detail.UtilizationPercent);

        var affectedRuns = await GetAffectedProductionRunsAsync(equipmentId, start, end);
        var affectedRunSummaries = affectedRuns.Select(r => new AffectedRunSummary(
            r.Id,
            r.Name,
            r.ScheduledStartDate,
            TimeSpan.FromHours(2), // Estimated delay
            $"Waiting for {equipment.Name}"
        )).ToList();

        var avgWait = affectedRunSummaries.Any()
            ? TimeSpan.FromTicks((long)affectedRunSummaries.Average(a => a.DelayDuration.Ticks))
            : TimeSpan.Zero;
        var maxWait = affectedRunSummaries.Any()
            ? affectedRunSummaries.Max(a => a.DelayDuration)
            : TimeSpan.Zero;

        var contributingFactors = new List<string>();
        if (detail.UtilizationPercent > 90)
            contributingFactors.Add("Very high demand on this equipment");
        if (detail.MaintenanceHours > detail.TotalCapacityHours * 0.1m)
            contributingFactors.Add("Significant maintenance overhead");
        if (affectedRuns.Count > 5)
            contributingFactors.Add("Multiple production runs competing for time slots");

        return new BottleneckAnalysis(
            equipmentId,
            equipment.Name,
            severity,
            detail.UtilizationPercent,
            affectedRunSummaries,
            avgWait,
            maxWait,
            Math.Max(0, detail.AllocatedHours - detail.TotalCapacityHours),
            contributingFactors
        );
    }

    public Task<IEnumerable<BottleneckResolution>> SuggestResolutionsAsync(Bottleneck bottleneck)
    {
        var resolutions = new List<BottleneckResolution>();

        if (bottleneck.Severity >= BottleneckSeverity.High)
        {
            resolutions.Add(new BottleneckResolution(
                ResolutionType.AddEquipment,
                $"Add another {bottleneck.EquipmentType} to increase capacity",
                50000m, // Estimated cost
                bottleneck.EstimatedLostCapacity * 0.5m, // Capacity gain
                TimeSpan.FromDays(90),
                new List<string> { "Budget approval", "Space availability", "Staff training" },
                90
            ));
        }

        resolutions.Add(new BottleneckResolution(
            ResolutionType.ExtendHours,
            "Extend operating hours by adding a second shift",
            8000m,
            bottleneck.EstimatedLostCapacity * 0.3m,
            TimeSpan.FromDays(14),
            new List<string> { "Staff availability", "Safety approval" },
            75
        ));

        resolutions.Add(new BottleneckResolution(
            ResolutionType.OptimizeSchedule,
            "Optimize production schedule to reduce gaps and conflicts",
            0m,
            bottleneck.EstimatedLostCapacity * 0.15m,
            TimeSpan.FromDays(7),
            new List<string>(),
            60
        ));

        if (bottleneck.Severity >= BottleneckSeverity.Medium)
        {
            resolutions.Add(new BottleneckResolution(
                ResolutionType.ReduceMaintenanceTime,
                "Implement predictive maintenance to reduce downtime",
                5000m,
                bottleneck.EstimatedLostCapacity * 0.1m,
                TimeSpan.FromDays(30),
                new List<string> { "Maintenance team buy-in", "Sensor installation" },
                50
            ));
        }

        return Task.FromResult<IEnumerable<BottleneckResolution>>(
            resolutions.OrderByDescending(r => r.EffectivenessScore));
    }

    #endregion

    #region Capacity Planning

    public async Task<CapacityPlan> CreateCapacityPlanAsync(CreateCapacityPlanDto dto, int companyId, int userId)
    {
        if (dto.PlanPeriodEnd <= dto.PlanPeriodStart)
            throw new ArgumentException("Plan period end must be after start");

        var planType = Enum.Parse<CapacityPlanType>(dto.PlanType, true);

        var plan = new CapacityPlan
        {
            CompanyId = companyId,
            Name = dto.Name,
            Description = dto.Description,
            PlanPeriodStart = dto.PlanPeriodStart,
            PlanPeriodEnd = dto.PlanPeriodEnd,
            PlanType = planType,
            Status = CapacityPlanStatus.Draft,
            TargetProofGallons = dto.TargetProofGallons,
            TargetBottles = dto.TargetBottles,
            TargetBatches = dto.TargetBatches,
            Notes = dto.Notes,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.CapacityPlans.Add(plan);
        await _context.SaveChangesAsync();

        if (dto.Allocations != null)
        {
            foreach (var allocationDto in dto.Allocations)
            {
                var allocation = new CapacityAllocation
                {
                    CapacityPlanId = plan.Id,
                    EquipmentId = allocationDto.EquipmentId,
                    AllocationType = Enum.Parse<CapacityAllocationType>(allocationDto.AllocationType, true),
                    StartDate = allocationDto.StartDate,
                    EndDate = allocationDto.EndDate,
                    HoursAllocated = allocationDto.HoursAllocated,
                    ProductionType = string.IsNullOrEmpty(allocationDto.ProductionType)
                        ? null
                        : Enum.Parse<ProductionType>(allocationDto.ProductionType, true),
                    Notes = allocationDto.Notes,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.CapacityAllocations.Add(allocation);
            }
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation("Created capacity plan {PlanId} for company {CompanyId}", plan.Id, companyId);
        return plan;
    }

    public async Task<CapacityPlan> UpdateCapacityPlanAsync(int planId, UpdateCapacityPlanDto dto, int companyId)
    {
        var plan = await _context.CapacityPlans
            .FirstOrDefaultAsync(p => p.Id == planId && p.CompanyId == companyId)
            ?? throw new InvalidOperationException($"Capacity plan {planId} not found");

        if (plan.Status != CapacityPlanStatus.Draft)
            throw new InvalidOperationException("Only draft plans can be modified");

        if (dto.Name != null) plan.Name = dto.Name;
        if (dto.Description != null) plan.Description = dto.Description;
        if (dto.PlanPeriodStart.HasValue) plan.PlanPeriodStart = dto.PlanPeriodStart.Value;
        if (dto.PlanPeriodEnd.HasValue) plan.PlanPeriodEnd = dto.PlanPeriodEnd.Value;
        if (dto.PlanType != null) plan.PlanType = Enum.Parse<CapacityPlanType>(dto.PlanType, true);
        if (dto.Status != null) plan.Status = Enum.Parse<CapacityPlanStatus>(dto.Status, true);
        if (dto.TargetProofGallons.HasValue) plan.TargetProofGallons = dto.TargetProofGallons;
        if (dto.TargetBottles.HasValue) plan.TargetBottles = dto.TargetBottles;
        if (dto.TargetBatches.HasValue) plan.TargetBatches = dto.TargetBatches;
        if (dto.Notes != null) plan.Notes = dto.Notes;

        plan.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated capacity plan {PlanId}", planId);
        return plan;
    }

    public async Task<CapacityValidation> ValidateCapacityPlanAsync(int planId)
    {
        var plan = await _context.CapacityPlans
            .Include(p => p.Allocations)
            .ThenInclude(a => a.Equipment)
            .FirstOrDefaultAsync(p => p.Id == planId)
            ?? throw new InvalidOperationException($"Capacity plan {planId} not found");

        var issues = new List<ValidationIssue>();
        var warnings = new List<ValidationWarning>();

        // Check date constraints
        if (plan.PlanPeriodEnd <= plan.PlanPeriodStart)
        {
            issues.Add(new ValidationIssue("DATE_INVALID", "Plan end date must be after start date", null, null));
        }

        // Check for overlapping allocations per equipment
        var allocationsByEquipment = plan.Allocations.GroupBy(a => a.EquipmentId);
        foreach (var group in allocationsByEquipment)
        {
            var sorted = group.OrderBy(a => a.StartDate).ToList();
            for (int i = 0; i < sorted.Count - 1; i++)
            {
                if (sorted[i].EndDate > sorted[i + 1].StartDate)
                {
                    issues.Add(new ValidationIssue(
                        "OVERLAP",
                        $"Overlapping allocations for {sorted[i].Equipment.Name}",
                        sorted[i].EquipmentId,
                        sorted[i].Equipment.Name
                    ));
                }
            }
        }

        // Check capacity constraints
        var constraints = await GetActiveConstraintsAsync(plan.CompanyId, plan.PlanPeriodStart, plan.PlanPeriodEnd);
        foreach (var allocation in plan.Allocations)
        {
            var relevantConstraints = constraints.Where(c =>
                c.EquipmentId == null || c.EquipmentId == allocation.EquipmentId);

            foreach (var constraint in relevantConstraints)
            {
                if (constraint.ConstraintType == CapacityConstraintType.MaxHoursPerDay)
                {
                    var days = (allocation.EndDate - allocation.StartDate).Days;
                    var avgHoursPerDay = days > 0 ? allocation.HoursAllocated / days : allocation.HoursAllocated;
                    if (avgHoursPerDay > constraint.ConstraintValue)
                    {
                        warnings.Add(new ValidationWarning(
                            "EXCEEDS_DAILY_HOURS",
                            $"Allocation exceeds max daily hours ({avgHoursPerDay:F1}h > {constraint.ConstraintValue}h)",
                            allocation.EquipmentId,
                            allocation.Equipment.Name
                        ));
                    }
                }
            }
        }

        // Check for high utilization
        var overview = await GetCapacityOverviewAsync(plan.CompanyId, plan.PlanPeriodStart, plan.PlanPeriodEnd);
        if (overview.OverallUtilizationPercent > 90)
        {
            warnings.Add(new ValidationWarning(
                "HIGH_UTILIZATION",
                $"Overall utilization is very high ({overview.OverallUtilizationPercent:F1}%)",
                null,
                null
            ));
        }

        return new CapacityValidation(!issues.Any(), issues, warnings);
    }

    public async Task<IEnumerable<CapacityPlan>> GetCapacityPlansAsync(int companyId, CapacityPlanStatus? status = null, CapacityPlanType? planType = null)
    {
        var query = _context.CapacityPlans
            .Where(p => p.CompanyId == companyId);

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        if (planType.HasValue)
            query = query.Where(p => p.PlanType == planType.Value);

        return await query
            .Include(p => p.Allocations)
            .OrderByDescending(p => p.PlanPeriodStart)
            .ToListAsync();
    }

    public async Task<CapacityPlan?> GetCapacityPlanAsync(int planId, int companyId)
    {
        return await _context.CapacityPlans
            .Include(p => p.Allocations)
            .ThenInclude(a => a.Equipment)
            .FirstOrDefaultAsync(p => p.Id == planId && p.CompanyId == companyId);
    }

    public async Task<CapacityPlan> ActivateCapacityPlanAsync(int planId, int companyId)
    {
        var plan = await _context.CapacityPlans
            .FirstOrDefaultAsync(p => p.Id == planId && p.CompanyId == companyId)
            ?? throw new InvalidOperationException($"Capacity plan {planId} not found");

        if (plan.Status != CapacityPlanStatus.Draft)
            throw new InvalidOperationException("Only draft plans can be activated");

        var validation = await ValidateCapacityPlanAsync(planId);
        if (!validation.IsValid)
            throw new InvalidOperationException($"Plan has validation errors: {string.Join(", ", validation.Issues.Select(i => i.Description))}");

        plan.Status = CapacityPlanStatus.Active;
        plan.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Activated capacity plan {PlanId}", planId);
        return plan;
    }

    public async Task DeleteCapacityPlanAsync(int planId, int companyId)
    {
        var plan = await _context.CapacityPlans
            .FirstOrDefaultAsync(p => p.Id == planId && p.CompanyId == companyId)
            ?? throw new InvalidOperationException($"Capacity plan {planId} not found");

        if (plan.Status == CapacityPlanStatus.Active)
        {
            plan.Status = CapacityPlanStatus.Archived;
            plan.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _context.CapacityPlans.Remove(plan);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Deleted/archived capacity plan {PlanId}", planId);
    }

    #endregion

    #region Forecasting

    public async Task<CapacityForecast> ForecastCapacityAsync(int companyId, int weeksAhead, ForecastMethod method = ForecastMethod.MovingAverage)
    {
        var historicalData = await GetHistoricalUtilizationDataAsync(companyId, 12);
        if (historicalData.Count < MinDataPointsForForecast)
            throw new InvalidOperationException("Insufficient historical data for forecasting");

        var forecasts = new List<WeeklyForecast>();
        var currentDate = DateTime.UtcNow.Date;
        var startOfWeek = currentDate.AddDays(-(int)currentDate.DayOfWeek);

        // Get current total capacity for the company
        var equipment = await _context.Equipment
            .Where(e => e.CompanyId == companyId && e.IsActive)
            .CountAsync();
        var weeklyCapacity = equipment * DefaultDailyOperatingHours * 7;

        for (int i = 0; i < weeksAhead; i++)
        {
            var weekStart = startOfWeek.AddDays(i * 7);
            var weekEnd = weekStart.AddDays(6);

            var prediction = method switch
            {
                ForecastMethod.MovingAverage => CalculateMovingAverage(historicalData, i),
                ForecastMethod.ExponentialSmoothing => CalculateExponentialSmoothing(historicalData, i),
                ForecastMethod.LinearRegression => CalculateLinearRegression(historicalData, i),
                ForecastMethod.SeasonalAdjusted => CalculateSeasonalAdjusted(historicalData, i, weekStart),
                _ => CalculateMovingAverage(historicalData, i)
            };

            var confidenceInterval = 10m + (i * 2); // Increase uncertainty for future weeks
            forecasts.Add(new WeeklyForecast(
                weekStart,
                weekEnd,
                Math.Round(prediction, 2),
                Math.Max(0, Math.Round(prediction - confidenceInterval, 2)),
                Math.Min(100, Math.Round(prediction + confidenceInterval, 2)),
                Math.Round(weeklyCapacity * prediction / 100, 2),
                weeklyCapacity
            ));
        }

        return new CapacityForecast(
            DateTime.UtcNow,
            method,
            forecasts,
            0.85m - (weeksAhead * 0.01m),
            new List<string>
            {
                "Based on historical utilization patterns",
                "Assumes current equipment configuration",
                "Does not account for planned maintenance"
            }
        );
    }

    public async Task<DemandForecast> ForecastDemandAsync(int companyId, int weeksAhead)
    {
        var historicalOrders = await _context.Orders
            .Where(o => o.CompanyId == companyId && o.CreatedAt >= DateTime.UtcNow.AddMonths(-12))
            .GroupBy(o => new { o.CreatedAt.Year, Week = (o.CreatedAt.DayOfYear / 7) })
            .Select(g => new { g.Key.Year, g.Key.Week, Count = g.Count(), TotalQuantity = g.Sum(o => o.Quantity) })
            .ToListAsync();

        var forecasts = new List<WeeklyDemandForecast>();
        var startOfWeek = DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek);

        var avgQuantity = historicalOrders.Any() ? historicalOrders.Average(o => o.TotalQuantity) : 100;
        var avgBatches = historicalOrders.Any() ? (int)historicalOrders.Average(o => o.Count) : 5;

        for (int i = 0; i < weeksAhead; i++)
        {
            var weekStart = startOfWeek.AddDays(i * 7);
            var weekEnd = weekStart.AddDays(6);

            forecasts.Add(new WeeklyDemandForecast(
                weekStart,
                weekEnd,
                Math.Round((decimal)avgQuantity * (1 + (i * 0.02m)), 2),
                avgBatches,
                Math.Round((decimal)avgQuantity * 0.8m, 2),
                Math.Round((decimal)avgQuantity * 1.2m, 2)
            ));
        }

        return new DemandForecast(
            DateTime.UtcNow,
            forecasts,
            0.75m,
            "Historical order patterns"
        );
    }

    public async Task<GapAnalysis> AnalyzeCapacityGapAsync(int companyId, DateTime start, DateTime end)
    {
        var capacityForecast = await ForecastCapacityAsync(companyId, (int)((end - start).Days / 7) + 1);
        var demandForecast = await ForecastDemandAsync(companyId, (int)((end - start).Days / 7) + 1);

        var weeklyGaps = new List<WeeklyGap>();
        decimal totalCapacity = 0;
        decimal totalDemand = 0;

        foreach (var weekCap in capacityForecast.WeeklyForecasts)
        {
            var weekDemand = demandForecast.WeeklyForecasts
                .FirstOrDefault(d => d.WeekStart == weekCap.WeekStart);

            if (weekDemand != null)
            {
                var demandHours = (weekDemand.PredictedBatches * 8); // Assume 8 hours per batch
                var gap = weekCap.AvailableHours - demandHours;

                weeklyGaps.Add(new WeeklyGap(
                    weekCap.WeekStart,
                    weekCap.AvailableHours,
                    demandHours,
                    gap,
                    weekCap.AvailableHours > 0 ? Math.Round((gap / weekCap.AvailableHours) * 100, 2) : 0
                ));

                totalCapacity += weekCap.AvailableHours;
                totalDemand += demandHours;
            }
        }

        var totalGap = totalCapacity - totalDemand;
        var hasShortfall = totalGap < 0;

        var recommendations = new List<string>();
        if (hasShortfall)
        {
            recommendations.Add("Consider adding equipment or extending operating hours");
            recommendations.Add("Review production schedule for optimization opportunities");
        }
        else if (totalGap > totalCapacity * 0.3m)
        {
            recommendations.Add("Significant excess capacity available");
            recommendations.Add("Consider taking on additional orders or scheduling maintenance");
        }

        return new GapAnalysis(
            start,
            end,
            totalCapacity,
            totalDemand,
            totalGap,
            totalCapacity > 0 ? Math.Round((totalGap / totalCapacity) * 100, 2) : 0,
            hasShortfall,
            weeklyGaps,
            recommendations
        );
    }

    #endregion

    #region What-If Scenarios

    public async Task<ScenarioResult> RunScenarioAsync(WhatIfScenarioDto scenario, int companyId)
    {
        _logger.LogInformation("Running what-if scenario '{Name}' for company {CompanyId}", scenario.Name, companyId);

        // Get baseline
        var baselineOverview = await GetCapacityOverviewAsync(companyId, scenario.EvaluationPeriodStart, scenario.EvaluationPeriodEnd);
        var baselineBottlenecks = await IdentifyBottlenecksAsync(companyId, scenario.EvaluationPeriodStart, scenario.EvaluationPeriodEnd);

        // Apply changes and calculate projected impact
        decimal capacityChange = 0;
        decimal costImpact = 0;

        foreach (var change in scenario.Changes)
        {
            switch (change.Type)
            {
                case ScenarioChangeType.AddEquipment:
                    var hoursPerDay = change.Parameters.TryGetValue("hoursPerDay", out var hpd) ? Convert.ToDecimal(hpd) : DefaultDailyOperatingHours;
                    var days = (scenario.EvaluationPeriodEnd - scenario.EvaluationPeriodStart).Days + 1;
                    capacityChange += hoursPerDay * days;
                    costImpact += change.Parameters.TryGetValue("cost", out var cost) ? Convert.ToDecimal(cost) : 50000m;
                    break;

                case ScenarioChangeType.RemoveEquipment:
                    if (change.EquipmentId.HasValue)
                    {
                        var equipDetail = await GetEquipmentCapacityAsync(change.EquipmentId.Value, scenario.EvaluationPeriodStart, scenario.EvaluationPeriodEnd);
                        capacityChange -= equipDetail.TotalCapacityHours;
                    }
                    break;

                case ScenarioChangeType.ChangeOperatingHours:
                    var hoursDelta = change.Parameters.TryGetValue("hoursDelta", out var hd) ? Convert.ToDecimal(hd) : 0;
                    var equipCount = await _context.Equipment.CountAsync(e => e.CompanyId == companyId && e.IsActive);
                    capacityChange += hoursDelta * equipCount * ((scenario.EvaluationPeriodEnd - scenario.EvaluationPeriodStart).Days + 1);
                    costImpact += hoursDelta > 0 ? 8000m : -2000m;
                    break;
            }
        }

        var projectedCapacity = new CapacityOverview(
            baselineOverview.TotalEquipmentCount + scenario.Changes.Count(c => c.Type == ScenarioChangeType.AddEquipment) - scenario.Changes.Count(c => c.Type == ScenarioChangeType.RemoveEquipment),
            baselineOverview.TotalCapacityHours + capacityChange,
            baselineOverview.AllocatedHours,
            baselineOverview.AvailableHours + capacityChange,
            baselineOverview.TotalCapacityHours + capacityChange > 0
                ? Math.Round((baselineOverview.AllocatedHours / (baselineOverview.TotalCapacityHours + capacityChange)) * 100, 2)
                : 0,
            baselineOverview.EquipmentSummaries,
            baselineOverview.Alerts
        );

        var capacityChangePercent = baselineOverview.TotalCapacityHours > 0
            ? Math.Round((capacityChange / baselineOverview.TotalCapacityHours) * 100, 2)
            : 0;

        var summary = capacityChange > 0
            ? $"Adding {capacityChange:F0} hours of capacity ({capacityChangePercent:F1}% increase)"
            : capacityChange < 0
                ? $"Reducing {Math.Abs(capacityChange):F0} hours of capacity ({Math.Abs(capacityChangePercent):F1}% decrease)"
                : "No net change in capacity";

        return new ScenarioResult(
            0, // Scenario not persisted
            scenario.Name,
            projectedCapacity,
            baselineBottlenecks.ToList(),
            capacityChangePercent,
            costImpact,
            summary
        );
    }

    public Task<IEnumerable<ScenarioComparison>> CompareScenarioResultsAsync(List<int> scenarioIds)
    {
        // Since scenarios aren't persisted, return empty for now
        return Task.FromResult<IEnumerable<ScenarioComparison>>(new List<ScenarioComparison>());
    }

    #endregion

    #region Snapshots

    public async Task CaptureCapacitySnapshotAsync(int companyId, DateTime snapshotDate)
    {
        var equipment = await _context.Equipment
            .Where(e => e.CompanyId == companyId && e.IsActive)
            .ToListAsync();

        var snapshots = new List<CapacitySnapshot>();

        foreach (var eq in equipment)
        {
            var detail = await GetEquipmentCapacityAsync(eq.Id, snapshotDate.Date, snapshotDate.Date.AddDays(1));

            snapshots.Add(new CapacitySnapshot
            {
                CompanyId = companyId,
                SnapshotDate = snapshotDate.Date,
                EquipmentId = eq.Id,
                TotalCapacityHours = detail.TotalCapacityHours,
                AllocatedHours = detail.AllocatedHours,
                MaintenanceHours = detail.MaintenanceHours,
                UtilizationPercent = detail.UtilizationPercent,
                CreatedAt = DateTime.UtcNow
            });
        }

        _context.CapacitySnapshots.AddRange(snapshots);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Captured {Count} capacity snapshots for company {CompanyId} on {Date}",
            snapshots.Count, companyId, snapshotDate.Date);
    }

    public async Task<IEnumerable<CapacitySnapshot>> GetHistoricalSnapshotsAsync(int companyId, DateTime start, DateTime end)
    {
        return await _context.CapacitySnapshots
            .Include(s => s.Equipment)
            .Where(s => s.CompanyId == companyId &&
                        s.SnapshotDate >= start &&
                        s.SnapshotDate <= end)
            .OrderBy(s => s.SnapshotDate)
            .ThenBy(s => s.EquipmentId)
            .ToListAsync();
    }

    #endregion

    #region Constraint Management

    public async Task<IEnumerable<CapacityConstraint>> GetConstraintsAsync(int companyId, int? equipmentId = null, bool activeOnly = true)
    {
        var query = _context.CapacityConstraints
            .Include(c => c.Equipment)
            .Where(c => c.CompanyId == companyId);

        if (equipmentId.HasValue)
            query = query.Where(c => c.EquipmentId == equipmentId || c.EquipmentId == null);

        if (activeOnly)
            query = query.Where(c => c.IsActive);

        return await query.OrderBy(c => c.ConstraintType).ToListAsync();
    }

    public async Task<CapacityConstraint> CreateConstraintAsync(CreateConstraintDto dto, int companyId, int userId)
    {
        var constraint = new CapacityConstraint
        {
            CompanyId = companyId,
            EquipmentId = dto.EquipmentId,
            ConstraintType = Enum.Parse<CapacityConstraintType>(dto.ConstraintType, true),
            ConstraintValue = dto.ConstraintValue,
            EffectiveFrom = dto.EffectiveFrom,
            EffectiveTo = dto.EffectiveTo,
            Reason = dto.Reason,
            IsActive = true,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.CapacityConstraints.Add(constraint);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created capacity constraint {ConstraintId} for company {CompanyId}", constraint.Id, companyId);
        return constraint;
    }

    public async Task<CapacityConstraint> UpdateConstraintAsync(int constraintId, UpdateConstraintDto dto, int companyId)
    {
        var constraint = await _context.CapacityConstraints
            .FirstOrDefaultAsync(c => c.Id == constraintId && c.CompanyId == companyId)
            ?? throw new InvalidOperationException($"Constraint {constraintId} not found");

        if (dto.ConstraintValue.HasValue) constraint.ConstraintValue = dto.ConstraintValue.Value;
        if (dto.EffectiveFrom.HasValue) constraint.EffectiveFrom = dto.EffectiveFrom.Value;
        if (dto.EffectiveTo.HasValue) constraint.EffectiveTo = dto.EffectiveTo;
        if (dto.Reason != null) constraint.Reason = dto.Reason;
        if (dto.IsActive.HasValue) constraint.IsActive = dto.IsActive.Value;

        constraint.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return constraint;
    }

    public async Task DeleteConstraintAsync(int constraintId, int companyId)
    {
        var constraint = await _context.CapacityConstraints
            .FirstOrDefaultAsync(c => c.Id == constraintId && c.CompanyId == companyId)
            ?? throw new InvalidOperationException($"Constraint {constraintId} not found");

        constraint.IsActive = false;
        constraint.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deactivated capacity constraint {ConstraintId}", constraintId);
    }

    #endregion

    #region Private Helper Methods

    private async Task<List<CapacityConstraint>> GetActiveConstraintsAsync(int companyId, DateTime start, DateTime end)
    {
        return await _context.CapacityConstraints
            .Where(c => c.CompanyId == companyId &&
                        c.IsActive &&
                        c.EffectiveFrom <= end &&
                        (c.EffectiveTo == null || c.EffectiveTo >= start))
            .ToListAsync();
    }

    private decimal GetDailyHoursForEquipment(int equipmentId, List<CapacityConstraint> constraints)
    {
        var maxHoursConstraint = constraints
            .Where(c => (c.EquipmentId == null || c.EquipmentId == equipmentId) &&
                        c.ConstraintType == CapacityConstraintType.MaxHoursPerDay)
            .MinBy(c => c.ConstraintValue);

        return maxHoursConstraint?.ConstraintValue ?? DefaultDailyOperatingHours;
    }

    private static decimal CalculateAllocatedHours(List<CapacityAllocation> allocations, DateTime start, DateTime end)
    {
        return allocations.Sum(a => CalculateHoursInRange(a, start, end));
    }

    private static decimal CalculateHoursInRange(CapacityAllocation allocation, DateTime start, DateTime end)
    {
        var overlapStart = allocation.StartDate > start ? allocation.StartDate : start;
        var overlapEnd = allocation.EndDate < end ? allocation.EndDate : end;

        if (overlapStart >= overlapEnd) return 0;

        var totalDays = (allocation.EndDate - allocation.StartDate).Days;
        var overlapDays = (overlapEnd - overlapStart).Days;

        if (totalDays <= 0) return allocation.HoursAllocated;

        return allocation.HoursAllocated * ((decimal)overlapDays / totalDays);
    }

    private static BottleneckSeverity GetBottleneckSeverity(decimal utilizationPercent)
    {
        return utilizationPercent switch
        {
            >= 95 => BottleneckSeverity.Critical,
            >= 90 => BottleneckSeverity.High,
            >= 85 => BottleneckSeverity.Medium,
            _ => BottleneckSeverity.Low
        };
    }

    private async Task<List<ProductionRun>> GetAffectedProductionRunsAsync(int equipmentId, DateTime start, DateTime end)
    {
        return await _context.ProductionRuns
            .Where(r => r.EquipmentBookings.Any(b => b.EquipmentId == equipmentId) &&
                        r.ScheduledStartDate <= end && r.ScheduledEndDate >= start &&
                        r.Status != ProductionRunStatus.Completed &&
                        r.Status != ProductionRunStatus.Cancelled)
            .ToListAsync();
    }

    private async Task<List<decimal>> GetHistoricalUtilizationDataAsync(int companyId, int weeksBack)
    {
        var snapshots = await _context.CapacitySnapshots
            .Where(s => s.CompanyId == companyId &&
                        s.SnapshotDate >= DateTime.UtcNow.AddDays(-weeksBack * 7))
            .GroupBy(s => new { Year = s.SnapshotDate.Year, Week = s.SnapshotDate.DayOfYear / 7 })
            .Select(g => g.Average(s => s.UtilizationPercent))
            .ToListAsync();

        // If not enough historical data, generate synthetic data
        if (snapshots.Count < MinDataPointsForForecast)
        {
            var syntheticData = new List<decimal>();
            for (int i = 0; i < 12; i++)
            {
                syntheticData.Add(65 + (i % 4) * 5);
            }
            return syntheticData;
        }

        return snapshots;
    }

    private static decimal CalculateMovingAverage(List<decimal> data, int periodsAhead)
    {
        var windowSize = Math.Min(4, data.Count);
        var recentData = data.TakeLast(windowSize).ToList();
        return recentData.Average();
    }

    private static decimal CalculateExponentialSmoothing(List<decimal> data, int periodsAhead)
    {
        const decimal alpha = 0.3m;
        var smoothed = data.First();

        foreach (var value in data.Skip(1))
        {
            smoothed = alpha * value + (1 - alpha) * smoothed;
        }

        return smoothed;
    }

    private static decimal CalculateLinearRegression(List<decimal> data, int periodsAhead)
    {
        var n = data.Count;
        var xSum = (n * (n + 1)) / 2m;
        var xSquaredSum = (n * (n + 1) * (2 * n + 1)) / 6m;
        var ySum = data.Sum();
        var xySum = data.Select((y, i) => y * (i + 1)).Sum();

        var slope = (n * xySum - xSum * ySum) / (n * xSquaredSum - xSum * xSum);
        var intercept = (ySum - slope * xSum) / n;

        return intercept + slope * (n + 1 + periodsAhead);
    }

    private static decimal CalculateSeasonalAdjusted(List<decimal> data, int periodsAhead, DateTime weekStart)
    {
        var baseValue = CalculateMovingAverage(data, periodsAhead);

        // Simple seasonal adjustment based on month
        var seasonalFactor = weekStart.Month switch
        {
            >= 1 and <= 2 => 0.9m,  // Slow start to year
            >= 3 and <= 5 => 1.1m,  // Spring pickup
            >= 6 and <= 8 => 1.0m,  // Summer baseline
            >= 9 and <= 11 => 1.15m, // Fall busy season
            12 => 0.85m,             // Holiday slowdown
            _ => 1.0m
        };

        return baseValue * seasonalFactor;
    }

    #endregion
}
