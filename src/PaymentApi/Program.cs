using Microsoft.EntityFrameworkCore;
using PaymentApi.Data;
using Shared.Contracts;
using Temporalio.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<PaymentDbContext>(opt =>
    opt.UseSqlite("Data Source=payments.db"));
builder.Services.ConfigureHttpJsonOptions(opts =>
    opts.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

builder.Services.AddSingleton<ITemporalClient>(_ =>
    TemporalClient.ConnectAsync(new("localhost:7233")).GetAwaiter().GetResult());

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<PaymentDbContext>().Database.EnsureCreated();

app.MapPost("/payments", async (CreatePaymentRequest req, PaymentDbContext db, ITemporalClient temporal) =>
{
    if (req.Amount <= 0 || req.Amount > 99_999_999.99m)
        return Results.BadRequest("Amount must be between 0.01 and 99,999,999.99.");

    if (req.RoutingNumber.Length != 9 || !req.RoutingNumber.All(char.IsDigit))
        return Results.BadRequest("Routing number must be 9 digits.");

    if (req.AccountNumber.Length > 17)
        return Results.BadRequest("Account number must be 17 chars or fewer.");

    if (req.AccountHolderName.Length > 22)
        return Results.BadRequest("Account holder name must be 22 chars or fewer (NACHA limit).");

    var payment = new Payment
    {
        RoutingNumber = req.RoutingNumber,
        AccountNumber = req.AccountNumber,
        AccountHolderName = req.AccountHolderName,
        Amount = req.Amount,
        Type = req.Type,
        AllowsRepresentment = req.AllowsRepresentment
    };
    db.Payments.Add(payment);
    await db.SaveChangesAsync();

    await temporal.StartWorkflowAsync(
        "PaymentWorkflow",
        new object[] { payment.PaymentId, payment.AllowsRepresentment },
        new WorkflowOptions(id: $"payment-{payment.PaymentId}", taskQueue: "ach-worker"));

    return Results.Created($"/payments/{payment.PaymentId}", new { payment.PaymentId });
});

app.MapGet("/payments", async (PaymentDbContext db, string? status) =>
{
    var payments = await db.Payments.Include(p => p.Activities)
        .OrderByDescending(p => p.CreatedAt).Take(1000).ToListAsync();
    var result = status != null
        ? payments.Where(p => p.CurrentStatus == status).ToList()
        : payments;
    return Results.Ok(result.Select(p => new {
        p.PaymentId, p.AccountHolderName, p.Amount, p.Type,
        p.AllowsRepresentment, p.CurrentStatus, p.CreatedAt
    }));
});

app.MapGet("/payments/{id:guid}", async (Guid id, PaymentDbContext db) =>
{
    var payment = await db.Payments.Include(p => p.Activities)
        .FirstOrDefaultAsync(p => p.PaymentId == id);
    return payment is null ? Results.NotFound() : Results.Ok(payment);
});

app.MapPost("/payments/{id:guid}/activities", async (Guid id, AddPaymentActivityRequest req, PaymentDbContext db) =>
{
    var payment = await db.Payments.FindAsync(id);
    if (payment is null) return Results.NotFound();

    var activity = new PaymentActivity
    {
        PaymentId = id,
        Type = req.Type,
        Amount = req.Amount,
        ReferenceCode = req.ReferenceCode,
        Notes = req.Notes
    };
    db.PaymentActivities.Add(activity);
    await db.SaveChangesAsync();
    return Results.Created($"/payments/{id}/activities/{activity.ActivityId}", activity);
});

app.MapGet("/health", () => Results.Ok("healthy"));

app.Run();

public partial class Program { }
