namespace Vixly.Operations.Application.Api;

// =========================================================
// FILE: OperationsApiRow.cs
//
// PURPOSE:
// One row read for the company API or a webhook (Slice 10).
//
// RESPONSIBILITIES:
// - Carry its place (time and id) and what is shown of it
//
// IMPORTANT NOTES:
// - The place is what a cursor remembers
// =========================================================
public sealed record OperationsApiRow(DateTimeOffset CreatedUtc, Guid Id, object View);
