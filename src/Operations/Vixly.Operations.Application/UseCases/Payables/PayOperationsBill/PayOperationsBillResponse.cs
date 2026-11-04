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

namespace Vixly.Operations.Application.UseCases.Payables.PayOperationsBill;

// =========================================================
// FILE: PayOperationsBillResponse.cs
//
// PURPOSE:
// The answer to payOperationsBill.
//
// RESPONSIBILITIES:
// - Say whether it succeeded, with what was asked or the refusal
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class PayOperationsBillResponse
{
    private PayOperationsBillResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static PayOperationsBillResponse Success()
    {
        return new PayOperationsBillResponse { Succeeded = true };
    }

    public static PayOperationsBillResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new PayOperationsBillResponse { Error = error };
    }
}
