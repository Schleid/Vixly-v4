namespace Vixly.Platform.Web.Features.Tenants;

// =========================================================
// FILE: TenantMove.cs
//
// PURPOSE:
// The lifecycle moves the console can offer (D-T1).
//
// RESPONSIBILITIES:
// - Name each move
//
// IMPORTANT NOTES:
// - Mirrors the domain moves; the domain still decides
// =========================================================
public enum TenantMove
{
    Activate,
    Suspend,
    Reactivate,
    Deactivate,
    Ban
}
