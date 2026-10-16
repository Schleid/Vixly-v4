using Vixly.Platform.Domain.Legal;

namespace Vixly.Platform.Application.UseCases.Legal;

// =========================================================
// FILE: PlatformLegalStatusAnswer.cs
//
// PURPOSE:
// A tenant's legal status for Operations (Slice 9 contract, D-Lg11).
//
// RESPONSIBILITIES:
// - Carry the status, deadline, whether access is read-only for it, and the Terms to show
//
// IMPORTANT NOTES:
// - Terms is null when nothing needs acceptance
// =========================================================
public sealed record PlatformLegalStatusAnswer(PlatformLegalStatus Status, DateTimeOffset? DeadlineUtc, bool ReadOnly, PlatformLegalTerms? Terms);
