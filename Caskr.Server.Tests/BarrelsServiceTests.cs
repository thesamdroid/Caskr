using Caskr.server.Models;
using Caskr.server.Repos;
using Caskr.server.Services;
using Moq;

namespace Caskr.Server.Tests;

public class BarrelsServiceTests
{
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
}
