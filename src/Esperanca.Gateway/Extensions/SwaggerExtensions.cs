namespace Esperanca.Gateway.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddGatewaySwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        return services;
    }

    public static WebApplication UseGatewaySwagger(
        this WebApplication app,
        IConfiguration configuration)
    {
        var endpoints = configuration.GetSection("Swagger:Endpoints").Get<SwaggerEndpoint[]>() ?? [];

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            foreach (var endpoint in endpoints)
            {
                options.SwaggerEndpoint(endpoint.Url, endpoint.Name);
            }

            options.RoutePrefix = "swagger";
            options.DocumentTitle = "ONG Esperanca - API Gateway";
        });

        return app;
    }

    public class SwaggerEndpoint
    {
        public string Url { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
