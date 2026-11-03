using Vixly.Operations.Domain.Common;

namespace Vixly.Operations.Domain.Identity;

// =========================================================
// FILE: OperationsDeviceBlock.cs
//
// PURPOSE:
// A licensed device blocked after it was used from another country than its own (owner, R-6, 2026-10-21).
//
// RESPONSIBILITIES:
// - Record which device, from where, when and for whom it was blocked
// - Lift the block once, by a company administrator
//
// IMPORTANT NOTES:
// - Owner's words: "block sign-in until company system admin unblock them"; no code or email lets the person through
// - One open block per device (unique filtered index); a lifted block stays as history
// - Bound to its tenant by OperationsEntity
// =========================================================
public sealed class OperationsDeviceBlock : OperationsEntity
{
    private OperationsDeviceBlock()
    {
        RegisteredCountry = string.Empty;
        Country = string.Empty;
    }

    private OperationsDeviceBlock(Guid tenantId, Guid deviceId, string? deviceName, string? deviceNumber, string registeredCountry, string country, Guid? userId,
        DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        DeviceId = deviceId;
        DeviceName = deviceName;
        DeviceNumber = deviceNumber;
        RegisteredCountry = registeredCountry;
        Country = country;
        UserId = userId;
    }

    public Guid DeviceId { get; private set; }

    public string? DeviceName { get; private set; }

    public string? DeviceNumber { get; private set; }

    // The country the device was registered in, and the one it was used from.
    public string RegisteredCountry { get; private set; }

    public string Country { get; private set; }

    // The staff member whose sign-in or session was refused (none when unknown).
    public Guid? UserId { get; private set; }

    public DateTimeOffset? UnblockedUtc { get; private set; }

    public Guid? UnblockedByUserId { get; private set; }

    public bool IsOpen => UnblockedUtc is null;

    public static OperationsDeviceBlock Block(Guid tenantId, Guid deviceId, string? deviceName, string? deviceNumber, string registeredCountry, string country,
        Guid? userId, DateTimeOffset utcNow)
    {
        if (deviceId == Guid.Empty)
        {
            throw new ArgumentException("A device is required.", nameof(deviceId));
        }

        return new OperationsDeviceBlock(tenantId, deviceId, Trim(deviceName, OperationsIdentityLimits.DeviceNameMaxLength),
            Trim(deviceNumber, OperationsIdentityLimits.DeviceNumberMaxLength), Code(registeredCountry), Code(country), userId, utcNow);
    }

    public bool Unblock(Guid byUserId, DateTimeOffset utcNow)
    {
        if (!IsOpen)
        {
            return false;
        }

        UnblockedUtc = utcNow;
        UnblockedByUserId = byUserId;
        MarkAsModified(utcNow);
        return true;
    }

    private static string? Trim(string? value, int max) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().Length <= max ? value.Trim() : value.Trim()[..max];

    private static string Code(string? country) =>
        string.IsNullOrWhiteSpace(country) ? "XX" : country.Trim().ToUpperInvariant()[..Math.Min(2, country.Trim().Length)];
}
