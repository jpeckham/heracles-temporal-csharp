using AchWorker.UseCases.DeleteTransferredFile;

namespace AchWorker.Presenters.DeleteTransferredFile;

public class DeleteTransferredFilePresenter : IDeleteTransferredFileOutputBoundary
{
    public DeleteTransferredFileResponseModel? ViewModel { get; private set; }

    public void Present(DeleteTransferredFileResponseModel response)
    {
        ViewModel = response;
    }
}
