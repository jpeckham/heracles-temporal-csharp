using System.Net.Http.Json;
using AchWorker.Gateways;
using Shared.Contracts;
using Shared.Models;

namespace AchWorker.OutputAdapters;

public class PaymentApiGateway(IHttpClientFactory httpFactory) : IPaymentGateway
{
    private HttpClient PaymentClient => httpFactory.CreateClient("PaymentApi");

    public async Task<List<Guid>> CollectPendingAsync()
    {
        var resp = await PaymentClient.GetFromJsonAsync<List<PaymentSummary>>("/payments?status=Pending");
        return resp?.Select(p => p.PaymentId).ToList() ?? [];
    }

    public async Task<PaymentDetail> GetDetailAsync(Guid paymentId)
    {
        var payment = await PaymentClient.GetFromJsonAsync<PaymentDetailResponse>($"/payments/{paymentId}");
        if (payment is null) throw new ApplicationException($"Payment {paymentId} not found");
        return new PaymentDetail(payment.PaymentId, payment.RoutingNumber, payment.AccountNumber,
            payment.AccountHolderName, payment.Amount, payment.Type);
    }

    public async Task AddActivityAsync(Guid paymentId, PaymentActivityType type, string? referenceCode = null, string? notes = null)
    {
        var req = new AddPaymentActivityRequest(type, ReferenceCode: referenceCode, Notes: notes);
        var resp = await PaymentClient.PostAsJsonAsync($"/payments/{paymentId}/activities", req);
        if (!resp.IsSuccessStatusCode)
            throw new ApplicationException($"AddActivity ({type}) failed for {paymentId}: {resp.StatusCode}");
    }

    public async Task<bool> ExistsAsync(Guid paymentId)
    {
        var resp = await PaymentClient.GetAsync($"/payments/{paymentId}");
        return resp.IsSuccessStatusCode;
    }

    private record PaymentSummary(Guid PaymentId, string CurrentStatus);
    private record PaymentDetailResponse(Guid PaymentId, string RoutingNumber, string AccountNumber,
        string AccountHolderName, decimal Amount, string Type);
}
