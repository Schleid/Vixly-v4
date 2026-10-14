namespace Vixly.Platform.Domain.Tenants;

// =========================================================
// FILE: PlatformSuspensionCategory.cs
//
// PURPOSE:
// Why a tenant was suspended (Slice 2, D-T2).
//
// RESPONSIBILITIES:
// - Name the values the model accepts
//
// IMPORTANT NOTES:
// - Persisted as numbers; never renumber
// - A fixed list; names come from resources in en, fr and es
// - Shown to the tenant in notices; the reason never is
// =========================================================
public enum PlatformSuspensionCategory
{
    Compliance = 0,
    Security = 1,
    FraudInvestigation = 2,
    Administrative = 3
}
