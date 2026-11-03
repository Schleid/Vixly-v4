using Vixly.Operations.Domain.Settings;

namespace Vixly.Operations.Application.UseCases.Settings.UpdateOperationsTenantSettings;

// =========================================================
// FILE: UpdateOperationsTenantSettingsRequest.cs
//
// PURPOSE:
// Saves the tenant's settings (Slice 1E).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class UpdateOperationsTenantSettingsRequest
{
    public string CurrencyCode { get; init; } = OperationsTenantSettings.DefaultCurrency;

    public OperationsWeightUnit WeightUnit { get; init; }

    public OperationsDimensionUnit DimensionUnit { get; init; }

    public bool AllowCollect { get; init; }

    // Prices on the public website (owner, 2026-10-21).
    public bool PublicPricing { get; init; }

    public bool AllowCredit { get; init; }

    public OperationsDeliveryModes DeliveryModes { get; init; }

    public bool RequireSecondFactorForAllStaff { get; init; }

    // Require MFA Yes/No (owner, 2026-10-22); null leaves it as it is.
    public bool? RequireMfa { get; init; }

    // Inactivity sign-out in minutes (owner, 2026-10-27); null leaves each as it is.
    public int? StaffIdleMinutes { get; init; }

    public int? CustomerIdleMinutes { get; init; }

    public int? IdleWarnMinutes { get; init; }

    public string ShipmentPrefix { get; init; } = string.Empty;

    public string ReceivingPrefix { get; init; } = string.Empty;

    public string InvoicePrefix { get; init; } = string.Empty;

    public string PaymentPrefix { get; init; } = string.Empty;

    // Null keeps the current prefix.
    public string? CustomerPrefix { get; init; }

    // Null keeps the current parcel prefix (Slice 3B).
    public string? ParcelPrefix { get; init; }

    // Slice 5E: orders of supplies; null keeps the current one.
    public string? SupplyOrderPrefix { get; init; }

    // Slice 5F: an approved credit note; null keeps the current one.
    public string? CreditNotePrefix { get; init; }

    // Slice 6A: bundles and repack boxes; null keeps the current one.
    public string? BundlePrefix { get; init; }

    public string? BoxPrefix { get; init; }

    // Slice 6B.
    public string? DispatchPrefix { get; init; }

    // Owner, 2026-10-28: the two letters of the tracking numbers; null keeps the current ones.
    public string? TrackingPrefix { get; init; }

    // Slice 4B: the company's IANA time zone (null keeps the current one).
    public string? TimeZone { get; init; }
}
