using AchWorker.Activities;
using AchWorker.Gateways;
using AchWorker.OutputAdapters;
using AchWorker.UseCases.AddAchEntry;
using AchWorker.UseCases.CollectPendingPayments;
using AchWorker.UseCases.CreateAchFile;
using AchWorker.UseCases.DeleteAchFile;
using AchWorker.UseCases.DeleteTransferredFile;
using AchWorker.UseCases.FinalizeAchFile;
using AchWorker.UseCases.HardAuthorizePayment;
using AchWorker.UseCases.MarkReceivedFileProcessed;
using AchWorker.UseCases.ParseReturnFile;
using AchWorker.UseCases.RecordAchReturn;
using AchWorker.UseCases.RecordRepresentment;
using AchWorker.UseCases.RecordSettlement;
using AchWorker.UseCases.RevertAchFileToDraft;
using AchWorker.UseCases.SignalBankReturn;
using AchWorker.UseCases.SignalPaymentAddedToBatch;
using AchWorker.UseCases.TransferAchFile;
using AchWorker.UseCases.VoidPaymentAuth;
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

// Output adapters (gateway implementations)
builder.Services.AddScoped<IAchFileGateway, AchApiGateway>();
builder.Services.AddScoped<IPaymentGateway, PaymentApiGateway>();
builder.Services.AddScoped<ISftpGateway, SftpApiGateway>();
builder.Services.AddScoped<IPaymentSignalGateway, PaymentSignalGateway>();

// Use case interactors
builder.Services.AddScoped<ICollectPendingPaymentsInputBoundary, CollectPendingPaymentsInteractor>();
builder.Services.AddScoped<IHardAuthorizePaymentInputBoundary, HardAuthorizePaymentInteractor>();
builder.Services.AddScoped<IVoidPaymentAuthInputBoundary, VoidPaymentAuthInteractor>();
builder.Services.AddScoped<ICreateAchFileInputBoundary, CreateAchFileInteractor>();
builder.Services.AddScoped<IAddAchEntryInputBoundary, AddAchEntryInteractor>();
builder.Services.AddScoped<IFinalizeAchFileInputBoundary, FinalizeAchFileInteractor>();
builder.Services.AddScoped<IDeleteAchFileInputBoundary, DeleteAchFileInteractor>();
builder.Services.AddScoped<IRevertAchFileToDraftInputBoundary, RevertAchFileToDraftInteractor>();
builder.Services.AddScoped<IDeleteTransferredFileInputBoundary, DeleteTransferredFileInteractor>();
builder.Services.AddScoped<ITransferAchFileInputBoundary, TransferAchFileInteractor>();
builder.Services.AddScoped<ISignalPaymentAddedToBatchInputBoundary, SignalPaymentAddedToBatchInteractor>();
builder.Services.AddScoped<ISignalBankReturnInputBoundary, SignalBankReturnInteractor>();
builder.Services.AddScoped<IRecordSettlementInputBoundary, RecordSettlementInteractor>();
builder.Services.AddScoped<IRecordAchReturnInputBoundary, RecordAchReturnInteractor>();
builder.Services.AddScoped<IRecordRepresentmentInputBoundary, RecordRepresentmentInteractor>();
builder.Services.AddScoped<IParseReturnFileInputBoundary, ParseReturnFileInteractor>();
builder.Services.AddScoped<IMarkReceivedFileProcessedInputBoundary, MarkReceivedFileProcessedInteractor>();

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
        catch (Exception ex) when (
            ex is Temporalio.Exceptions.ScheduleAlreadyRunningException ||
            (ex is Temporalio.Exceptions.RpcException rpc &&
             rpc.Code == Temporalio.Exceptions.RpcException.StatusCode.AlreadyExists))
        {
            logger.LogInformation("Daily ACH batch schedule already exists — skipping");
        }
    }
}
