using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Legal;
using Vixly.Platform.Domain.Legal;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Legal.GetPublicLegalFile;

// =========================================================
// FILE: GetPublicLegalFileUseCase.cs
//
// PURPOSE:
// Downloads a published legal document in one language (public pages, owners).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Anonymous: published documents are public
// =========================================================
public sealed class GetPublicLegalFileUseCase
{
    private readonly PlatformLegalAdministration _legal;

    public GetPublicLegalFileUseCase(
        PlatformLegalAdministration legal)
    {
        _legal = legal ?? throw new ArgumentNullException(nameof(legal));
    }

    public async Task<GetPublicLegalFileResponse> ExecuteAsync(
        PlatformAnonymousAuthorizationContext context,
        GetPublicLegalFileRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformLegalFileContent? file = await _legal.Queries.GetFileContentAsync(request.DocumentId, request.Language?.Trim().ToLowerInvariant() ?? string.Empty, cancellationToken);

        // Drafts are never public.
        return file is { Status: PlatformLegalDocumentStatus.Published or PlatformLegalDocumentStatus.Superseded }
            ? GetPublicLegalFileResponse.Success(file)
            : GetPublicLegalFileResponse.Failure(PlatformLegalErrors.DocumentNotFound);
    }
}
