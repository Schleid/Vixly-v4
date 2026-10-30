namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: OperationsLegalTerms.cs
//
// PURPOSE:
// The Terms version the Owner reads and accepts.
//
// RESPONSIBILITIES:
// - Carry the version, language, file hash, summary and included documents
//
// IMPORTANT NOTES:
// - The hash proves which file was shown (Platform checks it)
// =========================================================
public sealed record OperationsLegalTerms(Guid DocumentId, string Version, string Language, string Sha256, string? Summary, IReadOnlyList<OperationsLegalPart> Parts);
