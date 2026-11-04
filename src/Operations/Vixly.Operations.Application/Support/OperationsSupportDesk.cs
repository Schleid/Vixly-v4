using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Site;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Support;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.Support;

// =========================================================
// FILE: OperationsSupportDesk.cs
//
// PURPOSE:
// The support agent's call: the caller's phone first, then a tracking number that must be theirs (owner, 2026-10-29, design section 9).
//
// RESPONSIBILITIES:
// - Find the caller by their full phone number: short name, code and standing only
// - Open one shipment or package by its tracking number (Vixly's, or the carrier's for a mailbox package), only when it is the caller's
// - Without a matching caller, the status only, by Vixly's tracking number
// - Record every lookup (never the phone typed) and stop at OperationsAccessLimits.SupportLookupsPerHour an hour
//
// IMPORTANT NOTES:
// - The caller's: they sent it (the shipment's customer), or they receive it (the recipient's phone is theirs)
// - Not found and not the caller's give the same answer; the second is recorded as such
// - The caller's account id travels between the two steps protected by the web layer; this re-reads it each time
// =========================================================
public sealed class OperationsSupportDesk
{
    public const string CallerAction = "ops.calls.caller";

    public const string TrackingAction = "ops.calls.tracking";

    public const string Area = "calls";

    private const int PhoneMinDigits = 7;

    private readonly IOperationsCustomerRepository _customers;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsTrackingRepository _tracking;

    private readonly IOperationsBillingRepository _billing;

    private readonly IOperationsLocationRepository _locations;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsAuditReader _audit;

    private readonly OperationsAuditTrail _trail;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public OperationsSupportDesk(IOperationsCustomerRepository customers, IOperationsReceivingRepository receiving, IOperationsTrackingRepository tracking,
        IOperationsBillingRepository billing, IOperationsLocationRepository locations, IOperationsSettingsRepository settings, IOperationsAuditReader audit,
        OperationsAuditTrail trail, IOperationsUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _trail = trail ?? throw new ArgumentNullException(nameof(trail));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    // =====================================================
    // STEP 1: THE CALLER'S PHONE
    // =====================================================
    public async Task<(OperationsSupportCaller? Caller, Error? Error)> CallerAsync(OperationsAccessView access, string? phone, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(access);

        string digits = OperationsSupportMasks.Digits(phone);

        if (digits.Length < PhoneMinDigits || (phone ?? string.Empty).Any(char.IsLetter))
        {
            return (null, OperationsSupportErrors.PhoneInvalid);
        }

        if (await OverLimitAsync(access, cancellationToken))
        {
            return (null, OperationsSupportErrors.LookupLimit);
        }

        List<OperationsCustomer> found = (await _customers.FindForCounterAsync(digits, 5, cancellationToken))
            .Where(customer => OperationsSupportMasks.SamePhone(customer.Phone, digits) && customer.Status is not (OperationsCustomerStatus.Closed or OperationsCustomerStatus.Banned))
            .ToList();
        OperationsCustomer? caller = found.Count == 1 ? found[0] : null;

        _trail.Record(access.User.Id, CallerAction, nameof(OperationsCustomer), caller?.Id, after: new { found = found.Count });
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Two accounts with one number: nobody is named; the call goes on as a recipient's.
        return caller is null
            ? (null, null)
            : (new OperationsSupportCaller(caller.Id, OperationsSupportMasks.ShortName(caller.FirstName, caller.LastName), caller.Code, await GoodStandingAsync(caller, cancellationToken)),
                null);
    }

    // =====================================================
    // STEP 2: THE TRACKING NUMBER
    // =====================================================
    public async Task<(OperationsSupportCase? Case, Error? Error)> CaseAsync(OperationsAccessView access, Guid? callerId, string? number, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(access);

        if (string.IsNullOrWhiteSpace(number))
        {
            return (null, OperationsSupportErrors.NotFound);
        }

        if (await OverLimitAsync(access, cancellationToken))
        {
            return (null, OperationsSupportErrors.LookupLimit);
        }

        OperationsCustomer? caller = callerId is { } id ? await _customers.GetAsync(id, cancellationToken) : null;
        Found found = await FindAsync(caller, number, cancellationToken);

        _trail.Record(access.User.Id, TrackingAction, found.Shipment is null ? nameof(OperationsParcel) : nameof(OperationsShipment), found.Shipment?.Id ?? found.ParcelId,
            arguments: new { number = number.Trim() }, after: new { result = found.Result });
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return found.Result switch
        {
            "status_only" => (await StatusOnlyAsync(found.Shipment!, found.Number, cancellationToken), null),
            "caller_required" => (null, OperationsSupportErrors.CallerRequired),
            "shipment" or "package" => (await FullAsync(caller!, found, cancellationToken), null),
            _ => (null, OperationsSupportErrors.NotFound)
        };
    }

    // For a call ticket: the same match, without counting or recording it again.
    // The ticket's warehouse: where the package is, else where the shipment goes, else where it left.
    public async Task<(Guid CustomerId, Guid WarehouseId, Guid? ShipmentId, Guid? ParcelId, string Number)?> MatchAsync(Guid callerId, string? number, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(number) || await _customers.GetAsync(callerId, cancellationToken) is not { } caller)
        {
            return null;
        }

        Found found = await FindAsync(caller, number, cancellationToken);
        Guid? warehouse = found.Parcel?.WarehouseId ?? found.Shipment?.DestinationWarehouseId ?? found.Shipment?.OriginWarehouseId;
        return found.Result is "shipment" or "package" && warehouse is { } at ? (caller.Id, at, found.Shipment?.Id, found.ParcelId, found.Number) : null;
    }

