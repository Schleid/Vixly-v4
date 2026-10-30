using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Devices;
using Vixly.Operations.Application.Identity;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Staff;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Account.ConfirmOperationsSecondFactor;

// =========================================================
// FILE: ConfirmOperationsSecondFactorUseCase.cs
//
// PURPOSE:
// Confirms my second factor for the next 5 minutes of sensitive actions (step-up, Slice 1D).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class ConfirmOperationsSecondFactorUseCase
{
    private readonly OperationsAccess _access;

    private readonly OperationsStepUp _stepUp;

    private readonly IOperationsSessionRepository _sessions;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public ConfirmOperationsSecondFactorUseCase(
        OperationsAccess access,
        OperationsStepUp stepUp,
        IOperationsSessionRepository sessions,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _stepUp = stepUp ?? throw new ArgumentNullException(nameof(stepUp));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ConfirmOperationsSecondFactorResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ConfirmOperationsSecondFactorRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _access.LoadAsync(context, cancellationToken) is not { } access || context.SessionId is not { } sessionId)
        {
            return ConfirmOperationsSecondFactorResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (_stepUp.Check(access.User, request.Code) is { } refused)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return ConfirmOperationsSecondFactorResponse.Failure(refused);
        }

        OperationsUserSession? session = await _sessions.GetAsync(sessionId, cancellationToken);

        if (session is null)
        {
            return ConfirmOperationsSecondFactorResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        session.ConfirmSecondFactor(_timeProvider.GetUtcNow());

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? ConfirmOperationsSecondFactorResponse.Success()
            : ConfirmOperationsSecondFactorResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
