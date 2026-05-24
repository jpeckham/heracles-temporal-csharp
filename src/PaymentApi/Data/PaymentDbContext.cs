using Microsoft.EntityFrameworkCore;
using PaymentApi.Entities;

namespace PaymentApi.Data;

public class PaymentDbContext(DbContextOptions<PaymentDbContext> options) : DbContext(options)
{
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentActivity> PaymentActivities => Set<PaymentActivity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PaymentActivity>()
            .HasKey(a => a.ActivityId);

        modelBuilder.Entity<Payment>()
            .HasMany(p => p.Activities)
            .WithOne()
            .HasForeignKey(a => a.PaymentId);
    }
}
