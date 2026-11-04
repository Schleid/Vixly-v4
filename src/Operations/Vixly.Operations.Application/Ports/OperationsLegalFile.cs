namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: OperationsLegalFile.cs
//
// PURPOSE:
// A published legal PDF read from Platform.
//
// RESPONSIBILITIES:
// - Carry the bytes and the type
//
// IMPORTANT NOTES:
// - Published versions only (Platform serves no drafts)
// =========================================================
public sealed record OperationsLegalFile(byte[] Content, string ContentType);
