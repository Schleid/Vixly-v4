using Vixly.Operations.Application.Dispatch;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Payables;

namespace Vixly.Operations.Application.Payables;

// =========================================================
// FILE: OperationsPayablesDesk.cs
//
// PURPOSE:
// What the bill use cases share (Slice 8).
//
// RESPONSIBILITIES:
// - Turn the shares typed on a bill (a dispatch or shipment number and an amount) into allocations
// - When a bill starts or stops counting, put what the carriers billed on its dispatches and share it again on their shipments
//
// IMPORTANT NOTES:
// - The caller saves
// =========================================================
public sealed class OperationsPayablesDesk
{
    private readonly IOperationsPayablesRepository _payables;

    private readonly IOperationsDispatchRepository _dispatches;

    private readonly OperationsDispatchCosting _costing;

    private readonly Vixly.Operations.Application.Security.OperationsAccess _access;

    public OperationsPayablesDesk(IOperationsPayablesRepository payables, IOperationsDispatchRepository dispatches, OperationsDispatchCosting costing,
        Vixly.Operations.Application.Security.OperationsAccess access)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _payables = payables ?? throw new ArgumentNullException(nameof(payables));
        _dispatches = dispatches ?? throw new ArgumentNullException(nameof(dispatches));
        _costing = costing ?? throw new ArgumentNullException(nameof(costing));
    }

    public IOperationsPayablesRepository Payables => _payables;

    public IOperationsDispatchRepository Dispatches => _dispatches;

    // What the approver read: a bill changed since gets another version.
    public static string Version(OperationsBill bill) =>
        (bill ?? throw new ArgumentNullException(nameof(bill))).LastModifiedUtc?.UtcTicks.ToString(System.Globalization.CultureInfo.InvariantCulture)
        ?? bill.CreatedUtc.UtcTicks.ToString(System.Globalization.CultureInfo.InvariantCulture);

    // The person holds any one of these rights (company-wide).
    public async Task<(Vixly.Operations.Application.Security.OperationsAccessView? Access, Vixly.Shared.Kernel.Errors.Error? Refused)> OpenAsync(
        Vixly.Operations.Application.Security.OperationsAuthorizationContext context, CancellationToken cancellationToken, params string[] permissions)
    {
        (Vixly.Operations.Application.Security.OperationsAccessView? View, Vixly.Shared.Kernel.Errors.Error? Refused) last = (null, null);

        foreach (string permission in permissions)
        {
            last = await _access.RequireAsync(context, permission, false, cancellationToken);

            if (last.Refused is null)
            {
                return last;
            }
        }

        return last;
    }

    public Task<(Vixly.Operations.Application.Security.OperationsAccessView? View, Vixly.Shared.Kernel.Errors.Error? Refused)> RequireAsync(
        Vixly.Operations.Application.Security.OperationsAuthorizationContext context, string permission, bool stepUp, CancellationToken cancellationToken) =>
        _access.RequireAsync(context, permission, stepUp, cancellationToken);

    // Each line: a dispatch or shipment number and an amount. Null with the refusal when a number is unknown, an amount wrong, or (review) a carrier's
    // bill put on a dispatch that carrier is not on.
    public async Task<(IReadOnlyList<OperationsBillAllocation>? Allocations, Vixly.Shared.Kernel.Errors.Error? Refused)> AllocationsAsync(
        IReadOnlyList<(string Number, decimal Amount)> lines, Guid? carrierId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(lines);
        List<OperationsBillAllocation> allocations = new();

        foreach ((string number, decimal amount) in lines)
        {
            string text = number.Trim().ToUpperInvariant();

            if (text.Length is 0 or > 40 || amount <= 0m || decimal.Round(amount, 2) != amount)
            {
                return (null, OperationsPayableErrors.AllocationInvalid);
            }

            if (await _payables.FindDispatchAsync(text, cancellationToken) is { } dispatch)
            {
                if (carrierId is { } carrier && await _dispatches.GetDispatchAsync(dispatch.Id, cancellationToken) is { } shown && shown.Legs.All(leg => leg.CarrierId != carrier))
                {
                    return (null, OperationsPayableErrors.CarrierNotOnDispatch);
                }

                allocations.Add(new OperationsBillAllocation(dispatch.Id, null, dispatch.Number, amount));
            }
            else if (await _payables.FindShipmentAsync(text, cancellationToken) is { } shipment)
            {
                allocations.Add(new OperationsBillAllocation(null, shipment.Id, shipment.Number, amount));
            }
            else
            {
                return (null, OperationsPayableErrors.AllocationNotFound);
            }
        }

        return (allocations, null);
    }

    // The dispatches this bill touches (before and after a change): what the carriers billed each, shared again on its shipments.
    public async Task ApplyAsync(OperationsBill bill, IEnumerable<Guid> dispatchIds, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(bill);

        foreach (Guid dispatchId in dispatchIds.Distinct())
        {
            if (await _dispatches.GetDispatchAsync(dispatchId, cancellationToken) is not { } dispatch)
            {
                continue;
            }

            // Review: only carriers' bills; complete once every carrier on the dispatch's legs has billed (until then the estimate stays).
            List<(Guid CarrierId, decimal Amount)> shares = (await _payables.CarrierSharesAsync(dispatchId, bill.Id, cancellationToken)).ToList();

            if (bill.Counts && bill.Party == OperationsBillParty.Carrier && bill.CarrierId is { } carrier)
            {
                shares.AddRange(bill.Allocations.Where(allocation => allocation.DispatchId == dispatchId).Select(allocation => (carrier, allocation.Amount)));
            }

            HashSet<Guid> carriers = dispatch.Legs.Where(leg => leg.CarrierId is not null).Select(leg => leg.CarrierId!.Value).ToHashSet();
            dispatch.Billed(shares.Count == 0 ? null : shares.Sum(share => share.Amount), carriers.Count > 0 && carriers.All(id => shares.Any(share => share.CarrierId == id)), utcNow);
            await _costing.ShareAsync(dispatch, utcNow, cancellationToken);
        }
    }
}
