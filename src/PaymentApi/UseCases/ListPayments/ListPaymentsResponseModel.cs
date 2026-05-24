using PaymentApi.Entities;

namespace PaymentApi.UseCases.ListPayments;

public record ListPaymentsResponseModel(List<Payment> Payments);
