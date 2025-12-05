namespace Caskr.server.Services.Production;

#region Overview DTOs

/// <summary>
/// Overview of capacity for a company over a date range.
/// </summary>
public record CapacityOverview(
    int TotalEquipmentCount,
    decimal TotalCapacityHours,
    decimal AllocatedHours,
    decimal AvailableHours,
    decimal OverallUtilizationPercent,
    List<EquipmentCapacitySummary> EquipmentSummaries,
    List<CapacityAlert> Alerts
);

/// <summary>
/// Summary of capacity for a single piece of equipment.
/// </summary>
public record EquipmentCapacitySummary(
    int EquipmentId,
    string EquipmentName,
    string EquipmentType,
    decimal TotalCapacityHours,
    decimal AllocatedHours,
    decimal AvailableHours,
    decimal UtilizationPercent
);

/// <summary>
/// Detailed capacity information for a single piece of equipment.
/// </summary>
public record EquipmentCapacityDetail(
    int EquipmentId,
    string EquipmentName,
    string EquipmentType,
    decimal TotalCapacityHours,
    decimal AllocatedHours,
    decimal MaintenanceHours,
    decimal BufferHours,
    decimal AvailableHours,
    decimal UtilizationPercent,
    List<AllocationSummary> Allocations,
    List<ConstraintSummary> ActiveConstraints
);

/// <summary>
/// Summary of an allocation.
/// </summary>
public record AllocationSummary(
    int AllocationId,
    string AllocationType,
    DateTime StartDate,
    DateTime EndDate,
    decimal HoursAllocated,
    string? ProductionType,
    string? Notes
);

/// <summary>
/// Summary of a constraint.
/// </summary>
public record ConstraintSummary(
    int ConstraintId,
    string ConstraintType,
    decimal ConstraintValue,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    string? Reason
);

/// <summary>
/// Capacity breakdown by production type.
/// </summary>
public record CapacityByProductionType(
    string ProductionType,
    decimal AllocatedHours,
    decimal PercentOfTotal,
    int AllocationCount
);

/// <summary>
/// Alert about capacity issues.
/// </summary>
public record CapacityAlert(
    CapacityAlertSeverity Severity,
    string Title,
    string Description,
    int? EquipmentId,
    string? EquipmentName
);

/// <summary>
/// Severity level for capacity alerts.
/// </summary>
public enum CapacityAlertSeverity
{
    Info,
    Warning,
    Critical
}

#endregion

#region Utilization DTOs

/// <summary>
/// Report of utilization across a company.
/// </summary>
public record UtilizationReport(
    DateTime PeriodStart,
    DateTime PeriodEnd,
    decimal OverallUtilizationPercent,
    decimal TotalHoursAvailable,
    decimal TotalHoursUsed,
    List<UtilizationBreakdown> Breakdowns
);

/// <summary>
/// Utilization breakdown for a period.
/// </summary>
public record UtilizationBreakdown(
    DateTime PeriodStart,
    DateTime PeriodEnd,
    decimal UtilizationPercent,
    decimal HoursAvailable,
    decimal HoursUsed
);

/// <summary>
/// Utilization for a specific piece of equipment.
/// </summary>
public record EquipmentUtilization(
    int EquipmentId,
    string EquipmentName,
    string EquipmentType,
    decimal UtilizationPercent,
    decimal HoursAvailable,
    decimal HoursAllocated,
    decimal HoursInMaintenance
);

/// <summary>
/// Trend of utilization over time.
/// </summary>
public record UtilizationTrend(
    DateTime PeriodStart,
    DateTime PeriodEnd,
    List<MonthlyUtilization> MonthlyData,
    decimal TrendDirection,
    string TrendDescription
);

/// <summary>
/// Monthly utilization data.
/// </summary>
public record MonthlyUtilization(
    int Year,
    int Month,
    decimal UtilizationPercent,
    decimal HoursAvailable,
    decimal HoursUsed
);

