using Microsoft.EntityFrameworkCore;
using PaymentApi.Data;
using PaymentApi.Entities;
using PaymentApi.Gateways;

namespace PaymentApi.OutputAdapters;

public class PaymentGateway(PaymentDbContext db) : IPaymentGateway
{
    public async Task SaveAsync(Payment payment)
    {
        db.Payments.Add(payment);
        await db.SaveChangesAsync();
    }

    public async Task<Payment?> FindByIdAsync(Guid paymentId) =>
        await db.Payments.Include(p => p.Activities)
            .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

    public async Task<List<Payment>> FindAllAsync() =>
        await db.Payments.Include(p => p.Activities)
            .OrderByDescending(p => p.CreatedAt)
            .Take(1000)
            .ToListAsync();

    public async Task SaveActivityAsync(PaymentActivity activity)
    {
        db.PaymentActivities.Add(activity);
        await db.SaveChangesAsync();
    }
}
