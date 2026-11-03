namespace Vixly.Operations.Application.Support;

// =========================================================
// FILE: OperationsSupportCaller.cs
//
// PURPOSE:
// What a support agent sees after the caller's phone matched one account (owner, 2026-10-29, S-7).
//
// RESPONSIBILITIES:
// - Carry the first name and last initial, the customer code and the account standing
//
// IMPORTANT NOTES:
// - Never an amount, an address, the full phone or the email (design 9.1)
// =========================================================
public sealed record OperationsSupportCaller(Guid CustomerId, string ShortName, string? Code, bool GoodStanding);
