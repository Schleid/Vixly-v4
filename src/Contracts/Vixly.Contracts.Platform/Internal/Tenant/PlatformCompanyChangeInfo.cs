namespace Vixly.Contracts.Platform.Internal.Tenant;

// =========================================================
// FILE: PlatformCompanyChangeInfo.cs
//
// PURPOSE:
// A tenant’s latest company change request and its decision (B12).
//
// RESPONSIBILITIES:
// - Carry the request, both sets of details and the decision
//
// IMPORTANT NOTES:
// - Status is a wire code: pending, approved, rejected, withdrawn
// =========================================================
public sealed record PlatformCompanyChangeInfo(
    Guid RequestId,
    string Status,
    PlatformCompanyDetailsInfo Previous,
    PlatformCompanyDetailsInfo Proposed,
    string Reason,
    string SubmittedByName,
    DateTimeOffset SubmittedUtc,
    DateTimeOffset? DecidedUtc,
    string? DecisionNote);
