using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Caskr.server;
using Caskr.server.Controllers;
using Caskr.server.Models;
using Caskr.server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using UserTypeEnum = Caskr.server.UserType;

namespace Caskr.Server.Tests;

public sealed class TtbTransactionsControllerTests : IDisposable
{
    private readonly Mock<IUsersService> usersService = new();
    private readonly Mock<ITtbAuditLogger> auditLogger = new();
    private readonly CaskrDbContext dbContext;

    public TtbTransactionsControllerTests()
    {
        var options = new DbContextOptionsBuilder<CaskrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        dbContext = new CaskrDbContext(options);

        dbContext.Companies.Add(new Company
        {
            Id = 10,
            CompanyName = "Heritage Spirits",
            CreatedAt = DateTime.UtcNow,
            RenewalDate = DateTime.UtcNow.AddYears(1)
        });

        dbContext.Companies.Add(new Company
        {
            Id = 22,
            CompanyName = "Other Distillery",
            CreatedAt = DateTime.UtcNow,
            RenewalDate = DateTime.UtcNow.AddYears(1)
        });

        dbContext.Users.Add(new User
        {
            Id = 25,
            CompanyId = 10,
            Email = "admin@heritage.test",
            Name = "Heritage Admin",
            UserTypeId = (int)UserTypeEnum.Admin,
            IsPrimaryContact = true
        });

        dbContext.Users.Add(new User
        {
            Id = 30,
            CompanyId = 22,
            Email = "other@company.test",
            Name = "Other User",
            UserTypeId = (int)UserTypeEnum.Distiller,
            IsPrimaryContact = false
        });

        dbContext.SaveChanges();
    }

    [Fact]
    public async Task List_ReturnsTransactionsForCompanyAndMonth()
    {
        // Arrange
        dbContext.TtbTransactions.AddRange(
            new TtbTransaction
            {
                CompanyId = 10,
                TransactionDate = new DateTime(2024, 9, 15),
                TransactionType = TtbTransactionType.Production,
                ProductType = "Bourbon",
                SpiritsType = TtbSpiritsType.Under190Proof,
                ProofGallons = 100.50m,
                WineGallons = 50.25m,
                SourceEntityType = "Batch",
                SourceEntityId = 123,
                Notes = "Test batch"
            },
            new TtbTransaction
            {
                CompanyId = 10,
                TransactionDate = new DateTime(2024, 9, 20),
                TransactionType = TtbTransactionType.Loss,
                ProductType = "Bourbon",
                SpiritsType = TtbSpiritsType.Under190Proof,
                ProofGallons = 5.00m,
                WineGallons = 2.50m,
                SourceEntityType = "Manual",
                SourceEntityId = null,
                Notes = "Evaporation loss"
            },
            new TtbTransaction
            {
                CompanyId = 10,
                TransactionDate = new DateTime(2024, 8, 15),
                TransactionType = TtbTransactionType.Production,
                ProductType = "Vodka",
                SpiritsType = TtbSpiritsType.Neutral190OrMore,
                ProofGallons = 200.00m,
                WineGallons = 100.00m,
                SourceEntityType = "Batch",
                SourceEntityId = 456
            },
            new TtbTransaction
            {
                CompanyId = 22,
                TransactionDate = new DateTime(2024, 9, 10),
                TransactionType = TtbTransactionType.Production,
                ProductType = "Rum",
                SpiritsType = TtbSpiritsType.Under190Proof,
                ProofGallons = 150.00m,
                WineGallons = 75.00m,
                SourceEntityType = "Batch",
                SourceEntityId = 789
            }
        );

        await dbContext.SaveChangesAsync();

        var controller = CreateController(25);

        // Act
        var result = await controller.List(companyId: 10, month: 9, year: 2024);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var transactions = Assert.IsAssignableFrom<IEnumerable<TtbTransactionResponse>>(okResult.Value);
        var transactionList = transactions.ToList();

        Assert.Equal(2, transactionList.Count);
        Assert.All(transactionList, t => Assert.Equal(10, t.CompanyId));
        Assert.All(transactionList, t => Assert.True(t.TransactionDate.Month == 9 && t.TransactionDate.Year == 2024));
    }

    [Fact]
    public async Task List_WithInvalidMonth_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController(25);

