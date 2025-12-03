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

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddSwaggerGen();

// Mobile redirect configuration
builder.Services.AddMobileRedirect(builder.Configuration);

builder.Services.BindServices(builder.Configuration);

var app = builder.Build();

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
