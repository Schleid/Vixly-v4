using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Inspection;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Inspection;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Inspection.CopyOperationsStartingList;

// =========================================================
// FILE: CopyOperationsStartingListUseCase.cs
//
// PURPOSE:
// Copies the starting restricted-items list into an empty company list, with a rule that uses it (Slice 3C, D-R5).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - The copy belongs to the company: it changes its entries freely (D-R5)
// - Names and what is needed in the language of the person copying; the words cover English, French and Spanish declarations
// =========================================================
public sealed class CopyOperationsStartingListUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsInspectionRepository _inspections;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public CopyOperationsStartingListUseCase(
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

    public async Task<CopyOperationsStartingListResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        CopyOperationsStartingListRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.InspectionConfigure, false, cancellationToken);

        if (refused is not null)
        {
            return CopyOperationsStartingListResponse.Failure(refused);
        }

        // Only into an empty list: a second copy (a double click) would add every entry twice.
        if ((await _inspections.ListItemsAsync(cancellationToken)).Count > 0)
        {
            return CopyOperationsStartingListResponse.Failure(OperationsInspectionErrors.ListNotEmpty);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        IReadOnlyList<OperationsStartingItem> starting = OperationsStartingList.In(request.Language);

        foreach (OperationsStartingItem entry in starting)
        {
            _inspections.Add(OperationsRestrictedItem.Create(context.RequiredTenantId, entry.Name, entry.Words, entry.Kind, null, null, entry.Requirement, utcNow));
        }

        if (!(await _inspections.ListRulesAsync(cancellationToken)).Any(rule => rule.Kind == OperationsInspectionRuleKind.ListedItems))
        {
            _inspections.Add(OperationsInspectionRule.Create(context.RequiredTenantId, OperationsInspectionRuleKind.ListedItems, null, utcNow));
        }

        _audit.Record(access!.User.Id, "ops.inspection.list_copied", nameof(OperationsRestrictedItem), null, after: new { items = starting.Count, language = request.Language });

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return CopyOperationsStartingListResponse.Success(starting.Count);
        }

        // Copied twice at once: the other copy is the list now.
        _unitOfWork.DiscardChanges();
        return CopyOperationsStartingListResponse.Failure((await _inspections.ListItemsAsync(cancellationToken)).Count > 0 ? OperationsInspectionErrors.ListNotEmpty : OperationsCommonErrors.ConcurrencyConflict);
    }
}
