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

namespace Vixly.Operations.Application.UseCases.Payables.VoidOperationsBill;

// =========================================================
// FILE: VoidOperationsBillResponse.cs
//
// PURPOSE:
// The answer to voidOperationsBill.
//
// RESPONSIBILITIES:
// - Say whether it succeeded, with what was asked or the refusal
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class VoidOperationsBillResponse
{
    private VoidOperationsBillResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static VoidOperationsBillResponse Success()
    {
        return new VoidOperationsBillResponse { Succeeded = true };
    }

    public static VoidOperationsBillResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new VoidOperationsBillResponse { Error = error };
    }
}
