using AchApi.Gateways;
using Shared.Models;

namespace AchApi.UseCases.FinalizeAchFile;

public class FinalizeAchFileInteractor(IAchFileGateway fileGateway, INachaGeneratorGateway nachaGenerator) : IFinalizeAchFileInputBoundary
{
    public async Task FinalizeAchFileAsync(IFinalizeAchFileOutputBoundary presenter, FinalizeAchFileRequestModel request)
    {
        var file = await fileGateway.GetAchFileWithEntriesAsync(request.FileId);
        if (file is null)
        {
            presenter.PresentNotFound();
            return;
        }

        if (file.Entries.Count == 0)
        {
            presenter.PresentBadRequest("Cannot finalize empty file.");
            return;
        }

        file.NachaContent = nachaGenerator.Generate(file);
        file.Status = AchFileStatus.Finalized;
        file.FinalizedAt = DateTime.UtcNow;
        await fileGateway.SaveAchFileAsync(file);

        var contentBase64 = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(file.NachaContent));
        presenter.Present(new FinalizeAchFileResponseModel(file.FileId, file.Status, contentBase64));
    }
}
