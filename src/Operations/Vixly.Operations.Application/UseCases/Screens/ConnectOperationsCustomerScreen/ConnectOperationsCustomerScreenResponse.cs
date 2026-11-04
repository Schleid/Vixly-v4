using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Screens;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Screens;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Screens.ConnectOperationsCustomerScreen;

// =========================================================
// FILE: ConnectOperationsCustomerScreenResponse.cs
//
// PURPOSE:
// The answer to connectOperationsCustomerScreen.
//
// RESPONSIBILITIES:
// - Say whether it succeeded, with what was asked or the refusal
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ConnectOperationsCustomerScreenResponse
{
    private ConnectOperationsCustomerScreenResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public string Secret { get; private init; } = string.Empty;

    public static ConnectOperationsCustomerScreenResponse Success(string secret)
    {
        return new ConnectOperationsCustomerScreenResponse { Succeeded = true, Secret = secret };
    }

    public static ConnectOperationsCustomerScreenResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ConnectOperationsCustomerScreenResponse { Error = error };
    }
}
