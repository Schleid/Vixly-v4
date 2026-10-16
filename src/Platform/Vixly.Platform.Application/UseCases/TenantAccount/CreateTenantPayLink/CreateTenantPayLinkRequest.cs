namespace Vixly.Platform.Application.UseCases.TenantAccount.CreateTenantPayLink;

// =========================================================
// FILE: CreateTenantPayLinkRequest.cs
//
// PURPOSE:
// A payment link for one of the tenant's open invoices (call 23).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class CreateTenantPayLinkRequest
{
    public Guid PlatformInvoiceId { get; init; }
}
