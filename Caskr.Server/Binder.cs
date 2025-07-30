using System.Reflection;
using Caskr.server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Caskr.server
{
    public static class Binder
    {
        private static readonly string[] KnownSuffixes = new []{"Repository", "Service", "Helper"};

public static void BindServices(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("CaskrDatabaseConnectionString");
            
            services.AddDbContext<CaskrDbContext>(options =>
                options.UseNpgsql(connectionString));

            var bindableTypes = GetAutoBindedTypes(Assembly.GetAssembly(typeof(Binder))!);

            foreach (var type in bindableTypes)
            {
                var interfaces = type.GetInterfaces();
                if (interfaces.Length == 0)
                {
                    throw new Exception($"Type {type.Name} does not implement any interfaces.");
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
                if (type.GetCustomAttributes(typeof(AutoBind), true).Length > 0 || 
                    (KnownSuffixes.Any(x => type.Name.ToLower().EndsWith(x.ToLower())) && !type.Name.ToLower().StartsWith("i")))
                {
                    yield return type;
                }
            }
        }
    }
}
