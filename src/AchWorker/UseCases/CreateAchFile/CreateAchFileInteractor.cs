using AchWorker.Gateways;

namespace AchWorker.UseCases.CreateAchFile;

public class CreateAchFileInteractor(IAchFileGateway achFileGateway) : ICreateAchFileInputBoundary
{
    public async Task CreateAchFileAsync(ICreateAchFileOutputBoundary presenter, CreateAchFileRequestModel request)
    {
        var fileId = await achFileGateway.CreateAsync();
        presenter.Present(new CreateAchFileResponseModel(fileId));
    }
}
