using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Reports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Reports;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Reports.RunOperationsReport;

// =========================================================
// FILE: RunOperationsReportResponse.cs
//
// PURPOSE:
// The answer to runOperationsReport.
//
// RESPONSIBILITIES:
// - Say whether it succeeded, with what was asked or the refusal
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RunOperationsReportResponse
{
    private RunOperationsReportResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsReportDefinition? Report { get; private init; }

    public OperationsReportTable? Table { get; private init; }

    public IReadOnlyList<decimal?>? Previous { get; private init; }

    public DateOnly From { get; private init; }

    public DateOnly To { get; private init; }

    public DateOnly? PreviousFrom { get; private init; }

    public DateOnly? PreviousTo { get; private init; }

    public OperationsReportGrain Grain { get; private init; }

    public OperationsReportDimension Dimension { get; private init; }

    public Guid? WarehouseId { get; private init; }

    public int Days { get; private init; }

    public IReadOnlyList<OperationsReportPlace> Places { get; private init; } = Array.Empty<OperationsReportPlace>();

    public bool SeesMoney { get; private init; }

    public string CurrencyCode { get; private init; } = string.Empty;

    public OperationsWeightUnit WeightUnit { get; private init; }

    public static RunOperationsReportResponse Success(OperationsReportDefinition? report, OperationsReportTable? table, IReadOnlyList<decimal?>? previous, DateOnly from, DateOnly to, DateOnly? previousFrom, DateOnly? previousTo, OperationsReportGrain grain, OperationsReportDimension dimension, Guid? warehouseId, int days, IReadOnlyList<OperationsReportPlace> places, bool seesMoney, string currencyCode, OperationsWeightUnit weightUnit)
    {
        return new RunOperationsReportResponse { Succeeded = true, Report = report, Table = table, Previous = previous, From = from, To = to, PreviousFrom = previousFrom, PreviousTo = previousTo, Grain = grain, Dimension = dimension, WarehouseId = warehouseId, Days = days, Places = places, SeesMoney = seesMoney, CurrencyCode = currencyCode, WeightUnit = weightUnit };
    }

    public static RunOperationsReportResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RunOperationsReportResponse { Error = error };
    }
}
