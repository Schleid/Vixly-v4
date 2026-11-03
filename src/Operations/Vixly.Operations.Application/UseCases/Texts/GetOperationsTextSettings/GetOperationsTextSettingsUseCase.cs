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

namespace Vixly.Operations.Application.UseCases.Texts.GetOperationsTextSettings;

// =========================================================
// FILE: GetOperationsTextSettingsUseCase.cs
//
// PURPOSE:
// Settings → Messages (Slice 9): the company’s Twilio account, the notices it sends by text, its WhatsApp templates, and the recent messages.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.settings.view; never the token itself
// =========================================================
public sealed class GetOperationsTextSettingsUseCase
{
    private readonly OperationsAccess _access;

    private readonly OperationsTextDesk _texts;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsPlanFeatures _features;

    public GetOperationsTextSettingsUseCase(
        OperationsAccess access,
        OperationsTextDesk texts,
        IOperationsSettingsRepository settings,
        IOperationsPlanFeatures features)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _texts = texts ?? throw new ArgumentNullException(nameof(texts));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _features = features ?? throw new ArgumentNullException(nameof(features));
    }

    public async Task<GetOperationsTextSettingsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsTextSettingsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.SettingsView, false, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsTextSettingsResponse.Failure(refused);
        }

        return GetOperationsTextSettingsResponse.Success((await _features.GetAsync(context.RequiredTenantId, cancellationToken)).TextMessages,
            await _texts.Texts.GetProviderAsync(cancellationToken), (await _settings.FindAsync(cancellationToken))?.TextNotices ?? OperationsTextNotices.None,
            await _texts.Texts.ListMessagesAsync(100, cancellationToken), access!.Has(OperationsPermissions.SettingsEdit));
    }
}
