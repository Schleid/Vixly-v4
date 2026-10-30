using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Licensing;

// =========================================================
// FILE: PlatformOwnerSupportRequest.cs
//
// PURPOSE:
// A Vixly support action for an Owner who lost a device or their second factor (owner decision D-O13, 2026-10-18).
//
// RESPONSIBILITIES:
// - Record who asked, why, and how support checked the caller
// - Device recovery: approved by its requester at once
// - Second-factor reset: approved by a second staff member, never the requester
// - Complete once the emailed link is used
//
// IMPORTANT NOTES:
// - The Owner is the highest privilege in a tenant: no self-service page starts these
// - The link goes to the Owner contact only; the request never holds a token
// =========================================================
public sealed class PlatformOwnerSupportRequest : AuditableEntity
{
    private PlatformOwnerSupportRequest()
    {
        Reason = string.Empty;
        IdentityCheck = string.Empty;
    }

    private PlatformOwnerSupportRequest(Guid tenantId, PlatformOwnerSupportKind kind, string reason, string identityCheck, Guid? replaceDeviceId, Guid requestedBy, DateTimeOffset utcNow)
        : base(utcNow)
    {
        PlatformTenantId = tenantId;
        Kind = kind;
        Reason = reason;
        IdentityCheck = identityCheck;
        ReplaceDeviceId = replaceDeviceId;
        RequestedBy = requestedBy;
        RequestedUtc = utcNow;
        Status = PlatformOwnerSupportStatus.Pending;
    }

    public Guid PlatformTenantId { get; private set; }

    public PlatformOwnerSupportKind Kind { get; private set; }

    public string Reason { get; private set; }

    // How support confirmed the caller, for example a call back to the Owner phone on file.
    public string IdentityCheck { get; private set; }

    // Device recovery only: the lost device that gives its seat when none is free.
    public Guid? ReplaceDeviceId { get; private set; }

    public Guid RequestedBy { get; private set; }

    public DateTimeOffset RequestedUtc { get; private set; }

    public PlatformOwnerSupportStatus Status { get; private set; }

    public Guid? DecidedBy { get; private set; }

    public DateTimeOffset? DecidedUtc { get; private set; }

    public string? DecisionNote { get; private set; }

    public DateTimeOffset? CompletedUtc { get; private set; }

    public bool IsOpen => Status is PlatformOwnerSupportStatus.Pending or PlatformOwnerSupportStatus.Approved;

    // Device recovery: one staff member, as before, now with the reason and the identity check on record.
    public static PlatformOwnerSupportRequest StartDeviceRecovery(Guid tenantId, string? reason, string? identityCheck, Guid? replaceDeviceId, Guid requestedBy, DateTimeOffset utcNow)
    {
        PlatformOwnerSupportRequest request = new(tenantId, PlatformOwnerSupportKind.DeviceRecovery, Text(reason, PlatformLicensingErrors.SupportReasonRequired),
            Text(identityCheck, PlatformLicensingErrors.IdentityCheckRequired), replaceDeviceId, Staff(requestedBy), utcNow)
        {
            Status = PlatformOwnerSupportStatus.Approved,
            DecidedBy = requestedBy,
            DecidedUtc = utcNow
        };

        return request;
    }

    // Second-factor reset: waits for a second staff member.
    public static PlatformOwnerSupportRequest RequestSecondFactorReset(Guid tenantId, string? reason, string? identityCheck, Guid requestedBy, DateTimeOffset utcNow)
    {
        return new PlatformOwnerSupportRequest(tenantId, PlatformOwnerSupportKind.SecondFactorReset, Text(reason, PlatformLicensingErrors.SupportReasonRequired),
            Text(identityCheck, PlatformLicensingErrors.IdentityCheckRequired), null, Staff(requestedBy), utcNow);
    }

    public void Approve(Guid approvedBy, string? note, DateTimeOffset utcNow)
    {
        if (Status != PlatformOwnerSupportStatus.Pending)
        {
            throw new DomainException(PlatformLicensingErrors.SupportNotPending);
        }

        if (Staff(approvedBy) == RequestedBy)
        {
            throw new DomainException(PlatformLicensingErrors.ApproverMustDiffer);
        }

        Decide(PlatformOwnerSupportStatus.Approved, approvedBy, note, utcNow);
    }

    public void Refuse(Guid refusedBy, string? note, DateTimeOffset utcNow)
    {
        if (Status != PlatformOwnerSupportStatus.Pending)
        {
            throw new DomainException(PlatformLicensingErrors.SupportNotPending);
        }

        Decide(PlatformOwnerSupportStatus.Refused, Staff(refusedBy), note, utcNow);
    }

    public void Complete(DateTimeOffset utcNow)
    {
        if (Status != PlatformOwnerSupportStatus.Approved)
        {
            throw new DomainException(PlatformLicensingErrors.SupportNotPending);
        }

        Status = PlatformOwnerSupportStatus.Completed;
        CompletedUtc = utcNow;
        MarkAsModified(utcNow);
    }

    private void Decide(PlatformOwnerSupportStatus status, Guid by, string? note, DateTimeOffset utcNow)
    {
        string? trimmed = string.IsNullOrWhiteSpace(note) ? null : note.Trim();

        if (trimmed is { Length: > PlatformLicensingLimits.SupportTextMaxLength })
        {
            throw new DomainException(PlatformLicensingErrors.ReasonRequired);
        }

        Status = status;
        DecidedBy = by;
        DecidedUtc = utcNow;
        DecisionNote = trimmed;
        MarkAsModified(utcNow);
    }

    private static string Text(string? value, Vixly.Shared.Kernel.Errors.Error error)
    {
        string text = value?.Trim() ?? string.Empty;
        return text.Length is 0 or > PlatformLicensingLimits.SupportTextMaxLength ? throw new DomainException(error) : text;
    }

    private static Guid Staff(Guid staffId) => staffId == Guid.Empty ? throw new DomainException(PlatformLicensingErrors.ActorRequired) : staffId;
}
