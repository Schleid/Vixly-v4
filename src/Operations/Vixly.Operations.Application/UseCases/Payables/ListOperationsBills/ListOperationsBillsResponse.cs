using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Payables;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Dispatch;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Payables;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Payables.ListOperationsBills;

// =========================================================
// FILE: ListOperationsBillsResponse.cs
//
// PURPOSE:
// The answer to listOperationsBills.
//
// RESPONSIBILITIES:
// - Say whether it succeeded, with what was asked or the refusal
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListOperationsBillsResponse
{
    private ListOperationsBillsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsBill> Bills { get; private init; } = Array.Empty<OperationsBill>();

    public decimal Owed { get; private init; }

    public decimal Overdue { get; private init; }

    public int ToApprove { get; private init; }

    public DateOnly Today { get; private init; }

    public bool CanEnter { get; private init; }

    public bool CanApprove { get; private init; }

    public bool NeedApproval { get; private init; }

    public bool CanChangeSettings { get; private init; }

    public string CurrencyCode { get; private init; } = string.Empty;

    public static ListOperationsBillsResponse Success(IReadOnlyList<OperationsBill> bills, decimal owed, decimal overdue, int toApprove, DateOnly today, bool canEnter, bool canApprove, bool needApproval, bool canChangeSettings, string currencyCode)
    {
        return new ListOperationsBillsResponse { Succeeded = true, Bills = bills, Owed = owed, Overdue = overdue, ToApprove = toApprove, Today = today, CanEnter = canEnter, CanApprove = canApprove, NeedApproval = needApproval, CanChangeSettings = canChangeSettings, CurrencyCode = currencyCode };
    }

    public static ListOperationsBillsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListOperationsBillsResponse { Error = error };
    }
}
