namespace Vixly.Operations.Application.Arrivals;

// =========================================================
// FILE: OperationsArrivalBundleRow.cs
//
// PURPOSE:
// A bundle of a dispatch received at a destination (Slice 7).
//
// RESPONSIBILITIES:
// - Carry what the pages show
//
// IMPORTANT NOTES:
// - Read model only
// =========================================================
public sealed record OperationsArrivalBundleRow(Guid BundleId, string Number, string Destination, bool EndsHere, DateTimeOffset? ArrivedUtc, DateTimeOffset? OpenedUtc, bool Here,
    int ParcelCount, int ParcelsHere);
