namespace Vixly.Platform.Application.UseCases.Payments.GetRegistrationInvoice;

// =========================================================
// FILE: GetRegistrationInvoiceRequest.cs
//
// PURPOSE:
// Shows a self-registering company the invoice it must pay (D-P7).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetRegistrationInvoiceRequest
{
    public string? AccessToken { get; init; }
}
