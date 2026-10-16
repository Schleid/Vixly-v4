namespace Vixly.Contracts.Platform.Legal;

// =========================================================
// FILE: PlatformLegalPartInfo.cs
//
// PURPOSE:
// One document the Terms include (Privacy, DPA, AUP, SLA).
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Public.Web and Platform.Api
//
// IMPORTANT NOTES:
// - Kind: "privacy", "data_processing", "acceptable_use" or "service_level"
// - Wire type: BCL only (C-0); never a domain type
// =========================================================
public sealed record PlatformLegalPartInfo(Guid DocumentId, string Kind, string Version, string Sha256, string? Summary, string FileUrl);
