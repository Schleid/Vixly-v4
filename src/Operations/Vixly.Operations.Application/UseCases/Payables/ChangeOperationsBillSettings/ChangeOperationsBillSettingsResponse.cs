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

namespace Vixly.Operations.Application.UseCases.Payables.ChangeOperationsBillSettings;

// =========================================================
// FILE: ChangeOperationsBillSettingsResponse.cs
//
// PURPOSE:
// The answer to changeOperationsBillSettings.
//
// RESPONSIBILITIES:
// - Say whether it succeeded, with what was asked or the refusal
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ChangeOperationsBillSettingsResponse
{
    private ChangeOperationsBillSettingsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ChangeOperationsBillSettingsResponse Success()
    {
        return new ChangeOperationsBillSettingsResponse { Succeeded = true };
    }

    public static ChangeOperationsBillSettingsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ChangeOperationsBillSettingsResponse { Error = error };
    }
}
