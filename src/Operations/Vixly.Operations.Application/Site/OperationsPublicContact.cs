namespace Vixly.Operations.Application.Site;

// =========================================================
// FILE: OperationsPublicContact.cs
//
// PURPOSE:
// How to reach the tenant in one country.
//
// RESPONSIBILITIES:
// - Carry the contact details
//
// IMPORTANT NOTES:
// - In the tenant order
// =========================================================
public sealed record OperationsPublicContact(string CountryCode, string? Label, IReadOnlyList<string> Phones, string? WhatsApp, string? Email, string? Hours);
