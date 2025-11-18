using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Caskr.server.Controllers;
using Caskr.server.Models;
using Caskr.server.Services;
using Caskr.server;
using Caskr.Server.Services;
using Caskr.Server.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UserTypeEnum = Caskr.server.UserType;

namespace Caskr.Server.Tests;

public class QuickBooksControllerTests : IDisposable
{
    private readonly Mock<IQuickBooksAuthService> _authService = new();
    private readonly Mock<IUsersService> _usersService = new();
    private readonly Mock<IQuickBooksDataService> _quickBooksDataService = new();
    private readonly Mock<IQuickBooksInvoiceSyncService> _invoiceSyncService = new();
    private readonly IConfiguration _configuration;
    private IMemoryCache? _memoryCache;
    private CaskrDbContext? _context;

    public QuickBooksControllerTests()
    {
        var settings = new Dictionary<string, string?>
        {
            ["QuickBooks:ConnectSuccessRedirectUrl"] = "https://app.caskr.dev/accounting/quickbooks/success"
        };
        _configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
    }

    [Fact]
    public async Task Connect_WithValidRequest_ReturnsAuthorizationUrl()
    {
        var user = new User { Id = 100, CompanyId = 77, UserTypeId = (int)UserTypeEnum.Admin };
        var controller = CreateController(user);
        var authorizationUri = new Uri("https://quickbooks.example.com/authorize");
        _authService
            .Setup(s => s.GetAuthorizationUrlAsync(77, It.IsAny<string>()))
            .ReturnsAsync(authorizationUri);

        var result = await controller.Connect(new QuickBooksCompanyRequest { CompanyId = 77 });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<QuickBooksAuthUrlResponse>(ok.Value);
        Assert.Equal(authorizationUri.ToString(), payload.AuthUrl);
    }

