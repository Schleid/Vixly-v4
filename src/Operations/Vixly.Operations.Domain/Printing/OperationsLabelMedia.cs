namespace Vixly.Operations.Domain.Printing;

// =========================================================
// FILE: OperationsLabelMedia.cs
//
// PURPOSE:
// The labels a printer holds (Slice 4D step 2).
//
// RESPONSIBILITIES:
// - The 2 x 1 inch parcel ID labels of the dock, or the 4 x 6 inch shipping labels of inspection
//
// IMPORTANT NOTES:
// - Stored as a number; printers before Slice 4D hold ID labels
// =========================================================
public enum OperationsLabelMedia
{
    IdLabel = 1,
    ShippingLabel = 2
}
