using Vixly.Platform.Domain.Legal;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformLegalFileContent.cs
//
// PURPOSE:
// The bytes of one legal document file, for download.
//
// RESPONSIBILITIES:
// - Carry the PDF and what it belongs to
//
// IMPORTANT NOTES:
// - Public callers get published or superseded versions only
// =========================================================
public sealed record PlatformLegalFileContent(byte[] Content, string Sha256, string Language, PlatformLegalDocumentKind Kind, string Version, PlatformLegalDocumentStatus Status);
