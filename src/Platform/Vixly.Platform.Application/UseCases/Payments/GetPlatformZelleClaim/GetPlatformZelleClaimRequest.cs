namespace Vixly.Platform.Application.UseCases.Payments.GetPlatformZelleClaim;

// =========================================================
// FILE: GetPlatformZelleClaimRequest.cs
//
// PURPOSE:
// Reads one Zelle report with the invoice balance and what the reader may do.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetPlatformZelleClaimRequest
{
    public Guid ClaimId { get; init; }
}
