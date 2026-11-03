using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Company.RemoveOperationsDocumentLogo;

// =========================================================
// FILE: RemoveOperationsDocumentLogoUseCase.cs
//
// PURPOSE:
// Removes the logo from the company's documents and labels; they show the company name again (owner, 2026-10-28).
//
// RESPONSIBILITIES:
// - Who and when are kept; the change is audited
//
// IMPORTANT NOTES:
// - The people who edit the company's contacts (Owner, Administrators), with a fresh confirmation
// =========================================================
public sealed class RemoveOperationsDocumentLogoUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public RemoveOperationsDocumentLogoUseCase(OperationsAccess access, IOperationsSettingsRepository settings, OperationsAuditTrail audit, IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<RemoveOperationsDocumentLogoResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        RemoveOperationsDocumentLogoRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.CompanyEditContacts, true, cancellationToken);

        if (refused is not null)
        {
            return RemoveOperationsDocumentLogoResponse.Failure(refused);
        }

        OperationsTenantSettings? settings = await _settings.FindAsync(cancellationToken);

        if (settings?.DocumentLogoImageId is null)
        {
            return RemoveOperationsDocumentLogoResponse.Success();
        }

        settings.ChangeDocumentLogo(null, null, access!.User.Id, _timeProvider.GetUtcNow());
        _audit.Record(access.User.Id, "ops.company.document_logo_removed", nameof(OperationsTenantSettings), settings.Id);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? RemoveOperationsDocumentLogoResponse.Success()
            : RemoveOperationsDocumentLogoResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
