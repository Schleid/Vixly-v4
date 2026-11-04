namespace Vixly.Contracts.Operations.Packages;

// =========================================================
// FILE: CustomerParcelPhotoDto.cs
//
// PURPOSE:
// A photo of the customer's package, fetched by id (Slice 3B).
//
// RESPONSIBILITIES:
// - Carry the values between Customer.Web and Operations.Api
//
// IMPORTANT NOTES:
// - Wire type: add fields at the end, never rename (ADR-0014)
// =========================================================
public sealed record CustomerParcelPhotoDto(Guid PhotoId, string Kind, DateTimeOffset TakenUtc);
