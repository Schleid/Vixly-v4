namespace Vixly.Platform.Application.UseCases.MyAccount.RemoveMyPasskey;

// =========================================================
// FILE: RemoveMyPasskeyRequest.cs
//
// PURPOSE:
// Input for RemoveMyPasskey.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Only a passkey of the signed-in user can be named here
// =========================================================
public sealed class RemoveMyPasskeyRequest
{
    public Guid PasskeyId { get; init; }
}
