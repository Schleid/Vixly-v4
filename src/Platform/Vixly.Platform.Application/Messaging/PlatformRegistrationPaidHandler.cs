using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Messaging;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformRegistrationPaidHandler.cs
//
// PURPOSE:
// Moves a self-registration on to account setup once its invoice is paid (D-P7).
//
// RESPONSIBILITIES:
// - React to "invoice paid" for a tenant still registering
// - Record the step in the audit log
//
// IMPORTANT NOTES:
// - Works for card and hand payments alike
// - A replayed message changes nothing
// =========================================================
public sealed class PlatformRegistrationPaidHandler : IPlatformOutboxHandler
{
    private readonly IPlatformTenantRegistrationRepository _registrations;

    private readonly PlatformAuditTrail _audit;

    private readonly PlatformOutboxWriter _outbox;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public PlatformRegistrationPaidHandler(IPlatformTenantRegistrationRepository registrations, PlatformAuditTrail audit, PlatformOutboxWriter outbox, IPlatformUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _registrations = registrations ?? throw new ArgumentNullException(nameof(registrations));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public string MessageType => PlatformMessageTypes.InvoicePaid;

    public async Task HandleAsync(PlatformOutboxMessage message, CancellationToken cancellationToken)
    {
        PlatformInvoicePaid paid = PlatformOutboxWriter.Read<PlatformInvoicePaid>(message);
        PlatformTenantRegistration? registration = await _registrations.FindByTenantAsync(paid.PlatformTenantId, cancellationToken);

        if (registration is null || !registration.RecordPaid(_timeProvider.GetUtcNow()))
        {
            return;
        }

        _audit.Record(null, "platform.registrations.paid", null, nameof(PlatformTenantRegistration), registration.Id, after: new { invoice = paid.Number });
        _outbox.Write(
            PlatformMessageTypes.RegistrationAccountSetupReady,
            paid.PlatformTenantId,
            new PlatformRegistrationAccountSetupReady(paid.PlatformTenantId, registration.Id, _timeProvider.GetUtcNow()));

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            _unitOfWork.DiscardChanges();
            throw new DomainException(PlatformCommonErrors.ConcurrencyConflict);
        }
    }
}
