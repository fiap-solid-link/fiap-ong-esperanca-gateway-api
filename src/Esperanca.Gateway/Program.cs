using Esperanca.Gateway.Extensions;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.AddSerilog();
    builder.AddApplicationInsights();
    builder.Services.AddGatewayCors(builder.Configuration);
    builder.Services.AddGatewayRateLimiting(builder.Configuration);
    builder.Services.AddGatewayHealthChecks(builder.Configuration);
    builder.Services.AddGatewayReverseProxy(builder.Configuration);
    builder.Services.AddGatewaySwagger();

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseGatewaySwagger(builder.Configuration);
    app.UseCors();
    app.UseRateLimiter();
    app.MapGatewayHealthChecks();
    app.MapReverseProxy();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
