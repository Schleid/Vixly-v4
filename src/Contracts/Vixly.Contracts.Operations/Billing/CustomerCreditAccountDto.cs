namespace Vixly.Contracts.Operations.Billing;

// =========================================================
// FILE: CustomerCreditAccountDto.cs
//
// PURPOSE:
// The customer's credit account on the portal (Slice 5F step 2b, K-8).
//
// RESPONSIBILITIES:
// - Carry the state, the limit, the terms, what is owed, available and overdue, and the open items
//
// IMPORTANT NOTES:
// - HasAccount false: the customer has none and the rest is empty
// =========================================================
public sealed record CustomerCreditAccountDto(bool HasAccount, string State, decimal Limit, int TermsDays, string CurrencyCode, decimal Owed, decimal Available, decimal Overdue,
    IReadOnlyList<CustomerCreditItemDto> Items);
