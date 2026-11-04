namespace Vixly.Operations.Application.UseCases.Customers.ResendOperationsCustomerVerification;

// =========================================================
// FILE: ResendOperationsCustomerVerificationRequest.cs
//
// PURPOSE:
// Sends a new confirmation link; the same answer for unknown emails (Slice 2).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ResendOperationsCustomerVerificationRequest
{
    public string Email { get; init; } = string.Empty;

    public string PortalSlug { get; init; } = string.Empty;
}
