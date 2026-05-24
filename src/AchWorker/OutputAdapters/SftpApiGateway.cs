using System.Net.Http.Json;
using AchWorker.Gateways;

namespace AchWorker.OutputAdapters;

public class SftpApiGateway(IHttpClientFactory httpFactory) : ISftpGateway
{
    private HttpClient SftpClient => httpFactory.CreateClient("SftpApi");

    public async Task<Guid> TransferFileAsync(Guid achFileId, string fileName, string contentBase64)
    {
        var req = new
        {
            AchFileId = achFileId,
            FileName = fileName,
            ContentBase64 = contentBase64
        };
        var resp = await SftpClient.PostAsJsonAsync("/files/outbound", req);
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<TransferResponse>();
        return result!.FileId;
    }

    public async Task DeleteTransferredAsync(Guid achFileId)
    {
        await SftpClient.DeleteAsync($"/files/outbound/by-ach/{achFileId}");
    }

    public async Task<string?> GetInboundContentBase64Async(Guid receivedFileId)
    {
        var content = await SftpClient.GetFromJsonAsync<ContentResponse>($"/files/inbound/{receivedFileId}/content");
        return content?.ContentBase64;
    }

    public async Task MarkProcessedAsync(Guid receivedFileId)
    {
        var resp = await SftpClient.SendAsync(new HttpRequestMessage(HttpMethod.Patch, $"/files/inbound/{receivedFileId}/status")
        {
            Content = JsonContent.Create(new { Status = "Processed" })
        });
        if (!resp.IsSuccessStatusCode)
            throw new ApplicationException($"MarkReceivedFileProcessed failed: {resp.StatusCode}");
    }

    private record ContentResponse(string ContentBase64);
    private record TransferResponse(Guid FileId);
}
