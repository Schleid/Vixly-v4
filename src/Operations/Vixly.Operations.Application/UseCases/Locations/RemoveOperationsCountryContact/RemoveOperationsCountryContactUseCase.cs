using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;

namespace Vixly.Operations.Application.UseCases.Locations.RemoveOperationsCountryContact;

// =========================================================
// FILE: RemoveOperationsCountryContactUseCase.cs
//
// PURPOSE:
// Removes a country contact (Slice 1B).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Owner only until Slice 1C
// =========================================================
public sealed class RemoveOperationsCountryContactUseCase
{
    private readonly IOperationsLocationRepository _locations;

    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    public RemoveOperationsCountryContactUseCase(
        IOperationsLocationRepository locations,
        OperationsOwnerCheck owner,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork)
    {
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<RemoveOperationsCountryContactResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        RemoveOperationsCountryContactRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.RefuseUnlessAllowedAsync(context, Vixly.Operations.Domain.Access.OperationsPermissions.BranchesManage, cancellationToken) is { } refused)
        {
            return RemoveOperationsCountryContactResponse.Failure(refused);
        }

        OperationsCountryContact? contact = await _locations.GetContactAsync(request.ContactId, cancellationToken);

        if (contact is null)
        {
            return RemoveOperationsCountryContactResponse.Failure(OperationsCommonErrors.NotFound);
        }

        _locations.Remove(contact);
        _audit.Record(context.UserId, "ops.locations.contact_removed", nameof(OperationsCountryContact), contact.Id, before: new { country = contact.CountryCode });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? RemoveOperationsCountryContactResponse.Success()
            : RemoveOperationsCountryContactResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
