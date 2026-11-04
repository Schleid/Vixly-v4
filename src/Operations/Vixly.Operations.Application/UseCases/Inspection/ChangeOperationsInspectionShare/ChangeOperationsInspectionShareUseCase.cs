using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Inspection;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Inspection.ChangeOperationsInspectionShare;

// =========================================================
// FILE: ChangeOperationsInspectionShareUseCase.cs
//
// PURPOSE:
// Changes how many parcels in a hundred are opened at random (Slice 3C, D-R4).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Company-wide (ops.inspection.configure); 0 turns the random share off, the rules still apply
// =========================================================
public sealed class ChangeOperationsInspectionShareUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public ChangeOperationsInspectionShareUseCase(
        OperationsAccess access,
        IOperationsSettingsRepository settings,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ChangeOperationsInspectionShareResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ChangeOperationsInspectionShareRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.InspectionConfigure, false, cancellationToken);

        if (refused is not null)
        {
            return ChangeOperationsInspectionShareResponse.Failure(refused);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsTenantSettings? saved = await _settings.FindAsync(cancellationToken);
        OperationsTenantSettings settings = saved ?? OperationsTenantSettings.CreateDefault(context.RequiredTenantId, utcNow);
        int before = settings.InspectionPercent;

        try
        {
            settings.ChangeInspectionPercent(request.Percent, utcNow);
        }
        catch (DomainException exception)
        {
            return ChangeOperationsInspectionShareResponse.Failure(exception.Error);
        }

        if (saved is null)
        {
            _settings.Add(settings);
        }

        _audit.Record(access!.User.Id, "ops.inspection.share_changed", nameof(OperationsTenantSettings), settings.Id, before: new { percent = before }, after: new { percent = settings.InspectionPercent });

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return ChangeOperationsInspectionShareResponse.Success();
        }

        // Saved twice at once (the company's first setting): the other one wrote the same share.
        _unitOfWork.DiscardChanges();
        return (await _settings.FindAsync(cancellationToken))?.InspectionPercent == request.Percent
            ? ChangeOperationsInspectionShareResponse.Success()
            : ChangeOperationsInspectionShareResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
