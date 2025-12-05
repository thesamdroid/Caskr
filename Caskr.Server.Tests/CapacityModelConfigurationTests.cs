using Caskr.server.Models;
using Caskr.server.Models.Production;
using Microsoft.EntityFrameworkCore;

namespace Caskr.Server.Tests;

public class CapacityModelConfigurationTests : IDisposable
{
    private readonly CaskrDbContext _context;

    public CapacityModelConfigurationTests()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CaskrDbContext(options);
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
        var userType = new UserType
        {
            Name = "Test User Type"
        };
        _context.UserTypes.Add(userType);
        await _context.SaveChangesAsync();

        var user = new User
        {
            Name = "Test User",
            Email = "test@example.com",
            CompanyId = companyId,
            UserTypeId = userType.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    private async Task<Equipment> CreateTestEquipmentAsync(int companyId)
    {
        var equipment = new Equipment
        {
            CompanyId = companyId,
            Name = "Test Still",
            EquipmentType = EquipmentType.Still,
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

    #endregion

    #region CapacityPlan Tests

    [Fact]
    public async Task CapacityPlan_CanBeCreated()
    {
        var company = await CreateTestCompanyAsync();

        var plan = new CapacityPlan
        {
            CompanyId = company.Id,
            Name = "Q1 2025 Capacity Plan",
            Description = "First quarter capacity planning",
            PlanPeriodStart = new DateTime(2025, 1, 1),
            PlanPeriodEnd = new DateTime(2025, 3, 31),
            PlanType = CapacityPlanType.Quarterly,
            Status = CapacityPlanStatus.Draft,
            TargetProofGallons = 10000,
            TargetBottles = 50000,
            TargetBatches = 20,
            Notes = "Plan notes",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.CapacityPlans.Add(plan);
        await _context.SaveChangesAsync();

        var savedPlan = await _context.CapacityPlans.FindAsync(plan.Id);
        Assert.NotNull(savedPlan);
        Assert.Equal("Q1 2025 Capacity Plan", savedPlan.Name);
        Assert.Equal(CapacityPlanType.Quarterly, savedPlan.PlanType);
        Assert.Equal(CapacityPlanStatus.Draft, savedPlan.Status);
        Assert.Equal(10000, savedPlan.TargetProofGallons);
    }

    [Fact]
    public async Task CapacityPlan_WithCreatedByUser_CanBeCreated()
    {
        var company = await CreateTestCompanyAsync();
        var user = await CreateTestUserAsync(company.Id);

        var plan = new CapacityPlan
        {
            CompanyId = company.Id,
            Name = "Plan with User",
            PlanPeriodStart = DateTime.UtcNow,
            PlanPeriodEnd = DateTime.UtcNow.AddMonths(1),
            PlanType = CapacityPlanType.Monthly,
            CreatedByUserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.CapacityPlans.Add(plan);
        await _context.SaveChangesAsync();

        var savedPlan = await _context.CapacityPlans
            .Include(p => p.CreatedByUser)
            .FirstOrDefaultAsync(p => p.Id == plan.Id);

        Assert.NotNull(savedPlan);
        Assert.NotNull(savedPlan.CreatedByUser);
        Assert.Equal("Test User", savedPlan.CreatedByUser.Name);
    }

    [Fact]
    public async Task CapacityPlan_StatusTransitions_WorkCorrectly()
    {
        var company = await CreateTestCompanyAsync();

        var plan = new CapacityPlan
        {
            CompanyId = company.Id,
            Name = "Status Test Plan",
            PlanPeriodStart = DateTime.UtcNow,
            PlanPeriodEnd = DateTime.UtcNow.AddMonths(1),
            PlanType = CapacityPlanType.Monthly,
            Status = CapacityPlanStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.CapacityPlans.Add(plan);
        await _context.SaveChangesAsync();

        // Transition to Active
        plan.Status = CapacityPlanStatus.Active;
        plan.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var activePlan = await _context.CapacityPlans.FindAsync(plan.Id);
        Assert.NotNull(activePlan);
        Assert.Equal(CapacityPlanStatus.Active, activePlan.Status);

        // Transition to Completed
        plan.Status = CapacityPlanStatus.Completed;
        plan.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var completedPlan = await _context.CapacityPlans.FindAsync(plan.Id);
        Assert.NotNull(completedPlan);
        Assert.Equal(CapacityPlanStatus.Completed, completedPlan.Status);
    }

    #endregion

    #region CapacityAllocation Tests

    [Fact]
    public async Task CapacityAllocation_CanBeCreated()
    {
        var company = await CreateTestCompanyAsync();
        var equipment = await CreateTestEquipmentAsync(company.Id);

        var plan = new CapacityPlan
        {
            CompanyId = company.Id,
            Name = "Allocation Test Plan",
            PlanPeriodStart = DateTime.UtcNow,
            PlanPeriodEnd = DateTime.UtcNow.AddMonths(1),
            PlanType = CapacityPlanType.Monthly,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.CapacityPlans.Add(plan);
        await _context.SaveChangesAsync();

        var allocation = new CapacityAllocation
        {
            CapacityPlanId = plan.Id,
            EquipmentId = equipment.Id,
            AllocationType = CapacityAllocationType.Production,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7),
            HoursAllocated = 40,
            ProductionType = ProductionType.Distillation,
            Notes = "Weekly distillation allocation",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.CapacityAllocations.Add(allocation);
        await _context.SaveChangesAsync();

        var savedAllocation = await _context.CapacityAllocations.FindAsync(allocation.Id);
        Assert.NotNull(savedAllocation);
        Assert.Equal(40, savedAllocation.HoursAllocated);
        Assert.Equal(CapacityAllocationType.Production, savedAllocation.AllocationType);
        Assert.Equal(ProductionType.Distillation, savedAllocation.ProductionType);
    }

    [Fact]
    public async Task CapacityAllocation_NavigationProperties_Work()
    {
        var company = await CreateTestCompanyAsync();
        var equipment = await CreateTestEquipmentAsync(company.Id);

        var plan = new CapacityPlan
        {
            CompanyId = company.Id,
            Name = "Navigation Test Plan",
            PlanPeriodStart = DateTime.UtcNow,
            PlanPeriodEnd = DateTime.UtcNow.AddMonths(1),
            PlanType = CapacityPlanType.Monthly,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.CapacityPlans.Add(plan);
        await _context.SaveChangesAsync();

        var allocation = new CapacityAllocation
        {
            CapacityPlanId = plan.Id,
            EquipmentId = equipment.Id,
            AllocationType = CapacityAllocationType.Maintenance,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            HoursAllocated = 8,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.CapacityAllocations.Add(allocation);
        await _context.SaveChangesAsync();

        var savedAllocation = await _context.CapacityAllocations
            .Include(a => a.CapacityPlan)
            .Include(a => a.Equipment)
            .FirstOrDefaultAsync(a => a.Id == allocation.Id);

        Assert.NotNull(savedAllocation);
        Assert.NotNull(savedAllocation.CapacityPlan);
        Assert.NotNull(savedAllocation.Equipment);
        Assert.Equal("Navigation Test Plan", savedAllocation.CapacityPlan.Name);
        Assert.Equal("Test Still", savedAllocation.Equipment.Name);
    }

    [Fact]
    public async Task CapacityAllocation_CascadeDeleteWithPlan()
    {
        var company = await CreateTestCompanyAsync();
        var equipment = await CreateTestEquipmentAsync(company.Id);

        var plan = new CapacityPlan
        {
            CompanyId = company.Id,
            Name = "Cascade Delete Test Plan",
            PlanPeriodStart = DateTime.UtcNow,
            PlanPeriodEnd = DateTime.UtcNow.AddMonths(1),
            PlanType = CapacityPlanType.Monthly,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.CapacityPlans.Add(plan);
        await _context.SaveChangesAsync();

        var allocation1 = new CapacityAllocation
        {
            CapacityPlanId = plan.Id,
            EquipmentId = equipment.Id,
            AllocationType = CapacityAllocationType.Production,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7),
            HoursAllocated = 40,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var allocation2 = new CapacityAllocation
        {
            CapacityPlanId = plan.Id,
            EquipmentId = equipment.Id,
            AllocationType = CapacityAllocationType.Maintenance,
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(8),
            HoursAllocated = 8,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.CapacityAllocations.AddRange(allocation1, allocation2);
        await _context.SaveChangesAsync();

        var allocation1Id = allocation1.Id;
        var allocation2Id = allocation2.Id;

        // Delete the plan
        _context.CapacityPlans.Remove(plan);
        await _context.SaveChangesAsync();

        // Verify allocations are also deleted
        var deletedAllocation1 = await _context.CapacityAllocations.FindAsync(allocation1Id);
        var deletedAllocation2 = await _context.CapacityAllocations.FindAsync(allocation2Id);
        Assert.Null(deletedAllocation1);
        Assert.Null(deletedAllocation2);
    }

    [Fact]
    public async Task CapacityAllocation_MultipleAllocationsPerEquipment()
    {
        var company = await CreateTestCompanyAsync();
        var equipment = await CreateTestEquipmentAsync(company.Id);

        var plan = new CapacityPlan
        {
            CompanyId = company.Id,
            Name = "Multi Allocation Test Plan",
            PlanPeriodStart = DateTime.UtcNow,
            PlanPeriodEnd = DateTime.UtcNow.AddMonths(1),
            PlanType = CapacityPlanType.Monthly,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.CapacityPlans.Add(plan);
        await _context.SaveChangesAsync();

        var allocation1 = new CapacityAllocation
        {
            CapacityPlanId = plan.Id,
            EquipmentId = equipment.Id,
            AllocationType = CapacityAllocationType.Production,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1),
            HoursAllocated = 8,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var allocation2 = new CapacityAllocation
        {
            CapacityPlanId = plan.Id,
            EquipmentId = equipment.Id,
            AllocationType = CapacityAllocationType.Production,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(2),
            HoursAllocated = 8,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.CapacityAllocations.AddRange(allocation1, allocation2);
        await _context.SaveChangesAsync();

        var equipmentAllocations = await _context.CapacityAllocations
            .Where(a => a.EquipmentId == equipment.Id)
            .ToListAsync();

        Assert.Equal(2, equipmentAllocations.Count);
    }

    #endregion

    #region CapacityConstraint Tests

    [Fact]
    public async Task CapacityConstraint_CanBeCreated()
    {
        var company = await CreateTestCompanyAsync();

        var constraint = new CapacityConstraint
        {
            CompanyId = company.Id,
            EquipmentId = null, // Company-wide constraint
            ConstraintType = CapacityConstraintType.MaxHoursPerDay,
            ConstraintValue = 16,
            EffectiveFrom = DateTime.UtcNow,
            EffectiveTo = null,
            Reason = "Standard operating hours",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.CapacityConstraints.Add(constraint);
        await _context.SaveChangesAsync();

        var savedConstraint = await _context.CapacityConstraints.FindAsync(constraint.Id);
        Assert.NotNull(savedConstraint);
        Assert.Equal(CapacityConstraintType.MaxHoursPerDay, savedConstraint.ConstraintType);
        Assert.Equal(16, savedConstraint.ConstraintValue);
        Assert.Null(savedConstraint.EquipmentId); // Company-wide
        Assert.True(savedConstraint.IsActive);
    }

    [Fact]
    public async Task CapacityConstraint_EquipmentSpecific_CanBeCreated()
    {
        var company = await CreateTestCompanyAsync();
        var equipment = await CreateTestEquipmentAsync(company.Id);

        var constraint = new CapacityConstraint
        {
            CompanyId = company.Id,
            EquipmentId = equipment.Id,
            ConstraintType = CapacityConstraintType.MaxRunsPerDay,
            ConstraintValue = 2,
            EffectiveFrom = DateTime.UtcNow,
            Reason = "Still capacity limitation",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.CapacityConstraints.Add(constraint);
        await _context.SaveChangesAsync();

        var savedConstraint = await _context.CapacityConstraints
            .Include(c => c.Equipment)
            .FirstOrDefaultAsync(c => c.Id == constraint.Id);

        Assert.NotNull(savedConstraint);
        Assert.NotNull(savedConstraint.Equipment);
        Assert.Equal("Test Still", savedConstraint.Equipment.Name);
    }

    [Fact]
    public async Task CapacityConstraint_CompanyWide_NullEquipmentId()
    {
        var company = await CreateTestCompanyAsync();

        var constraint = new CapacityConstraint
        {
            CompanyId = company.Id,
            EquipmentId = null,
            ConstraintType = CapacityConstraintType.MaxHoursPerWeek,
            ConstraintValue = 80,
            EffectiveFrom = DateTime.UtcNow,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.CapacityConstraints.Add(constraint);
        await _context.SaveChangesAsync();

        var companyWideConstraints = await _context.CapacityConstraints
            .Where(c => c.CompanyId == company.Id && c.EquipmentId == null)
            .ToListAsync();

        Assert.Single(companyWideConstraints);
        Assert.Null(companyWideConstraints[0].EquipmentId);
    }

    [Fact]
    public async Task CapacityConstraint_FilterByActiveStatus()
    {
        var company = await CreateTestCompanyAsync();

        var activeConstraint = new CapacityConstraint
        {
            CompanyId = company.Id,
            ConstraintType = CapacityConstraintType.MaxHoursPerDay,
            ConstraintValue = 16,
            EffectiveFrom = DateTime.UtcNow,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var inactiveConstraint = new CapacityConstraint
        {
            CompanyId = company.Id,
            ConstraintType = CapacityConstraintType.MaxRunsPerDay,
            ConstraintValue = 3,
            EffectiveFrom = DateTime.UtcNow.AddMonths(-6),
            EffectiveTo = DateTime.UtcNow.AddMonths(-1),
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.CapacityConstraints.AddRange(activeConstraint, inactiveConstraint);
        await _context.SaveChangesAsync();

        var activeConstraints = await _context.CapacityConstraints
            .Where(c => c.CompanyId == company.Id && c.IsActive)
            .ToListAsync();

        Assert.Single(activeConstraints);
        Assert.Equal(CapacityConstraintType.MaxHoursPerDay, activeConstraints[0].ConstraintType);
    }

    #endregion

    #region CapacitySnapshot Tests

    [Fact]
    public async Task CapacitySnapshot_CanBeCreated()
    {
        var company = await CreateTestCompanyAsync();
        var equipment = await CreateTestEquipmentAsync(company.Id);

        var snapshot = new CapacitySnapshot
        {
            CompanyId = company.Id,
            SnapshotDate = DateTime.UtcNow.Date,
            EquipmentId = equipment.Id,
            TotalCapacityHours = 24,
            AllocatedHours = 20,
            MaintenanceHours = 2,
            UtilizationPercent = 91.67m,
            PlannedProofGallons = 500,
            ActualProofGallons = 485,
            CreatedAt = DateTime.UtcNow
        };

        _context.CapacitySnapshots.Add(snapshot);
        await _context.SaveChangesAsync();

        var savedSnapshot = await _context.CapacitySnapshots.FindAsync(snapshot.Id);
        Assert.NotNull(savedSnapshot);
        Assert.Equal(24, savedSnapshot.TotalCapacityHours);
        Assert.Equal(20, savedSnapshot.AllocatedHours);
        Assert.Equal(91.67m, savedSnapshot.UtilizationPercent);
    }

    [Fact]
    public async Task CapacitySnapshot_NavigationProperties_Work()
    {
        var company = await CreateTestCompanyAsync();
        var equipment = await CreateTestEquipmentAsync(company.Id);

        var snapshot = new CapacitySnapshot
        {
            CompanyId = company.Id,
            SnapshotDate = DateTime.UtcNow.Date,
            EquipmentId = equipment.Id,
            TotalCapacityHours = 24,
            AllocatedHours = 16,
            MaintenanceHours = 0,
            UtilizationPercent = 66.67m,
            CreatedAt = DateTime.UtcNow
        };

        _context.CapacitySnapshots.Add(snapshot);
        await _context.SaveChangesAsync();

        var savedSnapshot = await _context.CapacitySnapshots
            .Include(s => s.Company)
            .Include(s => s.Equipment)
            .FirstOrDefaultAsync(s => s.Id == snapshot.Id);

        Assert.NotNull(savedSnapshot);
        Assert.NotNull(savedSnapshot.Company);
        Assert.NotNull(savedSnapshot.Equipment);
        Assert.Equal("Test Distillery", savedSnapshot.Company.CompanyName);
        Assert.Equal("Test Still", savedSnapshot.Equipment.Name);
    }

    [Fact]
    public async Task CapacitySnapshot_HistoricalData_CanBeQueried()
    {
        var company = await CreateTestCompanyAsync();
        var equipment = await CreateTestEquipmentAsync(company.Id);

        var snapshots = new List<CapacitySnapshot>();
        for (int i = 0; i < 7; i++)
        {
            snapshots.Add(new CapacitySnapshot
            {
                CompanyId = company.Id,
                SnapshotDate = DateTime.UtcNow.Date.AddDays(-i),
                EquipmentId = equipment.Id,
                TotalCapacityHours = 24,
                AllocatedHours = 16 + i,
                MaintenanceHours = 0,
                UtilizationPercent = (16m + i) / 24m * 100,
                CreatedAt = DateTime.UtcNow
            });
        }

        _context.CapacitySnapshots.AddRange(snapshots);
        await _context.SaveChangesAsync();

        var startDate = DateTime.UtcNow.Date.AddDays(-5);
        var endDate = DateTime.UtcNow.Date;

        var historicalSnapshots = await _context.CapacitySnapshots
            .Where(s => s.CompanyId == company.Id &&
                        s.SnapshotDate >= startDate &&
                        s.SnapshotDate <= endDate)
            .OrderBy(s => s.SnapshotDate)
            .ToListAsync();

        Assert.Equal(6, historicalSnapshots.Count);
    }

    #endregion

    #region Enum Tests

    [Fact]
    public void CapacityPlanType_HasExpectedValues()
    {
        Assert.Equal(0, (int)CapacityPlanType.Weekly);
        Assert.Equal(1, (int)CapacityPlanType.Monthly);
        Assert.Equal(2, (int)CapacityPlanType.Quarterly);
        Assert.Equal(3, (int)CapacityPlanType.Annual);
    }

    [Fact]
    public void CapacityPlanStatus_HasExpectedValues()
    {
        Assert.Equal(0, (int)CapacityPlanStatus.Draft);
        Assert.Equal(1, (int)CapacityPlanStatus.Active);
        Assert.Equal(2, (int)CapacityPlanStatus.Completed);
        Assert.Equal(3, (int)CapacityPlanStatus.Archived);
    }

    [Fact]
    public void CapacityAllocationType_HasExpectedValues()
    {
        Assert.Equal(0, (int)CapacityAllocationType.Production);
        Assert.Equal(1, (int)CapacityAllocationType.Maintenance);
        Assert.Equal(2, (int)CapacityAllocationType.Buffer);
        Assert.Equal(3, (int)CapacityAllocationType.Reserved);
    }

    [Fact]
    public void CapacityConstraintType_HasExpectedValues()
    {
        Assert.Equal(0, (int)CapacityConstraintType.MaxHoursPerDay);
        Assert.Equal(1, (int)CapacityConstraintType.MaxHoursPerWeek);
        Assert.Equal(2, (int)CapacityConstraintType.MaxRunsPerDay);
        Assert.Equal(3, (int)CapacityConstraintType.MaxProofGallonsPerRun);
        Assert.Equal(4, (int)CapacityConstraintType.MinTimeBetweenRuns);
        Assert.Equal(5, (int)CapacityConstraintType.MaxConcurrentRuns);
    }

    #endregion

    #region Company Scoping Tests

    [Fact]
    public async Task CapacityPlan_FilterByCompany_Works()
    {
        var company1 = await CreateTestCompanyAsync();
        var company2 = new Company
        {
            CompanyName = "Second Distillery",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Companies.Add(company2);
        await _context.SaveChangesAsync();

        var plan1 = new CapacityPlan
        {
            CompanyId = company1.Id,
            Name = "Company 1 Plan",
            PlanPeriodStart = DateTime.UtcNow,
            PlanPeriodEnd = DateTime.UtcNow.AddMonths(1),
            PlanType = CapacityPlanType.Monthly,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var plan2 = new CapacityPlan
        {
            CompanyId = company2.Id,
            Name = "Company 2 Plan",
            PlanPeriodStart = DateTime.UtcNow,
            PlanPeriodEnd = DateTime.UtcNow.AddMonths(1),
            PlanType = CapacityPlanType.Monthly,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.CapacityPlans.AddRange(plan1, plan2);
        await _context.SaveChangesAsync();

        var company1Plans = await _context.CapacityPlans
            .Where(p => p.CompanyId == company1.Id)
            .ToListAsync();

        Assert.Single(company1Plans);
        Assert.Equal("Company 1 Plan", company1Plans[0].Name);
    }

    #endregion
}
