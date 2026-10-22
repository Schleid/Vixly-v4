using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Locations;

// =========================================================
// FILE: OperationsWarehouse.cs
//
// PURPOSE:
// A warehouse of a branch, with its own address and country (Slice 1B).
//
// RESPONSIBILITIES:
// - Hold roles, address, contact, hours and whether it accepts deliveries
// - Be shown on the public site or not
// - Slice 4C: as a destination of the company's routes, offer pickup (its pickup-point role) and/or home delivery in its area, through a hub, run by the company or a partner
//
// IMPORTANT NOTES:
// - A receiving warehouse address becomes the customers mailbox address in Slice 2
// - Code unique per tenant (database)
// =========================================================
public sealed class OperationsWarehouse : OperationsEntity
{
    public const int MaxHomeArea = 100;

    private readonly List<string> _homeArea = new();

    private OperationsWarehouse()
    {
        Code = string.Empty;
        Name = string.Empty;
        Address = null!;
    }

    private OperationsWarehouse(Guid tenantId, DateTimeOffset createdUtc)
        : base(tenantId, createdUtc)
    {
        Code = string.Empty;
        Name = string.Empty;
        Address = null!;
    }

    public Guid BranchId { get; private set; }

    public string Code { get; private set; }

    public string Name { get; private set; }

    public OperationsPostalAddress Address { get; private set; }

    public string? Phone { get; private set; }

    public string? Email { get; private set; }

    public string? OpeningHours { get; private set; }

    public OperationsWarehouseRoles Roles { get; private set; }

    public bool AcceptingDeliveries { get; private set; }

    // The customers' mailbox (owner, 2026-10-21): online purchases and pre-alerts come here; one per company.
    public bool IsMailbox { get; private set; }

    // A mailbox is an active receiving warehouse that accepts deliveries.
    public bool CanBeMailbox => Status == OperationsLocationStatus.Active && AcceptingDeliveries && Roles.HasFlag(OperationsWarehouseRoles.Receiving);

    public OperationsLocationStatus Status { get; private set; }

    public bool ShowOnSite { get; private set; }

    // Slice 4B: an IANA time zone for opening hours and pickup times; null: the company's.
    public string? TimeZone { get; private set; }

    // Slice 4B: the manager is a staff member, never typed contact details that go stale.
    public Guid? ManagerUserId { get; private set; }

    // Slice 4C: as a destination, it also delivers to homes in its area (pickup is its pickup-point role).
    public bool DeliversHome { get; private set; }

    // Slice 7 (R-3): parcels wait for customs clearance here before they are ready.
    public bool ClearsCustoms { get; private set; }

    // Slice 4C: the gateway that serves it (Jérémie through Les Cayes); the route does not change the price.
    public Guid? HubWarehouseId { get; private set; }

    // Slice 4C: the company itself, or a partner operating for it.
    public OperationsDestinationOperator OperatedBy { get; private set; } = OperationsDestinationOperator.Company;

    public string? PartnerName { get; private set; }

    public string? PartnerPhone { get; private set; }

    public string? PartnerEmail { get; private set; }

    // Slice 7 step 4 (R-8, owner 2026-10-21): the company lets this partner take payments (counter, cash on delivery); off by default.
    public bool PartnerHandlesMoney { get; private set; }

    // Money is handled here: always at the company's own warehouses, at a partner's only when the company allows it.
    public bool HandlesMoney => OperatedBy != OperationsDestinationOperator.Partner || PartnerHandlesMoney;

    // Slice 4C: the communes it delivers to at home; empty: anywhere in its country.
    public IReadOnlyList<string> HomeArea => _homeArea;

    public bool IsPublic => ShowOnSite && Status == OperationsLocationStatus.Active;

    // Slice 4C: a destination of the company's routes: in use, with pickup there or home delivery from there.
    public bool IsDestination => Status == OperationsLocationStatus.Active && (Roles.HasFlag(OperationsWarehouseRoles.PickupPoint) || DeliversHome);

    // Slice 4C: an origin, where packages are dropped off.
    public bool IsOrigin => Status == OperationsLocationStatus.Active && Roles.HasFlag(OperationsWarehouseRoles.Receiving);

    public bool Offers(OperationsDeliveryModes mode) => IsDestination && mode switch
    {
        OperationsDeliveryModes.Pickup => Roles.HasFlag(OperationsWarehouseRoles.PickupPoint),
        OperationsDeliveryModes.HomeDelivery => DeliversHome,
        _ => false
    };

    public bool Covers(string? city) => _homeArea.Count == 0 || (city is not null && _homeArea.Any(listed => OperationsPlaceNames.Fold(listed) == OperationsPlaceNames.Fold(city)));

    public static OperationsWarehouse Create(Guid tenantId, Guid branchId, OperationsLocationDetails details, OperationsWarehouseRoles roles, bool acceptingDeliveries, DateTimeOffset utcNow)
    {
        OperationsWarehouse warehouse = new(tenantId, utcNow);
        warehouse.Apply(branchId, details, roles, acceptingDeliveries);
        return warehouse;
    }

