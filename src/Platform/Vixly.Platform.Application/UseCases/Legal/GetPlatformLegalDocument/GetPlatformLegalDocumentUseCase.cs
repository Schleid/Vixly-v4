using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Legal;
using Vixly.Platform.Domain.Legal;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Legal.GetPlatformLegalDocument;

// =========================================================
// FILE: GetPlatformLegalDocumentUseCase.cs
//
// PURPOSE:
// Reads a legal document version with its files, included versions and the reader's actions.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class GetPlatformLegalDocumentUseCase
{
    private readonly PlatformLegalAdministration _legal;

    public GetPlatformLegalDocumentUseCase(
        PlatformLegalAdministration legal)
    {
        _legal = legal ?? throw new ArgumentNullException(nameof(legal));
    }

    public async Task<GetPlatformLegalDocumentResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        GetPlatformLegalDocumentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _legal.AuthorizeAsync(context, PlatformPermissions.LegalView, requireStepUp: false, cancellationToken) is { } refused)
        {
            return GetPlatformLegalDocumentResponse.Failure(refused);
        }

        PlatformLegalDocumentItem? item = await _legal.Queries.GetDocumentAsync(request.DocumentId, cancellationToken);
        PlatformLegalDocument? document = item is null ? null : await _legal.Store.GetDocumentAsync(request.DocumentId, cancellationToken);

        if (item is null || document is null)
        {
            return GetPlatformLegalDocumentResponse.Failure(PlatformLegalErrors.DocumentNotFound);
        }

        IReadOnlyList<PlatformLegalDocumentItem> published = await _legal.Queries.ListPublishedAsync(cancellationToken);
        PlatformLegalDocumentItem[] parts = document.Parts
            .Select(part => published.FirstOrDefault(option => option.DocumentId == part.IncludedDocumentId)
                ?? new PlatformLegalDocumentItem(part.IncludedDocumentId, part.Kind, part.Version, PlatformLegalDocumentStatus.Superseded, false, null, null, null, null, item.CreatedUtc))
            .ToArray();

        return GetPlatformLegalDocumentResponse.Success(new PlatformLegalDocumentDetail(
            item,
            await _legal.Queries.ListFilesAsync(request.DocumentId, cancellationToken),
            parts,
            document.Kind == PlatformLegalDocumentKind.Terms ? published.Where(option => option.Kind != PlatformLegalDocumentKind.Terms).ToArray() : Array.Empty<PlatformLegalDocumentItem>(),
            await _legal.AbilitiesAsync(context, cancellationToken)));
    }
}
