using AchWorker.Gateways;

namespace AchWorker.UseCases.RevertAchFileToDraft;

public class RevertAchFileToDraftInteractor(IAchFileGateway achFileGateway) : IRevertAchFileToDraftInputBoundary
{
    public async Task RevertAchFileToDraftAsync(IRevertAchFileToDraftOutputBoundary presenter, RevertAchFileToDraftRequestModel request)
    {
        await achFileGateway.RevertToDraftAsync(request.FileId);
        presenter.Present(new RevertAchFileToDraftResponseModel());
    }
}
