using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Application.UseCases.Locations.SaveOperationsCountryContact;

// =========================================================
// FILE: SaveOperationsCountryContactUseCase.cs
//
// PURPOSE:
// Creates or changes how customers in one country reach the tenant (Slice 1B).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Shown on the public site at once: contacts have no draft
// =========================================================
public sealed class SaveOperationsCountryContactUseCase
{
    private readonly IOperationsLocationRepository _locations;

    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SaveOperationsCountryContactUseCase(
        IOperationsLocationRepository locations,
        OperationsOwnerCheck owner,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SaveOperationsCountryContactResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SaveOperationsCountryContactRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.RefuseUnlessAllowedAsync(context, Vixly.Operations.Domain.Access.OperationsPermissions.BranchesManage, cancellationToken) is { } refused)
        {
            return SaveOperationsCountryContactResponse.Failure(refused);
        }

        OperationsCountryContact? contact = request.ContactId is { } id ? await _locations.GetContactAsync(id, cancellationToken) : null;

        if (request.ContactId is not null && contact is null)
        {
            return SaveOperationsCountryContactResponse.Failure(OperationsCommonErrors.NotFound);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        try
        {
            if (contact is null)
            {
                contact = OperationsCountryContact.Create(context.RequiredTenantId, request.CountryCode, request.Label, request.Phones, request.WhatsApp, request.Email, request.Hours, request.SortOrder, utcNow);
                _locations.Add(contact);
            }
            else
            {
                contact.Update(request.CountryCode, request.Label, request.Phones, request.WhatsApp, request.Email, request.Hours, request.SortOrder, utcNow);
            }
        }
        catch (DomainException exception)
        {
            return SaveOperationsCountryContactResponse.Failure(exception.Error);
        }

        _audit.Record(context.UserId, request.ContactId is null ? "ops.locations.contact_created" : "ops.locations.contact_changed", nameof(OperationsCountryContact), contact.Id, after: new { country = contact.CountryCode });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? SaveOperationsCountryContactResponse.Success(contact.Id)
            : SaveOperationsCountryContactResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
