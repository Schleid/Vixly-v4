namespace Vixly.Operations.Application.UseCases.Insurance.SaveOperationsInsuranceSettings;

// =========================================================
// FILE: SaveOperationsInsuranceSettingsRequest.cs
//
// PURPOSE:
// The company's insurance settings: whether staff may change the insurance of an open shipment (Slice 5D, D-6).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SaveOperationsInsuranceSettingsRequest
{
    public bool StaffMayChangeInsurance { get; init; } = true;
}