    private async Task<Found> FindAsync(OperationsCustomer? caller, string number, CancellationToken cancellationToken)
    {
        // Vixly's tracking number (its check digit fails on a typo, so it is never looked up then).
        if (OperationsTrackingNumbers.Read(number) is { } tracking && await _receiving.FindShipmentByTrackingNumberAsync(tracking, cancellationToken) is { } shipment)
        {
            string formatted = OperationsTrackingNumbers.Format(tracking);

            if (caller is null)
            {
                return new Found("status_only", formatted, shipment, null, null);
            }

            return Belongs(shipment, caller) ? new Found("shipment", formatted, shipment, null, null) : new Found("not_theirs", formatted, shipment, null, null);
        }

        // A carrier's number (Amazon, UPS, USPS...): only for the caller's own packages.
        if (!OperationsCarrierTracking.TryNormalize(number, out string carrierNumber))
        {
            return new Found("not_found", number.Trim(), null, null, null);
        }

        if (caller is null)
        {
            return new Found("caller_required", carrierNumber, null, null, null);
        }

        if (await _receiving.FindCustomerParcelByCarrierNumberAsync(caller.Id, carrierNumber, cancellationToken) is { } parcel)
        {
            OperationsShipment? its = parcel.ShipmentId is { } shipmentId ? await _receiving.GetShipmentAsync(shipmentId, cancellationToken) : null;
            return new Found("package", carrierNumber, its, parcel.Id, parcel);
        }

        if (await _receiving.FindOpenPreAlertAsync(caller.Id, carrierNumber, cancellationToken) is { ShipmentId: { } announcedIn })
        {
            return new Found("package", carrierNumber, await _receiving.GetShipmentAsync(announcedIn, cancellationToken), null, null);
        }

        return new Found("not_found", carrierNumber, null, null, null);
    }

    // They sent it, or they receive it (the recipient's phone is the caller's).
    private static bool Belongs(OperationsShipment shipment, OperationsCustomer caller) =>
        shipment.CustomerId == caller.Id || OperationsSupportMasks.SamePhone(shipment.RecipientPhone, caller.Phone);

    private async Task<OperationsSupportCase> StatusOnlyAsync(OperationsShipment shipment, string number, CancellationToken cancellationToken)
    {
        (IReadOnlyList<OperationsParcel> parcels, IReadOnlyList<OperationsPublicMilestone> milestones) = await MilestonesAsync(shipment, cancellationToken);
        return new OperationsSupportCase(true, number, false, milestones, parcels.Any(parcel => parcel.HoldReasons != OperationsHoldReasons.None))
        {
            Cancelled = shipment.Status == OperationsShipmentStatus.Cancelled
        };
    }

