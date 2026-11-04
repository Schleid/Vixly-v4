using Vixly.Operations.Domain.Common;

namespace Vixly.Operations.Domain.Identity;

// =========================================================
// FILE: OperationsStaffPin.cs
//
// PURPOSE:
// A staff member's 4-digit PIN for signing in on a licensed device (Slice 12A, owner H-8: "badge or a 4-digit PIN").
//
// RESPONSIBILITIES:
// - Keep the PIN's hash, set by the person after a full sign-in
// - Count wrong PINs and lock it after five, until the next full sign-in
//
// IMPORTANT NOTES:
// - The hash is the password hasher's (BCrypt), never the PIN
// - One row per person (unique index)
// - Bound to its tenant by OperationsEntity
// =========================================================
public sealed class OperationsStaffPin : OperationsEntity
{
    public const int Length = 4;

    public const int MaxFailures = 5;

    private OperationsStaffPin()
    {
        PinHash = string.Empty;
    }

    private OperationsStaffPin(Guid tenantId, Guid userId, string pinHash, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        UserId = userId;
        PinHash = pinHash;
    }

    public Guid UserId { get; private set; }

    public string PinHash { get; private set; }

    public int Failures { get; private set; }

    public bool IsLocked => Failures >= MaxFailures;

    // Four digits, nothing else.
    public static bool IsWellFormed(string? pin) => pin is { Length: Length } && pin.All(char.IsAsciiDigit);

    public static OperationsStaffPin Set(Guid tenantId, Guid userId, string pinHash, DateTimeOffset utcNow)
    {
        if (userId == Guid.Empty)
        {
            throw new Vixly.Shared.Kernel.Domain.DomainException(OperationsIdentityErrors.UserRequired);
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(pinHash);
        return new OperationsStaffPin(tenantId, userId, pinHash, utcNow);
    }

    public void Change(string pinHash, DateTimeOffset utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pinHash);
        PinHash = pinHash;
        Failures = 0;
        MarkAsModified(utcNow);
    }

    // Returns true when this failure locked the PIN.
    public bool RecordFailure(DateTimeOffset utcNow)
    {
        if (IsLocked)
        {
            return false;
        }

        Failures++;
        MarkAsModified(utcNow);
        return IsLocked;
    }

    public void Succeeded(DateTimeOffset utcNow)
    {
        if (Failures != 0)
        {
            Failures = 0;
            MarkAsModified(utcNow);
        }
    }

    // A full sign-in (password and second factor) unlocks the PIN.
    public void Unlock(DateTimeOffset utcNow) => Succeeded(utcNow);
}
