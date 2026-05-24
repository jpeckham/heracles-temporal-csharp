using AchWorker.Gateways;

namespace AchWorker.UseCases.TransferAchFile;

public class TransferAchFileInteractor(IAchFileGateway achFileGateway, ISftpGateway sftpGateway) : ITransferAchFileInputBoundary
{
    public async Task TransferAchFileAsync(ITransferAchFileOutputBoundary presenter, TransferAchFileRequestModel request)
    {
        var contentBase64 = await achFileGateway.GetContentBase64Async(request.AchFileId);
        var fileName = $"ACH_{DateTime.UtcNow:yyyyMMdd_HHmmss}.txt";
        var transferredFileId = await sftpGateway.TransferFileAsync(request.AchFileId, fileName, contentBase64);
        presenter.Present(new TransferAchFileResponseModel(transferredFileId));
    }
}
