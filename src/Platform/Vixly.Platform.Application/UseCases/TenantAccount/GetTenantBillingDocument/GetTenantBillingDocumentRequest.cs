using Vixly.Platform.Domain.Billing;

namespace Vixly.Platform.Application.UseCases.TenantAccount.GetTenantBillingDocument;

// =========================================================
// FILE: GetTenantBillingDocumentRequest.cs
//
// PURPOSE:
// One of the tenant's billing PDFs (call 22).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetTenantBillingDocumentRequest
{
    public PlatformBillingDocumentKind Kind { get; init; }

    public Guid OwnerId { get; init; }
}
