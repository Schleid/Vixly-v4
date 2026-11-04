using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Billing;

// =========================================================
// FILE: OperationsCreditAccount.cs
//
// PURPOSE:
// A customer's credit account (Slice 5F step 2b, K-1 and K-5): opened and managed by the credit department.
//
// RESPONSIBILITIES:
// - Hold the limit, the terms and the state
// - Refuse closing while something is owed; refuse a hold or a close without a reason
// - Remember the last month its statement was sent
//
// IMPORTANT NOTES:
// - One per customer (unique index); what is owed is read from its charged invoices, never stored here
// - Its terms: its own days, or the company's
// =========================================================
public sealed class OperationsCreditAccount : OperationsEntity
{
    public const int MaxTermsDays = 365;

    public const decimal MaxLimit = 10_000_000m;

    private OperationsCreditAccount()
    {
    }

    private OperationsCreditAccount(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
    }

    public Guid CustomerId { get; private set; }

    public OperationsCreditAccountStatus Status { get; private set; }

    public decimal Limit { get; private set; }

    // None: the company's terms.
    public int? TermsDays { get; private set; }

    public string? Reason { get; private set; }

    public Guid OpenedByUserId { get; private set; }

    public Guid? ChangedByUserId { get; private set; }

    // The month ("2026-10") of the last statement notice sent (K-7).
    public string? StatementMonth { get; private set; }

    public bool IsActive => Status == OperationsCreditAccountStatus.Active;

    public int TermsOr(int companyDays) => TermsDays ?? companyDays;

    public static OperationsCreditAccount Open(Guid tenantId, Guid customerId, decimal limit, int? termsDays, Guid userId, DateTimeOffset utcNow)
    {
        if (customerId == Guid.Empty)
        {
            throw new DomainException(OperationsCreditErrors.NotFound);
        }

        OperationsCreditAccount account = new(tenantId, utcNow) { CustomerId = customerId, Status = OperationsCreditAccountStatus.Active, OpenedByUserId = userId };
        account.Set(limit, termsDays);
        return account;
    }

    // The credit department changes the limit, the terms or the state; a hold or a close says why; closed only when nothing is owed on it.
    public void Change(decimal limit, int? termsDays, OperationsCreditAccountStatus status, string? reason, decimal owed, Guid userId, DateTimeOffset utcNow)
    {
        if (!Enum.IsDefined(status))
        {
            throw new DomainException(OperationsCreditErrors.NotActive);
        }

        string? why = OperationsBillingLimits.Text(reason, OperationsBillingLimits.ReasonMaxLength, OperationsCreditErrors.ReasonRequired);

        if (status != OperationsCreditAccountStatus.Active && status != Status && why is null)
        {
            throw new DomainException(OperationsCreditErrors.ReasonRequired);
        }

        if (status == OperationsCreditAccountStatus.Closed && owed > 0m)
        {
            throw new DomainException(OperationsCreditErrors.BalanceOpen);
        }

        Set(limit, termsDays);
        (Status, Reason, ChangedByUserId) = (status, why ?? (status == Status ? Reason : null), userId);
        MarkAsModified(utcNow);
    }

    public void StatementSent(string month, DateTimeOffset utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(month);
        StatementMonth = month;
        MarkAsModified(utcNow);
    }

    private void Set(decimal limit, int? termsDays)
    {
        if (limit is < 0m or > MaxLimit || decimal.Round(limit, 2) != limit)
        {
            throw new DomainException(OperationsCreditErrors.LimitInvalid);
        }

        if (termsDays is < 0 or > MaxTermsDays)
        {
            throw new DomainException(OperationsCreditErrors.TermsInvalid);
        }

        (Limit, TermsDays) = (limit, termsDays);
    }
}
