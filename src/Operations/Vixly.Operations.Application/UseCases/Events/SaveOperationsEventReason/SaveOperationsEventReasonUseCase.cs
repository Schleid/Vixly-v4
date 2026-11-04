using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Events;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Events.SaveOperationsEventReason;

// =========================================================
// FILE: SaveOperationsEventReasonUseCase.cs
//
// PURPOSE:
// Add a cause of the company's own, or change one (Slice 6C, E-3).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.settings.edit
// - Events keep the words they were written with
// =========================================================
public sealed class SaveOperationsEventReasonUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsEventRepository _events;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SaveOperationsEventReasonUseCase(
        OperationsAccess access,
        IOperationsEventRepository events,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SaveOperationsEventReasonResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SaveOperationsEventReasonRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.SettingsEdit, false, cancellationToken);

        if (refused is not null)
        {
            return SaveOperationsEventReasonResponse.Failure(refused);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsEventReason? reason = null;

        if (request.ReasonId is { } id && (reason = await _events.GetReasonAsync(id, cancellationToken)) is null)
        {
            return SaveOperationsEventReasonResponse.Failure(OperationsEventErrors.NotFound);
        }

        try
        {
            if (reason is null)
            {
                IReadOnlyList<OperationsEventReason> existing = await _events.ListReasonsAsync(cancellationToken);

                // The same cause added twice (a double click): the one already there.
                if (existing.FirstOrDefault(item => item.Kind == request.Kind && string.Equals(item.TextEn, request.TextEn?.Trim(), StringComparison.OrdinalIgnoreCase)) is { } same)
                {
                    return SaveOperationsEventReasonResponse.Success(same.Id);
                }

                if (existing.Count >= OperationsEventReason.MaxPerCompany)
                {
                    return SaveOperationsEventReasonResponse.Failure(OperationsEventErrors.TooManyReasons);
                }

                reason = OperationsEventReason.Create(context.RequiredTenantId, request.Kind, request.TextEn, request.TextFr, request.TextEs, utcNow);
                _events.Add(reason);
            }
            else
            {
                reason.Change(request.TextEn, request.TextFr, request.TextEs, request.Active, utcNow);
            }
        }
        catch (DomainException exception)
        {
            return SaveOperationsEventReasonResponse.Failure(exception.Error);
        }

        _audit.Record(access!.User.Id, "ops.events.cause_saved", nameof(OperationsEventReason), reason.Id, after: new { kind = reason.Kind.ToString(), en = reason.TextEn, fr = reason.TextFr, es = reason.TextEs, active = reason.IsActive });
        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? SaveOperationsEventReasonResponse.Success(reason.Id)
            : SaveOperationsEventReasonResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
