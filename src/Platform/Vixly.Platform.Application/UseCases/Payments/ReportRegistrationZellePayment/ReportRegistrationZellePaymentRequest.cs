namespace Vixly.Platform.Application.UseCases.Payments.ReportRegistrationZellePayment;

// =========================================================
// FILE: ReportRegistrationZellePaymentRequest.cs
//
// PURPOSE:
// Takes a self-registering company's "I paid with Zelle" report for its first invoice.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ReportRegistrationZellePaymentRequest
{
    public string? AccessToken { get; init; }

    public decimal Amount { get; init; }

    public DateTime? SentOn { get; init; }

    public string? SenderName { get; init; }

    public string? Reference { get; init; }
}
