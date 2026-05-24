using Shared.Models;

namespace PaymentApi.Entities;

public class PaymentActivity
{
    public Guid ActivityId { get; set; } = Guid.NewGuid();
    public Guid PaymentId { get; set; }
    public PaymentActivityType Type { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public decimal? Amount { get; set; }
    public string? ReferenceCode { get; set; }
    public string? Notes { get; set; }
}
