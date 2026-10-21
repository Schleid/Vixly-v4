namespace Vixly.Contracts.Operations.Packages;

// =========================================================
// FILE: CustomerTrackingLineDto.cs
//
// PURPOSE:
// One step of a package's history as its customer may see it (Slice 3B, ADR-0021 decision 5).
//
// RESPONSIBILITIES:
// - Carry the values between Customer.Web and Operations.Api
//
// IMPORTANT NOTES:
// - Wire type: add fields at the end, never rename (ADR-0014)
// - Only customer-safe arguments; no actor, note or location
// =========================================================
public sealed record CustomerTrackingLineDto(string Code, DateTimeOffset OccurredUtc, IReadOnlyDictionary<string, string> Arguments);
