using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Payments;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Payments.UpdatePlatformZelleAccount;

// =========================================================
// FILE: UpdatePlatformZelleAccountUseCase.cs
//
// PURPOSE:
// Saves Vixly's Zelle details and turns Zelle at checkout on or off.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class UpdatePlatformZelleAccountUseCase
{
    private readonly PlatformZelleAdministration _zelle;

    public UpdatePlatformZelleAccountUseCase(
        PlatformZelleAdministration zelle)
    {
        _zelle = zelle ?? throw new ArgumentNullException(nameof(zelle));
    }

    public async Task<UpdatePlatformZelleAccountResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        UpdatePlatformZelleAccountRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _zelle.AuthorizeAsync(context, PlatformPermissions.PaymentsConfigure, requireStepUp: true, cancellationToken) is { } refused)
        {
            return UpdatePlatformZelleAccountResponse.Failure(refused);
        }

        PlatformZelleAccount account = await _zelle.AccountAsync(cancellationToken);

        try
        {
            account.Update(request.Enabled, request.RecipientName, request.Email, request.Tag, request.Phone, context.PlatformUserId, _zelle.UtcNow);
        }
        catch (DomainException exception)
        {
            return UpdatePlatformZelleAccountResponse.Failure(exception.Error);
        }

        _zelle.Payments.Audit(context, "platform.payments.zelle_updated", PlatformPermissions.PaymentsConfigure, nameof(PlatformZelleAccount), account.Id,
            new { enabled = account.Enabled, recipient = account.RecipientName, email = account.Email, tag = account.Tag, phone = account.Phone });

        return await _zelle.SaveAsync(cancellationToken)
            ? UpdatePlatformZelleAccountResponse.Success()
            : UpdatePlatformZelleAccountResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
