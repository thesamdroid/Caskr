using Caskr.server.Models;
using Caskr.server.Models.Production;
using Microsoft.EntityFrameworkCore;

namespace Caskr.Server.Tests;

public class ProductionModelConfigurationTests : IDisposable
{
    private readonly CaskrDbContext _context;

    public ProductionModelConfigurationTests()
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

    #endregion

    #region ProductionRun Tests

    [Fact]
    public async Task ProductionRun_CanBeCreated()
    {
        var company = await CreateTestCompanyAsync();

        var run = new ProductionRun
        {
            CompanyId = company.Id,
            Name = "Test Mash Run #1",
            Description = "First test mash run",
            ProductionType = ProductionType.Mashing,
            Status = ProductionRunStatus.Scheduled,
            ScheduledStartDate = DateTime.UtcNow.AddDays(1),
            ScheduledEndDate = DateTime.UtcNow.AddDays(1).AddHours(4),
            Notes = "Test notes",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ProductionRuns.Add(run);
        await _context.SaveChangesAsync();

        var savedRun = await _context.ProductionRuns.FindAsync(run.Id);
        Assert.NotNull(savedRun);
        Assert.Equal("Test Mash Run #1", savedRun.Name);
        Assert.Equal(ProductionType.Mashing, savedRun.ProductionType);
        Assert.Equal(ProductionRunStatus.Scheduled, savedRun.Status);
    }

    [Fact]
    public async Task ProductionRun_WithCreatedByUser_CanBeCreated()
    {
        var company = await CreateTestCompanyAsync();
        var user = await CreateTestUserAsync(company.Id);

        var run = new ProductionRun
        {
            CompanyId = company.Id,
            Name = "Test Run with User",
            ProductionType = ProductionType.Distillation,
            ScheduledStartDate = DateTime.UtcNow.AddDays(1),
            ScheduledEndDate = DateTime.UtcNow.AddDays(1).AddHours(8),
            CreatedByUserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ProductionRuns.Add(run);
        await _context.SaveChangesAsync();

        var savedRun = await _context.ProductionRuns
            .Include(r => r.CreatedByUser)
            .FirstOrDefaultAsync(r => r.Id == run.Id);

        Assert.NotNull(savedRun);
        Assert.NotNull(savedRun.CreatedByUser);
        Assert.Equal("Test User", savedRun.CreatedByUser.Name);
    }

    [Fact]
    public async Task ProductionRun_StatusTransitions_WorkCorrectly()
    {
        var company = await CreateTestCompanyAsync();

        var run = new ProductionRun
        {
            CompanyId = company.Id,
            Name = "Status Test Run",
            ProductionType = ProductionType.Fermentation,
            Status = ProductionRunStatus.Scheduled,
            ScheduledStartDate = DateTime.UtcNow,
            ScheduledEndDate = DateTime.UtcNow.AddHours(24),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ProductionRuns.Add(run);
        await _context.SaveChangesAsync();

        // Transition to InProgress
        run.Status = ProductionRunStatus.InProgress;
        run.ActualStartDate = DateTime.UtcNow;
        run.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var inProgressRun = await _context.ProductionRuns.FindAsync(run.Id);
        Assert.NotNull(inProgressRun);
        Assert.Equal(ProductionRunStatus.InProgress, inProgressRun.Status);
        Assert.NotNull(inProgressRun.ActualStartDate);

        // Transition to Completed
        run.Status = ProductionRunStatus.Completed;
        run.ActualEndDate = DateTime.UtcNow;
        run.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var completedRun = await _context.ProductionRuns.FindAsync(run.Id);
        Assert.NotNull(completedRun);
        Assert.Equal(ProductionRunStatus.Completed, completedRun.Status);
        Assert.NotNull(completedRun.ActualEndDate);
    }

    #endregion

    #region Equipment Tests

    [Fact]
    public async Task Equipment_CanBeCreated()
    {
        var company = await CreateTestCompanyAsync();

        var equipment = new Equipment
        {
            CompanyId = company.Id,
            Name = "Still #1",
            EquipmentType = EquipmentType.Still,
            Capacity = 500,
            CapacityUnit = "gallons",
            Location = "Building A",
            IsActive = true,
            MaintenanceNotes = "Annual maintenance due",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Equipment.Add(equipment);
        await _context.SaveChangesAsync();

        var savedEquipment = await _context.Equipment.FindAsync(equipment.Id);
        Assert.NotNull(savedEquipment);
        Assert.Equal("Still #1", savedEquipment.Name);
        Assert.Equal(EquipmentType.Still, savedEquipment.EquipmentType);
        Assert.Equal(500, savedEquipment.Capacity);
        Assert.Equal("gallons", savedEquipment.CapacityUnit);
    }

    [Fact]
    public async Task Equipment_FilterByType_Works()
    {
        var company = await CreateTestCompanyAsync();

        var still = new Equipment
        {
            CompanyId = company.Id,
            Name = "Still #1",
            EquipmentType = EquipmentType.Still,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var fermenter = new Equipment
        {
            CompanyId = company.Id,
            Name = "Fermenter #1",
            EquipmentType = EquipmentType.Fermenter,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var tank = new Equipment
        {
            CompanyId = company.Id,
            Name = "Tank #1",
            EquipmentType = EquipmentType.Tank,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Equipment.AddRange(still, fermenter, tank);
        await _context.SaveChangesAsync();

        var stills = await _context.Equipment
            .Where(e => e.EquipmentType == EquipmentType.Still)
            .ToListAsync();

        Assert.Single(stills);
        Assert.Equal("Still #1", stills[0].Name);
    }

    [Fact]
    public async Task Equipment_IsActiveFilter_Works()
    {
        var company = await CreateTestCompanyAsync();

        var activeEquipment = new Equipment
        {
            CompanyId = company.Id,
            Name = "Active Equipment",
            EquipmentType = EquipmentType.MashTun,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var inactiveEquipment = new Equipment
        {
            CompanyId = company.Id,
            Name = "Inactive Equipment",
            EquipmentType = EquipmentType.MashTun,
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Equipment.AddRange(activeEquipment, inactiveEquipment);
        await _context.SaveChangesAsync();

        var activeOnly = await _context.Equipment
            .Where(e => e.IsActive)
            .ToListAsync();

        Assert.Single(activeOnly);
        Assert.Equal("Active Equipment", activeOnly[0].Name);
    }

    #endregion

    #region EquipmentBooking Tests

    [Fact]
    public async Task EquipmentBooking_CanBeCreated()
    {
        var company = await CreateTestCompanyAsync();

        var run = new ProductionRun
        {
            CompanyId = company.Id,
            Name = "Booking Test Run",
            ProductionType = ProductionType.Distillation,
            ScheduledStartDate = DateTime.UtcNow.AddDays(1),
            ScheduledEndDate = DateTime.UtcNow.AddDays(1).AddHours(8),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.ProductionRuns.Add(run);

        var equipment = new Equipment
        {
            CompanyId = company.Id,
            Name = "Booking Test Still",
            EquipmentType = EquipmentType.Still,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Equipment.Add(equipment);
        await _context.SaveChangesAsync();

        var booking = new EquipmentBooking
        {
            ProductionRunId = run.Id,
            EquipmentId = equipment.Id,
            StartTime = run.ScheduledStartDate,
            EndTime = run.ScheduledEndDate,
            Status = EquipmentBookingStatus.Confirmed,
            Notes = "Booking notes",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.EquipmentBookings.Add(booking);
        await _context.SaveChangesAsync();

        var savedBooking = await _context.EquipmentBookings.FindAsync(booking.Id);
        Assert.NotNull(savedBooking);
        Assert.Equal(run.Id, savedBooking.ProductionRunId);
        Assert.Equal(equipment.Id, savedBooking.EquipmentId);
        Assert.Equal(EquipmentBookingStatus.Confirmed, savedBooking.Status);
    }

    [Fact]
    public async Task EquipmentBooking_NavigationProperties_Work()
    {
        var company = await CreateTestCompanyAsync();

        var run = new ProductionRun
        {
            CompanyId = company.Id,
            Name = "Navigation Test Run",
            ProductionType = ProductionType.Mashing,
            ScheduledStartDate = DateTime.UtcNow.AddDays(1),
            ScheduledEndDate = DateTime.UtcNow.AddDays(1).AddHours(4),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.ProductionRuns.Add(run);

        var equipment = new Equipment
        {
            CompanyId = company.Id,
            Name = "Navigation Test Equipment",
            EquipmentType = EquipmentType.MashTun,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Equipment.Add(equipment);
        await _context.SaveChangesAsync();

        var booking = new EquipmentBooking
        {
            ProductionRunId = run.Id,
            EquipmentId = equipment.Id,
            StartTime = run.ScheduledStartDate,
            EndTime = run.ScheduledEndDate,
            Status = EquipmentBookingStatus.Tentative,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.EquipmentBookings.Add(booking);
        await _context.SaveChangesAsync();

        var savedBooking = await _context.EquipmentBookings
            .Include(b => b.ProductionRun)
            .Include(b => b.Equipment)
            .FirstOrDefaultAsync(b => b.Id == booking.Id);

        Assert.NotNull(savedBooking);
        Assert.NotNull(savedBooking.ProductionRun);
        Assert.NotNull(savedBooking.Equipment);
        Assert.Equal("Navigation Test Run", savedBooking.ProductionRun.Name);
        Assert.Equal("Navigation Test Equipment", savedBooking.Equipment.Name);
    }

    [Fact]
    public async Task EquipmentBooking_CascadeDeleteWithProductionRun()
    {
        var company = await CreateTestCompanyAsync();

        var run = new ProductionRun
        {
            CompanyId = company.Id,
            Name = "Cascade Delete Test Run",
            ProductionType = ProductionType.Bottling,
            ScheduledStartDate = DateTime.UtcNow.AddDays(1),
            ScheduledEndDate = DateTime.UtcNow.AddDays(1).AddHours(6),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.ProductionRuns.Add(run);

        var equipment = new Equipment
        {
            CompanyId = company.Id,
            Name = "Cascade Test Equipment",
            EquipmentType = EquipmentType.BottlingLine,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Equipment.Add(equipment);
        await _context.SaveChangesAsync();

        var booking = new EquipmentBooking
        {
            ProductionRunId = run.Id,
            EquipmentId = equipment.Id,
            StartTime = run.ScheduledStartDate,
            EndTime = run.ScheduledEndDate,
            Status = EquipmentBookingStatus.Confirmed,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.EquipmentBookings.Add(booking);
        await _context.SaveChangesAsync();

        var bookingId = booking.Id;

        // Delete the production run
        _context.ProductionRuns.Remove(run);
        await _context.SaveChangesAsync();

        // Verify booking is also deleted
        var deletedBooking = await _context.EquipmentBookings.FindAsync(bookingId);
        Assert.Null(deletedBooking);
    }

    #endregion

    #region ProductionCalendarEvent Tests

    [Fact]
    public async Task ProductionCalendarEvent_CanBeCreated()
    {
        var company = await CreateTestCompanyAsync();

        var calendarEvent = new ProductionCalendarEvent
        {
            CompanyId = company.Id,
            Title = "Maintenance Window",
            EventType = CalendarEventType.Maintenance,
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(4),
            AllDay = false,
            Color = "#FF6B6B",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ProductionCalendarEvents.Add(calendarEvent);
        await _context.SaveChangesAsync();

        var savedEvent = await _context.ProductionCalendarEvents.FindAsync(calendarEvent.Id);
        Assert.NotNull(savedEvent);
        Assert.Equal("Maintenance Window", savedEvent.Title);
        Assert.Equal(CalendarEventType.Maintenance, savedEvent.EventType);
        Assert.Equal("#FF6B6B", savedEvent.Color);
    }

    [Fact]
    public async Task ProductionCalendarEvent_AllDayEvent_CanBeCreated()
    {
        var company = await CreateTestCompanyAsync();

        var allDayEvent = new ProductionCalendarEvent
        {
            CompanyId = company.Id,
            Title = "Company Holiday",
            EventType = CalendarEventType.Other,
            StartDate = DateTime.UtcNow.AddDays(14).Date,
            EndDate = DateTime.UtcNow.AddDays(14).Date.AddDays(1),
            AllDay = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ProductionCalendarEvents.Add(allDayEvent);
        await _context.SaveChangesAsync();

        var savedEvent = await _context.ProductionCalendarEvents.FindAsync(allDayEvent.Id);
        Assert.NotNull(savedEvent);
        Assert.True(savedEvent.AllDay);
    }

    [Fact]
    public async Task ProductionCalendarEvent_LinkedToProductionRun_Works()
    {
        var company = await CreateTestCompanyAsync();

        var run = new ProductionRun
        {
            CompanyId = company.Id,
            Name = "Calendar Linked Run",
            ProductionType = ProductionType.Distillation,
            ScheduledStartDate = DateTime.UtcNow.AddDays(2),
            ScheduledEndDate = DateTime.UtcNow.AddDays(2).AddHours(8),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.ProductionRuns.Add(run);
        await _context.SaveChangesAsync();

        var calendarEvent = new ProductionCalendarEvent
        {
            CompanyId = company.Id,
            Title = run.Name,
            EventType = CalendarEventType.ProductionRun,
            StartDate = run.ScheduledStartDate,
            EndDate = run.ScheduledEndDate,
            ProductionRunId = run.Id,
            Color = "#FF6B6B", // Distillation color
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.ProductionCalendarEvents.Add(calendarEvent);
        await _context.SaveChangesAsync();

        var savedEvent = await _context.ProductionCalendarEvents
            .Include(e => e.ProductionRun)
            .FirstOrDefaultAsync(e => e.Id == calendarEvent.Id);

        Assert.NotNull(savedEvent);
        Assert.NotNull(savedEvent.ProductionRun);
        Assert.Equal("Calendar Linked Run", savedEvent.ProductionRun.Name);
    }

    [Fact]
    public async Task ProductionCalendarEvent_FilterByDateRange_Works()
    {
        var company = await CreateTestCompanyAsync();

        var event1 = new ProductionCalendarEvent
        {
            CompanyId = company.Id,
            Title = "Event in Range",
            EventType = CalendarEventType.Meeting,
            StartDate = DateTime.UtcNow.AddDays(5),
            EndDate = DateTime.UtcNow.AddDays(5).AddHours(2),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var event2 = new ProductionCalendarEvent
        {
            CompanyId = company.Id,
            Title = "Event Out of Range",
            EventType = CalendarEventType.Deadline,
            StartDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow.AddDays(30).AddHours(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ProductionCalendarEvents.AddRange(event1, event2);
        await _context.SaveChangesAsync();

        var rangeStart = DateTime.UtcNow;
        var rangeEnd = DateTime.UtcNow.AddDays(10);

        var eventsInRange = await _context.ProductionCalendarEvents
            .Where(e => e.StartDate >= rangeStart && e.StartDate <= rangeEnd)
            .ToListAsync();

        Assert.Single(eventsInRange);
        Assert.Equal("Event in Range", eventsInRange[0].Title);
    }

    #endregion

    #region Enum Tests

    [Fact]
    public void ProductionType_HasExpectedValues()
    {
        Assert.Equal(0, (int)ProductionType.Mashing);
        Assert.Equal(1, (int)ProductionType.Fermentation);
        Assert.Equal(2, (int)ProductionType.Distillation);
        Assert.Equal(3, (int)ProductionType.Barreling);
        Assert.Equal(4, (int)ProductionType.Bottling);
        Assert.Equal(5, (int)ProductionType.Other);
    }

    [Fact]
    public void ProductionRunStatus_HasExpectedValues()
    {
        Assert.Equal(0, (int)ProductionRunStatus.Scheduled);
        Assert.Equal(1, (int)ProductionRunStatus.InProgress);
        Assert.Equal(2, (int)ProductionRunStatus.Completed);
        Assert.Equal(3, (int)ProductionRunStatus.Cancelled);
    }

    [Fact]
    public void EquipmentType_HasExpectedValues()
    {
        Assert.Equal(0, (int)EquipmentType.Still);
        Assert.Equal(1, (int)EquipmentType.Fermenter);
        Assert.Equal(2, (int)EquipmentType.MashTun);
        Assert.Equal(3, (int)EquipmentType.BottlingLine);
        Assert.Equal(4, (int)EquipmentType.Labeler);
        Assert.Equal(5, (int)EquipmentType.Tank);
        Assert.Equal(6, (int)EquipmentType.Other);
    }

    [Fact]
    public void EquipmentBookingStatus_HasExpectedValues()
    {
        Assert.Equal(0, (int)EquipmentBookingStatus.Tentative);
        Assert.Equal(1, (int)EquipmentBookingStatus.Confirmed);
        Assert.Equal(2, (int)EquipmentBookingStatus.Cancelled);
    }

    [Fact]
    public void CalendarEventType_HasExpectedValues()
    {
        Assert.Equal(0, (int)CalendarEventType.ProductionRun);
        Assert.Equal(1, (int)CalendarEventType.Maintenance);
        Assert.Equal(2, (int)CalendarEventType.Meeting);
        Assert.Equal(3, (int)CalendarEventType.Deadline);
        Assert.Equal(4, (int)CalendarEventType.Other);
    }

    #endregion

    #region Company Scoping Tests

    [Fact]
    public async Task ProductionRun_FilterByCompany_Works()
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

        var run1 = new ProductionRun
        {
            CompanyId = company1.Id,
            Name = "Company 1 Run",
            ProductionType = ProductionType.Mashing,
            ScheduledStartDate = DateTime.UtcNow.AddDays(1),
            ScheduledEndDate = DateTime.UtcNow.AddDays(1).AddHours(4),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var run2 = new ProductionRun
        {
            CompanyId = company2.Id,
            Name = "Company 2 Run",
            ProductionType = ProductionType.Distillation,
            ScheduledStartDate = DateTime.UtcNow.AddDays(2),
            ScheduledEndDate = DateTime.UtcNow.AddDays(2).AddHours(8),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ProductionRuns.AddRange(run1, run2);
        await _context.SaveChangesAsync();

        var company1Runs = await _context.ProductionRuns
            .Where(r => r.CompanyId == company1.Id)
            .ToListAsync();

        Assert.Single(company1Runs);
        Assert.Equal("Company 1 Run", company1Runs[0].Name);
    }

    #endregion
}
