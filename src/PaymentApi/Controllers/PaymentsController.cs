using Microsoft.AspNetCore.Mvc;
using PaymentApi.Presenters.AddPaymentActivity;
using PaymentApi.Presenters.GetPayment;
using PaymentApi.Presenters.ListPayments;
using PaymentApi.Presenters.MakePayment;
using PaymentApi.UseCases.AddPaymentActivity;
using PaymentApi.UseCases.GetPayment;
using PaymentApi.UseCases.ListPayments;
using PaymentApi.UseCases.MakePayment;
using Shared.Contracts;

namespace PaymentApi.Controllers;

[ApiController]
[Route("payments")]
public class PaymentsController(
    IMakePaymentInputBoundary makePayment,
    IGetPaymentInputBoundary getPayment,
    IListPaymentsInputBoundary listPayments,
    IAddPaymentActivityInputBoundary addActivity) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreatePayment(CreatePaymentRequest req)
    {
        if (req.Amount <= 0 || req.Amount > 99_999_999.99m)
            return BadRequest("Amount must be between 0.01 and 99,999,999.99.");
        if (req.RoutingNumber.Length != 9 || !req.RoutingNumber.All(char.IsDigit))
            return BadRequest("Routing number must be 9 digits.");
        if (string.IsNullOrWhiteSpace(req.AccountNumber))
            return BadRequest("Account number is required.");
        if (req.AccountNumber.Length > 17)
            return BadRequest("Account number must be 17 chars or fewer.");
        if (req.AccountHolderName.Length > 22)
            return BadRequest("Account holder name must be 22 chars or fewer (NACHA limit).");

        var presenter = new MakePaymentPresenter();
        var request = new MakePaymentRequestModel(
            req.RoutingNumber, req.AccountNumber, req.AccountHolderName,
            req.Amount, req.Type, req.AllowsRepresentment);

        await makePayment.MakePaymentAsync(presenter, request);
        return Created($"/payments/{presenter.ViewModel!.PaymentId}", presenter.ViewModel);
    }

    [HttpGet]
    public async Task<IActionResult> ListPayments([FromQuery] string? status)
    {
        var presenter = new ListPaymentsPresenter();
        await listPayments.ListPaymentsAsync(presenter, new ListPaymentsRequestModel(status));
        return Ok(presenter.ViewModel);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPayment(Guid id)
    {
        var presenter = new GetPaymentPresenter();
        await getPayment.GetPaymentAsync(presenter, new GetPaymentRequestModel(id));
        return presenter.NotFound ? NotFound() : Ok(presenter.ViewModel);
    }

    [HttpPost("{id:guid}/activities")]
    public async Task<IActionResult> AddActivity(Guid id, AddPaymentActivityRequest req)
    {
        var presenter = new AddPaymentActivityPresenter();
        var request = new AddPaymentActivityRequestModel(id, req.Type, req.Amount, req.ReferenceCode, req.Notes);
        await addActivity.AddPaymentActivityAsync(presenter, request);

        if (presenter.NotFound) return NotFound();
        return Created($"/payments/{id}/activities/{presenter.ViewModel!.ActivityId}", presenter.ViewModel);
    }
}
