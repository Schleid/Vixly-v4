namespace Vixly.Operations.Application.Api;

// =========================================================
// FILE: OperationsApiEventRow.cs
//
// PURPOSE:
// One event read for a webhook address (Slice 10).
//
// RESPONSIBILITIES:
// - Carry the event id, the record it is about and its database version
//
// IMPORTANT NOTES:
// - The version is the place a webhook address remembers
// =========================================================
public sealed record OperationsApiEventRow(Guid Id, Guid SourceId, long Version, DateTimeOffset CreatedUtc);
