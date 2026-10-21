namespace Vixly.Operations.Application.UseCases.Customers.SetOperationsCustomerCredit;

// =========================================================
// FILE: SetOperationsCustomerCreditRequest.cs
//
// PURPOSE:
// Staff allow or remove credit for a customer, with a reason (Slice 2; used by the payment gate from Slice 9).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SetOperationsCustomerCreditRequest
{
    public Guid CustomerId { get; init; }

    public bool Allowed { get; init; }

    public string Reason { get; init; } = string.Empty;
}
