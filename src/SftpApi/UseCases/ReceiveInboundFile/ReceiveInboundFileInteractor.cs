using System.Security.Cryptography;
using SftpApi.Entities;
using SftpApi.Gateways;
using Temporalio.Client;

namespace SftpApi.UseCases.ReceiveInboundFile;

public class ReceiveInboundFileInteractor(ISftpFileGateway gateway, ITemporalClient temporal) : IReceiveInboundFileInputBoundary
{
    public async Task ExecuteAsync(IReceiveInboundFileOutputBoundary presenter, ReceiveInboundFileRequestModel request)
    {
        var bytes = Convert.FromBase64String(request.ContentBase64);
        var hash = Convert.ToHexString(SHA256.HashData(bytes));

        var file = new ReceivedFile
        {
            FileName = request.FileName,
            ContentBase64 = request.ContentBase64,
            ContentHash = hash
        };

        await gateway.AddReceivedFileAsync(file);

        await temporal.StartWorkflowAsync(
            "AchReturnWorkflow",
            new object[] { file.FileId },
            new WorkflowOptions(id: $"ach-return-{file.FileId}", taskQueue: "ach-worker"));

        presenter.Present(new ReceiveInboundFileResponseModel(file.FileId));
    }
}
