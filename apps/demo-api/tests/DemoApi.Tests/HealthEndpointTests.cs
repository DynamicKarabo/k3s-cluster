using System.Net.Http.Json;
using DemoApi.Tests.Factory;

namespace DemoApi.Tests;

public class HealthEndpointTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    [Fact]
    public async Task Healthz_Returns_200()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/healthz");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(body);
        Assert.Equal("healthy", body["status"]);
    }

    [Fact]
    public async Task Readyz_Returns_200()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/readyz");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(body);
        Assert.Equal("ready", body["status"]);
    }

    [Fact]
    public async Task ApiInfo_Returns_Version()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/info");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(body);
        Assert.Contains("version", body.Keys);
        Assert.Contains("cluster", body.Keys);
        Assert.Contains("runtime", body.Keys);
        Assert.Contains("process", body.Keys);
    }

    [Fact]
    public async Task Metrics_Endpoint_Returns_Prometheus()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/metrics");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("# HELP", content);  // Prometheus format indicator
    }
}
