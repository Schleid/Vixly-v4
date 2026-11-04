namespace Vixly.Contracts.Operations.Packages;

// =========================================================
// FILE: CustomerHoldAnswerBody.cs
//
// PURPOSE:
// The customer's answer to a hold on their package (Slice 3E).
//
// RESPONSIBILITIES:
// - Carry the values between Customer.Web and Operations.Api
//
// IMPORTANT NOTES:
// - Wire type: add fields at the end, never rename (ADR-0014)
// - Document: the file's bytes (up to 10 MB), checked by Operations
// =========================================================
public sealed record CustomerHoldAnswerBody(string Reason, string? Contents, decimal? Value, string? Choice, string? Note, string? FileName, byte[]? Document);