    [Fact]
    public async Task Disconnect_UserWithoutCompanyAccess_ReturnsForbid()
    {
        var user = new User { Id = 200, CompanyId = 10, UserTypeId = (int)UserTypeEnum.Admin };
        var controller = CreateController(user);

        var response = await controller.Disconnect(new QuickBooksCompanyRequest { CompanyId = 11 });

        Assert.IsType<ForbidResult>(response);
        _authService.Verify(s => s.RevokeAccessAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Status_NoIntegration_ReturnsDisconnectedPayload()
    {
        var user = new User { Id = 300, CompanyId = 21, UserTypeId = (int)UserTypeEnum.Admin };
        var controller = CreateController(user);

        var result = await controller.GetStatus(21);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<QuickBooksStatusResponse>(ok.Value);
        Assert.False(payload.Connected);
        Assert.Null(payload.RealmId);
        Assert.Null(payload.ConnectedAt);
    }

    [Fact]
    public async Task GetAccounts_NoIntegration_ReturnsNotFound()
    {
        var user = new User { Id = 400, CompanyId = 55, UserTypeId = (int)UserTypeEnum.Admin };
        var controller = CreateController(user);

        var result = await controller.GetAccounts(user.CompanyId, false);

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        var payload = Assert.IsType<QuickBooksErrorResponse>(notFound.Value);
        Assert.Equal("QuickBooks not connected for this company", payload.Message);
    }

    [Fact]
    public async Task GetAccounts_WithIntegration_ReturnsAccounts()
    {
        var user = new User { Id = 500, CompanyId = 88, UserTypeId = (int)UserTypeEnum.Admin };
        var controller = CreateController(user);
        _context!.AccountingIntegrations.Add(new AccountingIntegration
        {
            CompanyId = user.CompanyId,
            Provider = AccountingProvider.QuickBooks,
            RealmId = "123",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        _context.SaveChanges();

        _quickBooksDataService
            .Setup(s => s.GetChartOfAccountsAsync(user.CompanyId, false))
            .ReturnsAsync(new List<QBOAccount>
            {
                new("77", "Cash", "Bank", "CashOnHand", true)
            });

        var result = await controller.GetAccounts(user.CompanyId, false);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var accounts = Assert.IsType<List<QuickBooksAccountResponse>>(ok.Value);
        Assert.Single(accounts);
        Assert.Equal("Cash", accounts[0].Name);
    }

    [Fact]
    public async Task GetAccounts_WithRefreshFlag_BypassesCache()
    {
        var user = new User { Id = 600, CompanyId = 99, UserTypeId = (int)UserTypeEnum.Admin };
        var controller = CreateController(user);
        _context!.AccountingIntegrations.Add(new AccountingIntegration
        {
            CompanyId = user.CompanyId,
            Provider = AccountingProvider.QuickBooks,
            RealmId = "123",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        _context.SaveChanges();

        _quickBooksDataService
            .Setup(s => s.GetChartOfAccountsAsync(user.CompanyId, true))
            .ReturnsAsync(new List<QBOAccount>());

        await controller.GetAccounts(user.CompanyId, true);

        _quickBooksDataService.Verify(s => s.GetChartOfAccountsAsync(user.CompanyId, true), Times.Once);
    }

    [Fact]
    public async Task SaveMappings_WithValidPayload_ReplacesExistingMappings()
    {
        var user = new User { Id = 700, CompanyId = 66, UserTypeId = (int)UserTypeEnum.Admin };
        var controller = CreateController(user);
        _context!.AccountingIntegrations.Add(new AccountingIntegration
        {
            CompanyId = user.CompanyId,
            Provider = AccountingProvider.QuickBooks,
            RealmId = "realm",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        _context.ChartOfAccountsMappings.Add(new ChartOfAccountsMapping
        {
            CompanyId = user.CompanyId,
            CaskrAccountType = CaskrAccountType.Cogs,
            QboAccountId = "old",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        _context.SaveChanges();

        var chartOfAccounts = Enum.GetValues<CaskrAccountType>()
            .Select((type, index) => new QBOAccount(index.ToString(), type.ToString(), "Expense", type.ToString(), true))
            .ToList();

        _quickBooksDataService
            .Setup(s => s.GetChartOfAccountsAsync(user.CompanyId, false))
            .ReturnsAsync(chartOfAccounts);

        var request = new QuickBooksMappingRequest
        {
            CompanyId = user.CompanyId,
            Mappings = Enum.GetValues<CaskrAccountType>()
                .Select((type, index) => new QuickBooksAccountMappingDto
                {
                    CaskrAccountType = type.ToString(),
                    QboAccountId = index.ToString(),
                    QboAccountName = $"{type} Account"
                })
                .ToList()
        };

        var result = await controller.SaveMappings(request);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<List<QuickBooksAccountMappingResponse>>(ok.Value);
        Assert.Equal(chartOfAccounts.Count, payload.Count);

        var savedMappings = _context.ChartOfAccountsMappings
            .Where(m => m.CompanyId == user.CompanyId)
            .ToList();
        Assert.Equal(chartOfAccounts.Count, savedMappings.Count);
        Assert.DoesNotContain(savedMappings, m => m.QboAccountId == "old");
        _quickBooksDataService.Verify(s => s.GetChartOfAccountsAsync(user.CompanyId, false), Times.Once);
    }

    [Fact]
    public async Task SaveMappings_WhenMissingAccountType_ReturnsBadRequest()
    {
        var user = new User { Id = 701, CompanyId = 44, UserTypeId = (int)UserTypeEnum.Admin };
        var controller = CreateController(user);
        _context!.AccountingIntegrations.Add(new AccountingIntegration
        {
            CompanyId = user.CompanyId,
            Provider = AccountingProvider.QuickBooks,
            RealmId = "realm",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        _context.SaveChanges();

        var mappings = Enum.GetValues<CaskrAccountType>()
            .Where(type => type != CaskrAccountType.Overhead)
            .Select((type, index) => new QuickBooksAccountMappingDto
            {
                CaskrAccountType = type.ToString(),
                QboAccountId = index.ToString(),
                QboAccountName = $"{type} Account"
            })
            .ToList();

        var request = new QuickBooksMappingRequest
        {
            CompanyId = user.CompanyId,
            Mappings = mappings
        };

        var result = await controller.SaveMappings(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var payload = Assert.IsType<QuickBooksErrorResponse>(badRequest.Value);
        Assert.Contains("Overhead", payload.Message);
        _quickBooksDataService.Verify(s => s.GetChartOfAccountsAsync(It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task GetSyncPreferences_WhenPreferenceMissing_ReturnsNotFound()
    {
        var user = new User { Id = 702, CompanyId = 52, UserTypeId = (int)UserTypeEnum.Admin };
        var controller = CreateController(user);
        _context!.AccountingIntegrations.Add(new AccountingIntegration
        {
            CompanyId = user.CompanyId,
            Provider = AccountingProvider.QuickBooks,
            RealmId = "realm",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        _context.SaveChanges();

        var result = await controller.GetSyncPreferences(user.CompanyId);

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        var payload = Assert.IsType<QuickBooksErrorResponse>(notFound.Value);
        Assert.Contains("preferences", payload.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveSyncPreferences_WithValidRequest_CreatesPreference()
    {
        var user = new User { Id = 703, CompanyId = 53, UserTypeId = (int)UserTypeEnum.Admin };
        var controller = CreateController(user);
        _context!.AccountingIntegrations.Add(new AccountingIntegration
        {
            CompanyId = user.CompanyId,
            Provider = AccountingProvider.QuickBooks,
            RealmId = "realm",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        _context.SaveChanges();

        var request = new QuickBooksSyncPreferencesRequest
        {
            CompanyId = user.CompanyId,
            AutoSyncInvoices = true,
            AutoSyncCogs = true,
            SyncFrequency = "Hourly"
        };

        var result = await controller.SaveSyncPreferences(request);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<QuickBooksSyncPreferencesResponse>(ok.Value);
        Assert.Equal(user.CompanyId, payload.CompanyId);
        Assert.True(payload.AutoSyncInvoices);
        Assert.Equal("Hourly", payload.SyncFrequency);

        var savedPreference = await _context.AccountingSyncPreferences.SingleAsync();
        Assert.Equal(user.CompanyId, savedPreference.CompanyId);
        Assert.True(savedPreference.AutoSyncInvoices);
        Assert.True(savedPreference.AutoSyncCogs);
        Assert.Equal("Hourly", savedPreference.SyncFrequency);
    }

    [Fact]
    public async Task TestQuickBooksConnection_WithoutIntegration_ReturnsFailurePayload()
    {
        var user = new User { Id = 704, CompanyId = 54, UserTypeId = (int)UserTypeEnum.Admin };
        var controller = CreateController(user);

        var result = await controller.TestQuickBooksConnection(user.CompanyId);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<QuickBooksConnectionTestResponse>(ok.Value);
        Assert.False(payload.Success);
        Assert.Contains("not connected", payload.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TestQuickBooksConnection_WithIntegration_VerifiesDataService()
    {
        var user = new User { Id = 705, CompanyId = 56, UserTypeId = (int)UserTypeEnum.Admin };
        var controller = CreateController(user);
        _context!.AccountingIntegrations.Add(new AccountingIntegration
        {
            CompanyId = user.CompanyId,
            Provider = AccountingProvider.QuickBooks,
            RealmId = "realm",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        _context.SaveChanges();

        _quickBooksDataService
            .Setup(s => s.GetChartOfAccountsAsync(user.CompanyId, true))
            .ReturnsAsync(new List<QBOAccount>());

        var result = await controller.TestQuickBooksConnection(user.CompanyId);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<QuickBooksConnectionTestResponse>(ok.Value);
        Assert.True(payload.Success);
        _quickBooksDataService.Verify(s => s.GetChartOfAccountsAsync(user.CompanyId, true), Times.Once);
    }

    [Fact]
    public async Task GetInvoiceSyncStatus_WithExistingLog_ReturnsLatestEntry()
    {
        var user = new User { Id = 710, CompanyId = 45, UserTypeId = (int)UserTypeEnum.Admin };
        var controller = CreateController(user);

        var invoice = new Invoice
        {
            Id = 900,
            CompanyId = user.CompanyId,
            InvoiceNumber = "INV-900",
            CustomerName = "Test",
            CurrencyCode = "USD",
            InvoiceDate = DateTime.UtcNow,
            SubtotalAmount = 0,
            TotalAmount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context!.Invoices.Add(invoice);
        _context.AccountingSyncLogs.Add(new AccountingSyncLog
        {
            CompanyId = user.CompanyId,
            EntityType = "Invoice",
            EntityId = invoice.Id.ToString(),
            SyncStatus = SyncStatus.Success,
            ExternalEntityId = "QB-900",
            SyncedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        _context.SaveChanges();

        var result = await controller.GetInvoiceSyncStatus(invoice.Id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<QuickBooksInvoiceSyncStatusResponse>(ok.Value);
        Assert.Equal(invoice.Id, payload.InvoiceId);
        Assert.Equal(SyncStatus.Success, payload.Status);
        Assert.Equal("QB-900", payload.QboInvoiceId);
    }

    [Fact]
    public async Task SyncInvoice_WithActiveIntegration_ReturnsSuccessResponse()
    {
        var user = new User { Id = 711, CompanyId = 46, UserTypeId = (int)UserTypeEnum.Admin };
        var controller = CreateController(user);

        var invoice = new Invoice
        {
            Id = 901,
            CompanyId = user.CompanyId,
            InvoiceNumber = "INV-901",
            CustomerName = "Test",
            CurrencyCode = "USD",
            InvoiceDate = DateTime.UtcNow,
            SubtotalAmount = 0,
            TotalAmount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context!.Invoices.Add(invoice);
        _context.AccountingIntegrations.Add(new AccountingIntegration
        {
            CompanyId = user.CompanyId,
            Provider = AccountingProvider.QuickBooks,
            RealmId = "realm",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        _context.SaveChanges();

        _invoiceSyncService
            .Setup(s => s.SyncInvoiceToQBOAsync(invoice.Id))
            .ReturnsAsync(new InvoiceSyncResult(true, "QB-901", null));

        var result = await controller.SyncInvoice(new QuickBooksInvoiceSyncRequest { InvoiceId = invoice.Id });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<QuickBooksInvoiceSyncResponse>(ok.Value);
        Assert.True(payload.Success);
        Assert.Equal("QB-901", payload.QboInvoiceId);
        _invoiceSyncService.Verify(s => s.SyncInvoiceToQBOAsync(invoice.Id), Times.Once);
    }

    [Fact]
    public async Task SyncInvoice_WhenRecentInProgressLogExists_ReturnsConflict()
    {
        var user = new User { Id = 712, CompanyId = 47, UserTypeId = (int)UserTypeEnum.Admin };
        var controller = CreateController(user);

        var invoice = new Invoice
        {
            Id = 902,
            CompanyId = user.CompanyId,
            InvoiceNumber = "INV-902",
            CustomerName = "Test",
            CurrencyCode = "USD",
            InvoiceDate = DateTime.UtcNow,
            SubtotalAmount = 0,
            TotalAmount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context!.Invoices.Add(invoice);
        _context.AccountingIntegrations.Add(new AccountingIntegration
        {
            CompanyId = user.CompanyId,
            Provider = AccountingProvider.QuickBooks,
            RealmId = "realm",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        _context.AccountingSyncLogs.Add(new AccountingSyncLog
        {
            CompanyId = user.CompanyId,
            EntityType = "Invoice",
            EntityId = invoice.Id.ToString(),
            SyncStatus = SyncStatus.InProgress,
            SyncedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        _context.SaveChanges();

        var result = await controller.SyncInvoice(new QuickBooksInvoiceSyncRequest { InvoiceId = invoice.Id });

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        var payload = Assert.IsType<QuickBooksErrorResponse>(conflict.Value);
        Assert.Equal("Sync already in progress", payload.Message);
        _invoiceSyncService.Verify(s => s.SyncInvoiceToQBOAsync(It.IsAny<int>()), Times.Never);
    }

    private QuickBooksController CreateController(User user)
    {
        _context = new CaskrDbContext(new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

        _memoryCache = new MemoryCache(new MemoryCacheOptions());

        var controller = new QuickBooksController(
            _authService.Object,
            _usersService.Object,
            _quickBooksDataService.Object,
            _context,
            NullLogger<QuickBooksController>.Instance,
            _configuration,
            _memoryCache,
            _invoiceSyncService.Object);

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            }, "test"))
        };
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("api.caskr.dev");

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        _usersService.Setup(s => s.GetUserByIdAsync(user.Id)).ReturnsAsync(user);
        return controller;
    }

    public void Dispose()
    {
        _context?.Dispose();
        _memoryCache?.Dispose();
    }
}
