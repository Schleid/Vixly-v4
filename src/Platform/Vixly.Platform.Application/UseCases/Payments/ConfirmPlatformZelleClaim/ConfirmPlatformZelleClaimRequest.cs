namespace Vixly.Platform.Application.UseCases.Payments.ConfirmPlatformZelleClaim;

// =========================================================
// FILE: ConfirmPlatformZelleClaimRequest.cs
//
// PURPOSE:
// Staff found the Zelle money: records a Zelle payment on the invoice and closes the report.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ConfirmPlatformZelleClaimRequest
{
    public Guid ClaimId { get; init; }

    public decimal? ReceivedAmount { get; init; }

    public DateTimeOffset? ReceivedUtc { get; init; }

    public string? Note { get; init; }
}
