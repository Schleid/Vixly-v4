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

namespace Vixly.Operations.Application.UseCases.Payables.GetOperationsBill;

// =========================================================
// FILE: GetOperationsBillResponse.cs
//
// PURPOSE:
// The answer to getOperationsBill.
//
// RESPONSIBILITIES:
// - Say whether it succeeded, with what was asked or the refusal
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsBillResponse
{
    private GetOperationsBillResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsBill? Bill { get; private init; }

    public IReadOnlyList<OperationsBillPartyOption> Parties { get; private init; } = Array.Empty<OperationsBillPartyOption>();

    public bool CanEdit { get; private init; }

    public bool CanDecide { get; private init; }

    public bool CanPay { get; private init; }

    public bool CanVoid { get; private init; }

    public bool NeedApproval { get; private init; }

    public string CurrencyCode { get; private init; } = string.Empty;

    public DateOnly Today { get; private init; }

    public IReadOnlyDictionary<Guid, string> People { get; private init; } = new Dictionary<Guid, string>();

    public static GetOperationsBillResponse Success(OperationsBill? bill, IReadOnlyList<OperationsBillPartyOption> parties, bool canEdit, bool canDecide, bool canPay, bool canVoid, bool needApproval, string currencyCode, DateOnly today, IReadOnlyDictionary<Guid, string> people)
    {
        return new GetOperationsBillResponse { Succeeded = true, Bill = bill, Parties = parties, CanEdit = canEdit, CanDecide = canDecide, CanPay = canPay, CanVoid = canVoid, NeedApproval = needApproval, CurrencyCode = currencyCode, Today = today, People = people };
    }

    public static GetOperationsBillResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsBillResponse { Error = error };
    }
}
