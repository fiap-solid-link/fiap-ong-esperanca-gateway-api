using Esperanca.Gateway.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Esperanca.Gateway.UnitTest.Extensions.RateLimitingExtensions;

public class RateLimitingExtensionsTest
{
    [Fact]
    public void AddGatewayRateLimiting_WhenCalled_ThenSets429AsRejectionStatusCode()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        // Act
        services.AddGatewayRateLimiting(config);

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<RateLimiterOptions>>().Value;
        options.RejectionStatusCode.ShouldBe(StatusCodes.Status429TooManyRequests);
    }

    [Fact]
    public void AddGatewayRateLimiting_WhenPermitLimitConfigured_ThenUsesConfiguredValue()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "RateLimiting:PermitLimit", "50" },
                { "RateLimiting:WindowSeconds", "30" }
            })
            .Build();

        // Act — must not throw with custom values
        var exception = Record.Exception(() => services.AddGatewayRateLimiting(config));

        // Assert
        exception.ShouldBeNull();
    }

    [Fact]
    public void AddGatewayRateLimiting_WhenConfigMissing_ThenRegistersWithoutThrowing()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        // Act — must not throw when config is absent (defaults are used)
        var exception = Record.Exception(() => services.AddGatewayRateLimiting(config));

        // Assert
        exception.ShouldBeNull();
    }

    [Fact]
    public void AddGatewayRateLimiting_WhenCalled_ThenReturnsSameServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        // Act
        var result = services.AddGatewayRateLimiting(config);

        // Assert
        result.ShouldBeSameAs(services);
    }
}
