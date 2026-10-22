using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Insurance;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Insurance;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Insurance.SaveOperationsLiabilityTerms;

// =========================================================
// FILE: SaveOperationsLiabilityTermsUseCase.cs
//
// PURPOSE:
// Sets what the company owes, for one transport mode, when the customer did not insure (Slice 5B, I-3).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.insurance.manage
// =========================================================
public sealed class SaveOperationsLiabilityTermsUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsInsuranceRepository _insurance;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SaveOperationsLiabilityTermsUseCase(
        OperationsAccess access,
        IOperationsInsuranceRepository insurance,
        IOperationsSettingsRepository settings,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _insurance = insurance ?? throw new ArgumentNullException(nameof(insurance));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SaveOperationsLiabilityTermsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SaveOperationsLiabilityTermsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.InsuranceManage, false, cancellationToken);

        if (refused is not null)
        {
            return SaveOperationsLiabilityTermsResponse.Failure(refused);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsWeightUnit unit = (await _settings.FindAsync(cancellationToken))?.WeightUnit ?? OperationsWeightUnit.Kilogram;
        OperationsLiabilityTerms? terms = await _insurance.FindLiabilityAsync(request.Mode, cancellationToken);

        try
        {
            if (terms is null)
            {
                terms = OperationsLiabilityTerms.Create(context.RequiredTenantId, request.Mode, utcNow);
                _insurance.Add(terms);
            }

            // The amount per weight is in the company's unit (lb or kg) as entered.
            terms.Change(request.Basis, request.Amount, unit, request.Cap, request.CustomerText, utcNow);
        }
        catch (DomainException exception)
        {
            return SaveOperationsLiabilityTermsResponse.Failure(exception.Error);
        }

        _audit.Record(access!.User.Id, "ops.insurance.liability_saved", nameof(OperationsLiabilityTerms), terms.Id,
            after: new { mode = terms.Mode.ToString(), basis = terms.Basis.ToString(), amount = terms.Amount, unit = terms.WeightUnit.ToString(), cap = terms.Cap });
        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? SaveOperationsLiabilityTermsResponse.Success()
            : SaveOperationsLiabilityTermsResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
