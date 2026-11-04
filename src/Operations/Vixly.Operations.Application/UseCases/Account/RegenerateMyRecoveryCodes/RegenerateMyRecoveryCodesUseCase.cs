using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Identity;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Staff;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Account.RegenerateMyRecoveryCodes;

// =========================================================
// FILE: RegenerateMyRecoveryCodesUseCase.cs
//
// PURPOSE:
// Replaces my ten recovery codes; the old ones stop working (Slice 1D).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Step-up; the codes are shown once
// =========================================================
public sealed class RegenerateMyRecoveryCodesUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsSecretGenerator _secrets;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public RegenerateMyRecoveryCodesUseCase(
        OperationsAccess access,
        IOperationsSecretGenerator secrets,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<RegenerateMyRecoveryCodesResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        RegenerateMyRecoveryCodesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _access.LoadAsync(context, cancellationToken) is not { } access)
        {
            return RegenerateMyRecoveryCodesResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (!access.User.HasSecondFactor || !await _access.IsFreshAsync(context, cancellationToken))
        {
            return RegenerateMyRecoveryCodesResponse.Failure(OperationsAccessErrors.StepUpRequired);
        }

        List<OperationsSecret> codes = Enumerable.Range(0, OperationsIdentityLimits.RecoveryCodeCount).Select(_ => _secrets.NewRecoveryCode()).ToList();
        access.User.ReplaceRecoveryCodes(codes.Select(code => code.Hash).ToList(), _timeProvider.GetUtcNow());
        _audit.Record(access.User.Id, "ops.account.recovery_codes_replaced", nameof(OperationsUser), access.User.Id);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? RegenerateMyRecoveryCodesResponse.Success(codes.Select(code => code.Value).ToList())
            : RegenerateMyRecoveryCodesResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
