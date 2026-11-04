namespace Vixly.Operations.Domain.Identity;

// =========================================================
// FILE: OperationsSignInPlace.cs
//
// PURPOSE:
// Where a sign-in happens: the licensed device and the country
// (owner, 2026-10-21: member activity; a device signing in from
// another country than the one it was registered in is flagged
// and must confirm with a second factor).
//
// RESPONSIBILITIES:
// - Name the device as registered, and the country now
// - Say whether the country differs from the device's own
//
// IMPORTANT NOTES:
// - Countries are Cloudflare's two-letter codes. A device with no
//   country of its own (registered before countries were kept) is
//   never flagged; once it has one, any other code is a change,
//   Cloudflare's unknown (XX) and Tor (T1) included (review). No
//   header at all (not through Cloudflare) tells nothing
// - The device is the one whose cookie Platform verified; none
//   when the browser has none
// =========================================================
public sealed record OperationsSignInPlace
{
    public OperationsSignInPlace(Guid? deviceId, string? deviceName, string? deviceNumber, string? registeredCountry, string? country)
    {
        DeviceId = deviceId;
        DeviceName = OperationsIdentityText.Clip(deviceName, OperationsIdentityLimits.DeviceNameMaxLength);
        DeviceNumber = OperationsIdentityText.Clip(deviceNumber, OperationsIdentityLimits.DeviceNumberMaxLength);
        Country = Code(country);
        RegisteredCountry = Code(registeredCountry);
        CountryChanged = deviceId is not null && RegisteredCountry is not null && Country is not null && Country != RegisteredCountry;
    }

    public static OperationsSignInPlace None { get; } = new(null, null, null, null, null);

    public Guid? DeviceId { get; }

    public string? DeviceName { get; }

    public string? DeviceNumber { get; }

    public string? Country { get; }

    public string? RegisteredCountry { get; }

    public bool CountryChanged { get; private init; }

    // As kept on a pending sign-in: the change was decided when the password was checked.
    public static OperationsSignInPlace Kept(Guid? deviceId, string? deviceName, string? deviceNumber, string? country, bool countryChanged) =>
        new OperationsSignInPlace(deviceId, deviceName, deviceNumber, null, country) with { CountryChanged = countryChanged };

    // Without a device, the country alone (an invitation accepted, a browser not yet registered).
    public static OperationsSignInPlace Nowhere(string? country) => new(null, null, null, null, country);

    private static string? Code(string? value)
    {
        string? text = value?.Trim().ToUpperInvariant();
        return text is { Length: 2 } && text.All(char.IsAsciiLetterOrDigit) ? text : null;
    }
}
