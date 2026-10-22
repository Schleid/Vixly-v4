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

namespace Vixly.Operations.Application.UseCases.Cashier.ChangeOperationsCardAcceptance;

// =========================================================
// FILE: ChangeOperationsCardAcceptanceUseCase.cs
//
// PURPOSE:
// Turns cards on or off for the company (Slice 5F step 4).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.settings.edit
// - On only while a Stripe account is connected
// =========================================================
public sealed class ChangeOperationsCardAcceptanceUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsCardRepository _cards;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public ChangeOperationsCardAcceptanceUseCase(
        OperationsAccess access,
        IOperationsCardRepository cards,
        IOperationsSettingsRepository settings,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _cards = cards ?? throw new ArgumentNullException(nameof(cards));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ChangeOperationsCardAcceptanceResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ChangeOperationsCardAcceptanceRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.SettingsEdit, false, cancellationToken);

        if (refused is not null)
        {
            return ChangeOperationsCardAcceptanceResponse.Failure(refused);
        }

        if (request.Accept && await _cards.GetProviderAsync(cancellationToken) is null)
        {
            return ChangeOperationsCardAcceptanceResponse.Failure(OperationsCardErrors.NotConnected);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsTenantSettings? settings = await _settings.FindAsync(cancellationToken);

        if (settings is null)
        {
            settings = OperationsTenantSettings.CreateDefault(context.RequiredTenantId, utcNow);
            _settings.Add(settings);
        }

        settings.ChangeCard(request.Accept, utcNow);
        _audit.Record(access!.User.Id, "ops.payments.card_accepted", nameof(OperationsTenantSettings), settings.Id, after: new { accept = request.Accept });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? ChangeOperationsCardAcceptanceResponse.Success()
            : ChangeOperationsCardAcceptanceResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
