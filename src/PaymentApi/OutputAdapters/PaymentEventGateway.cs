using PaymentApi.Entities;
using PaymentApi.Gateways;
using Temporalio.Client;

namespace PaymentApi.OutputAdapters;

public class PaymentEventGateway(ITemporalClient temporalClient) : IPaymentEventGateway
{
    public async Task PaymentCreatedAsync(Payment payment)
    {
        await temporalClient.StartWorkflowAsync(
            "PaymentWorkflow",
            new object[] { payment.PaymentId, payment.AllowsRepresentment },
            new WorkflowOptions(id: $"payment-{payment.PaymentId}", taskQueue: "ach-worker"));
    }
}
