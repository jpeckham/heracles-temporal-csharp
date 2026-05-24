namespace AchApi.Entities;

public class AchEntry
{
    public Guid EntryId { get; set; } = Guid.NewGuid();
    public Guid FileId { get; set; }
    public Guid PaymentId { get; set; }
    public string RoutingNumber { get; set; } = "";
    public string AccountNumber { get; set; } = "";
    public string AccountHolderName { get; set; } = "";
    public decimal Amount { get; set; }
    public string TransactionCode { get; set; } = "";  // 22=Credit, 27=Debit
    public int RepresentmentCount { get; set; }
}
