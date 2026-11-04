namespace Vixly.Platform.Domain.Sales;

// =========================================================
// FILE: PlatformSalesLeadStatus.cs
//
// PURPOSE:
// Where a contact request from the public site stands (Stage 2, D-U3).
//
// RESPONSIBILITIES:
// - Name the lead states
//
// IMPORTANT NOTES:
// - Persisted as numbers; never renumber
// =========================================================
public enum PlatformSalesLeadStatus
{
    New = 1,
    Contacted = 2,
    Closed = 3
}
