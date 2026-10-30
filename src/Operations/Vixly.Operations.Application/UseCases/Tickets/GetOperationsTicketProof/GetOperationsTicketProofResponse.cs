using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Tickets;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Tickets.GetOperationsTicketProof;

// =========================================================
// FILE: GetOperationsTicketProofResponse.cs
//
// PURPOSE:
// The carrier's proof of delivery attached to a ticket, for staff (Slice 4D step 4).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsTicketProofResponse
{
    private GetOperationsTicketProofResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public byte[] Content { get; private init; } = Array.Empty<byte>();

    public string ContentType { get; private init; } = string.Empty;

    public static GetOperationsTicketProofResponse Success(byte[] content, string contentType)
    {
        return new GetOperationsTicketProofResponse { Succeeded = true, Content = content, ContentType = contentType };
    }

    public static GetOperationsTicketProofResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsTicketProofResponse { Error = error };
    }
}
