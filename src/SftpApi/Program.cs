using Microsoft.EntityFrameworkCore;
using SftpApi.Data;
using SftpApi.Gateways;
using SftpApi.OutputAdapters;
using SftpApi.UseCases.DeleteOutboundFile;
using SftpApi.UseCases.DeleteOutboundFileByAch;
using SftpApi.UseCases.GetInboundFileContent;
using SftpApi.UseCases.ListOutboundFiles;
using SftpApi.UseCases.ReceiveInboundFile;
using SftpApi.UseCases.ReceiveOutboundFile;
using SftpApi.UseCases.UpdateInboundFileStatus;
using Temporalio.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<SftpDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("Sftp") ?? "Data Source=sftp.db"));

builder.Services.AddSingleton<ITemporalClient>(_ =>
    TemporalClient.ConnectAsync(new(
        builder.Configuration["Temporal:Address"] ?? "localhost:7233")).GetAwaiter().GetResult());

builder.Services.AddScoped<ISftpFileGateway, SftpFileGateway>();

builder.Services.AddScoped<IReceiveOutboundFileInputBoundary, ReceiveOutboundFileInteractor>();
builder.Services.AddScoped<IListOutboundFilesInputBoundary, ListOutboundFilesInteractor>();
builder.Services.AddScoped<IDeleteOutboundFileByAchInputBoundary, DeleteOutboundFileByAchInteractor>();
builder.Services.AddScoped<IDeleteOutboundFileInputBoundary, DeleteOutboundFileInteractor>();
builder.Services.AddScoped<IReceiveInboundFileInputBoundary, ReceiveInboundFileInteractor>();
builder.Services.AddScoped<IGetInboundFileContentInputBoundary, GetInboundFileContentInteractor>();
builder.Services.AddScoped<IUpdateInboundFileStatusInputBoundary, UpdateInboundFileStatusInteractor>();

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<SftpDbContext>().Database.EnsureCreated();

app.MapControllers();
app.MapGet("/health", () => Results.Ok("healthy"));

app.Run();

public partial class Program { }
