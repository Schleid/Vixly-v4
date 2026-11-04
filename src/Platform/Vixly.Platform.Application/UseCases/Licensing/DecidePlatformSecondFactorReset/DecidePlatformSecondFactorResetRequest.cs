namespace Vixly.Platform.Application.UseCases.Licensing.DecidePlatformSecondFactorReset;

// =========================================================
// FILE: DecidePlatformSecondFactorResetRequest.cs
//
// PURPOSE:
// A second Vixly staff member approves or refuses a reset of the Owner's second factor (D-O13).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class DecidePlatformSecondFactorResetRequest
{
    public Guid RequestId { get; init; }

    public bool Approve { get; init; }

    public string? Note { get; init; }
}
