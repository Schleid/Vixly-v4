using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Receiving;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Shipments;

// =========================================================
// FILE: OperationsShipment.cs
//
// PURPOSE:
// A customer's shipment: the parcels that travel together (ADR-0018; owner, 2026-10-18).
//
// RESPONSIBILITIES:
// - Open at a receiving warehouse when the first parcel arrives; parcels join while it is open
//
// IMPORTANT NOTES:
// - A parcel always belongs to a shipment (D-R1); service, charges and number arrive with Slice 5
// - The customer creates a shipment with its instructions, with or without packages; pre-alerts come first and are put into it (owner, 2026-10-19)
// - A package that is in none of the customer's shipments waits in its waiting shipment (no instructions) until the customer places it (Slice 4D)
// - Slice 5C: the number of parcels the customer declares; final (Booked) when they are all in, measured and clear, then locked (owner, 2026-10-20)
// =========================================================
public sealed class OperationsShipment : OperationsEntity
{
    private OperationsShipment()
    {
    }

    private OperationsShipment(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
    }

    public Guid CustomerId { get; private set; }

    public Guid OriginWarehouseId { get; private set; }

    public OperationsShipmentStatus Status { get; private set; }

    public Guid? DestinationBranchId { get; private set; }

    public OperationsPostalAddress? DeliveryAddress { get; private set; }

    public string? DestinationCountryCode { get; private set; }

    public string? RecipientName { get; private set; }

    public string? RecipientPhone { get; private set; }

    public OperationsShipmentOptions Options { get; private set; }

    // Slice 4C: the destination warehouse the customer chose; with the origin it is the route that prices the shipment.
    public Guid? DestinationWarehouseId { get; private set; }

    // Slice 4C: when the customer accepted the insurance terms in force, for an insured shipment.
    public DateTimeOffset? InsuranceAcceptedUtc { get; private set; }

    // Slice 5D: the insurance product chosen; null with insurance: a shipment from before products (the mode's product).
    public Guid? InsurancePlanId { get; private set; }

    // Slice 5D: the clerk who recorded the customer's acceptance at the counter; null: accepted by the customer on the portal.
    public Guid? InsuranceRecordedByUserId { get; private set; }

    // Slice 5D (D-8): the customer's signature at the counter (the file store's key of the image), when, and the clerk present.
    public string? CustomerSignatureKey { get; private set; }

    public DateTimeOffset? CustomerSignedUtc { get; private set; }

    public Guid? CustomerSignatureRecordedByUserId { get; private set; }

    // Signed on screen for its declaration and insurance choice; signed again replaces it. Only while the shipment is open (review of 5D):
    // once booked, the signed page is the one it left with.
    public void RecordSignature(string fileKey, Guid recordedBy, DateTimeOffset utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileKey);
        EnsureOpen();

