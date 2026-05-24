using AchWorker.Gateways;

namespace AchWorker.UseCases.DeleteAchFile;

public class DeleteAchFileInteractor(IAchFileGateway achFileGateway) : IDeleteAchFileInputBoundary
{
    public async Task DeleteAchFileAsync(IDeleteAchFileOutputBoundary presenter, DeleteAchFileRequestModel request)
    {
        await achFileGateway.DeleteAsync(request.FileId);
        presenter.Present(new DeleteAchFileResponseModel());
    }
}
