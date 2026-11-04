using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Insurance;
using Vixly.Operations.Domain.Settings;

namespace Vixly.Operations.Application.Insurance;

// =========================================================
// FILE: OperationsLiabilityView.cs
//
// PURPOSE:
// The company's liability for one transport mode when the customer did not insure, as staff and customers see it (Slice 5B, I-3).
//
// RESPONSIBILITIES:
// - Carry the basis, amount and unit, cap and words, and where they come from
//
// IMPORTANT NOTES:
// - FromCover: the company has not set liability terms yet; the figures of its insurance cover (Slice 4C) are used, so nothing is lost;
//   the cover's limit was per shipment (CapPerShipment), never per parcel (review)
// =========================================================
public sealed record OperationsLiabilityView(
    OperationsTransportMode Mode,
    OperationsLiabilityBasis Basis,
    decimal? Amount,
    OperationsWeightUnit WeightUnit,
    decimal? Cap,
    string? CustomerText,
    bool FromCover,
    bool CapPerShipment = false);
