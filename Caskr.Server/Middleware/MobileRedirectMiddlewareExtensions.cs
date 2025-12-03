using Caskr.server.Configuration;

namespace Caskr.server.Middleware;

/// <summary>
/// Extension methods for registering mobile redirect middleware
/// </summary>
public static class MobileRedirectMiddlewareExtensions
{
    /// <summary>
    /// Adds mobile redirect services to the service collection
    /// </summary>
    public static IServiceCollection AddMobileRedirect(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure options from appsettings
        services.Configure<MobileRedirectOptions>(
            configuration.GetSection(MobileRedirectOptions.SectionName));

        return services;
    }

    /// <summary>
    /// Adds mobile redirect services with custom configuration action
    /// </summary>
    public static IServiceCollection AddMobileRedirect(
        this IServiceCollection services,
        Action<MobileRedirectOptions> configureOptions)
    {
        services.Configure(configureOptions);
        return services;
    }

    /// <summary>
    /// Uses mobile redirect middleware in the application pipeline
    /// </summary>
    public static IApplicationBuilder UseMobileRedirect(this IApplicationBuilder app)
    {
        return app.UseMiddleware<MobileRedirectMiddleware>();
    }
}
