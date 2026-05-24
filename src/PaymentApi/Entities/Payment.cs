using Shared.Models;

namespace PaymentApi.Entities;

public class Payment
{
    public Guid PaymentId { get; set; } = Guid.NewGuid();
    public string RoutingNumber { get; set; } = "";
    public string AccountNumber { get; set; } = "";
    public string AccountHolderName { get; set; } = "";
    public decimal Amount { get; set; }
    public PaymentType Type { get; set; }
    public bool AllowsRepresentment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<PaymentActivity> Activities { get; set; } = [];

    public string CurrentStatus => Activities.Count == 0
        ? "Pending"
        : Activities.MaxBy(a => a.OccurredAt)!.Type.ToString();
}
