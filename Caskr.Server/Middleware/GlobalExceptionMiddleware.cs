using System.Net;
using System.Text.Json;
using Serilog.Context;

namespace Caskr.server.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // Enrich log context with exception details for Seq
            using (LogContext.PushProperty("ExceptionType", ex.GetType().Name))
            using (LogContext.PushProperty("RequestPath", context.Request.Path.Value))
            using (LogContext.PushProperty("RequestMethod", context.Request.Method))
            using (LogContext.PushProperty("QueryString", context.Request.QueryString.Value))
            using (LogContext.PushProperty("StatusCode", GetStatusCode(ex)))
            {
                _logger.LogError(ex,
                    "Unhandled exception occurred processing {RequestMethod} {RequestPath}. TraceId: {TraceId}",
                    context.Request.Method,
                    context.Request.Path.Value,
                    context.TraceIdentifier);
            }

            await HandleExceptionAsync(context, ex);
        }
    }

    private static int GetStatusCode(Exception exception) => exception switch
    {
        ArgumentException => (int)HttpStatusCode.BadRequest,
        KeyNotFoundException => (int)HttpStatusCode.NotFound,
        UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
        InvalidOperationException => (int)HttpStatusCode.Conflict,
        _ => (int)HttpStatusCode.InternalServerError
    };

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse
        {
            TraceId = context.TraceIdentifier,
            CorrelationId = context.Items.TryGetValue("CorrelationId", out var correlationId)
                ? correlationId?.ToString()
                : null,
            Timestamp = DateTime.UtcNow
        };

        switch (exception)
        {
            case ArgumentException argEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = argEx.Message;
                response.Error = "Bad Request";
                break;

            case KeyNotFoundException keyEx:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                response.Message = keyEx.Message;
                response.Error = "Not Found";
                break;

            case UnauthorizedAccessException uaEx:
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                response.Message = uaEx.Message;
                response.Error = "Unauthorized";
                break;

            case InvalidOperationException ioEx:
                context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                response.Message = ioEx.Message;
                response.Error = "Operation Failed";
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.Error = "Internal Server Error";
                response.Message = _env.IsDevelopment()
                    ? exception.Message
                    : "An unexpected error occurred. Please try again later.";
                break;
        }

        // Include stack trace only in development
        if (_env.IsDevelopment())
        {
            response.Details = exception.StackTrace;
        }

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }
}

public class ErrorResponse
{
    public string Error { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Details { get; set; }
}

public static class GlobalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionMiddleware>();
    }
}
