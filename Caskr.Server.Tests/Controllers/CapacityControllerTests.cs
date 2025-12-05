using Caskr.server.Controllers;
using Caskr.server.Models;
using Caskr.server.Models.Production;
using Caskr.server.Services.Production;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace Caskr.Server.Tests.Controllers;

public class CapacityControllerTests : IDisposable
{
    private readonly CaskrDbContext _context;
    private readonly Mock<ICapacityAnalysisService> _serviceMock;
    private readonly Mock<ILogger<CapacityController>> _loggerMock;
    private readonly CapacityController _controller;

    public CapacityControllerTests()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CaskrDbContext(options);
        _serviceMock = new Mock<ICapacityAnalysisService>();
        _loggerMock = new Mock<ILogger<CapacityController>>();
        _controller = new CapacityController(_context, _serviceMock.Object, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region Helper Methods

    private async Task<(Company company, User user)> SetupAuthenticatedUserAsync()
    {
        var company = new Company
        {
            CompanyName = "Test Distillery",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Companies.Add(company);
        await _context.SaveChangesAsync();

        var userType = new UserType { Name = "Admin" };
        _context.UserTypes.Add(userType);
        await _context.SaveChangesAsync();

        var user = new User
        {
            Name = "Test User",
            Email = "test@example.com",
            CompanyId = company.Id,
            UserTypeId = userType.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Set up the controller context with authenticated user
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuthentication");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        return (company, user);
    }

    private async Task<Equipment> CreateTestEquipmentAsync(int companyId)
    {
        var equipment = new Equipment
        {
            CompanyId = companyId,
            Name = "Test Still",
            EquipmentType = EquipmentType.Still,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Equipment.Add(equipment);
        await _context.SaveChangesAsync();
        return equipment;
    }

    #endregion

    #region Overview Tests

    [Fact]
    public async Task GetOverview_ValidDateRange_ReturnsOverview()
    {
        var (company, _) = await SetupAuthenticatedUserAsync();
        var startDate = DateTime.UtcNow;
        var endDate = DateTime.UtcNow.AddDays(7);

        var mockOverview = new CapacityOverview(
            2, 224m, 100m, 124m, 44.64m,
            new List<EquipmentCapacitySummary>(),
            new List<CapacityAlert>()
        );

        _serviceMock.Setup(s => s.GetCapacityOverviewAsync(company.Id, startDate, endDate))
            .ReturnsAsync(mockOverview);

        var result = await _controller.GetOverview(company.Id, startDate, endDate);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<CapacityOverviewResponse>(okResult.Value);
        Assert.Equal(2, response.TotalEquipmentCount);
        Assert.Equal(224m, response.TotalCapacityHours);
    }

    [Fact]
    public async Task GetOverview_InvalidDateRange_Returns400()
    {
        var (company, _) = await SetupAuthenticatedUserAsync();
        var startDate = DateTime.UtcNow.AddDays(7);
        var endDate = DateTime.UtcNow; // End before start

        var result = await _controller.GetOverview(company.Id, startDate, endDate);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetEquipmentCapacity_NotFound_Returns404()
    {
        await SetupAuthenticatedUserAsync();
        var nonExistentId = 999;

        var result = await _controller.GetEquipmentCapacity(nonExistentId, DateTime.UtcNow, DateTime.UtcNow.AddDays(7));

        Assert.IsType<NotFoundResult>(result.Result);
    }

    #endregion

    #region Bottleneck Tests

    [Fact]
    public async Task GetBottlenecks_WithData_ReturnsBottlenecks()
    {
        var (company, _) = await SetupAuthenticatedUserAsync();
        var startDate = DateTime.UtcNow;
        var endDate = DateTime.UtcNow.AddDays(7);

        var mockBottlenecks = new List<Bottleneck>
        {
            new Bottleneck(1, "Still #1", "Still", BottleneckSeverity.High, 92m, 3, TimeSpan.FromHours(2), 10m, "High utilization")
        };

        _serviceMock.Setup(s => s.IdentifyBottlenecksAsync(company.Id, startDate, endDate, null))
            .ReturnsAsync(mockBottlenecks);

        var result = await _controller.GetBottlenecks(company.Id, startDate, endDate);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var bottlenecks = Assert.IsAssignableFrom<IEnumerable<BottleneckResponse>>(okResult.Value);
        Assert.Single(bottlenecks);
    }

    [Fact]
    public async Task GetBottlenecks_FilterBySeverity_FiltersCorrectly()
    {
        var (company, _) = await SetupAuthenticatedUserAsync();
        var startDate = DateTime.UtcNow;
        var endDate = DateTime.UtcNow.AddDays(7);

        var mockBottlenecks = new List<Bottleneck>
        {
            new Bottleneck(1, "Still #1", "Still", BottleneckSeverity.Critical, 98m, 5, TimeSpan.FromHours(4), 20m, "Critical")
        };

        _serviceMock.Setup(s => s.IdentifyBottlenecksAsync(company.Id, startDate, endDate, BottleneckSeverity.High))
            .ReturnsAsync(mockBottlenecks);

        var result = await _controller.GetBottlenecks(company.Id, startDate, endDate, "High");

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var bottlenecks = Assert.IsAssignableFrom<IEnumerable<BottleneckResponse>>(okResult.Value);
        Assert.Single(bottlenecks);
    }

    #endregion

    #region Plan Tests

    [Fact]
    public async Task CreatePlan_ValidData_Returns201()
    {
        var (company, user) = await SetupAuthenticatedUserAsync();

        var dto = new CreateCapacityPlanDto(
            "Test Plan",
            "Description",
            DateTime.UtcNow,
            DateTime.UtcNow.AddMonths(1),
            "Monthly",
            1000m, 5000, 10, "Notes", null
        );

        var mockPlan = new CapacityPlan
        {
            Id = 1,
            CompanyId = company.Id,
            Name = dto.Name,
            PlanPeriodStart = dto.PlanPeriodStart,
            PlanPeriodEnd = dto.PlanPeriodEnd,
            PlanType = CapacityPlanType.Monthly,
            Status = CapacityPlanStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Allocations = new List<CapacityAllocation>()
        };

        _serviceMock.Setup(s => s.CreateCapacityPlanAsync(dto, company.Id, user.Id))
            .ReturnsAsync(mockPlan);

        var result = await _controller.CreatePlan(company.Id, dto);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(201, createdResult.StatusCode);
    }

    [Fact]
    public async Task ValidatePlan_WithIssues_ReturnsValidationErrors()
    {
        var (company, user) = await SetupAuthenticatedUserAsync();

        var plan = new CapacityPlan
        {
            CompanyId = company.Id,
            Name = "Test Plan",
            PlanPeriodStart = DateTime.UtcNow,
            PlanPeriodEnd = DateTime.UtcNow.AddMonths(1),
            PlanType = CapacityPlanType.Monthly,
            Status = CapacityPlanStatus.Draft,
            CreatedByUserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.CapacityPlans.Add(plan);
        await _context.SaveChangesAsync();

        var mockValidation = new CapacityValidation(
            false,
            new List<ValidationIssue> { new ValidationIssue("OVERLAP", "Overlapping allocations", 1, "Still #1") },
            new List<ValidationWarning>()
        );

        _serviceMock.Setup(s => s.ValidateCapacityPlanAsync(plan.Id))
            .ReturnsAsync(mockValidation);

        var result = await _controller.ValidatePlan(plan.Id);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var validation = Assert.IsType<CapacityValidation>(okResult.Value);
        Assert.False(validation.IsValid);
        Assert.Single(validation.Issues);
    }

    [Fact]
    public async Task ActivatePlan_DraftStatus_UpdatesToActive()
    {
        var (company, user) = await SetupAuthenticatedUserAsync();

        var plan = new CapacityPlan
        {
            CompanyId = company.Id,
            Name = "Test Plan",
            PlanPeriodStart = DateTime.UtcNow,
            PlanPeriodEnd = DateTime.UtcNow.AddMonths(1),
            PlanType = CapacityPlanType.Monthly,
            Status = CapacityPlanStatus.Draft,
            CreatedByUserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Allocations = new List<CapacityAllocation>()
        };
        _context.CapacityPlans.Add(plan);
        await _context.SaveChangesAsync();

        var activatedPlan = new CapacityPlan
        {
            Id = plan.Id,
            CompanyId = company.Id,
            Name = plan.Name,
            PlanPeriodStart = plan.PlanPeriodStart,
            PlanPeriodEnd = plan.PlanPeriodEnd,
            PlanType = plan.PlanType,
            Status = CapacityPlanStatus.Active,
            CreatedAt = plan.CreatedAt,
            UpdatedAt = DateTime.UtcNow,
            Allocations = new List<CapacityAllocation>()
        };

        _serviceMock.Setup(s => s.ActivateCapacityPlanAsync(plan.Id, company.Id))
            .ReturnsAsync(activatedPlan);

        var result = await _controller.ActivatePlan(plan.Id, company.Id);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<CapacityPlanDetailResponse>(okResult.Value);
        Assert.Equal("Active", response.Status);
    }

    #endregion

    #region Forecast Tests

    [Fact]
    public async Task GetForecast_DefaultParams_ReturnsData()
    {
        var (company, _) = await SetupAuthenticatedUserAsync();

        var mockForecast = new CapacityForecast(
            DateTime.UtcNow,
            ForecastMethod.MovingAverage,
            new List<WeeklyForecast>
            {
                new WeeklyForecast(DateTime.UtcNow, DateTime.UtcNow.AddDays(6), 65m, 55m, 75m, 100m, 150m)
            },
            0.85m,
            new List<string> { "Based on historical data" }
        );

        _serviceMock.Setup(s => s.ForecastCapacityAsync(company.Id, 12, ForecastMethod.MovingAverage))
            .ReturnsAsync(mockForecast);

        var result = await _controller.GetForecast(company.Id);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ForecastResponse>(okResult.Value);
        Assert.NotEmpty(response.Weeks);
    }

    [Fact]
    public async Task GetForecast_InvalidMethod_Returns400()
    {
        var (company, _) = await SetupAuthenticatedUserAsync();

        var result = await _controller.GetForecast(company.Id, 12, "InvalidMethod");

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetGapAnalysis_ReturnsRecommendations()
    {
        var (company, _) = await SetupAuthenticatedUserAsync();
        var startDate = DateTime.UtcNow;
        var endDate = DateTime.UtcNow.AddDays(28);

        var mockAnalysis = new GapAnalysis(
            startDate, endDate, 1000m, 800m, 200m, 20m, false,
            new List<WeeklyGap>(),
            new List<string> { "Significant excess capacity available" }
        );

        _serviceMock.Setup(s => s.AnalyzeCapacityGapAsync(company.Id, startDate, endDate))
            .ReturnsAsync(mockAnalysis);

        var result = await _controller.GetGapAnalysis(company.Id, startDate, endDate);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var analysis = Assert.IsType<GapAnalysis>(okResult.Value);
        Assert.NotEmpty(analysis.Recommendations);
    }

    #endregion

    #region Scenario Tests

    [Fact]
    public async Task RunScenario_ValidData_ReturnsResult()
    {
        var (company, _) = await SetupAuthenticatedUserAsync();

        var scenario = new WhatIfScenarioDto(
            "Add Equipment",
            new List<ScenarioChange>
            {
                new ScenarioChange(ScenarioChangeType.AddEquipment, null, new Dictionary<string, object>())
            },
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(30)
        );

        var mockResult = new ScenarioResult(
            1, "Add Equipment",
            new CapacityOverview(3, 336m, 100m, 236m, 29.76m, new List<EquipmentCapacitySummary>(), new List<CapacityAlert>()),
            new List<Bottleneck>(),
            50m, 50000m, "Adding 112 hours of capacity"
        );

        _serviceMock.Setup(s => s.RunScenarioAsync(It.IsAny<WhatIfScenarioDto>(), company.Id))
            .ReturnsAsync(mockResult);

        var result = await _controller.RunScenario(company.Id, scenario);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var scenarioResult = Assert.IsType<ScenarioResult>(okResult.Value);
        Assert.True(scenarioResult.CapacityChangePercent > 0);
    }

    #endregion

    #region Constraint Tests

    [Fact]
    public async Task GetConstraints_ReturnsActiveConstraints()
    {
        var (company, _) = await SetupAuthenticatedUserAsync();

        var mockConstraints = new List<CapacityConstraint>
        {
            new CapacityConstraint
            {
                Id = 1,
                CompanyId = company.Id,
                ConstraintType = CapacityConstraintType.MaxHoursPerDay,
                ConstraintValue = 16m,
                EffectiveFrom = DateTime.UtcNow,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _serviceMock.Setup(s => s.GetConstraintsAsync(company.Id, null, true))
            .ReturnsAsync(mockConstraints);

        var result = await _controller.GetConstraints(company.Id);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var constraints = Assert.IsAssignableFrom<IEnumerable<ConstraintResponse>>(okResult.Value);
        Assert.Single(constraints);
    }

    [Fact]
    public async Task CreateConstraint_ValidData_Returns201()
    {
        var (company, user) = await SetupAuthenticatedUserAsync();

        var dto = new CreateConstraintDto(
            null,
            "MaxHoursPerDay",
            16m,
            DateTime.UtcNow,
            null,
            "Standard hours"
        );

        var mockConstraint = new CapacityConstraint
        {
            Id = 1,
            CompanyId = company.Id,
            ConstraintType = CapacityConstraintType.MaxHoursPerDay,
            ConstraintValue = 16m,
            EffectiveFrom = DateTime.UtcNow,
            IsActive = true,
            CreatedByUserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _serviceMock.Setup(s => s.CreateConstraintAsync(dto, company.Id, user.Id))
            .ReturnsAsync(mockConstraint);

        var result = await _controller.CreateConstraint(company.Id, dto);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(201, createdResult.StatusCode);
    }

    #endregion

    #region Export Tests

    [Fact]
    public async Task ExportUtilization_CSV_ReturnsFile()
    {
        var (company, _) = await SetupAuthenticatedUserAsync();
        var startDate = DateTime.UtcNow;
        var endDate = DateTime.UtcNow.AddDays(7);

        var mockUtilization = new List<EquipmentUtilization>
        {
            new EquipmentUtilization(1, "Still #1", "Still", 75m, 112m, 84m, 8m)
        };

        _serviceMock.Setup(s => s.GetEquipmentUtilizationAsync(company.Id, startDate, endDate))
            .ReturnsAsync(mockUtilization);

        var result = await _controller.ExportUtilization(company.Id, startDate, endDate);

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/csv", fileResult.ContentType);
        Assert.Contains(".csv", fileResult.FileDownloadName);
    }

    #endregion

    #region Authorization Tests

    [Fact]
    public async Task GetOverview_WrongCompany_Returns403()
    {
        var (company, _) = await SetupAuthenticatedUserAsync();
        var wrongCompanyId = company.Id + 100; // Different company

        var result = await _controller.GetOverview(wrongCompanyId, DateTime.UtcNow, DateTime.UtcNow.AddDays(7));

        Assert.IsType<ForbidResult>(result.Result);
    }

    #endregion
}
