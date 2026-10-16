using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformZelleQueries.cs
//
// PURPOSE:
// Read models for Zelle details and reports.
//
// RESPONSIBILITIES:
// - Read the details without the image, the image on its own, and the paged report list
//
// IMPORTANT NOTES:
// - No tracking
// =========================================================
public interface IPlatformZelleQueries
{
    Task<PlatformZelleAccountItem?> GetAccountAsync(CancellationToken cancellationToken);

    Task<PlatformZelleImage?> GetQrCodeAsync(CancellationToken cancellationToken);

    Task<PagedResult<PlatformZelleClaimItem>> ListClaimsAsync(PlatformZelleClaimFilter filter, PagedRequest page, CancellationToken cancellationToken);

    Task<PlatformZelleClaimItem?> GetClaimAsync(Guid claimId, CancellationToken cancellationToken);
}
