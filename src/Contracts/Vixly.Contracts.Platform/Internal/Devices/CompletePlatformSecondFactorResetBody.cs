namespace Vixly.Contracts.Platform.Internal.Devices;

// =========================================================
// FILE: CompletePlatformSecondFactorResetBody.cs
//
// PURPOSE:
// Operations posts the token of an emailed second-factor reset link (D-O13).
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Platform.Api and Operations
//
// IMPORTANT NOTES:
// - Wire type: BCL only (C-0); never a domain type
// - Version 1: add fields only; consumers ignore unknown fields (D-K4)
// =========================================================
public sealed record CompletePlatformSecondFactorResetBody(string Token);
