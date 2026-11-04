using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Inspection;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Inspection.SaveOperationsInspectionRule;

// =========================================================
// FILE: SaveOperationsInspectionRuleUseCase.cs
//
// PURPOSE:
// Adds an inspection rule, or takes one in or out of use (Slice 3C).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Company-wide (ops.inspection.configure); a rule never changes its kind or value, it is taken out of use and a new one added
// =========================================================
public sealed class SaveOperationsInspectionRuleUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsInspectionRepository _inspections;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SaveOperationsInspectionRuleUseCase(
        OperationsAccess access,
        IOperationsInspectionRepository inspections,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _inspections = inspections ?? throw new ArgumentNullException(nameof(inspections));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SaveOperationsInspectionRuleResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SaveOperationsInspectionRuleRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.InspectionConfigure, false, cancellationToken);

        if (refused is not null)
        {
            return SaveOperationsInspectionRuleResponse.Failure(refused);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsInspectionRule rule;

        try
        {
            if (request.RuleId is { } ruleId)
            {
                if (await _inspections.GetRuleAsync(ruleId, cancellationToken) is not { } found)
                {
                    return SaveOperationsInspectionRuleResponse.Failure(OperationsInspectionErrors.RuleNotFound);
                }

                rule = found;
                rule.ChangeActive(request.Active, utcNow);
            }
            else
            {
                IReadOnlyList<OperationsInspectionRule> rules = await _inspections.ListRulesAsync(cancellationToken);

                if (rules.Count >= OperationsInspectionLimits.MaxRules)
                {
                    return SaveOperationsInspectionRuleResponse.Failure(OperationsInspectionErrors.TooMany);
                }

                rule = OperationsInspectionRule.Create(context.RequiredTenantId, request.Kind, request.Value, utcNow);

                // The same rule twice adds nothing: the earlier one is put back in use (a double click, ADR-0019).
                if (rules.FirstOrDefault(item => item.Kind == rule.Kind && string.Equals(item.Value, rule.Value, StringComparison.OrdinalIgnoreCase)) is { } same && await _inspections.GetRuleAsync(same.Id, cancellationToken) is { } existing)
                {
                    existing.ChangeActive(true, utcNow);
                    rule = existing;
                }
                else
                {
                    _inspections.Add(rule);
                }
            }
        }
        catch (DomainException exception)
        {
            return SaveOperationsInspectionRuleResponse.Failure(exception.Error);
        }

        _audit.Record(access!.User.Id, "ops.inspection.rule_saved", nameof(OperationsInspectionRule), rule.Id, after: new { kind = rule.Kind.ToString(), value = rule.Value, active = rule.IsActive });

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return SaveOperationsInspectionRuleResponse.Success(rule.Id);
        }

        // The same rule added twice at once: the other one saved it (ADR-0019).
        _unitOfWork.DiscardChanges();
        return request.RuleId is null && (await _inspections.ListRulesAsync(cancellationToken)).FirstOrDefault(item => item.Kind == rule.Kind && string.Equals(item.Value, rule.Value, StringComparison.OrdinalIgnoreCase)) is { } raced
            ? SaveOperationsInspectionRuleResponse.Success(raced.Id)
            : SaveOperationsInspectionRuleResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