#endregion

#region Bottleneck DTOs

/// <summary>
/// Represents a production bottleneck.
/// </summary>
public record Bottleneck(
    int EquipmentId,
    string EquipmentName,
    string EquipmentType,
    BottleneckSeverity Severity,
    decimal UtilizationPercent,
    int AffectedProductionRuns,
    TimeSpan AverageWaitTime,
    decimal EstimatedLostCapacity,
    string Description
);

/// <summary>
/// Severity of a bottleneck.
/// </summary>
public enum BottleneckSeverity
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Detailed analysis of a bottleneck.
/// </summary>
public record BottleneckAnalysis(
    int EquipmentId,
    string EquipmentName,
    BottleneckSeverity Severity,
    decimal UtilizationPercent,
    List<AffectedRunSummary> AffectedRuns,
    TimeSpan AverageWaitTime,
    TimeSpan MaxWaitTime,
    decimal EstimatedLostCapacity,
    List<string> ContributingFactors
);

/// <summary>
/// Summary of a production run affected by a bottleneck.
/// </summary>
public record AffectedRunSummary(
    int ProductionRunId,
    string ProductionRunName,
    DateTime ScheduledStart,
    TimeSpan DelayDuration,
    string ImpactDescription
);

/// <summary>
/// Suggested resolution for a bottleneck.
/// </summary>
public record BottleneckResolution(
    ResolutionType Type,
    string Description,
    decimal EstimatedCostImpact,
    decimal EstimatedCapacityGain,
    TimeSpan EstimatedImplementationTime,
    List<string> Prerequisites,
    int EffectivenessScore
);

/// <summary>
/// Type of resolution for a bottleneck.
/// </summary>
public enum ResolutionType
{
    AddEquipment,
    ExtendHours,
    OptimizeSchedule,
    ReduceMaintenanceTime,
    AddShift,
    Outsource
}

#endregion

#region Capacity Plan DTOs

/// <summary>
/// DTO for creating a capacity plan.
/// </summary>
public record CreateCapacityPlanDto(
    string Name,
    string? Description,
    DateTime PlanPeriodStart,
    DateTime PlanPeriodEnd,
    string PlanType,
    decimal? TargetProofGallons,
    int? TargetBottles,
    int? TargetBatches,
    string? Notes,
    List<CreateCapacityAllocationDto>? Allocations
);

/// <summary>
/// DTO for updating a capacity plan.
/// </summary>
public record UpdateCapacityPlanDto(
    string? Name,
    string? Description,
    DateTime? PlanPeriodStart,
    DateTime? PlanPeriodEnd,
    string? PlanType,
    string? Status,
    decimal? TargetProofGallons,
    int? TargetBottles,
    int? TargetBatches,
    string? Notes
);

/// <summary>
/// DTO for creating a capacity allocation.
/// </summary>
public record CreateCapacityAllocationDto(
    int EquipmentId,
    string AllocationType,
    DateTime StartDate,
    DateTime EndDate,
    decimal HoursAllocated,
    string? ProductionType,
    string? Notes
);

/// <summary>
/// Validation result for a capacity plan.
/// </summary>
public record CapacityValidation(
    bool IsValid,
    List<ValidationIssue> Issues,
    List<ValidationWarning> Warnings
);

/// <summary>
/// A validation issue that prevents plan activation.
/// </summary>
public record ValidationIssue(
    string Code,
    string Description,
    int? EquipmentId,
    string? EquipmentName
);

/// <summary>
/// A validation warning that should be reviewed.
/// </summary>
public record ValidationWarning(
    string Code,
    string Description,
    int? EquipmentId,
    string? EquipmentName
);

#endregion

#region Forecast DTOs

/// <summary>
/// Forecast of capacity.
/// </summary>
public record CapacityForecast(
    DateTime GeneratedAt,
    ForecastMethod Method,
    List<WeeklyForecast> WeeklyForecasts,
    decimal ConfidenceLevel,
    List<string> Assumptions
);

