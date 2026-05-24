using AchWorker.Gateways;

namespace AchWorker.UseCases.FinalizeAchFile;

public class FinalizeAchFileInteractor(IAchFileGateway achFileGateway) : IFinalizeAchFileInputBoundary
{
    public async Task FinalizeAchFileAsync(IFinalizeAchFileOutputBoundary presenter, FinalizeAchFileRequestModel request)
    {
        await achFileGateway.FinalizeAsync(request.FileId);
        presenter.Present(new FinalizeAchFileResponseModel());
    }
}
