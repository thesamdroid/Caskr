using System.Reflection;
using Caskr.server.Models;
using Caskr.server.Models.SupplyChain;
using Caskr.server.Services;
using Caskr.Server.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Caskr.server
{
    public static class Binder
    {
        private static readonly string[] KnownSuffixes = new []{"Repository", "Service", "Helper"};

public static void BindServices(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("CaskrDatabaseConnectionString");

            services.AddSingleton(configuration);
            services.AddDataProtection();

            services.AddDbContext<CaskrDbContext>(options =>
                options.UseNpgsql(connectionString));

            services.AddHttpClient<IKeycloakClient, KeycloakClient>();
            services.AddSingleton<IQuickBooksOAuthClientFactory, QuickBooksOAuthClientFactory>();

            var bindableTypes = GetAutoBindedTypes(Assembly.GetAssembly(typeof(Binder))!);

            foreach (var type in bindableTypes)
            {
                var interfaces = type.GetInterfaces()
                    // Exclude IHostedService and IDisposable - these are handled separately
                    .Where(i => i != typeof(IHostedService) &&
                                i != typeof(IDisposable) &&
                                !i.IsAssignableTo(typeof(IHostedService)))
                    .ToArray();

                if (interfaces.Length == 0)
                {
                    // Skip types that only implement IHostedService/IDisposable
                    continue;
                }
                foreach (var @interface in interfaces)
                {
                    services.AddScoped(@interface, type);
                }
            }
        }

        /// <summary>
        /// Get all types in the assembly that are marked with the AutoBind attribute or have a known suffix.
        /// SUFFIX DOES NOT WORK FOR CLASSES THAT START WITH I
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        static IEnumerable<Type> GetAutoBindedTypes(this Assembly assembly)
        {
            foreach (Type type in assembly.GetTypes())
            {
                // Skip static classes (IsAbstract && IsSealed) and abstract classes
                if (type.IsAbstract)
                    continue;

                if (type.GetCustomAttributes(typeof(AutoBind), true).Length > 0 ||
                    (KnownSuffixes.Any(x => type.Name.ToLower().EndsWith(x.ToLower())) && !type.Name.ToLower().StartsWith("i")))
                {
                    yield return type;
                }
            }
        }
    }
}
