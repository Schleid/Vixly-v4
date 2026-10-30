using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Tenants;

// =========================================================
// FILE: PlatformTenantChangeRequest.cs
//
// PURPOSE:
// A tenant’s request to change its legal company details, with proof, for TechCrafters to approve (B12, D-T8).
//
// RESPONSIBILITIES:
// - Hold the details before and the details asked for, the reason and the proof
// - Approve (applies the change to the tenant), reject with a note, or withdraw
//
// IMPORTANT NOTES:
// - Company legal details never change any other way once the tenant exists
// - Issued invoices keep the details they were issued with
// - Submitted by the tenant owner through Operations; decided by staff in the console
// =========================================================
public sealed class PlatformTenantChangeRequest : AuditableEntity
{
    private PlatformTenantChangeRequest()
    {
        Previous = null!;
        Proposed = null!;
        Reason = string.Empty;
        SubmittedByReference = string.Empty;
        SubmittedByName = string.Empty;
        Proof = null!;
    }

    private PlatformTenantChangeRequest(DateTimeOffset utcNow)
        : base(utcNow)
    {
        Previous = null!;
        Proposed = null!;
        Reason = string.Empty;
        SubmittedByReference = string.Empty;
        SubmittedByName = string.Empty;
        Proof = null!;
        Status = PlatformTenantChangeStatus.Pending;
    }

    public Guid PlatformTenantId { get; private set; }

    public PlatformCompanyDetails Previous { get; private set; }

    public PlatformCompanyDetails Proposed { get; private set; }

    public string Reason { get; private set; }

    public PlatformTenantChangeStatus Status { get; private set; }

    public string SubmittedByReference { get; private set; }

    public string SubmittedByName { get; private set; }

    public DateTimeOffset? DecidedUtc { get; private set; }

    public Guid? DecidedByUserId { get; private set; }

    public string? DecisionNote { get; private set; }

    public PlatformTenantChangeProof Proof { get; private set; }

    public bool IsPending => Status == PlatformTenantChangeStatus.Pending;

    public static PlatformTenantChangeRequest Submit(
        PlatformTenant tenant,
        PlatformCompanyDetails proposed,
        string? reason,
        string? proofFileName,
        byte[]? proofContent,
        string? actorReference,
        string? actorName,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        ArgumentNullException.ThrowIfNull(proposed);

        string why = reason?.Trim() ?? string.Empty;

        if (why.Length == 0)
        {
            throw new DomainException(PlatformTenantChangeErrors.ReasonRequired);
        }

        if (why.Length > PlatformTenantChangeLimits.ReasonMaxLength)
        {
            throw new DomainException(PlatformTenantChangeErrors.ReasonTooLong);
        }

        if (Same(tenant.Company, proposed))
        {
            throw new DomainException(PlatformTenantChangeErrors.NoDifference);
        }

        PlatformTenantChangeRequest request = new(utcNow)
        {
            PlatformTenantId = tenant.Id,
            Previous = tenant.Company,
            Proposed = proposed,
            Reason = why,
            SubmittedByReference = Clip(actorReference),
            SubmittedByName = Clip(actorName)
        };

        request.Proof = PlatformTenantChangeProof.Create(request.Id, proofFileName, proofContent);
        return request;
    }

    public PlatformTenantLifecycleEvent Approve(PlatformTenant tenant, string? note, Guid staffUserId, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        if (tenant.Id != PlatformTenantId)
        {
            throw new ArgumentException("The tenant is not the one this request is for.", nameof(tenant));
        }

        Decide(PlatformTenantChangeStatus.Approved, note, required: false, staffUserId, utcNow);
        return tenant.ApplyCompanyChange(Proposed, Id, staffUserId, utcNow);
    }

    public void Reject(string? note, Guid staffUserId, DateTimeOffset utcNow)
    {
        Decide(PlatformTenantChangeStatus.Rejected, note, required: true, staffUserId, utcNow);
    }

    public void Withdraw(DateTimeOffset utcNow)
    {
        if (!IsPending)
        {
            throw new DomainException(PlatformTenantChangeErrors.NotPending);
        }

        Status = PlatformTenantChangeStatus.Withdrawn;
        DecidedUtc = utcNow;
        MarkAsModified(utcNow);
    }

    // Two sets of details are the same when every field a person would read is the same.
    public static bool Same(PlatformCompanyDetails left, PlatformCompanyDetails right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        return left.LegalName == right.LegalName
            && left.TradingName == right.TradingName
            && string.Equals(left.Email.Value, right.Email.Value, StringComparison.OrdinalIgnoreCase)
            && left.Phone.Value == right.Phone.Value
            && left.Address.Line1 == right.Address.Line1
            && left.Address.Line2 == right.Address.Line2
            && left.Address.City == right.Address.City
            && left.Address.StateProvince == right.Address.StateProvince
            && left.Address.PostalCode == right.Address.PostalCode
            && left.Address.CountryCode == right.Address.CountryCode;
    }

    private void Decide(PlatformTenantChangeStatus status, string? note, bool required, Guid staffUserId, DateTimeOffset utcNow)
    {
        Guid staff = PlatformTenantText.Actor(staffUserId);

        if (!IsPending)
        {
            throw new DomainException(PlatformTenantChangeErrors.NotPending);
        }

        string? text = string.IsNullOrWhiteSpace(note) ? null : note.Trim();

        if (required && text is null)
        {
            throw new DomainException(PlatformTenantChangeErrors.NoteRequired);
        }

        if (text?.Length > PlatformTenantChangeLimits.NoteMaxLength)
        {
            throw new DomainException(PlatformTenantChangeErrors.NoteTooLong);
        }

        Status = status;
        DecisionNote = text;
        DecidedByUserId = staff;
        DecidedUtc = utcNow;
        MarkAsModified(utcNow);
    }

    private static string Clip(string? value)
    {
        string text = value?.Trim() ?? string.Empty;
        return text.Length <= PlatformTenantChangeLimits.ActorMaxLength ? text : text[..PlatformTenantChangeLimits.ActorMaxLength];
    }
}
