using Shared.Contracts;
using Shared.Models;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Heracles.Integration.Tests;

public class PaymentValidationIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task CreatePayment_WithBlankRoutingNumber_ReturnsBadRequest()
    {
        var request = new CreatePaymentRequest(
            RoutingNumber: "   ",
            AccountNumber: "123456789",
            AccountHolderName: "Jane Doe",
            Amount: 25.00m,
            Type: PaymentType.Credit,
            AllowsRepresentment: true);

        var response = await PaymentClient.PostAsJsonAsync("/payments", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Routing number is required.", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task CreatePayment_WithBlankAccountNumber_ReturnsBadRequest()
    {
        var request = new CreatePaymentRequest(
            RoutingNumber: "021000021",
            AccountNumber: "   ",
            AccountHolderName: "Jane Doe",
            Amount: 25.00m,
            Type: PaymentType.Credit,
            AllowsRepresentment: true);

        var response = await PaymentClient.PostAsJsonAsync("/payments", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Account number is required.", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task CreatePayment_WithBlankAccountHolderName_ReturnsBadRequest()
    {
        var request = new CreatePaymentRequest(
            RoutingNumber: "021000021",
            AccountNumber: "123456789",
            AccountHolderName: "   ",
            Amount: 25.00m,
            Type: PaymentType.Credit,
            AllowsRepresentment: true);

        var response = await PaymentClient.PostAsJsonAsync("/payments", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Account holder name is required.", await response.Content.ReadAsStringAsync());
    }
}
