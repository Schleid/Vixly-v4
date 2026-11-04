namespace Vixly.Operations.Domain.Locations;

// =========================================================
// FILE: OperationsBranchLegal.cs
//
// PURPOSE:
// A branch's legal identity in its country (owner, 2026-10-29, B-4: "each country has its own rules and regulations").
//
// RESPONSIBILITIES:
// - Carry the local company name, the tax number and the lines the country requires on documents
//
// IMPORTANT NOTES:
// - All optional; empty, the company's own identity prints, as before
// =========================================================
public sealed record OperationsBranchLegal(string? LegalName, string? TaxNumber, string? Notes)
{
    public static OperationsBranchLegal None { get; } = new(null, null, null);
}
