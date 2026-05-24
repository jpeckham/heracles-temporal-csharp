using AchApi.Gateways;

namespace AchApi.UseCases.CreateAchFile;

public class CreateAchFileInteractor(IAchFileGateway gateway) : ICreateAchFileInputBoundary
{
    public async Task CreateAchFileAsync(ICreateAchFileOutputBoundary presenter, CreateAchFileRequestModel request)
    {
        var file = await gateway.CreateAchFileAsync();
        presenter.Present(new CreateAchFileResponseModel(file.FileId, file.BatchNumber));
    }
}
