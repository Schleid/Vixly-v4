namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsUsageReporter.cs
//
// PURPOSE:
// Sends a company's shipments of one month to Platform (shipment cap, owner 2026-10-29).
//
// RESPONSIBILITIES:
// - Report the month ("2026-10"), the count and whether the month is over
//
// IMPORTANT NOTES:
// - Implemented in Infrastructure; false when Platform cannot be reached (the next hour tries again)
// =========================================================
public interface IOperationsUsageReporter
{
    Task<bool> ReportShipmentsAsync(Guid tenantId, string month, int shipments, bool isFinal, CancellationToken cancellationToken);
}
