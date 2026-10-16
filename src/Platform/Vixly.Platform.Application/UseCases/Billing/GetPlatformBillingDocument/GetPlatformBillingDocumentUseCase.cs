using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Billing;
using Vixly.Platform.Domain.Billing;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Billing.GetPlatformBillingDocument;

// =========================================================
// FILE: GetPlatformBillingDocumentUseCase.cs
//
// PURPOSE:
// Returns the stored PDF of an invoice, credit note or receipt (D-B10).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class GetPlatformBillingDocumentUseCase
{
    private readonly PlatformBillingAdministration _billing;

    public GetPlatformBillingDocumentUseCase(
        PlatformBillingAdministration billing)
    {
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
    }

    public async Task<GetPlatformBillingDocumentResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        GetPlatformBillingDocumentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _billing.AuthorizeAsync(context, PlatformPermissions.BillingView, requireStepUp: false, cancellationToken) is { } refused)
        {
            return GetPlatformBillingDocumentResponse.Failure(refused);
        }

        PlatformBillingDocument? document = Enum.IsDefined(request.Kind)
            ? await _billing.Store.GetDocumentAsync(request.Kind, request.OwnerId, cancellationToken)
            : null;

        return document is null
            ? GetPlatformBillingDocumentResponse.Failure(PlatformBillingErrors.DocumentNotFound)
            : GetPlatformBillingDocumentResponse.Success(document.Content, document.FileName);
    }
}
