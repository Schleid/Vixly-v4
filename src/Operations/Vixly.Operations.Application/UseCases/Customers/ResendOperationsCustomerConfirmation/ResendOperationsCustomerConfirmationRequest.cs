namespace Vixly.Operations.Application.UseCases.Customers.ResendOperationsCustomerConfirmation;

// =========================================================
// FILE: ResendOperationsCustomerConfirmationRequest.cs
//
// PURPOSE:
// Input of ResendOperationsCustomerConfirmation.
//
// RESPONSIBILITIES:
// - Carry the customer, the address to send to (corrected or not) and the company's portal
//
// IMPORTANT NOTES:
// - Email empty or unchanged: the link goes to the address on file
// =========================================================
public sealed class ResendOperationsCustomerConfirmationRequest
{
    public Guid CustomerId { get; init; }

    public string? Email { get; init; }

    public string PortalSlug { get; init; } = string.Empty;
}
