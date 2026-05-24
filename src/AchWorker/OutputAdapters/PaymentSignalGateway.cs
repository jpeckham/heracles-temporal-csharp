using AchWorker.Gateways;
using Shared.Contracts;
using Temporalio.Client;

namespace AchWorker.OutputAdapters;

public class PaymentSignalGateway(ITemporalClient temporalClient) : IPaymentSignalGateway
{
    public async Task SignalAddedToBatchAsync(Guid paymentId, Guid achFileId, bool isSameDayAch)
    {
        var handle = temporalClient.GetWorkflowHandle($"payment-{paymentId}");
        await handle.SignalAsync("AddedToBatch", [new BatchDetails(achFileId, isSameDayAch)]);
    }

    public async Task SignalBankReturnAsync(Guid paymentId, AchReturnDetails details)
    {
        var handle = temporalClient.GetWorkflowHandle($"payment-{paymentId}");
        await handle.SignalAsync("BankReturn", [details]);
    }
}
