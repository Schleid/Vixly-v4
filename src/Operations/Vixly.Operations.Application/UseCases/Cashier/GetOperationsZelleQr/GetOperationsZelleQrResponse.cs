using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Locations;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Cashier.GetOperationsZelleQr;

// =========================================================
// FILE: GetOperationsZelleQrResponse.cs
//
// PURPOSE:
// The company's Zelle QR picture, for the cashier and the settings page (Slice 5F, P-8).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsZelleQrResponse
{
    private GetOperationsZelleQrResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public byte[] Content { get; private init; } = Array.Empty<byte>();

    public string ContentType { get; private init; } = string.Empty;

    public static GetOperationsZelleQrResponse Success(byte[] content, string contentType)
    {
        return new GetOperationsZelleQrResponse { Succeeded = true, Content = content, ContentType = contentType };
    }

    public static GetOperationsZelleQrResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsZelleQrResponse { Error = error };
    }
}
