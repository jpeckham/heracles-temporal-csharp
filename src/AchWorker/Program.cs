using AchWorker.Activities;
using AchWorker.Workflows;
using Temporalio.Client;
using Temporalio.Client.Schedules;
using Temporalio.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHttpClient("PaymentApi", c =>
    c.BaseAddress = new Uri(builder.Configuration["Services:PaymentApi"] ?? "http://localhost:5001"));
builder.Services.AddHttpClient("AchApi", c =>
    c.BaseAddress = new Uri(builder.Configuration["Services:AchApi"] ?? "http://localhost:5002"));
builder.Services.AddHttpClient("SftpApi", c =>
    c.BaseAddress = new Uri(builder.Configuration["Services:SftpApi"] ?? "http://localhost:5003"));

builder.Services.AddSingleton<ITemporalClient>(_ =>
    TemporalClient.ConnectAsync(new(
        builder.Configuration["Temporal:Address"] ?? "localhost:7233")).GetAwaiter().GetResult());

builder.Services
    .AddHostedTemporalWorker(
        clientTargetHost: builder.Configuration["Temporal:Address"] ?? "localhost:7233",
        clientNamespace: "default",
        taskQueue: "ach-worker")
    .AddScopedActivities<PaymentActivities>()
    .AddScopedActivities<AchActivities>()
    .AddScopedActivities<SftpActivities>()
    .AddWorkflow<PaymentWorkflow>()
    .AddWorkflow<AchBatchWorkflow>()
    .AddWorkflow<AchReturnWorkflow>();

builder.Services.AddHostedService<ScheduleRegistrationService>();

var host = builder.Build();
await host.RunAsync();

public class ScheduleRegistrationService(ITemporalClient client, ILogger<ScheduleRegistrationService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await client.CreateScheduleAsync(
                "daily-ach-batch",
                new Schedule(
                    Action: ScheduleActionStartWorkflow.Create(
                        "AchBatchWorkflow",
                        Array.Empty<object>(),
                        new WorkflowOptions(
                            id: $"ach-batch-{DateTime.UtcNow:yyyyMMdd}",
                            taskQueue: "ach-worker")),
                    Spec: new ScheduleSpec
                    {
                        CronExpressions = ["0 17 * * 1-5"],
                        Jitter = TimeSpan.FromMinutes(1)
                    }),
                new ScheduleOptions { TriggerImmediately = false });

            logger.LogInformation("Daily ACH batch schedule registered");
        }
        catch (Temporalio.Exceptions.RpcException ex)
            when (ex.Code == Temporalio.Exceptions.RpcException.StatusCode.AlreadyExists)
        {
            logger.LogInformation("Daily ACH batch schedule already exists — skipping");
        }
    }
}
