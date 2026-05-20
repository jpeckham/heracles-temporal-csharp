namespace Shared.Models;

public enum PaymentActivityType
{
    SoftAuth,
    HardAuth,
    Capture,
    Settlement,
    AchSubmitted,
    AchReturn,
    Representment,
    Void,
    Dispute,
    DisputeReversed,
    Refund,
    PaidOut
}
