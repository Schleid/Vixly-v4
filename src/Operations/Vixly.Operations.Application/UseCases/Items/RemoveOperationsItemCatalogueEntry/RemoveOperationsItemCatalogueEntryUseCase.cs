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

namespace Vixly.Operations.Application.UseCases.Items.RemoveOperationsItemCatalogueEntry;

// =========================================================
// FILE: RemoveOperationsItemCatalogueEntryUseCase.cs
//
// PURPOSE:
// Removes an item from the company's catalogue (owner, 2026-11-01).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Items already classified keep their code
// =========================================================
public sealed class RemoveOperationsItemCatalogueEntryUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsItemRepository _items;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    public RemoveOperationsItemCatalogueEntryUseCase(OperationsAccess access, IOperationsItemRepository items, OperationsAuditTrail audit, IOperationsUnitOfWork unitOfWork)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _items = items ?? throw new ArgumentNullException(nameof(items));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<RemoveOperationsItemCatalogueEntryResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        RemoveOperationsItemCatalogueEntryRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.ServicesManage, false, cancellationToken);

        if (refused is not null)
        {
            return RemoveOperationsItemCatalogueEntryResponse.Failure(refused);
        }

        if (await _items.GetCatalogueEntryAsync(request.EntryId, cancellationToken) is not { } entry)
        {
            return RemoveOperationsItemCatalogueEntryResponse.Failure(OperationsItemErrors.CatalogueEntryNotFound);
        }

        _items.RemoveCatalogueEntry(entry);
        _audit.Record(access!.User.Id, "ops.items.catalogue_removed", nameof(OperationsItemCatalogueEntry), entry.Id, before: new { entry.Description, code = entry.HsCode });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? RemoveOperationsItemCatalogueEntryResponse.Success()
            : RemoveOperationsItemCatalogueEntryResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
