using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Cards;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Screens;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Screens;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Screens.ActOnOperationsScreen;

// =========================================================
// FILE: ActOnOperationsScreenResponse.cs
//
// PURPOSE:
// The answer to actOnOperationsScreen.
//
// RESPONSIBILITIES:
// - Say whether it succeeded, with what was asked or the refusal
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ActOnOperationsScreenResponse
{
    private ActOnOperationsScreenResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ActOnOperationsScreenResponse Success()
    {
        return new ActOnOperationsScreenResponse { Succeeded = true };
    }

    public static ActOnOperationsScreenResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ActOnOperationsScreenResponse { Error = error };
    }
}
