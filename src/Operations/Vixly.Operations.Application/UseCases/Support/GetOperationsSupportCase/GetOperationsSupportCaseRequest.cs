namespace Vixly.Operations.Application.UseCases.Support.GetOperationsSupportCase;

// =========================================================
// FILE: GetOperationsSupportCaseRequest.cs
//
// PURPOSE:
// Step 2 of a support call: one shipment or package by its tracking number, only when it is the caller's (owner, 2026-10-29, design 9.1, 9.2).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsSupportCaseRequest
{
    public Guid? CallerId { get; init; }

    public string? Number { get; init; }
}
