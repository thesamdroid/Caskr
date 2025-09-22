using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Caskr.server.Models;
using Caskr.server.Repos;
using Caskr.server.Services;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Caskr.Server.Tests;

public class BarrelsServiceTests
{
    private readonly Mock<IBarrelsRepository> _repository = new();
    private readonly BarrelsService _service;

    public BarrelsServiceTests()
    {
        _service = new BarrelsService(_repository.Object);
    }

    [Fact]
    public async Task ForecastBarrelsAsync_NormalizesUnspecifiedDateToUtc()
    {
        var repository = new Mock<IBarrelsRepository>();
        DateTime capturedTargetDate = default;
        repository
            .Setup(r => r.ForecastBarrelsAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<int>()))
            .Callback<int, DateTime, int>((_, targetDate, _) => capturedTargetDate = targetDate)
            .ReturnsAsync(Array.Empty<Barrel>());

        var service = new BarrelsService(repository.Object);
        var unspecifiedTargetDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

        await service.ForecastBarrelsAsync(1, unspecifiedTargetDate, 5);

        Assert.Equal(DateTimeKind.Utc, capturedTargetDate.Kind);
        Assert.Equal(DateTime.SpecifyKind(unspecifiedTargetDate, DateTimeKind.Utc), capturedTargetDate);
    }

    [Fact]
    public async Task ImportBarrelsAsync_NoBatchOrMashBill_Throws()
    {
        var file = CreateCsvFile("sku,rickhouse\nSKU1,R1");

        var lookupInvoked = false;
        _repository
            .Setup(r => r.GetRickhouseIdsByNameAsync(1, It.IsAny<IEnumerable<string>>()))
            .Callback(() => lookupInvoked = true)
            .ReturnsAsync(new Dictionary<string, int> { { "r1", 10 } });

        await Assert.ThrowsAsync<BatchRequiredException>(() =>
            _service.ImportBarrelsAsync(1, 2, file, null, null));

        Assert.True(lookupInvoked);
    }

    [Fact]
    public async Task ImportBarrelsAsync_NullRickhouseLookup_TreatedAsMissing()
    {
        var file = CreateCsvFile("sku,rickhouse\nSKU1,R1");

        _repository
            .Setup(r => r.GetRickhouseIdsByNameAsync(1, It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync((Dictionary<string, int>?)null);

        await Assert.ThrowsAsync<RickhouseNotFoundException>(() =>
            _service.ImportBarrelsAsync(1, 2, file, 5, 7));
    }

    [Fact]
    public async Task ImportBarrelsAsync_WithMashBill_CreatesBatchAndBarrels()
    {
        var file = CreateCsvFile("sku,rickhouse\nSKU1,R1\nSKU2,R1");

        _repository.Setup(r => r.GetRickhouseIdsByNameAsync(1, It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new Dictionary<string, int> { { "r1", 10 } });
        _repository.Setup(r => r.MashBillExistsForCompanyAsync(1, 5)).ReturnsAsync(true);
        _repository.Setup(r => r.CreateBatchAsync(1, 5)).ReturnsAsync(3);
        _repository.Setup(r => r.EnsureOrderForBatchAsync(1, 2, 3, 2)).ReturnsAsync(7);

        IEnumerable<Barrel>? savedBarrels = null;
        _repository
            .Setup(r => r.AddBarrelsAsync(It.IsAny<IEnumerable<Barrel>>()))
            .Callback<IEnumerable<Barrel>>(barrels => savedBarrels = barrels)
            .Returns(Task.CompletedTask);

        var result = await _service.ImportBarrelsAsync(1, 2, file, null, 5);

        Assert.Equal(3, result.BatchId);
        Assert.Equal(2, result.CreatedCount);
        Assert.True(result.CreatedNewBatch);

        Assert.NotNull(savedBarrels);
        var list = savedBarrels!.ToList();
        Assert.All(list, barrel =>
        {
            Assert.Equal(1, barrel.CompanyId);
            Assert.Equal(3, barrel.BatchId);
            Assert.Equal(7, barrel.OrderId);
            Assert.Equal(10, barrel.RickhouseId);
        });
        Assert.Contains(list, barrel => barrel.Sku == "SKU1");
        Assert.Contains(list, barrel => barrel.Sku == "SKU2");
    }

    [Fact]
    public async Task ImportBarrelsAsync_WithExistingBatch_DoesNotCreateNewBatch()
    {
        var file = CreateCsvFile("SKU1,R1");

        _repository.Setup(r => r.GetRickhouseIdsByNameAsync(1, It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new Dictionary<string, int> { { "r1", 10 } });
        _repository.Setup(r => r.BatchExistsForCompanyAsync(1, 4)).ReturnsAsync(true);
        _repository.Setup(r => r.EnsureOrderForBatchAsync(1, 2, 4, 1)).ReturnsAsync(9);
        _repository.Setup(r => r.AddBarrelsAsync(It.IsAny<IEnumerable<Barrel>>()))
            .Returns(Task.CompletedTask);

        var result = await _service.ImportBarrelsAsync(1, 2, file, 4, null);

        Assert.Equal(4, result.BatchId);
        Assert.False(result.CreatedNewBatch);
        _repository.Verify(r => r.CreateBatchAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ImportBarrelsAsync_SampleCsvFile_IsValid()
    {
        var samplePath = Path.Combine(AppContext.BaseDirectory, "TestData", "barrel-import-sample.csv");
        Assert.True(File.Exists(samplePath));

        var bytes = await File.ReadAllBytesAsync(samplePath);
        using var memoryStream = new MemoryStream(bytes);
        var file = new FormFile(memoryStream, 0, memoryStream.Length, "file", "barrel-import-sample.csv");

        _repository.Setup(r => r.GetRickhouseIdsByNameAsync(1, It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new Dictionary<string, int>
            {
                { "rickhouse a", 10 },
                { "rickhouse b", 11 }
            });
        _repository.Setup(r => r.MashBillExistsForCompanyAsync(1, 7)).ReturnsAsync(true);
        _repository.Setup(r => r.CreateBatchAsync(1, 7)).ReturnsAsync(5);
        _repository.Setup(r => r.EnsureOrderForBatchAsync(1, 2, 5, 4)).ReturnsAsync(13);

        IEnumerable<Barrel>? savedBarrels = null;
        _repository
            .Setup(r => r.AddBarrelsAsync(It.IsAny<IEnumerable<Barrel>>()))
            .Callback<IEnumerable<Barrel>>(barrels => savedBarrels = barrels)
            .Returns(Task.CompletedTask);

        var result = await _service.ImportBarrelsAsync(1, 2, file, null, 7);

        Assert.Equal(5, result.BatchId);
        Assert.Equal(4, result.CreatedCount);
        Assert.True(result.CreatedNewBatch);

        Assert.NotNull(savedBarrels);
        var list = savedBarrels!.ToList();
        Assert.Equal(4, list.Count);
        Assert.All(list, barrel =>
        {
            Assert.Equal(1, barrel.CompanyId);
            Assert.Equal(5, barrel.BatchId);
            Assert.Equal(13, barrel.OrderId);
        });

        var skuOrder = list.Select(barrel => barrel.Sku).ToList();
        Assert.Equal(new[] { "BAR-0001", "BAR-0002", "BAR-0003", "BAR-0004" }, skuOrder);

        var rickhouseIds = list.Select(barrel => barrel.RickhouseId).ToList();
        Assert.Equal(new[] { 10, 10, 11, 11 }, rickhouseIds);
    }

    private static IFormFile CreateCsvFile(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", "barrels.csv");
    }
}
