using System.Globalization;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Licensing;

// =========================================================
// FILE: PlatformDevice.cs
//
// PURPOSE:
// A tenant device and its seat: joining, approval, proof, release and revocation (D-L3 to D-L6).
//
// RESPONSIBILITIES:
// - Enrol with a public key or a secret hash; approve or deny
// - Check a rotating secret and rotate it; spot a reused old secret
// - Release within the allowance, ask Vixly over it, revoke at once when lost
// - Say whether it holds a seat
//
// IMPORTANT NOTES:
// - Final states never come back: a returning device enrols again
// - The caller applies the seat change to PlatformTenantLicensing in the same commit
// - Row version: two hosts never change one device at once
// =========================================================
public sealed class PlatformDevice : AuditableEntity
{
    private PlatformDevice()
    {
        Number = string.Empty;
        Name = string.Empty;
    }

    private PlatformDevice(Guid tenantId, string number, string name, PlatformDeviceKind kind, PlatformDeviceProof proof, string? publicKey, string? secretHash, PlatformDeviceActor actor, DateTimeOffset utcNow)
        : base(utcNow)
    {
        PlatformTenantId = tenantId;
        Number = number;
        Name = name;
        Kind = kind;
        Proof = proof;
        PublicKey = publicKey;
        SecretHash = secretHash;
        SecretIssuedUtc = secretHash is null ? null : utcNow;
        Status = PlatformDeviceStatus.Pending;
        RequestedByReference = actor.Reference;
        RequestedByName = actor.Name;
    }

    public Guid PlatformTenantId { get; private set; }

    public string Number { get; private set; }

    public string Name { get; private set; }

    public PlatformDeviceKind Kind { get; private set; }

    public PlatformDeviceProof Proof { get; private set; }

    public string? PublicKey { get; private set; }

    public string? SecretHash { get; private set; }

    public DateTimeOffset? SecretIssuedUtc { get; private set; }

    public string? PreviousSecretHash { get; private set; }

    public DateTimeOffset? PreviousSecretValidUntilUtc { get; private set; }

    public PlatformDeviceStatus Status { get; private set; }

    // Revoked, but its seat is held until Vixly staff approve the release (over the allowance).
    public bool SeatHeld { get; private set; }

    public PlatformDeviceReason? Reason { get; private set; }

    public string? RequestedByReference { get; private set; }

    public string? RequestedByName { get; private set; }

    public DateTimeOffset? ApprovedUtc { get; private set; }

    public DateTimeOffset? ReleaseRequestedUtc { get; private set; }

    public DateTimeOffset? EndedUtc { get; private set; }

    // Ended by the tenant (release or lost-device revoke): counts against the release allowance.
    public bool EndedByTenant { get; private set; }

    public DateTimeOffset? LastSeenUtc { get; private set; }

    public bool HoldsSeat => Status is PlatformDeviceStatus.Active or PlatformDeviceStatus.ReleasePending || (Status == PlatformDeviceStatus.Revoked && SeatHeld);

    public bool AwaitsReview => Status == PlatformDeviceStatus.ReleasePending || (Status == PlatformDeviceStatus.Revoked && SeatHeld);

    public static (PlatformDevice Device, PlatformDeviceEvent Event) Enrol(
        Guid tenantId,
        string number,
        string? name,
        PlatformDeviceKind kind,
        string? publicKey,
        string? secretHash,
        PlatformDeviceActor actor,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(actor);
        string text = name?.Trim() ?? string.Empty;

        if (text.Length == 0 || text.Length > PlatformLicensingLimits.NameMaxLength)
        {
            throw new DomainException(PlatformLicensingErrors.NameInvalid);
        }

        if (!Enum.IsDefined(kind))
        {
            throw new DomainException(PlatformLicensingErrors.KindInvalid);
        }

        bool hasKey = !string.IsNullOrWhiteSpace(publicKey);
        bool hasSecret = !string.IsNullOrWhiteSpace(secretHash);

        if (hasKey == hasSecret || (hasKey && publicKey!.Length > PlatformLicensingLimits.PublicKeyMaxLength)
            || (hasSecret && secretHash!.Length != PlatformLicensingLimits.HashLength) || tenantId == Guid.Empty)
        {
            throw new DomainException(PlatformLicensingErrors.PublicKeyInvalid);
        }

        PlatformDevice device = new(tenantId, number, text, kind, hasKey ? PlatformDeviceProof.KeyPair : PlatformDeviceProof.Secret, hasKey ? publicKey!.Trim() : null, hasSecret ? secretHash : null, actor, utcNow);
        return (device, device.Record(PlatformDeviceEventCodes.Requested, actor, utcNow, arguments: Args(("kind", kind.ToString()), ("proof", device.Proof.ToString()))));
    }

