using Vixly.Operations.Application.Delivery;
using Vixly.Operations.Domain.Delivery;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Delivery.GetOperationsDeliveryRoute;

// =========================================================
// FILE: GetOperationsDeliveryRouteResponse.cs
//
// PURPOSE:
// A delivery route: its stops, what to collect, how each went; the shipments that may join it while planned (Slice 7 step 3, R-7, R-12, R-13, R-15).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsDeliveryRouteResponse
{
    private GetOperationsDeliveryRouteResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid RouteId { get; private init; }

    public string Number { get; private init; } = string.Empty;

    public OperationsRouteStatus Status { get; private init; }

    public DateOnly ServiceDate { get; private init; }

    public string DriverName { get; private init; } = string.Empty;

    public string WarehouseName { get; private init; } = string.Empty;

    public string? TimeZone { get; private init; }

    public bool Planner { get; private init; }

    public bool CanRecord { get; private init; }

    public IReadOnlyList<OperationsDeliveryStopRow> Stops { get; private init; } = Array.Empty<OperationsDeliveryStopRow>();

    public IReadOnlyList<OperationsDeliveryWaitingRow> Waiting { get; private init; } = Array.Empty<OperationsDeliveryWaitingRow>();

    public decimal CashCollected { get; private init; }

    public decimal? CashHandedIn { get; private init; }

    public string? SettleNote { get; private init; }

    public string? CancelReason { get; private init; }

    public bool CashPaidIn { get; private init; }

    public OperationsDeliveryProofs Proofs { get; private init; }

    public OperationsDeliveryRecording Recording { get; private init; }

    public bool CashOnDelivery { get; private init; }

    public string CurrencyCode { get; private init; } = string.Empty;

    public static GetOperationsDeliveryRouteResponse Success(Guid routeId, string number, OperationsRouteStatus status, DateOnly serviceDate, string driverName, string warehouseName, string? timeZone, bool planner, bool canRecord, IReadOnlyList<OperationsDeliveryStopRow> stops, IReadOnlyList<OperationsDeliveryWaitingRow> waiting, decimal cashCollected, decimal? cashHandedIn, string? settleNote, string? cancelReason, bool cashPaidIn, OperationsDeliveryProofs proofs, OperationsDeliveryRecording recording, bool cashOnDelivery, string currencyCode)
    {
        return new GetOperationsDeliveryRouteResponse { Succeeded = true, RouteId = routeId, Number = number, Status = status, ServiceDate = serviceDate, DriverName = driverName, WarehouseName = warehouseName, TimeZone = timeZone, Planner = planner, CanRecord = canRecord, Stops = stops, Waiting = waiting, CashCollected = cashCollected, CashHandedIn = cashHandedIn, SettleNote = settleNote, CancelReason = cancelReason, CashPaidIn = cashPaidIn, Proofs = proofs, Recording = recording, CashOnDelivery = cashOnDelivery, CurrencyCode = currencyCode };
    }

    public static GetOperationsDeliveryRouteResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsDeliveryRouteResponse { Error = error };
    }
}
