using Vixly.Operations.Application.Holds;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Screens;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Screens;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Screens.GetOperationsScreenZelleQr;

// =========================================================
// FILE: GetOperationsScreenZelleQrUseCase.cs
//
// PURPOSE:
// The company’s Zelle QR picture, for the customer screen while it shows the Zelle step (Slice 5F step 3, P-9).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - No sign-in: the screen is known only by its own secret
// =========================================================
public sealed class GetOperationsScreenZelleQrUseCase
{
    private readonly OperationsScreenDesk _desk;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsFileStore _files;

    public GetOperationsScreenZelleQrUseCase(
        OperationsScreenDesk desk,
        IOperationsSettingsRepository settings,
        IOperationsFileStore files)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _files = files ?? throw new ArgumentNullException(nameof(files));
    }

    public async Task<GetOperationsScreenZelleQrResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsScreenZelleQrRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        // Only after the customer accepted the terms (P-9), and only while Zelle is on the screen.
        if (context.TenantId is null || await _desk.FromSecretAsync(request.Secret, cancellationToken) is not { Step: OperationsScreenStep.Zelle }
            || (await _settings.FindAsync(cancellationToken))?.ZelleQrKey is not { } key || await _files.ReadAsync(context.TenantId.Value, key, cancellationToken) is not { } content
            || OperationsDocumentInspector.TypeOf(content) is not { } type)
        {
            return GetOperationsScreenZelleQrResponse.Failure(OperationsPaymentErrors.NotFound);
        }

        return GetOperationsScreenZelleQrResponse.Success(content, type);
    }
}
