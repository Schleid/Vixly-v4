using Vixly.Platform.Domain.Legal;

namespace Vixly.Platform.Application.UseCases.Legal;

// =========================================================
// FILE: PlatformTenantLegalState.cs
//
// PURPOSE:
// Where one tenant stands with the Terms.
//
// RESPONSIBILITIES:
// - Carry the status, the deadline and the version to accept
//
// IMPORTANT NOTES:
// - NotRequired when nothing needs acceptance or the tenant is not asked
// =========================================================
public sealed record PlatformTenantLegalState(PlatformLegalStatus Status, DateTimeOffset? DeadlineUtc, Guid? RequiredDocumentId, string? RequiredVersion)
{
    public static PlatformTenantLegalState NotRequired { get; } = new(PlatformLegalStatus.NotRequired, null, null, null);

    public bool Overdue => Status == PlatformLegalStatus.Overdue;
}
