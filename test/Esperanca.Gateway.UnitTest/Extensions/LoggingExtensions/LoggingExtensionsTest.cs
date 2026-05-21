using Esperanca.Gateway.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace Esperanca.Gateway.UnitTest.Extensions.LoggingExtensions;

public class LoggingExtensionsTest
{
    [Fact]
    public void AddApplicationInsights_WhenConnectionStringIsNull_ThenDoesNotRegisterAnyService()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        var countBefore = builder.Services.Count;

        // Act
        builder.AddApplicationInsights();

        // Assert
        builder.Services.Count.ShouldBe(countBefore);
    }

    [Fact]
    public void AddApplicationInsights_WhenConnectionStringIsEmpty_ThenDoesNotRegisterAnyService()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "ApplicationInsights:ConnectionString", "" }
        });
        var countBefore = builder.Services.Count;

        // Act
        builder.AddApplicationInsights();

        // Assert
        builder.Services.Count.ShouldBe(countBefore);
    }

    [Fact]
    public void AddApplicationInsights_WhenConnectionStringProvided_ThenRegistersApplicationInsightsTelemetry()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "ApplicationInsights:ConnectionString", "InstrumentationKey=test-key-00000000-0000" }
        });
        var countBefore = builder.Services.Count;

        // Act
        builder.AddApplicationInsights();

        // Assert
        builder.Services.Count.ShouldBeGreaterThan(countBefore);
    }
}
