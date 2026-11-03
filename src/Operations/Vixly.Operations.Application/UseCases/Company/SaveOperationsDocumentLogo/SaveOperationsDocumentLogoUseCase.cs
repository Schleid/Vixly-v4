using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Branding;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Company.SaveOperationsDocumentLogo;

// =========================================================
// FILE: SaveOperationsDocumentLogoUseCase.cs
//
// PURPOSE:
// Saves the logo printed on the company's documents and labels (owner, 2026-10-28).
//
// RESPONSIBILITIES:
// - A PNG within the image limits; its black and white copy for thermal labels is made at once
// - Who and when are kept; the change is audited
//
// IMPORTANT NOTES:
// - The people who edit the company's contacts (Owner, Administrators), with a fresh confirmation
// - The file type comes from its bytes, never from its name
// =========================================================
public sealed class SaveOperationsDocumentLogoUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsSiteRepository _site;

    private readonly IOperationsImageInspector _inspector;

    private readonly IOperationsLabelLogoMaker _labels;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SaveOperationsDocumentLogoUseCase(
        OperationsAccess access,
        IOperationsSettingsRepository settings,
        IOperationsSiteRepository site,
        IOperationsImageInspector inspector,
        IOperationsLabelLogoMaker labels,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _site = site ?? throw new ArgumentNullException(nameof(site));
        _inspector = inspector ?? throw new ArgumentNullException(nameof(inspector));
        _labels = labels ?? throw new ArgumentNullException(nameof(labels));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SaveOperationsDocumentLogoResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SaveOperationsDocumentLogoRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.CompanyEditContacts, true, cancellationToken);

        if (refused is not null)
        {
            return SaveOperationsDocumentLogoResponse.Failure(refused);
        }

        if (request.Content.Length == 0 || request.Content.Length > OperationsSiteImage.MaxBytes)
        {
            return SaveOperationsDocumentLogoResponse.Failure(OperationsBrandingErrors.ImageTooLarge);
        }

        // Anything but a PNG (an unreadable file too) gets the same answer: the document logo is a PNG.
        if (_inspector.Inspect(request.Content) is not { ContentType: "image/png" } info)
        {
            return SaveOperationsDocumentLogoResponse.Failure(OperationsBrandingErrors.DocumentLogoPngOnly);
        }

        if (_labels.MakeLabelCopy(request.Content) is not { } labelCopy || _inspector.Inspect(labelCopy) is not { } labelInfo)
        {
            return SaveOperationsDocumentLogoResponse.Failure(OperationsBrandingErrors.DocumentLogoUnreadable);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsSiteImage logo;
        OperationsSiteImage label;

        try
        {
            logo = OperationsSiteImage.Create(context.RequiredTenantId, OperationsSiteImageKind.DocumentLogo, info.ContentType, request.Content, info.Width, info.Height, info.Sha256, utcNow);
            label = OperationsSiteImage.Create(context.RequiredTenantId, OperationsSiteImageKind.LabelLogo, labelInfo.ContentType, labelCopy, labelInfo.Width, labelInfo.Height, labelInfo.Sha256, utcNow);
        }
        catch (DomainException exception)
        {
            return SaveOperationsDocumentLogoResponse.Failure(exception.Error);
        }

        OperationsTenantSettings? settings = await _settings.FindAsync(cancellationToken);

        if (settings is null)
        {
            settings = OperationsTenantSettings.CreateDefault(context.RequiredTenantId, utcNow);
            _settings.Add(settings);
        }

        bool replaced = settings.DocumentLogoImageId is not null;
        _site.AddImage(logo);
        _site.AddImage(label);
        settings.ChangeDocumentLogo(logo.Id, label.Id, access!.User.Id, utcNow);
        _audit.Record(access.User.Id, "ops.company.document_logo_saved", nameof(OperationsTenantSettings), settings.Id,
            arguments: new { replaced, width = info.Width, height = info.Height, bytes = request.Content.Length });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? SaveOperationsDocumentLogoResponse.Success()
            : SaveOperationsDocumentLogoResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
