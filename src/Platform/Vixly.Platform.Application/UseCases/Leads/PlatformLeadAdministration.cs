using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Messaging;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Leads;

// =========================================================
// FILE: PlatformLeadAdministration.cs
//
// PURPOSE:
// Shared steps of the contact-request use cases (Stage 2, D-U3).
//
// RESPONSIBILITIES:
// - Authorize staff
// - Reach the store, the outbox, the audit and the clock
//
// IMPORTANT NOTES:
// - Anonymous submission skips the gate; the bot check stands in for it
// =========================================================
public sealed class PlatformLeadAdministration
{
    private readonly IPlatformAuthorizationGate _gate;

    private readonly IPlatformSalesLeadRepository _leads;

    private readonly IPlatformBotCheck _botCheck;

    private readonly IPlatformRequestMetadata _metadata;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly PlatformAuditTrail _audit;

    private readonly PlatformOutboxWriter _outbox;

    private readonly TimeProvider _timeProvider;

    public PlatformLeadAdministration(
        IPlatformAuthorizationGate gate,
        IPlatformSalesLeadRepository leads,
        IPlatformBotCheck botCheck,
        IPlatformRequestMetadata metadata,
        IPlatformUnitOfWork unitOfWork,
        PlatformAuditTrail audit,
        PlatformOutboxWriter outbox,
        TimeProvider timeProvider)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _leads = leads ?? throw new ArgumentNullException(nameof(leads));
        _botCheck = botCheck ?? throw new ArgumentNullException(nameof(botCheck));
        _metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public DateTimeOffset UtcNow => _timeProvider.GetUtcNow();

    public IPlatformSalesLeadRepository Store => _leads;

    public IPlatformBotCheck BotCheck => _botCheck;

    public string? ClientIp => _metadata.ClientIp;

    public PlatformAuditTrail Audit => _audit;

    public PlatformOutboxWriter Outbox => _outbox;

    public async Task<Error?> AuthorizeAsync(PlatformAuthorizationContext context, string permissionCode, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        await _gate.AuthorizeAsync(context, permissionCode, cancellationToken);
        return null;
    }

    public Task<bool> SaveAsync(CancellationToken cancellationToken) => _unitOfWork.SaveChangesAsync(cancellationToken);
}
