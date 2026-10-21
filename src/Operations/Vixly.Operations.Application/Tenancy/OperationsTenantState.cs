namespace Vixly.Operations.Application.Tenancy;

// =========================================================
// FILE: OperationsTenantState.cs
//
// PURPOSE:
// What a host name means for Operations (Stage 3, Slice 0, D-F2).
//
// RESPONSIBILITIES:
// - Name the four outcomes of host resolution
//
// IMPORTANT NOTES:
// - SettingUp: the tenant exists but its owner has not finished setup (only setup pages)
// - Suspended: known but closed for work ("not available")
// =========================================================
public enum OperationsTenantState
{
    NotFound = 0,
    Live = 1,
    SettingUp = 2,
    Suspended = 3
}
