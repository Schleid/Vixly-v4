using Vixly.Operations.Domain.Delivery;

namespace Vixly.Operations.Application.Delivery;

// =========================================================
// FILE: OperationsRunSheet.cs
//
// PURPOSE:
// What a route's printed run sheet shows (Slice 7 step 3, R-7, R-12).
//
// RESPONSIBILITIES:
// - Carry the company, the route, its driver and its stops in order, with what to collect and the proof asked
//
// IMPORTANT NOTES:
// - The driver writes each stop's result on it; staff enter them when the driver is back
// =========================================================
public sealed record OperationsRunSheet(string CompanyName, string Number, string WarehouseName, DateOnly ServiceDate, string DriverName, IReadOnlyList<OperationsDeliveryStopRow> Stops,
    OperationsDeliveryProofs Proofs, string CurrencyCode);
