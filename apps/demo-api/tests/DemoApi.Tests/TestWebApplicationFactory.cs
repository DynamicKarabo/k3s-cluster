using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace DemoApi.Tests.Factory;

/// <summary>
/// Custom WebApplicationFactory that sets test environment and default config.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["App:Instance"] = "demo-api-test",
                ["App:Version"] = "0.0.0-test",
                ["CLUSTER_NAME"] = "k3s-test",
            });
        });

        builder.UseEnvironment("Testing");
        return base.CreateHost(builder);
    }
}
