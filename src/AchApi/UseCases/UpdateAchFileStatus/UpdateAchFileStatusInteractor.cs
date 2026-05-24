using AchApi.Gateways;
using Shared.Models;

namespace AchApi.UseCases.UpdateAchFileStatus;

public class UpdateAchFileStatusInteractor(IAchFileGateway gateway) : IUpdateAchFileStatusInputBoundary
{
    public async Task UpdateAchFileStatusAsync(IUpdateAchFileStatusOutputBoundary presenter, UpdateAchFileStatusRequestModel request)
    {
        var file = await gateway.GetAchFileByIdAsync(request.FileId);
        if (file is null)
        {
            presenter.PresentNotFound();
            return;
        }

        if (!Enum.TryParse<AchFileStatus>(request.Status, out var status))
        {
            presenter.PresentBadRequest($"Invalid status: {request.Status}");
            return;
        }

        file.Status = status;
        await gateway.SaveAchFileAsync(file);
        presenter.Present(new UpdateAchFileStatusResponseModel());
    }
}
