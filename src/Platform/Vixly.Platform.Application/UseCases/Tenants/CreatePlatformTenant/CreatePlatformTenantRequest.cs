namespace Vixly.Platform.Application.UseCases.Tenants.CreatePlatformTenant;

// =========================================================
// FILE: CreatePlatformTenantRequest.cs
//
// PURPOSE:
// Input for CreatePlatformTenant.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Company and owner are validated by the domain
// =========================================================
public sealed class CreatePlatformTenantRequest
{
    public PlatformCompanyInput Company { get; init; } = new();

    public string Slug { get; init; } = string.Empty;

    public string OwnerName { get; init; } = string.Empty;

    public string OwnerEmail { get; init; } = string.Empty;

    public string? OwnerPhone { get; init; }

    public string Language { get; init; } = "en";

    public string? TimeZone { get; init; }
}