    // =====================================================
    // METHOD: APPROVAL (D-L4)
    // =====================================================
    public PlatformDeviceChange Approve(PlatformDeviceActor actor, DateTimeOffset utcNow, string eventCode = PlatformDeviceEventCodes.Approved)
    {
        Require(PlatformDeviceStatus.Pending);
        Status = PlatformDeviceStatus.Active;
        ApprovedUtc = utcNow;
        MarkAsModified(utcNow);
        return new PlatformDeviceChange(Record(eventCode, actor, utcNow), true, false);
    }

    public PlatformDeviceChange Deny(PlatformDeviceReason reason, PlatformDeviceActor actor, DateTimeOffset utcNow)
    {
        Require(PlatformDeviceStatus.Pending);
        End(PlatformDeviceStatus.Denied, reason, false, utcNow);
        return new PlatformDeviceChange(Record(PlatformDeviceEventCodes.Denied, actor, utcNow, reason), false, false);
    }

    public PlatformDeviceChange? ExpireIfDue(DateTimeOffset utcNow)
    {
        if (Status != PlatformDeviceStatus.Pending || utcNow < CreatedUtc.Add(PlatformLicensingLimits.PendingLifetime))
        {
            return null;
        }

        End(PlatformDeviceStatus.Expired, null, false, utcNow);
        return new PlatformDeviceChange(Record(PlatformDeviceEventCodes.Expired, PlatformDeviceActor.System, utcNow), false, false);
    }

    // =====================================================
    // METHOD: RELEASE (D-L6; owner: allowance, then Vixly review)
    // =====================================================
    // withinAllowance: the release is immediate. Otherwise it waits for Vixly staff; a lost or stolen
    // device stops at once either way, but over the allowance its seat is held until staff decide.
    public PlatformDeviceChange Release(PlatformDeviceReason reason, bool withinAllowance, PlatformDeviceActor actor, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (!PlatformLicensingLimits.ChosenByTenant(reason))
        {
            throw new DomainException(PlatformLicensingErrors.ReasonRequired);
        }

        if (Status == PlatformDeviceStatus.Revoked && SeatHeld)
        {
            if (!withinAllowance)
            {
                throw new DomainException(PlatformLicensingErrors.NotAllowedInStatus);
            }

            SeatHeld = false;
            MarkAsModified(utcNow);
            return new PlatformDeviceChange(Record(PlatformDeviceEventCodes.Released, actor, utcNow, reason), false, true);
        }

        if (Status is not (PlatformDeviceStatus.Active or PlatformDeviceStatus.ReleasePending))
        {
            throw new DomainException(PlatformLicensingErrors.NotAllowedInStatus);
        }

        if (reason == PlatformDeviceReason.LostOrStolen)
        {
            End(PlatformDeviceStatus.Revoked, reason, true, utcNow);
            SeatHeld = !withinAllowance;
            return new PlatformDeviceChange(Record(PlatformDeviceEventCodes.Revoked, actor, utcNow, reason, arguments: Args(("seatHeld", SeatHeld ? "true" : "false"))), false, withinAllowance);
        }

        if (withinAllowance)
        {
            End(PlatformDeviceStatus.Released, reason, true, utcNow);
            return new PlatformDeviceChange(Record(PlatformDeviceEventCodes.Released, actor, utcNow, reason), false, true);
        }

        if (Status == PlatformDeviceStatus.ReleasePending)
        {
            throw new DomainException(PlatformLicensingErrors.NotAllowedInStatus);
        }

        Status = PlatformDeviceStatus.ReleasePending;
        Reason = reason;
        ReleaseRequestedUtc = utcNow;
        MarkAsModified(utcNow);
        return new PlatformDeviceChange(Record(PlatformDeviceEventCodes.ReleaseRequested, actor, utcNow, reason), false, false);
    }

    public PlatformDeviceChange WithdrawRelease(PlatformDeviceActor actor, DateTimeOffset utcNow)
    {
        Require(PlatformDeviceStatus.ReleasePending);
        Status = PlatformDeviceStatus.Active;
        Reason = null;
        ReleaseRequestedUtc = null;
        MarkAsModified(utcNow);
        return new PlatformDeviceChange(Record(PlatformDeviceEventCodes.ReleaseWithdrawn, actor, utcNow), false, false);
    }

    // Vixly staff decide a release over the allowance; a refusal keeps the seat in use.
    public PlatformDeviceChange DecideRelease(bool approve, string? note, PlatformDeviceActor staff, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(staff);

        if (!AwaitsReview)
        {
            throw new DomainException(PlatformLicensingErrors.ReleaseNotPending);
        }

        string? text = string.IsNullOrWhiteSpace(note) ? null : note.Trim();

        if (text is null || text.Length > 500)
        {
            throw new DomainException(PlatformLicensingErrors.ReasonRequired);
        }

        if (!approve)
        {
            if (Status == PlatformDeviceStatus.ReleasePending)
            {
                Status = PlatformDeviceStatus.Active;
                ReleaseRequestedUtc = null;
            }

            MarkAsModified(utcNow);
            return new PlatformDeviceChange(Record(PlatformDeviceEventCodes.ReleaseRefused, staff, utcNow, Reason, text), false, false);
        }

        if (Status == PlatformDeviceStatus.ReleasePending)
        {
            End(PlatformDeviceStatus.Released, Reason, true, utcNow);
        }
        else
        {
            SeatHeld = false;
            MarkAsModified(utcNow);
        }

        return new PlatformDeviceChange(Record(PlatformDeviceEventCodes.ReleaseApproved, staff, utcNow, Reason, text), false, true);
    }

