using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Heracles.Integration.Tests;

public class AchEntryValidationIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task AddEntry_WithUnknownType_ReturnsBadRequest()
    {
        var createResponse = await AchClient.PostAsync("/files", null);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<CreateAchFileResponse>();

        var entryResponse = await AchClient.PostAsJsonAsync($"/files/{created!.FileId}/entries/full", new
        {
            PaymentId = Guid.NewGuid(),
            RoutingNumber = "021000021",
            AccountNumber = "123456789",
            AccountHolderName = "Jane Doe",
            Amount = 25.00m,
            Type = "Wire"
        });

        Assert.Equal(HttpStatusCode.BadRequest, entryResponse.StatusCode);
        Assert.Equal("Entry type must be Credit or Debit.", await entryResponse.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task AddEntry_WithZeroAmount_ReturnsBadRequest()
    {
        var createResponse = await AchClient.PostAsync("/files", null);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<CreateAchFileResponse>();

        var entryResponse = await AchClient.PostAsJsonAsync($"/files/{created!.FileId}/entries/full", new
        {
            PaymentId = Guid.NewGuid(),
            RoutingNumber = "021000021",
            AccountNumber = "123456789",
            AccountHolderName = "Jane Doe",
            Amount = 0m,
            Type = "Credit"
        });

        Assert.Equal(HttpStatusCode.BadRequest, entryResponse.StatusCode);
        Assert.Equal("Entry amount must be positive.", await entryResponse.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task AddEntry_WithInvalidRoutingNumber_ReturnsBadRequest()
    {
        var createResponse = await AchClient.PostAsync("/files", null);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<CreateAchFileResponse>();

        var entryResponse = await AchClient.PostAsJsonAsync($"/files/{created!.FileId}/entries/full", new
        {
            PaymentId = Guid.NewGuid(),
            RoutingNumber = "ABC",
            AccountNumber = "123456789",
            AccountHolderName = "Jane Doe",
            Amount = 25.00m,
            Type = "Credit"
        });

        Assert.Equal(HttpStatusCode.BadRequest, entryResponse.StatusCode);
        Assert.Equal("Routing number must be 9 digits.", await entryResponse.Content.ReadAsStringAsync());
    }

    private record CreateAchFileResponse(Guid FileId, int BatchNumber);
}
