using Vixly.Operations.Application.Reports;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsReportReader.cs
//
// PURPOSE:
// Works out the figures of a report (Slice 11).
//
// RESPONSIBILITIES:
// - Run one report for its days, cut and warehouses
//
// IMPORTANT NOTES:
// - Read only, tenant-filtered; heavy counts are done by the database
// - At most 13 months a run (owner, 2026-10-22)
// =========================================================
public interface IOperationsReportReader
{
    Task<OperationsReportTable> RunAsync(OperationsReportQuery query, CancellationToken cancellationToken);
}
