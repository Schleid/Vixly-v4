using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Reports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Reports;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Reports.ListOperationsReports;

// =========================================================
// FILE: ListOperationsReportsResponse.cs
//
// PURPOSE:
// The answer to listOperationsReports.
//
// RESPONSIBILITIES:
// - Say whether it succeeded, with what was asked or the refusal
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListOperationsReportsResponse
{
    private ListOperationsReportsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsReportDefinition> Reports { get; private init; } = Array.Empty<OperationsReportDefinition>();

    public bool SeesMoney { get; private init; }

    public bool SeesProfit { get; private init; }

    public static ListOperationsReportsResponse Success(IReadOnlyList<OperationsReportDefinition> reports, bool seesMoney, bool seesProfit)
    {
        return new ListOperationsReportsResponse { Succeeded = true, Reports = reports, SeesMoney = seesMoney, SeesProfit = seesProfit };
    }

    public static ListOperationsReportsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListOperationsReportsResponse { Error = error };
    }
}
