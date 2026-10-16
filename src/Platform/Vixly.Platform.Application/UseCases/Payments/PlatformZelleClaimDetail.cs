using Vixly.Platform.Application.Queries;

namespace Vixly.Platform.Application.UseCases.Payments;

// =========================================================
// FILE: PlatformZelleClaimDetail.cs
//
// PURPOSE:
// One Zelle report with what the reader may do.
//
// RESPONSIBILITIES:
// - Carry the report page in one read
//
// IMPORTANT NOTES:
// - The invoice balance is read now, not when reported
// =========================================================
public sealed record PlatformZelleClaimDetail(PlatformZelleClaimItem Claim, PlatformZelleClaimAbilities Abilities);
