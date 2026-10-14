using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Messaging;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Sales;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Sales;

// =========================================================
// FILE: PlatformSalesAdministration.cs
//
// PURPOSE:
// What the sales use cases share: the gate, step-up, the Sales settings, link tokens, audit and approval requests.
//
// RESPONSIBILITIES:
// - Check a permission and, when asked, a recent second factor
// - Read the Sales settings, empty when never saved (D-S22)
// - Make offer link tokens and keep only their hash
// - Audit a change and ask the approvers (D-S24, D-S26)
//
// IMPORTANT NOTES:
// - No commercial default here: an empty settings row sends everything to approval
// - The approval request is an outbox message in the same commit
// =========================================================
public sealed class PlatformSalesAdministration
{
    private readonly IPlatformAuthorizationGate _gate;

    private readonly IPlatformStepUpGuard _stepUp;

    private readonly IPlatformPermissionReader _permissions;

    private readonly IPlatformSalesSettingsRepository _settings;

    private readonly IPlatformPromotionRepository _promotions;

    private readonly IPlatformOfferRepository _offers;

    private readonly IPlatformSecretGenerator _secrets;

    private readonly IPlatformPublicLinks _links;

    private readonly PlatformAuditTrail _audit;

    private readonly PlatformOutboxWriter _outbox;

    private readonly TimeProvider _timeProvider;

    public PlatformSalesAdministration(
        IPlatformAuthorizationGate gate,
        IPlatformStepUpGuard stepUp,
        IPlatformPermissionReader permissions,
        IPlatformSalesSettingsRepository settings,
        IPlatformPromotionRepository promotions,
        IPlatformOfferRepository offers,
        IPlatformSecretGenerator secrets,
        IPlatformPublicLinks links,
        PlatformAuditTrail audit,
        PlatformOutboxWriter outbox,
        TimeProvider timeProvider)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _stepUp = stepUp ?? throw new ArgumentNullException(nameof(stepUp));
        _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _promotions = promotions ?? throw new ArgumentNullException(nameof(promotions));
        _offers = offers ?? throw new ArgumentNullException(nameof(offers));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        _links = links ?? throw new ArgumentNullException(nameof(links));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public DateTimeOffset UtcNow => _timeProvider.GetUtcNow();

    public IPlatformSalesSettingsRepository SettingsStore => _settings;

    public IPlatformPromotionRepository Promotions => _promotions;

    public IPlatformOfferRepository Offers => _offers;

    // =====================================================
    // METHOD: AUTHORIZE
    // =====================================================
    public async Task<Error?> AuthorizeAsync(
        PlatformAuthorizationContext context,
        string permissionCode,
        bool requireStepUp,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionCode);

        await _gate.AuthorizeAsync(context, permissionCode, cancellationToken);

        return requireStepUp && !await _stepUp.HasRecentSecondFactorAsync(context, cancellationToken)
            ? PlatformCommonErrors.StepUpRequired
            : null;
    }

    public async Task<bool> HoldsAsync(PlatformAuthorizationContext context, string permissionCode, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        PlatformPermissionSnapshot? actor = await _permissions.ReadAsync(context.PlatformUserId, UtcNow, cancellationToken);

        return actor is not null && actor.IsAccountActive && PlatformAccess.Holds(actor, permissionCode);
    }

    // =====================================================
    // METHOD: SETTINGS (D-S22)
    // =====================================================
    public async Task<PlatformSalesSettings> SettingsAsync(CancellationToken cancellationToken)
    {
        return await _settings.GetAsync(cancellationToken) ?? PlatformSalesSettings.Empty(UtcNow);
    }

    // =====================================================
    // METHOD: OFFER LINKS (D-S21)
    // =====================================================
    public (string Link, string Hash) NewOfferLink()
    {
        PlatformSecret secret = _secrets.NewToken();

        return (_links.OfferLink(secret.Value), secret.Hash);
    }

    public string HashToken(string token)
    {
        return _secrets.HashToken(token);
    }

    // =====================================================
    // METHOD: RECORD
    // =====================================================
    public void Audit(
        PlatformAuthorizationContext? context,
        string actionCode,
        string? permissionCode,
        string targetType,
        Guid targetId,
        object? after = null,
        string? reason = null)
    {
        _audit.Record(context?.PlatformUserId, actionCode, permissionCode, targetType, targetId, after: after, reason: reason);
    }

    public void RequestApproval(PlatformApprovalKind kind, Guid targetId, string permissionCode, string title, Guid? requestedByUserId)
    {
        _outbox.Write(
            PlatformMessageTypes.ApprovalRequested,
            targetId,
            new PlatformApprovalRequested(kind, targetId, permissionCode, title, requestedByUserId, UtcNow));
    }
}
