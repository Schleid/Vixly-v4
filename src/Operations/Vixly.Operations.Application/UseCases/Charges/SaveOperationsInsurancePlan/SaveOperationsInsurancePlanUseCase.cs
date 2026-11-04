using Vixly.Contracts.Platform.Catalog;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Charges;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Charges.SaveOperationsInsurancePlan;

// =========================================================
// FILE: SaveOperationsInsurancePlanUseCase.cs
//
// PURPOSE:
// Sets the company's insurance plan, from now or from a later day: a percentage with a minimum, or value bands (Slice 4 and 4B).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class SaveOperationsInsurancePlanUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsChargeRepository _charges;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SaveOperationsInsurancePlanUseCase(
        OperationsAccess access,
        IOperationsChargeRepository charges,
        IOperationsSettingsRepository settings,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _charges = charges ?? throw new ArgumentNullException(nameof(charges));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SaveOperationsInsurancePlanResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SaveOperationsInsurancePlanRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.ChargesConfigure, false, cancellationToken);

        if (refused is not null)
        {
            return SaveOperationsInsurancePlanResponse.Failure(refused);
        }

        if (OperationsPriceText.InsuranceBands(request.BandsText) is not { } bands)
        {
            return SaveOperationsInsurancePlanResponse.Failure(OperationsChargeErrors.BandsInvalid);
        }

        OperationsInsuranceTerms terms = request.Method == OperationsInsuranceMethod.Percent
            ? new OperationsInsuranceTerms(request.Method, request.Percent, request.MinimumPremium, Array.Empty<OperationsInsuranceBand>(), request.MaxValue, request.Coverage)
            : new OperationsInsuranceTerms(request.Method, null, null, bands, request.MaxValue, request.Coverage);
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(context.RequiredTenantId, utcNow);
        OperationsInsurancePlan? plan = null;

        // Slice 5D: one of the mode's products, or a new one.
        if (request.PlanId is { } planId && (plan = await _charges.GetInsurancePlanAsync(planId, cancellationToken)) is null)
        {
            return SaveOperationsInsurancePlanResponse.Failure(OperationsChargeErrors.VersionNotFound);
        }

        try
        {
            DateTimeOffset? start = OperationsCharging.Start(request.EffectiveFrom, settings, utcNow);

            if (plan is null)
            {
                plan = OperationsInsurancePlan.Create(context.RequiredTenantId, request.Mode, terms, start, utcNow, request.Name);
                _charges.Add(plan);
            }
            else if (start is not null || (plan.TermsAt(utcNow) ?? plan.Versions.FirstOrDefault()) is not { } current || !Same(current, terms))
            {
                plan.Schedule(terms, start, utcNow);
            }

            plan.Describe(request.Name ?? plan.Name, request.PitchEn, request.PitchFr, request.PitchEs, request.Position, utcNow);
            plan.ChangeStatus(request.Active, utcNow);
        }
        catch (DomainException exception)
        {
            return SaveOperationsInsurancePlanResponse.Failure(exception.Error);
        }

        OperationsInsuranceVersion latest = plan.Versions[^1];
        _audit.Record(access!.User.Id, "ops.charges.insurance_saved", nameof(OperationsInsurancePlan), plan.Id, after: new
        {
            mode = plan.Mode.ToString(), name = plan.Name, position = plan.Position, method = latest.Method.ToString(), cover = latest.Coverage.Cover.ToString(), insurer = latest.Coverage.InsurerName,
            risks = latest.Coverage.Risks.ToString(), payout = latest.Coverage.Payout.ToString(), percent = latest.Percent, minimum = latest.MinimumPremium, bands = latest.Bands.Count, max = latest.MaxValue, active = plan.IsActive,
            effectiveFrom = latest.EffectiveFromUtc
        });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? SaveOperationsInsurancePlanResponse.Success()
            : SaveOperationsInsurancePlanResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }

    private static bool Same(OperationsInsuranceVersion version, OperationsInsuranceTerms terms) =>
        version.Method == terms.Method && version.Percent == terms.Percent && version.MinimumPremium == terms.MinimumPremium && version.MaxValue == terms.MaxValue
        && version.Bands.Count == terms.Bands.Count && version.Bands.Zip(terms.Bands.OrderBy(band => band.UpToValue)).All(pair => pair.First.UpToValue == pair.Second.UpToValue && pair.First.Premium == pair.Second.Premium)
        && version.Coverage.ToTerms() == OperationsInsuranceCoverage.Create(terms.Coverage ?? OperationsInsuranceCoverage.Defaults).ToTerms();
}
