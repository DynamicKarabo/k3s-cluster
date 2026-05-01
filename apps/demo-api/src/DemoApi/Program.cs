using System.Diagnostics;
using System.Runtime.InteropServices;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

var builder = WebApplication.CreateBuilder(args);

// ── Configuration from environment variables ──
var configSection = builder.Configuration.GetSection("App");
var instanceName = Environment.GetEnvironmentVariable("APP_INSTANCE") ?? "demo-api";
var version = Environment.GetEnvironmentVariable("APP_VERSION") ?? "0.1.0";
var clusterName = Environment.GetEnvironmentVariable("CLUSTER_NAME") ?? "k3s-local";
var listenPort = Environment.GetEnvironmentVariable("APP_PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{listenPort}");

// ── OpenTelemetry Metrics ──
var otelResourceBuilder = ResourceBuilder.CreateDefault()
    .AddService(serviceName: instanceName, serviceVersion: version)
    .AddAttributes(new Dictionary<string, object>
    {
        ["deployment.environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
        ["cluster.name"] = clusterName,
    });

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .SetResourceBuilder(otelResourceBuilder)
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddProcessInstrumentation()
        .AddPrometheusExporter());

// ── Health checks ──
builder.Services.AddHealthChecks();

var app = builder.Build();

// ── Middleware pipeline ──
app.UseOpenTelemetryPrometheusScrapingEndpoint();

// ── Endpoints ──
app.MapGet("/", () => Results.Ok(new
{
    service = instanceName,
    version,
    status = "running",
    cluster = clusterName
}));

app.MapGet("/healthz", () => Results.Ok(new { status = "healthy" }))
   .WithTags("Health");

app.MapGet("/readyz", () =>
{
    // In a real app, check DB connectivity here.
    // For demo purposes, always ready.
    return Results.Ok(new { status = "ready" });
}).WithTags("Health");

app.MapGet("/api/info", () =>
{
    var process = Process.GetCurrentProcess();
    return Results.Ok(new
    {
        version,
        cluster = clusterName,
        instance = instanceName,
        runtime = new
        {
            os = RuntimeInformation.OSDescription,
            framework = RuntimeInformation.FrameworkDescription,
            architecture = RuntimeInformation.ProcessArchitecture.ToString()
        },
        process = new
        {
            startTime = process.StartTime,
            memoryMB = process.WorkingSet64 / 1024 / 1024,
            duration = DateTime.UtcNow - process.StartTime.ToUniversalTime()
        }
    });
}).WithTags("Info");

app.Run();
