using Vixly.Operations.Domain.Billing;

namespace Vixly.Operations.Application.UseCases.Credit.ChangeOperationsCreditAccount;

// =========================================================
// FILE: ChangeOperationsCreditAccountRequest.cs
//
// PURPOSE:
// Change a credit account's limit, terms or state (Slice 5F step 2b, K-1 and K-5).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ChangeOperationsCreditAccountRequest
{
    public Guid CustomerId { get; init; }

    public decimal Limit { get; init; }

    public int? TermsDays { get; init; }

    public OperationsCreditAccountStatus Status { get; init; }

    public string? Reason { get; init; }
}
