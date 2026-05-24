namespace AchApi.UseCases.UpdateAchFileStatus;

public interface IUpdateAchFileStatusInputBoundary
{
    Task UpdateAchFileStatusAsync(IUpdateAchFileStatusOutputBoundary presenter, UpdateAchFileStatusRequestModel request);
}
