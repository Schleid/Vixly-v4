using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Legal;

// =========================================================
// FILE: PlatformTenantTermsAcceptance.cs
//
// PURPOSE:
// A tenant's acceptance of one Terms version, with its evidence (D-Lg5, D-Lg11).
//
// RESPONSIBILITIES:
// - Record version, file hash, language read, who, when, client IP and user agent, and how
// - Staff records carry the reason and the signed paper
//
// IMPORTANT NOTES:
// - Append-only: never changed or deleted (DENY in the global script)
// - One per tenant and version
// =========================================================
public sealed class PlatformTenantTermsAcceptance : AuditableEntity
{
    private PlatformTenantTermsAcceptance()
    {
        Version = string.Empty;
        Sha256 = string.Empty;
        Language = string.Empty;
    }

    private PlatformTenantTermsAcceptance(Guid tenantId, PlatformLegalDocument terms, string sha256, string language, PlatformTermsAcceptanceSource source, DateTimeOffset acceptedUtc, DateTimeOffset utcNow)
        : base(utcNow)
    {
        PlatformTenantId = tenantId;
        PlatformLegalDocumentId = terms.Id;
        Version = terms.Version;
        Sha256 = sha256;
        Language = language;
        Source = source;
        AcceptedUtc = acceptedUtc;
    }

    public Guid PlatformTenantId { get; private set; }

    public Guid PlatformLegalDocumentId { get; private set; }

    public string Version { get; private set; }

    public string Sha256 { get; private set; }

    public string Language { get; private set; }

    public PlatformTermsAcceptanceSource Source { get; private set; }

    public DateTimeOffset AcceptedUtc { get; private set; }

    public string? ActorReference { get; private set; }

    public string? ActorName { get; private set; }

    public string? ActorRole { get; private set; }

    public string? ClientIp { get; private set; }

    public string? UserAgent { get; private set; }

    public Guid? PlatformTenantRegistrationId { get; private set; }

    public Guid? RecordedByUserId { get; private set; }

    public string? Reason { get; private set; }

    public byte[]? Evidence { get; private set; }

    public string? EvidenceContentType { get; private set; }

    public static PlatformTenantTermsAcceptance ByOwner(Guid tenantId, PlatformLegalDocument terms, PlatformLegalDocumentFile file, string? actorReference, string? actorName, string? role, string? clientIp, string? userAgent, DateTimeOffset utcNow)
    {
        Check(terms, file);

        if (!string.Equals(role?.Trim(), PlatformLegalLimits.OwnerRole, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException(PlatformLegalErrors.OwnerOnly);
        }

        string reference = Text(actorReference, PlatformLegalLimits.ActorReferenceMaxLength) ?? throw new DomainException(PlatformLegalErrors.ActorRequired);

        return new PlatformTenantTermsAcceptance(tenantId, terms, file.Sha256, file.Language, PlatformTermsAcceptanceSource.Operations, utcNow, utcNow)
        {
            ActorReference = reference,
            ActorName = Text(actorName, PlatformLegalLimits.ActorNameMaxLength),
            ActorRole = PlatformLegalLimits.OwnerRole,
            ClientIp = Text(clientIp, PlatformLegalLimits.IpAddressMaxLength),
            UserAgent = Text(userAgent, PlatformLegalLimits.UserAgentMaxLength)
        };
    }

    public static PlatformTenantTermsAcceptance AtRegistration(Guid tenantId, PlatformLegalDocument terms, PlatformLegalDocumentFile file, Guid registrationId, string? ownerName, string? clientIp, DateTimeOffset acceptedUtc, DateTimeOffset utcNow)
    {
        Check(terms, file);

        return new PlatformTenantTermsAcceptance(tenantId, terms, file.Sha256, file.Language, PlatformTermsAcceptanceSource.Registration, acceptedUtc, utcNow)
        {
            PlatformTenantRegistrationId = registrationId,
            ActorName = Text(ownerName, PlatformLegalLimits.ActorNameMaxLength),
            ActorRole = PlatformLegalLimits.OwnerRole,
            ClientIp = Text(clientIp, PlatformLegalLimits.IpAddressMaxLength)
        };
    }

    public static PlatformTenantTermsAcceptance RecordedByStaff(Guid tenantId, PlatformLegalDocument terms, PlatformLegalDocumentFile file, string? ownerName, string? reason, byte[]? evidence, string? evidenceContentType, Guid staffUserId, DateTimeOffset utcNow)
    {
        Check(terms, file);

        if (staffUserId == Guid.Empty)
        {
            throw new DomainException(PlatformLegalErrors.ActorRequired);
        }

        string why = Text(reason, PlatformLegalLimits.ReasonMaxLength) ?? throw new DomainException(PlatformLegalErrors.ReasonRequired);
        bool pdf = evidenceContentType == "application/pdf" && evidence is { Length: > 4 } && evidence[0] == (byte)'%' && evidence[1] == (byte)'P';
        bool png = evidenceContentType == "image/png" && evidence is { Length: > 8 } && evidence[0] == 0x89 && evidence[1] == 0x50;
        bool jpeg = evidenceContentType == "image/jpeg" && evidence is { Length: > 3 } && evidence[0] == 0xFF && evidence[1] == 0xD8;

        if ((!pdf && !png && !jpeg) || evidence!.Length > PlatformLegalLimits.MaxEvidenceMegabytes * 1024 * 1024)
        {
            throw new DomainException(PlatformLegalErrors.EvidenceInvalid);
        }

        return new PlatformTenantTermsAcceptance(tenantId, terms, file.Sha256, file.Language, PlatformTermsAcceptanceSource.StaffRecord, utcNow, utcNow)
        {
            ActorName = Text(ownerName, PlatformLegalLimits.ActorNameMaxLength),
            ActorRole = PlatformLegalLimits.OwnerRole,
            RecordedByUserId = staffUserId,
            Reason = why,
            Evidence = evidence,
            EvidenceContentType = evidenceContentType
        };
    }

    private static void Check(PlatformLegalDocument terms, PlatformLegalDocumentFile file)
    {
        ArgumentNullException.ThrowIfNull(terms);
        ArgumentNullException.ThrowIfNull(file);

        if (terms.Kind != PlatformLegalDocumentKind.Terms || terms.Status != PlatformLegalDocumentStatus.Published || file.PlatformLegalDocumentId != terms.Id)
        {
            throw new DomainException(PlatformLegalErrors.NothingToAccept);
        }
    }

    private static string? Text(string? value, int max)
    {
        string? text = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        return text is null ? null : text.Length > max ? text[..max] : text;
    }
}
