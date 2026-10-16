using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Legal;
using Vixly.Platform.Domain.Legal;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Legal.GetPlatformLegalRegister;

// =========================================================
// FILE: GetPlatformLegalRegisterUseCase.cs
//
// PURPOSE:
// Pages the tenants that must accept the current required Terms, with their status (D-Lg12).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class GetPlatformLegalRegisterUseCase
{
    private readonly PlatformLegalAdministration _legal;

    public GetPlatformLegalRegisterUseCase(
        PlatformLegalAdministration legal)
    {
        _legal = legal ?? throw new ArgumentNullException(nameof(legal));
    }

    public async Task<GetPlatformLegalRegisterResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        GetPlatformLegalRegisterRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _legal.AuthorizeAsync(context, PlatformPermissions.LegalView, requireStepUp: false, cancellationToken) is { } refused)
        {
            return GetPlatformLegalRegisterResponse.Failure(refused);
        }

        if (await _legal.Store.LatestRequiredTermsAsync(cancellationToken) is not { } required)
        {
            return GetPlatformLegalRegisterResponse.Success(new PagedResult<PlatformLegalRegisterItem>(Array.Empty<PlatformLegalRegisterItem>(), PagedRequest.Create(request.Page, request.PageSize), 0), null);
        }

        return GetPlatformLegalRegisterResponse.Success(
            await _legal.Queries.RegisterAsync(required, new PlatformLegalRegisterFilter(request.Status, string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim()),
                PagedRequest.Create(request.Page, request.PageSize), _legal.UtcNow, cancellationToken),
            await _legal.Queries.GetDocumentAsync(required.Id, cancellationToken));
    }
}
