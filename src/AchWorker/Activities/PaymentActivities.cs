using System.Net.Http.Json;
using Shared.Contracts;
using Shared.Models;
using Temporalio.Activities;
using Temporalio.Client;

namespace AchWorker.Activities;

public class PaymentActivities(IHttpClientFactory httpFactory, ITemporalClient temporalClient)
{
    private HttpClient PaymentClient => httpFactory.CreateClient("PaymentApi");

    [Activity]
    public async Task<List<Guid>> CollectPendingPaymentsAsync()
    {
        var resp = await PaymentClient.GetFromJsonAsync<List<PaymentSummary>>("/payments?status=Pending");
        return resp?.Select(p => p.PaymentId).ToList() ?? [];
    }

    [Activity]
    public async Task HardAuthAsync(Guid paymentId)
    {
        var req = new AddPaymentActivityRequest(PaymentActivityType.HardAuth);
        var resp = await PaymentClient.PostAsJsonAsync($"/payments/{paymentId}/activities", req);
        if (!resp.IsSuccessStatusCode)
            throw new ApplicationException($"HardAuth failed for {paymentId}: {resp.StatusCode}");
    }

    [Activity]
    public async Task VoidPaymentAuthIfExistsAsync(Guid paymentId)
    {
        var checkResp = await PaymentClient.GetAsync($"/payments/{paymentId}");
        if (!checkResp.IsSuccessStatusCode) return;

        var req = new AddPaymentActivityRequest(PaymentActivityType.Void);
        await PaymentClient.PostAsJsonAsync($"/payments/{paymentId}/activities", req);
    }

    [Activity]
    public async Task RecordSettlementAsync(Guid paymentId)
    {
        var req = new AddPaymentActivityRequest(PaymentActivityType.Settlement);
        await PaymentClient.PostAsJsonAsync($"/payments/{paymentId}/activities", req);
        var req2 = new AddPaymentActivityRequest(PaymentActivityType.PaidOut);
        await PaymentClient.PostAsJsonAsync($"/payments/{paymentId}/activities", req2);
    }

    [Activity]
    public async Task RecordAchReturnAsync(Guid paymentId, AchReturnDetails details)
    {
        var req = new AddPaymentActivityRequest(PaymentActivityType.AchReturn,
            ReferenceCode: details.RCode, Notes: details.Description);
        var resp = await PaymentClient.PostAsJsonAsync($"/payments/{paymentId}/activities", req);
        if (!resp.IsSuccessStatusCode)
            throw new ApplicationException($"RecordAchReturn failed: {resp.StatusCode}");
    }

    [Activity]
    public async Task RecordRepresentmentAsync(Guid paymentId, int representmentCount)
    {
        var req = new AddPaymentActivityRequest(PaymentActivityType.Representment,
            Notes: $"Attempt {representmentCount}");
        await PaymentClient.PostAsJsonAsync($"/payments/{paymentId}/activities", req);
        // Reset to SoftAuth so the payment is re-queued for next batch
        var pendingReq = new AddPaymentActivityRequest(PaymentActivityType.SoftAuth,
            Notes: "Re-queued for representment");
        await PaymentClient.PostAsJsonAsync($"/payments/{paymentId}/activities", pendingReq);
    }

    [Activity]
    public async Task SignalPaymentAddedToBatchAsync(Guid paymentId, Guid achFileId, bool isSameDayAch)
    {
        var handle = temporalClient.GetWorkflowHandle($"payment-{paymentId}");
        await handle.SignalAsync("AddedToBatch", [new BatchDetails(achFileId, isSameDayAch)]);
    }

    [Activity]
    public async Task SignalBankReturnAsync(Guid paymentId, AchReturnDetails details)
    {
        var handle = temporalClient.GetWorkflowHandle($"payment-{paymentId}");
        await handle.SignalAsync("BankReturn", [details]);
    }

    private record PaymentSummary(Guid PaymentId, string CurrentStatus);
}
