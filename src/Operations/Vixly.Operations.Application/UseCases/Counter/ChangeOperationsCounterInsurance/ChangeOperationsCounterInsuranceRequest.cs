namespace Vixly.Operations.Application.UseCases.Counter.ChangeOperationsCounterInsurance;

// =========================================================
// FILE: ChangeOperationsCounterInsuranceRequest.cs
//
// PURPOSE:
// At the counter, the customer takes another insurance product, or none, for an open shipment (Slice 5D, D-6).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ChangeOperationsCounterInsuranceRequest
{
    public Guid ShipmentId { get; init; }

    // None: no insurance.
    public Guid? InsurancePlanId { get; init; }

    // The customer accepted the chosen product's terms; the clerk records it.
    public bool Accepted { get; init; }
}