        // Act
        var result = await controller.List(companyId: 10, month: 13, year: 2024);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var problemDetails = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Contains("Month must be between 1 and 12", problemDetails.Detail);
    }

    [Fact]
    public async Task List_WithYearBefore2020_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController(25);

        // Act
        var result = await controller.List(companyId: 10, month: 9, year: 2019);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var problemDetails = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Contains("2020 or later", problemDetails.Detail);
    }

    [Fact]
    public async Task List_WhenUserLacksPermission_ReturnsForbid()
    {
        // Arrange
        var controller = CreateController(30);

        // Act
        var result = await controller.List(companyId: 10, month: 9, year: 2024);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Create_CreatesManualTransaction()
    {
        // Arrange
        var controller = CreateController(25);
        var request = new CreateTtbTransactionRequest
        {
            CompanyId = 10,
            TransactionDate = new DateTime(2024, 9, 15),
            TransactionType = TtbTransactionType.Loss,
            ProductType = "Bourbon",
            SpiritsType = TtbSpiritsType.Under190Proof,
            ProofGallons = 10.50m,
            WineGallons = 5.25m,
            Notes = "Manual entry for loss"
        };

        // Act
        var result = await controller.Create(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var response = Assert.IsType<TtbTransactionResponse>(createdResult.Value);

        Assert.Equal(request.TransactionDate, response.TransactionDate);
        Assert.Equal(request.TransactionType, response.TransactionType);
        Assert.Equal(request.ProductType, response.ProductType);
        Assert.Equal(request.SpiritsType, response.SpiritsType);
        Assert.Equal(request.ProofGallons, response.ProofGallons);
        Assert.Equal(request.WineGallons, response.WineGallons);
        Assert.Equal("Manual", response.SourceEntityType);
        Assert.Null(response.SourceEntityId);

        // Verify in database
        var savedTransaction = await dbContext.TtbTransactions.FindAsync(response.Id);
        Assert.NotNull(savedTransaction);
        Assert.Equal("Manual", savedTransaction.SourceEntityType);
    }

    [Fact]
    public async Task Create_WithNegativeProofGallons_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController(25);
        var request = new CreateTtbTransactionRequest
        {
            CompanyId = 10,
            TransactionDate = new DateTime(2024, 9, 15),
            TransactionType = TtbTransactionType.Loss,
            ProductType = "Bourbon",
            SpiritsType = TtbSpiritsType.Under190Proof,
            ProofGallons = -10.50m,
            WineGallons = 5.25m
        };

        // Act
        var result = await controller.Create(request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var problemDetails = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Contains("ProofGallons cannot be negative", problemDetails.Detail);
    }

    [Fact]
    public async Task Create_WhenUserLacksPermission_ReturnsForbid()
    {
        // Arrange
        var controller = CreateController(30);
        var request = new CreateTtbTransactionRequest
        {
            CompanyId = 10,
            TransactionDate = new DateTime(2024, 9, 15),
            TransactionType = TtbTransactionType.Loss,
            ProductType = "Bourbon",
            SpiritsType = TtbSpiritsType.Under190Proof,
            ProofGallons = 10.50m,
            WineGallons = 5.25m
        };

        // Act
        var result = await controller.Create(request);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Get_ReturnsTransactionById()
    {
        // Arrange
        var transaction = new TtbTransaction
        {
            CompanyId = 10,
            TransactionDate = new DateTime(2024, 9, 15),
            TransactionType = TtbTransactionType.Production,
            ProductType = "Bourbon",
            SpiritsType = TtbSpiritsType.Under190Proof,
            ProofGallons = 100.50m,
            WineGallons = 50.25m,
            SourceEntityType = "Manual",
            Notes = "Test transaction"
        };

        dbContext.TtbTransactions.Add(transaction);
        await dbContext.SaveChangesAsync();

        var controller = CreateController(25);

        // Act
        var result = await controller.Get(transaction.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<TtbTransactionResponse>(okResult.Value);

        Assert.Equal(transaction.Id, response.Id);
        Assert.Equal(transaction.ProductType, response.ProductType);
        Assert.Equal(transaction.ProofGallons, response.ProofGallons);
    }

    [Fact]
    public async Task Get_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var controller = CreateController(25);

        // Act
        var result = await controller.Get(99999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Update_UpdatesManualTransaction()
    {
        // Arrange
        var transaction = new TtbTransaction
        {
            CompanyId = 10,
            TransactionDate = new DateTime(2024, 9, 15),
            TransactionType = TtbTransactionType.Loss,
            ProductType = "Bourbon",
            SpiritsType = TtbSpiritsType.Under190Proof,
            ProofGallons = 10.50m,
            WineGallons = 5.25m,
            SourceEntityType = "Manual",
            Notes = "Original notes"
        };

        dbContext.TtbTransactions.Add(transaction);
        await dbContext.SaveChangesAsync();

        var controller = CreateController(25);
        var updateRequest = new UpdateTtbTransactionRequest
        {
            TransactionDate = new DateTime(2024, 9, 20),
            TransactionType = TtbTransactionType.Destruction,
            ProductType = "Bourbon Updated",
            SpiritsType = TtbSpiritsType.Under190Proof,
            ProofGallons = 15.00m,
            WineGallons = 7.50m,
            Notes = "Updated notes"
        };

        // Act
        var result = await controller.Update(transaction.Id, updateRequest);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<TtbTransactionResponse>(okResult.Value);

        Assert.Equal(updateRequest.TransactionDate, response.TransactionDate);
        Assert.Equal(updateRequest.TransactionType, response.TransactionType);
        Assert.Equal(updateRequest.ProductType, response.ProductType);
        Assert.Equal(updateRequest.ProofGallons, response.ProofGallons);
        Assert.Equal(updateRequest.Notes, response.Notes);

        // Verify in database
        dbContext.ChangeTracker.Clear();
        var updatedTransaction = await dbContext.TtbTransactions.FindAsync(transaction.Id);
        Assert.Equal(updateRequest.ProductType, updatedTransaction!.ProductType);
    }

    [Fact]
    public async Task Update_WithAutoGeneratedTransaction_ReturnsBadRequest()
    {
        // Arrange
        var transaction = new TtbTransaction
        {
            CompanyId = 10,
            TransactionDate = new DateTime(2024, 9, 15),
            TransactionType = TtbTransactionType.Production,
            ProductType = "Bourbon",
            SpiritsType = TtbSpiritsType.Under190Proof,
            ProofGallons = 100.50m,
            WineGallons = 50.25m,
            SourceEntityType = "Batch",
            SourceEntityId = 123
        };

        dbContext.TtbTransactions.Add(transaction);
        await dbContext.SaveChangesAsync();

        var controller = CreateController(25);
        var updateRequest = new UpdateTtbTransactionRequest
        {
            TransactionDate = new DateTime(2024, 9, 20),
            TransactionType = TtbTransactionType.Production,
            ProductType = "Bourbon Updated",
            SpiritsType = TtbSpiritsType.Under190Proof,
            ProofGallons = 150.00m,
            WineGallons = 75.00m
        };

        // Act
        var result = await controller.Update(transaction.Id, updateRequest);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var problemDetails = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Contains("Only manual transactions can be edited", problemDetails.Detail);
    }

    [Fact]
    public async Task Delete_DeletesManualTransaction()
    {
        // Arrange
        var transaction = new TtbTransaction
        {
            CompanyId = 10,
            TransactionDate = new DateTime(2024, 9, 15),
            TransactionType = TtbTransactionType.Loss,
            ProductType = "Bourbon",
            SpiritsType = TtbSpiritsType.Under190Proof,
            ProofGallons = 10.50m,
            WineGallons = 5.25m,
            SourceEntityType = "Manual"
        };

        dbContext.TtbTransactions.Add(transaction);
        await dbContext.SaveChangesAsync();

        var controller = CreateController(25);

        // Act
        var result = await controller.Delete(transaction.Id);

        // Assert
        Assert.IsType<NoContentResult>(result);

        // Verify deletion in database
        dbContext.ChangeTracker.Clear();
        var deletedTransaction = await dbContext.TtbTransactions.FindAsync(transaction.Id);
        Assert.Null(deletedTransaction);
    }

    [Fact]
    public async Task Delete_WithAutoGeneratedTransaction_ReturnsBadRequest()
    {
        // Arrange
        var transaction = new TtbTransaction
        {
            CompanyId = 10,
            TransactionDate = new DateTime(2024, 9, 15),
            TransactionType = TtbTransactionType.Production,
            ProductType = "Bourbon",
            SpiritsType = TtbSpiritsType.Under190Proof,
            ProofGallons = 100.50m,
            WineGallons = 50.25m,
            SourceEntityType = "Batch",
            SourceEntityId = 123
        };

        dbContext.TtbTransactions.Add(transaction);
        await dbContext.SaveChangesAsync();

        var controller = CreateController(25);

        // Act
        var result = await controller.Delete(transaction.Id);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var problemDetails = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Contains("Only manual transactions can be deleted", problemDetails.Detail);
    }

    [Fact]
    public async Task Delete_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var controller = CreateController(25);

        // Act
        var result = await controller.Delete(99999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Create_WhenMonthIsLocked_ReturnsForbidden()
    {
        // Arrange
        // Setup audit logger to indicate the month is locked
        auditLogger.Setup(a => a.IsMonthLockedAsync(10, 9, 2024))
            .ReturnsAsync(true);

        var controller = CreateControllerWithLockedMonth(25, 10, 9, 2024);
        var request = new CreateTtbTransactionRequest
        {
            CompanyId = 10,
            TransactionDate = new DateTime(2024, 9, 15),
            TransactionType = TtbTransactionType.Loss,
            ProductType = "Bourbon",
            SpiritsType = TtbSpiritsType.Under190Proof,
            ProofGallons = 10.50m,
            WineGallons = 5.25m,
            Notes = "Manual entry for loss"
        };

        // Act
        var result = await controller.Create(request);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, statusCodeResult.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(statusCodeResult.Value);
        Assert.Contains("Cannot modify data for submitted reports", problemDetails.Detail);
    }

    [Fact]
    public async Task Update_WhenMonthIsLocked_ReturnsForbidden()
    {
        // Arrange
        var transaction = new TtbTransaction
        {
            CompanyId = 10,
            TransactionDate = new DateTime(2024, 9, 15),
            TransactionType = TtbTransactionType.Loss,
            ProductType = "Bourbon",
            SpiritsType = TtbSpiritsType.Under190Proof,
            ProofGallons = 10.50m,
            WineGallons = 5.25m,
            SourceEntityType = "Manual",
            Notes = "Original notes"
        };

        dbContext.TtbTransactions.Add(transaction);
        await dbContext.SaveChangesAsync();

        var controller = CreateControllerWithLockedMonth(25, 10, 9, 2024);
        var updateRequest = new UpdateTtbTransactionRequest
        {
            TransactionDate = new DateTime(2024, 9, 20),
            TransactionType = TtbTransactionType.Destruction,
            ProductType = "Bourbon Updated",
            SpiritsType = TtbSpiritsType.Under190Proof,
            ProofGallons = 15.00m,
            WineGallons = 7.50m,
            Notes = "Updated notes"
        };

        // Act
        var result = await controller.Update(transaction.Id, updateRequest);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, statusCodeResult.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(statusCodeResult.Value);
        Assert.Contains("Cannot modify data for submitted reports", problemDetails.Detail);
    }

    [Fact]
    public async Task Delete_WhenMonthIsLocked_ReturnsForbidden()
    {
        // Arrange
        var transaction = new TtbTransaction
        {
            CompanyId = 10,
            TransactionDate = new DateTime(2024, 9, 15),
            TransactionType = TtbTransactionType.Loss,
            ProductType = "Bourbon",
            SpiritsType = TtbSpiritsType.Under190Proof,
            ProofGallons = 10.50m,
            WineGallons = 5.25m,
            SourceEntityType = "Manual"
        };

        dbContext.TtbTransactions.Add(transaction);
        await dbContext.SaveChangesAsync();

        var controller = CreateControllerWithLockedMonth(25, 10, 9, 2024);

        // Act
        var result = await controller.Delete(transaction.Id);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, statusCodeResult.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(statusCodeResult.Value);
        Assert.Contains("Cannot modify data for submitted reports", problemDetails.Detail);
    }

    private TtbTransactionsController CreateController(int userId)
    {
        usersService.Setup(u => u.GetUserByIdAsync(userId)).ReturnsAsync(dbContext.Users.Find(userId));

        // Setup audit logger to allow all changes by default (month not locked)
        auditLogger.Setup(a => a.IsMonthLockedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(false);

        var controller = new TtbTransactionsController(
            dbContext,
            usersService.Object,
            auditLogger.Object,
            NullLogger<TtbTransactionsController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                    }, "TestAuth"))
                }
            }
        };

        return controller;
    }

    private TtbTransactionsController CreateControllerWithLockedMonth(int userId, int companyId, int month, int year)
    {
        usersService.Setup(u => u.GetUserByIdAsync(userId)).ReturnsAsync(dbContext.Users.Find(userId));

        // Setup audit logger to indicate the month is locked
        auditLogger.Setup(a => a.IsMonthLockedAsync(companyId, month, year))
            .ReturnsAsync(true);
        // Allow other months
        auditLogger.Setup(a => a.IsMonthLockedAsync(It.Is<int>(c => c != companyId), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(false);
        auditLogger.Setup(a => a.IsMonthLockedAsync(companyId, It.Is<int>(m => m != month), It.IsAny<int>()))
            .ReturnsAsync(false);
        auditLogger.Setup(a => a.IsMonthLockedAsync(companyId, month, It.Is<int>(y => y != year)))
            .ReturnsAsync(false);

        var controller = new TtbTransactionsController(
            dbContext,
            usersService.Object,
            auditLogger.Object,
            NullLogger<TtbTransactionsController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                    }, "TestAuth"))
                }
            }
        };

        return controller;
    }

    public void Dispose()
    {
        dbContext.Dispose();
    }
}
