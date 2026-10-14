namespace Vixly.Platform.Application.UseCases.SignIn.SignOut;

// =========================================================
// FILE: SignOutRequest.cs
//
// PURPOSE:
// Input for SignOut. Empty: the session comes from the context.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - EverywhereElseToo also ends the user other sessions
// =========================================================
public sealed class SignOutRequest
{
    public bool EverywhereElseToo { get; init; }
}
