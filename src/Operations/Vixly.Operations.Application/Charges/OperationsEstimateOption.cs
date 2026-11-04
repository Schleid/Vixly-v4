namespace Vixly.Operations.Application.Charges;

// =========================================================
// FILE: OperationsEstimateOption.cs
//
// PURPOSE:
// One "what if" choice in the shipment estimate (Slice 5D) and the total it would make.
//
// RESPONSIBILITIES:
// - Carry the kind ("none", "product", "consolidate", "separate"), the insurance product, the change and the total
// - Say whether it is the shipment's current choice
//
// IMPORTANT NOTES:
// - Change and Total are null when the choice is not possible (e.g. the value is above the product's maximum)
// =========================================================
public sealed record OperationsEstimateOption(string Kind, Guid? PlanId, string? Name, decimal? Change, decimal? Total, bool Current);
