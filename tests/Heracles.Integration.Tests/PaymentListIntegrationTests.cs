using Shared.Contracts;
using Shared.Models;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace Heracles.Integration.Tests;

public class PaymentListIntegrationTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task ListPayments_StatusFilter_TrimsAndIgnoresCase()
    {
        var createRequest = new CreatePaymentRequest(
            RoutingNumber: "021000021",
            AccountNumber: "123456789",
            AccountHolderName: "Jane Doe",
            Amount: 25.00m,
            Type: PaymentType.Credit,
            AllowsRepresentment: true);
        var createResponse = await PaymentClient.PostAsJsonAsync("/payments", createRequest, JsonOpts);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<PaymentCreatedResponse>(JsonOpts);

        var listResponse = await PaymentClient.GetAsync("/payments?status=%20pending%20");
        listResponse.EnsureSuccessStatusCode();
        var payments = await listResponse.Content.ReadFromJsonAsync<List<ListPaymentResponse>>(JsonOpts);

        Assert.Contains(payments!, payment => payment.PaymentId == created!.PaymentId);
    }

    private record PaymentCreatedResponse(Guid PaymentId);
    private record ListPaymentResponse(Guid PaymentId);
}
