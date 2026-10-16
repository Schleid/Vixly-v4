using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.UseCases.Legal;
using Vixly.Platform.Domain.Legal;
using Vixly.Platform.Domain.Messaging;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformLegalRegistrationHandler.cs
//
// PURPOSE:
// Records the Terms the registrant accepted when the tenant becomes active (D-Lg9).
//
// RESPONSIBILITIES:
// - On activation, find the registration and the published Terms version it accepted
// - Record the acceptance with the registration's language, time and IP
//
// IMPORTANT NOTES:
// - A replayed message changes nothing (one acceptance per tenant and version)
// - Staff-created tenants have no registration: they get 30 days (D-Lg10)
// =========================================================
public sealed class PlatformLegalRegistrationHandler : IPlatformOutboxHandler
{
    private readonly PlatformLegalAdministration _legal;

    public PlatformLegalRegistrationHandler(PlatformLegalAdministration legal)
    {
        _legal = legal ?? throw new ArgumentNullException(nameof(legal));
    }

    public string MessageType => PlatformMessageTypes.TenantStatusChanged;

    public async Task HandleAsync(PlatformOutboxMessage message, CancellationToken cancellationToken)
    {
        PlatformTenantStatusChanged changed = PlatformOutboxWriter.Read<PlatformTenantStatusChanged>(message);

        if (changed.ToStatus != PlatformTenantStatus.Active
            || await _legal.Registrations.FindByTenantAsync(changed.PlatformTenantId, cancellationToken) is not { } registration
            || await _legal.Store.FindTermsByVersionAsync(registration.TermsVersion, cancellationToken) is not { } terms
            || await _legal.Store.HasAcceptedAsync(changed.PlatformTenantId, terms.Id, cancellationToken)
            || (await _legal.Store.GetFileAsync(terms.Id, registration.Language.Value, cancellationToken)
                ?? await _legal.Store.GetFileAsync(terms.Id, Vixly.Shared.Kernel.Localization.LanguageCode.Default.Value, cancellationToken)) is not { } file)
        {
            return;
        }

        PlatformTenantTermsAcceptance acceptance = PlatformTenantTermsAcceptance.AtRegistration(
            changed.PlatformTenantId, terms, file, registration.Id, registration.OwnerName, registration.ConsentIpAddress, registration.CreatedUtc, _legal.UtcNow);
        _legal.Store.AddAcceptance(acceptance);
        _legal.Accepted(acceptance);
        _legal.Audit(null, "platform.legal.accepted_at_registration", null, nameof(PlatformTenant), changed.PlatformTenantId, new { version = terms.Version, language = acceptance.Language });

        if (!await _legal.SaveAsync(cancellationToken))
        {
            throw new InvalidOperationException("The registration acceptance could not be saved; the message will be retried.");
        }
    }
}
