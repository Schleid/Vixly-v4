using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Counter.GetOperationsCounterDeclaration;

// =========================================================
// FILE: GetOperationsCounterDeclarationResponse.cs
//
// PURPOSE:
// The page the customer signs at the counter: their declaration of each parcel and the insurance they chose or declined (Slice 5D, D-8).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsCounterDeclarationResponse
{
    private GetOperationsCounterDeclarationResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public byte[] Content { get; private init; } = Array.Empty<byte>();

    public string FileName { get; private init; } = string.Empty;

    public static GetOperationsCounterDeclarationResponse Success(byte[] content, string fileName)
    {
        return new GetOperationsCounterDeclarationResponse { Succeeded = true, Content = content, FileName = fileName };
    }

    public static GetOperationsCounterDeclarationResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsCounterDeclarationResponse { Error = error };
    }
}
