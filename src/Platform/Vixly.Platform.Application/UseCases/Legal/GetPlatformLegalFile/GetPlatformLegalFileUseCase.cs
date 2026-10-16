using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Legal;
using Vixly.Platform.Domain.Legal;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Legal.GetPlatformLegalFile;

// =========================================================
// FILE: GetPlatformLegalFileUseCase.cs
//
// PURPOSE:
// Downloads one language file of a legal document for staff.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class GetPlatformLegalFileUseCase
{
    private readonly PlatformLegalAdministration _legal;

    public GetPlatformLegalFileUseCase(
        PlatformLegalAdministration legal)
    {
        _legal = legal ?? throw new ArgumentNullException(nameof(legal));
    }

    public async Task<GetPlatformLegalFileResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        GetPlatformLegalFileRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _legal.AuthorizeAsync(context, PlatformPermissions.LegalView, requireStepUp: false, cancellationToken) is { } refused)
        {
            return GetPlatformLegalFileResponse.Failure(refused);
        }

        PlatformLegalFileContent? file = await _legal.Queries.GetFileContentAsync(request.DocumentId, request.Language?.Trim().ToLowerInvariant() ?? string.Empty, cancellationToken);
        return file is null ? GetPlatformLegalFileResponse.Failure(PlatformLegalErrors.DocumentNotFound) : GetPlatformLegalFileResponse.Success(file);
    }
}