    public void Update(Guid branchId, OperationsLocationDetails details, OperationsWarehouseRoles roles, bool acceptingDeliveries, OperationsLocationStatus status, DateTimeOffset utcNow)
    {
        Apply(branchId, details, roles, acceptingDeliveries);
        Status = Enum.IsDefined(status) ? status : OperationsLocationStatus.Active;
        IsMailbox = IsMailbox && CanBeMailbox;
        MarkAsModified(utcNow);
    }

    // Owner, 2026-10-21: the company names one warehouse as its customers' mailbox; the use case clears the others.
    public void SetMailbox(bool mailbox, DateTimeOffset utcNow)
    {
        if (mailbox && !CanBeMailbox)
        {
            throw new DomainException(OperationsLocationErrors.MailboxInvalid);
        }

        IsMailbox = mailbox;
        MarkAsModified(utcNow);
    }

    public void ChangeOperation(string? timeZone, Guid? managerUserId, DateTimeOffset utcNow)
    {
        TimeZone = OperationsTimeZones.Optional(timeZone);
        ManagerUserId = managerUserId == Guid.Empty ? null : managerUserId;
        MarkAsModified(utcNow);
    }

    public void ChangeDestination(OperationsDestinationTerms terms, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(terms);
        List<string> area = (terms.HomeArea ?? Array.Empty<string>()).Select(city => city?.Trim() ?? string.Empty).Where(city => city.Length > 0).ToList();

        if (area.Count > MaxHomeArea || area.Any(city => city.Length > OperationsLocationLimits.CityMaxLength || city.Any(char.IsControl) || city.IndexOfAny(['<', '>']) >= 0)
            || area.Select(OperationsPlaceNames.Fold).Distinct(StringComparer.Ordinal).Count() != area.Count)
        {
            throw new DomainException(OperationsDestinationErrors.AreaInvalid);
        }

        if (terms.HubWarehouseId == Id)
        {
            throw new DomainException(OperationsDestinationErrors.HubInvalid);
        }

        if (!Enum.IsDefined(terms.OperatedBy))
        {
            throw new DomainException(OperationsDestinationErrors.PartnerInvalid);
        }

        bool partner = terms.OperatedBy == OperationsDestinationOperator.Partner;
        string? partnerName = partner
            ? OperationsLocationText.Required(terms.PartnerName, OperationsLocationLimits.NameMaxLength, OperationsDestinationErrors.PartnerInvalid, OperationsDestinationErrors.PartnerInvalid)
            : null;
        DeliversHome = terms.DeliversHome;
        ClearsCustoms = terms.ClearsCustoms;
        HubWarehouseId = terms.HubWarehouseId == Guid.Empty ? null : terms.HubWarehouseId;
        OperatedBy = terms.OperatedBy;
        PartnerName = partnerName;
        PartnerPhone = partner ? OperationsLocationText.Phone(terms.PartnerPhone) : null;
        PartnerEmail = partner ? OperationsLocationText.Email(terms.PartnerEmail) : null;
        PartnerHandlesMoney = partner && terms.PartnerHandlesMoney;
        _homeArea.Clear();
        _homeArea.AddRange(terms.DeliversHome ? area : Array.Empty<string>());
        MarkAsModified(utcNow);
    }

    private void Apply(Guid branchId, OperationsLocationDetails details, OperationsWarehouseRoles roles, bool acceptingDeliveries)
    {
        ArgumentNullException.ThrowIfNull(details);
        ArgumentNullException.ThrowIfNull(details.Address);

        BranchId = branchId == Guid.Empty ? throw new DomainException(OperationsLocationErrors.BranchRequired) : branchId;
        Roles = (roles & (OperationsWarehouseRoles.Receiving | OperationsWarehouseRoles.DispatchHub | OperationsWarehouseRoles.PickupPoint | OperationsWarehouseRoles.Storage)) is var known && known != OperationsWarehouseRoles.None
            ? known
            : throw new DomainException(OperationsLocationErrors.RoleRequired);
        Code = OperationsLocationText.Code(details.Code);
        Name = OperationsLocationText.Required(details.Name, OperationsLocationLimits.NameMaxLength, OperationsLocationErrors.NameRequired, OperationsLocationErrors.NameTooLong);
        Address = details.Address;
        Phone = OperationsLocationText.Phone(details.Phone);
        Email = OperationsLocationText.Email(details.Email);
        OpeningHours = OperationsLocationText.Optional(details.OpeningHours, OperationsLocationLimits.HoursMaxLength, OperationsLocationErrors.HoursTooLong);
        AcceptingDeliveries = acceptingDeliveries && Roles.HasFlag(OperationsWarehouseRoles.Receiving);
        ShowOnSite = details.ShowOnSite;
    }
}
