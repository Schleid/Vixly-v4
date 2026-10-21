namespace Vixly.Operations.Application.Shipments;

// =========================================================
// FILE: OperationsPackageDraft.cs
//
// PURPOSE:
// What the customer typed for one announced package (Slice 3B; owner, 2026-10-18).
//
// RESPONSIBILITIES:
// - Carry the carrier, tracking number, store, contents, value and expected date as typed
//
// IMPORTANT NOTES:
// - Checked by the domain
// =========================================================
public sealed record OperationsPackageDraft(string? Carrier, string? TrackingNumber, string? Store, string? Contents, decimal DeclaredValue, DateOnly? ExpectedDate);
