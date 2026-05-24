using Microsoft.EntityFrameworkCore;
using PaymentApi.Data;
using PaymentApi.Gateways;
using PaymentApi.OutputAdapters;
using PaymentApi.UseCases.AddPaymentActivity;
using PaymentApi.UseCases.GetPayment;
using PaymentApi.UseCases.ListPayments;
using PaymentApi.UseCases.MakePayment;
using Temporalio.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<PaymentDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("Payments") ?? "Data Source=payments.db"));

builder.Services.AddSingleton<ITemporalClient>(_ =>
    TemporalClient.ConnectAsync(new(
        builder.Configuration["Temporal:Address"] ?? "localhost:7233")).GetAwaiter().GetResult());

builder.Services.AddScoped<IPaymentGateway, PaymentGateway>();
builder.Services.AddScoped<IPaymentEventGateway, PaymentEventGateway>();
builder.Services.AddScoped<IMakePaymentInputBoundary, MakePaymentInteractor>();
builder.Services.AddScoped<IGetPaymentInputBoundary, GetPaymentInteractor>();
builder.Services.AddScoped<IListPaymentsInputBoundary, ListPaymentsInteractor>();
builder.Services.AddScoped<IAddPaymentActivityInputBoundary, AddPaymentActivityInteractor>();

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<PaymentDbContext>().Database.EnsureCreated();

app.MapControllers();
app.MapGet("/health", () => Results.Ok("healthy"));

app.Run();

public partial class Program { }
