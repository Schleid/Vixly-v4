namespace Vixly.Operations.Application.UseCases.Customers.LookUpOperationsCustomers;

// =========================================================
// FILE: LookUpOperationsCustomersRequest.cs
//
// PURPOSE:
// What a person typed to find one customer (owner, 2026-10-29, design 4.4).
//
// RESPONSIBILITIES:
// - Carry the exact phone, email or customer code
//
// IMPORTANT NOTES:
// - Never a name or a partial number
// =========================================================
public sealed class LookUpOperationsCustomersRequest
{
    public string? Text { get; init; }
}
