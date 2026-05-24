namespace AchWorker.Gateways;

public interface IAchFileGateway
{
    Task<Guid> CreateAsync();
    Task<Guid> AddEntryAsync(Guid fileId, Guid paymentId, string routingNumber, string accountNumber,
        string accountHolderName, decimal amount, string type, int representmentCount);
    Task FinalizeAsync(Guid fileId);
    Task DeleteAsync(Guid fileId);
    Task RevertToDraftAsync(Guid fileId);
    Task<string> GetContentBase64Async(Guid fileId);
}
