using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.TenantAccount.ReportTenantShipmentUsage;

// =========================================================
// FILE: ReportTenantShipmentUsageResponse.cs
//
// PURPOSE:
// Records a company's shipments of one month and, once the month is closed, charges the shipments above the plan's cap (shipment cap, owner 2026-10-29).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ReportTenantShipmentUsageResponse
{
    private ReportTenantShipmentUsageResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ReportTenantShipmentUsageResponse Success()
    {
        return new ReportTenantShipmentUsageResponse { Succeeded = true };
    }

    public static ReportTenantShipmentUsageResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ReportTenantShipmentUsageResponse { Error = error };
    }
}
