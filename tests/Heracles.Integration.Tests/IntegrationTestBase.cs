extern alias PaymentApiAssembly;
extern alias AchApiAssembly;
extern alias SftpApiAssembly;

using AchWorker.Activities;
using AchWorker.Workflows;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Temporalio.Client;
using Temporalio.Testing;
using Temporalio.Worker;
using Microsoft.Extensions.Configuration;

namespace Heracles.Integration.Tests;

public class IntegrationTestBase : IAsyncLifetime
{
    protected WorkflowEnvironment TemporalEnv { get; private set; } = null!;
    protected HttpClient PaymentClient { get; private set; } = null!;
    protected HttpClient AchClient { get; private set; } = null!;
    protected HttpClient SftpClient { get; private set; } = null!;
    protected TemporalWorker Worker { get; private set; } = null!;

    private WebApplicationFactory<PaymentApiAssembly::Program> _paymentFactory = null!;
    private WebApplicationFactory<AchApiAssembly::Program> _achFactory = null!;
    private WebApplicationFactory<SftpApiAssembly::Program> _sftpFactory = null!;

    // Unique DB file paths per test run
    private readonly string _paymentDb = $"test-payments-{Guid.NewGuid()}.db";
    private readonly string _achDb = $"test-ach-{Guid.NewGuid()}.db";
    private readonly string _sftpDb = $"test-sftp-{Guid.NewGuid()}.db";

    public async Task InitializeAsync()
    {
        TemporalEnv = await WorkflowEnvironment.StartLocalAsync();

        _paymentFactory = new WebApplicationFactory<PaymentApiAssembly::Program>()
            .WithWebHostBuilder(b =>
            {
                b.ConfigureAppConfiguration((_, config) =>
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:Payments"] = $"Data Source={_paymentDb}"
                    }));
                b.ConfigureServices(services =>
                    ReplaceTemporalClient(services, TemporalEnv.Client));
            });

        _achFactory = new WebApplicationFactory<AchApiAssembly::Program>()
            .WithWebHostBuilder(b =>
                b.ConfigureAppConfiguration((_, config) =>
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:Ach"] = $"Data Source={_achDb}"
                    })));

        _sftpFactory = new WebApplicationFactory<SftpApiAssembly::Program>()
            .WithWebHostBuilder(b =>
            {
                b.ConfigureAppConfiguration((_, config) =>
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:Sftp"] = $"Data Source={_sftpDb}"
                    }));
                b.ConfigureServices(services =>
                    ReplaceTemporalClient(services, TemporalEnv.Client));
            });

        PaymentClient = _paymentFactory.CreateClient();
        AchClient = _achFactory.CreateClient();
        SftpClient = _sftpFactory.CreateClient();

        var httpFactory = new TestHttpClientFactory(PaymentClient, AchClient, SftpClient);

        Worker = new TemporalWorker(TemporalEnv.Client,
            new TemporalWorkerOptions("ach-worker")
                .AddWorkflow<PaymentWorkflow>()
                .AddWorkflow<AchBatchWorkflow>()
                .AddWorkflow<AchReturnWorkflow>()
                .AddAllActivities(new PaymentActivities(httpFactory, TemporalEnv.Client))
                .AddAllActivities(new AchActivities(httpFactory))
                .AddAllActivities(new SftpActivities(httpFactory)));
    }

    public async Task DisposeAsync()
    {
        Worker.Dispose();
        await TemporalEnv.DisposeAsync();
        PaymentClient.Dispose();
        AchClient.Dispose();
        SftpClient.Dispose();
        await _paymentFactory.DisposeAsync();
        await _achFactory.DisposeAsync();
        await _sftpFactory.DisposeAsync();

        // Clean up SQLite test files
        foreach (var f in new[] { _paymentDb, _achDb, _sftpDb })
        {
            try { File.Delete(f); } catch { /* ignore */ }
        }
    }

    private static void ReplaceTemporalClient(IServiceCollection services, ITemporalClient client)
    {
        var d = services.SingleOrDefault(s => s.ServiceType == typeof(ITemporalClient));
        if (d != null) services.Remove(d);
        services.AddSingleton(client);
    }
}

public class TestHttpClientFactory(HttpClient payment, HttpClient ach, HttpClient sftp)
    : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => name switch
    {
        "PaymentApi" => payment,
        "AchApi" => ach,
        "SftpApi" => sftp,
        _ => throw new InvalidOperationException($"Unknown client: {name}")
    };
}
