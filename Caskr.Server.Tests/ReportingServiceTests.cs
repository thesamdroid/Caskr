using Caskr.server.Models;
using Caskr.server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace Caskr.Server.Tests;

public class ReportingServiceTests
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<ReportingService> _logger;

    public ReportingServiceTests()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
        _logger = new LoggerFactory().CreateLogger<ReportingService>();
    }

    private CaskrDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CaskrDbContext(options);
    }

    private async Task<(CaskrDbContext context, int companyId, int userId)> SetupTestDataAsync()
    {
        var context = CreateInMemoryContext();

        // Add company
        var company = new Company
        {
            Id = 1,
            CompanyName = "Test Distillery",
            CreatedAt = DateTime.UtcNow,
            RenewalDate = DateTime.UtcNow.AddYears(1)
        };
        context.Companies.Add(company);

        // Add user
        var userType = new UserType { Id = 1, Name = "Admin" };
        context.UserTypes.Add(userType);

        var user = new User
        {
            Id = 1,
            Name = "Test User",
            Email = "test@test.com",
            CompanyId = 1,
            UserTypeId = 1,
            IsActive = true
        };
        context.Users.Add(user);

        // Add rickhouse
        var rickhouse = new Rickhouse
        {
            Id = 1,
            CompanyId = 1,
            Name = "Rickhouse A",
            Address = "123 Test St"
        };
        context.Rickhouses.Add(rickhouse);

        // Add batches
        var mashBill = new MashBill
        {
            Id = 1,
            CompanyId = 1,
            Name = "Bourbon Mash"
        };
        context.MashBills.Add(mashBill);

        var batch = new Batch
        {
            Id = 1,
            CompanyId = 1,
            MashBillId = 1
        };
        context.Batches.Add(batch);

        // Add status
        var status = new Status { Id = 1, Name = "Aging" };
        context.Statuses.Add(status);

        // Add spirit type
        var spiritType = new SpiritType { Id = 1, Name = "Bourbon" };
        context.SpiritTypes.Add(spiritType);

        // Add order
        var order = new Order
        {
            Id = 1,
            Name = "Test Order",
            CompanyId = 1,
            OwnerId = 1,
            StatusId = 1,
            SpiritTypeId = 1,
            BatchId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Orders.Add(order);

        // Add barrels
        var barrels = new[]
        {
            new Barrel { Id = 1, CompanyId = 1, Sku = "BBL-001", BatchId = 1, OrderId = 1, RickhouseId = 1 },
            new Barrel { Id = 2, CompanyId = 1, Sku = "BBL-002", BatchId = 1, OrderId = 1, RickhouseId = 1 },
            new Barrel { Id = 3, CompanyId = 1, Sku = "BBL-003", BatchId = 1, OrderId = 1, RickhouseId = 1 }
        };
        context.Barrels.AddRange(barrels);

        await context.SaveChangesAsync();

        return (context, 1, 1);
    }

    [Fact]
    public async Task GetTemplatesForCompanyAsync_ReturnsOnlyActiveTemplates()
    {
        var (context, companyId, userId) = await SetupTestDataAsync();

        context.ReportTemplates.AddRange(
            new ReportTemplate
            {
                CompanyId = companyId,
                Name = "Active Template",
                DataSources = "[\"barrels\"]",
                Columns = "[\"barrels.sku\"]",
                IsActive = true,
                CreatedByUserId = userId
            },
            new ReportTemplate
            {
                CompanyId = companyId,
                Name = "Inactive Template",
                DataSources = "[\"barrels\"]",
                Columns = "[\"barrels.sku\"]",
                IsActive = false,
                CreatedByUserId = userId
            }
        );
        await context.SaveChangesAsync();

        var service = new ReportingService(context, _cache, _logger);

        var templates = await service.GetTemplatesForCompanyAsync(companyId);

        Assert.Single(templates);
        Assert.Equal("Active Template", templates.First().Name);
    }

    [Fact]
    public async Task GetTemplatesForCompanyAsync_DoesNotReturnOtherCompanyTemplates()
    {
        var (context, companyId, userId) = await SetupTestDataAsync();

        // Add another company
        var company2 = new Company
        {
            Id = 2,
            CompanyName = "Other Distillery",
            CreatedAt = DateTime.UtcNow,
            RenewalDate = DateTime.UtcNow.AddYears(1)
        };
        context.Companies.Add(company2);

        context.ReportTemplates.AddRange(
            new ReportTemplate
            {
                CompanyId = companyId,
                Name = "Company 1 Template",
                DataSources = "[\"barrels\"]",
                Columns = "[\"barrels.sku\"]",
                IsActive = true,
                CreatedByUserId = userId
            },
            new ReportTemplate
            {
                CompanyId = 2,
                Name = "Company 2 Template",
                DataSources = "[\"barrels\"]",
                Columns = "[\"barrels.sku\"]",
                IsActive = true,
                CreatedByUserId = userId
            }
        );
        await context.SaveChangesAsync();

        var service = new ReportingService(context, _cache, _logger);

        var templates = await service.GetTemplatesForCompanyAsync(companyId);

        Assert.Single(templates);
        Assert.Equal("Company 1 Template", templates.First().Name);
    }

    [Fact]
    public async Task ValidateTemplateAsync_ReturnsError_WhenTemplateNotFound()
    {
        var (context, companyId, _) = await SetupTestDataAsync();

        var service = new ReportingService(context, _cache, _logger);

        var result = await service.ValidateTemplateAsync(999, companyId);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not found"));
    }

    [Fact]
    public async Task ValidateTemplateAsync_ReturnsError_WhenNoDataSources()
    {
        var (context, companyId, userId) = await SetupTestDataAsync();

        var template = new ReportTemplate
        {
            CompanyId = companyId,
            Name = "Invalid Template",
            DataSources = "[]",
            Columns = "[\"barrels.sku\"]",
            IsActive = true,
            CreatedByUserId = userId
        };
        context.ReportTemplates.Add(template);
        await context.SaveChangesAsync();

        var service = new ReportingService(context, _cache, _logger);

        var result = await service.ValidateTemplateAsync(template.Id, companyId);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("data source"));
    }

    [Fact]
    public async Task ValidateTemplateAsync_ReturnsError_WhenNoColumns()
    {
        var (context, companyId, userId) = await SetupTestDataAsync();

        var template = new ReportTemplate
        {
            CompanyId = companyId,
            Name = "Invalid Template",
            DataSources = "[\"barrels\"]",
            Columns = "[]",
            IsActive = true,
            CreatedByUserId = userId
        };
        context.ReportTemplates.Add(template);
        await context.SaveChangesAsync();

        var service = new ReportingService(context, _cache, _logger);

        var result = await service.ValidateTemplateAsync(template.Id, companyId);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("column"));
    }

    [Fact]
    public async Task ValidateTemplateAsync_ReturnsWarning_WhenColumnReferencesUnknownTable()
    {
        var (context, companyId, userId) = await SetupTestDataAsync();

        var template = new ReportTemplate
        {
            CompanyId = companyId,
            Name = "Template with Unknown Table Column",
            DataSources = "[\"barrels\"]",
            Columns = "[\"barrels.sku\", \"batches.id\"]",
            IsActive = true,
            CreatedByUserId = userId
        };
        context.ReportTemplates.Add(template);
        await context.SaveChangesAsync();

        var service = new ReportingService(context, _cache, _logger);

        var result = await service.ValidateTemplateAsync(template.Id, companyId);

        // Should pass validation but have warning about batches not in data sources
        Assert.Contains(result.Warnings, w => w.Contains("batches") && w.Contains("not in data sources"));
    }

    [Fact]
    public async Task ValidateTemplateAsync_ReturnsError_ForDisallowedTable()
    {
        var (context, companyId, userId) = await SetupTestDataAsync();

        var template = new ReportTemplate
        {
            CompanyId = companyId,
            Name = "Invalid Table Template",
            DataSources = "[\"forbidden_table\"]",
            Columns = "[\"forbidden_table.id\"]",
            IsActive = true,
            CreatedByUserId = userId
        };
        context.ReportTemplates.Add(template);
        await context.SaveChangesAsync();

        var service = new ReportingService(context, _cache, _logger);

        var result = await service.ValidateTemplateAsync(template.Id, companyId);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not allowed"));
    }

    [Fact]
    public async Task ValidateTemplateAsync_ReturnsError_ForDangerousSqlKeyword()
    {
        var (context, companyId, userId) = await SetupTestDataAsync();

        var template = new ReportTemplate
        {
            CompanyId = companyId,
            Name = "SQL Injection Template",
            DataSources = "[\"barrels\"]",
            Columns = "[\"barrels.sku\"]",
            Filters = "{\"filter\": \"barrels.sku = @sku; DROP TABLE barrels;\", \"defaultParameters\": {}}",
            IsActive = true,
            CreatedByUserId = userId
        };
        context.ReportTemplates.Add(template);
        await context.SaveChangesAsync();

        var service = new ReportingService(context, _cache, _logger);

        var result = await service.ValidateTemplateAsync(template.Id, companyId);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("DROP"));
    }

    [Fact]
    public async Task ValidateTemplateAsync_ReturnsSuccess_ForValidTemplate()
    {
        var (context, companyId, userId) = await SetupTestDataAsync();

        var template = new ReportTemplate
        {
            CompanyId = companyId,
            Name = "Valid Template",
            DataSources = "[\"barrels\"]",
            Columns = "[\"barrels.sku\", \"barrels.id\"]",
            Filters = "{\"filter\": \"barrels.sku = @sku\", \"defaultParameters\": {\"sku\": \"test\"}}",
            IsActive = true,
            CreatedByUserId = userId
        };
        context.ReportTemplates.Add(template);
        await context.SaveChangesAsync();

        var service = new ReportingService(context, _cache, _logger);

        var result = await service.ValidateTemplateAsync(template.Id, companyId);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ExecuteReportAsync_ThrowsException_WhenTemplateNotFound()
    {
        var (context, companyId, _) = await SetupTestDataAsync();

        var service = new ReportingService(context, _cache, _logger);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ExecuteReportAsync(999, companyId));
    }

    [Fact]
    public async Task ExecuteReportAsync_ThrowsException_WhenAccessingOtherCompanyTemplate()
    {
        var (context, companyId, userId) = await SetupTestDataAsync();

        // Add template for another company
        var company2 = new Company
        {
            Id = 2,
            CompanyName = "Other Distillery",
            CreatedAt = DateTime.UtcNow,
            RenewalDate = DateTime.UtcNow.AddYears(1)
        };
        context.Companies.Add(company2);

        var template = new ReportTemplate
        {
            CompanyId = 2,
            Name = "Other Company Template",
            DataSources = "[\"barrels\"]",
            Columns = "[\"barrels.sku\"]",
            IsActive = true,
            CreatedByUserId = userId
        };
        context.ReportTemplates.Add(template);
        await context.SaveChangesAsync();

        var service = new ReportingService(context, _cache, _logger);

        // Company 1 tries to access company 2's template
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ExecuteReportAsync(template.Id, companyId));
    }

    [Fact]
    public async Task ExecuteReportAsync_ThrowsException_ForDisallowedTable()
    {
        var (context, companyId, userId) = await SetupTestDataAsync();

        var template = new ReportTemplate
        {
            CompanyId = companyId,
            Name = "Invalid Table Template",
            DataSources = "[\"hacker_table\"]",
            Columns = "[\"hacker_table.secret\"]",
            IsActive = true,
            CreatedByUserId = userId
        };
        context.ReportTemplates.Add(template);
        await context.SaveChangesAsync();

        var service = new ReportingService(context, _cache, _logger);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ExecuteReportAsync(template.Id, companyId));
    }

    [Fact]
    public void ReportResult_TotalPages_CalculatesCorrectly()
    {
        var result = new ReportResult
        {
            TotalRows = 55,
            PageSize = 10,
            Page = 1
        };

        Assert.Equal(6, result.TotalPages);
    }

    [Fact]
    public void ReportResult_TotalPages_ReturnsZero_WhenPageSizeIsZero()
    {
        var result = new ReportResult
        {
            TotalRows = 55,
            PageSize = 0,
            Page = 1
        };

        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public void ReportColumn_HasCorrectDefaults()
    {
        var column = new ReportColumn();

        Assert.Equal(string.Empty, column.Name);
        Assert.Equal(string.Empty, column.DisplayName);
        Assert.Equal("string", column.DataType);
        Assert.Equal(string.Empty, column.SourceColumn);
    }

    [Fact]
    public void ReportExecutionParameters_HasCorrectDefaults()
    {
        var parameters = new ReportExecutionParameters();

        Assert.NotNull(parameters.Parameters);
        Assert.Empty(parameters.Parameters);
        Assert.Equal(1, parameters.Page);
        Assert.Null(parameters.PageSize);
        Assert.Null(parameters.SortOverride);
    }

    [Fact]
    public void ReportFilterConfig_HasCorrectDefaults()
    {
        var config = new ReportFilterConfig();

        Assert.Equal(string.Empty, config.Filter);
        Assert.NotNull(config.DefaultParameters);
        Assert.Empty(config.DefaultParameters);
    }

    [Fact]
    public async Task ReportTemplate_DefaultValues_AreCorrect()
    {
        var (context, companyId, userId) = await SetupTestDataAsync();

        var template = new ReportTemplate
        {
            CompanyId = companyId,
            Name = "Test Template",
            CreatedByUserId = userId
        };

        Assert.Equal("[]", template.DataSources);
        Assert.Equal("[]", template.Columns);
        Assert.Equal(50, template.DefaultPageSize);
        Assert.True(template.IsActive);
        Assert.False(template.IsSystemTemplate);
    }
}
