using AchApi.Data;
using AchApi.Gateways;
using AchApi.OutputAdapters;
using AchApi.UseCases.AddAchEntry;
using AchApi.UseCases.CreateAchFile;
using AchApi.UseCases.DeleteAchEntry;
using AchApi.UseCases.DeleteAchFile;
using AchApi.UseCases.FinalizeAchFile;
using AchApi.UseCases.GetAchFileContent;
using AchApi.UseCases.ListAchFiles;
using AchApi.UseCases.UpdateAchFileStatus;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AchDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("Ach") ?? "Data Source=ach.db"));

builder.Services.ConfigureHttpJsonOptions(opts =>
    opts.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

// Output adapters
builder.Services.AddScoped<IAchFileGateway, AchFileGateway>();
builder.Services.AddScoped<INachaGeneratorGateway, NachaGeneratorGateway>();

// Interactors
builder.Services.AddScoped<ICreateAchFileInputBoundary, CreateAchFileInteractor>();
builder.Services.AddScoped<IAddAchEntryInputBoundary, AddAchEntryInteractor>();
builder.Services.AddScoped<IFinalizeAchFileInputBoundary, FinalizeAchFileInteractor>();
builder.Services.AddScoped<IDeleteAchFileInputBoundary, DeleteAchFileInteractor>();
builder.Services.AddScoped<IDeleteAchEntryInputBoundary, DeleteAchEntryInteractor>();
builder.Services.AddScoped<IUpdateAchFileStatusInputBoundary, UpdateAchFileStatusInteractor>();
builder.Services.AddScoped<IGetAchFileContentInputBoundary, GetAchFileContentInteractor>();
builder.Services.AddScoped<IListAchFilesInputBoundary, ListAchFilesInteractor>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<AchDbContext>().Database.EnsureCreated();

app.MapControllers();
app.MapGet("/health", () => Results.Ok("healthy"));

app.Run();

public partial class Program { }
