using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.Maintenance;

// =========================================================
// FILE: PlatformMaintenanceAdministration.cs
//
// PURPOSE:
// Shared steps of the maintenance window use cases.
//
// RESPONSIBILITIES:
// - Authorize staff, with step-up for changes
// - Reach the store, the secrets, the audit and the clock
//
// IMPORTANT NOTES:
// - The sites read windows anonymously; no gate there
// =========================================================
public sealed class PlatformMaintenanceAdministration
{
    private readonly IPlatformAuthorizationGate _gate;

    private readonly IPlatformStepUpGuard _stepUp;

    private readonly IPlatformMaintenanceRepository _store;

    private readonly IPlatformSecretGenerator _secrets;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly PlatformAuditTrail _audit;

    private readonly TimeProvider _timeProvider;

    private readonly Vixly.Platform.Application.Messaging.PlatformOutboxWriter _outbox;

    public PlatformMaintenanceAdministration(
        IPlatformAuthorizationGate gate,
        IPlatformStepUpGuard stepUp,
        IPlatformMaintenanceRepository store,
        IPlatformSecretGenerator secrets,
        IPlatformUnitOfWork unitOfWork,
        PlatformAuditTrail audit,
        TimeProvider timeProvider,
        Vixly.Platform.Application.Messaging.PlatformOutboxWriter outbox)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _stepUp = stepUp ?? throw new ArgumentNullException(nameof(stepUp));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
    }

    public DateTimeOffset UtcNow => _timeProvider.GetUtcNow();

    public IPlatformMaintenanceRepository Store => _store;

    public IPlatformSecretGenerator Secrets => _secrets;

    public PlatformAuditTrail Audit => _audit;

    public async Task<Error?> AuthorizeAsync(PlatformAuthorizationContext context, string permissionCode, bool requireStepUp, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        await _gate.AuthorizeAsync(context, permissionCode, cancellationToken);

        return requireStepUp && !await _stepUp.HasRecentSecondFactorAsync(context, cancellationToken)
            ? PlatformCommonErrors.StepUpRequired
            : null;
    }

    // Saved with the change: the Cloudflare Worker gets the new list once it is committed (owner, 2026-10-28).
    public void TellEdge(Guid windowId)
    {
        _outbox.Write(Vixly.Platform.Application.Messaging.PlatformMessageTypes.EdgeMaintenanceChanged, PlatformEdgeMaintenance.StreamId,
            new Vixly.Platform.Application.Messaging.PlatformEdgeMaintenanceChanged(windowId));
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
}
