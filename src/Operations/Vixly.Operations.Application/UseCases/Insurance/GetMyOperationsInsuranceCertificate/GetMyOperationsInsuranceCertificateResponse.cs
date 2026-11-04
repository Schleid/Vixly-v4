using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Insurance.GetMyOperationsInsuranceCertificate;

// =========================================================
// FILE: GetMyOperationsInsuranceCertificateResponse.cs
//
// PURPOSE:
// The insurance certificate of the customer's own shipment (Slice 5B).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetMyOperationsInsuranceCertificateResponse
{
    private GetMyOperationsInsuranceCertificateResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public byte[] Content { get; private init; } = Array.Empty<byte>();

    public string FileName { get; private init; } = string.Empty;

    public static GetMyOperationsInsuranceCertificateResponse Success(byte[] content, string fileName)
    {
        return new GetMyOperationsInsuranceCertificateResponse { Succeeded = true, Content = content, FileName = fileName };
    }

    public static GetMyOperationsInsuranceCertificateResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetMyOperationsInsuranceCertificateResponse { Error = error };
    }
}