        (CustomerSignatureKey, CustomerSignedUtc, CustomerSignatureRecordedByUserId) = (fileKey, utcNow, recordedBy);
        MarkAsModified(utcNow);
    }

    // Slice 4D: the service chosen by the customer; staff may change it before dispatch.
    public string? ServiceCode { get; private set; }

    // Slice 4D step 2: the shipment number on its shipping labels (SH...), given once.
    public string? Number { get; private set; }

    // Slice 5C: how many parcels travel in it, as the customer said (portal) or told the clerk (counter); null for a shipment from before.
    public int? ParcelCount { get; private set; }

    // Slice 5C: created by a clerk at the counter; its count is the clerk's to correct until the first parcel is in.
    public bool CreatedAtCounter { get; private set; }

    // Slice 5C: when it became final, and the amount then (the price of its service for its parcels).
    public DateTimeOffset? BookedUtc { get; private set; }

    // Slice 7 step 2 (R-6): someone the customer names to collect it, with the same proof as the recipient.
    public string? CollectorName { get; private set; }

    public string? CollectorPhone { get; private set; }

    // Slice 7 step 2 (R-5): the pickup code sent to the customer, kept protected with the server's key; wrong tries counted (review).
    public const int PickupCodeTries = 5;

    public string? PickupCodeProtected { get; private set; }

    public DateTimeOffset? PickupCodeUtc { get; private set; }

    public int PickupCodeFailures { get; private set; }

    // Collected (the last release at the counter).
    public DateTimeOffset? PickedUpUtc { get; private set; }

    // R-10 (review): when its first parcel became ready for pickup, and the reminders sent since; kept on the shipment.
    public DateTimeOffset? PickupReadyUtc { get; private set; }

    public int PickupRemindersSent { get; private set; }

    // The last bill at the destination's request key: a double click bills once (review).
    public Guid? DestinationBillKey { get; private set; }

    public void ChangeCollector(string? name, string? phone, DateTimeOffset utcNow)
    {
        // A person may collect only what is picked up at a branch or warehouse, once booked, until it is collected (R-6, review).
        if (Status != OperationsShipmentStatus.Booked || PickedUpUtc is not null || DeliveryAddress is not null || (DestinationBranchId is null && DestinationWarehouseId is null))
        {
            throw new DomainException(OperationsShipmentErrors.CollectorLocked);
        }

        (CollectorName, CollectorPhone) = string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(phone)
            ? (null, null)
            : (OperationsShipmentInstructions.Name(name), OperationsShipmentInstructions.Phone(phone));
        MarkAsModified(utcNow);
    }

    public void SetPickupCode(string protectedCode, DateTimeOffset utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(protectedCode);
        (PickupCodeProtected, PickupCodeUtc, PickupCodeFailures) = (protectedCode, utcNow, 0);
        MarkAsModified(utcNow);
    }

    // Every try is counted first, in the database in one statement (review: tries sent at once are all counted); after five, the code no longer works.
    public bool PickupCodeLocked => PickupCodeFailures >= PickupCodeTries;

    // The right code: the tries start again.
    public void PickupCodeAccepted(DateTimeOffset utcNow)
    {
        if (PickupCodeFailures != 0)
        {
            PickupCodeFailures = 0;
            MarkAsModified(utcNow);
        }
    }

    public void MarkPickupReady(DateTimeOffset utcNow)
    {
        if (PickupReadyUtc is null)
        {
            PickupReadyUtc = utcNow;
            MarkAsModified(utcNow);
        }
    }

    public bool PickupRemind(int sent, DateTimeOffset utcNow)
    {
        if (PickupRemindersSent != sent - 1 || sent > Vixly.Operations.Domain.Holds.OperationsHoldLimits.ReminderDays.Count)
        {
            return false;
        }

        PickupRemindersSent = sent;
        MarkAsModified(utcNow);
        return true;
    }

    public void BilledAtDestination(Guid? requestKey, DateTimeOffset utcNow)
    {
        DestinationBillKey = requestKey;
        MarkAsModified(utcNow);
    }

    public void PickedUp(DateTimeOffset utcNow)
    {
        PickedUpUtc = utcNow;
        MarkAsModified(utcNow);
    }

    // Slice 7 (R-1): the customer was told it is ready for pickup (once).
    public DateTimeOffset? PickupNotifiedUtc { get; private set; }

    public bool NotePickupNotified(DateTimeOffset utcNow)
    {
        if (PickupNotifiedUtc is not null)
        {
            return false;
        }

        PickupNotifiedUtc = utcNow;
        MarkAsModified(utcNow);
        return true;
    }

    public decimal? BookedTotal { get; private set; }

    public string? BookedCurrencyCode { get; private set; }

    public const int MaxParcelCount = 99;

    public bool IsBooked => Status == OperationsShipmentStatus.Booked;

    // The parcel count: 1 to 99, never below the parcels already in it.
    public void DeclareParcelCount(int count, int entered, DateTimeOffset utcNow)
    {
        EnsureOpen();

        if (count is < 1 or > MaxParcelCount)
        {
            throw new DomainException(OperationsShipmentErrors.ParcelCountInvalid);
        }

        if (count < entered)
        {
            throw new DomainException(OperationsShipmentErrors.ParcelCountBelowEntered);
        }

        if (ParcelCount != count)
        {
            ParcelCount = count;
            MarkAsModified(utcNow);
        }
    }

    public void OpenedAtCounter() => CreatedAtCounter = true;

    // All its parcels are in, measured and clear: final, with its amount; nothing changes after this (owner, 2026-10-20).
    public void Book(decimal total, string currencyCode, DateTimeOffset utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode);
        EnsureOpen();

        if (!HasInstructions || ServiceCode is null || ParcelCount is null || total < 0m)
        {
            throw new DomainException(OperationsShipmentErrors.NotReadyToBook);
        }

        (Status, BookedUtc, BookedTotal, BookedCurrencyCode) = (OperationsShipmentStatus.Booked, utcNow, decimal.Round(total, 2), currencyCode);
        MarkAsModified(utcNow);
    }

    public void Numbered(string number)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number);
        Number ??= number;
    }

    // Slice 4D: the reference of the customer's form that created it, so a double click creates one shipment (ADR-0019).
    public string? RequestKey { get; private set; }

    public void Keyed(string? requestKey)
    {
        RequestKey = string.IsNullOrWhiteSpace(requestKey) ? null : requestKey.Trim();
    }

    // Slice 4D: the shipment a package waits in until the customer puts it in one of its shipments (a parcel always belongs to a shipment).
    public bool IsWaiting => !HasInstructions;

    public bool HasInstructions => DestinationCountryCode is not null;

    // A shipment with no instructions yet, for a package that arrived before the customer said where it goes.
    public static OperationsShipment Open(Guid tenantId, Guid customerId, Guid originWarehouseId, DateTimeOffset utcNow) =>
        new(tenantId, utcNow) { CustomerId = customerId, OriginWarehouseId = originWarehouseId, Status = OperationsShipmentStatus.Open };

    // The customer's own shipment: where it goes, who receives it and the options, before any package arrives.
    public static OperationsShipment Request(Guid tenantId, Guid customerId, Guid originWarehouseId, OperationsShipmentInstructions instructions, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(instructions);

        if (customerId == Guid.Empty || originWarehouseId == Guid.Empty)
        {
            throw new DomainException(OperationsShipmentErrors.OriginInvalid);
        }

        OperationsShipment shipment = Open(tenantId, customerId, originWarehouseId, utcNow);
        shipment.Apply(instructions, utcNow);
        return shipment;
    }

    // Changed by the customer while the shipment is still open.
    public void ChangeInstructions(OperationsShipmentInstructions instructions, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(instructions);
        EnsureOpen();
        object before = Signed();
        Apply(instructions, utcNow);

        if (!before.Equals(Signed()))
        {
            ClearSignature();
        }

        MarkAsModified(utcNow);
    }

    // What the customer signs for (the declaration page): where it goes, to whom, the service, the options and the insurance product.
    private object Signed() => (DestinationBranchId,
        (DeliveryAddress?.Line1, DeliveryAddress?.Line2, DeliveryAddress?.City, DeliveryAddress?.Region, DeliveryAddress?.PostalCode, DeliveryAddress?.CountryCode), DestinationCountryCode, DestinationWarehouseId, RecipientName, RecipientPhone, Options, ServiceCode, InsurancePlanId);

    // A change to what the customer signed for makes the signature stale: the counter asks for it again (review of 5D).
    private void ClearSignature() => (CustomerSignatureKey, CustomerSignedUtc, CustomerSignatureRecordedByUserId) = (null, null, null);

    // A package announced in it or a parcel joining it changes the row, so a cancel made at the same moment clashes on its row version (review of 3C step 1).
    // Slice 5D: a new parcel changes the declaration, so a signature given before it is stale.
    public void TakePackage(DateTimeOffset utcNow)
    {
        EnsureOpen();
        ClearSignature();
        MarkAsModified(utcNow);
    }

    // Slice 5E: its supplies changed at the counter; the row version makes a booking at the same moment clash (review of 5E). Never on a final shipment.
    public void SuppliesChanged(DateTimeOffset utcNow)
    {
        EnsureOpen();
        MarkAsModified(utcNow);
    }

    // Slice 5B: a certificate was issued for the shipment when first asked for; the row version guards it against a change at the same moment.
    public void InsuranceCertified(DateTimeOffset utcNow) => MarkAsModified(utcNow);

    // Slice 5A: staff change the service before the shipment leaves (Q-D1); the use case checks it is priced on the route.
    public void ChangeService(string serviceCode, DateTimeOffset utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceCode);
        EnsureOpen();

        if (IsWaiting)
        {
            throw new DomainException(OperationsShipmentErrors.Waiting);
        }

        if (ServiceCode != serviceCode.Trim())
        {
            ClearSignature();
        }

        ServiceCode = serviceCode.Trim();
        MarkAsModified(utcNow);
    }

    // Cancelled by the customer before any of its packages arrived (the use case checks there are none).
    public void Cancel(DateTimeOffset utcNow)
    {
        EnsureOpen();
        Status = OperationsShipmentStatus.Cancelled;
        MarkAsModified(utcNow);
    }

    private void Apply(OperationsShipmentInstructions instructions, DateTimeOffset utcNow)
    {
        (DestinationBranchId, DeliveryAddress, DestinationCountryCode, DestinationWarehouseId) = (instructions.BranchId, instructions.Address, instructions.CountryCode, instructions.WarehouseId);
        (RecipientName, RecipientPhone, Options, ServiceCode) = (instructions.RecipientName, instructions.RecipientPhone, instructions.Options, instructions.ServiceCode);

        // Insured: the time the terms were accepted, renewed with each acceptance; not insured: no acceptance.
        InsuranceAcceptedUtc = !instructions.Options.HasFlag(OperationsShipmentOptions.Insured) ? null : instructions.TermsAccepted ? utcNow : InsuranceAcceptedUtc;
        InsuranceRecordedByUserId = !instructions.Options.HasFlag(OperationsShipmentOptions.Insured) ? null : instructions.TermsAccepted ? instructions.AcceptanceRecordedBy : InsuranceRecordedByUserId;
        InsurancePlanId = instructions.Options.HasFlag(OperationsShipmentOptions.Insured) ? instructions.InsurancePlanId ?? InsurancePlanId : null;
    }

    // Slice 5D: insure with another product, or not at all, on an open shipment; the customer's acceptance is required to insure.
    public void ChangeInsurance(Guid? planId, bool accepted, Guid? recordedBy, DateTimeOffset utcNow)
    {
        EnsureOpen();

        if (planId != InsurancePlanId || (planId is null) == Options.HasFlag(OperationsShipmentOptions.Insured))
        {
            ClearSignature();
        }

        if (planId is null)
        {
            (Options, InsurancePlanId, InsuranceAcceptedUtc, InsuranceRecordedByUserId) = (Options & ~OperationsShipmentOptions.Insured, null, null, null);
        }
        else if (!accepted)
        {
            throw new DomainException(OperationsShipmentErrors.InsuranceTermsRequired);
        }
        else
        {
            (Options, InsurancePlanId, InsuranceAcceptedUtc, InsuranceRecordedByUserId) = (Options | OperationsShipmentOptions.Insured, planId, utcNow, recordedBy);
        }

        MarkAsModified(utcNow);
    }

    // Slice 6A (D-4): how this consolidated shipment travels when the ship manager switched it; none: the company's way.
    public Vixly.Operations.Domain.Bundles.OperationsConsolidationWay? ConsolidationWay { get; private set; }

    public Vixly.Operations.Domain.Bundles.OperationsConsolidationWay WayOr(Vixly.Operations.Domain.Bundles.OperationsConsolidationWay companyWay) => ConsolidationWay ?? companyWay;

    // Switched for a consolidated shipment, before any of its parcels is boxed or bundled (the use case checks that); never on a cancelled one.
    public void ChooseConsolidation(Vixly.Operations.Domain.Bundles.OperationsConsolidationWay way, DateTimeOffset utcNow)
    {
        if (!Options.HasFlag(OperationsShipmentOptions.Consolidate) || Status == OperationsShipmentStatus.Cancelled || !Enum.IsDefined(way))
        {
            throw new DomainException(Vixly.Operations.Domain.Bundles.OperationsBundleErrors.NotConsolidated);
        }

        ConsolidationWay = way;
        MarkAsModified(utcNow);
    }

    // Slice 6A (review): when its first parcel is boxed or bundled, the way in effect is kept on the shipment, so a later change of the company's way never strands it;
    // touching the row makes two scans of its parcels at the same moment clash on its row version.
    public void StartConsolidating(Vixly.Operations.Domain.Bundles.OperationsConsolidationWay companyWay, DateTimeOffset utcNow)
    {
        if (!Options.HasFlag(OperationsShipmentOptions.Consolidate))
        {
            return;
        }

        ConsolidationWay ??= companyWay;
        MarkAsModified(utcNow);
    }

    public void EnsureOpen()
    {
        if (Status != OperationsShipmentStatus.Open)
        {
            throw new DomainException(OperationsReceivingErrors.ShipmentNotOpen);
        }
    }
}
