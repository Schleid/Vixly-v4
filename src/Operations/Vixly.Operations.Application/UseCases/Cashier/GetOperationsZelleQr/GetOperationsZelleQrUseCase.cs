using Vixly.Operations.Application.Billing;
using Vixly.Operations.Application.Holds;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Cashier.GetOperationsZelleQr;

// =========================================================
// FILE: GetOperationsZelleQrUseCase.cs
//
// PURPOSE:
// The company's Zelle QR picture, for the cashier and the settings page (Slice 5F, P-8).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.payments.take or ops.settings.view
// =========================================================
public sealed class GetOperationsZelleQrUseCase
{
    private readonly OperationsBillingDesk _desk;

    private readonly OperationsAccess _access;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsFileStore _files;

    public GetOperationsZelleQrUseCase(
        OperationsBillingDesk desk,
        OperationsAccess access,
        IOperationsSettingsRepository settings,
        IOperationsFileStore files)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _files = files ?? throw new ArgumentNullException(nameof(files));
    }

    public async Task<GetOperationsZelleQrResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsZelleQrRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? cashier, IReadOnlyList<OperationsWarehouse> _, Error? notCashier) = await _desk.OpenAsync(context, OperationsPermissions.PaymentsTake, cancellationToken);
        (OperationsAccessView? _, Error? notSettings) = notCashier is null ? (cashier, null) : await _access.RequireAsync(context, OperationsPermissions.SettingsView, false, cancellationToken);

        if (notSettings is not null)
        {
            return GetOperationsZelleQrResponse.Failure(notSettings);
        }

        if ((await _settings.FindAsync(cancellationToken))?.ZelleQrKey is not { } key || await _files.ReadAsync(context.RequiredTenantId, key, cancellationToken) is not { } content
            || OperationsDocumentInspector.TypeOf(content) is not { } type)
        {
            return GetOperationsZelleQrResponse.Failure(OperationsPaymentErrors.NotFound);
        }

        return GetOperationsZelleQrResponse.Success(content, type);
    }
}
