namespace Vixly.Contracts.Operations.Internal;

// =========================================================
// FILE: OperationsMaintenancePassDto.cs
//
// PURPOSE:
// A maintenance pass link: what Customer.Web sends and what Operations.Api answers.
//
// RESPONSIBILITIES:
// - Carry the token, or the end of the window it opens
//
// IMPORTANT NOTES:
// - Additive changes only (cross-plane rule 4)
// =========================================================
public sealed record OperationsMaintenancePassDto(string? Token, DateTimeOffset? EndsUtc);
