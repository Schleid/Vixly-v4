using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Application.Shipments;

// =========================================================
// FILE: OperationsShipmentParcel.cs
//
// PURPOSE:
// One parcel on the staff shipment page, with whether it is cleared and its chargeable weight (Slice 5A).
//
// RESPONSIBILITIES:
// - Carry the parcel and what the page works out for it
//
// IMPORTANT NOTES:
// - Chargeable weight: the greater of actual and volumetric, in kilograms (owner, A-9)
// =========================================================
public sealed record OperationsShipmentParcel(OperationsParcel Parcel, bool Cleared, decimal? ChargeableKg);
