namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformLegalFileItem.cs
//
// PURPOSE:
// One language file of a legal document, without its bytes.
//
// RESPONSIBILITIES:
// - Carry language, hash, size and summary
//
// IMPORTANT NOTES:
// - Read model
// =========================================================
public sealed record PlatformLegalFileItem(string Language, string Sha256, int SizeBytes, string? Summary, DateTimeOffset UpdatedUtc);
