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

namespace Vixly.Operations.Application.UseCases.Screens.PairOperationsCustomerScreen;

// =========================================================
// FILE: PairOperationsCustomerScreenResponse.cs
//
// PURPOSE:
// The answer to pairOperationsCustomerScreen.
//
// RESPONSIBILITIES:
// - Say whether it succeeded, with what was asked or the refusal
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class PairOperationsCustomerScreenResponse
{
    private PairOperationsCustomerScreenResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid ScreenId { get; private init; }

    public string Code { get; private init; } = string.Empty;

    public DateTimeOffset ExpiresUtc { get; private init; }

    public static PairOperationsCustomerScreenResponse Success(Guid screenId, string code, DateTimeOffset expiresUtc)
    {
        return new PairOperationsCustomerScreenResponse { Succeeded = true, ScreenId = screenId, Code = code, ExpiresUtc = expiresUtc };
    }

    public static PairOperationsCustomerScreenResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new PairOperationsCustomerScreenResponse { Error = error };
    }
}
