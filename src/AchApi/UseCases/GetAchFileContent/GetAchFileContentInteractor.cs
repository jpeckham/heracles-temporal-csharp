using AchApi.Gateways;

namespace AchApi.UseCases.GetAchFileContent;

public class GetAchFileContentInteractor(IAchFileGateway gateway) : IGetAchFileContentInputBoundary
{
    public async Task GetAchFileContentAsync(IGetAchFileContentOutputBoundary presenter, GetAchFileContentRequestModel request)
    {
        var file = await gateway.GetAchFileByIdAsync(request.FileId);
        if (file?.NachaContent is null)
        {
            presenter.PresentNotFound();
            return;
        }

        var contentBase64 = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(file.NachaContent));
        presenter.Present(new GetAchFileContentResponseModel(contentBase64));
    }
}
