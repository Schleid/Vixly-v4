namespace Vixly.Platform.Application.UseCases.TenantAccount.ReportTenantShipmentUsage;

// =========================================================
// FILE: ReportTenantShipmentUsageRequest.cs
//
// PURPOSE:
// Records a company's shipments of one month and, once the month is closed, charges the shipments above the plan's cap (shipment cap, owner 2026-10-29).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ReportTenantShipmentUsageRequest
{
    public string Month { get; init; } = string.Empty;

    public int Shipments { get; init; }

    public bool IsFinal { get; init; }
}
