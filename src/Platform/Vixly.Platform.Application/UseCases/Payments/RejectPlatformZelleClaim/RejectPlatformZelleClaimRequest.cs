namespace Vixly.Platform.Application.UseCases.Payments.RejectPlatformZelleClaim;

// =========================================================
// FILE: RejectPlatformZelleClaimRequest.cs
//
// PURPOSE:
// Staff could not find the Zelle money: closes the report and tells the customer why.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class RejectPlatformZelleClaimRequest
{
    public Guid ClaimId { get; init; }

    public string? Reason { get; init; }

    public string? Note { get; init; }
}
