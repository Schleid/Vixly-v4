using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Texts;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Texts;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Texts.ConnectOperationsTextProvider;

// =========================================================
// FILE: ConnectOperationsTextProviderUseCase.cs
//
// PURPOSE:
// Connects the company’s own Twilio account, or changes its senders (Slice 9).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.settings.edit with the second check; the plan must include text messages
// - The token is checked with Twilio, protected, and never shown again (its last characters only)
// =========================================================
public sealed class ConnectOperationsTextProviderUseCase
{
    private readonly OperationsAccess _access;

    private readonly OperationsTextDesk _texts;

    private readonly IOperationsSecretProtector _protector;

    private readonly IOperationsPlanFeatures _features;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public ConnectOperationsTextProviderUseCase(
        OperationsAccess access,
        OperationsTextDesk texts,
        IOperationsSecretProtector protector,
        IOperationsPlanFeatures features,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _texts = texts ?? throw new ArgumentNullException(nameof(texts));
        _protector = protector ?? throw new ArgumentNullException(nameof(protector));
        _features = features ?? throw new ArgumentNullException(nameof(features));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ConnectOperationsTextProviderResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ConnectOperationsTextProviderRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.SettingsEdit, true, cancellationToken);

        if (refused is not null)
        {
            return ConnectOperationsTextProviderResponse.Failure(refused);
        }

        if (!(await _features.GetAsync(context.RequiredTenantId, cancellationToken)).TextMessages)
        {
            return ConnectOperationsTextProviderResponse.Failure(OperationsTextErrors.FeatureOff);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsTextProvider? provider = await _texts.Texts.GetProviderAsync(cancellationToken);
        string accountSid = (request.AccountSid ?? string.Empty).Trim();
        string? token = string.IsNullOrWhiteSpace(request.AuthToken) ? null : request.AuthToken.Trim();

        try
        {
            // Only the senders: the account and token stay as they are.
            if (token is null && provider is not null && (accountSid.Length == 0 || accountSid == provider.AccountSid))
            {
                provider.Senders(request.SmsFrom, request.WhatsAppFrom, utcNow);
            }
            else
            {
                if (token is null || !OperationsTextProvider.KeysLookRight(accountSid, token))
                {
                    return ConnectOperationsTextProviderResponse.Failure(OperationsTextErrors.KeysInvalid);
                }

                string name;

                try
                {
                    name = await _texts.Gateway.CheckAsync(accountSid, token, cancellationToken);
                }
                catch (OperationsTextRefusedException)
                {
                    return ConnectOperationsTextProviderResponse.Failure(OperationsTextErrors.KeysInvalid);
                }
                catch (OperationsTextUnavailableException)
                {
                    return ConnectOperationsTextProviderResponse.Failure(OperationsTextErrors.ProviderUnavailable);
                }

                if (provider is null)
                {
                    provider = OperationsTextProvider.Connect(context.RequiredTenantId, accountSid, token, _protector.Protect(token, OperationsSecretPurposes.TwilioToken), name,
                        request.SmsFrom, request.WhatsAppFrom, access!.User.Id, utcNow);
                    _texts.Texts.Add(provider);
                }
                else
                {
                    provider.Change(accountSid, token, _protector.Protect(token, OperationsSecretPurposes.TwilioToken), name, request.SmsFrom, request.WhatsAppFrom,
                        access!.User.Id, utcNow);
                }
            }

            provider.ReportTo(request.CallbackUrl, utcNow);
        }
        catch (DomainException exception)
        {
            _unitOfWork.DiscardChanges();
            return ConnectOperationsTextProviderResponse.Failure(exception.Error);
        }

        _audit.Record(access!.User.Id, "ops.settings.texts_connected", nameof(OperationsTextProvider), provider.Id,
            after: new { token = "…" + provider.TokenHint, provider.AccountName, sms = provider.SmsFrom is not null, whatsApp = provider.WhatsAppFrom is not null });

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return ConnectOperationsTextProviderResponse.Success();
        }

        _unitOfWork.DiscardChanges();
        return ConnectOperationsTextProviderResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
