using Vixly.Platform.Domain.Billing;

namespace Vixly.Platform.Application.UseCases.Billing.GetPlatformBillingDocument;

// =========================================================
// FILE: GetPlatformBillingDocumentRequest.cs
//
// PURPOSE:
// Returns the stored PDF of an invoice, credit note or receipt (D-B10).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetPlatformBillingDocumentRequest
{
    public PlatformBillingDocumentKind Kind { get; init; }

    public Guid OwnerId { get; init; }
}
