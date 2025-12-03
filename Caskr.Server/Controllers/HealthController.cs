using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Caskr.server.Models;

namespace Caskr.server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly CaskrDbContext _dbContext;

    public HealthController(CaskrDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Basic health check endpoint
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Detailed health check including database connectivity
    /// </summary>
    [HttpGet("detailed")]
    public async Task<IActionResult> GetDetailed()
    {
        var health = new HealthCheckResult
        {
            Status = "healthy",
            Timestamp = DateTime.UtcNow,
            Version = typeof(HealthController).Assembly.GetName().Version?.ToString() ?? "1.0.0",
            Components = new Dictionary<string, ComponentHealth>()
        };

        // Check database connectivity
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync();
            health.Components["database"] = new ComponentHealth
            {
                Status = canConnect ? "healthy" : "unhealthy",
                Message = canConnect ? "Connected" : "Cannot connect to database"
            };
        }
        catch (Exception ex)
        {
            health.Status = "degraded";
            health.Components["database"] = new ComponentHealth
            {
                Status = "unhealthy",
                Message = ex.Message
            };
        }

        return health.Status == "healthy" ? Ok(health) : StatusCode(503, health);
    }
}

public class HealthCheckResult
{
    public string Status { get; set; } = "healthy";
    public DateTime Timestamp { get; set; }
    public string Version { get; set; } = "1.0.0";
    public Dictionary<string, ComponentHealth> Components { get; set; } = new();
}

public class ComponentHealth
{
    public string Status { get; set; } = "healthy";
    public string Message { get; set; } = "";
}
