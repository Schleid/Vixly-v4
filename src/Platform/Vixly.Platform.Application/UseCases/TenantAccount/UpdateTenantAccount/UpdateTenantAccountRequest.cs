using Vixly.Platform.Application.UseCases.Tenants;

namespace Vixly.Platform.Application.UseCases.TenantAccount.UpdateTenantAccount;

// =========================================================
// FILE: UpdateTenantAccountRequest.cs
//
// PURPOSE:
// The tenant changes its billing and technical contacts, language and time zone (call 16).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class UpdateTenantAccountRequest
{
    public IReadOnlyList<PlatformContactInput>? Contacts { get; init; }

    public string? Language { get; init; }

    public string? TimeZone { get; init; }
}
