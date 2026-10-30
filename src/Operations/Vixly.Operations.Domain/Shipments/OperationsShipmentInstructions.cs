using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Domain.Shipments;

// =========================================================
// FILE: OperationsShipmentInstructions.cs
//
// PURPOSE:
// Where a customer's shipment goes and who receives it, as the customer writes it (owner, 2026-10-18).
//
// RESPONSIBILITIES:
// - Pickup at a destination branch, or delivery to an address
// - The recipient's name and phone; the options asked for
// - The destination country, which inspection rules use from the first check
//
// IMPORTANT NOTES:
// - Written by the customer only (D-C6); checked here, never trusted from the form
// =========================================================
public sealed class OperationsShipmentInstructions
{
    public const int RecipientMaxLength = 100;

    public const int PhoneMaxLength = 20;

    private OperationsShipmentInstructions(Guid? branchId, OperationsPostalAddress? address, string countryCode, string recipientName, string recipientPhone, OperationsShipmentOptions options,
        Guid? warehouseId = null)
    {
        (BranchId, Address, CountryCode, RecipientName, RecipientPhone, Options, WarehouseId) = (branchId, address, countryCode, recipientName, recipientPhone, options, warehouseId);
    }

    // Slice 4C: the destination warehouse the customer chose.
    public Guid? WarehouseId { get; }

    public Guid? BranchId { get; }

    public OperationsPostalAddress? Address { get; }

    public string CountryCode { get; }

    public string RecipientName { get; }

    public string RecipientPhone { get; }

    public OperationsShipmentOptions Options { get; }

    // Slice 4C: the customer accepted the insurance terms with these instructions (each time it asks for insurance).
    public bool TermsAccepted { get; private set; }

    // Slice 4D: the service the customer chose (air, ocean, ground ...); none lets the company choose.
    public string? ServiceCode { get; private set; }

    public OperationsShipmentInstructions ForService(string? serviceCode)
    {
        ServiceCode = string.IsNullOrWhiteSpace(serviceCode) ? null : serviceCode.Trim();
        return this;
    }

    // Slice 5D: the insurance product chosen; kept only when insured.
    public Guid? InsurancePlanId { get; private set; }

    // Slice 5D: at the counter the customer accepts and the clerk records it; null: the customer on the portal.
    public Guid? AcceptanceRecordedBy { get; private set; }

    public OperationsShipmentInstructions ForInsurance(Guid? planId)
    {
        InsurancePlanId = Options.HasFlag(OperationsShipmentOptions.Insured) ? planId : null;
        return this;
    }

    public OperationsShipmentInstructions AcceptTerms(Guid? recordedBy = null)
    {
        TermsAccepted = Options.HasFlag(OperationsShipmentOptions.Insured);
        AcceptanceRecordedBy = TermsAccepted ? recordedBy : null;
        return this;
    }

    // Collected at one of the company's destination branches.
    public static OperationsShipmentInstructions Pickup(OperationsBranch branch, string? recipientName, string? recipientPhone, OperationsShipmentOptions options)
    {
        ArgumentNullException.ThrowIfNull(branch);

        if (branch.Status != OperationsLocationStatus.Active || branch.Side == OperationsBranchSide.Origin)
        {
            throw new DomainException(OperationsShipmentErrors.DestinationInvalid);
        }

        return new(branch.Id, null, branch.Address.CountryCode, Name(recipientName), Phone(recipientPhone), Checked(options));
    }

    // Slice 4C: collected at the destination warehouse the customer chose, where it is a pickup point.
    public static OperationsShipmentInstructions Pickup(OperationsWarehouse destination, string? recipientName, string? recipientPhone, OperationsShipmentOptions options)
    {
        ArgumentNullException.ThrowIfNull(destination);

        if (!destination.Offers(OperationsDeliveryModes.Pickup))
        {
            throw new DomainException(OperationsShipmentErrors.DestinationInvalid);
        }

        return new(destination.BranchId, null, destination.Address.CountryCode, Name(recipientName), Phone(recipientPhone), Checked(options), destination.Id);
    }

    // Slice 4C: delivered from the destination warehouse the customer chose to an address in its area.
    public static OperationsShipmentInstructions HomeDelivery(OperationsWarehouse destination, OperationsPostalAddress address, string? recipientName, string? recipientPhone,
        OperationsShipmentOptions options)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(address);

        if (!destination.Offers(OperationsDeliveryModes.HomeDelivery) || destination.Address.CountryCode != address.CountryCode || !destination.Covers(address.City))
        {
            throw new DomainException(OperationsShipmentErrors.DestinationInvalid);
        }

        return new(null, address, address.CountryCode, Name(recipientName), Phone(recipientPhone), Checked(options), destination.Id);
    }

    // Delivered to the recipient's door.
    public static OperationsShipmentInstructions HomeDelivery(OperationsPostalAddress address, string? recipientName, string? recipientPhone, OperationsShipmentOptions options)
    {
        ArgumentNullException.ThrowIfNull(address);
        return new(null, address, address.CountryCode, Name(recipientName), Phone(recipientPhone), Checked(options));
    }

    internal static string Name(string? value)
    {
        string text = value?.Trim() ?? string.Empty;

        return text.Length is 0 or > RecipientMaxLength || text.Any(char.IsControl) || text.IndexOfAny(['<', '>']) >= 0 || text.Contains("://", StringComparison.Ordinal)
            ? throw new DomainException(OperationsShipmentErrors.RecipientInvalid)
            : text;
    }

    internal static string Phone(string? value) =>
        PhoneNumber.TryCreate(value, out PhoneNumber? phone) && phone.Value.StartsWith('+') && phone.Value.Length <= PhoneMaxLength
            ? phone.Value
            : throw new DomainException(OperationsShipmentErrors.RecipientPhoneInvalid);

    private static OperationsShipmentOptions Checked(OperationsShipmentOptions options) =>
        (options & ~(OperationsShipmentOptions.Consolidate | OperationsShipmentOptions.Refrigerated | OperationsShipmentOptions.Insured | OperationsShipmentOptions.FreightCollect))
            == OperationsShipmentOptions.None
            ? options
            : throw new DomainException(OperationsShipmentErrors.DestinationInvalid);
}
