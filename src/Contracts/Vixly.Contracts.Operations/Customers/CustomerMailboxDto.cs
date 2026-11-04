namespace Vixly.Contracts.Operations.Customers;

// =========================================================
// FILE: CustomerMailboxDto.cs
//
// PURPOSE:
// A customer's mailbox address at one receiving warehouse (Slice 2).
//
// RESPONSIBILITIES:
// - Carry the values between Customer.Web and Operations.Api
//
// IMPORTANT NOTES:
// - Wire type: add fields at the end, never rename (ADR-0014)
// =========================================================
public sealed record CustomerMailboxDto(string WarehouseName, string Addressee, string Line1, string? Line2, string City, string? Region, string? PostalCode, string CountryCode);
