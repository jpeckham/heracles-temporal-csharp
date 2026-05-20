using System.Net.Http.Json;
using Temporalio.Activities;

namespace AchWorker.Activities;

public class AchActivities(IHttpClientFactory httpFactory)
{
    private HttpClient AchClient => httpFactory.CreateClient("AchApi");
    private HttpClient PaymentClient => httpFactory.CreateClient("PaymentApi");

    [Activity]
    public async Task<Guid> CreateAchFileAsync()
    {
        var resp = await AchClient.PostAsJsonAsync("/files", new { });
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<FileCreatedResponse>();
        return result!.FileId;
    }

    [Activity]
    public async Task<Guid> AddEntryAsync(Guid fileId, Guid paymentId, int representmentCount = 0)
    {
        var payment = await PaymentClient.GetFromJsonAsync<PaymentDetail>($"/payments/{paymentId}");
        if (payment is null) throw new ApplicationException($"Payment {paymentId} not found");

        var req = new
        {
            payment.PaymentId,
            payment.RoutingNumber,
            payment.AccountNumber,
            payment.AccountHolderName,
            payment.Amount,
            payment.Type,   // string enum name ("Credit" or "Debit")
            RepresentmentCount = representmentCount
        };
        var resp = await AchClient.PostAsJsonAsync($"/files/{fileId}/entries/full", req);
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<EntryCreatedResponse>();
        return result!.EntryId;
    }

    [Activity]
    public async Task FinalizeAchFileAsync(Guid fileId)
    {
        var resp = await AchClient.PostAsJsonAsync($"/files/{fileId}/finalize", new { });
        if (!resp.IsSuccessStatusCode)
            throw new ApplicationException($"Finalize failed: {await resp.Content.ReadAsStringAsync()}");
    }

    [Activity]
    public async Task DeleteAchFileIfExistsAsync(Guid fileId)
    {
        await AchClient.DeleteAsync($"/files/{fileId}");
    }

    [Activity]
    public async Task RevertAchFileToDraftAsync(Guid fileId)
    {
        await AchClient.SendAsync(new HttpRequestMessage(HttpMethod.Patch, $"/files/{fileId}/status")
        {
            Content = JsonContent.Create(new { Status = "Draft" })
        });
    }

    [Activity]
    public async Task<List<AchReturnRecordDto>> ParseReturnFileAsync(Guid receivedFileId)
    {
        var sftpClient = httpFactory.CreateClient("SftpApi");
        var content = await sftpClient.GetFromJsonAsync<ContentResponse>($"/files/inbound/{receivedFileId}/content");
        if (content is null) return [];

        var nachaText = System.Text.Encoding.ASCII.GetString(Convert.FromBase64String(content.ContentBase64));
        var returns = new List<AchReturnRecordDto>();

        foreach (var line in nachaText.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.Length < 94 || line[0] != '7') continue;
            var rCode = line[3..6].Trim();
            if (Guid.TryParse(line[13..49].Trim(), out var paymentId))
                returns.Add(new AchReturnRecordDto(paymentId, rCode));
        }

        return returns;
    }

    private record FileCreatedResponse(Guid FileId);
    private record EntryCreatedResponse(Guid EntryId);
    private record PaymentDetail(Guid PaymentId, string RoutingNumber, string AccountNumber,
        string AccountHolderName, decimal Amount, string Type);
    private record ContentResponse(string ContentBase64);
    public record AchReturnRecordDto(Guid PaymentId, string RCode);
}
