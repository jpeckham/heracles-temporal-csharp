using System.Net.Http.Json;
using Temporalio.Activities;

namespace AchWorker.Activities;

public class SftpActivities(IHttpClientFactory httpFactory)
{
    private HttpClient SftpClient => httpFactory.CreateClient("SftpApi");
    private HttpClient AchClient => httpFactory.CreateClient("AchApi");

    [Activity]
    public async Task<Guid> TransferAchFileAsync(Guid achFileId)
    {
        var content = await AchClient.GetFromJsonAsync<ContentResponse>($"/files/{achFileId}/content");
        if (content is null) throw new ApplicationException("ACH file content not found");

        var fileName = $"ACH_{DateTime.UtcNow:yyyyMMdd_HHmmss}.txt";
        var req = new
        {
            AchFileId = achFileId,
            FileName = fileName,
            content.ContentBase64
        };

        var resp = await SftpClient.PostAsJsonAsync("/files/outbound", req);
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<TransferResponse>();
        return result!.FileId;
    }

    [Activity]
    public async Task DeleteTransferredFileIfExistsAsync(Guid achFileId)
    {
        await SftpClient.DeleteAsync($"/files/outbound/by-ach/{achFileId}");
    }

    [Activity]
    public async Task MarkReceivedFileProcessedAsync(Guid receivedFileId)
    {
        await SftpClient.SendAsync(new HttpRequestMessage(HttpMethod.Patch, $"/files/inbound/{receivedFileId}/status")
        {
            Content = JsonContent.Create(new { Status = "Processed" })
        });
    }

    private record ContentResponse(string ContentBase64);
    private record TransferResponse(Guid FileId);
}
