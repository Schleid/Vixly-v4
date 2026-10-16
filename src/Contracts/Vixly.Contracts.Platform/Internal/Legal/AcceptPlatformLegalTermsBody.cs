namespace Vixly.Contracts.Platform.Internal.Legal;

// =========================================================
// FILE: AcceptPlatformLegalTermsBody.cs
//
// PURPOSE:
// The owner accepts the Terms shown (call 13).
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Platform.Api and Operations or Edge
//
// IMPORTANT NOTES:
// - The browser IP and user agent travel in the service token
// - Wire type: BCL only (C-0); never a domain type
// - Version 1: add fields only; consumers ignore unknown fields (D-K4)
// =========================================================
public sealed record AcceptPlatformLegalTermsBody(Guid DocumentId, string Sha256, string Language);
