using Vixly.Operations.Domain.Branding;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsBrandRepository.cs
//
// PURPOSE:
// The brand of the current tenant.
//
// RESPONSIBILITIES:
// - Find and add
// - Read only the name, or only the published settings, when that is all a page or document shows (harmonization H-1)
// - List the companies that have a brand (system scope, for the nightly send to the Cloudflare Worker)
//
// IMPORTANT NOTES:
// - One per tenant
// =========================================================
public interface IOperationsBrandRepository
{
    Task<OperationsTenantBrand?> FindAsync(CancellationToken cancellationToken);

    // The company's display name (the published one), without the website texts; null without a brand.
    Task<string?> FindDisplayNameAsync(CancellationToken cancellationToken);

    // The published settings (name, colours, logos, theme), without the website texts; null without a brand.
    Task<OperationsBrandSettings?> FindPublishedAsync(CancellationToken cancellationToken);

    void Add(OperationsTenantBrand brand);

    Task<IReadOnlyList<Guid>> ListTenantIdsAsync(CancellationToken cancellationToken);
}
