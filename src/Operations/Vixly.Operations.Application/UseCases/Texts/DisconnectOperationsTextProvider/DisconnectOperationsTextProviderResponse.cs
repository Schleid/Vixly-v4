using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Texts;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Texts;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Texts.DisconnectOperationsTextProvider;

// =========================================================
// FILE: DisconnectOperationsTextProviderResponse.cs
//
// PURPOSE:
// The answer to disconnectOperationsTextProvider.
//
// RESPONSIBILITIES:
// - Say whether it succeeded, with what was asked or the refusal
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class DisconnectOperationsTextProviderResponse
{
    private DisconnectOperationsTextProviderResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static DisconnectOperationsTextProviderResponse Success()
    {
        return new DisconnectOperationsTextProviderResponse { Succeeded = true };
    }

    public static DisconnectOperationsTextProviderResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new DisconnectOperationsTextProviderResponse { Error = error };
    }
}
