using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Legal;
using Vixly.Platform.Domain.Legal;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Legal.ListPlatformLegalDocuments;

// =========================================================
// FILE: ListPlatformLegalDocumentsUseCase.cs
//
// PURPOSE:
// Lists legal document versions, newest first, optionally of one kind.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class ListPlatformLegalDocumentsUseCase
{
    private readonly PlatformLegalAdministration _legal;

    public ListPlatformLegalDocumentsUseCase(
        PlatformLegalAdministration legal)
    {
        _legal = legal ?? throw new ArgumentNullException(nameof(legal));
    }

    public async Task<ListPlatformLegalDocumentsResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ListPlatformLegalDocumentsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _legal.AuthorizeAsync(context, PlatformPermissions.LegalView, requireStepUp: false, cancellationToken) is { } refused)
        {
            return ListPlatformLegalDocumentsResponse.Failure(refused);
        }

        return ListPlatformLegalDocumentsResponse.Success(
            await _legal.Queries.ListDocumentsAsync(request.Kind, Vixly.Shared.Kernel.Paging.PagedRequest.Create(request.Page, request.PageSize), cancellationToken),
            await _legal.AbilitiesAsync(context, cancellationToken));
    }
}
