namespace Vixly.Operations.Application.UseCases.Cashier.ChangeOperationsCardAcceptance;

// =========================================================
// FILE: ChangeOperationsCardAcceptanceRequest.cs
//
// PURPOSE:
// Turns cards on or off for the company (Slice 5F step 4).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ChangeOperationsCardAcceptanceRequest
{
    public bool Accept { get; init; }
}
