namespace Vixly.Operations.Application.Dispatch;

// =========================================================
// FILE: OperationsManifestLeg.cs
//
// PURPOSE:
// One leg on a dispatch manifest (Slice 6B, D-6).
//
// RESPONSIBILITIES:
// - Carry its order, mode, carrier, places, reference and times
//
// IMPORTANT NOTES:
// - Times as moments; the layout shows them in the company's time zone
// =========================================================
public sealed record OperationsManifestLeg(int Sequence, string Mode, string? Carrier, string From, string To, string? Reference, DateTimeOffset? Departs, DateTimeOffset? Arrives);