    // =====================================================
    // METHOD: REVOKE (staff, system, recovery)
    // =====================================================
    public PlatformDeviceChange Revoke(PlatformDeviceReason reason, PlatformDeviceActor actor, DateTimeOffset utcNow, string? note = null)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (Status is not (PlatformDeviceStatus.Pending or PlatformDeviceStatus.Active or PlatformDeviceStatus.ReleasePending) && !(Status == PlatformDeviceStatus.Revoked && SeatHeld))
        {
            throw new DomainException(PlatformLicensingErrors.NotAllowedInStatus);
        }

        bool held = HoldsSeat;
        End(PlatformDeviceStatus.Revoked, reason, false, utcNow);
        SeatHeld = false;
        return new PlatformDeviceChange(Record(PlatformDeviceEventCodes.Revoked, actor, utcNow, reason, note), false, held);
    }

    // =====================================================
    // METHOD: PROOF (D-L5)
    // =====================================================
    public PlatformDeviceSecretCheck CheckSecret(string? secretHash, DateTimeOffset utcNow)
    {
        if (Proof != PlatformDeviceProof.Secret || string.IsNullOrEmpty(secretHash))
        {
            return PlatformDeviceSecretCheck.Invalid;
        }

        if (string.Equals(secretHash, SecretHash, StringComparison.Ordinal))
        {
            return SecretIssuedUtc is { } issued && utcNow >= issued.Add(PlatformLicensingLimits.SecretRotation)
                ? PlatformDeviceSecretCheck.ValidRotate
                : PlatformDeviceSecretCheck.Valid;
        }

        if (string.Equals(secretHash, PreviousSecretHash, StringComparison.Ordinal))
        {
            return utcNow <= PreviousSecretValidUntilUtc ? PlatformDeviceSecretCheck.Valid : PlatformDeviceSecretCheck.Reused;
        }

        return PlatformDeviceSecretCheck.Invalid;
    }

    public void RotateSecret(string newSecretHash, DateTimeOffset utcNow)
    {
        if (Proof != PlatformDeviceProof.Secret || newSecretHash is null || newSecretHash.Length != PlatformLicensingLimits.HashLength)
        {
            throw new DomainException(PlatformLicensingErrors.ProofInvalid);
        }

        PreviousSecretHash = SecretHash;
        PreviousSecretValidUntilUtc = utcNow.Add(PlatformLicensingLimits.SecretGrace);
        SecretHash = newSecretHash;
        SecretIssuedUtc = utcNow;
        MarkAsModified(utcNow);
    }

    // Last seen is written at most once an hour, so checks stay cheap.
    public bool Touch(DateTimeOffset utcNow)
    {
        if (LastSeenUtc is { } seen && utcNow - seen < PlatformLicensingLimits.LastSeenEvery)
        {
            return false;
        }

        LastSeenUtc = utcNow;
        MarkAsModified(utcNow);
        return true;
    }

    public PlatformDeviceEvent Record(string eventCode, PlatformDeviceActor actor, DateTimeOffset utcNow, PlatformDeviceReason? reason = null, string? note = null, IReadOnlyDictionary<string, string>? arguments = null)
    {
        Dictionary<string, string> values = new(arguments ?? new Dictionary<string, string>(StringComparer.Ordinal), StringComparer.Ordinal)
        {
            ["number"] = Number,
            ["name"] = Name
        };

        return PlatformDeviceEvent.Record(PlatformTenantId, Id, eventCode, actor, utcNow, reason, note, values);
    }

    private void End(PlatformDeviceStatus status, PlatformDeviceReason? reason, bool byTenant, DateTimeOffset utcNow)
    {
        Status = status;
        Reason = reason;
        EndedUtc = utcNow;
        EndedByTenant = byTenant;
        ReleaseRequestedUtc = null;
        MarkAsModified(utcNow);
    }

    private void Require(PlatformDeviceStatus status)
    {
        if (Status != status)
        {
            throw new DomainException(PlatformLicensingErrors.NotAllowedInStatus);
        }
    }

    private static Dictionary<string, string> Args(params (string Key, string Value)[] values)
    {
        return values.ToDictionary(value => value.Key, value => value.Value, StringComparer.Ordinal);
    }
}
