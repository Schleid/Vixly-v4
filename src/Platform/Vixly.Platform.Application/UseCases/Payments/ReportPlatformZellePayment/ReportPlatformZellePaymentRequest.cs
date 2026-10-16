namespace Vixly.Platform.Application.UseCases.Payments.ReportPlatformZellePayment;

// =========================================================
// FILE: ReportPlatformZellePaymentRequest.cs
//
// PURPOSE:
// Takes a customer's "I paid with Zelle" report from a pay link; staff check it before anything is paid.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ReportPlatformZellePaymentRequest
{
    public string? Token { get; init; }

    public decimal Amount { get; init; }

    public DateTime? SentOn { get; init; }

    public string? SenderName { get; init; }

    public string? Reference { get; init; }
}
