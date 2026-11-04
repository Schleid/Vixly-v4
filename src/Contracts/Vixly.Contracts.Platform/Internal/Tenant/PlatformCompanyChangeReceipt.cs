namespace Vixly.Contracts.Platform.Internal.Tenant;

// =========================================================
// FILE: PlatformCompanyChangeReceipt.cs
//
// PURPOSE:
// Answer to a submitted company change request (B12).
//
// RESPONSIBILITIES:
// - Carry the new request’s id
//
// IMPORTANT NOTES:
// - 201 Created
// =========================================================
public sealed record PlatformCompanyChangeReceipt(Guid RequestId);
