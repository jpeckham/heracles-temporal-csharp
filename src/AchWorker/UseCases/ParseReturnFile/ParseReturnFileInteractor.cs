using AchWorker.Entities;
using AchWorker.Gateways;

namespace AchWorker.UseCases.ParseReturnFile;

public class ParseReturnFileInteractor(ISftpGateway sftpGateway) : IParseReturnFileInputBoundary
{
    public async Task ParseReturnFileAsync(IParseReturnFileOutputBoundary presenter, ParseReturnFileRequestModel request)
    {
        var contentBase64 = await sftpGateway.GetInboundContentBase64Async(request.ReceivedFileId);
        var records = new List<AchReturnRecord>();

        if (contentBase64 is not null)
        {
            var nachaText = System.Text.Encoding.ASCII.GetString(Convert.FromBase64String(contentBase64));
            foreach (var line in nachaText.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                if (line.Length < 94 || line[0] != '7') continue;
                var rCode = line[3..6].Trim();
                if (Guid.TryParse(line[13..49].Trim(), out var paymentId))
                    records.Add(new AchReturnRecord(paymentId, rCode));
            }
        }

        presenter.Present(new ParseReturnFileResponseModel(records));
    }
}
