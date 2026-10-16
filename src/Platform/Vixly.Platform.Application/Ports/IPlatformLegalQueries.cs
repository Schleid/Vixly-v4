using Vixly.Platform.Application.Queries;
using Vixly.Platform.Domain.Legal;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformLegalQueries.cs
//
// PURPOSE:
// Read models for the legal screens, the public pages and the reminder clock.
//
// RESPONSIBILITIES:
// - List and read documents and files
// - Page the acceptance register
// - List a tenant's acceptances and reminders
// - List tenants the clock must look at
//
// IMPORTANT NOTES:
// - No tracking; only Active and Suspended tenants are ever asked
// =========================================================
public interface IPlatformLegalQueries
{
    Task<PagedResult<PlatformLegalDocumentItem>> ListDocumentsAsync(PlatformLegalDocumentKind? kind, PagedRequest page, CancellationToken cancellationToken);

    Task<PlatformLegalDocumentItem?> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken);

    Task<IReadOnlyList<PlatformLegalDocumentItem>> ListPublishedAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<PlatformLegalFileItem>> ListFilesAsync(Guid documentId, CancellationToken cancellationToken);

    Task<PlatformLegalFileContent?> GetFileContentAsync(Guid documentId, string language, CancellationToken cancellationToken);

    Task<PagedResult<PlatformLegalRegisterItem>> RegisterAsync(PlatformLegalDocument required, PlatformLegalRegisterFilter filter, PagedRequest page, DateTimeOffset utcNow, CancellationToken cancellationToken);

    Task<IReadOnlyList<PlatformTermsAcceptanceItem>> ListAcceptancesAsync(Guid platformTenantId, CancellationToken cancellationToken);

    Task<IReadOnlyList<PlatformTermsReminderItem>> ListRemindersAsync(Guid platformTenantId, CancellationToken cancellationToken);

    // acceptedSinceUtc set: tenants that accepted nothing since then; null: tenants with no reminder for the document yet.
    Task<IReadOnlyList<PlatformLegalTenantRow>> ListTenantsToRemindAsync(Guid documentId, DateTimeOffset? acceptedSinceUtc, int take, CancellationToken cancellationToken);
}
