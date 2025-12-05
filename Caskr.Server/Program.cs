using Caskr.server;
using Caskr.server.Middleware;
using Caskr.server.Models;
using Caskr.server.Services;
using Caskr.Server.Services.BackgroundJobs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using MediatR;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;

// Bootstrap logger for startup errors (before configuration is loaded)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Caskr application");

    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog from appsettings.json and environment
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithExceptionDetails()
        .Enrich.WithMachineName()
        .Enrich.WithEnvironmentName()
        .Enrich.WithProcessId()
        .Enrich.WithThreadId()
        .Enrich.WithCorrelationId()
        .Enrich.WithProperty("Application", "Caskr")
        .Enrich.WithProperty("Version", typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0"));

// Add services to the container.

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMediatR(typeof(Program));
builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
builder.Services.AddHostedService<BackgroundWorkerService>();
builder.Services.AddHostedService<QuickBooksSyncHostedService>();
builder.Services.AddSingleton<ISystemClock, SystemClock>();
builder.Services.AddSingleton<ITtbAutoReportProcessor, TtbAutoReportProcessor>();
builder.Services.AddHostedService<TtbAutoReportGeneratorService>();
builder.Services.AddScoped<ITtbInventorySnapshotCalculator, TtbInventorySnapshotCalculator>();
builder.Services.AddSingleton<TtbInventorySnapshotService>();
builder.Services.AddSingleton<ITtbInventorySnapshotBackfillService>(sp => sp.GetRequiredService<TtbInventorySnapshotService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<TtbInventorySnapshotService>());
builder.Services.AddHttpClient("WebhookClient");
builder.Services.AddHostedService<WebhookDeliveryWorker>();
var rawSigningKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(rawSigningKey))
{
    throw new InvalidOperationException("JWT signing key is not configured.");
}

var signingKeyBytes = Encoding.UTF8.GetBytes(rawSigningKey);
if (signingKeyBytes.Length < 32)
{
    signingKeyBytes = SHA256.HashData(signingKeyBytes);
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(signingKeyBytes)
        };
    });
builder.Services.AddAuthorization();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Caskr API",
        Version = "v1",
        Description = "Caskr distillery management application API. Includes public pricing endpoints and admin management APIs.",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Caskr Support",
            Email = "support@caskr.com"
        }
    });

    // Include XML comments for API documentation
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // Add security definition for JWT
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Mobile redirect configuration
builder.Services.AddMobileRedirect(builder.Configuration);

builder.Services.BindServices(builder.Configuration);

var app = builder.Build();

// Correlation ID - must be early in pipeline for distributed tracing
app.UseCorrelationId();

// Serilog request logging - captures all HTTP requests with timing
app.UseSerilogRequestLogging(options =>
{
    // Customize the message template
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

    // Emit debug-level logs for health check endpoints
    options.GetLevel = (httpContext, elapsed, ex) =>
    {
        if (ex != null)
            return LogEventLevel.Error;

        if (httpContext.Response.StatusCode >= 500)
            return LogEventLevel.Error;

        if (httpContext.Response.StatusCode >= 400)
            return LogEventLevel.Warning;

        // Health check endpoints at Debug level to reduce noise
        if (httpContext.Request.Path.StartsWithSegments("/api/health"))
            return LogEventLevel.Debug;

        return LogEventLevel.Information;
    };

    // Enrich with additional request data
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
        diagnosticContext.Set("ClientIP", httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");

        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            diagnosticContext.Set("UserId", httpContext.User.FindFirst("sub")?.Value ?? "unknown");
            diagnosticContext.Set("UserName", httpContext.User.Identity.Name ?? "unknown");
        }
    };
});

// Global exception handling - must be first in pipeline
app.UseGlobalExceptionHandler();

// Mobile redirect - after exception handling, before static files
app.UseMobileRedirect();

app.UseDefaultFiles();
app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.Information("Caskr application shutting down");
    Log.CloseAndFlush();
}
