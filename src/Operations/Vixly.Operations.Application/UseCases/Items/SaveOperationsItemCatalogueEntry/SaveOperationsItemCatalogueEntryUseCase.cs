using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Items;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Items;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Items.SaveOperationsItemCatalogueEntry;

// =========================================================
// FILE: SaveOperationsItemCatalogueEntryUseCase.cs
//
// PURPOSE:
// Adds or changes an item in the company's catalogue (owner, 2026-11-01).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Items already classified keep their code; the catalogue only helps the next ones
// =========================================================
public sealed class SaveOperationsItemCatalogueEntryUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsItemRepository _items;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SaveOperationsItemCatalogueEntryUseCase(OperationsAccess access, IOperationsItemRepository items, OperationsAuditTrail audit, IOperationsUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _items = items ?? throw new ArgumentNullException(nameof(items));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SaveOperationsItemCatalogueEntryResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SaveOperationsItemCatalogueEntryRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.ServicesManage, false, cancellationToken);

        if (refused is not null)
        {
            return SaveOperationsItemCatalogueEntryResponse.Failure(refused);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsItemCatalogueEntry entry;

        try
        {
            if (request.EntryId is { } entryId)
            {
                if (await _items.GetCatalogueEntryAsync(entryId, cancellationToken) is not { } found)
                {
                    return SaveOperationsItemCatalogueEntryResponse.Failure(OperationsItemErrors.CatalogueEntryNotFound);
                }

                entry = found;
                entry.Change(request.Description, request.Matches, request.HsCode, request.Unit, utcNow);
            }
            else
            {
                if ((await _items.ListCatalogueAsync(cancellationToken)).Count >= OperationsItemCatalogueEntry.MaxPerCompany)
                {
                    return SaveOperationsItemCatalogueEntryResponse.Failure(OperationsItemErrors.CatalogueFull);
                }

                entry = OperationsItemCatalogueEntry.Create(context.RequiredTenantId, request.Description, request.Matches, request.HsCode, request.Unit, utcNow);
                _items.AddCatalogueEntry(entry);
            }
        }
        catch (DomainException exception)
        {
            return SaveOperationsItemCatalogueEntryResponse.Failure(exception.Error);
        }

        _audit.Record(access!.User.Id, request.EntryId is null ? "ops.items.catalogue_added" : "ops.items.catalogue_changed", nameof(OperationsItemCatalogueEntry), entry.Id,
            after: new { entry.Description, code = entry.HsCode, unit = entry.Unit.ToString() });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? SaveOperationsItemCatalogueEntryResponse.Success(entry.Id)
            : SaveOperationsItemCatalogueEntryResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
