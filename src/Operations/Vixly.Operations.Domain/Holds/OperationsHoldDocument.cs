namespace Vixly.Operations.Domain.Holds;

// =========================================================
// FILE: OperationsHoldDocument.cs
//
// PURPOSE:
// A file a customer sent with an answer, once stored (Slice 3E).
//
// RESPONSIBILITIES:
// - Carry where it is stored and what it is
//
// IMPORTANT NOTES:
// - Checked by content (PDF, PNG or JPEG), never by its name
// =========================================================
public sealed record OperationsHoldDocument(string StorageKey, string ContentType, string? FileName, int Bytes, string Sha256);
