using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Screens;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Screens;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Screens.GetOperationsScreenZelleQr;

// =========================================================
// FILE: GetOperationsScreenZelleQrResponse.cs
//
// PURPOSE:
// The answer to getOperationsScreenZelleQr.
//
// RESPONSIBILITIES:
// - Say whether it succeeded, with what was asked or the refusal
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsScreenZelleQrResponse
{
    private GetOperationsScreenZelleQrResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public byte[] Content { get; private init; } = Array.Empty<byte>();

    public string ContentType { get; private init; } = string.Empty;

    public static GetOperationsScreenZelleQrResponse Success(byte[] content, string contentType)
    {
        return new GetOperationsScreenZelleQrResponse { Succeeded = true, Content = content, ContentType = contentType };
    }

    public static GetOperationsScreenZelleQrResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsScreenZelleQrResponse { Error = error };
    }
}
