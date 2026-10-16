namespace Vixly.Platform.Application.Payments;

// =========================================================
// FILE: PlatformCardMetadata.cs
//
// PURPOSE:
// The metadata keys Vixly puts on provider objects to find its own records again.
//
// RESPONSIBILITIES:
// - Carry the metadata keys Vixly puts on provider objects to find its own records again
//
// IMPORTANT NOTES:
// - Never personal data beyond the consenting email
// =========================================================
public static class PlatformCardMetadata
{
    public const string Transaction = "vx_transaction";

    public const string Link = "vx_link";

    public const string Tenant = "vx_tenant";

    public const string Invoice = "vx_invoice";

    public const string SaveCard = "vx_save_card";

    public const string AutoCharge = "vx_auto_charge";

    public const string ConsentEmail = "vx_consent_email";

    public const string ConsentVersion = "vx_consent_version";

    public const string AddedVia = "vx_added_via";

    // The outbox stream of provider events that name no tenant.
    public static readonly Guid ProviderStream = Guid.Parse("0199f0c0-0000-7000-8000-0000000000c5");

    public static Guid? Id(IReadOnlyDictionary<string, string> metadata, string key)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        return metadata.TryGetValue(key, out string? value) && Guid.TryParse(value, out Guid id) ? id : null;
    }

    public static bool Flag(IReadOnlyDictionary<string, string> metadata, string key)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        return metadata.TryGetValue(key, out string? value) && value == "true";
    }
}
