using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Domain.Shipments;

// =========================================================
// FILE: OperationsShipmentErrors.cs
//
// PURPOSE:
// The refusals of customer shipments (Slice 3C, owner 2026-10-18).
//
// RESPONSIBILITIES:
// - Name each refusal by a code; the texts are in the plane resources
//
// IMPORTANT NOTES:
// - Codes never change once released
// =========================================================
public static class OperationsShipmentErrors
{
    public static Error NotFound { get; } = Error.Create("ops.shipments.not_found");

    public static Error DestinationInvalid { get; } = Error.Create("ops.shipments.destination_invalid");

    public static Error DeliveryModeUnavailable { get; } = Error.Create("ops.shipments.delivery_mode_unavailable");

    public static Error OriginInvalid { get; } = Error.Create("ops.shipments.origin_invalid");

    public static Error RecipientInvalid { get; } = Error.Create("ops.shipments.recipient_invalid");

    public static Error RecipientPhoneInvalid { get; } = Error.Create("ops.shipments.recipient_phone_invalid");

    public static Error HasParcels { get; } = Error.Create("ops.shipments.has_parcels");

    public static Error InstructionsRequired { get; } = Error.Create("ops.shipments.instructions_required");

    public static Error ParcelElsewhere { get; } = Error.Create("ops.shipments.parcel_elsewhere");

    // Slice 4C: insured only once the customer accepted the insurance terms.
    public static Error InsuranceTermsRequired { get; } = Error.Create("ops.shipments.insurance_terms_required");

    // Slice 5D: the signature is not a small PNG image.
    public static Error SignatureInvalid { get; } = Error.Create("ops.shipments.signature_invalid");

    // Slice 5D: insured, but with which of the company's insurance products.
    public static Error InsuranceChoiceRequired { get; } = Error.Create("ops.shipments.insurance_choice_required");

    // Slice 4D: a service the company does not price for the customer.
    public static Error ServiceInvalid { get; } = Error.Create("ops.shipments.service_invalid");

    // Slice 4D: the packages of the waiting shipment go into the customer's shipments; it is never sent itself.
    public static Error Waiting { get; } = Error.Create("ops.shipments.waiting");

    // Slice 5A: documents only for a shipment with where it goes and at least one parcel.
    public static Error NoDocuments { get; } = Error.Create("ops.shipments.no_documents");

    // Slice 5A: the service changes only while every parcel is still at the origin.
    public static Error Departed { get; } = Error.Create("ops.shipments.departed");

    // Slice 5B (review): the customer accepted the insurance terms of one mode; another mode needs the customer to accept its terms.
    public static Error InsuredModeChange { get; } = Error.Create("ops.shipments.insured_mode_change");

    // Slice 5C: the parcel count (1 to 99), never below the parcels already in it; the count reached; final shipments are locked.
    public static Error ParcelCountInvalid { get; } = Error.Create("ops.shipments.parcel_count_invalid");

    public static Error ParcelCountBelowEntered { get; } = Error.Create("ops.shipments.parcel_count_below");

    public static Error ParcelCountReached { get; } = Error.Create("ops.shipments.parcel_count_reached");

    public static Error ParcelCountLocked { get; } = Error.Create("ops.shipments.parcel_count_locked");

    public static Error NotReadyToBook { get; } = Error.Create("ops.shipments.not_ready");

    public static Error Booked { get; } = Error.Create("ops.shipments.booked");

    // Slice 7 step 2 (R-6): who may collect is named once the shipment is booked, until it is collected.
    public static Error CollectorLocked { get; } = Error.Create("ops.shipments.collector_locked");

    // Freight collect only when the company allows it (owner, 2026-10-21).
    public static Error CollectUnavailable { get; } = Error.Create("ops.shipments.collect_unavailable");
}
