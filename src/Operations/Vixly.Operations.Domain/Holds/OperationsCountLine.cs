namespace Vixly.Operations.Domain.Holds;

// =========================================================
// FILE: OperationsCountLine.cs
//
// PURPOSE:
// One parcel a count expects in the undeclared parcels area, and what the clerk found (Slice 4D step 3).
//
// RESPONSIBILITIES:
// - The parcel, its number and shelf when the count started, the result and the note
//
// IMPORTANT NOTES:
// - Kept inside its count; changed only through it
// =========================================================
public sealed class OperationsCountLine
{
    private OperationsCountLine()
    {
        ParcelNumber = string.Empty;
    }

    internal OperationsCountLine(Guid parcelId, string parcelNumber, string? shelfCode)
    {
        (ParcelId, ParcelNumber, ShelfCode) = (parcelId, parcelNumber, shelfCode);
    }

    public Guid ParcelId { get; private set; }

    public string ParcelNumber { get; private set; }

    public string? ShelfCode { get; private set; }

    public OperationsCountResult Result { get; private set; }

    public string? Note { get; private set; }

    internal void Mark(OperationsCountResult result, string? note) => (Result, Note) = (result, note);
}
