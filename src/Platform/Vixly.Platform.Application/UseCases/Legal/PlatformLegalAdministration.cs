using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Messaging;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Legal;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Localization;

namespace Vixly.Platform.Application.UseCases.Legal;

// =========================================================
// FILE: PlatformLegalAdministration.cs
//
// PURPOSE:
// Shared steps of the legal use cases (Slice 8).
//
// RESPONSIBILITIES:
// - Staff authorization with step-up, abilities, save, audit, outbox notices and events
// - Build the public Terms view in one language
//
// IMPORTANT NOTES:
// - Tenant calls are scoped to the caller's tenant (S-13)
// =========================================================
public sealed class PlatformLegalAdministration
{
    private readonly IPlatformAuthorizationGate _gate;

    private readonly IPlatformStepUpGuard _stepUp;

    private readonly IPlatformPermissionReader _permissions;

    private readonly IPlatformLegalRepository _store;

    private readonly IPlatformLegalQueries _queries;

    private readonly PlatformLegalGate _legal;

    private readonly IPlatformTenantRepository _tenants;

    private readonly IPlatformTenantRegistrationRepository _registrations;

    private readonly IPlatformPublicLinks _links;

    private readonly PlatformAuditTrail _audit;

    private readonly PlatformOutboxWriter _outbox;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public PlatformLegalAdministration(
        IPlatformAuthorizationGate gate,
        IPlatformStepUpGuard stepUp,
        IPlatformPermissionReader permissions,
        IPlatformLegalRepository store,
        IPlatformLegalQueries queries,
        PlatformLegalGate legal,
        IPlatformTenantRepository tenants,
        IPlatformTenantRegistrationRepository registrations,
        IPlatformPublicLinks links,
        PlatformAuditTrail audit,
        PlatformOutboxWriter outbox,
        IPlatformUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _stepUp = stepUp ?? throw new ArgumentNullException(nameof(stepUp));
        _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
        _legal = legal ?? throw new ArgumentNullException(nameof(legal));
        _tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        _registrations = registrations ?? throw new ArgumentNullException(nameof(registrations));
        _links = links ?? throw new ArgumentNullException(nameof(links));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public DateTimeOffset UtcNow => _timeProvider.GetUtcNow();

    public IPlatformLegalRepository Store => _store;

    public IPlatformLegalQueries Queries => _queries;

    public PlatformLegalGate Legal => _legal;

    public IPlatformTenantRepository Tenants => _tenants;

    public IPlatformTenantRegistrationRepository Registrations => _registrations;

    public IPlatformPublicLinks Links => _links;

    public IPlatformUnitOfWork UnitOfWork => _unitOfWork;

    public async Task<Error?> AuthorizeAsync(PlatformAuthorizationContext context, string permissionCode, bool requireStepUp, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        await _gate.AuthorizeAsync(context, permissionCode, cancellationToken);

        return requireStepUp && !await _stepUp.HasRecentSecondFactorAsync(context, cancellationToken)
            ? PlatformCommonErrors.StepUpRequired
            : null;
    }

    public async Task<bool> HoldsAsync(PlatformAuthorizationContext context, string permissionCode, CancellationToken cancellationToken)
    {
        PlatformPermissionSnapshot? actor = await _permissions.ReadAsync(context.PlatformUserId, UtcNow, cancellationToken);
        return actor is not null && actor.IsAccountActive && PlatformAccess.Holds(actor, permissionCode);
    }

    public async Task<PlatformLegalAbilities> AbilitiesAsync(PlatformAuthorizationContext context, CancellationToken cancellationToken)
    {
        return new PlatformLegalAbilities(
            await HoldsAsync(context, PlatformPermissions.LegalManage, cancellationToken),
            await HoldsAsync(context, PlatformPermissions.LegalPublish, cancellationToken),
            await HoldsAsync(context, PlatformPermissions.LegalRemind, cancellationToken),
            await HoldsAsync(context, PlatformPermissions.LegalRecordAcceptance, cancellationToken));
    }

    public async Task<bool> SaveAsync(CancellationToken cancellationToken)
    {
        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return true;
        }

        _unitOfWork.DiscardChanges();
        return false;
    }

    public void Audit(Guid? staffUserId, string actionCode, string? permissionCode, string targetType, Guid? targetId, object? after = null, string? reason = null)
    {
        _audit.Record(staffUserId, actionCode, permissionCode, targetType, targetId, after: after, reason: reason);
    }

    public void Notice(Guid tenantId, Guid documentId, PlatformTermsReminderStep step)
    {
        _outbox.Write(PlatformMessageTypes.LegalNotice, tenantId, new PlatformLegalNotice(tenantId, documentId, step, UtcNow));
    }

    public void Accepted(PlatformTenantTermsAcceptance acceptance)
    {
        ArgumentNullException.ThrowIfNull(acceptance);
        _outbox.Write(PlatformMessageTypes.TermsAccepted, acceptance.PlatformTenantId,
            new PlatformTermsAccepted(acceptance.PlatformTenantId, acceptance.PlatformLegalDocumentId, acceptance.Version, acceptance.Source, UtcNow));
    }

    public void Published(PlatformLegalDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        _outbox.Write(PlatformMessageTypes.LegalVersionPublished, document.Id,
            new PlatformLegalVersionPublished(document.Id, document.Kind, document.Version, document.NeedsAcceptance, document.EffectiveUtc, document.DeadlineUtc, UtcNow));
    }

    // The current published Terms in a language (English when the language is unknown); null before the first publish.
    public async Task<PlatformLegalTerms?> TermsAsync(string? language, CancellationToken cancellationToken)
    {
        if (await _store.GetPublishedAsync(PlatformLegalDocumentKind.Terms, cancellationToken) is not { } terms)
        {
            return null;
        }

        string code = LanguageCode.TryCreate(language, out LanguageCode? parsed) ? parsed!.Value : LanguageCode.Default.Value;
        IReadOnlyList<PlatformLegalFileItem> files = await _queries.ListFilesAsync(terms.Id, cancellationToken);
        PlatformLegalFileItem? file = files.FirstOrDefault(item => item.Language == code);

        if (file is null)
        {
            return null;
        }

        List<PlatformLegalPart> parts = new();

        foreach (PlatformLegalDocumentPart part in terms.Parts)
        {
            PlatformLegalFileItem? partFile = (await _queries.ListFilesAsync(part.IncludedDocumentId, cancellationToken)).FirstOrDefault(item => item.Language == code);
            parts.Add(new PlatformLegalPart(part.IncludedDocumentId, part.Kind, part.Version, partFile?.Sha256 ?? string.Empty, partFile?.Summary));
        }

        return new PlatformLegalTerms(terms.Id, terms.Version, code, file.Sha256, file.Summary, terms.EffectiveUtc, parts);
    }
}
