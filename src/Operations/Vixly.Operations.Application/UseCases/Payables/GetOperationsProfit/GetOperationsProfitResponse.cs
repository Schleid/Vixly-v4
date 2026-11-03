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

namespace Vixly.Operations.Application.UseCases.Payables.GetOperationsProfit;

// =========================================================
// FILE: GetOperationsProfitResponse.cs
//
// PURPOSE:
// The answer to getOperationsProfit.
//
// RESPONSIBILITIES:
// - Say whether it succeeded, with what was asked or the refusal
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsProfitResponse
{
    private GetOperationsProfitResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsShipmentProfit> Rows { get; private init; } = Array.Empty<OperationsShipmentProfit>();

    public DateOnly From { get; private init; }

    public DateOnly To { get; private init; }

    public string CurrencyCode { get; private init; } = string.Empty;

    public static GetOperationsProfitResponse Success(IReadOnlyList<OperationsShipmentProfit> rows, DateOnly from, DateOnly to, string currencyCode)
    {
        return new GetOperationsProfitResponse { Succeeded = true, Rows = rows, From = from, To = to, CurrencyCode = currencyCode };
    }

    public static GetOperationsProfitResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsProfitResponse { Error = error };
    }
}
