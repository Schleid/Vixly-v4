using Vixly.Platform.Application.UseCases.Tenants;

namespace Vixly.Platform.Application.UseCases.TenantAccount.UpdateTenantCompanyProfile;

// =========================================================
// FILE: UpdateTenantCompanyProfileRequest.cs
//
// PURPOSE:
// The company's own profile as its owner saves it: trading name, email, phone and address (owner, 2026-10-25).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - The legal name is not taken from here: it changes only through a request with proof
// =========================================================
public sealed class UpdateTenantCompanyProfileRequest
{
    public PlatformCompanyInput Company { get; init; } = new();
}
