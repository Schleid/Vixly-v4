namespace Vixly.Operations.Application.UseCases.Counter.FindOperationsCounterCustomers;

// =========================================================
// FILE: FindOperationsCounterCustomersRequest.cs
//
// PURPOSE:
// Finds a customer at the front-desk counter by phone, email or customer code (Slice 5C, C-5).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class FindOperationsCounterCustomersRequest
{
    public string? Query { get; init; }
}
