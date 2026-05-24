using AchApi.Gateways;

namespace AchApi.UseCases.DeleteAchFile;

public class DeleteAchFileInteractor(IAchFileGateway gateway) : IDeleteAchFileInputBoundary
{
    public async Task DeleteAchFileAsync(IDeleteAchFileOutputBoundary presenter, DeleteAchFileRequestModel request)
    {
        var file = await gateway.GetAchFileByIdAsync(request.FileId);
        if (file is not null)
            await gateway.DeleteAchFileAsync(file);

        presenter.Present(new DeleteAchFileResponseModel());
    }
}
