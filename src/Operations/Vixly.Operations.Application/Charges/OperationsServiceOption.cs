namespace Vixly.Operations.Application.Charges;

// =========================================================
// FILE: OperationsServiceOption.cs
//
// PURPOSE:
// A service the company offers, with its own label when it set one (Slice 4).
//
// RESPONSIBILITIES:
// - Carry the code and label to a page
//
// IMPORTANT NOTES:
// - Label null: the page shows the catalog name
// =========================================================
public sealed record OperationsServiceOption(string Code, string? Label);