/// <summary>
/// Method used for forecasting.
/// </summary>
public enum ForecastMethod
{
    MovingAverage,
    ExponentialSmoothing,
    LinearRegression,
    SeasonalAdjusted
}

/// <summary>
/// Weekly forecast data.
/// </summary>
public record WeeklyForecast(
    DateTime WeekStart,
    DateTime WeekEnd,
    decimal PredictedUtilization,
    decimal LowerBound,
    decimal UpperBound,
    decimal PredictedHoursUsed,
    decimal AvailableHours
);

/// <summary>
/// Forecast of demand.
/// </summary>
public record DemandForecast(
    DateTime GeneratedAt,
    List<WeeklyDemandForecast> WeeklyForecasts,
    decimal ConfidenceLevel,
    string BasedOn
);

/// <summary>
/// Weekly demand forecast data.
/// </summary>
public record WeeklyDemandForecast(
    DateTime WeekStart,
    DateTime WeekEnd,
    decimal PredictedProofGallons,
    int PredictedBatches,
    decimal LowerBound,
    decimal UpperBound
);

/// <summary>
/// Analysis of gap between capacity and demand.
/// </summary>
public record GapAnalysis(
    DateTime PeriodStart,
    DateTime PeriodEnd,
    decimal CapacityHours,
    decimal DemandHours,
    decimal GapHours,
    decimal GapPercent,
    bool HasCapacityShortfall,
    List<WeeklyGap> WeeklyBreakdown,
    List<string> Recommendations
);

/// <summary>
/// Weekly gap analysis.
/// </summary>
public record WeeklyGap(
    DateTime WeekStart,
    decimal CapacityHours,
    decimal DemandHours,
    decimal GapHours,
    decimal GapPercent
);

#endregion

#region Scenario DTOs

/// <summary>
/// DTO for creating a what-if scenario.
/// </summary>
public record WhatIfScenarioDto(
    string Name,
    List<ScenarioChange> Changes,
    DateTime EvaluationPeriodStart,
    DateTime EvaluationPeriodEnd
);

/// <summary>
/// A change to apply in a what-if scenario.
/// </summary>
public record ScenarioChange(
    ScenarioChangeType Type,
    int? EquipmentId,
    Dictionary<string, object> Parameters
);

/// <summary>
/// Type of change in a scenario.
/// </summary>
public enum ScenarioChangeType
{
    AddEquipment,
    RemoveEquipment,
    ChangeCapacity,
    AddConstraint,
    RemoveConstraint,
    ChangeOperatingHours,
    AddProductionRun
}

/// <summary>
/// Result of running a what-if scenario.
/// </summary>
public record ScenarioResult(
    int ScenarioId,
    string ScenarioName,
    CapacityOverview ProjectedCapacity,
    List<Bottleneck> ProjectedBottlenecks,
    decimal CapacityChangePercent,
    decimal CostImpact,
    string Summary
);

/// <summary>
/// Comparison of multiple scenarios.
/// </summary>
public record ScenarioComparison(
    int ScenarioId,
    string ScenarioName,
    decimal UtilizationChange,
    decimal CapacityChange,
    decimal CostImpact,
    int BottlenecksResolved,
    int NewBottlenecks,
    int Rank,
    string Recommendation
);

#endregion

#region Constraint DTOs

/// <summary>
/// DTO for creating a capacity constraint.
/// </summary>
public record CreateConstraintDto(
    int? EquipmentId,
    string ConstraintType,
    decimal ConstraintValue,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    string? Reason
);

/// <summary>
/// DTO for updating a capacity constraint.
/// </summary>
public record UpdateConstraintDto(
    decimal? ConstraintValue,
    DateTime? EffectiveFrom,
    DateTime? EffectiveTo,
    string? Reason,
    bool? IsActive
);

#endregion
