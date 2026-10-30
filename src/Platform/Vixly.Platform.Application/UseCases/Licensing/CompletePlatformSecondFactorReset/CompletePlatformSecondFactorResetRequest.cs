namespace Vixly.Platform.Application.UseCases.Licensing.CompletePlatformSecondFactorReset;

// =========================================================
// FILE: CompletePlatformSecondFactorResetRequest.cs
//
// PURPOSE:
// Uses an emailed second-factor reset link on the tenant host (D-O13).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class CompletePlatformSecondFactorResetRequest
{
    public string? Token { get; init; }

    public Guid? ExpectedTenantId { get; init; }
}
