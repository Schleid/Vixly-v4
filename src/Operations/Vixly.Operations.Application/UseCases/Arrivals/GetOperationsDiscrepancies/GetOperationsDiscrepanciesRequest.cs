namespace Vixly.Operations.Application.UseCases.Arrivals.GetOperationsDiscrepancies;

// =========================================================
// FILE: GetOperationsDiscrepanciesRequest.cs
//
// PURPOSE:
// What did not match at a destination, for the destination and the origin (Slice 7, R-2).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsDiscrepanciesRequest
{
    public bool OpenOnly { get; init; } = true;
}
