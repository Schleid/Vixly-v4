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

namespace Vixly.Operations.Application.UseCases.Texts.DisconnectOperationsTextProvider;

// =========================================================
// FILE: DisconnectOperationsTextProviderUseCase.cs
//
// PURPOSE:
// Disconnects the company’s Twilio account (Slice 9); notices go by email again.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.settings.edit with the second check
// =========================================================
public sealed class DisconnectOperationsTextProviderUseCase
{
    private readonly OperationsAccess _access;

    private readonly OperationsTextDesk _texts;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public DisconnectOperationsTextProviderUseCase(
        OperationsAccess access,
        OperationsTextDesk texts,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _texts = texts ?? throw new ArgumentNullException(nameof(texts));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<DisconnectOperationsTextProviderResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        DisconnectOperationsTextProviderRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.SettingsEdit, true, cancellationToken);

        if (refused is not null)
        {
            return DisconnectOperationsTextProviderResponse.Failure(refused);
        }

        if (await _texts.Texts.GetProviderAsync(cancellationToken) is not { } provider)
        {
            return DisconnectOperationsTextProviderResponse.Success();
        }

        _texts.Texts.Remove(provider);
        _audit.Record(access!.User.Id, "ops.settings.texts_disconnected", nameof(OperationsTextProvider), provider.Id, before: new { token = "…" + provider.TokenHint });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? DisconnectOperationsTextProviderResponse.Success()
            : DisconnectOperationsTextProviderResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
