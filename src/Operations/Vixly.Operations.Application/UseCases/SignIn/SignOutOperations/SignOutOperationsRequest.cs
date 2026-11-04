namespace Vixly.Operations.Application.UseCases.SignIn.SignOutOperations;

// =========================================================
// FILE: SignOutOperationsRequest.cs
//
// PURPOSE:
// Ends the current staff session, and optionally the others (Slice 1).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SignOutOperationsRequest
{
    public bool EverywhereElseToo { get; init; }
}
