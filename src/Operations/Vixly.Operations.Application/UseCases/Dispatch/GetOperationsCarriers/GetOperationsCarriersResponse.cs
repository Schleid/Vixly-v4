using Vixly.Operations.Domain.Dispatch;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Dispatch.GetOperationsCarriers;

// =========================================================
// FILE: GetOperationsCarriersResponse.cs
//
// PURPOSE:
// The company's carriers (Slice 6B, D-1).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsCarriersResponse
{
    private GetOperationsCarriersResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsCarrier> Carriers { get; private init; } = Array.Empty<OperationsCarrier>();

    public static GetOperationsCarriersResponse Success(IReadOnlyList<OperationsCarrier> carriers)
    {
        return new GetOperationsCarriersResponse { Succeeded = true, Carriers = carriers };
    }

    public static GetOperationsCarriersResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsCarriersResponse { Error = error };
    }
}
