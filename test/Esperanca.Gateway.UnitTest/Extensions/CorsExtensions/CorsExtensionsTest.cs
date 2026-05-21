using Esperanca.Gateway.Extensions;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Esperanca.Gateway.UnitTest.Extensions.CorsExtensions;

public class CorsExtensionsTest
{
    [Fact]
    public void AddGatewayCors_WhenOriginsConfigured_ThenPolicyContainsConfiguredOrigins()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Cors:AllowedOrigins:0", "http://localhost:3000" },
                { "Cors:AllowedOrigins:1", "http://localhost:5173" }
            })
            .Build();

        // Act
        services.AddGatewayCors(config);

        // Assert
        var provider = services.BuildServiceProvider();
        var corsOptions = provider.GetRequiredService<IOptions<CorsOptions>>().Value;
        var policy = corsOptions.GetPolicy(corsOptions.DefaultPolicyName);
        policy.ShouldNotBeNull();
        policy.Origins.ShouldContain("http://localhost:3000");
        policy.Origins.ShouldContain("http://localhost:5173");
    }

    [Fact]
    public void AddGatewayCors_WhenOriginsNotConfigured_ThenPolicyHasEmptyOrigins()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        // Act
        services.AddGatewayCors(config);

        // Assert
        var provider = services.BuildServiceProvider();
        var corsOptions = provider.GetRequiredService<IOptions<CorsOptions>>().Value;
        var policy = corsOptions.GetPolicy(corsOptions.DefaultPolicyName);
        policy.ShouldNotBeNull();
        policy.Origins.ShouldBeEmpty();
    }

    [Fact]
    public void AddGatewayCors_WhenCalled_ThenReturnsSameServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        // Act
        var result = services.AddGatewayCors(config);

        // Assert
        result.ShouldBeSameAs(services);
    }
}
