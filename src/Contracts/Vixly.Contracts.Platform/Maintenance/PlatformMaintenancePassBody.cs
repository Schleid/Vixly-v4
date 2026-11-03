namespace Vixly.Contracts.Platform.Maintenance;

// =========================================================
// FILE: PlatformMaintenancePassBody.cs
//
// PURPOSE:
// A pass link a site asks Platform to check during a window.
//
// RESPONSIBILITIES:
// - Carry data over HTTP between a site and Platform.Api
//
// IMPORTANT NOTES:
// - Site: public, companies, customers or staff
// - Wire type: BCL only (C-0); never a domain type
// =========================================================
public sealed record PlatformMaintenancePassBody
{
    public string Token { get; init; } = string.Empty;

    public string Site { get; init; } = string.Empty;
}
