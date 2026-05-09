using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace Esperanca.Gateway.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddGatewayHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddUrlGroup(
                new Uri(configuration["HealthChecks:IdentityApi:Uri"]
                    ?? "http://fiap-ong-esperanca-identity-api/health"),
                name: "identity-api",
                timeout: TimeSpan.FromSeconds(10))
            .AddUrlGroup(
                new Uri(configuration["HealthChecks:CampanhasApi:Uri"]
                    ?? "http://fiap-ong-esperanca-campanhas-api/health"),
                name: "campanhas-api",
                timeout: TimeSpan.FromSeconds(10));

        return services;
    }

    public static void MapGatewayHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";

                var result = new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        description = e.Value.Description,
                        duration = e.Value.Duration.TotalMilliseconds
                    }),
                    totalDuration = report.TotalDuration.TotalMilliseconds
                };

                await context.Response.WriteAsJsonAsync(result);
            }
        });
    }
    
}
