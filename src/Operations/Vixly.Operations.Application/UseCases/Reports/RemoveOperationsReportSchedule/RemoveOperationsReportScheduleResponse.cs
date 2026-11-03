using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Reports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Identity;
using Vixly.Operations.Domain.Reports;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Reports.RemoveOperationsReportSchedule;

// =========================================================
// FILE: RemoveOperationsReportScheduleResponse.cs
//
// PURPOSE:
// The answer to removeOperationsReportSchedule.
//
// RESPONSIBILITIES:
// - Say whether it succeeded, with what was asked or the refusal
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RemoveOperationsReportScheduleResponse
{
    private RemoveOperationsReportScheduleResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static RemoveOperationsReportScheduleResponse Success()
    {
        return new RemoveOperationsReportScheduleResponse { Succeeded = true };
    }

    public static RemoveOperationsReportScheduleResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RemoveOperationsReportScheduleResponse { Error = error };
    }
}
