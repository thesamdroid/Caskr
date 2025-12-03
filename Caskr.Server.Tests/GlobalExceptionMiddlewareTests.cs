using System.IO;
using System.Text.Json;
using Caskr.server.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace Caskr.Server.Tests;

public class GlobalExceptionMiddlewareTests
{
    private readonly Mock<ILogger<GlobalExceptionMiddleware>> _loggerMock = new();

    [Fact]
    public async Task InvokeAsync_WhenNoException_PassesThrough()
    {
        // Arrange
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ =>
        {
            _.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        }, Environments.Production);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);
        Assert.Equal(0, context.Response.Body.Length);
    }

    [Fact]
    public async Task InvokeAsync_WhenArgumentException_ReturnsBadRequestWithDetailsInDevelopment()
    {
        // Arrange
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw new ArgumentException("Invalid input"), Environments.Development);

        // Act
        await middleware.InvokeAsync(context);
        var response = await ReadResponseAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);
        Assert.Equal("Bad Request", response.Error);
        Assert.Equal("Invalid input", response.Message);
        Assert.False(string.IsNullOrWhiteSpace(response.TraceId));
        Assert.NotNull(response.Details); // Stack trace should be present in development
    }

    [Fact]
    public async Task InvokeAsync_WhenUnhandledExceptionInProduction_HidesSensitiveDetails()
    {
        // Arrange
        var context = CreateHttpContext();
        var middleware = CreateMiddleware(_ => throw new Exception("Sensitive error"), Environments.Production);

        // Act
        await middleware.InvokeAsync(context);
        var response = await ReadResponseAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);
        Assert.Equal("Internal Server Error", response.Error);
        Assert.Equal("An unexpected error occurred. Please try again later.", response.Message);
        Assert.Null(response.Details);
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private GlobalExceptionMiddleware CreateMiddleware(RequestDelegate next, string environmentName)
    {
        var envMock = new Mock<IHostEnvironment>();
        envMock.SetupGet(e => e.EnvironmentName).Returns(environmentName);
        envMock.SetupGet(e => e.ApplicationName).Returns("Test App");
        envMock.SetupGet(e => e.ContentRootPath).Returns(AppContext.BaseDirectory);

        return new GlobalExceptionMiddleware(next, _loggerMock.Object, envMock.Object);
    }

    private static async Task<ErrorResponse> ReadResponseAsync(DefaultHttpContext context)
    {
        context.Response.Body.Position = 0;
        var responseJson = await new StreamReader(context.Response.Body).ReadToEndAsync();
        return JsonSerializer.Deserialize<ErrorResponse>(responseJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })!;
    }
}
