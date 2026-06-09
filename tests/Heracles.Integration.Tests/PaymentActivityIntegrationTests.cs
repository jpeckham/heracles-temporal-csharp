using Shared.Contracts;
using Shared.Models;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace Heracles.Integration.Tests;

public class PaymentActivityIntegrationTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task AddActivity_TrimsReferenceCodeAndNotesBeforeStoring()
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

        var activityRequest = new AddPaymentActivityRequest(
            Type: PaymentActivityType.Settlement,
            Amount: 25.00m,
            ReferenceCode: "  SETTLED-001  ",
            Notes: "  paid out  ");
        var activityResponse = await PaymentClient.PostAsJsonAsync(
            $"/payments/{created!.PaymentId}/activities",
            activityRequest,
            JsonOpts);
        activityResponse.EnsureSuccessStatusCode();
        var activity = await activityResponse.Content.ReadFromJsonAsync<PaymentActivityResponse>(JsonOpts);

        Assert.Equal("SETTLED-001", activity!.ReferenceCode);
        Assert.Equal("paid out", activity.Notes);
    }

    private record PaymentCreatedResponse(Guid PaymentId);
    private record PaymentActivityResponse(string? ReferenceCode, string? Notes);
}
