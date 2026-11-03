using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Reports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Identity;
using Vixly.Operations.Domain.Reports;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Reports.ListOperationsReportSchedules;

// =========================================================
// FILE: ListOperationsReportSchedulesResponse.cs
//
// PURPOSE:
// The answer to listOperationsReportSchedules.
//
// RESPONSIBILITIES:
// - Say whether it succeeded, with what was asked or the refusal
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListOperationsReportSchedulesResponse
{
    private ListOperationsReportSchedulesResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsReportSchedule> Schedules { get; private init; } = Array.Empty<OperationsReportSchedule>();

    public IReadOnlyList<OperationsUser> Staff { get; private init; } = Array.Empty<OperationsUser>();

    public static ListOperationsReportSchedulesResponse Success(IReadOnlyList<OperationsReportSchedule> schedules, IReadOnlyList<OperationsUser> staff)
    {
        return new ListOperationsReportSchedulesResponse { Succeeded = true, Schedules = schedules, Staff = staff };
    }

    public static ListOperationsReportSchedulesResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListOperationsReportSchedulesResponse { Error = error };
    }
}
