using Caskr.server.Models.Production;

namespace Caskr.server.Services.Production;

/// <summary>
/// Service for analyzing production capacity, identifying bottlenecks,
/// forecasting utilization, and supporting what-if scenario planning.
/// </summary>
public interface ICapacityAnalysisService
{
    #region Current Capacity Analysis

    /// <summary>
    /// Get an overview of capacity for a company over a date range.
    /// </summary>
    Task<CapacityOverview> GetCapacityOverviewAsync(int companyId, DateTime start, DateTime end);

    /// <summary>
    /// Get detailed capacity information for a specific piece of equipment.
    /// </summary>
    Task<EquipmentCapacityDetail> GetEquipmentCapacityAsync(int equipmentId, DateTime start, DateTime end);

    /// <summary>
    /// Get capacity breakdown by production type.
    /// </summary>
    Task<IEnumerable<CapacityByProductionType>> GetCapacityByTypeAsync(int companyId, DateTime start, DateTime end);

    #endregion

    #region Utilization Analysis

    /// <summary>
    /// Calculate utilization report for a company.
    /// </summary>
    Task<UtilizationReport> CalculateUtilizationAsync(int companyId, DateTime start, DateTime end, string groupBy = "week");

    /// <summary>
    /// Get per-equipment utilization.
    /// </summary>
    Task<IEnumerable<EquipmentUtilization>> GetEquipmentUtilizationAsync(int companyId, DateTime start, DateTime end);

    /// <summary>
    /// Get utilization trend over time.
    /// </summary>
    Task<UtilizationTrend> GetUtilizationTrendAsync(int companyId, int periodMonths = 6);

    #endregion

    #region Bottleneck Detection

    /// <summary>
    /// Identify bottlenecks in the production process.
    /// </summary>
    Task<IEnumerable<Bottleneck>> IdentifyBottlenecksAsync(int companyId, DateTime start, DateTime end, BottleneckSeverity? minSeverity = null);

    /// <summary>
    /// Get detailed analysis of a specific bottleneck.
    /// </summary>
    Task<BottleneckAnalysis> AnalyzeBottleneckAsync(int equipmentId, DateTime start, DateTime end);

    /// <summary>
    /// Suggest resolutions for a bottleneck.
    /// </summary>
    Task<IEnumerable<BottleneckResolution>> SuggestResolutionsAsync(Bottleneck bottleneck);

    #endregion

    #region Capacity Planning

    /// <summary>
    /// Create a new capacity plan.
    /// </summary>
    Task<CapacityPlan> CreateCapacityPlanAsync(CreateCapacityPlanDto dto, int companyId, int userId);

    /// <summary>
    /// Update an existing capacity plan.
    /// </summary>
    Task<CapacityPlan> UpdateCapacityPlanAsync(int planId, UpdateCapacityPlanDto dto, int companyId);

    /// <summary>
    /// Validate a capacity plan against constraints.
    /// </summary>
    Task<CapacityValidation> ValidateCapacityPlanAsync(int planId);

    /// <summary>
    /// Get capacity plans for a company.
    /// </summary>
    Task<IEnumerable<CapacityPlan>> GetCapacityPlansAsync(int companyId, CapacityPlanStatus? status = null, CapacityPlanType? planType = null);

    /// <summary>
    /// Get a capacity plan by ID.
    /// </summary>
    Task<CapacityPlan?> GetCapacityPlanAsync(int planId, int companyId);

    /// <summary>
    /// Activate a draft capacity plan.
    /// </summary>
    Task<CapacityPlan> ActivateCapacityPlanAsync(int planId, int companyId);

    /// <summary>
    /// Delete or archive a capacity plan.
    /// </summary>
    Task DeleteCapacityPlanAsync(int planId, int companyId);

    #endregion

    #region Forecasting

    /// <summary>
    /// Forecast capacity utilization.
    /// </summary>
    Task<CapacityForecast> ForecastCapacityAsync(int companyId, int weeksAhead, ForecastMethod method = ForecastMethod.MovingAverage);

    /// <summary>
    /// Forecast demand based on historical data.
    /// </summary>
    Task<DemandForecast> ForecastDemandAsync(int companyId, int weeksAhead);

    /// <summary>
    /// Analyze gap between capacity and demand.
    /// </summary>
    Task<GapAnalysis> AnalyzeCapacityGapAsync(int companyId, DateTime start, DateTime end);

    #endregion

    #region What-If Scenarios

    /// <summary>
    /// Run a what-if scenario and get projected results.
    /// </summary>
    Task<ScenarioResult> RunScenarioAsync(WhatIfScenarioDto scenario, int companyId);

    /// <summary>
    /// Compare multiple scenario results.
    /// </summary>
    Task<IEnumerable<ScenarioComparison>> CompareScenarioResultsAsync(List<int> scenarioIds);

    #endregion

    #region Snapshots

    /// <summary>
    /// Capture a capacity snapshot for historical tracking.
    /// </summary>
    Task CaptureCapacitySnapshotAsync(int companyId, DateTime snapshotDate);

    /// <summary>
    /// Get historical snapshots.
    /// </summary>
    Task<IEnumerable<CapacitySnapshot>> GetHistoricalSnapshotsAsync(int companyId, DateTime start, DateTime end);

    #endregion

    #region Constraint Management

    /// <summary>
    /// Get capacity constraints for a company.
    /// </summary>
    Task<IEnumerable<CapacityConstraint>> GetConstraintsAsync(int companyId, int? equipmentId = null, bool activeOnly = true);

    /// <summary>
    /// Create a capacity constraint.
    /// </summary>
    Task<CapacityConstraint> CreateConstraintAsync(CreateConstraintDto dto, int companyId, int userId);

    /// <summary>
    /// Update a capacity constraint.
    /// </summary>
    Task<CapacityConstraint> UpdateConstraintAsync(int constraintId, UpdateConstraintDto dto, int companyId);

    /// <summary>
    /// Deactivate a capacity constraint.
    /// </summary>
    Task DeleteConstraintAsync(int constraintId, int companyId);

    #endregion
}
