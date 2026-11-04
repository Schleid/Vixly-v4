namespace Vixly.Contracts.Operations.Packages;

// =========================================================
// FILE: CustomerPreAlertCreatedDto.cs
//
// PURPOSE:
// The pre-alert just written, and the package it matched if it had already arrived (Slice 3B).
//
// RESPONSIBILITIES:
// - Carry the values between Customer.Web and Operations.Api
//
// IMPORTANT NOTES:
// - Wire type: add fields at the end, never rename (ADR-0014)
// =========================================================
public sealed record CustomerPreAlertCreatedDto(Guid PreAlertId, Guid? ParcelId);
