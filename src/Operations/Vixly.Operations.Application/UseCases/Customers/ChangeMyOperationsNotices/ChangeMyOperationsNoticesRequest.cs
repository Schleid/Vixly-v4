using Vixly.Operations.Domain.Texts;

namespace Vixly.Operations.Application.UseCases.Customers.ChangeMyOperationsNotices;

// =========================================================
// FILE: ChangeMyOperationsNoticesRequest.cs
//
// PURPOSE:
// The signed-in customer chooses how the company’s notices reach them (Slice 9): email, SMS or WhatsApp, with their consent to texts.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ChangeMyOperationsNoticesRequest
{
    public string? SessionToken { get; init; }

    public OperationsNoticeChannel Channel { get; init; }

    // The customer ticked that they agree to receive the company’s notices by text at their phone number.
    public bool Consent { get; init; }
}
