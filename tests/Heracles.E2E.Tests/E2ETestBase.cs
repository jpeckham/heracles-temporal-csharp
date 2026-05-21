using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Temporalio.Client;
using Xunit;

namespace Heracles.E2E.Tests;

public abstract class E2ETestBase : IAsyncLifetime
{
    protected static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    protected ITemporalClient Temporal { get; private set; } = null!;
    protected HttpClient PaymentClient { get; } = new() { BaseAddress = new Uri("http://localhost:8081") };
    protected HttpClient AchClient { get; } = new() { BaseAddress = new Uri("http://localhost:8082") };
    protected HttpClient SftpClient { get; } = new() { BaseAddress = new Uri("http://localhost:8083") };

    public async Task InitializeAsync()
    {
        Temporal = await TemporalClient.ConnectAsync(new("localhost:7233"));
        await WaitForHealthAsync();
    }

    public Task DisposeAsync()
    {
        PaymentClient.Dispose();
        AchClient.Dispose();
        SftpClient.Dispose();
        return Task.CompletedTask;
    }

    private async Task WaitForHealthAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        while (!cts.Token.IsCancellationRequested)
        {
            try
            {
                var r1 = await PaymentClient.GetAsync("/health");
                var r2 = await AchClient.GetAsync("/health");
                var r3 = await SftpClient.GetAsync("/health");
                if (r1.IsSuccessStatusCode && r2.IsSuccessStatusCode && r3.IsSuccessStatusCode)
                    return;
            }
            catch { }
            await Task.Delay(1000);
        }
        throw new Exception("Stack not healthy after 30s — are the k8s services running?");
    }
}
