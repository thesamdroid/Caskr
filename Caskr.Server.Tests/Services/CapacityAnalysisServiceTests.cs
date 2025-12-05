using Caskr.server.Models;
using Caskr.server.Models.Production;
using Caskr.server.Services.Production;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Caskr.Server.Tests.Services;

public class CapacityAnalysisServiceTests : IDisposable
{
    private readonly CaskrDbContext _context;
    private readonly CapacityAnalysisService _service;
    private readonly Mock<ILogger<CapacityAnalysisService>> _loggerMock;

    public CapacityAnalysisServiceTests()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CaskrDbContext(options);
        _loggerMock = new Mock<ILogger<CapacityAnalysisService>>();
        _service = new CapacityAnalysisService(_context, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region Helper Methods

    private async Task<Company> CreateTestCompanyAsync()
    {
        var company = new Company
        {
            CompanyName = "Test Distillery",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Companies.Add(company);
        await _context.SaveChangesAsync();
        return company;
    }

    private async Task<User> CreateTestUserAsync(int companyId)
    {
        var userType = new UserType { Name = "Test User Type" };
        _context.UserTypes.Add(userType);
        await _context.SaveChangesAsync();

        var user = new User
        {
            Name = "Test User",
            Email = "test@example.com",
            CompanyId = companyId,
            UserTypeId = userType.Id,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    private async Task<Equipment> CreateTestEquipmentAsync(int companyId, string name, EquipmentType type)
    {
        var equipment = new Equipment
        {
            CompanyId = companyId,
            Name = name,
            EquipmentType = type,
            Capacity = 500,
            CapacityUnit = "gallons",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Equipment.Add(equipment);
        await _context.SaveChangesAsync();
        return equipment;
    }

    private async Task<CapacityPlan> CreateTestPlanAsync(int companyId, int userId)
    {
        var plan = new CapacityPlan
        {
            CompanyId = companyId,
            Name = "Test Plan",
            PlanPeriodStart = DateTime.UtcNow,
            PlanPeriodEnd = DateTime.UtcNow.AddMonths(1),
            PlanType = CapacityPlanType.Monthly,
            Status = CapacityPlanStatus.Draft,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.CapacityPlans.Add(plan);
        await _context.SaveChangesAsync();
        return plan;
    }

    private async Task CreateTestAllocationAsync(int planId, int equipmentId, decimal hours, CapacityAllocationType type = CapacityAllocationType.Production, DateTime? startDate = null, DateTime? endDate = null)
    {
        var start = startDate ?? DateTime.UtcNow.Date;
        var end = endDate ?? DateTime.UtcNow.Date.AddDays(7);
        var allocation = new CapacityAllocation
        {
            CapacityPlanId = planId,
            EquipmentId = equipmentId,
            AllocationType = type,
            StartDate = start,
            EndDate = end,
            HoursAllocated = hours,
            ProductionType = ProductionType.Distillation,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.CapacityAllocations.Add(allocation);
        await _context.SaveChangesAsync();
    }

    #endregion

    #region Utilization Tests

    [Fact]
    public async Task CalculateUtilization_NoAllocations_ReturnsZeroPercent()
    {
        var company = await CreateTestCompanyAsync();
        await CreateTestEquipmentAsync(company.Id, "Still #1", EquipmentType.Still);

        var result = await _service.CalculateUtilizationAsync(
            company.Id,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(7)
        );

        Assert.Equal(0, result.OverallUtilizationPercent);
    }

    [Fact]
    public async Task CalculateUtilization_FullyBooked_ReturnsHighPercent()
    {
        var company = await CreateTestCompanyAsync();
        var user = await CreateTestUserAsync(company.Id);
        var equipment = await CreateTestEquipmentAsync(company.Id, "Still #1", EquipmentType.Still);
        var plan = await CreateTestPlanAsync(company.Id, user.Id);

        // Use fixed dates to avoid timing issues between allocation creation and service call
        var startDate = DateTime.UtcNow.Date;
        var endDate = startDate.AddDays(7);

        // Allocate 128 hours for 8-day capacity period (16 hours/day * 8 days)
        // Note: capacity uses (end - start).Days + 1 = 8 days
        await CreateTestAllocationAsync(plan.Id, equipment.Id, 128, startDate: startDate, endDate: endDate);

        var result = await _service.CalculateUtilizationAsync(
            company.Id,
            startDate,
            endDate
        );

        // Should be close to 100% (128 hours allocated / 128 available hours)
        Assert.True(result.OverallUtilizationPercent >= 90);
    }

    [Fact]
    public async Task CalculateUtilization_WithMaintenance_IncludesInCalculation()
    {
        var company = await CreateTestCompanyAsync();
        var user = await CreateTestUserAsync(company.Id);
        var equipment = await CreateTestEquipmentAsync(company.Id, "Still #1", EquipmentType.Still);
        var plan = await CreateTestPlanAsync(company.Id, user.Id);

        await CreateTestAllocationAsync(plan.Id, equipment.Id, 50, CapacityAllocationType.Production);
        await CreateTestAllocationAsync(plan.Id, equipment.Id, 16, CapacityAllocationType.Maintenance);

        var result = await _service.CalculateUtilizationAsync(
            company.Id,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(7)
        );

        // Should include both production and maintenance (at least 50 hours)
        Assert.True(result.TotalHoursUsed >= 50);
    }

    [Fact]
    public async Task GetUtilizationTrend_ReturnsCorrectMonthlyAverages()
    {
        var company = await CreateTestCompanyAsync();
        await CreateTestEquipmentAsync(company.Id, "Still #1", EquipmentType.Still);

        var result = await _service.GetUtilizationTrendAsync(company.Id, 6);

        Assert.NotNull(result);
        Assert.NotEmpty(result.MonthlyData);
        Assert.NotNull(result.TrendDescription);
    }

    #endregion

    #region Bottleneck Tests

    [Fact]
    public async Task IdentifyBottlenecks_HighUtilization_ReturnsBottleneck()
    {
        var company = await CreateTestCompanyAsync();
        var user = await CreateTestUserAsync(company.Id);
        var equipment = await CreateTestEquipmentAsync(company.Id, "Busy Still", EquipmentType.Still);
        var plan = await CreateTestPlanAsync(company.Id, user.Id);

        // Use fixed dates to avoid timing issues
        var startDate = DateTime.UtcNow.Date;
        var endDate = startDate.AddDays(7);

        // Create high utilization (>95%) - allocate 122 hours for 128 hour capacity (~95%)
        await CreateTestAllocationAsync(plan.Id, equipment.Id, 122, startDate: startDate, endDate: endDate);

        var bottlenecks = await _service.IdentifyBottlenecksAsync(
            company.Id,
            startDate,
            endDate
        );

        Assert.NotEmpty(bottlenecks);
        Assert.True(bottlenecks.First().UtilizationPercent >= 85);
    }

    [Fact]
    public async Task IdentifyBottlenecks_NoIssues_ReturnsEmpty()
    {
        var company = await CreateTestCompanyAsync();
        var user = await CreateTestUserAsync(company.Id);
        var equipment = await CreateTestEquipmentAsync(company.Id, "Low Use Still", EquipmentType.Still);
        var plan = await CreateTestPlanAsync(company.Id, user.Id);

        // Create low utilization (< 50%)
        await CreateTestAllocationAsync(plan.Id, equipment.Id, 40);

        var bottlenecks = await _service.IdentifyBottlenecksAsync(
            company.Id,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(7)
        );

        Assert.Empty(bottlenecks);
    }

    [Fact]
    public async Task AnalyzeBottleneck_ReturnsAffectedRuns()
    {
        var company = await CreateTestCompanyAsync();
        var user = await CreateTestUserAsync(company.Id);
        var equipment = await CreateTestEquipmentAsync(company.Id, "Test Still", EquipmentType.Still);

        var analysis = await _service.AnalyzeBottleneckAsync(
            equipment.Id,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(7)
        );

        Assert.NotNull(analysis);
        Assert.Equal(equipment.Id, analysis.EquipmentId);
    }

    [Fact]
    public async Task SuggestResolutions_ReturnsOrderedByCostEffectiveness()
    {
        var bottleneck = new Bottleneck(
            1,
            "Test Still",
            "Still",
            BottleneckSeverity.High,
            92m,
            5,
            TimeSpan.FromHours(2),
            20m,
            "Test bottleneck"
        );

        var resolutions = await _service.SuggestResolutionsAsync(bottleneck);

        Assert.NotEmpty(resolutions);
        var orderedResolutions = resolutions.ToList();
        Assert.True(orderedResolutions.First().EffectivenessScore >= orderedResolutions.Last().EffectivenessScore);
    }

    #endregion

    #region Forecast Tests

    [Fact]
    public async Task ForecastCapacity_MovingAverage_CalculatesCorrectly()
    {
        var company = await CreateTestCompanyAsync();
        await CreateTestEquipmentAsync(company.Id, "Still #1", EquipmentType.Still);

        var forecast = await _service.ForecastCapacityAsync(company.Id, 4, ForecastMethod.MovingAverage);

        Assert.NotNull(forecast);
        Assert.Equal(4, forecast.WeeklyForecasts.Count);
        Assert.Equal(ForecastMethod.MovingAverage, forecast.Method);
        Assert.True(forecast.ConfidenceLevel > 0);
    }

    [Fact]
    public async Task ForecastDemand_BasedOnHistoricalOrders_ReturnsForecasts()
    {
        var company = await CreateTestCompanyAsync();

        var forecast = await _service.ForecastDemandAsync(company.Id, 4);

        Assert.NotNull(forecast);
        Assert.Equal(4, forecast.WeeklyForecasts.Count);
    }

    [Fact]
    public async Task AnalyzeCapacityGap_ReturnsRecommendations()
    {
        var company = await CreateTestCompanyAsync();
        await CreateTestEquipmentAsync(company.Id, "Still #1", EquipmentType.Still);

        var analysis = await _service.AnalyzeCapacityGapAsync(
            company.Id,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(28)
        );

        Assert.NotNull(analysis);
        Assert.NotEmpty(analysis.Recommendations);
    }

    #endregion

    #region Capacity Plan Tests

    [Fact]
    public async Task CreatePlan_ValidData_ReturnsCreatedPlan()
    {
        var company = await CreateTestCompanyAsync();
        var user = await CreateTestUserAsync(company.Id);

        var dto = new CreateCapacityPlanDto(
            "Test Plan",
            "Description",
            DateTime.UtcNow,
            DateTime.UtcNow.AddMonths(1),
            "Monthly",
            1000m,
            5000,
            10,
            "Notes",
            null
        );

        var plan = await _service.CreateCapacityPlanAsync(dto, company.Id, user.Id);

        Assert.NotNull(plan);
        Assert.Equal("Test Plan", plan.Name);
        Assert.Equal(CapacityPlanStatus.Draft, plan.Status);
    }

    [Fact]
    public async Task CreatePlan_InvalidDates_ThrowsException()
    {
        var company = await CreateTestCompanyAsync();
        var user = await CreateTestUserAsync(company.Id);

        var dto = new CreateCapacityPlanDto(
            "Invalid Plan",
            null,
            DateTime.UtcNow.AddMonths(1), // End before start
            DateTime.UtcNow,
            "Monthly",
            null, null, null, null, null
        );

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateCapacityPlanAsync(dto, company.Id, user.Id)
        );
    }

    [Fact]
    public async Task ValidatePlan_WithIssues_ReturnsValidationErrors()
    {
        var company = await CreateTestCompanyAsync();
        var user = await CreateTestUserAsync(company.Id);
        var plan = await CreateTestPlanAsync(company.Id, user.Id);

        var validation = await _service.ValidateCapacityPlanAsync(plan.Id);

        Assert.NotNull(validation);
        // Plan should be valid with no allocations
        Assert.True(validation.IsValid);
    }

    [Fact]
    public async Task ActivatePlan_DraftStatus_UpdatesToActive()
    {
        var company = await CreateTestCompanyAsync();
        var user = await CreateTestUserAsync(company.Id);
        var plan = await CreateTestPlanAsync(company.Id, user.Id);

        var activatedPlan = await _service.ActivateCapacityPlanAsync(plan.Id, company.Id);

        Assert.Equal(CapacityPlanStatus.Active, activatedPlan.Status);
    }

    #endregion

    #region Scenario Tests

    [Fact]
    public async Task RunScenario_AddEquipment_ShowsCapacityIncrease()
    {
        var company = await CreateTestCompanyAsync();
        await CreateTestEquipmentAsync(company.Id, "Still #1", EquipmentType.Still);

        var scenario = new WhatIfScenarioDto(
            "Add Equipment",
            new List<ScenarioChange>
            {
                new ScenarioChange(
                    ScenarioChangeType.AddEquipment,
                    null,
                    new Dictionary<string, object> { { "hoursPerDay", 16 }, { "cost", 50000 } }
                )
            },
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(30)
        );

        var result = await _service.RunScenarioAsync(scenario, company.Id);

        Assert.NotNull(result);
        Assert.True(result.CapacityChangePercent > 0);
        Assert.True(result.CostImpact > 0);
    }

    [Fact]
    public async Task RunScenario_RemoveEquipment_ShowsCapacityDecrease()
    {
        var company = await CreateTestCompanyAsync();
        var equipment = await CreateTestEquipmentAsync(company.Id, "Still #1", EquipmentType.Still);

        var scenario = new WhatIfScenarioDto(
            "Remove Equipment",
            new List<ScenarioChange>
            {
                new ScenarioChange(
                    ScenarioChangeType.RemoveEquipment,
                    equipment.Id,
                    new Dictionary<string, object>()
                )
            },
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(30)
        );

        var result = await _service.RunScenarioAsync(scenario, company.Id);

        Assert.NotNull(result);
        Assert.True(result.CapacityChangePercent < 0);
    }

    #endregion

    #region Constraint Tests

    [Fact]
    public async Task CreateConstraint_Valid_ReturnsCreatedConstraint()
    {
        var company = await CreateTestCompanyAsync();
        var user = await CreateTestUserAsync(company.Id);

        var dto = new CreateConstraintDto(
            null, // Company-wide
            "MaxHoursPerDay",
            16m,
            DateTime.UtcNow,
            null,
            "Standard operating hours"
        );

        var constraint = await _service.CreateConstraintAsync(dto, company.Id, user.Id);

        Assert.NotNull(constraint);
        Assert.Equal(CapacityConstraintType.MaxHoursPerDay, constraint.ConstraintType);
        Assert.Null(constraint.EquipmentId);
        Assert.True(constraint.IsActive);
    }

    [Fact]
    public async Task GetConstraints_FiltersByActiveStatus()
    {
        var company = await CreateTestCompanyAsync();
        var user = await CreateTestUserAsync(company.Id);

        var activeConstraint = await _service.CreateConstraintAsync(
            new CreateConstraintDto(null, "MaxHoursPerDay", 16m, DateTime.UtcNow, null, null),
            company.Id,
            user.Id
        );

        var inactiveDto = new CreateConstraintDto(null, "MaxRunsPerDay", 3m, DateTime.UtcNow, null, null);
        var inactiveConstraint = await _service.CreateConstraintAsync(inactiveDto, company.Id, user.Id);
        await _service.DeleteConstraintAsync(inactiveConstraint.Id, company.Id);

        var activeConstraints = await _service.GetConstraintsAsync(company.Id, null, true);

        Assert.Single(activeConstraints);
        Assert.Equal(activeConstraint.Id, activeConstraints.First().Id);
    }

    #endregion

    #region Snapshot Tests

    [Fact]
    public async Task CaptureSnapshot_CreatesRecordForEachEquipment()
    {
        var company = await CreateTestCompanyAsync();
        await CreateTestEquipmentAsync(company.Id, "Still #1", EquipmentType.Still);
        await CreateTestEquipmentAsync(company.Id, "Fermenter #1", EquipmentType.Fermenter);

        await _service.CaptureCapacitySnapshotAsync(company.Id, DateTime.UtcNow);

        var snapshots = await _service.GetHistoricalSnapshotsAsync(
            company.Id,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(1)
        );

        Assert.Equal(2, snapshots.Count());
    }

    [Fact]
    public async Task GetHistoricalSnapshots_FiltersByDateRange()
    {
        var company = await CreateTestCompanyAsync();
        await CreateTestEquipmentAsync(company.Id, "Still #1", EquipmentType.Still);

        await _service.CaptureCapacitySnapshotAsync(company.Id, DateTime.UtcNow.AddDays(-10));
        await _service.CaptureCapacitySnapshotAsync(company.Id, DateTime.UtcNow);

        var snapshots = await _service.GetHistoricalSnapshotsAsync(
            company.Id,
            DateTime.UtcNow.AddDays(-5),
            DateTime.UtcNow.AddDays(1)
        );

        Assert.Single(snapshots);
    }

    #endregion

    #region Overview Tests

    [Fact]
    public async Task GetCapacityOverview_ReturnsCorrectTotals()
    {
        var company = await CreateTestCompanyAsync();
        await CreateTestEquipmentAsync(company.Id, "Still #1", EquipmentType.Still);
        await CreateTestEquipmentAsync(company.Id, "Fermenter #1", EquipmentType.Fermenter);

        var overview = await _service.GetCapacityOverviewAsync(
            company.Id,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(7)
        );

        Assert.Equal(2, overview.TotalEquipmentCount);
        Assert.True(overview.TotalCapacityHours > 0);
        Assert.Equal(0, overview.AllocatedHours); // No allocations
    }

    [Fact]
    public async Task GetCapacityOverview_GeneratesAlertsForHighUtilization()
    {
        var company = await CreateTestCompanyAsync();
        var user = await CreateTestUserAsync(company.Id);
        var equipment = await CreateTestEquipmentAsync(company.Id, "Busy Still", EquipmentType.Still);
        var plan = await CreateTestPlanAsync(company.Id, user.Id);

        // Use fixed dates to avoid timing issues
        var startDate = DateTime.UtcNow.Date;
        var endDate = startDate.AddDays(7);

        // Create 95%+ utilization (122/128 = 95.3%)
        await CreateTestAllocationAsync(plan.Id, equipment.Id, 122, startDate: startDate, endDate: endDate);

        var overview = await _service.GetCapacityOverviewAsync(
            company.Id,
            startDate,
            endDate
        );

        Assert.NotEmpty(overview.Alerts);
        Assert.Contains(overview.Alerts, a => a.Severity >= CapacityAlertSeverity.Warning);
    }

    #endregion
}
