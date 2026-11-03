namespace Vixly.Operations.Application.UseCases.SignIn.BeginOperationsQuickSignIn;

// =========================================================
// FILE: BeginOperationsQuickSignInRequest.cs
//
// PURPOSE:
// Signs a staff member in with their badge or PIN on a licensed device of the company (Slice 12A, owner H-8).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class BeginOperationsQuickSignInRequest
{
    // The scanned badge code, or the person and their PIN.
    public string? Badge { get; init; }

    public Guid? UserId { get; init; }

    public string? Pin { get; init; }

    // From the browser's device cookie; checked with Platform.
    public Guid? DeviceId { get; init; }

    public string? DeviceSecret { get; init; }

    public string? PortalSlug { get; init; }
}
