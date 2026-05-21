using Esperanca.Gateway.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Esperanca.Gateway.UnitTest.Extensions.HealthCheckExtensions;

public class HealthCheckExtensionsTest
{
    [Fact]
    public void AddGatewayHealthChecks_WhenCalled_ThenRegistersIdentityApiCheck()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        // Act
        services.AddGatewayHealthChecks(config);

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        options.Registrations.ShouldContain(r => r.Name == "identity-api");
    }

    [Fact]
    public void AddGatewayHealthChecks_WhenCalled_ThenRegistersCampanhasApiCheck()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        // Act
        services.AddGatewayHealthChecks(config);

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        options.Registrations.ShouldContain(r => r.Name == "campanhas-api");
    }

    [Fact]
    public void AddGatewayHealthChecks_WhenUriConfigured_ThenUsesTwoRegistrations()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "HealthChecks:IdentityApi:Uri", "http://custom-identity/health" },
                { "HealthChecks:CampanhasApi:Uri", "http://custom-campanhas/health" }
            })
            .Build();

        // Act
        services.AddGatewayHealthChecks(config);

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        options.Registrations.Count.ShouldBe(2);
    }

    [Fact]
    public void AddGatewayHealthChecks_WhenCalled_ThenReturnsSameServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        // Act
        var result = services.AddGatewayHealthChecks(config);

        // Assert
        result.ShouldBeSameAs(services);
    }
}
