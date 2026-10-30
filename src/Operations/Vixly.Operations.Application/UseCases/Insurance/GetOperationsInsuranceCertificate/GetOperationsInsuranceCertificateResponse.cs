using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Insurance.GetOperationsInsuranceCertificate;

// =========================================================
// FILE: GetOperationsInsuranceCertificateResponse.cs
//
// PURPOSE:
// A shipment's insurance certificate, for staff (Slice 5B).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsInsuranceCertificateResponse
{
    private GetOperationsInsuranceCertificateResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public byte[] Content { get; private init; } = Array.Empty<byte>();

    public string FileName { get; private init; } = string.Empty;

    public static GetOperationsInsuranceCertificateResponse Success(byte[] content, string fileName)
    {
        return new GetOperationsInsuranceCertificateResponse { Succeeded = true, Content = content, FileName = fileName };
    }

    public static GetOperationsInsuranceCertificateResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsInsuranceCertificateResponse { Error = error };
    }
}
