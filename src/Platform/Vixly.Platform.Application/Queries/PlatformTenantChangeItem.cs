using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformTenantChangeItem.cs
//
// PURPOSE:
// One company change request as the console and the tenant read it (B12).
//
// RESPONSIBILITIES:
// - Carry the request, both sets of details and the proof’s name and size
//
// IMPORTANT NOTES:
// - Never carries the proof’s bytes
// =========================================================
public sealed record PlatformTenantChangeItem(
    Guid RequestId,
    Guid PlatformTenantId,
    string TenantCode,
    string TenantName,
    PlatformTenantChangeStatus Status,
    PlatformTenantCompanyItem Previous,
    PlatformTenantCompanyItem Proposed,
    string Reason,
    string SubmittedByName,
    DateTimeOffset SubmittedUtc,
    DateTimeOffset? DecidedUtc,
    string? DecidedByName,
    string? DecisionNote,
    string ProofFileName,
    string ProofContentType,
    int ProofSizeBytes);
