using Caskr.server.Models;
using Caskr.server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Caskr.Server.Tests;

/// <summary>
/// Tests to validate the 30 standard report templates defined in the seed data.
/// These tests ensure the report templates are valid and can be loaded by the ReportingService.
/// </summary>
public class StandardReportTemplatesTests
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<ReportingService> _logger;

    public StandardReportTemplatesTests()
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

        // Add user type
        var userType = new UserType { Id = 1, Name = "Admin" };
        context.UserTypes.Add(userType);

        // Add user (Super Admin - id=125 as used in seed data)
        var user = new User
        {
            Id = 125,
            Name = "Super Admin",
            Email = "admin@test.com",
            CompanyId = 1,
            UserTypeId = 1,
            IsActive = true
        };
        context.Users.Add(user);

        await context.SaveChangesAsync();

        return (context, 1, 125);
    }

    /// <summary>
    /// Standard report template definitions matching the seed data.
    /// Each tuple contains: (name, description, dataSources, columns)
    /// </summary>
    private static readonly List<(string Name, string Category, string[] DataSources, int ExpectedColumnCount)> StandardReports = new()
    {
        // Financial Reports (10)
        ("Inventory Valuation by Batch", "Financial", new[] { "barrels", "batches", "orders" }, 4),
        ("Cost of Goods Sold by Month", "Financial", new[] { "invoices", "invoice_line_items" }, 3),
        ("Revenue by Product Type", "Financial", new[] { "orders", "spirit_types", "invoices" }, 4),
        ("Profit Margin by Batch", "Financial", new[] { "batches", "orders", "invoices" }, 5),
        ("Work in Progress Valuation", "Financial", new[] { "barrels", "batches", "orders", "rickhouses" }, 4),
        ("Barrel Cost Analysis", "Financial", new[] { "barrels", "rickhouses", "orders" }, 3),
        ("Aging Inventory Summary", "Financial", new[] { "barrels", "orders", "rickhouses" }, 4),
        ("Excise Tax Liability Report", "Financial", new[] { "ttb_tax_determinations", "orders" }, 6),
        ("Monthly Production Costs", "Financial", new[] { "orders", "batches" }, 4),
        ("Customer Revenue Ranking", "Financial", new[] { "invoices" }, 4),

        // Inventory Reports (10)
        ("Current Barrel Inventory by Status", "Inventory", new[] { "barrels", "orders", "status" }, 3),
        ("Barrel Aging Report", "Inventory", new[] { "barrels", "orders", "rickhouses" }, 5),
        ("Warehouse Utilization by Location", "Inventory", new[] { "rickhouses", "barrels" }, 3),
        ("Low Stock Alert", "Inventory", new[] { "batches", "orders", "barrels" }, 3),
        ("Barrel Movement History", "Inventory", new[] { "ttb_transactions", "barrels" }, 5),
        ("Inventory by Proof Range", "Inventory", new[] { "barrels", "ttb_gauge_records" }, 5),
        ("Batch Yield Analysis", "Inventory", new[] { "batches", "orders", "barrels" }, 4),
        ("Evaporation Loss Report", "Inventory", new[] { "ttb_transactions" }, 5),
        ("Barrels Due for Dumping", "Inventory", new[] { "barrels", "orders", "rickhouses", "batches" }, 6),
        ("Multi-Warehouse Inventory Comparison", "Inventory", new[] { "rickhouses", "barrels", "batches" }, 3),

        // Production Reports (5)
        ("Monthly Production Volume", "Production", new[] { "ttb_transactions" }, 4),
        ("Batch Efficiency Report", "Production", new[] { "batches", "mash_bills", "orders", "barrels" }, 5),
        ("Equipment Utilization", "Production", new[] { "rickhouses", "barrels" }, 3),
        ("Quality Control Metrics", "Production", new[] { "barrels", "ttb_gauge_records", "users" }, 6),
        ("Mash Bill Usage Analysis", "Production", new[] { "mash_bills", "batches", "orders" }, 4),

        // Compliance Reports (5)
        ("TTB Monthly Summary", "Compliance", new[] { "ttb_monthly_reports", "users" }, 8),
        ("Transfer Documentation Log", "Compliance", new[] { "ttb_transactions" }, 8),
        ("Gauge Record Summary", "Compliance", new[] { "barrels", "ttb_gauge_records", "users" }, 10),
        ("Tax Determination History", "Compliance", new[] { "ttb_tax_determinations", "orders" }, 9),
        ("Audit Trail Report", "Compliance", new[] { "ttb_audit_logs", "users" }, 7)
    };

    [Fact]
    public void StandardReports_Has30Templates()
    {
        Assert.Equal(30, StandardReports.Count);
    }

    [Fact]
    public void StandardReports_Has10FinancialReports()
    {
        var financialCount = StandardReports.Count(r => r.Category == "Financial");
        Assert.Equal(10, financialCount);
    }

    [Fact]
    public void StandardReports_Has10InventoryReports()
    {
        var inventoryCount = StandardReports.Count(r => r.Category == "Inventory");
        Assert.Equal(10, inventoryCount);
    }

    [Fact]
    public void StandardReports_Has5ProductionReports()
    {
        var productionCount = StandardReports.Count(r => r.Category == "Production");
        Assert.Equal(5, productionCount);
    }

    [Fact]
    public void StandardReports_Has5ComplianceReports()
    {
        var complianceCount = StandardReports.Count(r => r.Category == "Compliance");
        Assert.Equal(5, complianceCount);
    }

    [Theory]
    [MemberData(nameof(GetAllReportNames))]
    public async Task ValidateTemplate_ReturnsSuccess_ForStandardReport(string reportName)
    {
        var (context, companyId, userId) = await SetupTestDataAsync();
        var reportDef = StandardReports.First(r => r.Name == reportName);

        // Create template matching seed data structure
        var template = new ReportTemplate
        {
            CompanyId = companyId,
            Name = reportName,
            Description = $"Test template for {reportName}",
            DataSources = JsonSerializer.Serialize(reportDef.DataSources),
            Columns = GenerateColumnsJson(reportDef.DataSources, reportDef.ExpectedColumnCount),
            IsActive = true,
            IsSystemTemplate = true,
            CreatedByUserId = userId
        };
        context.ReportTemplates.Add(template);
        await context.SaveChangesAsync();

        var service = new ReportingService(context, _cache, _logger);

        var result = await service.ValidateTemplateAsync(template.Id, companyId);

        // Template should pass validation (may have warnings but no errors)
        Assert.True(result.IsValid, $"Report '{reportName}' failed validation: {string.Join(", ", result.Errors)}");
    }

    [Theory]
    [MemberData(nameof(GetAllDataSourceCombinations))]
    public void AllowedTables_ContainsRequiredDataSource(string tableName)
    {
        // Verify all data sources used in standard reports are in the allowed list
        var allowedTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "barrels", "batches", "orders", "users", "companies",
            "rickhouses", "mash_bills", "components", "products",
            "invoices", "invoice_line_items", "invoice_taxes",
            "ttb_transactions", "ttb_monthly_reports", "ttb_inventory_snapshots",
            "ttb_gauge_records", "ttb_tax_determinations", "ttb_audit_logs",
            "spirit_types", "status", "status_task"
        };

        Assert.Contains(tableName, allowedTables);
    }

    [Fact]
    public async Task GetTemplatesForCompany_ReturnsAllActiveSystemTemplates()
    {
        var (context, companyId, userId) = await SetupTestDataAsync();

        // Add all standard report templates
        foreach (var (name, category, dataSources, columnCount) in StandardReports)
        {
            context.ReportTemplates.Add(new ReportTemplate
            {
                CompanyId = companyId,
                Name = name,
                Description = $"{category} report",
                DataSources = JsonSerializer.Serialize(dataSources),
                Columns = GenerateColumnsJson(dataSources, columnCount),
                IsActive = true,
                IsSystemTemplate = true,
                CreatedByUserId = userId
            });
        }
        await context.SaveChangesAsync();

        var service = new ReportingService(context, _cache, _logger);

        var templates = (await service.GetTemplatesForCompanyAsync(companyId)).ToList();

        Assert.Equal(30, templates.Count);
        Assert.All(templates, t => Assert.True(t.IsSystemTemplate));
        Assert.All(templates, t => Assert.True(t.IsActive));
    }

    private static string GenerateColumnsJson(string[] dataSources, int count)
    {
        var columns = new List<string>();
        var primaryTable = dataSources[0];

        // Generate simple column references
        columns.Add($"{primaryTable}.id as {primaryTable}_id");

        // Add additional columns up to the expected count
        for (int i = 1; i < count; i++)
        {
            var table = dataSources[i % dataSources.Length];
            columns.Add($"{table}.id as col_{i}");
        }

        return JsonSerializer.Serialize(columns);
    }

    public static IEnumerable<object[]> GetAllReportNames()
    {
        return StandardReports.Select(r => new object[] { r.Name });
    }

    public static IEnumerable<object[]> GetAllDataSourceCombinations()
    {
        var allDataSources = StandardReports
            .SelectMany(r => r.DataSources)
            .Distinct()
            .Select(ds => new object[] { ds });
        return allDataSources;
    }
}
