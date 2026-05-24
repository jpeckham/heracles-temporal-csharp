using System.Net.Http.Json;
using AchWorker.Gateways;

namespace AchWorker.OutputAdapters;

public class AchApiGateway(IHttpClientFactory httpFactory) : IAchFileGateway
{
    private HttpClient AchClient => httpFactory.CreateClient("AchApi");

    public async Task<Guid> CreateAsync()
    {
        var resp = await AchClient.PostAsJsonAsync("/files", new { });
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<FileCreatedResponse>();
        return result!.FileId;
    }

    public async Task<Guid> AddEntryAsync(Guid fileId, Guid paymentId, string routingNumber, string accountNumber,
        string accountHolderName, decimal amount, string type, int representmentCount)
    {
        var req = new
        {
            PaymentId = paymentId,
            RoutingNumber = routingNumber,
            AccountNumber = accountNumber,
            AccountHolderName = accountHolderName,
            Amount = amount,
            Type = type,
            RepresentmentCount = representmentCount
        };
        var resp = await AchClient.PostAsJsonAsync($"/files/{fileId}/entries/full", req);
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<EntryCreatedResponse>();
        return result!.EntryId;
    }

    public async Task FinalizeAsync(Guid fileId)
    {
        var resp = await AchClient.PostAsJsonAsync($"/files/{fileId}/finalize", new { });
        if (!resp.IsSuccessStatusCode)
            throw new ApplicationException($"Finalize failed: {await resp.Content.ReadAsStringAsync()}");
    }

    public async Task DeleteAsync(Guid fileId)
    {
        var resp = await AchClient.DeleteAsync($"/files/{fileId}");
        if (!resp.IsSuccessStatusCode && resp.StatusCode != System.Net.HttpStatusCode.NotFound)
            throw new ApplicationException($"DeleteAchFile failed: {resp.StatusCode}");
    }

    public async Task RevertToDraftAsync(Guid fileId)
    {
        var resp = await AchClient.SendAsync(new HttpRequestMessage(HttpMethod.Patch, $"/files/{fileId}/status")
        {
            Content = JsonContent.Create(new { Status = "Draft" })
        });
        if (!resp.IsSuccessStatusCode)
            throw new ApplicationException($"RevertAchFileToDraft failed: {resp.StatusCode}");
    }

    public async Task<string> GetContentBase64Async(Guid fileId)
    {
        var content = await AchClient.GetFromJsonAsync<ContentResponse>($"/files/{fileId}/content");
        if (content is null) throw new ApplicationException("ACH file content not found");
        return content.ContentBase64;
    }

    private record FileCreatedResponse(Guid FileId);
    private record EntryCreatedResponse(Guid EntryId);
    private record ContentResponse(string ContentBase64);
}
