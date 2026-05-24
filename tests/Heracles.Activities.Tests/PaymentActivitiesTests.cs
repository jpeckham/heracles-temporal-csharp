using AchWorker.Activities;
using AchWorker.OutputAdapters;
using AchWorker.UseCases.CollectPendingPayments;
using AchWorker.UseCases.HardAuthorizePayment;
using AchWorker.UseCases.RecordAchReturn;
using AchWorker.UseCases.RecordRepresentment;
using AchWorker.UseCases.RecordSettlement;
using AchWorker.UseCases.SignalBankReturn;
using AchWorker.UseCases.SignalPaymentAddedToBatch;
using AchWorker.UseCases.VoidPaymentAuth;
using NSubstitute;
using Shared.Contracts;
using Temporalio.Testing;
using Xunit;

namespace Heracles.Activities.Tests;

public class PaymentActivitiesTests
{
    private static PaymentActivities CreateActivities(HttpResponseMessage response)
    {
        var handler = new FakeHttpHandler(response);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://payment-api") };
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("PaymentApi").Returns(httpClient);

        var temporalClient = Substitute.For<Temporalio.Client.ITemporalClient>();
        return BuildPaymentActivities(factory, temporalClient);
    }

    private static PaymentActivities BuildPaymentActivities(IHttpClientFactory factory, Temporalio.Client.ITemporalClient temporalClient)
    {
        var paymentGateway = new PaymentApiGateway(factory);
        var signalGateway = new PaymentSignalGateway(temporalClient);
        return new PaymentActivities(
            new CollectPendingPaymentsInteractor(paymentGateway),
            new HardAuthorizePaymentInteractor(paymentGateway),
            new VoidPaymentAuthInteractor(paymentGateway),
            new RecordSettlementInteractor(paymentGateway),
            new RecordAchReturnInteractor(paymentGateway),
            new RecordRepresentmentInteractor(paymentGateway),
            new SignalPaymentAddedToBatchInteractor(signalGateway),
            new SignalBankReturnInteractor(signalGateway));
    }

    [Fact]
    public async Task CollectPendingPayments_ReturnsList()
    {
        var json = """[{"paymentId":"11111111-1111-1111-1111-111111111111","currentStatus":"Pending"}]""";
        var activities = CreateActivities(new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.OK,
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        });

        var env = new ActivityEnvironment();
        var result = await env.RunAsync(() => activities.CollectPendingPaymentsAsync());

        Assert.Single(result);
        Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), result[0]);
    }

    [Fact]
    public async Task HardAuth_NonSuccessStatus_Throws()
    {
        var activities = CreateActivities(new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.InternalServerError
        });

        var env = new ActivityEnvironment();
        await Assert.ThrowsAsync<ApplicationException>(() =>
            env.RunAsync(() => activities.HardAuthAsync(Guid.NewGuid())));
    }

    [Fact]
    public async Task VoidPaymentAuthIfExists_PaymentNotFound_DoesNotThrow()
    {
        var activities = CreateActivities(new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.NotFound
        });

        var env = new ActivityEnvironment();
        // Should not throw — idempotent when payment not found
        await env.RunAsync(() => activities.VoidPaymentAuthIfExistsAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task RecordSettlement_PostsTwoActivities()
    {
        var callCount = 0;
        var handler = new CountingHttpHandler(_ =>
        {
            callCount++;
            return new HttpResponseMessage(System.Net.HttpStatusCode.Created)
            {
                Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
            };
        });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://payment-api") };
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("PaymentApi").Returns(httpClient);
        var temporalClient = Substitute.For<Temporalio.Client.ITemporalClient>();
        var activities = BuildPaymentActivities(factory, temporalClient);

        var env = new ActivityEnvironment();
        await env.RunAsync(() => activities.RecordSettlementAsync(Guid.NewGuid()));

        Assert.Equal(2, callCount); // Settlement + PaidOut
    }
}

public class FakeHttpHandler(HttpResponseMessage response) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        => Task.FromResult(response);
}

public class CountingHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        => Task.FromResult(handler(request));
}
