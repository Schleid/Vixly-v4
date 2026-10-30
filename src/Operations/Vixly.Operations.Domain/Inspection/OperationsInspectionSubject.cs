namespace Vixly.Operations.Domain.Inspection;

// =========================================================
// FILE: OperationsInspectionSubject.cs
//
// PURPOSE:
// What the rules look at in a parcel: its declaration, value, carrier and destination (Slice 3C).
//
// RESPONSIBILITIES:
// - Carry the facts the rules and the restricted list compare
//
// IMPORTANT NOTES:
// - CountryCode is the shipment's destination; null while the customer has not said where it goes
// =========================================================
public sealed record OperationsInspectionSubject(string? DeclaredContents, decimal? DeclaredValue, string? Carrier, string? CountryCode)
{
    public IReadOnlyList<string> Declaration { get; } = OperationsDeclarationText.Words(DeclaredContents);
}
