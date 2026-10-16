using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Legal;
using Vixly.Platform.Domain.Legal;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Legal.SendPlatformTermsReminder;

// =========================================================
// FILE: SendPlatformTermsReminderUseCase.cs
//
// PURPOSE:
// Sends a Terms reminder to a tenant's owner now, with a reason (D-Lg7).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class SendPlatformTermsReminderUseCase
{
    private readonly PlatformLegalAdministration _legal;

    public SendPlatformTermsReminderUseCase(
        PlatformLegalAdministration legal)
    {
        _legal = legal ?? throw new ArgumentNullException(nameof(legal));
    }

    public async Task<SendPlatformTermsReminderResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        SendPlatformTermsReminderRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _legal.AuthorizeAsync(context, PlatformPermissions.LegalRemind, requireStepUp: false, cancellationToken) is { } refused)
        {
            return SendPlatformTermsReminderResponse.Failure(refused);
        }

        PlatformTenant? tenant = await _legal.Tenants.GetAsync(request.PlatformTenantId, cancellationToken);
        PlatformTenantLegalState state = tenant is null ? PlatformTenantLegalState.NotRequired : await _legal.Legal.StateAsync(tenant, cancellationToken);

        if (tenant is null || state.RequiredDocumentId is not Guid documentId || state.Status is PlatformLegalStatus.NotRequired or PlatformLegalStatus.Accepted)
        {
            return SendPlatformTermsReminderResponse.Failure(PlatformLegalErrors.TenantNotAsked);
        }

        try
        {
            _legal.Store.AddReminder(PlatformTermsReminder.Manual(tenant.Id, documentId, tenant.DefaultLanguage.Value, request.Reason, context.PlatformUserId,
                await _legal.Store.LastManualReminderAsync(tenant.Id, cancellationToken), _legal.UtcNow));
        }
        catch (DomainException exception)
        {
            return SendPlatformTermsReminderResponse.Failure(exception.Error);
        }

        _legal.Notice(tenant.Id, documentId, PlatformTermsReminderStep.Manual);
        _legal.Audit(context.PlatformUserId, "platform.legal.reminder_sent", PlatformPermissions.LegalRemind, nameof(PlatformTenant), tenant.Id, new { version = state.RequiredVersion }, request.Reason);

        return await _legal.SaveAsync(cancellationToken)
            ? SendPlatformTermsReminderResponse.Success()
            : SendPlatformTermsReminderResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
