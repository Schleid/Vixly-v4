namespace Vixly.Platform.Domain.Sales;

// =========================================================
// FILE: PlatformSalesLeadKind.cs
//
// PURPOSE:
// What kind of logistics operation a lead runs, as chosen on the contact form.
//
// RESPONSIBILITIES:
// - Name the choices of the contact form
//
// IMPORTANT NOTES:
// - Persisted as numbers; never renumber
// =========================================================
public enum PlatformSalesLeadKind
{
    Courier = 1,
    Freight = 2,
    Warehouse = 3,
    MultiBranch = 4,
    Other = 5
}