    private async Task<OperationsSupportCase> FullAsync(OperationsCustomer caller, Found found, CancellationToken cancellationToken)
    {
        OperationsShipment? shipment = found.Shipment;
        IReadOnlyList<OperationsParcel> parcels = Array.Empty<OperationsParcel>();
        IReadOnlyList<OperationsPublicMilestone> milestones = Array.Empty<OperationsPublicMilestone>();
        decimal? due = null;
        string? currency = null;
        bool paid = false;
        string? destination = null;

        if (shipment is not null)
        {
            (parcels, milestones) = await MilestonesAsync(shipment, cancellationToken);
            List<Vixly.Operations.Domain.Billing.OperationsInvoice> invoices = (await _billing.ListShipmentInvoicesAsync(new[] { shipment.Id }, cancellationToken))
                .Where(invoice => invoice.Status != OperationsInvoiceStatus.Voided).ToList();

            if (invoices.Count > 0)
            {
                due = Math.Max(0m, invoices.Sum(invoice => invoice.Owed));
                currency = invoices[0].CurrencyCode;
                paid = due == 0m;
            }

            destination = shipment.DestinationWarehouseId is { } warehouseId ? (await _locations.GetWarehouseAsync(warehouseId, cancellationToken))?.Name
                : shipment.DestinationBranchId is { } branchId ? (await _locations.GetBranchAsync(branchId, cancellationToken))?.Name : null;
        }
        else if (found.Parcel is { } alone)
        {
            parcels = new[] { alone };
            milestones = OperationsTrackingViews.ForPublic(await _tracking.ListForSubjectAsync(OperationsTrackingSubject.Parcel, alone.Id, cancellationToken))
                .Select(item => new OperationsPublicMilestone(item.Milestone.ToString(), item.OccurredUtc, null)).ToList();
        }

        // A package: its own holds; a shipment: the holds of all its packages.
        IEnumerable<OperationsParcel> held = found.Parcel is { } one ? new[] { one } : parcels;
        OperationsHoldReasons holds = held.Aggregate(OperationsHoldReasons.None, (all, parcel) => all | parcel.HoldReasons);

        return new OperationsSupportCase(false, found.Number, found.Result == "package", milestones, holds != OperationsHoldReasons.None)
        {
            ShipmentId = shipment?.Id,
            ParcelId = found.ParcelId,
            Cancelled = shipment?.Status == OperationsShipmentStatus.Cancelled,
            Collect = shipment?.FreightCollect == true,
            Holds = holds,
            Destination = destination,
            AmountDue = due,
            CurrencyCode = currency,
            Paid = paid,
            Received = found.Parcel is not null,
            ReceivedUtc = found.Parcel?.CreatedUtc,
            OtherPackages = found.Result == "package" ? Math.Max(0, parcels.Count - (found.Parcel is null ? 0 : 1)) : 0,
            CustomerName = OperationsSupportMasks.ShortName(caller.FirstName, caller.LastName),
            CustomerCode = caller.Code,
            MaskedPhone = OperationsSupportMasks.Phone(caller.Phone),
            MaskedEmail = OperationsSupportMasks.Email(caller.Email.Value),
            City = caller.HomeAddress?.City
        };
    }

    private async Task<(IReadOnlyList<OperationsParcel> Parcels, IReadOnlyList<OperationsPublicMilestone> Milestones)> MilestonesAsync(OperationsShipment shipment,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<OperationsParcel> parcels = await _receiving.ListShipmentParcelsAsync(shipment.Id, cancellationToken);
        List<OperationsTrackingEvent> events = new(await _tracking.ListForSubjectAsync(OperationsTrackingSubject.Shipment, shipment.Id, cancellationToken));

        if (parcels.Count > 0)
        {
            events.AddRange(await _tracking.ListForSubjectsAsync(OperationsTrackingSubject.Parcel, parcels.Select(parcel => parcel.Id).ToList(), cancellationToken));
        }

        return (parcels, OperationsTrackingViews.ForPublic(events).Select(item => new OperationsPublicMilestone(item.Milestone.ToString(), item.OccurredUtc, null)).ToList());
    }

    // Good standing: an active account with nothing overdue. Never an amount (design 9.1).
    private async Task<bool> GoodStandingAsync(OperationsCustomer customer, CancellationToken cancellationToken)
    {
        if (customer.Status != OperationsCustomerStatus.Active)
        {
            return false;
        }

        DateOnly today = OperationsTimeZones.Today(_timeProvider.GetUtcNow(), (await _settings.FindAsync(cancellationToken))?.TimeZone);
        return !(await _billing.ListCustomerInvoicesAsync(customer.Id, 200, cancellationToken))
            .Any(invoice => invoice.Status != OperationsInvoiceStatus.Voided && invoice.Owed > 0m && invoice.DueOn is { } due && due < today);
    }

    private async Task<bool> OverLimitAsync(OperationsAccessView access, CancellationToken cancellationToken)
    {
        (IReadOnlyList<Vixly.Operations.Domain.Audit.OperationsAuditEntry> _, int lastHour) = await _audit.ListAsync(
            new OperationsAuditFilter(access.User.Id, Area, _timeProvider.GetUtcNow().AddHours(-1), null), 1, 1, cancellationToken);
        return lastHour >= OperationsAccessLimits.SupportLookupsPerHour;
    }

    private sealed record Found(string Result, string Number, OperationsShipment? Shipment, Guid? ParcelId, OperationsParcel? Parcel);
}
