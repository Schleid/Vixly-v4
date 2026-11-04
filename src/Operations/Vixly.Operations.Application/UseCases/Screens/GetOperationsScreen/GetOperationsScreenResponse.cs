using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Operations.Application.Cards;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Screens;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Screens;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Screens.GetOperationsScreen;

// =========================================================
// FILE: GetOperationsScreenResponse.cs
//
// PURPOSE:
// The answer to getOperationsScreen.
//
// RESPONSIBILITIES:
// - Say whether it succeeded, with what was asked or the refusal
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsScreenResponse
{
    private GetOperationsScreenResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid ScreenId { get; private init; }

    public string ScreenName { get; private init; } = string.Empty;

    public OperationsScreenStep Step { get; private init; }

    public string Version { get; private init; } = string.Empty;

    public string? Language { get; private init; }

    public OperationsScreenView? View { get; private init; }

    public static GetOperationsScreenResponse Success(Guid screenId, string screenName, OperationsScreenStep step, string version, string? language, OperationsScreenView? view)
    {
        return new GetOperationsScreenResponse { Succeeded = true, ScreenId = screenId, ScreenName = screenName, Step = step, Version = version, Language = language, View = view };
    }

    public static GetOperationsScreenResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsScreenResponse { Error = error };
    }
}
