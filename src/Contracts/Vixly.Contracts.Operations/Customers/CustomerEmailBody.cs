namespace Vixly.Contracts.Operations.Customers;

// =========================================================
// FILE: CustomerEmailBody.cs
//
// PURPOSE:
// An email address and the portal it was typed on, for links (Slice 2).
//
// RESPONSIBILITIES:
// - Carry the values between Customer.Web and Operations.Api
//
// IMPORTANT NOTES:
// - Wire type: add fields at the end, never rename (ADR-0014)
// =========================================================
public sealed record CustomerEmailBody(string Email, string PortalSlug);
