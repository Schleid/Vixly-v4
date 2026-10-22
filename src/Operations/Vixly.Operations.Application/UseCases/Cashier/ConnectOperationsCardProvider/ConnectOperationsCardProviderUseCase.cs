using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Cards;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Cashier.ConnectOperationsCardProvider;

// =========================================================
// FILE: ConnectOperationsCardProviderUseCase.cs
//
// PURPOSE:
// Connects the company's own Stripe account with its keys (Slice 5F step 4; owner 2026-10-21).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.settings.edit and a fresh second factor
// - The keys are checked with Stripe, then kept protected with the server's key; never logged, never shown again
// =========================================================
public sealed class ConnectOperationsCardProviderUseCase
{
    private readonly OperationsAccess _access;

    private readonly Vixly.Operations.Application.Cards.OperationsCardDesk _desk;

    private readonly IOperationsCardRepository _cards;

    private readonly IOperationsCardGateway _gateway;

    private readonly IOperationsSecretProtector _protector;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public ConnectOperationsCardProviderUseCase(
        OperationsAccess access,
        Vixly.Operations.Application.Cards.OperationsCardDesk desk,
        IOperationsCardRepository cards,
        IOperationsCardGateway gateway,
        IOperationsSecretProtector protector,
        IOperationsSettingsRepository settings,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _cards = cards ?? throw new ArgumentNullException(nameof(cards));
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        _protector = protector ?? throw new ArgumentNullException(nameof(protector));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ConnectOperationsCardProviderResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ConnectOperationsCardProviderRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        // Who receives the company's card money changes only after a fresh second factor.
        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.SettingsEdit, true, cancellationToken);

        if (refused is not null)
        {
            return ConnectOperationsCardProviderResponse.Failure(refused);
        }

        string secret = request.SecretKey?.Trim() ?? string.Empty;
        string publishable = request.PublishableKey?.Trim() ?? string.Empty;
        string webhook = request.WebhookSecret?.Trim() ?? string.Empty;

        if (!OperationsCardProvider.KeysLookRight(secret, publishable, webhook))
        {
            return ConnectOperationsCardProviderResponse.Failure(OperationsCardErrors.KeysInvalid);
        }

        // Stripe answers for the key before anything is kept.
        string accountName;

        try
        {
            accountName = await _gateway.CheckAsync(secret, cancellationToken);
        }
        catch (OperationsCardRefusedException)
        {
            return ConnectOperationsCardProviderResponse.Failure(OperationsCardErrors.KeysInvalid);
        }
        catch (OperationsCardUnavailableException)
        {
            return ConnectOperationsCardProviderResponse.Failure(OperationsCardErrors.ProviderUnavailable);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsCardProvider? provider = await _cards.GetProviderAsync(cancellationToken);
        string? before = provider?.KeyHint;

        // Review: another key may be another Stripe account; what was started on the old one is closed there first.
        if (provider is not null && _protector.Unprotect(provider.SecretKeyProtected) is var oldKey && !string.Equals(oldKey, secret, StringComparison.Ordinal)
            && !await _desk.CloseOpenAsync(oldKey, cancellationToken))
        {
            _unitOfWork.DiscardChanges();
            return ConnectOperationsCardProviderResponse.Failure(OperationsCardErrors.PaymentsOpen);
        }

        try
        {
            if (provider is null)
            {
                provider = OperationsCardProvider.Connect(context.RequiredTenantId, secret, _protector.Protect(secret), publishable, _protector.Protect(webhook), accountName, access!.User.Id, utcNow);
                _cards.Add(provider);
            }
            else
            {
                provider.Change(secret, _protector.Protect(secret), publishable, _protector.Protect(webhook), accountName, access!.User.Id, utcNow);
            }
        }
        catch (DomainException exception)
        {
            return ConnectOperationsCardProviderResponse.Failure(exception.Error);
        }

        OperationsTenantSettings? settings = await _settings.FindAsync(cancellationToken);

        if (settings is null)
        {
            settings = OperationsTenantSettings.CreateDefault(context.RequiredTenantId, utcNow);
            _settings.Add(settings);
        }

        settings.ChangeCard(true, utcNow);

        // Never the keys: their last characters, the mode and the account's name.
        _audit.Record(access.User.Id, "ops.payments.card_connected", nameof(OperationsCardProvider), provider.Id,
            before: before is null ? null : new { key = "…" + before }, after: new { key = "…" + provider.KeyHint, live = provider.Live, account = provider.AccountName });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? ConnectOperationsCardProviderResponse.Success()
            : ConnectOperationsCardProviderResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
