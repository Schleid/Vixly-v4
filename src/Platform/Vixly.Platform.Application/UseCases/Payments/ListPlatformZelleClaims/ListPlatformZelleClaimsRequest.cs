using Vixly.Platform.Domain.Payments;

namespace Vixly.Platform.Application.UseCases.Payments.ListPlatformZelleClaims;

// =========================================================
// FILE: ListPlatformZelleClaimsRequest.cs
//
// PURPOSE:
// Lists Zelle reports, paged; the waiting ones are staff's to-do list.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ListPlatformZelleClaimsRequest
{
    public PlatformZelleClaimStatus? Status { get; init; }

    public Guid? PlatformTenantId { get; init; }

    public string? Search { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 25;
}
