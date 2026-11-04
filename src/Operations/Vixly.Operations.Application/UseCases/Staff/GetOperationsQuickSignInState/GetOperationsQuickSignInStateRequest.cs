namespace Vixly.Operations.Application.UseCases.Staff.GetOperationsQuickSignInState;

// =========================================================
// FILE: GetOperationsQuickSignInStateRequest.cs
//
// PURPOSE:
// Whether a person has a badge in use and a PIN (Slice 12A): for the staff page, or the person's own account.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsQuickSignInStateRequest
{
    // Empty: the signed-in person.
    public Guid? UserId { get; init; }
}
