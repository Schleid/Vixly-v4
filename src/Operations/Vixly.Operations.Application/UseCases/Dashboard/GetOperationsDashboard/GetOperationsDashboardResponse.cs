using Vixly.Operations.Application.Dashboard;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Dashboard.GetOperationsDashboard;

// =========================================================
// FILE: GetOperationsDashboardResponse.cs
//
// PURPOSE:
// The answer to getOperationsDashboard.
//
// RESPONSIBILITIES:
// - Say whether it succeeded, with what was asked or the refusal
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsDashboardResponse
{
    private GetOperationsDashboardResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsDashboardCounter> Counters { get; private init; } = Array.Empty<OperationsDashboardCounter>();

    public static GetOperationsDashboardResponse Success(IReadOnlyList<OperationsDashboardCounter> counters)
    {
        return new GetOperationsDashboardResponse { Succeeded = true, Counters = counters };
    }

    public static GetOperationsDashboardResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsDashboardResponse { Error = error };
    }
}
