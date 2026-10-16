using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Payments.ReceivePlatformProviderEvent;

// =========================================================
// FILE: ReceivePlatformProviderEventResponse.cs
//
// PURPOSE:
// Accepts a webhook from the card provider: checks its signature, keeps it once and queues it (D-P5).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ReceivePlatformProviderEventResponse
{
    private ReceivePlatformProviderEventResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public bool Duplicate { get; private init; }

    public static ReceivePlatformProviderEventResponse Success(bool duplicate)
    {
        return new ReceivePlatformProviderEventResponse { Succeeded = true, Duplicate = duplicate };
    }

    public static ReceivePlatformProviderEventResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ReceivePlatformProviderEventResponse { Error = error };
    }
}
