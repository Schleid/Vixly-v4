using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Branding;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Company.GetOperationsDocumentLogo;

// =========================================================
// FILE: GetOperationsDocumentLogoUseCase.cs
//
// PURPOSE:
// The company's document logo as it prints, or its black and white label copy, with who saved it and when (owner, 2026-10-28).
//
// RESPONSIBILITIES:
// - The company page shows it, so a saved logo is seen to be there
//
// IMPORTANT NOTES:
// - Staff who see the company page
// =========================================================
public sealed class GetOperationsDocumentLogoUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsSiteRepository _site;

    private readonly IOperationsUserRepository _users;

    public GetOperationsDocumentLogoUseCase(OperationsAccess access, IOperationsSettingsRepository settings, IOperationsSiteRepository site, IOperationsUserRepository users)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _site = site ?? throw new ArgumentNullException(nameof(site));
        _users = users ?? throw new ArgumentNullException(nameof(users));
    }

    public async Task<GetOperationsDocumentLogoResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsDocumentLogoRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.CompanyView, false, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsDocumentLogoResponse.Failure(refused);
        }

        bool canEdit = access!.HasTenantWide(OperationsPermissions.CompanyEditContacts);
        OperationsTenantSettings? settings = await _settings.FindAsync(cancellationToken);
        string? changedBy = settings?.DocumentLogoByUserId is { } userId ? (await _users.GetAsync(userId, cancellationToken))?.FullName : null;

        if ((request.Label ? settings?.LabelLogoImageId : settings?.DocumentLogoImageId) is not { } imageId
            || await _site.GetImageAsync(imageId, cancellationToken) is not { } image)
        {
            return GetOperationsDocumentLogoResponse.Success(null, 0, 0, 0, settings?.DocumentLogoUtc, changedBy, canEdit);
        }

        return GetOperationsDocumentLogoResponse.Success(image.Content, image.Width, image.Height, image.Content.Length, settings!.DocumentLogoUtc, changedBy, canEdit);
    }
}
